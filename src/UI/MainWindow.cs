using System.Collections.Generic;
using System.Linq;
using System.Text;
using TittyMagic.Handlers;
using UnityEngine;
using static TittyMagic.Script;

namespace TittyMagic.UI
{
    internal class MainWindow : WindowBase
    {
        private readonly JSONStorableString _title;
        private readonly PhysicsParameter _massParameter;

        public MainWindow()
        {
            buildAction = () =>
            {
                CreateTitleTextField(false);
                CreateSoftnessInfoTextField(false, spacing: 35);
                CreateQuicknessInfoTextField(false);
                CreateBreastMassInfoTextField(false, spacing: 10);

                CreateRecalibrateButton(tittyMagic.recalibratePhysics, true);
                CreateSoftnessSlider(true, spacing: 65);
                CreateQuicknessSlider(true);
                CreateRecalibrateButton(tittyMagic.calculateBreastMass, true, spacing: 15);
                CreateAutoUpdateMassToggle(true);
                CreateMassOffsetSlider(true);
                CreateMassSlider(true);

                if(Gender.isFemale)
                {
                    CreateHardCollidersInfoTextField(false, spacing: 10);
                    CreateConfigureHardCollidersButton(true, spacing: 15);
                }
            };

            _title = new JSONStorableString("title", "");
            _massParameter = MainPhysicsHandler.massParameterGroup.left;

            if(Gender.isFemale)
            {
                nestedWindows.Add(new HardCollidersWindow(
                    () =>
                    {
                        activeNestedWindow = null;
                        buildAction();
                    }
                ));
            }
        }

        private void CreateTitleTextField(bool rightSide)
        {
            var textField = UIHelpers.TitleTextField(
                _title,
                $"{"\n".Size(12)}{nameof(TittyMagic)}    v{VERSION}",
                100,
                rightSide
            );
            textField.UItext.fontSize = 40;
            elements[_title.name] = textField;
        }

        private void CreateBreastMassInfoTextField(bool rightSide, int spacing = 0)
        {
            var sb = new StringBuilder();
            sb.Append("<b><i>Breast mass</i></b> is estimated from volume. Since it represents size, other physics");
            sb.Append(" parameters are adjusted based on its value. Calculating mass also recalibrates physics.");
            sb.Append("\n\n");
            sb.Append("<b><i>Auto-update mass</i></b> enables calculating mass automatically when changes in breast");
            sb.Append(" morphs are detected. Disabling it prevents repeated recalibration when using other plugins");
            sb.Append(" that animate morphs.");
            var storable = new JSONStorableString("breastMassInfoText", sb.ToString());
            AddSpacer(storable.name, spacing, rightSide);

            var textField = tittyMagic.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.backgroundColor = Color.clear;
            textField.height = 400;
            elements[storable.name] = textField;
        }

        private void CreateAutoUpdateMassToggle(bool rightSide, int spacing = 0)
        {
            var storable = tittyMagic.autoUpdateJsb;
            AddSpacer(storable.name, spacing, rightSide);

            var toggle = tittyMagic.CreateToggle(storable, rightSide);
            toggle.height = 52;
            toggle.label = "Auto-Update Mass";
            elements[storable.name] = toggle;
        }

        private void CreateMassOffsetSlider(bool rightSide, int spacing = 0)
        {
            var storable = _massParameter.offsetJsf;
            AddSpacer(storable.name, spacing, rightSide);

            var slider = tittyMagic.CreateSlider(storable, rightSide);
            slider.valueFormat = _massParameter.valueFormat;
            slider.label = "Breast Mass Offset";

            slider.AddListener((float _) => tittyMagic.StartCalibration(calibratesMass: true, waitsForListeners: true));
            slider.AddPointerUpDownListener();

            elements[storable.name] = slider;
        }

        private void CreateMassSlider(bool rightSide, int spacing = 0)
        {
            var storable = _massParameter.valueJsf;
            AddSpacer(storable.name, spacing, rightSide);

            var slider = tittyMagic.CreateSlider(storable, rightSide);
            slider.valueFormat = _massParameter.valueFormat;
            slider.SetActiveStyle(false);
            slider.slider.interactable = false;
            slider.quickButtonsEnabled = false;
            slider.defaultButtonEnabled = false;
            slider.label = "Breast Mass";
            elements[storable.name] = slider;
        }

        private void CreateSoftnessInfoTextField(bool rightSide, int spacing = 0)
        {
            var sb = new StringBuilder();
            sb.Append("<b><i>Breast softness</i></b> simulates implants at low values");
            sb.Append(" and natural breasts at high values.");
            var storable = new JSONStorableString("softnessInfoText", sb.ToString());
            AddSpacer(storable.name, spacing, rightSide);

            var textField = tittyMagic.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.backgroundColor = Color.clear;
            textField.height = 120;
            elements[storable.name] = textField;
        }

        private void CreateQuicknessInfoTextField(bool rightSide, int spacing = 0)
        {
            var sb = new StringBuilder();
            sb.Append("<b><i>Breast quickness</i></b> causes slow motion at low values");
            sb.Append(" and realistically responsive behavior at high values.");
            var storable = new JSONStorableString("quicknessInfoText", sb.ToString());
            AddSpacer(storable.name, spacing, rightSide);

            var textField = tittyMagic.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.backgroundColor = Color.clear;
            textField.height = 120;
            elements[storable.name] = textField;
        }

        private void CreateSoftnessSlider(bool rightSide, int spacing = 0)
        {
            var storable = tittyMagic.softnessJsf;
            AddSpacer(storable.name, spacing, rightSide);

            var slider = tittyMagic.CreateSlider(storable, rightSide);
            slider.valueFormat = "F0";
            slider.slider.wholeNumbers = true;
            slider.label = "Breast Softness";
            slider.AddPointerUpDownListener();
            elements[storable.name] = slider;
        }

        private void CreateQuicknessSlider(bool rightSide, int spacing = 0)
        {
            var storable = tittyMagic.quicknessJsf;
            AddSpacer(storable.name, spacing, rightSide);

            var slider = tittyMagic.CreateSlider(storable, rightSide);
            slider.valueFormat = "F0";
            slider.slider.wholeNumbers = true;
            slider.label = "Breast Quickness";
            slider.AddPointerUpDownListener();
            elements[storable.name] = slider;
        }

        private void CreateHardCollidersInfoTextField(bool rightSide, int spacing = 0)
        {
            var sb = new StringBuilder();
            sb.Append("<b><i>Hard colliders</i></b> make breasts both easier to move");
            sb.Append(" when touched and better at maintaining their volume and shape.");
            var storable = new JSONStorableString("hardCollidersInfoText", sb.ToString());
            AddSpacer(storable.name, spacing, rightSide);

            var textField = tittyMagic.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.backgroundColor = Color.clear;
            textField.height = 160;
            elements[storable.name] = textField;
        }

        private void CreateConfigureHardCollidersButton(bool rightSide, int spacing = 0)
        {
            var storable = tittyMagic.configureHardColliders;
            AddSpacer(storable.name, spacing, rightSide);

            var button = tittyMagic.CreateButton(storable.name, rightSide);
            storable.RegisterButton(button);
            button.buttonText.alignment = TextAnchor.MiddleLeft;
            button.label = "  Configure Hard Colliders...";
            button.height = 52;

            storable.actionCallback = () =>
            {
                ClearSelf();
                activeNestedWindow = nestedWindows.First();
                activeNestedWindow.Rebuild();
            };

            elements[storable.name] = button;
        }

        public List<UIDynamicSlider> GetSlidersForRefresh()
        {
            var sliders = new List<UIDynamicSlider>();
            if(elements.Any())
            {
                sliders.Add(elements[_massParameter.offsetJsf.name] as UIDynamicSlider);
                sliders.Add(elements[tittyMagic.softnessJsf.name] as UIDynamicSlider);
                sliders.Add(elements[tittyMagic.quicknessJsf.name] as UIDynamicSlider);
            }

            return sliders;
        }
    }
}
