using System.Text;
using TittyMagic.Handlers;
using TittyMagic.Models;
using UnityEngine;
using static TittyMagic.Script;

namespace TittyMagic.UI
{
    public class PhysicsWindow : WindowBase
    {
        public PhysicsWindow()
        {
            nestedWindows.Add(
                new ParameterWindow(
                    ParamName.MASS,
                    MainPhysicsHandler.massParameterGroup,
                    OnReturn
                )
            );
            foreach(var kvp in MainPhysicsHandler.parameterGroups)
            {
                nestedWindows.Add(
                    new ParameterWindow(
                        kvp.Key,
                        MainPhysicsHandler.parameterGroups[kvp.Key],
                        OnReturn
                    )
                );
            }

            if(personIsFemale)
            {
                foreach(var kvp in SoftPhysicsHandler.parameterGroups)
                {
                    nestedWindows.Add(
                        new ParameterWindow(
                            kvp.Key,
                            SoftPhysicsHandler.parameterGroups[kvp.Key],
                            OnReturn
                        )
                    );
                }
            }
        }

        protected override void OnBuild()
        {
            CreateHeaderTextField(new JSONStorableString("mainPhysicsParamsHeader", "Joint Physics Parameters"));

            /* Main physics info text area */
            {
                var sb = new StringBuilder();
                sb.Append("The pectoral joint gives breasts their primary physical properties.");
                var storable = new JSONStorableString("jointPhysicsInfoText", sb.ToString());
                var textField = tittyMagic.CreateTextField(storable);
                textField.UItext.fontSize = 28;
                textField.height = 100;
                textField.backgroundColor = Color.clear;
                elements[storable.name] = textField;
            }

            CreateParamButton(ParamName.MASS, MainPhysicsHandler.massParameterGroup, false);
            foreach(var group in MainPhysicsHandler.parameterGroups)
            {
                CreateParamButton(group.Key, group.Value, false);
            }

            CreateHeaderTextField(new JSONStorableString("softPhysicsParamsHeader", "Soft Physics Parameters"), true);

            /* Soft physics info text area */
            {
                var sb = new StringBuilder();
                if(personIsFemale)
                {
                    sb.Append("Physics of each breast's soft tissue is simulated with 111 small colliders and their associated joints.");
                }
                else
                {
                    sb.Append("Soft physics is not supported on a male character.");
                }

                var storable = new JSONStorableString("softPhysicsInfoText", sb.ToString());
                var textField = tittyMagic.CreateTextField(storable, true);
                textField.UItext.fontSize = 28;
                textField.height = 100;
                textField.backgroundColor = Color.clear;
                elements[storable.name] = textField;
            }

            /* Soft physics toggle */
            {
                var storable = SoftPhysicsHandler.breastSoftPhysicsOnJsb;
                var toggle = tittyMagic.CreateToggle(storable, true);
                toggle.height = 52;
                toggle.label = "Breast Soft Physics Enabled";
                elements[storable.name] = toggle;
                UpdateSoftPhysicsToggleStyle(tittyMagic.settingsMonitor.softPhysicsEnabled);
            }

            if(personIsFemale)
            {
                /* Allow self collision toggle */
                {
                    var storable = SoftPhysicsHandler.allowSelfCollisionJsb;
                    var toggle = tittyMagic.CreateToggle(storable, true);
                    toggle.height = 52;
                    toggle.label = "Breast Soft Physics Self Collide";
                    elements[storable.name] = toggle;
                }

                foreach(var group in SoftPhysicsHandler.parameterGroups)
                {
                    CreateParamButton(group.Key, group.Value, true);
                }
            }
        }

        public void UpdateSoftPhysicsToggleStyle(bool softPhysicsEnabled)
        {
            var storable = SoftPhysicsHandler.breastSoftPhysicsOnJsb;
            if(!elements.ContainsKey(storable.name))
            {
                return;
            }

            var toggle = (UIDynamicToggle) elements[storable.name];
            if(!personIsFemale)
            {
                toggle.SetActiveStyle(false, true);
            }
            else
            {
                toggle.textColor = storable.val && !softPhysicsEnabled ? Color.red : Color.black;
            }
        }

        private void CreateParamButton(string key, PhysicsParameterGroup param, bool rightSide)
        {
            var button = tittyMagic.CreateButton("  " + param.displayName, rightSide);
            button.height = 52;
            button.buttonText.alignment = TextAnchor.MiddleLeft;

            var nestedWindow = nestedWindows.Find(window => window.GetId() == key);
            button.AddListener(() =>
            {
                ClearSelf();
                activeNestedWindow = nestedWindow;
                activeNestedWindow.Rebuild();
            });

            ((ParameterWindow) nestedWindow).parentButton = button;
            button.label = ((ParameterWindow) nestedWindow).ParamButtonLabel();
            elements[key] = button;
        }
    }
}
