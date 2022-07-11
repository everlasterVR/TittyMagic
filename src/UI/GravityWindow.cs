using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TittyMagic.UI
{
    internal class GravityWindow : WindowBase
    {
        private readonly JSONStorableString _gravityPhysicsMultipliersHeader;
        private readonly JSONStorableString _gravityPhysicsMultipliersInfoText;
        private readonly JSONStorableString _offsetMorphingInfoText;
        private readonly JSONStorableString _otherSettingsHeader;

        public GravityWindow(Script script) : base(script)
        {
            id = 4;
            buildAction = BuildSelf;
            closeAction = ActionsOnClose;

            _gravityPhysicsMultipliersHeader = new JSONStorableString("gravityPhysicsMultipliersHeader", "");
            _gravityPhysicsMultipliersInfoText = new JSONStorableString("gravityPhysicsMultipliersInfoText", "");
            _otherSettingsHeader = new JSONStorableString("otherSettingsHeader", "");
            _offsetMorphingInfoText = new JSONStorableString("offsetMorphingInfoText", "");

            _gravityPhysicsMultipliersInfoText.val = "\n".Size(12) +
                "Adjust the effect of gravity on breasts." +
                "\n\n".Size(24) +
                "Which slider takes effect depends on the person's orientation: " +
                "e.g. Up gravity is applied when upside down (breasts \"fall up\" more heavily)." +
                "\n\n".Size(24) +
                "Low values maintain the shape better, and make breasts easier to move." +
                "\n\n".Size(24) +
                "High values add weight, causing breasts to deform more and fall in the direction of gravity more quickly." +
                "\n\n".Size(24) +
                "Adjusting the sliders lets you preview the effect in real time. " +
                "The final result requires a recalibration - click the button or navigate away from this view.";

            _offsetMorphingInfoText.val = "\n".Size(12) +
                "Compensates for the droop caused by Down gravity.";
        }

        private void BuildSelf()
        {
            elements = new Dictionary<string, UIDynamic>();

            CreateHeader(_gravityPhysicsMultipliersHeader, "Breast Gravity", false);
            CreateGravityPhysicsInfoTextArea(false);

            CreateRecalibrateButton(true, spacing: 5);
            var baseSlider = CreateBaseMultiplierSlider(true);
            CreateMultiplierSlider(script.gravityPhysicsHandler.upJsf, "Up", true, spacing: 5);
            CreateMultiplierSlider(script.gravityPhysicsHandler.downJsf, "Down", true);
            CreateMultiplierSlider(script.gravityPhysicsHandler.forwardJsf, "Forward", true);
            CreateMultiplierSlider(script.gravityPhysicsHandler.backJsf, "Back", true);
            CreateMultiplierSlider(script.gravityPhysicsHandler.leftRightJsf, "Left / Right", true);

            CreateHeader(_otherSettingsHeader, "Other", false);
            CreateOffsetMorphingInfoTextArea(true, spacing: 50);
            CreateOffsetMorphingSlider(false);

            baseSlider.AddListener(UpdateAllSliderColors);
            UpdateAllSliderColors(0);
        }

        private void CreateHeader(JSONStorableString storable, string text, bool rightSide, int spacing = 0)
        {
            AddSpacer(storable.name, spacing, rightSide);
            elements[storable.name] = UIHelpers.HeaderTextField(script, storable, text, rightSide);
        }

        private void CreateRecalibrateButton(bool rightSide, int spacing = 0)
        {
            var storable = script.recalibratePhysics;
            AddSpacer(storable.name, spacing, rightSide);

            var button = script.CreateButton("Recalibrate Physics", rightSide);
            storable.RegisterButton(button);
            button.height = 52;
            elements[storable.name] = button;
        }

        private UIDynamicSlider CreateBaseMultiplierSlider(bool rightSide, int spacing = 0)
        {
            var storable = script.gravityPhysicsHandler.baseJsf;
            AddSpacer(storable.name, spacing, rightSide);
            var slider = script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.label = "Base Multiplier";
            elements[storable.name] = slider;
            return slider;
        }

        private void CreateMultiplierSlider(JSONStorableFloat storable, string label, bool rightSide, int spacing = 0)
        {
            AddSpacer(storable.name, spacing, rightSide);
            var slider = script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.label = label;
            slider.AddListener((float value) => UpdateSliderColor(storable));
            elements[storable.name] = slider;
        }

        private void CreateGravityPhysicsInfoTextArea(bool rightSide, int spacing = 0)
        {
            var storable = _gravityPhysicsMultipliersInfoText;
            AddSpacer(storable.name, spacing, rightSide);

            var textField = script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.height = 825;
            textField.backgroundColor = Color.clear;
            elements[storable.name] = textField;
        }

        private void CreateOffsetMorphingSlider(bool rightSide, int spacing = 0)
        {
            var storable = script.offsetMorphHandler.offsetMorphingJsf;
            AddSpacer(storable.name, spacing, rightSide);

            var slider = script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.label = "Down Offset Morphing";
            elements[storable.name] = slider;
        }

        private void CreateOffsetMorphingInfoTextArea(bool rightSide, int spacing = 0)
        {
            var storable = _offsetMorphingInfoText;
            AddSpacer(storable.name, spacing, rightSide);

            var textField = script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.height = 115;
            textField.backgroundColor = Color.clear;
            elements[storable.name] = textField;
        }

        private void UpdateAllSliderColors(float value)
        {
            UpdateSliderColor(script.gravityPhysicsHandler.upJsf);
            UpdateSliderColor(script.gravityPhysicsHandler.downJsf);
            UpdateSliderColor(script.gravityPhysicsHandler.forwardJsf);
            UpdateSliderColor(script.gravityPhysicsHandler.backJsf);
            UpdateSliderColor(script.gravityPhysicsHandler.leftRightJsf);
        }

        private void UpdateSliderColor(JSONStorableFloat storable)
        {
            var slider = (UIDynamicSlider) elements[storable.name];
            var images = slider.slider.gameObject.transform.GetComponentsInChildren<Image>();
            var fillImage = images.First(image => image.name == "Fill");
            var handleImage = images.First(image => image.name == "Handle");
            var color = MultiplierSliderColor(script.gravityPhysicsHandler.baseJsf.val * storable.val);
            fillImage.color = color;
            handleImage.color = color;
        }

        private static Color MultiplierSliderColor(float value) =>
            value <= 1
                ? Color.Lerp(new Color(1, 1, 1, 0.25f), Color.white, value)
                : Color.Lerp(Color.white, new Color(1.0f, 0.2f, 0.2f), (value - 1) / 3);

        private void ActionsOnClose() =>
            script.RecalibrateOnNavigation();
    }
}
