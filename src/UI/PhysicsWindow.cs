using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TittyMagic.UI
{
    internal class PhysicsWindow : WindowBase
    {
        private readonly JSONStorableString _jointPhysicsParamsHeader;
        private readonly JSONStorableString _softPhysicsParamsHeader;

        public PhysicsWindow(Script script) : base(script)
        {
            id = 2;
            buildAction = BuildSelf;

            _jointPhysicsParamsHeader = new JSONStorableString("mainPhysicsParamsHeader", "");
            if(Gender.isFemale)
            {
                _softPhysicsParamsHeader = new JSONStorableString("softPhysicsParamsHeader", "");
            }

            CreateParameterWindows();
        }

        private void CreateParameterWindows()
        {
            UnityAction onReturnToParent = () =>
            {
                activeNestedWindow = null;
                buildAction();
            };

            script.mainPhysicsHandler.parameterGroups.Keys.ToList()
                .ForEach(key => nestedWindows[key] = new ParameterWindow(
                    script,
                    script.mainPhysicsHandler.parameterGroups[key],
                    onReturnToParent
                ));
            if(Gender.isFemale)
            {
                script.softPhysicsHandler.parameterGroups.Keys.ToList()
                    .ForEach(key => nestedWindows[key] = new ParameterWindow(
                        script,
                        script.softPhysicsHandler.parameterGroups[key],
                        onReturnToParent
                    ));
            }
        }

        private void BuildSelf()
        {
            elements = new Dictionary<string, UIDynamic>();

            CreateHeader(_jointPhysicsParamsHeader, "Joint Physics Parameters", false);
            script.mainPhysicsHandler?.parameterGroups.ToList()
                .ForEach(kvp => CreateParamButton(kvp.Key, kvp.Value, false));

            if(Gender.isFemale)
            {
                CreateHeader(_softPhysicsParamsHeader, "Soft Physics Parameters", true);
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

        private void CreateParamButton(string key, PhysicsParameterGroup param, bool rightSide)
        {
            var button = script.CreateButton("  " + param.displayName, rightSide);
            button.height = 52;
            button.buttonText.alignment = TextAnchor.MiddleLeft;

            button.AddListener(() =>
            {
                ClearSelf();
                activeNestedWindow = nestedWindows[key];
                activeNestedWindow.Rebuild();
            });

            var nestedWindow = (ParameterWindow) nestedWindows[key];
            nestedWindow.parentButton = button;
            button.label = nestedWindow.ParamButtonLabel();
            elements[key] = button;
        }
    }
}
