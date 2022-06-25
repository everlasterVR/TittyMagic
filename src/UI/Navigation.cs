using System;
using UnityEngine;
using static TittyMagic.UI.UIHelpers;

namespace TittyMagic.UI
{
    internal class Navigation
    {
        private readonly RectTransform _leftUIContent;
        private readonly RectTransform _rightUIContent;

        public Func<UIDynamicButton> instantiateButton { private get; set; }

        public IWindow activeWindow { get; set; }

        public NavigationButton mainSettingsButton { get; private set; }
        public NavigationButton morphingButton { get; private set; }
        public NavigationButton gravityButton { get; private set; }
        public NavigationButton advancedButton { get; private set; }

        public Navigation(RectTransform leftUIContent, RectTransform rightUIContent)
        {
            _leftUIContent = leftUIContent;
            _rightUIContent = rightUIContent;
        }

        public void CreateUINavigationButtons()
        {
            var leftGroupTransform = CreateHorizontalLayoutGroup(_leftUIContent).transform;
            var rightGroupTransform = CreateHorizontalLayoutGroup(_rightUIContent).transform;

            mainSettingsButton = new NavigationButton(instantiateButton(), "Main", leftGroupTransform);
            morphingButton = new NavigationButton(instantiateButton(), "Morphing", leftGroupTransform);
            gravityButton = new NavigationButton(instantiateButton(), "Gravity physics", rightGroupTransform);
            advancedButton = new NavigationButton(instantiateButton(), "Advanced", rightGroupTransform);
        }

        public void ActivateMainSettingsTab()
        {
            mainSettingsButton.SetActive();
            morphingButton.SetInactive();
            gravityButton.SetInactive();
            advancedButton.SetInactive();
        }

        public void ActivateMorphingTab()
        {
            mainSettingsButton.SetInactive();
            morphingButton.SetActive();
            gravityButton.SetInactive();
            advancedButton.SetInactive();
        }

        public void ActivateGravityPhysicsTab()
        {
            mainSettingsButton.SetInactive();
            morphingButton.SetInactive();
            gravityButton.SetActive();
            advancedButton.SetInactive();
        }

        public void ActivateAdvancedTab()
        {
            mainSettingsButton.SetInactive();
            morphingButton.SetInactive();
            gravityButton.SetInactive();
            advancedButton.SetActive();
        }
    }
}
