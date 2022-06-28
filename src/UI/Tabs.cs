using System;
using UnityEngine;

namespace TittyMagic.UI
{
    internal class Tabs
    {
        private readonly Script _script;

        public IWindow activeWindow { get; set; }

        public NavigationButton tab1Button { get; private set; }
        public NavigationButton tab2Button { get; private set; }
        public NavigationButton tab3Button { get; private set; }
        public NavigationButton tab4Button { get; private set; }

        public Tabs(Script script)
        {
            _script = script;
        }

        public void CreateUINavigationButtons()
        {
            var leftGroupTransform = UIHelpers.CreateHorizontalLayoutGroup(_script.GetLeftUIContent()).transform;
            var rightGroupTransform = UIHelpers.CreateHorizontalLayoutGroup(_script.GetRightUIContent()).transform;

            tab1Button = new NavigationButton(_script.InstantiateButton(), "Control", leftGroupTransform);
            tab2Button = new NavigationButton(_script.InstantiateButton(), "Physics Params", leftGroupTransform);
            tab3Button = new NavigationButton(_script.InstantiateButton(), "Morph Multipliers", rightGroupTransform);
            tab4Button = new NavigationButton(_script.InstantiateButton(), "Gravity Multipliers", rightGroupTransform);
        }

        public void ActivateTab1()
        {
            tab1Button.SetActive();
            tab2Button.SetInactive();
            tab3Button.SetInactive();
            tab4Button.SetInactive();
        }

        public void ActivateTab2()
        {
            tab1Button.SetInactive();
            tab2Button.SetActive();
            tab4Button.SetInactive();
            tab3Button.SetInactive();
        }

        public void ActivateTab3()
        {
            tab1Button.SetInactive();
            tab2Button.SetInactive();
            tab3Button.SetActive();
            tab4Button.SetInactive();
        }

        public void ActivateTab4()
        {
            tab1Button.SetInactive();
            tab2Button.SetInactive();
            tab3Button.SetInactive();
            tab4Button.SetActive();
        }
    }
}
