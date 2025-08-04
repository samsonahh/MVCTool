using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MVCTool
{
    public class ContentManagerDeleteSection : EditorTabSection
    {
        public override string SectionName => "Delete";
        public override bool IsDisabled => !LoginApi.IsLoggedIn || _isDeleting;

        private List<(int id, string name)> _channelContent = new();
        private int _selectedContentIndex = 0;

        private bool _isDeleting = false;

        private string _statusMessage = null;
        private string _errorMessage = null;

        private protected override void Load()
        {
           
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
            DrawChannelContentDropdown();
            if (GUILayout.Button("Refresh Channel Content List", GUILayout.Height(30)))
            {
                FetchChannelContentList();
            }
            if (GUILayout.Button("Delete Selected Content", GUILayout.Height(30)))
            {
                DeleteSelectedContent().Forget(); ;
            }

            if (string.IsNullOrEmpty(_statusMessage) == false)
                EditorGUILayout.HelpBox(_statusMessage, MessageType.Info);

            if(string.IsNullOrEmpty(_errorMessage) == false)
                EditorGUILayout.HelpBox(_errorMessage, MessageType.Error);
        }

        public override void Reset()
        {
            
        }

        private void FetchChannelContentList()
        {
            if (!LoginApi.IsLoggedIn)
            {
                _channelContent = new();
                _selectedContentIndex = 0;
                return;
            }

            ContentManager.ListContentFromChannel(ChannelManagerSelectSection.ChannelID).ContinueWith(contentList =>
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

        private async UniTask DeleteSelectedContent()
        {
            _isDeleting = true;
            _errorMessage = null;
            _statusMessage = null;

            try
            {
                await ContentManager.DeleteContent(_channelContent[_selectedContentIndex].id.ToString());
                _statusMessage = $"Content '{_channelContent[_selectedContentIndex].name}' deleted successfully!";
            }
            catch(System.Exception ex)
            {
                _errorMessage = $"Error deleting content: {ex.Message}";
            }
            finally
            {
                _isDeleting = false;
                RefreshTab();
            }
        }
    }
}