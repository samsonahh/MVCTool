using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MVCTool
{
    public class ChannelManagerDeleteSection : EditorTabSection
    {
        public override string SectionName => "Delete";
        public override bool IsDisabled => !LoginApi.IsLoggedIn || _isDeleting;
        private List<(string channelID, string name)> _channels = new();
        private int _selectedChannelIndex = 0;

        private bool _isDeleting = false;

        private string _statusMessage = null;
        private string _errorMessage = null;

        private protected override void Load()
        {

        }

        public override void OnEnter()
        {
            FetchChannelList();
        }

        public override void OnExit()
        {

        }

        private protected override void OnDraw()
        {
            DrawChannelsDropdown();

            if (GUILayout.Button("Delete Channel", GUILayout.Height(30)))
            {
                DeleteChannel(_channels[_selectedChannelIndex].channelID).Forget();
            }

            if (!string.IsNullOrEmpty(_statusMessage))
                EditorGUILayout.HelpBox(_statusMessage, MessageType.Info);

            if (!string.IsNullOrEmpty(_errorMessage))
                EditorGUILayout.HelpBox(_errorMessage, MessageType.Error);
        }

        public override void Reset()
        {

        }

        private void FetchChannelList()
        {
            if (!LoginApi.IsLoggedIn)
            {
                _channels = new();
                _selectedChannelIndex = 0;
                return;
            }

            ChannelManager.GetMyChannels().ContinueWith(channels =>
            {
                _channels = channels;
                _selectedChannelIndex = 0;
            }).Forget();
        }

        private void DrawChannelsDropdown()
        {
            if (_channels.Count == 0)
            {
                EditorGUILayout.LabelField("You have no channels.");
                return;
            }

            string[] contentNames = _channels.ConvertAll(content => $"{content.name}: {content.channelID}").ToArray();
            _selectedChannelIndex = EditorGUILayout.Popup("My Channels", _selectedChannelIndex, contentNames);
        }

        private async UniTask DeleteChannel(string channelID)
        {
            _isDeleting = true;
            _statusMessage = null;
            _errorMessage = null;

            try
            {
                await ChannelManager.DeleteChannel(channelID);
                _statusMessage = $"Channel, {channelID}, deleted successfully.";
            }
            catch (System.Exception ex)
            {
                _errorMessage = $"Error deleting channel: {ex.Message}";
            }
            finally
            {
                _isDeleting = false;
                RefreshTab();
            }
        }
    }
}