using System.Linq;
using System.Text;
using TittyMagic.Handlers;
using UnityEngine;
using UnityEngine.UI;
using static TittyMagic.Script;

namespace TittyMagic.UI
{
    internal class MorphingWindow : WindowBase
    {
        public MorphingWindow()
        {
            buildAction = () =>
            {
                CreateForceMorphingHeader(false);
                CreateMorphingInfoTextArea(false);

                CreateBaseMultiplierSlider(ForceMorphHandler.baseJsf, true, spacing: 72);
                CreateMultiplierSlider(ForceMorphHandler.upJsf, "Up", true, spacing: 5);
                CreateMultiplierSlider(ForceMorphHandler.downJsf, "Down", true);
                CreateMultiplierSlider(ForceMorphHandler.forwardJsf, "Forward", true);
                CreateMultiplierSlider(ForceMorphHandler.backJsf, "Back", true);
                CreateMultiplierSlider(ForceMorphHandler.leftRightJsf, "Left / Right", true);

                CreateOtherSettingsHeader(false);
                CreateNippleErectionSlider(false);
                CreateNippleErectionInfoTextArea(true, spacing: 50);

                elements[ForceMorphHandler.baseJsf.name].AddListener(UpdateAllSliderColors);
                UpdateAllSliderColors(0);
            };
        }

        private void CreateForceMorphingHeader(bool rightSide)
        {
            var storable = new JSONStorableString("forceMorphingHeader", "");
            elements[storable.name] = UIHelpers.HeaderTextField(storable, "Directional Force Morphing", rightSide);
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

        private void CreateMorphingInfoTextArea(bool rightSide, int spacing = 0)
        {
            var sb = new StringBuilder();
            sb.Append("\n".Size(12));
            sb.Append("Adjust how much breast shape is dynamically adjusted with morphs.");
            sb.Append("\n\n");
            sb.Append("The amount of morphing is based nipple's movement away from its");
            sb.Append(" neutral position. The neutral position is where the nipple is when");
            sb.Append(" the person is standing up and only the force of gravity is applied.");
            sb.Append("\n\n");
            sb.Append("Anything that causes the nipple to move will cause morphing:");
            sb.Append(" collision, gravity or any kind of animation.");
            sb.Append("\n\n");
            sb.Append("Too high multipliers for up, down and left/right directions can");
            sb.Append(" prevent breasts from returning to their neutral shape normally.");
            var storable = new JSONStorableString("forceMorphingMultipliersInfoText", sb.ToString());
            AddSpacer(storable.name, spacing, rightSide);

            var textField = tittyMagic.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.backgroundColor = Color.clear;
            textField.height = 825;
            elements[storable.name] = textField;
        }

        private void CreateNippleErectionSlider(bool rightSide, int spacing = 0)
        {
            var storable = NippleErectionHandler.nippleErectionJsf;
            AddSpacer(storable.name, spacing, rightSide);

            var slider = tittyMagic.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.label = "Nipple Erection";
            elements[storable.name] = slider;
        }

        private void CreateNippleErectionInfoTextArea(bool rightSide, int spacing = 0)
        {
            var sb = new StringBuilder();
            sb.Append("\n".Size(12));
            if(Gender.isFemale)
            {
                sb.Append("Expand nipple morphs and harden nipple physics.");
            }
            else
            {
                sb.Append("Expand nipple morphs.");
            }

            var storable = new JSONStorableString("nippleErectionInfoText", sb.ToString());
            AddSpacer(storable.name, spacing, rightSide);

            var textField = tittyMagic.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.height = 115;
            textField.backgroundColor = Color.clear;
            elements[storable.name] = textField;
        }

        private void UpdateAllSliderColors(float _)
        {
            UpdateSliderColor(ForceMorphHandler.upJsf);
            UpdateSliderColor(ForceMorphHandler.downJsf);
            UpdateSliderColor(ForceMorphHandler.forwardJsf);
            UpdateSliderColor(ForceMorphHandler.backJsf);
            UpdateSliderColor(ForceMorphHandler.leftRightJsf);
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
