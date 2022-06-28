using System.Collections.Generic;

namespace TittyMagic.UI
{
    internal class MorphingWindow : IWindow
    {
        private readonly Script _script;
        public Dictionary<string, UIDynamic> elements { get; private set; }

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

            _dynamicMorphingMultipliersInfoText.val = "\n".Size(12) +
                "Adjust the amount of breast morphing due to forces including gravity.\n" +
                "\n" +
                "Breasts morph up/down, left/right and forward/back.";
        }

        public void Rebuild()
        {
            elements = new Dictionary<string, UIDynamic>();

            CreateHeader(_dynamicMorphingMultipliersHeader, "Dynamic Morphing Multipliers", false);
            CreateMultiplierSlider(_script.morphingYStorable, "Up/Down", false);
            CreateMultiplierSlider(_script.morphingXStorable, "Left/Right", false);
            CreateMultiplierSlider(_script.morphingZStorable, "Forward/Back", false);
            CreateDynamicMorphingInfoTextArea(true, spacing: 62);

            CreateHeader(_otherSettingsHeader, "Other", false);
            CreateNippleErectionSlider(false);
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

        private void CreateNippleErectionSlider(bool rightSide, int spacing = 0)
        {
            var storable = _script.nippleErection;
            AddSpacer(storable.name, spacing, rightSide);

            var slider = _script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.label = "Nipple Erection";
            elements[storable.name] = slider;
        }

        private void CreateDynamicMorphingInfoTextArea(bool rightSide, int spacing = 0)
        {
            var storable = _dynamicMorphingMultipliersInfoText;
            AddSpacer(storable.name, spacing, rightSide);

            var textField = _script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.height = 390;
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
                sliders.Add(elements[_script.morphingXStorable.name] as UIDynamicSlider);
                sliders.Add(elements[_script.morphingYStorable.name] as UIDynamicSlider);
                sliders.Add(elements[_script.morphingZStorable.name] as UIDynamicSlider);
                sliders.Add(elements[_script.nippleErection.name] as UIDynamicSlider);
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
