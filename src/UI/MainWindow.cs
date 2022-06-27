using System.Collections.Generic;
using UnityEngine;

namespace TittyMagic.UI
{
    internal class MainWindow : IWindow
    {
        private readonly Script _script;
        public Dictionary<string, UIDynamic> elements { get; private set; }

        private readonly JSONStorableString _title;

        public int Id() => 1;

        public MainWindow(Script script)
        {
            _script = script;
            _title = new JSONStorableString("title", "");
        }

        public void Rebuild()
        {
            elements = new Dictionary<string, UIDynamic>();

            CreateTitleTextField(_title, false);
            CreateAutoRefreshToggle(_script.autoRefresh, true, spacing: 35);
            CreateCalculateMassButton(true);
            CreateRecalibrateButton(true);
            CreateMassSlider(_script.mass, false);
            CreateSoftnessSlider(_script.softness, false);
            CreateQuicknessSlider(_script.quickness, true);
        }

        private void CreateTitleTextField(JSONStorableString storable, bool rightSide)
        {
            storable.val = $"<size=18>\n</size><b>{nameof(TittyMagic)}</b><size=36>    v{Script.VERSION}</size>";
            var textField = _script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 46;
            textField.height = 100;
            textField.backgroundColor = Color.clear;
            textField.textColor = UIHelpers.funkyCyan;
            elements[storable.name] = textField;
        }

        private void CreateAutoRefreshToggle(JSONStorableBool storable, bool rightSide, float spacing)
        {
            elements[$"{storable.name}Spacer"] = _script.NewSpacer(spacing, rightSide);
            elements[storable.name] = _script.CreateToggle(storable, rightSide);
        }

        private void CreateCalculateMassButton(bool rightSide)
        {
            var button = _script.CreateButton("Calculate breast mass", rightSide);
            button.height = 52;
            elements["calculateBreastMass"] = button;
        }

        private void CreateRecalibrateButton(bool rightSide)
        {
            var button = _script.CreateButton("Recalibrate physics", rightSide);
            button.height = 52;
            elements["recalibratePhysics"] = button;
        }

        private void CreateMassSlider(JSONStorableFloat storable, bool rightSide)
        {
            var slider = _script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F3";
            slider.AddSliderClickMonitor();
            elements[storable.name] = slider;
        }

        private void CreateSoftnessSlider(JSONStorableFloat storable, bool rightSide)
        {
            var slider = _script.CreateSlider(storable, rightSide);
            slider.valueFormat = "0f";
            slider.slider.wholeNumbers = true;
            slider.AddSliderClickMonitor();
            elements[storable.name] = slider;
        }

        private void CreateQuicknessSlider(JSONStorableFloat storable, bool rightSide)
        {
            var slider = _script.CreateSlider(storable, rightSide);
            slider.valueFormat = "0f";
            slider.slider.wholeNumbers = true;
            slider.AddSliderClickMonitor();
            elements[storable.name] = slider;
        }

        public UIDynamic GetCalculateMassButton()
        {
            return elements["calculateBreastMass"];
        }

        public UIDynamic GetRecalibrateButton()
        {
            return elements["recalibratePhysics"];
        }

        public List<UIDynamicSlider> GetSliders()
        {
            var sliders = new List<UIDynamicSlider>();
            if(elements != null)
            {
                sliders.Add(elements[_script.mass.name] as UIDynamicSlider);
                sliders.Add(elements[_script.softness.name] as UIDynamicSlider);
                sliders.Add(elements[_script.quickness.name] as UIDynamicSlider);
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
