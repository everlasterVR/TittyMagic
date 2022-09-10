using System.Collections.Generic;
using System.Linq;
using System.Text;
using TittyMagic.Handlers;
using TittyMagic.Models;
using UnityEngine;
using static TittyMagic.Script;

namespace TittyMagic.UI
{
    public class MainWindow : WindowBase
    {
        private readonly JSONStorableString _title;
        private readonly PhysicsParameter _massParameter;
        public JSONStorableAction configureHardColliders { get; }
        private JSONStorableAction configureColliderFriction { get; }
        public JSONStorableAction openDevWindow { get; }

        public MainWindow()
        {
            buildAction = () =>
            {
                CreateTitleTextField(false);
                CreateInfoTextField(false, spacing: 35);

                CreateRecalibrateButton(tittyMagic.recalibratePhysics, true);
                CreateSoftnessSlider(true, spacing: 65);
                CreateQuicknessSlider(true);
                CreateRecalibrateButton(tittyMagic.calculateBreastMass, true, spacing: 15);
                CreateAutoUpdateMassToggle(true);
                CreateMassOffsetSlider(true);
                CreateMassSlider(true);

                if(personIsFemale)
                {
                    CreateConfigureHardCollidersButton(true, spacing: 15);
                    CreateConfigureColliderFrictionButton(true, spacing: 15);
                }
            };

            _title = new JSONStorableString("title", "");
            _massParameter = MainPhysicsHandler.massParameterGroup.left;

            configureHardColliders = new JSONStorableAction(
                "configureHardColliders",
                () =>
                {
                    if(!tittyMagic.enabled)
                    {
                        Utils.LogMessage("Enable the plugin to configure hard colliders.");
                        return;
                    }

                    ClearSelf();
                    activeNestedWindow = nestedWindows.Find(window => window.GetId() == "hardCollidersWindow");
                    activeNestedWindow.Rebuild();
                }
            );

            configureColliderFriction = new JSONStorableAction(
                "configureColliderFriction",
                () =>
                {
                    if(!tittyMagic.enabled)
                    {
                        Utils.LogMessage("Enable the plugin to configure collider friction.");
                        return;
                    }

                    tittyMagic.bindings.actions["OpenUI_ConfigureColliderFriction"].actionCallback();
                }
            );

            if(envIsDevelopment)
            {
                openDevWindow = new JSONStorableAction(
                    "openDevWindow",
                    () =>
                    {
                        ClearSelf();
                        activeNestedWindow = nestedWindows.Find(window => window.GetId() == "devWindow");
                        activeNestedWindow.Rebuild();
                    }
                );
                nestedWindows.Add(new DevWindow("devWindow", onReturnToParent));
            }

            if(personIsFemale)
            {
                nestedWindows.Add(new HardCollidersWindow("hardCollidersWindow", onReturnToParent));
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

        private void CreateInfoTextField(bool rightSide, int spacing = 0)
        {
            var sb = new StringBuilder();
            sb.Append("<b><i>Breast softness</i></b> simulates implants at low values");
            sb.Append(" and natural breasts at high values.");
            sb.Append("\n\n");
            sb.Append("<b><i>Breast quickness</i></b> causes slow motion at low values");
            sb.Append(" and realistically responsive behavior at high values.");
            sb.Append("\n\n");
            sb.Append("<b><i>Breast mass</i></b> is estimated from volume. Since it represents size, other physics");
            sb.Append(" parameters are adjusted based on its value. Calculating mass also recalibrates physics.");
            sb.Append("\n\n");
            sb.Append("<b><i>Auto-update mass</i></b> enables calculating mass automatically when changes in breast");
            sb.Append(" morphs are detected. Disabling it prevents repeated recalibration when using other plugins");
            sb.Append(" that animate morphs.");
            if(personIsFemale)
            {
                sb.Append("\n\n");
                sb.Append("<b><i>Hard colliders</i></b> make breasts both easier to move");
                sb.Append(" when touched and better at maintaining their volume and shape.");
                sb.Append("\n\n");
                sb.Append("<b><i>Collider friction</i></b> controls how easily colliders");
                sb.Append(" stick to or slip away from hands or other colliding objects.");
            }

            var storable = new JSONStorableString("infoText", sb.ToString());
            AddSpacer(storable.name, spacing, rightSide);

            var textField = tittyMagic.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.backgroundColor = Color.clear;
            textField.height = 965;
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
            slider.SetActiveStyle(false, true);
            slider.label = "Breast Mass";
            elements[storable.name] = slider;
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

        private void CreateConfigureHardCollidersButton(bool rightSide, int spacing = 0)
        {
            var storable = configureHardColliders;
            AddSpacer(storable.name, spacing, rightSide);

            var button = tittyMagic.CreateButton(storable.name, rightSide);
            storable.RegisterButton(button);
            button.buttonText.alignment = TextAnchor.MiddleLeft;
            button.label = "  Configure Hard Colliders...";
            button.height = 52;

            elements[storable.name] = button;
        }

        private void CreateConfigureColliderFrictionButton(bool rightSide, int spacing = 0)
        {
            var storable = configureColliderFriction;
            AddSpacer(storable.name, spacing, rightSide);

            var button = tittyMagic.CreateButton(storable.name, rightSide);
            storable.RegisterButton(button);
            button.buttonText.alignment = TextAnchor.MiddleLeft;
            button.label = "  Configure Collider Friction...";
            button.height = 52;

            elements[storable.name] = button;
        }

        public IEnumerable<UIDynamicSlider> GetSlidersForRefresh()
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
