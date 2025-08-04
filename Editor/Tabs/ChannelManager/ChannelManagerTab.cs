using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MVCTool
{
    public class ChannelManagerTab : EditorTab
    {
        public override string TabName => "Channel";

        public ChannelManagerSelectSection SelectSection { get; private set; }
        public ChannelManagerCreateSection CreateSection { get; private set; }
        public ChannelManagerDeleteSection DeleteSection { get; private set; }

        private protected override void Load()
        {
            SelectSection = new ChannelManagerSelectSection();
            CreateSection = new ChannelManagerCreateSection();
            DeleteSection = new ChannelManagerDeleteSection();

            AddSection(SelectSection);
            AddSection(CreateSection);
            AddSection(DeleteSection);
        }

        private protected override void OnEnter()
        {
            
        }

        private protected override void OnExit()
        {

        }

        private protected override void OnDraw()
        {
            EditorGUI.BeginDisabledGroup(!LoginApi.IsLoggedIn);
        }

        private protected override void OnDrawAfterSections()
        {
            EditorGUI.EndDisabledGroup();

            if (!LoginApi.IsLoggedIn)
                EditorGUILayout.HelpBox("You must be logged in to manage channels.", MessageType.Warning);
        }

        private protected override void OnReset()
        {
            
        }
    }
}