using System;
using UnityEditor;

namespace MVCTool
{
    public class ChannelManagerTab : EditorTab
    {
        public override string TabName => "Channel Manager";

        public const string ChannelIDEditorPrefsKey = "MVCTool_ChannelID";
        public string ChannelID { get; private set; } = null;

        private ChannelManagerNonUnityContentSection _nonUnityContentSection;
        private ChannelManagerUnityAssetsSection _unityAssetsSection;

        private protected override void Load()
        {
            _nonUnityContentSection = new ChannelManagerNonUnityContentSection();
            _unityAssetsSection = new ChannelManagerUnityAssetsSection();

            AddSection(_nonUnityContentSection);
            AddSection(_unityAssetsSection);

            ChannelID = EditorPrefs.GetString(ChannelIDEditorPrefsKey, null);
        }

        private protected override void OnEnter()
        {

        }

        private protected override void OnExit()
        {

        }

        private protected override void OnDraw()
        {
            EditorGUI.BeginDisabledGroup(!LoginApi.IsLoggedIn || _unityAssetsSection.IsUploadingAsset);

            ChannelID = EditorGUILayout.TextField("Channel ID", ChannelID);
            EditorGUILayout.Space(2.5f);
            MVCTheme.DrawSeparator();
        }

        private protected override void OnDrawAfterSections()
        {
            EditorGUI.EndDisabledGroup();

            if (!LoginApi.IsLoggedIn)
                EditorGUILayout.HelpBox("You must be logged in to build or upload asset bundles.", MessageType.Warning);
        }

        private protected override void OnReset()
        {
            EditorPrefs.DeleteKey(ChannelIDEditorPrefsKey); 
        }
    }
}