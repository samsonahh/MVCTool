using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MVCTool
{
    public class AvatarTabBuildSection : EditorTabSection
    {
        public override string SectionName => "Build";
        public override bool IsDisabled => !LoginApi.IsLoggedIn;

        private HashSet<BuildTarget> _availableBuildTargets = new HashSet<BuildTarget>();
        private Dictionary<BuildTarget, bool> _buildTargetOptions = new Dictionary<BuildTarget, bool>();
        private HashSet<BuildTarget> _selectedBuildTargets = new HashSet<BuildTarget>();

        private GameObject _avatarPrefabToBuild;

        private protected override void Load()
        {
            _availableBuildTargets = AssetManager.GetAvailableBuildTargets();
            _buildTargetOptions = new Dictionary<BuildTarget, bool>();
            foreach (var target in _availableBuildTargets)
            {
                _buildTargetOptions[target] = EditorPrefs.GetBool(AssetManager.SupportedBuildTargets[target].EditorPrefsKey, false);
            }
        }

        public override void OnEnter()
        {
            _availableBuildTargets = AssetManager.GetAvailableBuildTargets();
            _buildTargetOptions = new Dictionary<BuildTarget, bool>();
            foreach (var target in _availableBuildTargets)
            {
                _buildTargetOptions[target] = EditorPrefs.GetBool(AssetManager.SupportedBuildTargets[target].EditorPrefsKey, false);
            }
            _selectedBuildTargets = GetSelectedBuildTargets();
        }

        public override void OnExit()
        {

        }

        private protected override void OnDraw()
        {
            _avatarPrefabToBuild = EditorGUILayout.ObjectField("Avatar Prefab to Build", _avatarPrefabToBuild, typeof(GameObject), false) as GameObject;

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Select Build Targets:", EditorStyles.boldLabel);
            foreach (var target in _availableBuildTargets)
            {
                bool newValue = EditorGUILayout.ToggleLeft(AssetManager.SupportedBuildTargets[target].DisplayName, _buildTargetOptions[target]);
                if (newValue != _buildTargetOptions[target])
                {
                    _buildTargetOptions[target] = newValue;
                    EditorPrefs.SetBool(AssetManager.SupportedBuildTargets[target].EditorPrefsKey, _buildTargetOptions[target]);
                    _selectedBuildTargets = GetSelectedBuildTargets();

                    AssetManager.ClearBuiltAvatarPrefabData();

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
            else if (!isAvatarPrefab)
            {
                EditorGUILayout.HelpBox("The selected prefab does not contain an MVCAvatar component.", MessageType.Warning);
                AssetManager.ClearBuiltAvatarPrefabData();
            }
            else if (!canUploadAvatar)
            {
                EditorGUILayout.HelpBox("The selected MVCAvatar is not ready for upload. Please ensure it has the necessary components and references set up.", MessageType.Warning);
                AssetManager.ClearBuiltAvatarPrefabData();
            }

            if (!hasBuildTargets)
                EditorGUILayout.HelpBox("Please select at least one build target.", MessageType.Warning);

            EditorGUILayout.Space();

            GUI.enabled = false;
            EditorGUILayout.ObjectField("Built Avatar Prefab", AssetManager.BuiltAvatarPrefabData?.Prefab, typeof(GameObject), false);
            GUI.enabled = true;
        }

        public override void Reset()
        {

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

        private void BuildAvatarPrefab()
        {
            AssetManager.BuildAvatarPrefab(_avatarPrefabToBuild, GetSelectedBuildTargets());
        }
    }
}