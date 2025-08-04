using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MVCTool
{
    public class ContentManagerUploadSection : EditorTabSection
    {
        public override string SectionName => "Upload";
        public override bool IsDisabled => !LoginApi.IsLoggedIn || _isUploadingContent;

        private string _currentSelectedPath = "";

        private bool _isUploadingContent = false;

        private string _uploadContentStatusMessage = null;
        private string _contentErrorMessage = null;

        private protected override void Load()
        {
            
        }

        public override void OnEnter()
        {
            _contentErrorMessage = null;
            _uploadContentStatusMessage = null;
        }

        public override void OnExit()
        {

        }

        private protected override void OnDraw()
        {
            string displayPath = string.IsNullOrEmpty(_currentSelectedPath) ? "None" : _currentSelectedPath;
            GUILayout.Label($"<b>Selected File:</b> {displayPath}", MVCTheme.RichTextLabelStyle);
            if (GUILayout.Button("Select File", GUILayout.Height(30)))
            {
                _currentSelectedPath = EditorUtility.OpenFilePanel("Select File", "", "");
            }

            bool isFileSelected = !string.IsNullOrEmpty(_currentSelectedPath);
            EditorGUI.BeginDisabledGroup(!isFileSelected || _isUploadingContent);
            if (GUILayout.Button($"Upload File to Channel", GUILayout.Height(30)))
            {
                UploadNonUnityContentToChannel(ChannelManagerSelectSection.ChannelID, _currentSelectedPath).Forget();
            }
            EditorGUI.EndDisabledGroup();

            if (!isFileSelected)
                EditorGUILayout.HelpBox("Please select a file to upload.", MessageType.Warning);

            if (!string.IsNullOrEmpty(_contentErrorMessage))
                EditorGUILayout.HelpBox(_contentErrorMessage, MessageType.Error);

            if (!string.IsNullOrEmpty(_uploadContentStatusMessage))
                EditorGUILayout.HelpBox(_uploadContentStatusMessage, MessageType.Info);
        }

        public override void Reset()
        {

        }

        private async UniTask UploadNonUnityContentToChannel(string channelID, string filePath)
        {
            _contentErrorMessage = null;
            _uploadContentStatusMessage = null;

            _isUploadingContent = true;

            try
            {
                await ContentManager.UploadContentToChannel(channelID, filePath);
                _uploadContentStatusMessage = $"{filePath} uploaded to channel: {channelID} successfully!";
            }
            catch (System.Exception e)
            {
                _contentErrorMessage = $"Upload failed: {e.Message}";
            }
            finally
            {
                ForceDraw(); // Force redraw to immediately show the status message
                _isUploadingContent = false;
            }
        }
    }
}