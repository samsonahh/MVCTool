using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MVCTool
{
    public class AvatarTabSetupSection : EditorTabSection
    {
        public override string SectionName => "Setup";
        public override bool IsDisabled => false;

        private GameObject _selectedObject;

        private static readonly AnimatorController _mvcAnimatorController =
            AssetDatabase.LoadAssetAtPath<AnimatorController>
            ("Packages/mvctool/Assets/Animations/MVCAnimatorController.controller");

        private protected override void Load()
        {

        }

        public override void OnEnter()
        {
            _selectedObject = Selection.activeGameObject;
            Selection.selectionChanged += Selection_SelectionChanged;
        }

        public override void OnExit()
        {
            Selection.selectionChanged -= Selection_SelectionChanged;
        }

        private protected override void OnDraw()
        {
            GUILayout.Box(
                $"Please make sure you are double-clicked into the MVCAvatar prefab and are inside prefab view.\n" +
                $"Select the parent MVCAvatar GameObject inside the prefab view hierarchy.",
                MVCTheme.BoxStyle
            );

            MVCAvatar selectedAvatar = _selectedObject != null ? _selectedObject.GetComponent<MVCAvatar>() : null;
            bool isAvatarInHierarchy = !EditorUtility.IsPersistent(_selectedObject) && selectedAvatar != null;

            GUI.enabled = false;
            EditorGUILayout.ObjectField("Selected Avatar", selectedAvatar, typeof(MVCAvatar), true);
            GUI.enabled = true;

            EditorGUILayout.Space();

            EditorGUI.BeginDisabledGroup(!isAvatarInHierarchy);
            if (GUILayout.Button("Setup Avatar", GUILayout.Height(30)))
            {
                selectedAvatar.CreateRig();
                selectedAvatar.CreateHandReferences();
                selectedAvatar.AssignAnimatorController(_mvcAnimatorController);
                EditorUtility.SetDirty(selectedAvatar.gameObject); // Auto save prefab changes
            }
            EditorGUI.EndDisabledGroup();

            if (!isAvatarInHierarchy)
                EditorGUILayout.HelpBox("Please select an MVCAvatar in the scene hierarchy", MessageType.Warning);
        }

        public override void Reset()
        {
            
        }

        private void Selection_SelectionChanged()
        {
            _selectedObject = Selection.activeGameObject;
            ForceDraw();
        }
    }
}