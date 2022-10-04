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

        private readonly UnityAction _onReturnToParent;

        public ParameterWindow(string id, PhysicsParameterGroup parameterGroup, UnityAction onReturnToParent) : base(id)
        {
            _parameterGroup = parameterGroup;
            _parameter = _parameterGroup.left;
            _onReturnToParent = onReturnToParent;

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
        }

        protected override void OnBuild()
        {
            CreateBackButton(false, _onReturnToParent);

            if(_parameterGroup.requiresRecalibration)
            {
                CreateRecalibrateButton(recalibrationAction, true);
            }
            else if(_parameterGroup.allowsSoftColliderVisualization)
            {
                /* Visualization toggle */
                var storable = SoftPhysicsHandler.showSoftVerticesColliderPreviewsJsb;
                var toggle = tittyMagic.CreateToggle(storable, true);
                toggle.label = "Show Collider Previews";
                elements[storable.name] = toggle;

                _visualizer.enabled = true;
                _visualizer.ShowPreviewsJSON.val = true;
            }
            else
            {
                AddSpacer("upperRightSpacer", 50, true);
            }

            CreateHeaderTextField(new JSONStorableString("parameterHeader", _parameterGroup.displayName));

            /* Apply only to left breast toggle */
            {
                var storable = _parameterGroup.offsetOnlyLeftBreastJsb;
                var toggle = tittyMagic.CreateToggle(storable, true);
                toggle.label = "Apply Only To Left Breast";
                elements[storable.name] = toggle;
            }

            /* Info text area */
            {
                var storable = new JSONStorableString("infoText", _parameterGroup.infoText);
                var textField = tittyMagic.CreateTextField(storable);
                textField.UItext.fontSize = 28;
                textField.height = GetId() == ParamName.MASS ? 368 : 268;
                textField.backgroundColor = Color.clear;
                elements[storable.name] = textField;
            }

            /* Offset slider */
            {
                var storable = _parameter.offsetJsf;
                AddSpacer(storable.name, 10, true);

                var slider = tittyMagic.CreateSlider(storable, true);
                slider.valueFormat = _parameter.valueFormat;
                slider.label = "Offset";
                slider.slider.onValueChanged.AddListener(value =>
                {
                    parentButton.label = ParamButtonLabel();
                    if(_parameterGroup.requiresRecalibration)
                    {
                        tittyMagic.calibrationHelper.shouldRun = Math.Abs(value - _offsetWhenCalibrated) > 0.01f;
                    }
                });

                if(_parameterGroup.allowsSoftColliderVisualization)
                {
                    slider.AddPointerUpDownListener(
                        SoftPhysicsHandler.HidePreviewsOnPointerDown,
                        () => SoftPhysicsHandler.ShowPreviewsOnPointerDown()
                    );
                }

                elements[storable.name] = slider;
                _offsetWhenCalibrated = storable.val;
            }

            /* Value slider */
            {
                var storable = _parameter.valueJsf;
                var slider = tittyMagic.CreateSlider(storable, true);
                slider.valueFormat = _parameter.valueFormat;
                slider.label = "Value";
                slider.SetActiveStyle(false, true);
                slider.slider.onValueChanged.AddListener(_ => SyncAllMultiplierSliderValues());
                elements[storable.name] = slider;
            }

            /* Soft physics parameter group sections */
            CreateColliderGroupSection(SoftColliderGroup.MAIN, false);
            CreateColliderGroupSection(SoftColliderGroup.OUTER, true);
            CreateColliderGroupSection(SoftColliderGroup.AREOLA, false);
            CreateColliderGroupSection(SoftColliderGroup.NIPPLE, true);
        }

        protected override void OnClose()
        {
            if(_parameterGroup.allowsSoftColliderVisualization)
            {
                _visualizer.ShowPreviewsJSON.val = false;
                _visualizer.enabled = false;
            }

            if(tittyMagic.calibrationHelper.shouldRun)
            {
                tittyMagic.recalibratePhysics.actionCallback();
            }
        }

        private void CreateColliderGroupSection(string group, bool rightSide)
        {
            if(!_parameter.groupMultiplierParams.ContainsKey(group))
            {
                return;
            }

            var groupParam = _parameter.groupMultiplierParams[group];

            CreateHeaderTextField(new JSONStorableString($"{group}Header", group), rightSide);

            /* Multiplier offset slider */
            {
                var slider = tittyMagic.CreateSlider(groupParam.offsetJsf, rightSide);
                slider.valueFormat = "F2";
                slider.label = "Multiplier Offset";
                slider.slider.onValueChanged.AddListener(value => parentButton.label = ParamButtonLabel());
                elements[groupParam.offsetJsf.name] = slider;
            }

            /* Multiplier value slider */
            {
                var slider = tittyMagic.CreateSlider(groupParam.valueJsf, rightSide);
                slider.valueFormat = _parameter.valueFormat;
                slider.SetActiveStyle(false, true);
                var uiInputField = slider.sliderValueTextFromFloat.UIInputField;
                uiInputField.contentType = InputField.ContentType.Standard;
                slider.slider.onValueChanged.AddListener(value => SyncMultiplierSliderLabel(slider, value));
                SyncMultiplierSliderLabel(slider, groupParam.valueJsf.val);
                elements[groupParam.valueJsf.name] = slider;
            }

            if(_parameterGroup.allowsSoftColliderVisualization)
            {
                var slider = (UIDynamicSlider) elements[groupParam.offsetJsf.name];
                slider.AddPointerUpDownListener(
                    SoftPhysicsHandler.HidePreviewsOnPointerDown,
                    () => SoftPhysicsHandler.ShowPreviewsOnPointerDown(group)
                );
            }
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
