using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace MVCTool
{
    public class ChannelUploadTab : EditorTab
    {
        public override string TabName => "Channel Upload";

        private bool _isLoggedIn => LoginApi.HasBearerToken;

        private HashSet<BuildTarget> _availableBuildTargets = new HashSet<BuildTarget>();
        private Dictionary<BuildTarget, bool> _buildTargetOptions = new Dictionary<BuildTarget, bool>();
        private HashSet<BuildTarget> _selectedBuildTargets = new HashSet<BuildTarget>();

        private ReorderableList _prefabsToBuildReorderableList;
        private List<GameObject> _prefabsToBuild = new List<GameObject>();

        private ReorderableList _builtPrefabsReorderableList;
        private List<GameObject> _builtPrefabs = new List<GameObject>();

        private const string ChannelIDEditorPrefsKey = "MVCTool_ChannelID";
        private string _channelID = null;
        private bool _isUploading = false;

        private string _errorMessage = null;
        private string _uploadStatusMessage = null;

        public override void Draw()
        {
            EditorGUI.BeginDisabledGroup(!_isLoggedIn || _isUploading);

            DrawNonUnityContentSection();
            MVCTheme.DrawSeparator();
            DrawUnityAssetsSection();

            EditorGUI.EndDisabledGroup();

            if (!_isLoggedIn)
                EditorGUILayout.HelpBox("You must be logged in to build or upload asset bundles.", MessageType.Warning);
        }

        public override void OnEnter()
        {
            _errorMessage = null;
            _uploadStatusMessage = null;

            _availableBuildTargets = ContentUploader.GetAvailableBuildTargets();
            _buildTargetOptions = new Dictionary<BuildTarget, bool>();
            foreach (var target in _availableBuildTargets)
            {
                _buildTargetOptions[target] = EditorPrefs.GetBool(ContentUploader.SupportedBuildTargets[target].EditorPrefsKey, false);
            }
            _selectedBuildTargets = GetSelectedBuildTargets();
        }

        public override void OnExit()
        {
            
        }

        private protected override void Load()
        {
            _prefabsToBuild = new List<GameObject>();
            _builtPrefabs = ContentUploader.GetBuiltPrefabs();

            _prefabsToBuildReorderableList = CreatePrefabsToBuildReorderableList();
            _builtPrefabsReorderableList = CreateBuiltPrefabsReorderableList();

            _channelID = EditorPrefs.GetString(ChannelIDEditorPrefsKey, null);
        }

        public override void Reset()
        {
            _errorMessage = null;
            _uploadStatusMessage = null;

            EditorPrefs.DeleteKey(ChannelIDEditorPrefsKey); 
        }

        private void DrawNonUnityContentSection()
        {
            GUILayout.Label($"Non-Unity Content", MVCTheme.HeadingStyle);
            MVCTheme.DrawSeparator();
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
                ContentUploader.RemoveBuiltPrefab(_builtPrefabs[list.index]);
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
                bool newValue = EditorGUILayout.ToggleLeft(ContentUploader.SupportedBuildTargets[target].DisplayName, _buildTargetOptions[target]);
                if (newValue != _buildTargetOptions[target])
                {
                    _buildTargetOptions[target] = newValue;
                    EditorPrefs.SetBool(ContentUploader.SupportedBuildTargets[target].EditorPrefsKey, _buildTargetOptions[target]);
                    _selectedBuildTargets = GetSelectedBuildTargets();

                    ContentUploader.ClearBuiltPrefabs();
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
            bool canUpload = ContentUploader.BuiltPrefabManifests.Count > 0 && !_isUploading;
            EditorGUI.BeginDisabledGroup(!canUpload);

            EditorGUILayout.LabelField("Enter Channel ID:", EditorStyles.boldLabel);
            _channelID = EditorGUILayout.TextField("Channel ID", _channelID);

            EditorGUILayout.Space(2.5f);

            if (GUILayout.Button("Upload to Channel", GUILayout.Height(30)))
            {
                UploadToChannel(_channelID).Forget();
                EditorPrefs.SetString(ChannelIDEditorPrefsKey, _channelID);
            }

            EditorGUI.EndDisabledGroup();

            bool isBuildWarningVisible = _prefabsToBuild.Count >= 0 && _builtPrefabs.Count == 0;
            if (_isLoggedIn && isBuildWarningVisible)
            {
                EditorGUILayout.HelpBox("Please build your prefabs to upload.", MessageType.Warning);
                _uploadStatusMessage = null;
            }

            if (!string.IsNullOrEmpty(_errorMessage))
                EditorGUILayout.HelpBox(_errorMessage, MessageType.Error);

            if (!string.IsNullOrEmpty(_uploadStatusMessage))
                EditorGUILayout.HelpBox(_uploadStatusMessage, MessageType.Info);
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
            _errorMessage = null;

            RemoveListNullAndDuplicates(_prefabsToBuild);

            if (_prefabsToBuild.Count == 0)
            {
                ForceDraw();
                return;
            }

            ContentUploader.BuildPrefabs(_prefabsToBuild, GetSelectedBuildTargets());
            _builtPrefabs = ContentUploader.GetBuiltPrefabs();
            _builtPrefabsReorderableList.list = _builtPrefabs;
        }

        private async UniTask UploadToChannel(string channelID)
        {
            _errorMessage = null;
            _uploadStatusMessage = null;

            _isUploading = true;

            try
            {
                await ContentUploader.UploadBuiltPrefabsToChannel(channelID);
                _uploadStatusMessage = $"Asset bundles uploaded to {channelID} successfully!";
            }
            catch (System.Exception e)
            {
                _errorMessage = $"Upload failed: {e.Message}";
            }
            finally
            {
                _isUploading = false;
                ForceDraw(); // Force redraw to immediately show the status message
            }
        }
    }
}