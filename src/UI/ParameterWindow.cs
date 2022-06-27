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
        private readonly JSONStorableString _infoText;

        public ParameterWindow(Script script, PhysicsParameter leftParam, PhysicsParameter rightParam)
        {
            _script = script;
            _leftParam = leftParam;
            _rightParam = rightParam;

            _title = new JSONStorableString($"title", "");
            _infoText = new JSONStorableString("infoText", "");

            _infoText.val = "\n".Size(12) + leftParam.infoText;
        }

        public void Rebuild(UnityAction backButtonListener)
        {
            elements = new Dictionary<string, UIDynamic>();

            CreateBackButton(backButtonListener, false);
            CreateInfoTextArea(_infoText, false);
            CreateHeader(true);
            elements["headerMargin"] = _script.NewSpacer(20, true);

            if(_leftParam.currentValue != null)
            {
                CreateCurrentValueSlider(_leftParam, true);
            }

            if(_leftParam.baseValue != null)
            {
                CreateBaseValueSlider(_leftParam, true);
            }
        }

        private void CreateCurrentValueSlider(PhysicsParameter param, bool rightSide)
        {
            var slider = _script.CreateSlider(param.currentValue, rightSide);
            slider.valueFormat = param.valueFormat;
            slider.slider.interactable = false;
            slider.quickButtonsEnabled = false;
            slider.defaultButtonEnabled = false;
            elements[param.currentValue.name] = slider;
        }

        private void CreateBaseValueSlider(PhysicsParameter param, bool rightSide)
        {
            var slider = _script.CreateSlider(param.baseValue, rightSide);
            slider.valueFormat = param.valueFormat;
            slider.slider.interactable = false;
            slider.quickButtonsEnabled = false;
            slider.defaultButtonEnabled = false;
            elements[param.baseValue.name] = slider;
        }

        private void CreateBackButton(UnityAction backButtonListener, bool rightSide)
        {
            var button = _script.CreateButton("<  Back".Bold(), rightSide);
            button.textColor = Color.white;
            button.buttonColor = UIHelpers.sliderGray;
            button.AddListener(backButtonListener);
            elements["backButton"] = button;
        }

        private void CreateInfoTextArea(JSONStorableString storable, bool rightSide)
        {
            var textField = _script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.height = 390;
            elements[storable.name] = textField;
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
