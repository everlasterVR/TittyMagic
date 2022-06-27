using System;
using UnityEngine;
using static TittyMagic.UI.UIHelpers;

namespace TittyMagic.UI
{
    internal class Tabs
    {
        private readonly Script _script;

        public IWindow activeWindow { get; set; }

        public NavigationButton mainSettingsButton { get; private set; }
        public NavigationButton morphingButton { get; private set; }
        public NavigationButton gravityButton { get; private set; }
        public NavigationButton advancedButton { get; private set; }

        public Tabs(Script script)
        {
            _script = script;
        }

        public void CreateUINavigationButtons()
        {
            var leftGroupTransform = CreateHorizontalLayoutGroup(_script.GetLeftUIContent()).transform;
            var rightGroupTransform = CreateHorizontalLayoutGroup(_script.GetRightUIContent()).transform;

            mainSettingsButton = new NavigationButton(_script.InstantiateButton(), "Main", leftGroupTransform);
            morphingButton = new NavigationButton(_script.InstantiateButton(), "Morphing", leftGroupTransform);
            gravityButton = new NavigationButton(_script.InstantiateButton(), "Gravity physics", rightGroupTransform);
            advancedButton = new NavigationButton(_script.InstantiateButton(), "Advanced", rightGroupTransform);
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
