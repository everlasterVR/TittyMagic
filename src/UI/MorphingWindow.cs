using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TittyMagic.UI
{
    internal class MorphingWindow : IWindow
    {
        private readonly Script _script;
        private Dictionary<string, UIDynamic> _elements;

        public Dictionary<string, UIDynamic> GetElements() => _elements;

        private readonly JSONStorableString _dynamicMorphingMultipliersHeader;
        private readonly JSONStorableString _dynamicMorphingMultipliersInfoText;
        private readonly JSONStorableString _otherSettingsHeader;

        public int Id() => 3;

        public MorphingWindow(Script script)
        {
            _script = script;

            _dynamicMorphingMultipliersHeader = new JSONStorableString("dynamicMorphingMultipliersHeader", "");
            _dynamicMorphingMultipliersInfoText = new JSONStorableString("dynamicMorphingMultipliersInfoText", "");
            _otherSettingsHeader = new JSONStorableString("otherSettingsHeader", "");

            //TODO
            _dynamicMorphingMultipliersInfoText.val = "\n".Size(12) +
                "Adjust the amount of breast morphing due to forces including gravity.\n" +
                "\n" +
                "Breasts morph up/down, left/right and forward/back.";
        }

        public void Rebuild()
        {
            _elements = new Dictionary<string, UIDynamic>();

            CreateHeader(_dynamicMorphingMultipliersHeader, "Dynamic Force Morphing", false);

            var baseSlider = CreateBaseMultiplierSlider(false);

            CreateMultiplierSlider(_script.forceMorphHandler.upJsf, "Up", false, spacing: 5);
            CreateMultiplierSlider(_script.forceMorphHandler.downJsf, "Down", false);
            CreateMultiplierSlider(_script.forceMorphHandler.forwardJsf, "Forward", false);
            CreateMultiplierSlider(_script.forceMorphHandler.backJsf, "Back", false);
            CreateMultiplierSlider(_script.forceMorphHandler.leftRightJsf, "Left / Right", false);

            baseSlider.AddListener(UpdateAllSliderColors);
            UpdateAllSliderColors(0);

            CreateHeader(_otherSettingsHeader, "Other", false);
            CreateNippleErectionSlider(false);
        }

        private void CreateHeader(JSONStorableString storable, string text, bool rightSide) =>
            _elements[storable.name] = UIHelpers.HeaderTextField(_script, storable, text, rightSide);

        private UIDynamicSlider CreateBaseMultiplierSlider(bool rightSide, int spacing = 0)
        {
            var storable = _script.forceMorphHandler.baseJsf;
            AddSpacer(storable.name, spacing, rightSide);
            var slider = _script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.label = "Base Multiplier";
            _elements[storable.name] = slider;
            return slider;
        }

        private void CreateMultiplierSlider(JSONStorableFloat storable, string label, bool rightSide, int spacing = 0)
        {
            AddSpacer(storable.name, spacing, rightSide);
            var slider = _script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.label = label;
            slider.AddListener((float value) => UpdateSliderColor(storable));
            _elements[storable.name] = slider;
        }

        private void CreateMultiplierTextArea(JSONStorableString storable, bool rightSide, int spacing = 0)
        {
            AddSpacer(storable.name, spacing, rightSide);

            var textField = _script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.backgroundColor = Color.clear;
            textField.height = 120;
            _elements[storable.name] = textField;
        }

        private void CreateNippleErectionSlider(bool rightSide, int spacing = 0)
        {
            var storable = _script.nippleMorphHandler.nippleErectionJsf;
            AddSpacer(storable.name, spacing, rightSide);

            var slider = _script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.label = "Nipple Erection";
            _elements[storable.name] = slider;
        }

        private void AddSpacer(string name, int height, bool rightSide) => _elements[$"{name}Spacer"] = _script.NewSpacer(height, rightSide);

        private void UpdateAllSliderColors(float value)
        {
            UpdateSliderColor(_script.forceMorphHandler.upJsf);
            UpdateSliderColor(_script.forceMorphHandler.downJsf);
            UpdateSliderColor(_script.forceMorphHandler.forwardJsf);
            UpdateSliderColor(_script.forceMorphHandler.backJsf);
            UpdateSliderColor(_script.forceMorphHandler.leftRightJsf);
        }

        private void UpdateSliderColor(JSONStorableFloat storable)
        {
            var slider = (UIDynamicSlider) _elements[storable.name];
            var images = slider.slider.gameObject.transform.GetComponentsInChildren<Image>();
            var fillImage = images.First(image => image.name == "Fill");
            var handleImage = images.First(image => image.name == "Handle");
            var color = MultiplierSliderColor(_script.forceMorphHandler.baseJsf.val * storable.val);
            fillImage.color = color;
            handleImage.color = color;
        }

        private static Color MultiplierSliderColor(float value) =>
            value <= 1
                ? Color.Lerp(new Color(1, 1, 1, 0.25f), Color.white, value)
                : Color.Lerp(Color.white, new Color(1.0f, 0.2f, 0.2f), (value - 1) / 3);

        public List<UIDynamicSlider> GetSliders()
        {
            var sliders = new List<UIDynamicSlider>();
            if(_elements != null)
            {
                sliders.Add(_elements[_script.forceMorphHandler.upJsf.name] as UIDynamicSlider);
                sliders.Add(_elements[_script.forceMorphHandler.leftRightJsf.name] as UIDynamicSlider);
                sliders.Add(_elements[_script.forceMorphHandler.forwardJsf.name] as UIDynamicSlider);
                sliders.Add(_elements[_script.nippleMorphHandler.nippleErectionJsf.name] as UIDynamicSlider);
            }

            return sliders;
        }

        public void Clear() =>
            _elements.ToList().ForEach(element => _script.RemoveElement(element.Value));
    }
}
