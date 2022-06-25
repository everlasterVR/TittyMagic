// ReSharper disable MemberCanBePrivate.Global
using System;
using UnityEngine;
using static TittyMagic.UI.UIHelpers;

namespace TittyMagic.UI
{
    internal class MorphingWindow : IWindow
    {
        private readonly Script _script;

        public UIDynamic dynamicMorphingTitleTextFieldSpacer;
        public UIDynamicTextField dynamicMorphingTitleTextField;
        public UIDynamic morphingYSliderSpacer;
        public UIDynamicSlider morphingYSlider;
        public UIDynamic morphingXSliderSpacer;
        public UIDynamicSlider morphingXSlider;
        public UIDynamic morphingZSliderSpacer;
        public UIDynamicSlider morphingZSlider;
        public UIDynamic dynamicMorphingInfoTextFieldSpacer;
        public UIDynamicTextField dynamicMorphingInfoTextField;

        public UIDynamic additionalSettingsTitleTextFieldSpacer;
        public UIDynamicTextField additionalSettingsTitleTextField;
        public UIDynamic offsetMorphingSliderSpacer;
        public UIDynamicSlider offsetMorphingSlider;
        public UIDynamic nippleErectionSliderSpacer;
        public UIDynamicSlider nippleErectionSlider;
        public UIDynamic additionalSettingsInfoTextFieldSpacer;
        public UIDynamicTextField additionalSettingsInfoTextField;

        public int Id() => 2;

        public MorphingWindow(Script script)
        {
            _script = script;
        }


        public void Rebuild()
        {
            CreateDynamicMorphingTitle(_script.morphingTitleText, false);
            CreateMorphingYSlider(_script.morphingYStorable, false);
            CreateMorphingXSlider(_script.morphingXStorable, false);
            CreateMorphingZSlider(_script.morphingZStorable, false);
            CreateDynamicMorphingInfoTextArea(_script.morphingInfoText, true, spacing: 100);
            CreateAdditionalSettingsTitle(_script.additionalSettingsTitleText, false);
            CreateOffsetMorphingSlider(_script.offsetMorphing, false);
            CreateNippleErectionSlider(_script.nippleErection, false);
            CreateAdditionalSettingsInfoTextArea(_script.additionalSettingsInfoText, true, spacing: 100);
        }

        private void CreateDynamicMorphingTitle(JSONStorableString storable, bool rightSide, float spacing = 0)
        {
            dynamicMorphingTitleTextFieldSpacer = _script.NewSpacer(spacing, rightSide);
            dynamicMorphingTitleTextField = _script.CreateTextField(storable, rightSide);
            dynamicMorphingTitleTextField.UItext.fontSize = 32;
            dynamicMorphingTitleTextField.height = 100;
            dynamicMorphingTitleTextField.backgroundColor = Color.clear;
        }

        private void CreateAdditionalSettingsTitle(JSONStorableString storable, bool rightSide, float spacing = 0)
        {
            additionalSettingsTitleTextFieldSpacer = _script.NewSpacer(spacing, rightSide);
            additionalSettingsTitleTextField = _script.CreateTextField(storable, rightSide);
            additionalSettingsTitleTextField.UItext.fontSize = 32;
            additionalSettingsTitleTextField.height = 100;
            additionalSettingsTitleTextField.backgroundColor = Color.clear;
        }

        private void CreateDynamicMorphingInfoTextArea(JSONStorableString storable, bool rightSide, float spacing = 0)
        {
            dynamicMorphingInfoTextFieldSpacer = _script.NewSpacer(spacing, rightSide);
            dynamicMorphingInfoTextField = _script.CreateTextField(storable, rightSide);
            dynamicMorphingInfoTextField.UItext.fontSize = 28;
            dynamicMorphingInfoTextField.height = 390;
        }

        private void CreateAdditionalSettingsInfoTextArea(JSONStorableString storable, bool rightSide, float spacing = 0)
        {
            additionalSettingsInfoTextFieldSpacer = _script.NewSpacer(spacing, rightSide);
            additionalSettingsInfoTextField = _script.CreateTextField(storable, rightSide);
            additionalSettingsInfoTextField.UItext.fontSize = 28;
            additionalSettingsInfoTextField.height = 115;
        }

        private void CreateMorphingYSlider(JSONStorableFloat storable, bool rightSide, float spacing = 0)
        {
            morphingYSliderSpacer =_script.NewSpacer(spacing, rightSide);
            morphingYSlider = _script.CreateSlider(storable, rightSide);
            morphingYSlider.valueFormat = "F2";
        }

        private void CreateMorphingXSlider(JSONStorableFloat storable, bool rightSide, float spacing = 0)
        {
            morphingXSliderSpacer =_script.NewSpacer(spacing, rightSide);
            morphingXSlider = _script.CreateSlider(storable, rightSide);
            morphingXSlider.valueFormat = "F2";
        }

        private void CreateMorphingZSlider(JSONStorableFloat storable, bool rightSide, float spacing = 0)
        {
            morphingZSliderSpacer = _script.NewSpacer(spacing, rightSide);
            morphingZSlider = _script.CreateSlider(storable, rightSide);
            morphingZSlider.valueFormat = "F2";
        }

        private void CreateOffsetMorphingSlider(JSONStorableFloat storable, bool rightSide, float spacing = 0)
        {
            offsetMorphingSliderSpacer =_script.NewSpacer(spacing, rightSide);
            offsetMorphingSlider = _script.CreateSlider(storable, rightSide);
            offsetMorphingSlider.valueFormat = "F2";
            offsetMorphingSlider.AddSliderClickMonitor();
        }

        private void CreateNippleErectionSlider(JSONStorableFloat storable, bool rightSide, float spacing = 0)
        {
            nippleErectionSliderSpacer =_script.NewSpacer(spacing, rightSide);
            nippleErectionSlider = _script.CreateSlider(storable, rightSide);
            nippleErectionSlider.valueFormat = "F2";
        }

        public void Clear()
        {
            _script.RemoveSpacer(dynamicMorphingTitleTextFieldSpacer);
            _script.RemoveTextField(dynamicMorphingTitleTextField);
            _script.RemoveSpacer(morphingYSliderSpacer);
            _script.RemoveSlider(morphingYSlider);
            _script.RemoveSpacer(morphingXSliderSpacer);
            _script.RemoveSlider(morphingXSlider);
            _script.RemoveSpacer(morphingZSliderSpacer);
            _script.RemoveSlider(morphingZSlider);
            _script.RemoveSpacer(dynamicMorphingInfoTextFieldSpacer);
            _script.RemoveTextField(dynamicMorphingInfoTextField);
            _script.RemoveSpacer(additionalSettingsTitleTextFieldSpacer);
            _script.RemoveTextField(additionalSettingsTitleTextField);
            _script.RemoveSpacer(offsetMorphingSliderSpacer);
            _script.RemoveSlider(offsetMorphingSlider);
            _script.RemoveSpacer(nippleErectionSliderSpacer);
            _script.RemoveSlider(nippleErectionSlider);
            _script.RemoveSpacer(additionalSettingsInfoTextFieldSpacer);
            _script.RemoveTextField(additionalSettingsInfoTextField);
        }
    }
}
