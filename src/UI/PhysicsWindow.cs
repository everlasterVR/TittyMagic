using System.Text;
using TittyMagic.Handlers;
using UnityEngine;
using static TittyMagic.Script;

namespace TittyMagic.UI
{
    internal class PhysicsWindow : WindowBase
    {
        public PhysicsWindow()
        {
            buildAction = () =>
            {
                CreateHeader(
                    new JSONStorableString("mainPhysicsParamsHeader", ""),
                    "Joint Physics Parameters",
                    false
                );
                CreateJointPhysicsInfoTextArea(false);

                CreateParamButton(ParamName.MASS, MainPhysicsHandler.massParameterGroup, false);
                MainPhysicsHandler.parameterGroups.ToList()
                    .ForEach(kvp => CreateParamButton(kvp.Key, kvp.Value, false));

                CreateHeader(
                    new JSONStorableString("softPhysicsParamsHeader", ""),
                    "Soft Physics Parameters",
                    true
                );
                CreateSoftPhysicsInfoTextArea(true);
                CreateSoftPhysicsOnToggle(true);

                if(Gender.isFemale)
                {
                    CreateAllowSelfCollisionToggle(true);
                    SoftPhysicsHandler.parameterGroups.ToList()
                        .ForEach(kvp => CreateParamButton(kvp.Key, kvp.Value, true));
                }
            };

            /* Create parameter windows */
            {
                nestedWindows.Add(
                    new ParameterWindow(
                        ParamName.MASS,
                        MainPhysicsHandler.massParameterGroup,
                        onReturnToParent
                    )
                );
                foreach(var kvp in MainPhysicsHandler.parameterGroups)
                {
                    nestedWindows.Add(
                        new ParameterWindow(
                            kvp.Key,
                            MainPhysicsHandler.parameterGroups[kvp.Key],
                            onReturnToParent
                        )
                    );
                }

                if(Gender.isFemale)
                {
                    foreach(var kvp in SoftPhysicsHandler.parameterGroups)
                    {
                        nestedWindows.Add(
                            new ParameterWindow(
                                kvp.Key,
                                SoftPhysicsHandler.parameterGroups[kvp.Key],
                                onReturnToParent
                            )
                        );
                    }
                }
            }
        }

        private void CreateAllowSelfCollisionToggle(bool rightSide)
        {
            var storable = SoftPhysicsHandler.allowSelfCollisionJsb;
            var toggle = tittyMagic.CreateToggle(storable, rightSide);
            toggle.height = 52;
            toggle.label = "Breast Soft Physics Self Collide";
            elements[storable.name] = toggle;
        }

        private void CreateHeader(JSONStorableString storable, string text, bool rightSide, int spacing = 0)
        {
            elements[$"{storable.name}Spacer"] = tittyMagic.NewSpacer(spacing, rightSide);
            elements[storable.name] = UIHelpers.HeaderTextField(storable, text, rightSide);
        }

        private void CreateJointPhysicsInfoTextArea(bool rightSide, int spacing = 0)
        {
            var sb = new StringBuilder();
            sb.Append("The pectoral joint gives breasts their primary physical properties.");
            var storable = new JSONStorableString("jointPhysicsInfoText", sb.ToString());
            AddSpacer(storable.name, spacing, rightSide);

            var textField = tittyMagic.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.height = 100;
            textField.backgroundColor = Color.clear;
            elements[storable.name] = textField;
        }

        private void CreateSoftPhysicsInfoTextArea(bool rightSide, int spacing = 0)
        {
            var sb = new StringBuilder();
            if(Gender.isFemale)
            {
                sb.Append("Physics of each breast's soft tissue is simulated with 111 small colliders and their associated joints.");
            }
            else
            {
                sb.Append("Soft physics is not supported on a male character.");
            }

            var storable = new JSONStorableString("softPhysicsInfoText", sb.ToString());
            AddSpacer(storable.name, spacing, rightSide);

            var textField = tittyMagic.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.height = 100;
            textField.backgroundColor = Color.clear;
            elements[storable.name] = textField;
        }

        private void CreateSoftPhysicsOnToggle(bool rightSide, int spacing = 0)
        {
            var storable = SoftPhysicsHandler.softPhysicsOnJsb;
            AddSpacer(storable.name, spacing, rightSide);

            var toggle = tittyMagic.CreateToggle(storable, rightSide);
            toggle.height = 52;
            toggle.label = "Soft Physics Enabled";
            if(!Gender.isFemale)
            {
                toggle.SetActiveStyle(false, true);
            }

            elements[storable.name] = toggle;
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
