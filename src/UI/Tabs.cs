using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TittyMagic.UI.Components;
using static TittyMagic.Script;

namespace TittyMagic.UI
{
    public class Tabs
    {
        public IWindow activeWindow { get; private set; }

        private readonly Dictionary<IWindow, NavigationButton> _tabButtons;
        private readonly Transform _leftGroupTransform;
        private readonly Transform _rightGroupTransform;

        public Tabs(RectTransform leftUIContent, RectTransform rightUIContent)
        {
            _leftGroupTransform = CreateHorizontalLayoutGroup(leftUIContent).transform;
            _rightGroupTransform = CreateHorizontalLayoutGroup(rightUIContent).transform;
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

        private static HorizontalLayoutGroup CreateHorizontalLayoutGroup(RectTransform uiContent)
        {
            var verticalLayoutGroup = uiContent.GetComponent<VerticalLayoutGroup>();

            var gameObj = new GameObject();
            gameObj.transform.SetParent(verticalLayoutGroup.transform, false);

            var horizontalLayoutGroup = gameObj.AddComponent<HorizontalLayoutGroup>();
            horizontalLayoutGroup.spacing = 16f;
            horizontalLayoutGroup.childForceExpandWidth = true;
            horizontalLayoutGroup.childControlHeight = false;

            return horizontalLayoutGroup;
        }
    }
}
