using UnityEditor;

namespace MVCTool
{
    public class AvatarTab : EditorTab
    {
        public override string TabName => "Avatar";

        private AvatarTabSetupSection _setupSection;
        private AvatarTabBuildSection _buildSection;
        private AvatarTabUploadSection _uploadSection;

        private protected override void Load()
        {
            _setupSection = new AvatarTabSetupSection();
            _buildSection = new AvatarTabBuildSection();
            _uploadSection = new AvatarTabUploadSection();

            AddSection(_setupSection);
            AddSection(_buildSection);
            AddSection(_uploadSection);
        }

        private protected override void OnDraw()
        {
            
        }

        private protected override void OnDrawAfterSections()
        {
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