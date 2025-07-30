using UnityEditor;
using UnityEngine;

namespace MVCTool
{
    public abstract class EditorTabSection
    {
        private EditorTab _tab;

        public abstract string SectionName { get; }
        public void Init(EditorTab tab)
        {
            _tab = tab;
            Load();
        }

        /// <summary>
        /// Loads the section's EditorPrefs or other persistent data. Called during instantiation and before OnEnter.
        /// </summary>
        private protected abstract void Load();

        /// <summary>
        /// Called when the tab is selected.
        /// </summary>
        public abstract void OnEnter();

        /// <summary>
        /// Called when the tab is deselected or closed.
        /// </summary>
        public abstract void OnExit();

        /// <summary>
        /// Draws the content of the section.
        /// </summary>
        public void Draw()
        {
            GUILayout.Label($"{SectionName}", MVCTheme.HeadingStyle);
            MVCTheme.DrawSeparator();

            OnDraw();
        }

        /// <summary>
        /// Draws the content of the section.
        /// </summary>
        private protected abstract void OnDraw();

        /// <summary>
        /// Resets the tab's state, clearing any temporary data or UI elements.
        /// </summary>
        public abstract void Reset();

        /// <summary>
        /// Redraws the owner tab's content in the editor window.
        /// Needed due to not being able to call Draw() outside of OnGUI.
        /// </summary>
        public void ForceDraw()
        {
            _tab.ForceDraw();
        }
    }
}