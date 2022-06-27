using System.Collections.Generic;
using UnityEngine;
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

        private readonly JSONStorableString _title;

        public ParameterWindow(Script script, PhysicsParameter leftParam, PhysicsParameter rightParam)
        {
            _script = script;
            _leftParam = leftParam;
            _rightParam = rightParam;

            _title = new JSONStorableString("leftHeader", "");
        }

        public void Rebuild(UnityAction backButtonListener)
        {
            elements = new Dictionary<string, UIDynamic>();

            CreateBackButton(backButtonListener, false);
            elements["leftSpacer"] = _script.NewSpacer(10, true);

            CreateHeader(true);
        }

        private void CreateBackButton(UnityAction backButtonListener, bool rightSide)
        {
            var button = _script.CreateButton("<  Back".Bold(), rightSide);
            button.textColor = Color.white;
            button.buttonColor = UIHelpers.sliderGray;
            button.AddListener(backButtonListener);
            elements["backButton"] = button;
        }

        private void CreateHeader(bool rightSide)
        {
            elements[_title.name] = UIHelpers.TitleTextField(_script, _title, _leftParam.displayName, 62, rightSide);
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
