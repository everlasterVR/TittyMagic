using System.Collections.Generic;
using UnityEngine.Events;

namespace TittyMagic.UI
{
    internal class ParameterWindow
    {
        private readonly Script _script;
        private readonly PhysicsParameter _leftParam;
        private readonly PhysicsParameter _rightParam;
        // ReSharper disable once MemberCanBePrivate.Global
        public Dictionary<string, UIDynamic> elements { get; private set; }

        public ParameterWindow(Script script, PhysicsParameter leftParam, PhysicsParameter rightParam)
        {
            _script = script;
            _leftParam = leftParam;
            _rightParam = rightParam;
        }

        public void Rebuild(UnityAction backButtonListener)
        {
            elements = new Dictionary<string, UIDynamic>();

            var backButton = _script.CreateButton("Back");
            // backButton.height = 52;

            backButton.AddListener(backButtonListener);

            elements["backButton"] = backButton;
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

        public void Clear()
        {
            foreach(var element in elements)
            {
                _script.RemoveElement(element.Value);
            }
        }
    }
}
