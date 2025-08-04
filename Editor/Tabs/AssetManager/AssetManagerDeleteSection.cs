using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MVCTool
{
    public class AssetManagerDeleteSection : EditorTabSection
    {
        public override string SectionName => "Delete";
        public override bool IsDisabled => !LoginApi.IsLoggedIn;
        private List<(int id, string name)> _channelAssets = new();
        private int _selectedAssetIndex = 0;

        private bool _isDeleting = false;

        private string _statusMessage = null;
        private string _errorMessage = null;

        private protected override void Load()
        {
            
        }

        public override void OnEnter()
        {
            FetchChannelAssetList();
        }

        public override void OnExit()
        {

        }

        private protected override void OnDraw()
        {
            DrawChannelContentDropdown();
            if (GUILayout.Button("Refresh Channel Asset List", GUILayout.Height(30)))
            {
                FetchChannelAssetList();
            }
            if (GUILayout.Button("Delete Selected Asset", GUILayout.Height(30)))
            {
                DeleteSelectedAsset().Forget(); ;
            }

            if (string.IsNullOrEmpty(_statusMessage) == false)
                EditorGUILayout.HelpBox(_statusMessage, MessageType.Info);

            if (string.IsNullOrEmpty(_errorMessage) == false)
                EditorGUILayout.HelpBox(_errorMessage, MessageType.Error);
        }

        public override void Reset()
        {
            
        }
        private void FetchChannelAssetList()
        {
            if (!LoginApi.IsLoggedIn)
            {
                _channelAssets = new();
                _selectedAssetIndex = 0;
                return;
            }

            AssetManager.ListAssetsFromChannel(ChannelManagerSelectSection.ChannelID).ContinueWith(assetList =>
            {
                _channelAssets = assetList;
                _selectedAssetIndex = 0;
            }).Forget();
        }

        private void DrawChannelContentDropdown()
        {
            if (_channelAssets.Count == 0)
            {
                EditorGUILayout.LabelField("No assets available in this channel.");
                return;
            }

            string[] contentNames = _channelAssets.ConvertAll(content => content.name).ToArray();
            _selectedAssetIndex = EditorGUILayout.Popup("Channel Assets", _selectedAssetIndex, contentNames);
        }

        private async UniTask DeleteSelectedAsset()
        {
            _isDeleting = true;
            _errorMessage = null;
            _statusMessage = null;

            try
            {
                await AssetManager.DeleteAsset(_channelAssets[_selectedAssetIndex].id.ToString());
                _statusMessage = $"Asset '{_channelAssets[_selectedAssetIndex].name}' deleted successfully!";
            }
            catch (System.Exception ex)
            {
                _errorMessage = $"Error deleting asset: {ex.Message}";
            }
            finally
            {
                _isDeleting = false;
                RefreshTab();
            }
        }
    }
}