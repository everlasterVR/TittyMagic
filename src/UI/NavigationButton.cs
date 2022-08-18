using UnityEngine;
using UnityEngine.Events;
using static TittyMagic.UI.UIHelpers;

namespace TittyMagic.UI
{
    internal class NavigationButton
    {
        private readonly UIDynamicButton _uiDynamicButton;

        public NavigationButton(UIDynamicButton uiDynamicButton, string label, Transform parent)
        {
            _uiDynamicButton = uiDynamicButton;
            _uiDynamicButton.label = label;
            _uiDynamicButton.gameObject.transform.SetParent(parent, false);
            SetInactive();
        }

        public void SetActive()
        {
            _uiDynamicButton.textColor = funkyCyan;
            var colors = _uiDynamicButton.button.colors;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            _uiDynamicButton.button.colors = colors;
        }

        public void SetInactive()
        {
            _uiDynamicButton.textColor = Color.white;
            _uiDynamicButton.buttonColor = darkerGray;

            /* Set interactable style */
            var colors = _uiDynamicButton.button.colors;
            colors.highlightedColor = defaultBtnHighlightedColor;
            colors.pressedColor = defaultBtnPressedColor;
            _uiDynamicButton.button.colors = colors;
        }

        public void AddListener(UnityAction unityAction)
        {
            _uiDynamicButton.AddListener(unityAction);
        }
    }
}
