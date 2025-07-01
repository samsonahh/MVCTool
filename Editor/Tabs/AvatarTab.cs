using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MVCTool
{
    public class AvatarTab : EditorTab
    {
        public override string TabName => "Avatar";

        #region Editor
        private GameObject _selectedObject;
        #endregion

        #region Build/Upload
        private bool _isLoggedIn => LoginApi.HasBearerToken;

        private HashSet<BuildTarget> _availableBuildTargets = new HashSet<BuildTarget>();
        private Dictionary<BuildTarget, bool> _buildTargetOptions = new Dictionary<BuildTarget, bool>();
        private HashSet<BuildTarget> _selectedBuildTargets = new HashSet<BuildTarget>();

        private GameObject _avatarPrefabToBuild;

        private bool _isUploading = false;

        private string _errorMessage = null;
        private string _uploadStatusMessage = null;
        #endregion

        public override void Draw()
        {
            DrawEditorSection();

            EditorGUI.BeginDisabledGroup(!_isLoggedIn || _isUploading);

            MVCToolWindow.DrawSeparator();
            DrawBuildSection();
            MVCToolWindow.DrawSeparator();
            DrawUploadSection();

            EditorGUI.EndDisabledGroup();

            if (!_isLoggedIn)
                EditorGUILayout.HelpBox("You must be logged in to build or upload asset bundles.", MessageType.Warning);
        }

        public override void OnEnter()
        {
            _selectedObject = Selection.activeGameObject;
            _errorMessage = null;
            _uploadStatusMessage = null;

            _availableBuildTargets = AssetBundler.GetAvailableBuildTargets();
            _buildTargetOptions = new Dictionary<BuildTarget, bool>();
            foreach (var target in _availableBuildTargets)
            {
                _buildTargetOptions[target] = EditorPrefs.GetBool(AssetBundler.SupportedBuildTargets[target].EditorPrefsKey, false);
            }
            _selectedBuildTargets = GetSelectedBuildTargets();

            Selection.selectionChanged += Selection_SelectionChanged;
        }

        public override void OnExit()
        {
            Selection.selectionChanged -= Selection_SelectionChanged;
        }

        private protected override void Load()
        {
            _availableBuildTargets = AssetBundler.GetAvailableBuildTargets();
            _buildTargetOptions = new Dictionary<BuildTarget, bool>();
            foreach (var target in _availableBuildTargets)
            {
                _buildTargetOptions[target] = EditorPrefs.GetBool(AssetBundler.SupportedBuildTargets[target].EditorPrefsKey, false);
            }
        }

        public override void Reset()
        {

        }

        private void DrawEditorSection()
        {
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter
            };
            GUILayout.Label($"Editor", titleStyle);
            MVCToolWindow.DrawSeparator();

            MVCAvatar selectedAvatar = _selectedObject != null ? _selectedObject.GetComponent<MVCAvatar>() : null;
            bool isAvatarInHierarchy = !EditorUtility.IsPersistent(_selectedObject) && selectedAvatar != null;

            GUI.enabled = false;
            EditorGUILayout.ObjectField("Selected Avatar", selectedAvatar, typeof(MVCAvatar), true);
            GUI.enabled = true;

            EditorGUILayout.Space();

            EditorGUI.BeginDisabledGroup(!isAvatarInHierarchy);
            if (GUILayout.Button("Create Avatar Rig", GUILayout.Height(30)))
            {
                selectedAvatar.CreateRig();
            }
            if (GUILayout.Button("Setup Avatar Hand References", GUILayout.Height(30)))
            {
                selectedAvatar.CreateHandReferences();
            }
            EditorGUI.EndDisabledGroup();

            if (!isAvatarInHierarchy)
                EditorGUILayout.HelpBox("Please select an MVCAvatar in the scene hierarchy", MessageType.Warning);
        }

        private void DrawBuildSection()
        {
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter
            };
            GUILayout.Label($"Build", titleStyle);
            MVCToolWindow.DrawSeparator();

            _avatarPrefabToBuild = EditorGUILayout.ObjectField("Avatar Prefab to Build", _avatarPrefabToBuild, typeof(GameObject), false) as GameObject;

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Select Build Targets:", EditorStyles.boldLabel);
            foreach (var target in _availableBuildTargets)
            {
                bool newValue = EditorGUILayout.ToggleLeft(AssetBundler.SupportedBuildTargets[target].DisplayName, _buildTargetOptions[target]);
                if (newValue != _buildTargetOptions[target])
                {
                    _buildTargetOptions[target] = newValue;
                    EditorPrefs.SetBool(AssetBundler.SupportedBuildTargets[target].EditorPrefsKey, _buildTargetOptions[target]);
                    _selectedBuildTargets = GetSelectedBuildTargets();

                    AssetBundler.ClearBuiltAvatarPrefabData();

                    ForceDraw();
                }
            }

            bool hasPrefabToBuild = _avatarPrefabToBuild != null;
            MVCAvatar mvcAvatarToBuild = hasPrefabToBuild ? _avatarPrefabToBuild.GetComponent<MVCAvatar>() : null;
            bool isAvatarPrefab = hasPrefabToBuild && mvcAvatarToBuild != null;
            bool canUploadAvatar = isAvatarPrefab && mvcAvatarToBuild.IsReadyForUpload();
            bool hasBuildTargets = _selectedBuildTargets.Count > 0;
            bool canBuild = canUploadAvatar && hasBuildTargets;
            EditorGUI.BeginDisabledGroup(!canBuild);
            if (GUILayout.Button("Build Avatar Prefab", GUILayout.Height(30)))
            {
                BuildAvatarPrefab();
            }
            EditorGUI.EndDisabledGroup();

            if (!hasPrefabToBuild)
                EditorGUILayout.HelpBox("Please select an MVCAvatar prefab to build.", MessageType.Warning);
            else if(!isAvatarPrefab)
            {
                EditorGUILayout.HelpBox("The selected prefab does not contain an MVCAvatar component.", MessageType.Warning);
                AssetBundler.ClearBuiltAvatarPrefabData();
            }
            else if (!canUploadAvatar)
            {
                EditorGUILayout.HelpBox("The selected MVCAvatar is not ready for upload. Please ensure it has the necessary components and references set up.", MessageType.Warning);
                AssetBundler.ClearBuiltAvatarPrefabData();
            }

            if (!hasBuildTargets)
                EditorGUILayout.HelpBox("Please select at least one build target.", MessageType.Warning);

            EditorGUILayout.Space();

            GUI.enabled = false;
            EditorGUILayout.ObjectField("Built Avatar Prefab", AssetBundler.BuiltAvatarPrefabData?.Prefab , typeof(GameObject), false);
            GUI.enabled = true;
        }

        private void DrawUploadSection()
        {
            bool canUpload = AssetBundler.BuiltAvatarPrefabData != null && !_isUploading;
            EditorGUI.BeginDisabledGroup(!canUpload);

            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter
            };
            GUILayout.Label($"Upload", titleStyle);
            MVCToolWindow.DrawSeparator();

            if (GUILayout.Button("Upload Avatar", GUILayout.Height(30)))
            {
                UploadAvatar().Forget();
            }

            EditorGUI.EndDisabledGroup();

            bool isBuildWarningVisible = AssetBundler.BuiltAvatarPrefabData?.Prefab == null;
            if (_isLoggedIn && isBuildWarningVisible)
            {
                EditorGUILayout.HelpBox("Please build your avatar prefab to upload.", MessageType.Warning);
                _uploadStatusMessage = null;
            }

            if (!string.IsNullOrEmpty(_errorMessage))
                EditorGUILayout.HelpBox(_errorMessage, MessageType.Error);

            if (!string.IsNullOrEmpty(_uploadStatusMessage))
                EditorGUILayout.HelpBox(_uploadStatusMessage, MessageType.Info);
        }

        private void Selection_SelectionChanged()
        {
            _selectedObject = Selection.activeGameObject;
            ForceDraw();
        }

        private void BuildAvatarPrefab()
        {
            AssetBundler.BuildAvatarPrefab(_avatarPrefabToBuild, GetSelectedBuildTargets());
        }

        private async UniTask UploadAvatar()
        {
            _errorMessage = null;
            _uploadStatusMessage = null;

            _isUploading = true;

            try
            {
                await AssetBundler.UploadBuiltAvatarPrefab();
                _uploadStatusMessage = $"Avatar uploaded successfully!";
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
    }
}