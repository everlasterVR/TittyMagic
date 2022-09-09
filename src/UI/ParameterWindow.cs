using System;
using System.Linq;
using TittyMagic.Handlers;
using TittyMagic.Models;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static TittyMagic.Script;

namespace TittyMagic.UI
{
    public class ParameterWindow : WindowBase
    {
        private readonly PhysicsParameterGroup _parameterGroup;
        private readonly PhysicsParameter _parameter;

        public JSONStorableAction recalibrationAction { get; }
        private float _offsetWhenCalibrated;

        public UIDynamicButton parentButton { private get; set; }

        private readonly ColliderVisualizer _visualizer = SoftPhysicsHandler.colliderVisualizer;

        public ParameterWindow(string id, PhysicsParameterGroup parameterGroup, UnityAction onReturnToParent) : base(id)
        {
            _parameterGroup = parameterGroup;
            _parameter = _parameterGroup.left;

            buildAction = () =>
            {
                CreateBackButton(false);
                elements["backButton"].AddListener(onReturnToParent);
                if(_parameterGroup.requiresRecalibration)
                {
                    CreateRecalibrateButton(recalibrationAction, true);
                }
                else if(_parameterGroup.allowsSoftColliderVisualization)
                {
                    CreateVisualizationToggle(true);

                    _visualizer.enabled = true;
                    _visualizer.ShowPreviewsJSON.val = true;
                }
                else
                {
                    AddSpacer("upperRightSpacer", 50, true);
                }

                CreateTitle(false);
                CreateApplyOnlyToLeftBreastToggle(true);
                CreateInfoTextArea(false);

                CreateOffsetSlider(true, spacing: 10);
                CreateCurrentValueSlider(true);

                /* Soft physics parameter group sections*/
                {
                    CreateColliderGroupSection(SoftColliderGroup.MAIN, false);
                    CreateColliderGroupSection(SoftColliderGroup.OUTER, true);
                    CreateColliderGroupSection(SoftColliderGroup.AREOLA, false);
                    CreateColliderGroupSection(SoftColliderGroup.NIPPLE, true);
                }
            };

            recalibrationAction = new JSONStorableAction("recalibrationAction",
                () =>
                {
                    _offsetWhenCalibrated = _parameter.offsetJsf.val;
                    if(id == ParamName.MASS)
                    {
                        tittyMagic.calculateBreastMass.actionCallback();
                    }
                    else
                    {
                        tittyMagic.recalibratePhysics.actionCallback();
                    }
                });

            closeAction = () =>
            {
                if(_parameterGroup.allowsSoftColliderVisualization)
                {
                    _visualizer.ShowPreviewsJSON.val = false;
                    _visualizer.enabled = false;
                }

                if(tittyMagic.calibration.shouldRun)
                {
                    tittyMagic.recalibratePhysics.actionCallback();
                }
            };
        }

        private void CreateVisualizationToggle(bool rightSide, int spacing = 0)
        {
            var storable = SoftPhysicsHandler.showSoftVerticesColliderPreviewsJsb;
            AddSpacer(storable.name, spacing, rightSide);
            var toggle = tittyMagic.CreateToggle(storable, rightSide);
            toggle.label = "Show Collider Previews";
            elements[storable.name] = toggle;
        }

        private void CreateTitle(bool rightSide)
        {
            var storable = new JSONStorableString("title", "");
            var textField = UIHelpers.HeaderTextField(storable, _parameterGroup.displayName, rightSide);
            textField.UItext.fontSize = 32;
            elements[storable.name] = textField;
        }

        private void CreateApplyOnlyToLeftBreastToggle(bool rightSide, int spacing = 0)
        {
            var storable = _parameterGroup.offsetOnlyLeftBreastJsb;
            AddSpacer(storable.name, spacing, rightSide);
            var toggle = tittyMagic.CreateToggle(storable, rightSide);
            toggle.label = "Apply Only To Left Breast";
            elements[storable.name] = toggle;
        }

        private void CreateInfoTextArea(bool rightSide, int spacing = 0)
        {
            var storable = new JSONStorableString("infoText", _parameterGroup.infoText);
            AddSpacer(storable.name, spacing, rightSide);

            var textField = tittyMagic.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.height = GetId() == ParamName.MASS ? 368 : 268;
            textField.backgroundColor = Color.clear;
            elements[storable.name] = textField;
        }

        private void CreateOffsetSlider(bool rightSide, int spacing = 0)
        {
            var storable = _parameter.offsetJsf;
            AddSpacer(storable.name, spacing, rightSide);

            var slider = tittyMagic.CreateSlider(storable, rightSide);
            slider.valueFormat = _parameter.valueFormat;
            slider.label = "Offset";

            slider.slider.onValueChanged.AddListener(value =>
            {
                parentButton.label = ParamButtonLabel();
                if(_parameterGroup.requiresRecalibration)
                {
                    tittyMagic.calibration.shouldRun = Math.Abs(value - _offsetWhenCalibrated) > 0.01f;
                }
            });

            elements[storable.name] = slider;
            _offsetWhenCalibrated = storable.val;
        }

        private void CreateCurrentValueSlider(bool rightSide, int spacing = 0)
        {
            var storable = _parameter.valueJsf;
            AddSpacer(storable.name, spacing, rightSide);

            var slider = tittyMagic.CreateSlider(storable, rightSide);
            slider.valueFormat = _parameter.valueFormat;
            slider.label = "Value";
            slider.SetActiveStyle(false, true);

            slider.slider.onValueChanged.AddListener(_ => SyncAllMultiplierSliderValues());

            elements[storable.name] = slider;
        }

        private void CreateColliderGroupSection(string group, bool rightSide)
        {
            if(!_parameter.groupMultiplierParams.ContainsKey(group))
            {
                return;
            }

            var groupParam = _parameter.groupMultiplierParams[group];
            CreateGroupHeader(group, rightSide);
            CreateMultiplierOffsetSlider(groupParam.offsetJsf, rightSide);
            CreateMultiplierSlider(groupParam.valueJsf, rightSide);
        }

        private void CreateGroupHeader(string group, bool rightSide)
        {
            var storable = new JSONStorableString(group + "Header", "");
            elements[storable.name] = UIHelpers.HeaderTextField(storable, group, rightSide);
        }

        private void CreateMultiplierSlider(JSONStorableFloat storable, bool rightSide, int spacing = 0)
        {
            AddSpacer(storable.name, spacing, rightSide);

            var slider = tittyMagic.CreateSlider(storable, rightSide);
            slider.valueFormat = _parameter.valueFormat;
            slider.SetActiveStyle(false, true);
            var uiInputField = slider.sliderValueTextFromFloat.UIInputField;
            uiInputField.contentType = InputField.ContentType.Standard;

            slider.slider.onValueChanged.AddListener(value => SyncMultiplierSliderLabel(slider, value));

            SyncMultiplierSliderLabel(slider, storable.val);

            elements[storable.name] = slider;
        }

        public void SyncAllMultiplierSliderValues()
        {
            foreach(var storable in _parameter.groupMultiplierParams.Select(kvp => kvp.Value.valueJsf))
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
            if(textFromFloat.UIInputField != null)
            {
                slider.label = $"Multiplier: {slider.slider.value:F2}              →";
                textFromFloat.floatVal = value * _parameter.valueJsf.val;
            }
        }

        private void CreateMultiplierOffsetSlider(JSONStorableFloat storable, bool rightSide, int spacing = 0)
        {
            AddSpacer(storable.name, spacing, rightSide);

            var slider = tittyMagic.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.label = "Multiplier Offset";
            slider.slider.onValueChanged.AddListener(value => parentButton.label = ParamButtonLabel());

            elements[storable.name] = slider;
        }

        public string ParamButtonLabel()
        {
            string label = $"  {_parameterGroup.displayName}";
            var groupOffsetStorables = _parameter.groupMultiplierParams.Select(kvp => kvp.Value.offsetJsf);
            if(_parameter.offsetJsf.val != 0 || groupOffsetStorables.Any(jsf => jsf.val != 0))
            {
                label += " *".Bold();
            }

            return label;
        }
    }
}
