/*

ArmatureCleanup - a simple script to clean up an armature after multiple assets
have been messily applied to it, resulting in duplicate bones.

Copyright (c) 2023 SophieBlue

*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.UIElements;
using VRC.Dynamics;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Dynamics.Contact.Components;
using VRC.SDK3.Dynamics.PhysBone.Components;

namespace SophieBlue.ArmatureCleanup {

    // useful extension methods
    public static class ExtensionMethods {
        public static string GetPath(this Transform current) {
            if (current.parent == null)
                return "/" + current.name;
            return current.parent.GetPath() + "/" + current.name;
        }

        public static string GetPath(this Component component) {
            return component.transform.GetPath() + "/" + component.GetType().ToString();
        }
    }


    [ExecuteInEditMode]
    public class ArmatureCleanup {

        // data from the user
        private GameObject _avatar;

        // a class to hold bones we're going to move
        struct MoveBone {
            public Transform Source;
            public Transform Target;

            public MoveBone(Transform source, Transform target) {
                Source = source;
                Target = target;
            }
        }
        private Dictionary<int, MoveBone> moveBones = new Dictionary<int, MoveBone>();


        public ArmatureCleanup() {
            Undo.undoRedoPerformed += AssetDatabase.SaveAssets;
        }

        // setters
        public void setAvatar(GameObject avatar) {
            _avatar = avatar;
        }

        void findExtraBones(Transform parent) {

            // find child transforms of this object
            for (int i = 0; i < parent.childCount; i++) {
                Transform bone = parent.GetChild(i);

                // is this bone named the same as its parent?
                if (bone.gameObject.name == parent.gameObject.name) {
                    Debug.Log("Bone " + bone.GetPath() + " needs merging into parent " + parent.GetPath());

                    moveBones.Add(bone.gameObject.GetInstanceID(), new MoveBone(bone, parent));
                }

                // go deeper
                findExtraBones(bone);
            }
        }

        // move all the bones which are children of bones we're going to merge
        void moveExtraChildBones() {
            // go through each of the bones we're planning to merge into their parent,
            // looking for child bones so we can reparent those
            foreach (MoveBone item in moveBones.Values) {

                // find all children
                for (int i = 0; i < item.Source.childCount; i++) {
                    Transform bone = item.Source.GetChild(i);

                    Debug.Log("Found childbone " + bone.GetPath());

                    // if it's not found in the list, then re-parent it to
                    // its current parent's new target
                    if (! moveBones.ContainsKey(bone.gameObject.GetInstanceID())) {
                        Debug.Log("Moving bone " + bone.GetPath() +
                                  " to new parent " + item.Target.GetPath());
                        Undo.SetTransformParent(bone, item.Target, "Reparent bone " + bone.GetPath());
                    }
                }
            }
        }

        //
        // Copy components of the given type from the source object to the target
        //
        private void CopyComponents<T>(Transform sourceObj, Transform targetObj) where T: class {
            foreach (T item in sourceObj.gameObject.GetComponents<T>()) {
                Debug.Log("Copying " + (item as Component) +
                          " from " + sourceObj.GetPath() +
                          " to " + targetObj.GetPath());

                T target = Undo.AddComponent(targetObj.gameObject, typeof(T)) as T;
                EditorUtility.CopySerialized(item as UnityEngine.Object, target as UnityEngine.Object);
            }
        }

        // Update physbone root transform and colliders
        private void UpdatePhysbones(GameObject sourceObj) {

            foreach (VRCPhysBone item in sourceObj.GetComponentsInChildren<VRCPhysBone>(true)) {
                Undo.RecordObject(item, "Changing physbone target");

                // update the root transform
                var root = item.GetRootTransform();
                if (root != null) {
                    int rootId = root.gameObject.GetInstanceID();
                    MoveBone target;
                    if (moveBones.TryGetValue(rootId, out target)) {
                        item.rootTransform = target.Target;
                    }
                }

                // update all colliders
                for (int i = 0; i < item.colliders.Count; i++) {
                    int cId = (item.colliders[i] as VRCPhysBoneCollider).gameObject.GetInstanceID();
                    MoveBone cTarget;
                    if (moveBones.TryGetValue(cId, out cTarget)) {
                        VRCPhysBoneCollider cTargetCollider = cTarget.Target.gameObject.GetComponent<VRCPhysBoneCollider>();
                        if (cTargetCollider != null) {
                            item.colliders[i] = cTarget.Target.gameObject.GetComponent<VRCPhysBoneCollider>();
                        }
                    }
                }
            }
        }

        // update physbone collider root transforms
        private void UpdatePhysboneColliders(GameObject sourceObj) {

            foreach (VRCPhysBoneCollider item in sourceObj.GetComponentsInChildren<VRCPhysBoneCollider>(true)) {
                // update the root transform
                var root = item.GetRootTransform();
                if (root != null) {
                    int rootId = root.gameObject.GetInstanceID();
                    MoveBone target;
                    if (moveBones.TryGetValue(rootId, out target)) {
                        Undo.RecordObject(item.gameObject, "Changing physbone collider root transform");
                        item.rootTransform = target.Target;
                    }
                }
            }
        }

        // update contacts root transforms
        private void UpdateContacts<T>(GameObject sourceObj) where T: class {
            Debug.Log("Updating Contacts of type " + typeof(T));

            foreach (T item in sourceObj.GetComponentsInChildren<T>(true)) {
                // update the root transform
                var root = (item as ContactBase).GetRootTransform();
                if (root != null) {
                    int rootId = root.gameObject.GetInstanceID();
                    MoveBone target;
                    if (moveBones.TryGetValue(rootId, out target)) {
                        Undo.RecordObject((item as Component).gameObject, "Changing contact root transform");
                        (item as ContactBase).rootTransform = target.Target;
                    }
                }
            }
        }


        // update constraint sources
        private void UpdateConstraints<T>(GameObject sourceObj) where T: class {
            Debug.Log("Updating Constraints of type " + typeof(T));

            bool updated = false;
            foreach (T item in sourceObj.GetComponentsInChildren<T>(true)) {
                // get sources list
                List<ConstraintSource> sources = new List<ConstraintSource>();
                (item as IConstraint).GetSources(sources);

                // update source transforms
                for (int i = 0; i < sources.Count; i++) {
                    int rootId = sources[i].sourceTransform.gameObject.GetInstanceID();
                    MoveBone target;
                    if (moveBones.TryGetValue(rootId, out target)) {
                       ConstraintSource thingy = sources[i];
                       thingy.sourceTransform = target.Target;
                       sources[i] = thingy;
                       updated = true;
                    }
                }

                if (updated) {
                    Undo.RecordObject((item as Component).gameObject, "Changing constraint target");
                    // write back
                    (item as IConstraint).SetSources(sources);
                }
            }
        }

        // update station enter/exit points
        private void UpdateStations(GameObject sourceObj) {

            foreach (VRCStation item in sourceObj.GetComponentsInChildren<VRCStation>(true)) {

                if (item.stationEnterPlayerLocation != null) {
                    int id = item.stationEnterPlayerLocation.gameObject.GetInstanceID();
                    MoveBone target;
                    if (moveBones.TryGetValue(id, out target)) {
                        Undo.RecordObject(item.gameObject, "Changing station enter transform");
                        item.stationEnterPlayerLocation = target.Target;
                    }
                }

                if (item.stationExitPlayerLocation != null) {
                    int id = item.stationExitPlayerLocation.gameObject.GetInstanceID();
                    MoveBone target;
                    if (moveBones.TryGetValue(id, out target)) {
                        Undo.RecordObject(item.gameObject, "Changing station exit transform");
                        item.stationExitPlayerLocation = target.Target;
                    }
                }
            }
        }

        // update skinned mesh renderers root bones
        private void UpdateSkinnedMeshRenderers(GameObject avatar) {

            foreach (SkinnedMeshRenderer mesh in avatar.GetComponentsInChildren<SkinnedMeshRenderer>()) {
                // ignore anything without a root bone
                if (mesh.rootBone == null) {
                    continue;
                }

                int rootId = mesh.rootBone.gameObject.GetInstanceID();
                MoveBone target;

                // if the root bone is one we're moving, we'll rework the
                // renderer to use the target bones instead
                if (moveBones.TryGetValue(rootId, out target)) {

                    Undo.RecordObject(mesh, "Changing mesh bones");
                    Debug.Log("Changing mesh " + mesh.GetPath() + " root bone to " + target.Target.GetPath());
                    mesh.rootBone = target.Target;

                    // now we have to redo the whole bones list
                    List<Transform> meshBones = new List<Transform>();
                    foreach (Transform bone in mesh.bones) {
                        int boneId = bone.gameObject.GetInstanceID();
                        if (moveBones.TryGetValue(boneId, out target)) {
                            meshBones.Add(target.Target);
                        }
                        else {
                            meshBones.Add(bone);
                        }
                    }
                    mesh.bones = meshBones.ToArray();
                }
            }
        }

        [ContextMenu("Clean Up")]
        public void CleanUp() {
            if (_avatar == null) {
                Debug.LogError("You must assign a target avatar!");
                return;
            }

            // Get the armature and find the extra bones that we'll delete
            VRCAvatarDescriptor _avatarDescriptor = _avatar.GetComponentInChildren<VRCAvatarDescriptor>();
            if (_avatarDescriptor == null) {
                Debug.LogError("Cannot find VRCAvatarDescriptor on provided object");
                return;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Armature Cleanup");
            int undoGroupIndex = Undo.GetCurrentGroup();

            Debug.Log("Finding duplicate bones...");
            Transform root = _avatarDescriptor.transform.Find("Armature");
            moveBones.Clear();
            findExtraBones(root);

            Debug.Log("Moving child bones...");
            moveExtraChildBones();

            Debug.Log("Copying bone components...");
            foreach (MoveBone item in moveBones.Values) {
                CopyComponents<AimConstraint>(item.Source, item.Target);
                CopyComponents<Animation>(item.Source, item.Target);
                CopyComponents<Animator>(item.Source, item.Target);
                CopyComponents<AudioSource>(item.Source, item.Target);
                CopyComponents<Camera>(item.Source, item.Target);
                CopyComponents<Cloth>(item.Source, item.Target);
                CopyComponents<Collider>(item.Source, item.Target);
                CopyComponents<FlareLayer>(item.Source, item.Target);
                CopyComponents<CharacterJoint>(item.Source, item.Target);
                CopyComponents<ConfigurableJoint>(item.Source, item.Target);
                CopyComponents<HingeJoint>(item.Source, item.Target);
                CopyComponents<FixedJoint>(item.Source, item.Target);
                CopyComponents<SpringJoint>(item.Source, item.Target);
                CopyComponents<Light>(item.Source, item.Target);
                CopyComponents<LineRenderer>(item.Source, item.Target);
                CopyComponents<LookAtConstraint>(item.Source, item.Target);
                CopyComponents<MeshFilter>(item.Source, item.Target);
                CopyComponents<MeshRenderer>(item.Source, item.Target);
                CopyComponents<ParentConstraint>(item.Source, item.Target);
                CopyComponents<ParticleSystem>(item.Source, item.Target);
                CopyComponents<ParticleSystemRenderer>(item.Source, item.Target);
                CopyComponents<PositionConstraint>(item.Source, item.Target);
                CopyComponents<Rigidbody>(item.Source, item.Target);
                CopyComponents<RotationConstraint>(item.Source, item.Target);
                CopyComponents<ScaleConstraint>(item.Source, item.Target);
                CopyComponents<TrailRenderer>(item.Source, item.Target);
                CopyComponents<VRCContactReceiver>(item.Source, item.Target);
                CopyComponents<VRCContactSender>(item.Source, item.Target);
                CopyComponents<VRCPhysBone>(item.Source, item.Target);
                CopyComponents<VRCPhysBoneCollider>(item.Source, item.Target);
                CopyComponents<VRCStation>(item.Source, item.Target);
                CopyComponents<VRCSpatialAudioSource>(item.Source, item.Target);
                //CopyComponents<MeshParticleEmitter>(item.Source, item.Target);
                //CopyComponents<ParticleAnimtor>(item.Source, item.Target);
                //CopyComponents<ParticleRenderer>(item.Source, item.Target);
            }

            Debug.Log("Updating SkinnedMeshRenderers...");
            UpdateSkinnedMeshRenderers(_avatar);

            // Update components which may refer to now-moved transforms
            Debug.Log("Updating components...");
            UpdateContacts<VRCContactSender>(_avatar);
            UpdateContacts<VRCContactReceiver>(_avatar);
            UpdateConstraints<AimConstraint>(_avatar);
            UpdateConstraints<LookAtConstraint>(_avatar);
            UpdateConstraints<ParentConstraint>(_avatar);
            UpdateConstraints<PositionConstraint>(_avatar);
            UpdateConstraints<RotationConstraint>(_avatar);
            UpdateConstraints<ScaleConstraint>(_avatar);
            UpdatePhysboneColliders(_avatar);
            UpdatePhysbones(_avatar);
            UpdateStations(_avatar);

            // Finally, destroy all the duplicate bones
            foreach (MoveBone item in moveBones.Values) {
                Debug.Log("Destroying duplicate bone " + item.Source.GetPath());
                Undo.DestroyObjectImmediate(item.Source.gameObject);
            }
            Undo.CollapseUndoOperations(undoGroupIndex);
        }
    }
}
