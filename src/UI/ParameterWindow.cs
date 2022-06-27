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
            CreateInfoTextArea(true);

            CreateHeader(false);
            elements["headerMargin"] = _script.NewSpacer(20);

            if(_leftParam.currentValue != null)
                CreateCurrentValueSlider(_leftParam, false);

            if(_leftParam.baseValue != null)
                CreateBaseValueSlider(_leftParam, false);
        }

        private void CreateBackButton(UnityAction backButtonListener, bool rightSide)
        {
            var button = _script.CreateButton("<  Back".Bold(), rightSide);
            button.textColor = Color.white;
            button.buttonColor = UIHelpers.sliderGray;
            button.AddListener(backButtonListener);
            elements["backButton"] = button;
        }

        private void CreateCurrentValueSlider(PhysicsParameter param, bool rightSide, int spacing = 0)
        {
            var storable = param.currentValue;
            AddSpacer(storable.name, spacing, rightSide);

            var slider = _script.CreateSlider(storable, rightSide);
            slider.valueFormat = param.valueFormat;
            slider.slider.interactable = false;
            slider.quickButtonsEnabled = false;
            slider.defaultButtonEnabled = false;
            elements[storable.name] = slider;
        }

        private void CreateBaseValueSlider(PhysicsParameter param, bool rightSide, int spacing = 0)
        {
            var storable = param.baseValue;
            AddSpacer(storable.name, spacing, rightSide);

            var slider = _script.CreateSlider(storable, rightSide);
            slider.valueFormat = param.valueFormat;
            slider.slider.interactable = false;
            slider.quickButtonsEnabled = false;
            slider.defaultButtonEnabled = false;
            elements[storable.name] = slider;
        }

        private void CreateInfoTextArea(bool rightSide, int spacing = 0)
        {
            var storable = _infoText;
            AddSpacer(storable.name, spacing, rightSide);

            var textField = _script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.height = 430;
            textField.backgroundColor = Color.clear;
            elements[storable.name] = textField;
        }

        private void CreateHeader(bool rightSide)
        {
            elements[_title.name] = UIHelpers.TitleTextField(_script, _title, _leftParam.displayName, 62, rightSide);
        }

        private void AddSpacer(string name, int height, bool rightSide)
        {
            elements[$"{name}Spacer"] = _script.NewSpacer(height, rightSide);
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
