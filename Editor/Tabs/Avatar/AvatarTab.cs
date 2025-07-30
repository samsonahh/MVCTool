using System;
using UnityEditor;
using static UnityEditor.EditorGUI;

namespace MVCTool
{
    public class AvatarTab : EditorTab
    {
        public override string TabName => "Avatar";

        private AvatarTabSetupSection setupSection;
        private AvatarTabBuildSection buildSection;
        private AvatarTabUploadSection uploadSection;

        private protected override void Load()
        {
            setupSection = new AvatarTabSetupSection();
            buildSection = new AvatarTabBuildSection();
            uploadSection = new AvatarTabUploadSection();

            AddSection(setupSection);
            AddSection(buildSection);
            AddSection(uploadSection);
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