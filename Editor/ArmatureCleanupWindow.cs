/*

ArmatureCleanup - a simple script to clean up an armature after multiple assets
have been messily applied to it, resulting in duplicate bones.

Copyright (c) 2022 SophieBlue

*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;


namespace SophieBlue.ArmatureCleanup {
    public class ArmatureCleanupWindow : EditorWindow
    {
        private Vector2 scroll;

        // data from the user
        private GameObject _avatar;

        // Our class with the real logic
        private ArmatureCleanup armatureCleanup = new ArmatureCleanup();

        [MenuItem ("Tools/SophieBlue/Armature Cleanup")]
        public static void ShowWindow() {
            // Show existing window instance. If one doesn't exist, make one.
            var window = EditorWindow.GetWindow(typeof(ArmatureCleanupWindow));
            window.titleContent = new GUIContent("Armature Cleanup");
            window.Show();
        }

        private void Header() {
            GUIStyle styleTitle = new GUIStyle(GUI.skin.label);
            styleTitle.fontSize = 16;
            styleTitle.margin = new RectOffset(20, 20, 20, 20);
            EditorGUILayout.LabelField("Sophie's Armature Cleanup", styleTitle);
            EditorGUILayout.Space();

            // show the version
            GUIStyle styleVersion = new GUIStyle(GUI.skin.label);
            EditorGUILayout.LabelField(Version.VERSION, styleVersion);
            EditorGUILayout.Space();
        }

        private void MainOptions() {
            // The Avatar
            _avatar = EditorGUILayout.ObjectField(
                "Avatar", _avatar, typeof(GameObject), true) as GameObject;
        }

        private void ApplyOptions() {
            armatureCleanup.setAvatar(_avatar);
        }

        void OnGUI() {
            Header();

            scroll = EditorGUILayout.BeginScrollView(scroll);
            MainOptions();
            ApplyOptions();

            if (GUILayout.Button("Clean up!")) {
                armatureCleanup.CleanUp();
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
