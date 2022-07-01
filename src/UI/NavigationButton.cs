using UnityEngine;
using UnityEngine.Events;
using static TittyMagic.UI.UIHelpers;

namespace TittyMagic.UI
{
    internal class NavigationButton
    {
        private readonly UIDynamicButton _uiDynamicButton;
        private readonly string _label;

        public NavigationButton(UIDynamicButton uiDynamicButton, string label, Transform parent)
        {
            _uiDynamicButton = uiDynamicButton;
            _label = label;
            _uiDynamicButton.gameObject.transform.SetParent(parent, false);
            SetInactive();
        }

        public void SetActive()
        {
            _uiDynamicButton.label = _label.Color(funkyCyan);

            var colors = _uiDynamicButton.button.colors;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            _uiDynamicButton.button.colors = colors;
        }

        public void SetInactive()
        {
            _uiDynamicButton.buttonColor = darkerGray;
            _uiDynamicButton.label = _label.Color(Color.white);
            SetInteractable();
        }

        public void SetInteractable()
        {
            var colors = _uiDynamicButton.button.colors;
            colors.highlightedColor = defaultBtnHighlightedColor;
            colors.pressedColor = defaultBtnPressedColor;
            _uiDynamicButton.button.colors = colors;
        }

        public void AddListener(UnityAction call) => _uiDynamicButton.AddListener(call);

        public void RemoveListener(UnityAction call) => _uiDynamicButton.button.onClick.RemoveListener(call);

        public void RemoveAllListeners() => _uiDynamicButton.button.onClick.RemoveAllListeners();
    }
}
