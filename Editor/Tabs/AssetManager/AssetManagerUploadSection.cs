using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace MVCTool
{
    public class AssetManagerUploadSection : EditorTabSection
    {
        public override string SectionName => "Upload";
        public override bool IsDisabled => !LoginApi.IsLoggedIn || _isUploadingAsset;

        private AssetManagerBuildSection _buildSection;

        private bool _isUploadingAsset = false;

        private string _uploadAssetStatusMessage = null;
        private string _assetErrorMessage = null;

        private protected override void Load()
        {
            _buildSection = MVCToolWindow.Instance.AssetManagerTab.BuildSection;
        }

        public override void OnEnter()
        {
            _assetErrorMessage = null;
            _uploadAssetStatusMessage = null;
        }

        public override void OnExit()
        {

        }

        private protected override void OnDraw()
        {
            bool canUpload = AssetManager.BuiltPrefabManifests.Count > 0 && !_isUploadingAsset;
            EditorGUI.BeginDisabledGroup(!canUpload);

            if (GUILayout.Button($"Upload to Channel", GUILayout.Height(30)))
            {
                UploadUnityAssetsToChannel(ChannelManagerSelectSection.ChannelID).Forget();
            }

            EditorGUI.EndDisabledGroup();

            bool isBuildWarningVisible = _buildSection.PrefabsToBuild.Count >= 0 && _buildSection.BuiltPrefabs.Count == 0;
            if (LoginApi.IsLoggedIn && isBuildWarningVisible)
            {
                EditorGUILayout.HelpBox("Please build your prefabs to upload.", MessageType.Warning);
                _uploadAssetStatusMessage = null;
            }

            if (!string.IsNullOrEmpty(_assetErrorMessage))
                EditorGUILayout.HelpBox(_assetErrorMessage, MessageType.Error);

            if (!string.IsNullOrEmpty(_uploadAssetStatusMessage))
                EditorGUILayout.HelpBox(_uploadAssetStatusMessage, MessageType.Info);
        }

        public override void Reset()
        {
            _assetErrorMessage = null;
            _uploadAssetStatusMessage = null;
        }

        private async UniTask UploadUnityAssetsToChannel(string channelID)
        {
            _assetErrorMessage = null;
            _uploadAssetStatusMessage = null;

            _isUploadingAsset = true;

            try
            {
                await AssetManager.UploadBuiltPrefabsToChannel(channelID);
                _uploadAssetStatusMessage = $"Asset bundles uploaded to channel: {channelID} successfully!";
            }
            catch (System.Exception e)
            {
                _assetErrorMessage = $"Upload failed: {e.Message}";
            }
            finally
            {
                _isUploadingAsset = false;
                ForceDraw(); // Force redraw to immediately show the status message
            }
        }
    }
}