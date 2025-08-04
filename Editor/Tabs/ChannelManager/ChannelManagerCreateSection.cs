using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MVCTool
{
    public class ChannelManagerCreateSection : EditorTabSection
    {
        public override string SectionName => "Create";
        public override bool IsDisabled => !LoginApi.IsLoggedIn || _isCreating;

        private string _channelName = null;

        private bool _isCreating = false;

        private string _statusMessage = null;
        private string _errorMessage = null;

        private protected override void Load()
        {

        }

        public override void OnEnter()
        {

        }

        public override void OnExit()
        {

        }

        private protected override void OnDraw()
        {
            _channelName = EditorGUILayout.TextField("Name", _channelName);

            if (GUILayout.Button("Create Channel", GUILayout.Height(30)))
            {
                CreateChannel(_channelName).Forget();
            }

            if(!string.IsNullOrEmpty(_statusMessage))
                EditorGUILayout.HelpBox(_statusMessage, MessageType.Info);

            if(!string.IsNullOrEmpty(_errorMessage))
                EditorGUILayout.HelpBox(_errorMessage, MessageType.Error);
        }

        public override void Reset()
        {

        }

        private async UniTask CreateChannel(string channelName)
        {
            _isCreating = true;
            _statusMessage = null;
            _errorMessage = null;

            try
            {
                await ChannelManager.CreateChannel(channelName);
                _statusMessage = $"Channel, {channelName}, created successfully.";
            }
            catch (System.Exception ex)
            {
                _errorMessage = $"Error creating channel: {ex.Message}";
            }
            finally
            {
                _isCreating = false;
                RefreshTab();
            }
        }
    }
}