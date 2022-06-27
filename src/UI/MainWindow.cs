using System.Collections.Generic;

namespace TittyMagic.UI
{
    internal class MainWindow : IWindow
    {
        private readonly Script _script;
        private readonly SoftPhysicsHandler _softPhysicsHandler;
        public Dictionary<string, UIDynamic> elements { get; private set; }

        private readonly JSONStorableString _title;

        public int Id() => 1;

        public MainWindow(Script script, SoftPhysicsHandler softPhysicsHandler)
        {
            _script = script;
            _softPhysicsHandler = softPhysicsHandler;
            _title = new JSONStorableString("title", "");
        }

        public void Rebuild()
        {
            elements = new Dictionary<string, UIDynamic>();

            CreateTitleTextField(false);
            CreateRecalibrateButton(true, spacing: 35);
            CreateCalculateMassButton(true);
            CreateAutoUpdateMassToggle(true);
            CreateMassSlider(false);

            CreateSoftPhysicsOnToggle(false, spacing: 15);
            CreateUseAuxBreastCollidersToggle(true, spacing: 15);

            CreateSoftnessSlider(false);
            CreateQuicknessSlider(true);
        }

        private void CreateTitleTextField(bool rightSide)
        {
            elements[_title.name] = UIHelpers.TitleTextField(
                _script,
                _title,
                $"{"\n".Size(12)}{nameof(TittyMagic)}    v{Script.VERSION}",
                100,
                rightSide
            );
        }

        private void CreateAutoUpdateMassToggle(bool rightSide, int spacing = 0)
        {
            var storable = _script.autoUpdateMass;
            AddSpacer(storable.name, spacing, rightSide);

            var toggle = _script.CreateToggle(storable, rightSide);
            toggle.height = 52;
            toggle.label = "Auto-Update Mass";
            elements[storable.name] = toggle;
        }

        private void CreateSoftPhysicsOnToggle(bool rightSide, int spacing = 0)
        {
            var storable = _softPhysicsHandler.softPhysicsOn;
            AddSpacer(storable.name, spacing, rightSide);

            var toggle = _script.CreateToggle(storable, rightSide);
            toggle.height = 52;
            toggle.label = "Soft Physics Enabled";
            elements[storable.name] = toggle;
        }

        private void CreateUseAuxBreastCollidersToggle(bool rightSide, int spacing = 0)
        {
            var storable = _softPhysicsHandler.useAuxBreastColliders;
            AddSpacer(storable.name, spacing, rightSide);

            var toggle = _script.CreateToggle(storable, rightSide);
            toggle.height = 52;
            toggle.label = "Breast Hard Colliders";
            elements[storable.name] = toggle;
        }

        private void CreateCalculateMassButton(bool rightSide, int spacing = 0)
        {
            const string name = "calculateBreastMass";
            AddSpacer(name, spacing, rightSide);

            var button = _script.CreateButton("Calculate Breast Mass", rightSide);
            button.height = 52;
            elements["calculateBreastMass"] = button;
        }

        private void CreateRecalibrateButton(bool rightSide, int spacing = 0)
        {
            const string name = "recalibratePhysics";
            AddSpacer(name, spacing, rightSide);

            var button = _script.CreateButton("Recalibrate Physics", rightSide);
            button.height = 52;
            elements[name] = button;
        }

        private void CreateMassSlider(bool rightSide, int spacing = 0)
        {
            var storable = _script.mass;
            AddSpacer(storable.name, spacing, rightSide);

            var slider = _script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F3";
            slider.label = "Breast Mass";
            slider.AddSliderClickMonitor();
            elements[storable.name] = slider;
        }

        private void CreateSoftnessSlider(bool rightSide, int spacing = 0)
        {
            var storable = _script.softness;
            AddSpacer(storable.name, spacing, rightSide);

            var slider = _script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F0";
            slider.slider.wholeNumbers = true;
            slider.label = "Breast Softness";
            slider.AddSliderClickMonitor();
            elements[storable.name] = slider;
        }

        private void CreateQuicknessSlider(bool rightSide, int spacing = 0)
        {
            var storable = _script.quickness;
            AddSpacer(storable.name, spacing, rightSide);

            var slider = _script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F0";
            slider.slider.wholeNumbers = true;
            slider.label = "Breast Quickness";
            slider.AddSliderClickMonitor();
            elements[storable.name] = slider;
        }

        private void AddSpacer(string name, int height, bool rightSide)
        {
            elements[$"{name}Spacer"] = _script.NewSpacer(height, rightSide);
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
