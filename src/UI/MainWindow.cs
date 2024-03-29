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
        private readonly PhysicsParameter _massParameter;
        public JSONStorableAction openOptionsWindowAction { get; }
        public JSONStorableAction configureHardCollidersAction { get; }
        private JSONStorableAction configureColliderFrictionAction { get; }
        public JSONStorableAction openExperimentalWindowAction { get; }
        public JSONStorableAction openDevWindowAction { get; }
        public JSONStorableAction openDevMorphWindowAction { get; }

        public MainWindow()
        {
            _massParameter = MainPhysicsHandler.massParameterGroup.left;

            openOptionsWindowAction = new JSONStorableAction(
                "openOptions",
                () =>
                {
                    ClearSelf();
                    activeNestedWindow = nestedWindows.Find(window => window.GetId() == nameof(OptionsWindow));
                    activeNestedWindow.Rebuild();
                }
            );

            configureHardCollidersAction = new JSONStorableAction(
                "configureHardColliders",
                () =>
                {
                    if(!tittyMagic.enabled)
                    {
                        Utils.LogMessage("Enable the plugin to configure hard colliders.");
                        return;
                    }

                    ClearSelf();
                    activeNestedWindow = nestedWindows.Find(window => window.GetId() == nameof(HardCollidersWindow));
                    activeNestedWindow.Rebuild();
                }
            );

            configureColliderFrictionAction = new JSONStorableAction(
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
                openDevWindowAction = new JSONStorableAction(
                    "openDevWindow",
                    () =>
                    {
                        ClearSelf();
                        activeNestedWindow = nestedWindows.Find(window => window.GetId() == nameof(DevWindow));
                        activeNestedWindow.Rebuild();
                    }
                );

                openDevMorphWindowAction = new JSONStorableAction(
                    "openDevMorphWindow",
                    () =>
                    {
                        ClearSelf();
                        activeNestedWindow = nestedWindows.Find(window => window.GetId() == nameof(DevMorphWindow));
                        activeNestedWindow.Rebuild();
                    }
                );

                openExperimentalWindowAction = new JSONStorableAction(
                    "openExperimentalWindow",
                    () =>
                    {
                        ClearSelf();
                        activeNestedWindow = nestedWindows.Find(window => window.GetId() == nameof(ExperimentalWindow));
                        activeNestedWindow.Rebuild();
                    }
                );

                nestedWindows.Add(new DevWindow(nameof(DevWindow), OnReturn));
                nestedWindows.Add(new DevMorphWindow(nameof(DevMorphWindow), OnReturn));
                nestedWindows.Add(new ExperimentalWindow(nameof(ExperimentalWindow), OnReturn));
            }

            nestedWindows.Add(new OptionsWindow(nameof(OptionsWindow), OnReturn));

            if(personIsFemale)
            {
                nestedWindows.Add(new HardCollidersWindow(nameof(HardCollidersWindow), OnReturn));
            }
        }

        protected override void OnBuild()
        {
            CreateTitleTextField(
                new JSONStorableString("title", "\n".Size(24) + $"{nameof(TittyMagic)}    v{VERSION}".Bold()),
                fontSize: 40,
                height: 100,
                rightSide: false
            );

            /* Info text */
            {
                var sb = new StringBuilder();
                sb.Append("<b><i>Breast softness</i></b> simulates implants at low values");
                sb.Append(" and natural breasts at high values.");
                sb.Append("\n\n");
                sb.Append("<b><i>Breast quickness</i></b> causes slow motion at low values");
                sb.Append(" and realistically responsive behavior at high values.");
                sb.Append("\n\n");
                sb.Append("<b><i>Breast mass</i></b> is estimated from volume. Since it represents size, other physics");
                sb.Append(" parameters are adjusted based on its value. Calculating mass also calibrates the plugin.");
                sb.Append("\n\n");
                sb.Append("<b><i>Auto-update mass</i></b> enables calculating mass automatically when changes in breast");
                sb.Append(" morphs are detected. Disabling it prevents repeated calibrations due to animation of");
                sb.Append(" non-pose morphs (e.g. by other plugins).");
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
                AddSpacer(storable.name, 35);
                var textField = tittyMagic.CreateTextField(storable);
                textField.UItext.fontSize = 28;
                textField.backgroundColor = Color.clear;
                textField.height = 965;
                elements[storable.name] = textField;
            }

            CreateCalibrateButton(tittyMagic.calibrate, true);

            /* Open options window */
            {
                var storable = openOptionsWindowAction;
                var button = tittyMagic.CreateButton(storable.name, true);
                storable.RegisterButton(button);
                button.buttonText.alignment = TextAnchor.MiddleLeft;
                button.label = "  Show Calibration Options...";
                button.height = 52;
                elements[storable.name] = button;
            }

            /* Softness slider */
            {
                var storable = tittyMagic.softnessJsf;
                AddSpacer(storable.name, 15, true);
                var slider = tittyMagic.CreateSlider(storable, true);
                slider.valueFormat = "F0";
                slider.label = "Breast Softness";
                slider.AddPointerUpDownListener();
                elements[storable.name] = slider;
            }

            /* Quickness slider */
            {
                var storable = tittyMagic.quicknessJsf;
                var slider = tittyMagic.CreateSlider(storable, true);
                slider.valueFormat = "F0";
                slider.label = "Breast Quickness";
                slider.AddPointerUpDownListener();
                elements[storable.name] = slider;
            }

            /* Calculate breast mass button */
            CreateCalibrateButton(tittyMagic.calculateBreastMass, true, spacing: 15);

            /* Auto update toggle */
            {
                var storable = tittyMagic.calibrationHelper.autoUpdateJsb;
                var toggle = tittyMagic.CreateToggle(storable, true);
                toggle.height = 52;
                toggle.label = "Auto-Update Mass";
                elements[storable.name] = toggle;
            }

            /* Mass offset slider */
            {
                var storable = _massParameter.offsetJsf;
                var slider = tittyMagic.CreateSlider(storable, true);
                slider.valueFormat = _massParameter.valueFormat;
                slider.label = "Breast Mass Offset";
                slider.AddListener((float _) => tittyMagic.StartCalibration(calibratesMass: true, waitsForListeners: true));
                slider.AddPointerUpDownListener();
                elements[storable.name] = slider;
            }

            /* Mass value slider */
            {
                var storable = _massParameter.valueJsf;
                var slider = tittyMagic.CreateSlider(storable, true);
                slider.valueFormat = _massParameter.valueFormat;
                slider.SetActiveStyle(false, true);
                slider.label = "Breast Mass";
                elements[storable.name] = slider;
            }

            /* Configure hard colliders button */
            if(personIsFemale)
            {
                var storable = configureHardCollidersAction;
                AddSpacer(storable.name, 15, true);
                var button = tittyMagic.CreateButton(storable.name, true);
                storable.RegisterButton(button);
                button.buttonText.alignment = TextAnchor.MiddleLeft;
                button.label = "  Configure Hard Colliders...";
                button.height = 52;
                elements[storable.name] = button;
            }

            /* Configure collider friction button */
            if(personIsFemale)
            {
                var storable = configureColliderFrictionAction;
                var button = tittyMagic.CreateButton(storable.name, true);
                storable.RegisterButton(button);
                button.buttonText.alignment = TextAnchor.MiddleLeft;
                button.label = "  Configure Collider Friction...";
                button.height = 52;
                elements[storable.name] = button;
            }
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
