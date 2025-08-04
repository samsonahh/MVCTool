using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace MVCTool
{
    public class AssetManagerBuildSection : EditorTabSection
    {
        public override string SectionName => "Build";
        public override bool IsDisabled => !LoginApi.IsLoggedIn;

        private HashSet<BuildTarget> _availableBuildTargets = new HashSet<BuildTarget>();
        private Dictionary<BuildTarget, bool> _buildTargetOptions = new Dictionary<BuildTarget, bool>();
        private HashSet<BuildTarget> _selectedBuildTargets = new HashSet<BuildTarget>();

        private ReorderableList _prefabsToBuildReorderableList;
        public List<GameObject> PrefabsToBuild { get; private set; } = new List<GameObject>();

        private ReorderableList _builtPrefabsReorderableList;
        public List<GameObject> BuiltPrefabs { get; private set; } = new List<GameObject>();

        private protected override void Load()
        {
            PrefabsToBuild = new List<GameObject>();
            BuiltPrefabs = AssetManager.GetBuiltPrefabs();

            _prefabsToBuildReorderableList = CreatePrefabsToBuildReorderableList();
            _builtPrefabsReorderableList = CreateBuiltPrefabsReorderableList();
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
            _prefabsToBuildReorderableList.DoLayoutList();

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

                    AssetManager.ClearBuiltPrefabs();
                    BuiltPrefabs.Clear();

                    ForceDraw();
                }
            }

            bool hasPrefabsToBuild = PrefabsToBuild.Any(prefab => prefab != null);
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

        public override void Reset()
        {

        }

        private ReorderableList CreatePrefabsToBuildReorderableList()
        {
            ReorderableList reorderableList = new ReorderableList(PrefabsToBuild, typeof(GameObject), true, true, true, true);

            reorderableList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "Prefabs to Build");
            };

            reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                PrefabsToBuild[index] = EditorGUI.ObjectField(rect, PrefabsToBuild[index], typeof(GameObject), false) as GameObject;
            };

            reorderableList.onAddCallback = (list) =>
            {
                PrefabsToBuild.Add(null);
            };

            reorderableList.onRemoveCallback = (list) => {
                PrefabsToBuild.RemoveAt(list.index);
            };

            return reorderableList;
        }

        private ReorderableList CreateBuiltPrefabsReorderableList()
        {
            ReorderableList reorderableList = new ReorderableList(BuiltPrefabs, typeof(GameObject), false, true, false, true);

            reorderableList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "Built Prefabs");
            };

            reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                GUI.enabled = false;
                BuiltPrefabs[index] = EditorGUI.ObjectField(rect, BuiltPrefabs[index], typeof(GameObject), false) as GameObject;
                GUI.enabled = true;
            };

            reorderableList.onRemoveCallback = (list) => {
                AssetManager.RemoveBuiltPrefab(BuiltPrefabs[list.index]);
                BuiltPrefabs.RemoveAt(list.index);
            };

            return reorderableList;
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
            RemoveListNullAndDuplicates(PrefabsToBuild);

            if (PrefabsToBuild.Count == 0)
            {
                ForceDraw();
                return;
            }

            AssetManager.BuildPrefabs(PrefabsToBuild, GetSelectedBuildTargets());
            BuiltPrefabs = AssetManager.GetBuiltPrefabs();
            _builtPrefabsReorderableList.list = BuiltPrefabs;
        }
    }
}