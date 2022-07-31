using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace TittyMagic.UI
{
    internal class GravityWindow : WindowBase
    {
        public GravityWindow(Script script) : base(script)
        {
            buildAction = BuildSelf;
            closeAction = ActionsOnClose;
        }

        private void BuildSelf()
        {
            CreateBreastGravityHeader(false);
            CreateGravityPhysicsInfoTextArea(false);

            CreateRecalibrateButton(true);
            CreateBaseMultiplierSlider(script.gravityPhysicsHandler.baseJsf, true, spacing: 5);
            CreateMultiplierSlider(script.gravityPhysicsHandler.upJsf, "Up", true, spacing: 5);
            CreateMultiplierSlider(script.gravityPhysicsHandler.downJsf, "Down", true);
            CreateMultiplierSlider(script.gravityPhysicsHandler.forwardJsf, "Forward", true);
            CreateMultiplierSlider(script.gravityPhysicsHandler.backJsf, "Back", true);
            CreateMultiplierSlider(script.gravityPhysicsHandler.leftRightJsf, "Left / Right", true);

            CreateOtherSettingsHeader(false);
            CreateOffsetMorphingSlider(false);
            CreateOffsetMorphingInfoTextArea(true, spacing: 50);

            elements[script.gravityPhysicsHandler.baseJsf.name].AddListener(UpdateAllSliderColors);
            UpdateAllSliderColors(0);
        }

        private void CreateBreastGravityHeader(bool rightSide)
        {
            var storable = new JSONStorableString("breastGravityHeader", "");
            elements[storable.name] = UIHelpers.HeaderTextField(script, storable, "Breast Gravity", rightSide);
        }

        private void CreateMultiplierSlider(JSONStorableFloat storable, string label, bool rightSide, int spacing = 0)
        {
            AddSpacer(storable.name, spacing, rightSide);
            var slider = script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.label = label;
            slider.AddListener((float _) => UpdateSliderColor(storable));
            elements[storable.name] = slider;
        }

        private void CreateGravityPhysicsInfoTextArea(bool rightSide, int spacing = 0)
        {
            var sb = new StringBuilder();
            sb.Append("\n".Size(12));
            sb.Append("Adjust the effect of gravity on breasts.");
            sb.Append("\n\n");
            sb.Append("Which slider takes effect depends on the person's orientation (or pose).");
            sb.Append(" For example, <i>Up</i> gravity is applied when the person is upside down");
            sb.Append(" and gravity pulls breasts <i>up</i> relative to the chest.");
            sb.Append("\n\n");
            sb.Append("Breast gravity feeds into directional force morphing: the more breasts are");
            sb.Append(" pulled by gravity, the more they will morph as well.");
            sb.Append("\n\n");
            sb.Append("Adjusting the sliders lets you preview the effect, but the final result requires a recalibration.");
            sb.Append(" Recalibrate with the button or by navigating away from this window.");
            var storable = new JSONStorableString("gravityPhysicsMultipliersInfoText", sb.ToString());
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
            var sb = new StringBuilder();
            sb.Append("\n".Size(12));
            sb.Append("Compensate for the droop caused by Down gravity.");
            var storable = new JSONStorableString("offsetMorphingInfoText", sb.ToString());
            AddSpacer(storable.name, spacing, rightSide);

            var textField = script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.height = 115;
            textField.backgroundColor = Color.clear;
            elements[storable.name] = textField;
        }

        private void UpdateAllSliderColors(float _)
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
