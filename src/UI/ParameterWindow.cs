using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TittyMagic.UI
{
    internal class ParameterWindow : WindowBase
    {
        private readonly PhysicsParameterGroup _parameterGroup;
        private readonly UnityAction _returnToParent;

        private readonly JSONStorableString _title;
        private readonly JSONStorableString _infoText;
        private float _offsetWhenCalibrated;

        public string id { get; }
        public UIDynamicButton parentButton { private get; set; }

        public ParameterWindow(Script script, string id, PhysicsParameterGroup parameterGroup, UnityAction onReturnToParent) : base(script)
        {
            this.id = id;
            _parameterGroup = parameterGroup;
            buildAction = BuildSelf;
            closeAction = ActionsOnClose;
            _returnToParent = () =>
            {
                Clear();
                onReturnToParent();
            };

            _title = new JSONStorableString("title", "");
            _infoText = new JSONStorableString("infoText", "");
        }

        private void BuildSelf()
        {
            CreateBackButton(_returnToParent, false);

            CreateTitle(false);
            CreateInfoTextArea(false);
            CreateCurrentValueSlider(false);

            if(_parameterGroup.requiresRecalibration)
            {
                CreateApplyOnlyToLeftBreastToggle(true, spacing: 298);
                CreateRecalibrateButton(true); // 363
            }
            else
            {
                CreateApplyOnlyToLeftBreastToggle(true, spacing: 363);
            }

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
            var button = script.CreateButton("Return", rightSide);

            button.textColor = Color.white;
            var colors = button.button.colors;
            colors.normalColor = UIHelpers.sliderGray;
            colors.highlightedColor = Color.grey;
            colors.pressedColor = Color.grey;
            button.button.colors = colors;

            button.AddListener(backButtonListener);
            elements["backButton"] = button;
        }

        private void CreateTitle(bool rightSide)
        {
            var textField = UIHelpers.TitleTextField(script, _title, _parameterGroup.displayName, 62, rightSide);
            textField.UItext.fontSize = 32;
            elements[_title.name] = textField;
        }

        private void CreateApplyOnlyToLeftBreastToggle(bool rightSide, int spacing)
        {
            var storable = _parameterGroup.offsetOnlyLeftBreastJsb;
            AddSpacer(storable.name, spacing, rightSide);
            var toggle = script.CreateToggle(storable, rightSide);
            toggle.label = "Apply Only To Left Breast";
            elements[storable.name] = toggle;
        }

        private void CreateRecalibrateButton(bool rightSide, int spacing = 0)
        {
            var storable = script.recalibratePhysics;
            AddSpacer(storable.name, spacing, rightSide);

            var button = script.CreateButton("Recalibrate Physics", rightSide);
            storable.RegisterButton(button);
            button.height = 52;

            button.button.onClick.AddListener(() => _offsetWhenCalibrated = _parameterGroup.offsetJsf.val);

            elements[storable.name] = button;
        }

        private void CreateInfoTextArea(bool rightSide, int spacing = 0)
        {
            var storable = _infoText;
            storable.val = _parameterGroup.infoText;
            AddSpacer(storable.name, spacing, rightSide);

            var textField = script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.height = 288;
            textField.backgroundColor = Color.clear;
            elements[storable.name] = textField;
        }

        private void CreateCurrentValueSlider(bool rightSide, int spacing = 0)
        {
            var storable = _parameterGroup.currentValueJsf;
            AddSpacer(storable.name, spacing, rightSide);

            var slider = script.CreateSlider(storable, rightSide);
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

            var slider = script.CreateSlider(storable, rightSide);
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

            var slider = script.CreateSlider(storable, rightSide);
            slider.valueFormat = _parameterGroup.valueFormat;

            slider.slider.onValueChanged.AddListener(value =>
            {
                parentButton.label = ParamButtonLabel();
                if(_parameterGroup.requiresRecalibration)
                {
                    script.recalibrationNeeded = Math.Abs(value - _offsetWhenCalibrated) > 0.01f;
                }
            });

            elements[storable.name] = slider;
            _offsetWhenCalibrated = storable.val;
        }

        private void CreateMultiplierOffsetSlider(JSONStorableFloat storable, bool rightSide, int spacing = 0)
        {
            AddSpacer(storable.name, spacing, rightSide);

            var slider = script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";

            slider.slider.onValueChanged.AddListener(value => parentButton.label = ParamButtonLabel());

            elements[storable.name] = slider;
        }

        public string ParamButtonLabel()
        {
            string label = $"  {_parameterGroup.displayName}";
            if(_parameterGroup.offsetJsf.val != 0 || _parameterGroup.groupOffsetStorables.Any(jsf => jsf.val != 0))
            {
                label += " *".Bold();
            }

            return label;
        }

        private void ActionsOnClose()
            => script.RecalibrateOnNavigation();
    }
}
