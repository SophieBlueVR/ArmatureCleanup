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

    [ExecuteInEditMode]
    public class ArmatureCleanup {

        // data from the user
        private GameObject _avatar;

        // a class to hold bones we're going to move
        struct MoveBone {
            public Transform Bone;
            public Transform Target;

            public MoveBone(Transform bone, Transform target) {
                Bone = bone;
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
            List<Transform> childBones = new List<Transform>(parent.GetComponentsInChildren<Transform>(true));
            foreach (Transform bone in childBones) {
                // skip same object
                if (bone.gameObject.GetInstanceID() == parent.gameObject.GetInstanceID()) {
                    continue;
                }

                // skip non-direct children
                if (bone.parent.gameObject.GetInstanceID() != parent.gameObject.GetInstanceID()) {
                    continue;
                }

                // is this bone named the same as its parent?
                if (bone.gameObject.name == parent.gameObject.name) {
                    Debug.Log("Bone " + bone.gameObject.name +
                                " (" + bone.gameObject.GetInstanceID() + ") " +
                              " needs merging into parent: " + parent.gameObject.name +
                                " (" + parent.gameObject.GetInstanceID() + ")");

                    // we'll be migrating this one
                    moveBones.Add(bone.gameObject.GetInstanceID(), new MoveBone(bone, parent));
                }

                // go deeper
                findExtraBones(bone);
            }
        }

        //
        // Copy components of a specific type from the source object to the target
        //
        private void CopyComponents<T>(GameObject sourceObj, GameObject targetObj) where T: class {
            Debug.Log("Copying components of type " + typeof(T));
            foreach (T item in sourceObj.GetComponentsInChildren<T>(true)) {

                if (typeof(T) == typeof(VRCPhysBone)) {
                    var root = (item as VRCPhysBone).GetRootTransform();

                    if (root != null) {
                        // null this, it's not necessary
                        if (root = sourceObj.transform) {
                            (item as VRCPhysBone).rootTransform = null;
                        }
                        // otherwise we'll leave it as-is, event though this is kinda weird
                    }
                }

                T target = Undo.AddComponent(targetObj, typeof(T)) as T;
                EditorUtility.CopySerialized(item as UnityEngine.Object, target as UnityEngine.Object);
            }
        }

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

        private void UpdatePhysboneColliders(GameObject sourceObj) {

            foreach (VRCPhysBoneCollider item in sourceObj.GetComponentsInChildren<VRCPhysBoneCollider>(true)) {
                // update the root transform
                var root = item.GetRootTransform();
                if (root != null) {
                    int rootId = root.gameObject.GetInstanceID();
                    MoveBone target;
                    if (moveBones.TryGetValue(rootId, out target)) {
                        Undo.RecordObject(sourceObj, "Changing physbone collider root transform");
                        item.rootTransform = target.Target;
                    }
                }
            }
        }

        private void UpdateContacts<T>(GameObject sourceObj) where T: class {
            Debug.Log("Updating Contacts of type " + typeof(T));

            foreach (T item in sourceObj.GetComponentsInChildren<T>(true)) {
                // update the root transform
                var root = (item as ContactBase).GetRootTransform();
                if (root != null) {
                    int rootId = root.gameObject.GetInstanceID();
                    MoveBone target;
                    if (moveBones.TryGetValue(rootId, out target)) {
                        Undo.RecordObject(sourceObj, "Changing contact root transform");
                        (item as ContactBase).rootTransform = target.Target;
                    }
                }
            }
        }


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
                    Undo.RecordObject(sourceObj, "Changing constraint target");
                    // write back
                    (item as IConstraint).SetSources(sources);
                }
            }
        }

        private void UpdateStations(GameObject sourceObj) {

            foreach (VRCStation item in sourceObj.GetComponentsInChildren<VRCStation>(true)) {

                if (item.stationEnterPlayerLocation != null) {
                    int id = item.stationEnterPlayerLocation.gameObject.GetInstanceID();
                    MoveBone target;
                    if (moveBones.TryGetValue(id, out target)) {
                        Undo.RecordObject(sourceObj, "Changing station enter transform");
                        item.stationEnterPlayerLocation = target.Target;
                    }
                }

                if (item.stationExitPlayerLocation != null) {
                    int id = item.stationExitPlayerLocation.gameObject.GetInstanceID();
                    MoveBone target;
                    if (moveBones.TryGetValue(id, out target)) {
                        Undo.RecordObject(sourceObj, "Changing station exit transform");
                        item.stationExitPlayerLocation = target.Target;
                    }
                }
            }
        }

        private void UpdateSkinnedMeshRenderers(GameObject avatar) {
            // Find the skinned mesh renderers which use the wrong bones

            foreach (SkinnedMeshRenderer mesh in avatar.GetComponentsInChildren<SkinnedMeshRenderer>()) {

                // if it's got a root bone and we've found that in our "to move" list,
                // then we'll switch that to the target bone
                if (mesh.rootBone != null) {
                    MoveBone target;
                    int rootId = mesh.rootBone.gameObject.GetInstanceID();
                    if (moveBones.TryGetValue(rootId, out target)) {

                        // record changes we'll make to this thing
                        Undo.RecordObject(mesh, "Changing root bone");

                        Debug.Log("Setting root of " + mesh.gameObject.name + " to " + target.Target.gameObject.name);
                        mesh.rootBone = target.Target;
                    }
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


            Debug.Log("Copying bone components...");
            foreach (MoveBone item in moveBones.Values) {
                if (item.Bone.name == item.Target.name) {
                    Debug.Log("Copying components on bone " + item.Bone.name);

                    CopyComponents<AimConstraint>(item.Bone.gameObject, item.Target.gameObject);
                    CopyComponents<LookAtConstraint>(item.Bone.gameObject, item.Target.gameObject);
                    CopyComponents<ParentConstraint>(item.Bone.gameObject, item.Target.gameObject);
                    CopyComponents<PositionConstraint>(item.Bone.gameObject, item.Target.gameObject);
                    CopyComponents<RotationConstraint>(item.Bone.gameObject, item.Target.gameObject);
                    CopyComponents<ScaleConstraint>(item.Bone.gameObject, item.Target.gameObject);
                    CopyComponents<VRCContactReceiver>(item.Bone.gameObject, item.Target.gameObject);
                    CopyComponents<VRCContactSender>(item.Bone.gameObject, item.Target.gameObject);
                    CopyComponents<VRCPhysBone>(item.Bone.gameObject, item.Target.gameObject);
                    CopyComponents<VRCPhysBoneCollider>(item.Bone.gameObject, item.Target.gameObject);
                    CopyComponents<VRCStation>(item.Bone.gameObject, item.Target.gameObject);
                }
            }

            Debug.Log("Updating SkinnedMeshRenderers...");
            UpdateSkinnedMeshRenderers(_avatar);

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
                if (item.Bone.name == item.Target.name) {
                    Debug.Log("Destroying object " + item.Bone.name);
                    Undo.DestroyObjectImmediate(item.Bone.gameObject);
                }
            }

            Undo.CollapseUndoOperations(undoGroupIndex);
        }
    }
}
