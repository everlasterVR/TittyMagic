// ReSharper disable MemberCanBePrivate.Global
using System.Collections.Generic;
using static TittyMagic.UI.UIHelpers;

namespace TittyMagic.UI
{
    internal class MorphingWindow : IWindow
    {
        private readonly Script _script;
        public Dictionary<string, UIDynamic> elements { get; private set; }

        private readonly JSONStorableString _dynamicMorphingMultipliersHeader;
        private readonly JSONStorableString _dynamicMorphingMultipliersInfoText;
        private readonly JSONStorableString _additionalSettingsHeader;
        private readonly JSONStorableString _offsetMorphingInfoText;

        public int Id() => 2;

        public MorphingWindow(Script script)
        {
            _script = script;

            _dynamicMorphingMultipliersHeader = new JSONStorableString("dynamicMorphingMultipliersHeader", "");
            _dynamicMorphingMultipliersInfoText = new JSONStorableString("dynamicMorphingMultipliersInfoText", "");
            _additionalSettingsHeader = new JSONStorableString("additionalSettingsHeader", "");
            _offsetMorphingInfoText = new JSONStorableString("offsetMorphingInfoText", "");

            _dynamicMorphingMultipliersInfoText.val = "\n".Size(12) +
                "Adjust the amount of breast morphing due to forces including gravity.\n" +
                "\n" +
                "Breasts morph up/down, left/right and forward/back.";

            _offsetMorphingInfoText.val = "\n".Size(12) +
                "Rotate breasts up when upright to compensate for negative Up/Down Angle Target.";
        }

        public void Rebuild()
        {
            elements = new Dictionary<string, UIDynamic>();

            CreateHeader(_dynamicMorphingMultipliersHeader, "Dynamic morphing multipliers", false);
            CreateMultiplierSlider(_script.morphingYStorable, false);
            CreateMultiplierSlider(_script.morphingXStorable, false);
            CreateMultiplierSlider(_script.morphingZStorable, false);
            CreateDynamicMorphingInfoTextArea(_dynamicMorphingMultipliersInfoText, true, spacing: 62);

            CreateHeader(_additionalSettingsHeader, "Additional settings", false);
            CreateOffsetMorphingSlider(_script.offsetMorphing, false);
            CreateNippleErectionSlider(_script.nippleErection, false);
            CreateAdditionalSettingsInfoTextArea(_offsetMorphingInfoText, true, spacing: 62);
        }

        private void CreateHeader(JSONStorableString storable, string text, bool rightSide)
        {
            elements[storable.name] = HeaderTextField(_script, storable, text, rightSide);
        }

        private void CreateMultiplierSlider(JSONStorableFloat storable, bool rightSide)
        {
            var slider = _script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.AddSliderClickMonitor();
            elements[storable.name] = slider;
        }

        private void CreateOffsetMorphingSlider(JSONStorableFloat storable, bool rightSide)
        {
            var slider = _script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.AddSliderClickMonitor();
            elements[storable.name] = slider;
        }

        private void CreateNippleErectionSlider(JSONStorableFloat storable, bool rightSide)
        {
            var slider = _script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            elements[storable.name] = slider;
        }

        private void CreateDynamicMorphingInfoTextArea(JSONStorableString storable, bool rightSide, float spacing)
        {
            elements[$"{storable.name}Spacer"] = _script.NewSpacer(spacing, rightSide);
            var textField = _script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.height = 390;
            elements[storable.name] = textField;
        }

        private void CreateAdditionalSettingsInfoTextArea(JSONStorableString storable, bool rightSide, float spacing)
        {
            elements[$"{storable.name}Spacer"] = _script.NewSpacer(spacing, rightSide);
            var textField = _script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.height = 115;
            elements[storable.name] = textField;
        }

        public List<UIDynamicSlider> GetSliders()
        {
            var sliders = new List<UIDynamicSlider>();
            if(elements != null)
            {
                sliders.Add(elements[_script.morphingXStorable.name] as UIDynamicSlider);
                sliders.Add(elements[_script.morphingYStorable.name] as UIDynamicSlider);
                sliders.Add(elements[_script.morphingZStorable.name] as UIDynamicSlider);
                sliders.Add(elements[_script.offsetMorphing.name] as UIDynamicSlider);
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
