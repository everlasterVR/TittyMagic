// ReSharper disable MemberCanBePrivate.Global
using System;
using UnityEngine;
using static TittyMagic.UI.UIHelpers;

namespace TittyMagic.UI
{
    internal class MainWindow : IWindow
    {
        private readonly Script _script;

        public UIDynamicTextField titleTextField;
        public UIDynamic autoRefreshToggleSpacer;
        public UIDynamicToggle autoRefreshToggle;
        public UIDynamic calculateMassButtonSpacer;
        public UIDynamicButton calculateMassButton;
        public UIDynamic recalibrateButtonSpacer;
        public UIDynamicButton recalibrateButton;
        public UIDynamic massSliderSpacer;
        public UIDynamicSlider massSlider;
        public UIDynamic softnessSliderSpacer;
        public UIDynamicSlider softnessSlider;
        public UIDynamic quicknessSliderSpacer;
        public UIDynamicSlider quicknessSlider;

        public int Id() => 1;

        public MainWindow(Script script)
        {
            _script = script;
        }

        public void Rebuild()
        {
            CreateTitleTextField(_script.titleText, false);
            CreateAutoRefreshToggle(_script.autoRefresh, true, spacing: 35);
            CreateCalculateMassButton(true);
            CreateRecalibrateButton(true);
            CreateMassSlider(_script.mass, false);
            CreateSoftnessSlider(_script.softness, false);
            CreateQuicknessSlider(_script.quickness, true);
        }

        private void CreateTitleTextField(JSONStorableString storable, bool rightSide)
        {
            titleTextField = _script.CreateTextField(storable, rightSide);
            titleTextField.UItext.fontSize = 46;
            titleTextField.height = 100;
            titleTextField.backgroundColor = Color.clear;
            titleTextField.textColor = funkyCyan;
        }

        private void CreateInfoTextArea(JSONStorableString storable, bool rightSide, float spacing = 0)
        {
            _script.NewSpacer(spacing, rightSide);
            var textField = _script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.height = 390;
        }

        private void CreateSmallInfoTextArea(JSONStorableString storable, bool rightSide, float spacing = 0)
        {
            _script.NewSpacer(spacing, rightSide);
            var textField = _script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.height = 115;
        }

        private void CreateAutoRefreshToggle(JSONStorableBool storable, bool rightSide, float spacing = 0)
        {
            autoRefreshToggleSpacer = _script.NewSpacer(spacing, rightSide);
            autoRefreshToggle = _script.CreateToggle(storable, rightSide);
        }

        private void CreateCalculateMassButton(bool rightSide, float spacing = 0)
        {
            calculateMassButtonSpacer = _script.NewSpacer(spacing, rightSide);
            calculateMassButton = _script.CreateButton("Calculate breast mass", rightSide);
            calculateMassButton.height = 52;
        }

        private void CreateRecalibrateButton(bool rightSide, float spacing = 0)
        {
            recalibrateButtonSpacer = _script.NewSpacer(spacing, rightSide);
            recalibrateButton = _script.CreateButton("Recalibrate physics", rightSide);
            recalibrateButton.height = 52;
        }

        private void CreateMassSlider(JSONStorableFloat storable, bool rightSide, float spacing = 0)
        {
            massSliderSpacer = _script.NewSpacer(spacing, rightSide);
            massSlider = _script.CreateSlider(storable, rightSide);
            massSlider.valueFormat = "F3";
            massSlider.AddSliderClickMonitor();
        }

        private void CreateSoftnessSlider(JSONStorableFloat storable, bool rightSide, float spacing = 0)
        {
            softnessSliderSpacer = _script.NewSpacer(spacing, rightSide);
            softnessSlider = _script.CreateSlider(storable, rightSide);
            softnessSlider.valueFormat = "0f";
            softnessSlider.slider.wholeNumbers = true;
            softnessSlider.AddSliderClickMonitor();
        }

        private void CreateQuicknessSlider(JSONStorableFloat storable, bool rightSide, float spacing = 0)
        {
            quicknessSliderSpacer = _script.NewSpacer(spacing, rightSide);
            quicknessSlider = _script.CreateSlider(storable, rightSide);
            quicknessSlider.valueFormat = "0f";
            quicknessSlider.slider.wholeNumbers = true;
            quicknessSlider.AddSliderClickMonitor();
        }

        public void Clear()
        {
            _script.RemoveTextField(titleTextField);
            _script.RemoveSpacer(autoRefreshToggleSpacer);
            _script.RemoveToggle(autoRefreshToggle);
            _script.RemoveSpacer(calculateMassButtonSpacer);
            _script.RemoveButton(calculateMassButton);
            _script.RemoveSpacer(recalibrateButtonSpacer);
            _script.RemoveButton(recalibrateButton);
            _script.RemoveSpacer(massSliderSpacer);
            _script.RemoveSlider(massSlider);
            _script.RemoveSpacer(softnessSliderSpacer);
            _script.RemoveSlider(softnessSlider);
            _script.RemoveSpacer(quicknessSliderSpacer);
            _script.RemoveSlider(quicknessSlider);
        }
    }
}
