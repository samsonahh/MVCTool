using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace MVCTool
{
    public class ChannelManagerTab : EditorTab
    {
        public override string TabName => "Channel Manager";

        private const string ChannelIDEditorPrefsKey = "MVCTool_ChannelID";
        private string _channelID = null;

        #region Non Unity Content
        private string _currentSelectedPath = "";

        private bool _isUploadingContent = false;

        private string _uploadContentStatusMessage = null;
        private string _contentErrorMessage = null;
        #endregion

        #region Unity Assets
        private HashSet<BuildTarget> _availableBuildTargets = new HashSet<BuildTarget>();
        private Dictionary<BuildTarget, bool> _buildTargetOptions = new Dictionary<BuildTarget, bool>();
        private HashSet<BuildTarget> _selectedBuildTargets = new HashSet<BuildTarget>();

        private ReorderableList _prefabsToBuildReorderableList;
        private List<GameObject> _prefabsToBuild = new List<GameObject>();

        private ReorderableList _builtPrefabsReorderableList;
        private List<GameObject> _builtPrefabs = new List<GameObject>();
        
        private bool _isUploadingAsset = false;

        private string _uploadAssetStatusMessage = null;
        private string _assetErrorMessage = null;
        #endregion

        private protected override void OnDraw()
        {
            EditorGUI.BeginDisabledGroup(!LoginApi.IsLoggedIn || _isUploadingAsset);

            _channelID = EditorGUILayout.TextField("Channel ID", _channelID);
            EditorGUILayout.Space(2.5f);
            MVCTheme.DrawSeparator();

            DrawNonUnityContentSection();
            MVCTheme.DrawSeparator();
            DrawUnityAssetsSection();

            
        }

        private protected override void OnDrawAfterSections()
        {
            if (!LoginApi.IsLoggedIn)
                EditorGUILayout.HelpBox("You must be logged in to build or upload asset bundles.", MessageType.Warning);
        }

        private protected override void OnEnter()
        {
            _assetErrorMessage = null;
            _uploadAssetStatusMessage = null;

            _availableBuildTargets = ContentManager.GetAvailableBuildTargets();
            _buildTargetOptions = new Dictionary<BuildTarget, bool>();
            foreach (var target in _availableBuildTargets)
            {
                _buildTargetOptions[target] = EditorPrefs.GetBool(ContentManager.SupportedBuildTargets[target].EditorPrefsKey, false);
            }
            _selectedBuildTargets = GetSelectedBuildTargets();
        }

        private protected override void OnExit()
        {
            
        }

        private protected override void Load()
        {
            _prefabsToBuild = new List<GameObject>();
            _builtPrefabs = ContentManager.GetBuiltPrefabs();

            _prefabsToBuildReorderableList = CreatePrefabsToBuildReorderableList();
            _builtPrefabsReorderableList = CreateBuiltPrefabsReorderableList();

            _channelID = EditorPrefs.GetString(ChannelIDEditorPrefsKey, null);
        }

        private protected override void OnReset()
        {
            _assetErrorMessage = null;
            _uploadAssetStatusMessage = null;

            EditorPrefs.DeleteKey(ChannelIDEditorPrefsKey); 
        }

        private void DrawNonUnityContentSection()
        {
            GUILayout.Label($"Non-Unity Content", MVCTheme.HeadingStyle);
            MVCTheme.DrawSeparator();

            string displayPath = string.IsNullOrEmpty(_currentSelectedPath) ? "None" : _currentSelectedPath;
            GUILayout.Label($"<b>Selected File:</b> {displayPath}", MVCTheme.RichTextLabelStyle);
            if (GUILayout.Button("Select File", GUILayout.Height(30)))
            {
                _currentSelectedPath = EditorUtility.OpenFilePanel("Select File", "", "");
            }

            bool isFileSelected = !string.IsNullOrEmpty(_currentSelectedPath);
            EditorGUI.BeginDisabledGroup(!isFileSelected || _isUploadingContent);
            if (GUILayout.Button("Upload File", GUILayout.Height(30)))
            {
                UploadNonUnityContentToChannel(_channelID, _currentSelectedPath).Forget();
            }
            EditorGUI.EndDisabledGroup();

            if(!isFileSelected)
                EditorGUILayout.HelpBox("Please select a file to upload.", MessageType.Warning);

            if (!string.IsNullOrEmpty(_contentErrorMessage))
                EditorGUILayout.HelpBox(_contentErrorMessage, MessageType.Error);

            if (!string.IsNullOrEmpty(_uploadContentStatusMessage))
                EditorGUILayout.HelpBox(_uploadContentStatusMessage, MessageType.Info);
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

        private ReorderableList CreatePrefabsToBuildReorderableList()
        {
            ReorderableList reorderableList = new ReorderableList(_prefabsToBuild, typeof(GameObject), true, true, true, true);
            
            reorderableList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "Prefabs to Build");
            };
            
            reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                _prefabsToBuild[index] = EditorGUI.ObjectField(rect, _prefabsToBuild[index], typeof(GameObject), false) as GameObject;
            };

            reorderableList.onAddCallback = (list) =>
            {
                _prefabsToBuild.Add(null);
            };

            reorderableList.onRemoveCallback = (list) => {
                _prefabsToBuild.RemoveAt(list.index);
            };

            return reorderableList;
        }

        private ReorderableList CreateBuiltPrefabsReorderableList()
        {
            ReorderableList reorderableList = new ReorderableList(_builtPrefabs, typeof(GameObject), false, true, false, true);

            reorderableList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "Built Prefabs");
            };

            reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                GUI.enabled = false;
                _builtPrefabs[index] = EditorGUI.ObjectField(rect, _builtPrefabs[index], typeof(GameObject), false) as GameObject;
                GUI.enabled = true;
            };

            reorderableList.onRemoveCallback = (list) => {
                ContentManager.RemoveBuiltPrefab(_builtPrefabs[list.index]);
                _builtPrefabs.RemoveAt(list.index);
            };

            return reorderableList;
        }

        private void DrawUnityAssetsSection()
        {
            GUILayout.Label($"Unity Assets", MVCTheme.HeadingStyle);
            MVCTheme.DrawSeparator();
            DrawBuildAssetsSection();
            DrawUploadAssetsToChannelSection();
        }

        private void DrawBuildAssetsSection()
        {
            _prefabsToBuildReorderableList.DoLayoutList();

            EditorGUILayout.Space();
           
            EditorGUILayout.LabelField("Select Build Targets:", EditorStyles.boldLabel);
            foreach (var target in _availableBuildTargets)
            {
                bool newValue = EditorGUILayout.ToggleLeft(ContentManager.SupportedBuildTargets[target].DisplayName, _buildTargetOptions[target]);
                if (newValue != _buildTargetOptions[target])
                {
                    _buildTargetOptions[target] = newValue;
                    EditorPrefs.SetBool(ContentManager.SupportedBuildTargets[target].EditorPrefsKey, _buildTargetOptions[target]);
                    _selectedBuildTargets = GetSelectedBuildTargets();

                    ContentManager.ClearBuiltPrefabs();
                    _builtPrefabs.Clear();

                    ForceDraw();
                }
            }

            bool hasPrefabsToBuild = _prefabsToBuild.Any(prefab => prefab != null);
            bool hasBuildTargets = _selectedBuildTargets.Count > 0;
            bool canBuild = hasPrefabsToBuild && hasBuildTargets;
            EditorGUI.BeginDisabledGroup(!canBuild);

            if (GUILayout.Button("Build Prefabs", GUILayout.Height(30)))
            {
                BuildPrefabs();
            }

            EditorGUI.EndDisabledGroup();

            if (!hasPrefabsToBuild)
                EditorGUILayout.HelpBox("Please add prefabs to the list before building.", MessageType.Warning);

            if (!hasBuildTargets)
                EditorGUILayout.HelpBox("Please select at least one build target.", MessageType.Warning);

            EditorGUILayout.Space();

            _builtPrefabsReorderableList.DoLayoutList();
        }

        private void DrawUploadAssetsToChannelSection()
        {
            bool canUpload = ContentManager.BuiltPrefabManifests.Count > 0 && !_isUploadingAsset;
            EditorGUI.BeginDisabledGroup(!canUpload);

            if (GUILayout.Button("Upload to Channel", GUILayout.Height(30)))
            {
                UploadUnityAssetsToChannel(_channelID).Forget();
                EditorPrefs.SetString(ChannelIDEditorPrefsKey, _channelID);
            }

            EditorGUI.EndDisabledGroup();

            bool isBuildWarningVisible = _prefabsToBuild.Count >= 0 && _builtPrefabs.Count == 0;
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

        private void RemoveListNullAndDuplicates<T>(List<T> list)
        {
            list.RemoveAll(item => item == null); // Remove nulls

            HashSet<T> seen = new HashSet<T>();
            list.RemoveAll(item => !seen.Add(item)); // Remove duplicates
        }

        private HashSet<BuildTarget> GetSelectedBuildTargets()
        {
            HashSet<BuildTarget> selectedTargets = new HashSet<BuildTarget>();
            foreach (var kvp in _buildTargetOptions)
            {
                if (kvp.Value) // If the toggle is checked
                {
                    selectedTargets.Add(kvp.Key);
                }
            }
            return selectedTargets;
        }

        private void BuildPrefabs()
        {
            _assetErrorMessage = null;

            RemoveListNullAndDuplicates(_prefabsToBuild);

            if (_prefabsToBuild.Count == 0)
            {
                ForceDraw();
                return;
            }

            ContentManager.BuildPrefabs(_prefabsToBuild, GetSelectedBuildTargets());
            _builtPrefabs = ContentManager.GetBuiltPrefabs();
            _builtPrefabsReorderableList.list = _builtPrefabs;
        }

        private async UniTask UploadUnityAssetsToChannel(string channelID)
        {
            _assetErrorMessage = null;
            _uploadAssetStatusMessage = null;

            _isUploadingAsset = true;

            try
            {
                await ContentManager.UploadBuiltPrefabsToChannel(channelID);
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