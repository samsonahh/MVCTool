using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace MVCTool
{
    public class AvatarTabUploadSection : EditorTabSection
    {
        public override string SectionName => "Upload";
        public override bool IsDisabled => !LoginApi.IsLoggedIn || IsUploading;

        public bool IsUploading { get; private set; } = false;

        private string _errorMessage = null;
        private string _uploadStatusMessage = null;

        private protected override void Load()
        {

        }

        public override void OnEnter()
        {
            _errorMessage = null;
            _uploadStatusMessage = null;
        }

        public override void OnExit()
        {

        }

        private protected override void OnDraw()
        {
            bool canUpload = ContentManager.BuiltAvatarPrefabData != null && !IsUploading;
            EditorGUI.BeginDisabledGroup(!canUpload);

            if (GUILayout.Button("Upload Avatar", GUILayout.Height(30)))
            {
                UploadAvatar().Forget();
            }

            EditorGUI.EndDisabledGroup();

            bool isBuildWarningVisible = ContentManager.BuiltAvatarPrefabData?.Prefab == null;
            if (LoginApi.IsLoggedIn && isBuildWarningVisible)
            {
                EditorGUILayout.HelpBox("Please build your avatar prefab to upload.", MessageType.Warning);
                _uploadStatusMessage = null;
            }

            if (!string.IsNullOrEmpty(_errorMessage))
                EditorGUILayout.HelpBox(_errorMessage, MessageType.Error);

            if (!string.IsNullOrEmpty(_uploadStatusMessage))
                EditorGUILayout.HelpBox(_uploadStatusMessage, MessageType.Info);
        }

        public override void Reset()
        {

        }

        private async UniTask UploadAvatar()
        {
            _errorMessage = null;
            _uploadStatusMessage = null;

            IsUploading = true;

            try
            {
                await ContentManager.UploadBuiltAvatarPrefab();
                _uploadStatusMessage = $"Avatar uploaded successfully!";
            }
            catch (System.Exception e)
            {
                _errorMessage = $"Upload failed: {e.Message}";
            }
            finally
            {
                IsUploading = false;
                ForceDraw(); // Force redraw to immediately show the status message
            }
        }
    }
}