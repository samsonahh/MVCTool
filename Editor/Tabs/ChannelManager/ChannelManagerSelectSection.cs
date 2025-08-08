using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEditor;

namespace MVCTool
{
    public class ChannelManagerSelectSection : EditorTabSection
    {
        public override string SectionName => "Select";
        public override bool IsDisabled => !LoginApi.IsLoggedIn;

        public const string ChannelIDEditorPrefsKey = "MVCTool_ChannelID";
        public static string ChannelID { get; private set; } = string.Empty;

        private List<(string channelID, string name)> _channels = new();
        private int _selectedChannelIndex = 0;

        private protected override void Load()
        {
            ChannelID = EditorPrefs.GetString(ChannelIDEditorPrefsKey, null);
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
            string channelID = EditorGUILayout.TextField("Selected Channel ID", ChannelID);
            if(channelID != ChannelID)
            {
                SetChannelID(channelID);
                _selectedChannelIndex = _channels.FindIndex(_channels => _channels.channelID == channelID);
            }

            DrawChannelsDropdown();
        }

        public override void Reset()
        {
            EditorPrefs.DeleteKey(ChannelIDEditorPrefsKey);
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
                _selectedChannelIndex = _channels.FindIndex(_channels => _channels.channelID == ChannelID);
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
            int newIndex = EditorGUILayout.Popup("My Channels", _selectedChannelIndex, contentNames);
            if (newIndex != _selectedChannelIndex)
            {
                _selectedChannelIndex = newIndex;
                SetChannelID(_channels[_selectedChannelIndex].channelID);
            }
        }

        private void SetChannelID(string channelID)
        {
            if (string.IsNullOrEmpty(channelID) || channelID == ChannelID)
                return;

            ChannelID = channelID;
            EditorPrefs.SetString(ChannelIDEditorPrefsKey, ChannelID);
        }
    }
}