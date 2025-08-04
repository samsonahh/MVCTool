using UnityEditor;

namespace MVCTool
{
    public class AvatarTab : EditorTab
    {
        public override string TabName => "Avatar";

        public AvatarTabSetupSection SetupSection { get; private set; }
        public AvatarTabBuildSection BuildSection { get; private set; }
        public AvatarTabUploadSection UploadSection { get; private set; }

        private protected override void Load()
        {
            SetupSection = new AvatarTabSetupSection();
            BuildSection = new AvatarTabBuildSection();
            UploadSection = new AvatarTabUploadSection();

            AddSection(SetupSection);
            AddSection(BuildSection);
            AddSection(UploadSection);
        }

        private protected override void OnDraw()
        {
            EditorGUI.BeginDisabledGroup(!LoginApi.IsLoggedIn);
        }

        private protected override void OnDrawAfterSections()
        {
            EditorGUI.EndDisabledGroup();

            if (!LoginApi.IsLoggedIn)
                EditorGUILayout.HelpBox("You must be logged in to build or upload asset bundles.", MessageType.Warning);
        }

        private protected override void OnEnter()
        {
            
        }

        private protected override void OnExit()
        {
            
        }

        private protected override void OnReset()
        {
            
        }
    }
}