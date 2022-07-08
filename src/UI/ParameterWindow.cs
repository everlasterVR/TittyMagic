using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TittyMagic.UI
{
    internal class ParameterWindow
    {
        private readonly Script _script;
        private readonly PhysicsParameterGroup _parameterGroup;

        // ReSharper disable once MemberCanBePrivate.Global
        public Dictionary<string, UIDynamic> elements { get; private set; }

        private readonly JSONStorableString _title;
        private readonly JSONStorableString _infoText;

        public ParameterWindow(Script script, PhysicsParameterGroup parameterGroup)
        {
            _script = script;
            _parameterGroup = parameterGroup;

            _title = new JSONStorableString("title", "");
            _infoText = new JSONStorableString("infoText", "");

            _infoText.val = "\n".Size(12) + _parameterGroup.infoText;
        }

        public void Rebuild(UnityAction backButtonListener)
        {
            elements = new Dictionary<string, UIDynamic>();

            CreateBackButton(backButtonListener, false);
            // CreateInfoTextArea(true);

            CreateTitle(false);
            elements["headerMargin"] = _script.NewSpacer(20);
            elements["rightSideMargin"] = _script.NewSpacer(162, true);

            CreateCurrentValueSlider(false);
            foreach(var storable in _parameterGroup.groupMultiplierStorables)
            {
                CreateMultiplierSlider(storable, false);
            }

            CreateOffsetSlider(true);
            foreach(var storable in _parameterGroup.groupOffsetStorables)
            {
                CreateMultiplierOffsetSlider(storable, true);
            }
        }

        private void CreateBackButton(UnityAction backButtonListener, bool rightSide)
        {
            var button = _script.CreateButton("Return", rightSide);

            button.textColor = Color.white;
            var colors = button.button.colors;
            colors.normalColor = UIHelpers.sliderGray;
            colors.highlightedColor = Color.grey;
            colors.pressedColor = Color.grey;
            button.button.colors = colors;

            button.AddListener(backButtonListener);
            elements["backButton"] = button;
        }

        private void CreateCurrentValueSlider(bool rightSide, int spacing = 0)
        {
            var storable = _parameterGroup.currentValueJsf;
            AddSpacer(storable.name, spacing, rightSide);

            var slider = _script.CreateSlider(storable, rightSide);
            slider.valueFormat = _parameterGroup.valueFormat;
            slider.SetActiveStyle(false);
            slider.slider.interactable = false;
            slider.quickButtonsEnabled = false;
            slider.defaultButtonEnabled = false;

            slider.slider.onValueChanged.AddListener(SyncAllMultiplierSliderValues);

            elements[storable.name] = slider;
        }

        private void CreateMultiplierSlider(JSONStorableFloat storable, bool rightSide, int spacing = 0)
        {
            AddSpacer(storable.name, spacing, rightSide);

            var slider = _script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.SetActiveStyle(false);
            slider.slider.interactable = false;
            slider.quickButtonsEnabled = false;
            slider.defaultButtonEnabled = false;
            var uiInputField = slider.sliderValueTextFromFloat.UIInputField;
            uiInputField.contentType = InputField.ContentType.Standard;

            slider.slider.onValueChanged.AddListener(value => SyncMultiplierSliderText(slider, storable.name, value));
            SyncMultiplierSliderText(slider, storable.name, storable.val);

            elements[storable.name] = slider;
        }

        private void SyncAllMultiplierSliderValues(float value)
        {
            foreach(var storable in _parameterGroup.groupMultiplierStorables)
            {
                var uiDynamicSlider = elements[storable.name] as UIDynamicSlider;
                if(uiDynamicSlider != null)
                {
                    SyncMultiplierSliderText(uiDynamicSlider, storable.name, storable.val);
                }
            }
        }

        private void SyncMultiplierSliderText(UIDynamicSlider slider, string label, float value)
        {
            var textFromFloat = slider.sliderValueTextFromFloat;
            string currentValue = (value * _parameterGroup.currentValueJsf.val).ToString(_parameterGroup.valueFormat);
            if(textFromFloat.UIInputField != null)
            {
                slider.label = $"{label}: {slider.slider.value:F2}";
                textFromFloat.UIInputField.text = currentValue;
            }
        }

        private void CreateOffsetSlider(bool rightSide, int spacing = 0)
        {
            var storable = _parameterGroup.offsetJsf;
            AddSpacer(storable.name, spacing, rightSide);

            var slider = _script.CreateSlider(storable, rightSide);
            slider.valueFormat = _parameterGroup.valueFormat;
            elements[storable.name] = slider;
        }

        private void CreateMultiplierOffsetSlider(JSONStorableFloat storable, bool rightSide, int spacing = 0)
        {
            AddSpacer(storable.name, spacing, rightSide);

            var slider = _script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
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

        private void CreateTitle(bool rightSide)
        {
            var textField = UIHelpers.TitleTextField(_script, _title, _parameterGroup.displayName, 62, rightSide);
            textField.UItext.fontSize = 32;
            elements[_title.name] = textField;
        }

        private void AddSpacer(string name, int height, bool rightSide) =>
            elements[$"{name}Spacer"] = _script.NewSpacer(height, rightSide);

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
