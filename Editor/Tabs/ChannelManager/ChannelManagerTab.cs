using System;
using UnityEditor;

namespace MVCTool
{
    public class ChannelManagerTab : EditorTab
    {
        public override string TabName => "Channel Manager";

        public ChannelManagerChannelSection ChannelSection { get; private set; }
        public ChannelManagerNonUnityContentSection NonUnityContentSection { get; private set; }
        public ChannelManagerUnityAssetsSection UnityAssetsSection { get; private set; }

        private protected override void Load()
        {
            ChannelSection = new ChannelManagerChannelSection();
            NonUnityContentSection = new ChannelManagerNonUnityContentSection();
            UnityAssetsSection = new ChannelManagerUnityAssetsSection();

            AddSection(ChannelSection);
            AddSection(NonUnityContentSection);
            AddSection(UnityAssetsSection);
        }

        private protected override void OnEnter()
        {

        }

        private protected override void OnExit()
        {

        }

        private protected override void OnDraw()
        {
            EditorGUI.BeginDisabledGroup(!LoginApi.IsLoggedIn || UnityAssetsSection.IsUploadingAsset);
        }

        private protected override void OnDrawAfterSections()
        {
            EditorGUI.EndDisabledGroup();

            if (!LoginApi.IsLoggedIn)
                EditorGUILayout.HelpBox("You must be logged in to build or upload asset bundles.", MessageType.Warning);
        }

        private protected override void OnReset()
        {
            
        }
    }
}