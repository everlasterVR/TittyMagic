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
        private readonly PhysicsParameter _parameter;
        private readonly UnityAction _returnToParent;

        private float _offsetWhenCalibrated;

        public string id { get; }
        public UIDynamicButton parentButton { private get; set; }

        public ParameterWindow(Script script, string id, PhysicsParameterGroup parameterGroup, UnityAction onReturnToParent) : base(script)
        {
            this.id = id;
            _parameterGroup = parameterGroup;
            _parameter = _parameterGroup.left;
            buildAction = BuildSelf;
            closeAction = ActionsOnClose;
            _returnToParent = () =>
            {
                Clear();
                onReturnToParent();
            };
        }

        private void BuildSelf()
        {
            CreateBackButton(_returnToParent, false);
            if(_parameterGroup.requiresRecalibration)
            {
                CreateRecalibrateButton(true);
            }
            else
            {
                AddSpacer("upperRightSpacer", 50, true);
            }

            if(id == ParamName.MASS)
            {
                CreateCalculateMassButton(true);
            }

            CreateTitle(false);
            CreateApplyOnlyToLeftBreastToggle(true);

            CreateInfoTextArea(false);
            CreateOffsetSlider(true, spacing: 10);
            CreateCurrentValueSlider(true);

            if(_parameter.groupMultiplierParams != null)
            {
                CreateColliderGroupSection(SoftColliderGroup.MAIN, false);
                CreateColliderGroupSection(SoftColliderGroup.OUTER, true);
                CreateColliderGroupSection(SoftColliderGroup.AREOLA, false);
                CreateColliderGroupSection(SoftColliderGroup.NIPPLE, true);
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

        private void CreateCalculateMassButton(bool rightSide, int spacing = 0)
        {
            var storable = script.calculateBreastMass;
            AddSpacer(storable.name, spacing, rightSide);

            var button = script.CreateButton("Calculate Breast Mass", rightSide);
            storable.RegisterButton(button);
            button.height = 53;
            elements[storable.name] = button;
        }

        private void CreateTitle(bool rightSide)
        {
            var storable = new JSONStorableString("title", "");
            var textField = UIHelpers.HeaderTextField(script, storable, _parameterGroup.displayName, rightSide);
            textField.UItext.fontSize = 32;
            elements[storable.name] = textField;
        }

        private void CreateApplyOnlyToLeftBreastToggle(bool rightSide, int spacing = 0)
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

            button.button.onClick.AddListener(() => _offsetWhenCalibrated = _parameter.offsetJsf.val);

            elements[storable.name] = button;
        }

        private void CreateInfoTextArea(bool rightSide, int spacing = 0)
        {
            var storable = new JSONStorableString("infoText", _parameterGroup.infoText);
            AddSpacer(storable.name, spacing, rightSide);

            var textField = script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.height = id == ParamName.MASS ? 368 : 268;
            textField.backgroundColor = Color.clear;
            elements[storable.name] = textField;
        }

        private void CreateOffsetSlider(bool rightSide, int spacing = 0)
        {
            var storable = _parameter.offsetJsf;
            AddSpacer(storable.name, spacing, rightSide);

            var slider = script.CreateSlider(storable, rightSide);
            slider.valueFormat = _parameter.valueFormat;

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

        private void CreateCurrentValueSlider(bool rightSide, int spacing = 0)
        {
            var storable = _parameter.valueJsf;
            AddSpacer(storable.name, spacing, rightSide);

            var slider = script.CreateSlider(storable, rightSide);
            slider.valueFormat = _parameter.valueFormat;
            slider.SetActiveStyle(false);
            slider.slider.interactable = false;
            slider.quickButtonsEnabled = false;
            slider.defaultButtonEnabled = false;

            slider.slider.onValueChanged.AddListener(_ => SyncAllMultiplierSliderValues());

            elements[storable.name] = slider;
        }

        private void CreateColliderGroupSection(string group, bool rightSide)
        {
            var groupParam = _parameter.GetGroupParam(group);
            CreateGroupHeader(group, rightSide);
            CreateMultiplierOffsetSlider(groupParam.offsetJsf, rightSide);
            CreateMultiplierSlider(groupParam.valueJsf, rightSide);
        }

        private void CreateGroupHeader(string group, bool rightSide)
        {
            var storable = new JSONStorableString(group + "Header", "");
            elements[storable.name] = UIHelpers.HeaderTextField(script, storable, group, rightSide);
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

            slider.slider.onValueChanged.AddListener(value => SyncMultiplierSliderLabel(slider, value));

            SyncMultiplierSliderLabel(slider, storable.val);

            elements[storable.name] = slider;
        }

        public void SyncAllMultiplierSliderValues()
        {
            foreach(var storable in _parameter.GetGroupMultiplierStorables())
            {
                var uiDynamicSlider = elements[storable.name] as UIDynamicSlider;
                if(uiDynamicSlider != null)
                {
                    SyncMultiplierSliderLabel(uiDynamicSlider, storable.val);
                }
            }
        }

        private void SyncMultiplierSliderLabel(UIDynamicSlider slider, float value)
        {
            var textFromFloat = slider.sliderValueTextFromFloat;
            string currentValue = (value * _parameter.valueJsf.val).ToString(_parameter.valueFormat);
            if(textFromFloat.UIInputField != null)
            {
                slider.label = $"Multiplier: {slider.slider.value:F2}              →";
                textFromFloat.UIInputField.text = currentValue;
            }
        }

        private void CreateMultiplierOffsetSlider(JSONStorableFloat storable, bool rightSide, int spacing = 0)
        {
            AddSpacer(storable.name, spacing, rightSide);

            var slider = script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.label = "Multiplier Offset";
            slider.slider.onValueChanged.AddListener(value => parentButton.label = ParamButtonLabel());

            elements[storable.name] = slider;
        }

        public string ParamButtonLabel()
        {
            string label = $"  {_parameterGroup.displayName}";
            if(_parameter.offsetJsf.val != 0 || _parameter.GetGroupOffsetStorables().Any(jsf => jsf.val != 0))
            {
                label += " *".Bold();
            }

            return label;
        }

        private void ActionsOnClose()
            => script.RecalibrateOnNavigation();
    }
}
