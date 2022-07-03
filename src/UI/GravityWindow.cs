using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TittyMagic.UI
{
    internal class GravityWindow : IWindow
    {
        private readonly Script _script;
        private Dictionary<string, UIDynamic> _elements;

        public Dictionary<string, UIDynamic> GetElements() => _elements;

        private readonly JSONStorableString _gravityPhysicsMultipliersHeader;
        private readonly JSONStorableString _gravityPhysicsMultipliersInfoText;
        private readonly JSONStorableString _offsetMorphingInfoText;
        private readonly JSONStorableString _otherSettingsHeader;

        public int Id() => 4;

        public GravityWindow(Script script)
        {
            _script = script;

            _gravityPhysicsMultipliersHeader = new JSONStorableString("gravityPhysicsMultipliersHeader", "");
            _gravityPhysicsMultipliersInfoText = new JSONStorableString("gravityPhysicsMultipliersInfoText", "");
            _otherSettingsHeader = new JSONStorableString("otherSettingsHeader", "");
            _offsetMorphingInfoText = new JSONStorableString("offsetMorphingInfoText", "");

            _gravityPhysicsMultipliersInfoText.val = "\n".Size(12) +
                "Adjust the effect of chest angle on breast main physics settings." +
                "\n\n" +
                "Higher values mean breasts drop more heavily up/down and left/right, " +
                "are more swingy when leaning forward, and less springy when leaning back.";

            _offsetMorphingInfoText.val = "\n".Size(12) +
                "Rotate breasts up when upright to compensate for negative Up/Down Angle Target.";
        }

        public void Rebuild()
        {
            _elements = new Dictionary<string, UIDynamic>();

            CreateHeader(_gravityPhysicsMultipliersHeader, "Gravity Physics Multipliers", false);
            CreateRecalibrateButton(true, spacing: 62);

            var baseSlider = CreateBaseMultiplierSlider(false);

            CreateMultiplierSlider(_script.gravityPhysicsHandler.upJsf, "Up", false, spacing: 5);
            CreateMultiplierSlider(_script.gravityPhysicsHandler.downJsf, "Down", false);
            CreateMultiplierSlider(_script.gravityPhysicsHandler.forwardJsf, "Forward", false);
            CreateMultiplierSlider(_script.gravityPhysicsHandler.backJsf, "Back", false);
            CreateMultiplierSlider(_script.gravityPhysicsHandler.leftRightJsf, "Left/Right", false);
            CreateGravityPhysicsInfoTextArea(true, spacing: 72);

            baseSlider.AddListener(UpdateAllSliderColors);
            UpdateAllSliderColors(0);

            CreateHeader(_otherSettingsHeader, "Other", false);
            CreateOffsetMorphingSlider(false);
            CreateOffsetMorphingInfoTextArea(true, spacing: 62);
        }

        private void CreateHeader(JSONStorableString storable, string text, bool rightSide) =>
            _elements[storable.name] = UIHelpers.HeaderTextField(_script, storable, text, rightSide);

        private void CreateRecalibrateButton(bool rightSide, int spacing = 0)
        {
            var storable = _script.recalibratePhysics;
            AddSpacer(storable.name, spacing, rightSide);

            var button = _script.CreateButton("Recalibrate Physics", rightSide);
            storable.RegisterButton(button);
            button.height = 52;
            _elements[storable.name] = button;
        }

        private UIDynamicSlider CreateBaseMultiplierSlider(bool rightSide, int spacing = 0)
        {
            var storable = _script.gravityPhysicsHandler.baseJsf;
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

        private void CreateGravityPhysicsInfoTextArea(bool rightSide, int spacing = 0)
        {
            var storable = _gravityPhysicsMultipliersInfoText;
            AddSpacer(storable.name, spacing, rightSide);

            var textField = _script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.height = 660;
            textField.backgroundColor = Color.clear;
            _elements[storable.name] = textField;
        }

        private void CreateOffsetMorphingSlider(bool rightSide, int spacing = 0)
        {
            var storable = _script.offsetMorphHandler.offsetMorphingJsf;
            AddSpacer(storable.name, spacing, rightSide);

            var slider = _script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.label = "Gravity Offset Morphing";
            _elements[storable.name] = slider;
        }

        private void CreateOffsetMorphingInfoTextArea(bool rightSide, int spacing = 0)
        {
            var storable = _offsetMorphingInfoText;
            AddSpacer(storable.name, spacing, rightSide);

            var textField = _script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.height = 115;
            textField.backgroundColor = Color.clear;
            _elements[storable.name] = textField;
        }

        private void AddSpacer(string name, int height, bool rightSide) => _elements[$"{name}Spacer"] = _script.NewSpacer(height, rightSide);

        private void UpdateAllSliderColors(float value)
        {
            UpdateSliderColor(_script.gravityPhysicsHandler.upJsf);
            UpdateSliderColor(_script.gravityPhysicsHandler.downJsf);
            UpdateSliderColor(_script.gravityPhysicsHandler.forwardJsf);
            UpdateSliderColor(_script.gravityPhysicsHandler.backJsf);
            UpdateSliderColor(_script.gravityPhysicsHandler.leftRightJsf);
        }

        private void UpdateSliderColor(JSONStorableFloat storable)
        {
            var slider = (UIDynamicSlider) _elements[storable.name];
            var images = slider.slider.gameObject.transform.GetComponentsInChildren<Image>();
            var fillImage = images.First(image => image.name == "Fill");
            var handleImage = images.First(image => image.name == "Handle");
            var color = MultiplierSliderColor(_script.gravityPhysicsHandler.baseJsf.val * storable.val);
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
                sliders.Add(_elements[_script.gravityPhysicsHandler.baseJsf.name] as UIDynamicSlider);
                sliders.Add(_elements[_script.gravityPhysicsHandler.upJsf.name] as UIDynamicSlider);
                sliders.Add(_elements[_script.gravityPhysicsHandler.downJsf.name] as UIDynamicSlider);
                sliders.Add(_elements[_script.gravityPhysicsHandler.forwardJsf.name] as UIDynamicSlider);
                sliders.Add(_elements[_script.gravityPhysicsHandler.backJsf.name] as UIDynamicSlider);
                sliders.Add(_elements[_script.gravityPhysicsHandler.leftRightJsf.name] as UIDynamicSlider);
                sliders.Add(_elements[_script.offsetMorphHandler.offsetMorphingJsf.name] as UIDynamicSlider);
            }

            return sliders;
        }

        public void Clear() =>
            _elements.ToList().ForEach(element => _script.RemoveElement(element.Value));
    }
}
