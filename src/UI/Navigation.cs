using System;
using UnityEngine;
using static TittyMagic.UI.UIHelpers;

namespace TittyMagic.UI
{
    internal class Navigation
    {
        private readonly RectTransform _leftUIContent;
        private readonly RectTransform _rightUIContent;
        public Func<UIDynamicButton> instantiateButton;

        public IWindow activeWindow;

        public UIDynamicButton mainSettingsButton;
        public UIDynamicButton morphingButton;
        public UIDynamicButton gravityButton;
        public UIDynamicButton physicsButton;

        public Navigation(RectTransform leftUIContent, RectTransform rightUIContent)
        {
            _leftUIContent = leftUIContent;
            _rightUIContent = rightUIContent;
        }

        public void CreateUINavigationButtons()
        {
            var leftGroup = CreateHorizontalLayoutGroup(_leftUIContent);
            var rightGroup = CreateHorizontalLayoutGroup(_rightUIContent);

            mainSettingsButton = instantiateButton();
            mainSettingsButton.gameObject.transform.SetParent(leftGroup.transform, false);
            mainSettingsButton.label = "Main";

            morphingButton = instantiateButton();
            morphingButton.gameObject.transform.SetParent(leftGroup.transform, false);
            morphingButton.label = "Morphing";

            gravityButton = instantiateButton();
            gravityButton.gameObject.transform.SetParent(rightGroup.transform, false);
            gravityButton.label = "Gravity physics";

            physicsButton = instantiateButton();
            physicsButton.gameObject.transform.SetParent(rightGroup.transform, false);
            physicsButton.label = "Advanced";
        }
    }
}
