using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TittyMagic.UI
{
    internal class MainWindow : WindowBase
    {
        private readonly JSONStorableString _title;

        public MainWindow(Script script) : base(script)
        {
            buildAction = BuildSelf;
            _title = new JSONStorableString("title", "");
            if(Gender.isFemale)
            {
                nestedWindows.Add(new HardCollidersWindow(
                    script,
                    () =>
                    {
                        activeNestedWindow = null;
                        buildAction();
                    }
                ));
            }
        }

        private void BuildSelf()
        {
            CreateTitleTextField(false);
            CreateRecalibratingTextField(true);

            CreateBreastMassInfoTextField(false);
            CreateCalculateMassButton(true);
            CreateAutoUpdateMassToggle(true);
            CreateMassSlider(true);

            CreateSoftPhysicsInfoTextField(false);
            if(Gender.isFemale)
            {
                CreateSoftPhysicsOnToggle(true, spacing: 15);
                CreateSoftnessSlider(true);
            }
            else
            {
                CreateSoftnessSlider(true, spacing: 15);
            }

            CreateQuicknessSlider(true);

            CreateHardCollidersInfoTextField(false);
            if(Gender.isFemale)
            {
                CreateConfigureHardCollidersButton(true, spacing: 15);
            }

            elements[script.autoUpdateJsb.name].AddListener(value =>
                elements[script.mainPhysicsHandler.massJsf.name].SetActiveStyle(!value, true)
            );
            elements[script.mainPhysicsHandler.massJsf.name].SetActiveStyle(!script.autoUpdateJsb.val, true);
        }

        private void CreateTitleTextField(bool rightSide)
        {
            var textField = UIHelpers.TitleTextField(
                script,
                _title,
                $"{"\n".Size(12)}{nameof(TittyMagic)}    {Script.VERSION}",
                100,
                rightSide
            );
            textField.UItext.fontSize = 40;
            elements[_title.name] = textField;
        }

        private void CreateRecalibratingTextField(bool rightSide, int spacing = 0)
        {
            var storable = script.statusInfo;
            AddSpacer(storable.name, spacing, rightSide);
            elements[storable.name] = UIHelpers.NotificationTextField(script, storable, 100, rightSide);
        }

        private void CreateBreastMassInfoTextField(bool rightSide, int spacing = 0)
        {
            var sb = new StringBuilder();
            sb.Append("<b><i>Breast mass</i></b> is based on breast volume");
            sb.Append(", and determines the values of other physics parameters to best fit the estimated volume.");
            sb.Append("\n\n");
            sb.Append("<b><i>Auto-Update Mass</i></b> can be disabled to control mass manually");
            sb.Append(", and to prevent unwanted freezing when using other plugins that animate morphs.");
            var storable = new JSONStorableString("breastMassInfoText", sb.ToString());
            AddSpacer(storable.name, spacing, rightSide);

            var textField = script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.backgroundColor = Color.clear;
            textField.height = 355;
            elements[storable.name] = textField;
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

        private void CreateAutoUpdateMassToggle(bool rightSide, int spacing = 0)
        {
            var storable = script.autoUpdateJsb;
            AddSpacer(storable.name, spacing, rightSide);

            var toggle = script.CreateToggle(storable, rightSide);
            toggle.height = 52;
            toggle.label = "Auto-Update Mass";
            elements[storable.name] = toggle;
        }

        private void CreateMassSlider(bool rightSide, int spacing = 0)
        {
            var storable = script.mainPhysicsHandler.massJsf;
            AddSpacer(storable.name, spacing, rightSide);

            var slider = script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F3";
            slider.label = "Breast Mass";
            slider.AddSliderClickMonitor();
            elements[storable.name] = slider;
        }

        private void CreateSoftPhysicsInfoTextField(bool rightSide, int spacing = 0)
        {
            var sb = new StringBuilder();
            sb.Append("<b><i>Breast softness</i></b> simulates implants at low values");
            sb.Append(" and natural breasts at high values.");
            sb.Append("\n\n");
            sb.Append("<b><i>Breast quickness</i></b> offsets physics settings");
            sb.Append(", causing slow motion at low values, and more normal behavior at high values.");
            var storable = new JSONStorableString("softPhysicsInfoText", sb.ToString());
            AddSpacer(storable.name, spacing, rightSide);

            var textField = script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.backgroundColor = Color.clear;
            textField.height = 285;
            elements[storable.name] = textField;
        }

        private void CreateSoftPhysicsOnToggle(bool rightSide, int spacing = 0)
        {
            var storable = script.softPhysicsHandler.softPhysicsOn;
            AddSpacer(storable.name, spacing, rightSide);

            var toggle = script.CreateToggle(storable, rightSide);
            toggle.height = 52;
            toggle.label = "Soft Physics Enabled";
            elements[storable.name] = toggle;
        }

        private void CreateSoftnessSlider(bool rightSide, int spacing = 0)
        {
            var storable = script.softnessJsf;
            AddSpacer(storable.name, spacing, rightSide);

            var slider = script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F0";
            slider.slider.wholeNumbers = true;
            slider.label = "Breast Softness";
            slider.AddSliderClickMonitor();
            elements[storable.name] = slider;
        }

        private void CreateQuicknessSlider(bool rightSide, int spacing = 0)
        {
            var storable = script.quicknessJsf;
            AddSpacer(storable.name, spacing, rightSide);

            var slider = script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F0";
            slider.slider.wholeNumbers = true;
            slider.label = "Breast Quickness";
            slider.AddSliderClickMonitor();
            elements[storable.name] = slider;
        }

        private void CreateHardCollidersInfoTextField(bool rightSide, int spacing = 0)
        {
            var sb = new StringBuilder();
            sb.Append("<b><i>Hard colliders</i></b> cause breasts to move when touched, and help them maintain their volume and shape.");
            var storable = new JSONStorableString("hardCollidersInfoText", sb.ToString());
            AddSpacer(storable.name, spacing, rightSide);

            var textField = script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.backgroundColor = Color.clear;
            textField.height = 250;
            elements[storable.name] = textField;
        }

        private void CreateConfigureHardCollidersButton(bool rightSide, int spacing = 0)
        {
            var storable = script.configureHardColliders;
            AddSpacer(storable.name, spacing, rightSide);

            var button = script.CreateButton(storable.name, rightSide);
            storable.RegisterButton(button);
            button.buttonText.alignment = TextAnchor.MiddleLeft;
            button.label = "  Configure Hard Colliders...";
            button.height = 52;

            button.AddListener(() =>
            {
                ClearSelf();
                activeNestedWindow = nestedWindows.First();
                activeNestedWindow.Rebuild();
            });

            elements[storable.name] = button;
        }

        public IEnumerable<UIDynamicSlider> GetSlidersForRefresh()
        {
            var sliders = new List<UIDynamicSlider>();
            if(elements.Any())
            {
                sliders.Add(elements[script.mainPhysicsHandler.massJsf.name] as UIDynamicSlider);
                sliders.Add(elements[script.softnessJsf.name] as UIDynamicSlider);
                sliders.Add(elements[script.quicknessJsf.name] as UIDynamicSlider);
            }

            return sliders;
        }

        public void UpdateCollidersDebugInfo(string configId)
        {
            if(activeNestedWindow != null)
            {
                ((HardCollidersWindow) activeNestedWindow).UpdateCollidersDebugInfo(configId);
            }
        }
    }
}
