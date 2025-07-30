using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MVCTool
{
    public abstract class EditorTab
    {
        private EditorWindow _window;

        private List<EditorTabSection> _sections = new();

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
        /// Loads the tab's EditorPrefs or other persistent data. Called during instantiation and before OnEnter.
        /// </summary>
        private protected abstract void Load();

        /// <summary>
        /// Called when the tab is selected.
        /// </summary>
        public void Enter()
        {
            OnEnter();

            foreach (var section in _sections)
                section.OnEnter();
        }

        /// <summary>
        /// Called when the tab is selected.
        /// </summary>
        private protected abstract void OnEnter();

        /// <summary>
        /// Called when the tab is deselected or closed.
        /// </summary>
        public void Exit()
        {
            OnExit();

            foreach (var section in _sections)
                section.OnExit();
        }

        /// <summary>
        /// Called when the tab is deselected or closed.
        /// </summary>
        private protected abstract void OnExit();

        public void Draw()
        {
            OnDraw();

            for(int i = 0; i < _sections.Count; i++)
            {
                _sections[i].Draw();
                if (i < _sections.Count - 1) // Don't draw separator after the last section
                    MVCTheme.DrawSeparator();
            }

            OnDrawAfterSections();
        }

        /// <summary>
        /// Draws the content of the tab before any sections are drawn.
        /// </summary>
        private protected abstract void OnDraw();

        /// <summary>
        /// Draws the content of the tab after all sections have been drawn.
        /// </summary>
        private protected abstract void OnDrawAfterSections();

        public void Reset()
        {
            OnReset();

            foreach (var section in _sections)
                section.Reset();
        }

        /// <summary>
        /// Resets the tab's state, clearing any temporary data or UI elements.
        /// </summary>
        private protected abstract void OnReset();

        /// <summary>
        /// Redraws the tab's content in the editor window.
        /// Needed due to not being able to call Draw() outside of OnGUI.
        /// </summary>
        public void ForceDraw()
        {
            _window.Repaint();
        }

        /// <summary>
        /// Adds a section to the tab.
        /// </summary>
        private protected void AddSection(EditorTabSection section)
        {
            if(section == null)
                throw new ArgumentNullException(nameof(section), "Section cannot be null.");

            _sections.Add(section);
            section.Init(this);
        }
    }
}