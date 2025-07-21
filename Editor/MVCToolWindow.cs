using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MVCTool
{
    public class MVCToolWindow : EditorWindow
    {
        private EditorTabManager _tabManager;

        private const string SavedTabIndexSessionKey = "MVCTool_SavedTabIndex";

        private Vector2 _scrollPosition;

        [MenuItem("Window/MVCTool")]
        public static void ShowWindow()
        {
            GetWindow<MVCToolWindow>("MVC Tool");
        }

        private void OnEnable()
        {
            _tabManager = new EditorTabManager(
                new List<EditorTab>()
                {
                    new LoginTab(),
                    new ChannelUploadTab(),
                    new AvatarTab(),
                },
                this,
                SavedTabIndexSessionKey
            );
        }

        private void OnDisable()
        {
            _tabManager.Shutdown();
        }

        private void OnGUI()
        {
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 24,
                alignment = TextAnchor.MiddleCenter
            };
            GUILayout.Label($"MVC Tool: {_tabManager.CurrentTab.TabName}", titleStyle);

            GUILayout.Space(2.5f);

            _tabManager.DrawToolbar();

            DrawSeparator();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            _tabManager.Draw();

/*            GUILayout.Space(10);

            GUILayout.Label("Debug", EditorStyles.boldLabel);
            if (GUILayout.Button("Reset"))
            {
                _tabManager.Reset();
            }
            if (GUILayout.Button("Reload Domain"))
            {
                EditorUtility.RequestScriptReload();
            }*/

            EditorGUILayout.EndScrollView();
        }

        public static void DrawSeparator()
        {
            EditorGUILayout.Space(4);
            Rect rect = EditorGUILayout.GetControlRect(false, 2);
            EditorGUI.DrawRect(rect, Color.gray);
            EditorGUILayout.Space(4);
        }
    }
}
