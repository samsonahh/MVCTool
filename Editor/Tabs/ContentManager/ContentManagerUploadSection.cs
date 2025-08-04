using Cysharp.Threading.Tasks;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using UnityEditor;
using UnityEngine;

namespace MVCTool
{
    public class ContentManagerUploadSection : EditorTabSection
    {
        public override string SectionName => "Upload";
        public override bool IsDisabled => !LoginApi.IsLoggedIn || _isUploading;

        private string _currentSelectedPath = "";

        private bool _isUploading = false;

        private string _statusMessage = null;
        private string _errorMessage = null;

        // 360 content
        private bool _is360Content = false;
        private List<ContentManager._360Content.ProjectionTypeTags> _360ProjectionTypeTags = Enum.GetValues(typeof(ContentManager._360Content.ProjectionTypeTags)).Cast<ContentManager._360Content.ProjectionTypeTags>().ToList();
        private int _selected360ProjectionIndex = 0;
        private List<ContentManager._360Content.StereoFormatTags> _360StereoFormatTags = Enum.GetValues(typeof(ContentManager._360Content.StereoFormatTags)).Cast<ContentManager._360Content.StereoFormatTags>().ToList();
        private int _selected360StereoFormatIndex = 0;

        private protected override void Load()
        {
            
        }

        public override void OnEnter()
        {
            _errorMessage = null;
            _statusMessage = null;
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
            EditorGUI.BeginDisabledGroup(!isFileSelected || _isUploading);

            bool canBe360Content = ContentManager._360Content.HasValid360Extension(_currentSelectedPath);
            _is360Content = canBe360Content ? EditorGUILayout.Toggle("Is 360 Content", _is360Content) : false;
            if (_is360Content)
            {
                Draw360Options();
            }
            if (GUILayout.Button($"Upload File to Channel", GUILayout.Height(30)))
            {
                string overrideFilePath = null;
                if(canBe360Content)
                    overrideFilePath = ContentManager._360Content.Create360FilePath(_currentSelectedPath, 
                        _360ProjectionTypeTags[_selected360ProjectionIndex], 
                        _360StereoFormatTags[_selected360StereoFormatIndex]);

                UploadContentToChannel(ChannelManagerSelectSection.ChannelID, _currentSelectedPath, overrideFilePath).Forget();
            }
            EditorGUI.EndDisabledGroup();

            if (!isFileSelected)
                EditorGUILayout.HelpBox("Please select a file to upload.", MessageType.Warning);

            if (!string.IsNullOrEmpty(_errorMessage))
                EditorGUILayout.HelpBox(_errorMessage, MessageType.Error);

            if (!string.IsNullOrEmpty(_statusMessage))
                EditorGUILayout.HelpBox(_statusMessage, MessageType.Info);
        }

        public override void Reset()
        {

        }

        private async UniTask UploadContentToChannel(string channelID, string filePath, string overrideFilePath = null)
        {
            _errorMessage = null;
            _statusMessage = null;

            _isUploading = true;

            try
            {
                await ContentManager.UploadContentToChannel(channelID, filePath, overrideFilePath);
                _statusMessage = $"{filePath} uploaded to channel: {channelID} successfully!";
            }
            catch (System.Exception e)
            {
                _errorMessage = $"Upload failed: {e.Message}";
            }
            finally
            {
                ForceDraw(); // Force redraw to immediately show the status message
                _isUploading = false;
            }
        }

        private void Draw360Options()
        {
            GUILayout.Space(10);
            GUILayout.Label($"360 File Options:", EditorStyles.boldLabel);

            string[] projectionNames = _360ProjectionTypeTags
                .ConvertAll(tag => tag.ToString().TrimStart('_'))
                .ToArray();
            _selected360ProjectionIndex = EditorGUILayout.Popup("Projection Type", _selected360ProjectionIndex, projectionNames);
            
            string[] stereoNames = _360StereoFormatTags
                .ConvertAll(tag => tag.ToString().TrimStart('_'))
                .ToArray();
            _selected360StereoFormatIndex = EditorGUILayout.Popup("Stereo Format", _selected360StereoFormatIndex, stereoNames);
        }
    }
}