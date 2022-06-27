using System.Collections.Generic;
using static TittyMagic.UI.UIHelpers;

namespace TittyMagic.UI
{
    internal class GravityWindow : IWindow
    {
        private readonly Script _script;
        public Dictionary<string, UIDynamic> elements { get; private set; }

        private readonly JSONStorableString _gravityPhysicsMultipliersHeader;
        private readonly JSONStorableString _gravityPhysicsMultipliersInfoText;

        public int Id() => 3;

        public GravityWindow(Script script)
        {
            _script = script;

            _gravityPhysicsMultipliersHeader = new JSONStorableString("gravityPhysicsMultipliersHeader", "");
            _gravityPhysicsMultipliersInfoText = new JSONStorableString("gravityPhysicsMultipliersInfoText", "");

            _gravityPhysicsMultipliersInfoText.val = "\n".Size(12) +
                "Adjust the effect of chest angle on breast main physics settings." +
                "\n\n" +
                "Higher values mean breasts drop more heavily up/down and left/right, " +
                "are more swingy when leaning forward, and less springy when leaning back.";
        }

        public void Rebuild()
        {
            elements = new Dictionary<string, UIDynamic>();

            CreateHeader(_gravityPhysicsMultipliersHeader, "Gravity physics multipliers", false);
            CreateMultiplierSlider(_script.gravityYStorable, false);
            CreateMultiplierSlider(_script.gravityXStorable, false);
            CreateMultiplierSlider(_script.gravityZStorable, false);
            CreateGravityPhysicsInfoTextArea(_gravityPhysicsMultipliersInfoText, true, spacing: 62);
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

        private void CreateGravityPhysicsInfoTextArea(JSONStorableString storable, bool rightSide, float spacing)
        {
            elements[$"{storable.name}Spacer"] = _script.NewSpacer(spacing, rightSide);
            var textField = _script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.height = 390;
            elements[storable.name] = textField;
        }

        public List<UIDynamicSlider> GetSliders()
        {
            var sliders = new List<UIDynamicSlider>();
            if(elements != null)
            {
                sliders.Add(elements[_script.gravityYStorable.name] as UIDynamicSlider);
                sliders.Add(elements[_script.gravityXStorable.name] as UIDynamicSlider);
                sliders.Add(elements[_script.gravityZStorable.name] as UIDynamicSlider);
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
