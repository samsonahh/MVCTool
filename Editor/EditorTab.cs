using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MVCTool
{
    public abstract class EditorTab
    {
        private EditorWindow _window;
        public abstract string TabName { get; }

        /// <summary>
        /// Initializes the tab with the editor window it belongs to.
        /// </summary>
        /// <param name="window"></param>
        public void Init(EditorWindow window)
        {
            _window = window;
            Load();
        }

        /// <summary>
        /// Called when the tab is selected.
        /// </summary>
        public abstract void OnEnter();

        /// <summary>
        /// Called when the tab is deselected or closed.
        /// </summary>
        public abstract void OnExit();

        /// <summary>
        /// Draws the content of the tab.
        /// </summary>
        public abstract void Draw();

        /// <summary>
        /// Loads the tab's EditorPrefs or other persistent data. Called during instantiation and before OnEnter.
        /// </summary>
        private protected abstract void Load();

        /// <summary>
        /// Resets the tab's state, clearing any temporary data or UI elements.
        /// </summary>
        public abstract void Reset();

        /// <summary>
        /// Redraws the tab's content in the editor window.
        /// Needed due to not being able to call Draw() outside of OnGUI.
        /// </summary>
        private protected void ForceDraw()
        {
            _window.Repaint();
        }
    }
}