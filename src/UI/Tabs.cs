using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using static TittyMagic.Script;

namespace TittyMagic.UI
{
    internal class Tabs
    {
        public IWindow activeWindow { get; private set; }

        private readonly Dictionary<IWindow, NavigationButton> _tabButtons;
        private readonly Transform _leftGroupTransform;
        private readonly Transform _rightGroupTransform;

        public Tabs(RectTransform leftUIContent, RectTransform rightUIContent)
        {
            _leftGroupTransform = UIHelpers.CreateHorizontalLayoutGroup(leftUIContent).transform;
            _rightGroupTransform = UIHelpers.CreateHorizontalLayoutGroup(rightUIContent).transform;
            _tabButtons = new Dictionary<IWindow, NavigationButton>();
        }

        public void CreateNavigationButton(IWindow window, string name, UnityAction callback)
        {
            var parent = _tabButtons.Count < 2 ? _leftGroupTransform : _rightGroupTransform;
            var button = new NavigationButton(tittyMagic.InstantiateButton().GetComponent<UIDynamicButton>(), name, parent);
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
