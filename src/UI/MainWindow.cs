using System.Collections.Generic;
using System.Linq;
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
            CreateCalculateMassButton(true);
            CreateAutoUpdateMassToggle(true);
            CreateMassSlider(true);

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

        private void CreateAutoUpdateMassToggle(bool rightSide, int spacing = 0)
        {
            var storable = script.autoUpdateJsb;
            AddSpacer(storable.name, spacing, rightSide);

            var toggle = script.CreateToggle(storable, rightSide);
            toggle.height = 52;
            toggle.label = "Auto-Update Mass";
            elements[storable.name] = toggle;
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

        private void CreateRecalibratingTextField(bool rightSide, int spacing = 0)
        {
            var storable = script.statusInfo;
            AddSpacer(storable.name, spacing, rightSide);
            elements[storable.name] = UIHelpers.NotificationTextField(script, storable, 100, rightSide);
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
    }
}
