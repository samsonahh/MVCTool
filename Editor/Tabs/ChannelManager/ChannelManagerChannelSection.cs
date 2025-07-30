using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MVCTool
{
    public class ChannelManagerChannelSection : EditorTabSection
    {
        public override string SectionName => "Channel";
        public override bool IsDisabled => false;

        private ChannelManagerTab _channelManagerTab;

        public const string ChannelIDEditorPrefsKey = "MVCTool_ChannelID";
        public string ChannelID { get; private set; } = null;

        private List<(int id, string name)> _channelContent = new();
        private int _selectedContentIndex = 0;

        private protected override void Load()
        {
            _channelManagerTab = _parentTab as ChannelManagerTab;

            ChannelID = EditorPrefs.GetString(ChannelIDEditorPrefsKey, null);
        }

        public override void OnEnter()
        {
            FetchChannelContentList();
        }

        public override void OnExit()
        {

        }

        private protected override void OnDraw()
        {
            ChannelID = EditorGUILayout.TextField("Channel ID", ChannelID);

            DrawChannelContentDropdown();
            if (GUILayout.Button("Refresh Channel Content List", GUILayout.Height(30)))
            {
                FetchChannelContentList();
            }
            if (GUILayout.Button("Delete Selected Content", GUILayout.Height(30)))
            {
                DeleteSelectedContent();
            }
        }

        public override void Reset()
        {
            EditorPrefs.DeleteKey(ChannelIDEditorPrefsKey);
        }

        private void FetchChannelContentList()
        {
            ContentManager.ListContentFromChannel(ChannelID).ContinueWith(contentList =>
            {
                _channelContent = contentList;
                _selectedContentIndex = 0;
            }).Forget();
        }

        private void DrawChannelContentDropdown()
        {
            if( _channelContent.Count == 0 )
            {
                EditorGUILayout.LabelField("No content available in this channel.");
                return;
            }

            string[] contentNames = _channelContent.ConvertAll(content => content.name).ToArray();
            _selectedContentIndex = EditorGUILayout.Popup("Channel Content", _selectedContentIndex, contentNames);
        }

        private void DeleteSelectedContent()
        {
            ContentManager.DeleteContent(_channelContent[_selectedContentIndex].id.ToString()).ContinueWith(() => { 
                FetchChannelContentList(); 
            }).Forget();
            
        }
    }
}