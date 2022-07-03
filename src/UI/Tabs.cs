using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace TittyMagic.UI
{
    internal class Tabs
    {
        private readonly Script _script;

        public IWindow activeWindow { get; set; }

        private readonly Dictionary<int, NavigationButton> _tabButtons;
        private readonly Transform _leftGroupTransform;
        private readonly Transform _rightGroupTransform;

        public Tabs(Script script)
        {
            _script = script;
            _leftGroupTransform = UIHelpers.CreateHorizontalLayoutGroup(_script.GetLeftUIContent()).transform;
            _rightGroupTransform = UIHelpers.CreateHorizontalLayoutGroup(_script.GetRightUIContent()).transform;
            _tabButtons = new Dictionary<int, NavigationButton>();
        }

        public void CreateNavigationButton(int windowId, string name, UnityAction callback)
        {
            var parent = _tabButtons.Count < 2 ? _leftGroupTransform : _rightGroupTransform;
            var button = new NavigationButton(_script.InstantiateButton(), name, parent);
            button.AddListener(callback);
            _tabButtons[windowId] = button;
        }

        public void ActivateTab(int windowId)
        {
            var navigationButton = _tabButtons[windowId];
            navigationButton.SetActive();
            _tabButtons
                .Where(kvp => kvp.Key != windowId)
                .ToList()
                .ForEach(kvp => kvp.Value.SetInactive());
        }
    }
}
