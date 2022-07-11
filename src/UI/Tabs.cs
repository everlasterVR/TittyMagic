using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace TittyMagic.UI
{
    internal class Tabs
    {
        private readonly Script _script;

        public IWindow activeWindow { get; private set; }

        private readonly Dictionary<IWindow, NavigationButton> _tabButtons;
        private readonly Transform _leftGroupTransform;
        private readonly Transform _rightGroupTransform;

        public Tabs(Script script)
        {
            _script = script;
            _leftGroupTransform = UIHelpers.CreateHorizontalLayoutGroup(_script.GetLeftUIContent()).transform;
            _rightGroupTransform = UIHelpers.CreateHorizontalLayoutGroup(_script.GetRightUIContent()).transform;
            _tabButtons = new Dictionary<IWindow, NavigationButton>();
        }

        public void CreateNavigationButton(IWindow window, string name, UnityAction callback)
        {
            var parent = _tabButtons.Count < 2 ? _leftGroupTransform : _rightGroupTransform;
            var button = new NavigationButton(_script.InstantiateButton(), name, parent);
            button.AddListener(callback);
            _tabButtons[window] = button;
        }

        public void ActivateTab(IWindow window)
        {
            var navigationButton = _tabButtons[window];
            navigationButton.SetActive();
            activeWindow = window;
            _tabButtons
                .Where(kvp => kvp.Key != window)
                .ToList()
                .ForEach(kvp => kvp.Value.SetInactive());
        }
    }
}
