using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TittyMagic.UI
{
    internal class HardCollidersWindow
    {
        private readonly Script _script;

        // ReSharper disable once MemberCanBePrivate.Global
        public Dictionary<string, UIDynamic> elements { get; private set; }

        public HardCollidersWindow(Script script)
        {
            _script = script;
        }

        public void Rebuild(UnityAction backButtonListener)
        {
            elements = new Dictionary<string, UIDynamic>();

            CreateBackButton(backButtonListener, false);
        }

        private void CreateBackButton(UnityAction backButtonListener, bool rightSide)
        {
            var button = _script.CreateButton("Return", rightSide);
            button.textColor = Color.white;
            button.buttonColor = UIHelpers.sliderGray;
            button.AddListener(backButtonListener);
            elements["backButton"] = button;
        }

        public List<UIDynamicSlider> GetSliders()
        {
            var sliders = new List<UIDynamicSlider>();
            if(elements != null)
            {
                //TODO
            }

            return sliders;
        }

        public void Clear() =>
            elements.ToList().ForEach(element => _script.RemoveElement(element.Value));
    }
}
