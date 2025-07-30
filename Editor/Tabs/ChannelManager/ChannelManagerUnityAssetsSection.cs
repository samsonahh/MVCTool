using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace MVCTool
{
    public class ChannelManagerUnityAssetsSection : EditorTabSection
    {
        public override string SectionName => "Unity Assets";
        public override bool IsDisabled => false;

        private ChannelManagerTab _channelManagerTab;

        private HashSet<BuildTarget> _availableBuildTargets = new HashSet<BuildTarget>();
        private Dictionary<BuildTarget, bool> _buildTargetOptions = new Dictionary<BuildTarget, bool>();
        private HashSet<BuildTarget> _selectedBuildTargets = new HashSet<BuildTarget>();

        private ReorderableList _prefabsToBuildReorderableList;
        private List<GameObject> _prefabsToBuild = new List<GameObject>();

        private ReorderableList _builtPrefabsReorderableList;
        private List<GameObject> _builtPrefabs = new List<GameObject>();

        public bool IsUploadingAsset { get; private set; } = false;

        private string _uploadAssetStatusMessage = null;
        private string _assetErrorMessage = null;

        private protected override void Load()
        {
            _channelManagerTab = _parentTab as ChannelManagerTab;

            _prefabsToBuild = new List<GameObject>();
            _builtPrefabs = ContentManager.GetBuiltPrefabs();

            _prefabsToBuildReorderableList = CreatePrefabsToBuildReorderableList();
            _builtPrefabsReorderableList = CreateBuiltPrefabsReorderableList();
        }

        public override void OnEnter()
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

        public override void OnExit()
        {

        }

        private protected override void OnDraw()
        {
            DrawBuildAssetsSection();
            DrawUploadAssetsToChannelSection();
        }

        public override void Reset()
        {
            _assetErrorMessage = null;
            _uploadAssetStatusMessage = null;
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
            bool canUpload = ContentManager.BuiltPrefabManifests.Count > 0 && !IsUploadingAsset;
            EditorGUI.BeginDisabledGroup(!canUpload);

            if (GUILayout.Button("Upload to Channel", GUILayout.Height(30)))
            {
                UploadUnityAssetsToChannel(_channelManagerTab.ChannelID).Forget();
                EditorPrefs.SetString(ChannelManagerTab.ChannelIDEditorPrefsKey, _channelManagerTab.ChannelID);
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

            IsUploadingAsset = true;

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
                IsUploadingAsset = false;
                ForceDraw(); // Force redraw to immediately show the status message
            }
        }
    }
}