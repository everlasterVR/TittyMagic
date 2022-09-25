using System.Linq;
using System.Text;
using TittyMagic.Handlers;
using UnityEngine;
using UnityEngine.UI;
using static TittyMagic.Script;

namespace TittyMagic.UI
{
    public class GravityWindow : WindowBase
    {
        protected override void OnBuild()
        {
            CreateHeaderTextField(new JSONStorableString("breastGravityHeader", "Breast Gravity"));

            /* Gravity physics info text area */
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
                var textField = tittyMagic.CreateTextField(storable);
                textField.UItext.fontSize = 28;
                textField.height = 825;
                textField.backgroundColor = Color.clear;
                elements[storable.name] = textField;
            }

            CreateRecalibrateButton(tittyMagic.recalibratePhysics, true);
            CreateBaseMultiplierSlider(GravityPhysicsHandler.baseJsf, true, spacing: 5);
            CreateMultiplierSlider(GravityPhysicsHandler.upJsf, "Up", true, spacing: 5);
            CreateMultiplierSlider(GravityPhysicsHandler.downJsf, "Down", true);
            CreateMultiplierSlider(GravityPhysicsHandler.forwardJsf, "Forward", true);
            CreateMultiplierSlider(GravityPhysicsHandler.backJsf, "Back", true);
            CreateMultiplierSlider(GravityPhysicsHandler.leftRightJsf, "Left / Right", true);
            CreateOtherSettingsHeader(false);

            /* Offset morphing slider */
            {
                var storable = GravityOffsetMorphHandler.offsetMorphingJsf;
                var slider = tittyMagic.CreateSlider(storable);
                slider.valueFormat = "F2";
                slider.label = "Down Offset Morphing";
                elements[storable.name] = slider;
            }

            /* Offset morphing info text area */
            {
                var sb = new StringBuilder();
                sb.Append("\n".Size(12));
                sb.Append("Compensate for the droop caused by Down gravity.");
                var storable = new JSONStorableString("offsetMorphingInfoText", sb.ToString());
                AddSpacer(storable.name, 50, true);

                var textField = tittyMagic.CreateTextField(storable, true);
                textField.UItext.fontSize = 28;
                textField.height = 115;
                textField.backgroundColor = Color.clear;
                elements[storable.name] = textField;
            }

            elements[GravityPhysicsHandler.baseJsf.name].AddListener(UpdateAllSliderColors);
            UpdateAllSliderColors(0);
        }

        protected override void OnClose()
        {
            if(tittyMagic.calibration.shouldRun)
            {
                tittyMagic.recalibratePhysics.actionCallback();
            }
        }

        private void CreateMultiplierSlider(JSONStorableFloat storable, string label, bool rightSide, int spacing = 0)
        {
            AddSpacer(storable.name, spacing, rightSide);
            var slider = tittyMagic.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.label = label;
            slider.AddListener((float _) => UpdateSliderColor(storable));
            elements[storable.name] = slider;
        }

        private void UpdateAllSliderColors(float _)
        {
            UpdateSliderColor(GravityPhysicsHandler.upJsf);
            UpdateSliderColor(GravityPhysicsHandler.downJsf);
            UpdateSliderColor(GravityPhysicsHandler.forwardJsf);
            UpdateSliderColor(GravityPhysicsHandler.backJsf);
            UpdateSliderColor(GravityPhysicsHandler.leftRightJsf);
        }

        private void UpdateSliderColor(JSONStorableFloat storable)
        {
            var slider = (UIDynamicSlider) elements[storable.name];
            var images = slider.slider.gameObject.transform.GetComponentsInChildren<Image>();
            var fillImage = images.First(image => image.name == "Fill");
            var handleImage = images.First(image => image.name == "Handle");
            var color = MultiplierSliderColor(GravityPhysicsHandler.baseJsf.val * storable.val);
            fillImage.color = color;
            handleImage.color = color;
        }

        private static Color MultiplierSliderColor(float value) =>
            value <= 1
                ? Color.Lerp(new Color(1, 1, 1, 0.25f), Color.white, value)
                : Color.Lerp(Color.white, new Color(1.0f, 0.2f, 0.2f), (value - 1) / 3);
    }
}
