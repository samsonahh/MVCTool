using UnityEditor;
using UnityEngine;

namespace MVCTool
{
    public static class MVCTheme
    {
        public static void DrawSeparator()
        {
            EditorGUILayout.Space(4);
            Rect rect = EditorGUILayout.GetControlRect(false, 2);
            EditorGUI.DrawRect(rect, Color.gray);
            EditorGUILayout.Space(4);
        }

        public static GUIStyle HeadingStyle => new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 20,
            alignment = TextAnchor.MiddleCenter
        };

        public static GUIStyle BoxStyle => new GUIStyle(EditorStyles.helpBox)
        {
            fontSize = 12,
            wordWrap = true
        };
    }
}