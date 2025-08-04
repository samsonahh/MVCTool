using UnityEditor;

namespace MVCTool
{
    public class ContentManagerTab : EditorTab
    {
        public override string TabName => "Content Manager";

        public ContentManagerUploadSection UploadSection { get; private set; }
        public ContentManagerDeleteSection DeleteSection { get; private set; }

        private protected override void Load()
        {
            UploadSection = new ContentManagerUploadSection();
            DeleteSection = new ContentManagerDeleteSection();

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
                EditorGUILayout.HelpBox("You must be logged in to manage content.", MessageType.Warning);
        }

        private protected override void OnReset()
        {
            
        }
    }
}