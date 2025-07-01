using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MVCTool
{
    public class EditorTabManager
    {
        public List<EditorTab> Tabs { get; private set; }
        public EditorTab CurrentTab { get; private set; }
        public int CurrentTabIndex => Tabs.IndexOf(CurrentTab);

        private string _savedTabIndexSessionKey;

        public event Action<EditorTab> OnTabChanged = delegate { };

        public EditorTabManager(List<EditorTab> tabs, EditorWindow window, string savedTabIndexSessionKey)
        {
            if (tabs == null || tabs.Count == 0)
                throw new ArgumentException("Tabs list cannot be null or empty.", nameof(tabs));

            _savedTabIndexSessionKey = savedTabIndexSessionKey;

            foreach (EditorTab tab in tabs)
            {
                if (tab == null)
                    throw new ArgumentException("Tabs list cannot contain null elements.", nameof(tabs));
                tab.Init(window);
            }

            Tabs = tabs;
            ChangeTab(SessionState.GetInt(_savedTabIndexSessionKey, 0));
        }

        public void ChangeTab(int index)
        {
            if (index < 0 || index >= Tabs.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range of available tabs.");
            }
            ChangeTab(Tabs[index]);
        }

        private void ChangeTab(EditorTab newTab)
        {
            if (CurrentTab == newTab)
                return;

            CurrentTab?.OnExit();
            CurrentTab = newTab;
            GUI.FocusControl(null);
            CurrentTab?.OnEnter();

            SessionState.SetInt(_savedTabIndexSessionKey, Tabs.IndexOf(CurrentTab));

            OnTabChanged?.Invoke(newTab);
        }

        public void Draw()
        {
            CurrentTab?.Draw();
        }

        public void Shutdown()
        {
            CurrentTab?.OnExit();
        }

        public void Reset()
        {
            GUI.FocusControl(null);
            CurrentTab?.Reset();
        }

        public void DrawToolbar()
        {
            int currentIndex = CurrentTabIndex;
            string[] tabNames = GetTabNames();

            int newIndex = GUILayout.Toolbar(currentIndex, tabNames);
            if (newIndex != currentIndex)
                ChangeTab(newIndex);
        }

        private string[] GetTabNames()
        {
            string[] tabNames = new string[Tabs.Count];
            for (int i = 0; i < Tabs.Count; i++)
            {
                tabNames[i] = Tabs[i].TabName;
            }
            return tabNames;
        }
    }
}
