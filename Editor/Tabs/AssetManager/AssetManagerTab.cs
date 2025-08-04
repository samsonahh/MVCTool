using UnityEditor;

namespace MVCTool
{
    public class AssetManagerTab : EditorTab
    {
        public override string TabName => "Asset";

        public AssetManagerBuildSection BuildSection { get; private set; }
        public AssetManagerUploadSection UploadSection { get; private set; }
        public AssetManagerDeleteSection DeleteSection { get; private set; }

        private protected override void Load()
        {
            BuildSection = new AssetManagerBuildSection();
            UploadSection = new AssetManagerUploadSection();
            DeleteSection = new AssetManagerDeleteSection();

            AddSection(BuildSection);
            AddSection(UploadSection);
            AddSection(DeleteSection);
        }

        private protected override void OnEnter()
        {
            
        }

        private protected override void OnExit()
        {

        }

        private protected override void OnDraw()
        {
            EditorGUI.BeginDisabledGroup(!LoginApi.IsLoggedIn);
        }

        private protected override void OnDrawAfterSections()
        {
            EditorGUI.EndDisabledGroup();

            if (!LoginApi.IsLoggedIn)
                EditorGUILayout.HelpBox("You must be logged in to manage assets.", MessageType.Warning);
        }

        private protected override void OnReset()
        {

        }
    }
}