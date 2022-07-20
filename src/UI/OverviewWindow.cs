using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TittyMagic.UI
{
    internal class OverviewWindow : WindowBase
    {
        private readonly UnityAction _returnToParent;

        public string id { get; }

        public OverviewWindow(Script script, UnityAction onReturnToParent) : base(script)
        {
            id = "Overview";
            buildAction = BuildSelf;

            _returnToParent = () =>
            {
                Clear();
                onReturnToParent();
            };
        }

        private void BuildSelf()
        {
            CreateBackButton(_returnToParent, false);
            script.mainPhysicsHandler?.parameterGroups.Values.ToList()
                .ForEach(param => CreateCurrentValueSlider(param, false));

            AddSpacer("RightSideSpacer", 52, true);
            if(Gender.isFemale)
            {
                script.softPhysicsHandler.parameterGroups.Values.ToList()
                    .ForEach(param => CreateCurrentValueSlider(param, true));
            }
        }

        private void CreateBackButton(UnityAction backButtonListener, bool rightSide)
        {
            var button = script.CreateButton("Return", rightSide);

            button.textColor = Color.white;
            var colors = button.button.colors;
            colors.normalColor = UIHelpers.sliderGray;
            colors.highlightedColor = Color.grey;
            colors.pressedColor = Color.grey;
            button.button.colors = colors;

            button.AddListener(backButtonListener);
            elements["backButton"] = button;
        }

        private void CreateCurrentValueSlider(PhysicsParameterGroup param, bool rightSide)
        {
            var storable = param.left.valueJsf;
            var slider = script.CreateSlider(storable, rightSide);
            slider.valueFormat = param.left.valueFormat;
            slider.label = param.displayName;
            slider.slider.interactable = false;
            slider.quickButtonsEnabled = false;
            slider.defaultButtonEnabled = false;
            elements[param.id] = slider;
        }
    }
}
