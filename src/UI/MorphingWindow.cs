using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TittyMagic.UI
{
    internal class MorphingWindow : WindowBase
    {
        private readonly JSONStorableString _forceMorphingMultipliersHeader;
        private readonly JSONStorableString _forceMorphingMultipliersInfoText;
        private readonly JSONStorableString _otherSettingsHeader;

        public MorphingWindow(Script script) : base(script)
        {
            id = 3;
            buildAction = BuildSelf;

            _forceMorphingMultipliersHeader = new JSONStorableString("forceMorphingMultipliersHeader", "");
            _forceMorphingMultipliersInfoText = new JSONStorableString("forceMorphingMultipliersInfoText", "");
            _otherSettingsHeader = new JSONStorableString("otherSettingsHeader", "");

            //TODO
            _forceMorphingMultipliersInfoText.val = "\n".Size(12) +
                "Adjust the amount of breast morphing due to forces including gravity.\n" +
                "\n" +
                "Breasts morph up/down, left/right and forward/back.";
        }

        private void BuildSelf()
        {
            CreateHeader(_forceMorphingMultipliersHeader, "Directional Force Morphing", false);
            CreateMorphingInfoTextArea(false);

            var baseSlider = CreateBaseMultiplierSlider(true, spacing: 72);
            CreateMultiplierSlider(script.forceMorphHandler.upJsf, "Up", true, spacing: 5);
            CreateMultiplierSlider(script.forceMorphHandler.downJsf, "Down", true);
            CreateMultiplierSlider(script.forceMorphHandler.forwardJsf, "Forward", true);
            CreateMultiplierSlider(script.forceMorphHandler.backJsf, "Back", true);
            CreateMultiplierSlider(script.forceMorphHandler.leftRightJsf, "Left / Right", true);

            CreateHeader(_otherSettingsHeader, "Other", false);
            CreateNippleErectionSlider(false);

            baseSlider.AddListener(UpdateAllSliderColors);
            UpdateAllSliderColors(0);
        }

        private void CreateHeader(JSONStorableString storable, string text, bool rightSide) =>
            elements[storable.name] = UIHelpers.HeaderTextField(script, storable, text, rightSide);

        private UIDynamicSlider CreateBaseMultiplierSlider(bool rightSide, int spacing = 0)
        {
            var storable = script.forceMorphHandler.baseJsf;
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

        private void CreateMorphingInfoTextArea(bool rightSide, int spacing = 0)
        {
            var storable = _forceMorphingMultipliersInfoText;
            AddSpacer(storable.name, spacing, rightSide);

            var textField = script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.backgroundColor = Color.clear;
            textField.height = 825;
            elements[storable.name] = textField;
        }

        private void CreateNippleErectionSlider(bool rightSide, int spacing = 0)
        {
            var storable = script.nippleMorphHandler.nippleErectionJsf;
            AddSpacer(storable.name, spacing, rightSide);

            var slider = script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.label = "Nipple Erection";
            elements[storable.name] = slider;
        }

        private void UpdateAllSliderColors(float value)
        {
            UpdateSliderColor(script.forceMorphHandler.upJsf);
            UpdateSliderColor(script.forceMorphHandler.downJsf);
            UpdateSliderColor(script.forceMorphHandler.forwardJsf);
            UpdateSliderColor(script.forceMorphHandler.backJsf);
            UpdateSliderColor(script.forceMorphHandler.leftRightJsf);
        }

        private void UpdateSliderColor(JSONStorableFloat storable)
        {
            var slider = (UIDynamicSlider) elements[storable.name];
            var images = slider.slider.gameObject.transform.GetComponentsInChildren<Image>();
            var fillImage = images.First(image => image.name == "Fill");
            var handleImage = images.First(image => image.name == "Handle");
            var color = MultiplierSliderColor(script.forceMorphHandler.baseJsf.val * storable.val);
            fillImage.color = color;
            handleImage.color = color;
        }

        private static Color MultiplierSliderColor(float value) =>
            value <= 1
                ? Color.Lerp(new Color(1, 1, 1, 0.25f), Color.white, value)
                : Color.Lerp(Color.white, new Color(1.0f, 0.2f, 0.2f), (value - 1) / 3);
    }
}
