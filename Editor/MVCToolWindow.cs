using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MVCTool
{
    public class MVCToolWindow : EditorWindow
    {
        public static MVCToolWindow Instance { get; private set; }

        private EditorTabManager _tabManager;

        private const string SavedTabIndexSessionKey = "MVCTool_SavedTabIndex";

        private Vector2 _scrollPosition;

        // Tabs
        public LoginTab LoginTab { get; private set; }
        public ChannelManagerTab ChannelManagerTab { get; private set; }
        public ContentManagerTab ContentManagerTab { get; private set; }
        public AssetManagerTab AssetManagerTab { get; private set; }
        public AvatarTab AvatarTab { get; private set; }

        [MenuItem("Window/MVCTool")]
        public static void ShowWindow()
        {
            Instance = GetWindow<MVCToolWindow>("MVC Tool");
            Instance.Focus();
        }

        private void OnEnable()
        {
            Instance = this;

            LoginTab = new LoginTab();
            ChannelManagerTab = new ChannelManagerTab();
            ContentManagerTab = new ContentManagerTab();
            AssetManagerTab = new AssetManagerTab();
            AvatarTab = new AvatarTab();

            _tabManager = new EditorTabManager(
                new List<EditorTab>()
                {
                    LoginTab,
                    ChannelManagerTab,
                    ContentManagerTab,
                    AssetManagerTab,
                    AvatarTab,
                },
                this,
                SavedTabIndexSessionKey
            );
        }

        private void OnDisable()
        {
            _tabManager.Shutdown();

            if(Instance == this)
                Instance = null;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
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

            MVCTheme.DrawSeparator();

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
    }
}
