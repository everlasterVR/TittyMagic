using System.Linq;
using System.Text;
using TittyMagic.Handlers;
using UnityEngine;
using UnityEngine.UI;
using static TittyMagic.Script;

namespace TittyMagic.UI
{
    public class MorphingWindow : WindowBase
    {
        protected override void OnBuild()
        {
            CreateHeaderTextField(new JSONStorableString("forceMorphingHeader", "Directional Force Morphing"));

            /* Morphing info text area */
            {
                var sb = new StringBuilder();
                sb.Append("\n".Size(12));
                sb.Append("Adjust how much breast shape is dynamically adjusted with morphs.");
                sb.Append("\n\n");
                sb.Append("The amount of morphing is based on the breast's movement away from its");
                sb.Append(" neutral position relative to the chest. The neutral position is estimated as the");
                sb.Append(" position when the person is in an upright pose and only the force of gravity is applied.");
                sb.Append("\n\n");
                sb.Append("Anything that causes the breast to move will cause morphing: collision, gravity");
                sb.Append(" or any kind of animation.");
                sb.Append("\n\n");
                sb.Append("Too high multipliers for up and left/right directions can");
                sb.Append(" prevent breasts from returning to their neutral shape normally.");

                var storable = new JSONStorableString("forceMorphingMultipliersInfoText", sb.ToString());

                var textField = tittyMagic.CreateTextField(storable);
                textField.UItext.fontSize = 28;
                textField.backgroundColor = Color.clear;
                textField.height = 825;
                elements[storable.name] = textField;
            }

            CreateBaseMultiplierSlider(ForceMorphHandler.baseJsf, true, spacing: 72);
            CreateMultiplierSlider(ForceMorphHandler.upJsf, "Up", true, spacing: 5);
            CreateMultiplierSlider(ForceMorphHandler.forwardJsf, "Forward", true);
            CreateMultiplierSlider(ForceMorphHandler.backJsf, "Back", true);
            CreateMultiplierSlider(ForceMorphHandler.sidewaysInJsf, "Sideways In", true);
            CreateMultiplierSlider(ForceMorphHandler.sidewaysOutJsf, "Sideways Out", true);
            CreateOtherSettingsHeader(false);

            /* Nipple erection slider */
            {
                var storable = NippleErectionHandler.nippleErectionJsf;
                var slider = tittyMagic.CreateSlider(storable);
                slider.valueFormat = "F2";
                slider.label = "Nipple Erection";
                elements[storable.name] = slider;
            }

            /* Nipple erection info text area */
            {
                var sb = new StringBuilder();
                sb.Append("\n".Size(12));
                if(personIsFemale)
                {
                    sb.Append("Expand nipple morphs and harden nipple physics.");
                }
                else
                {
                    sb.Append("Expand nipple morphs.");
                }

                var storable = new JSONStorableString("nippleErectionInfoText", sb.ToString());
                AddSpacer(storable.name, 50, true);

                var textField = tittyMagic.CreateTextField(storable, true);
                textField.UItext.fontSize = 28;
                textField.height = 115;
                textField.backgroundColor = Color.clear;
                elements[storable.name] = textField;
            }

            elements[ForceMorphHandler.baseJsf.name].AddListener(UpdateAllSliderColors);
            UpdateAllSliderColors(0);
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
            UpdateSliderColor(ForceMorphHandler.upJsf);
            UpdateSliderColor(ForceMorphHandler.forwardJsf);
            UpdateSliderColor(ForceMorphHandler.backJsf);
            UpdateSliderColor(ForceMorphHandler.sidewaysInJsf);
            UpdateSliderColor(ForceMorphHandler.sidewaysOutJsf);
        }

        private void UpdateSliderColor(JSONStorableFloat storable)
        {
            var slider = (UIDynamicSlider) elements[storable.name];
            var images = slider.slider.gameObject.transform.GetComponentsInChildren<Image>();
            var fillImage = images.First(image => image.name == "Fill");
            var handleImage = images.First(image => image.name == "Handle");
            var color = MultiplierSliderColor(ForceMorphHandler.baseJsf.val * storable.val);
            fillImage.color = color;
            handleImage.color = color;
        }

        private static Color MultiplierSliderColor(float value) =>
            value <= 1
                ? Color.Lerp(new Color(1, 1, 1, 0.25f), Color.white, value)
                : Color.Lerp(Color.white, new Color(1.0f, 0.2f, 0.2f), (value - 1) / 3);
    }
}
