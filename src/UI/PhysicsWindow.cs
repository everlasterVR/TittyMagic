using System.Text;
using UnityEngine;
using UnityEngine.Events;

namespace TittyMagic.UI
{
    internal class PhysicsWindow : WindowBase
    {
        private UnityAction onReturnToParent => () =>
        {
            activeNestedWindow = null;
            buildAction();
        };

        public PhysicsWindow(Script script) : base(script)
        {
            buildAction = BuildSelf;
            CreateParameterWindows();
        }

        private void CreateParameterWindows()
        {
            nestedWindows.Add(
                new ParameterWindow(
                    script,
                    ParamName.MASS,
                    script.mainPhysicsHandler.massParameterGroup,
                    onReturnToParent
                )
            );
            foreach(var kvp in script.mainPhysicsHandler.parameterGroups)
            {
                nestedWindows.Add(
                    new ParameterWindow(
                        script,
                        kvp.Key,
                        script.mainPhysicsHandler.parameterGroups[kvp.Key],
                        onReturnToParent
                    )
                );
            }

            if(Gender.isFemale)
            {
                foreach(var kvp in script.softPhysicsHandler.parameterGroups)
                {
                    nestedWindows.Add(
                        new ParameterWindow(
                            script,
                            kvp.Key,
                            script.softPhysicsHandler.parameterGroups[kvp.Key],
                            onReturnToParent
                        )
                    );
                }
            }
        }

        private void BuildSelf()
        {
            CreateHeader(
                new JSONStorableString("mainPhysicsParamsHeader", ""),
                "Joint Physics Parameters",
                false
            );
            CreateJointPhysicsInfoTextArea(false);

            CreateParamButton(ParamName.MASS, script.mainPhysicsHandler.massParameterGroup, false);
            script.mainPhysicsHandler?.parameterGroups.ToList()
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
                script.softPhysicsHandler.parameterGroups.ToList()
                    .ForEach(kvp => CreateParamButton(kvp.Key, kvp.Value, true));
            }
        }

        private void CreateAllowSelfCollisionToggle(bool rightSide)
        {
            var storable = script.softPhysicsHandler.allowSelfCollision;
            var toggle = script.CreateToggle(storable, rightSide);
            toggle.height = 52;
            toggle.label = "Breast Soft Physics Self Collide";
            elements[storable.name] = toggle;
        }

        private void CreateHeader(JSONStorableString storable, string text, bool rightSide, int spacing = 0)
        {
            elements[$"{storable.name}Spacer"] = script.NewSpacer(spacing, rightSide);
            elements[storable.name] = UIHelpers.HeaderTextField(script, storable, text, rightSide);
        }

        private void CreateJointPhysicsInfoTextArea(bool rightSide, int spacing = 0)
        {
            var sb = new StringBuilder();
            sb.Append("The pectoral joint gives breasts their primary physical properties.");
            var storable = new JSONStorableString("jointPhysicsInfoText", sb.ToString());
            AddSpacer(storable.name, spacing, rightSide);

            var textField = script.CreateTextField(storable, rightSide);
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

            var textField = script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.height = 100;
            textField.backgroundColor = Color.clear;
            elements[storable.name] = textField;
        }

        private void CreateSoftPhysicsOnToggle(bool rightSide, int spacing = 0)
        {
            var storable = script.softPhysicsHandler.softPhysicsOn;
            AddSpacer(storable.name, spacing, rightSide);

            var toggle = script.CreateToggle(storable, rightSide);
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
            var button = script.CreateButton("  " + param.displayName, rightSide);
            button.height = 52;
            button.buttonText.alignment = TextAnchor.MiddleLeft;

            var nestedWindow = (ParameterWindow) nestedWindows.Find(window => (window as ParameterWindow)?.id == key);
            button.AddListener(() =>
            {
                ClearSelf();
                activeNestedWindow = nestedWindow;
                activeNestedWindow.Rebuild();
            });

            nestedWindow.parentButton = button;
            button.label = nestedWindow.ParamButtonLabel();
            elements[key] = button;
        }
    }
}
