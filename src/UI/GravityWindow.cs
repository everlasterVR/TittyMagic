using System.Collections.Generic;

namespace TittyMagic.UI
{
    internal class GravityWindow : IWindow
    {
        private readonly Script _script;
        public Dictionary<string, UIDynamic> elements { get; private set; }

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
            elements = new Dictionary<string, UIDynamic>();

            CreateHeader(_gravityPhysicsMultipliersHeader, "Gravity Physics Multipliers", false);
            CreateMultiplierSlider(_script.gravityYStorable, "Up/Down", false);
            CreateMultiplierSlider(_script.gravityXStorable, "Left/Right", false);
            CreateMultiplierSlider(_script.gravityZStorable, "Forward/Back", false);
            CreateGravityPhysicsInfoTextArea(true, spacing: 62);

            CreateHeader(_otherSettingsHeader, "Other", false);
            CreateOffsetMorphingSlider(false);
            CreateOffsetMorphingInfoTextArea(true, spacing: 62);
        }

        private void CreateHeader(JSONStorableString storable, string text, bool rightSide)
        {
            elements[storable.name] = UIHelpers.HeaderTextField(_script, storable, text, rightSide);
        }

        private void CreateMultiplierSlider(JSONStorableFloat storable, string label, bool rightSide, int spacing = 0)
        {
            AddSpacer(storable.name, spacing, rightSide);
            var slider = _script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.label = label;
            slider.AddSliderClickMonitor();
            elements[storable.name] = slider;
        }

        private void CreateGravityPhysicsInfoTextArea(bool rightSide, int spacing = 0)
        {
            var storable = _gravityPhysicsMultipliersInfoText;
            AddSpacer(storable.name, spacing, rightSide);

            var textField = _script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.height = 390;
            elements[storable.name] = textField;
        }

        private void CreateOffsetMorphingSlider(bool rightSide, int spacing = 0)
        {
            var storable = _script.offsetMorphing;
            AddSpacer(storable.name, spacing, rightSide);

            var slider = _script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.label = "Gravity Offset Morphing";
            slider.AddSliderClickMonitor();
            elements[storable.name] = slider;
        }

        private void CreateOffsetMorphingInfoTextArea(bool rightSide, int spacing = 0)
        {
            var storable = _offsetMorphingInfoText;
            AddSpacer(storable.name, spacing, rightSide);

            var textField = _script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.height = 115;
            elements[storable.name] = textField;
        }

        private void AddSpacer(string name, int height, bool rightSide)
        {
            elements[$"{name}Spacer"] = _script.NewSpacer(height, rightSide);
        }

        public List<UIDynamicSlider> GetSliders()
        {
            var sliders = new List<UIDynamicSlider>();
            if(elements != null)
            {
                sliders.Add(elements[_script.gravityYStorable.name] as UIDynamicSlider);
                sliders.Add(elements[_script.gravityXStorable.name] as UIDynamicSlider);
                sliders.Add(elements[_script.gravityZStorable.name] as UIDynamicSlider);
                sliders.Add(elements[_script.offsetMorphing.name] as UIDynamicSlider);
            }

            return sliders;
        }

        public void Clear()
        {
            foreach(var element in elements)
            {
                _script.RemoveElement(element.Value);
            }
        }
    }
}
