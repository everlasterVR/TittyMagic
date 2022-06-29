using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TittyMagic.UI
{
    internal class MainWindow : IWindow
    {
        private readonly Script _script;
        private readonly HardColliderHandler _hardColliderHandler;
        private readonly SoftPhysicsHandler _softPhysicsHandler;
        public Dictionary<string, UIDynamic> elements { get; private set; }

        private readonly JSONStorableString _hardCollidersHeader;
        private readonly JSONStorableString _hardCollidersInfoText;

        private readonly HardCollidersWindow _hardCollidersWindow;

        private bool _nestedWindowActive;
        private readonly JSONStorableString _title;

        public int Id() => 1;

        public MainWindow(Script script, HardColliderHandler hardColliderHandler, SoftPhysicsHandler softPhysicsHandler)
        {
            _script = script;
            _hardColliderHandler = hardColliderHandler;
            _softPhysicsHandler = softPhysicsHandler;
            _title = new JSONStorableString("title", "");

            _hardCollidersWindow = new HardCollidersWindow(script, _softPhysicsHandler);
            _hardCollidersHeader = new JSONStorableString("hardCollidersHeader", "");
            _hardCollidersInfoText = new JSONStorableString("hardCollidersInfoText", "");

            // TODO
            _hardCollidersInfoText.val = "\n".Size(12);
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
            CreateSoftnessSlider(false);
            CreateQuicknessSlider(true, spacing: 80);

            CreateHeader(_hardCollidersHeader, "Breast Hard Colliders", false, spacing: 15);
            CreateUseAuxBreastCollidersToggle(false);
            CreateColliderRadiusSlider(false);
            CreateColliderMassSlider(false);
            CreateHardCollidersInfoTextArea(true, spacing: 92);
            // CreateConfigureHardCollidersButton(false);
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

        private void CreateCalculateMassButton(bool rightSide, int spacing = 0)
        {
            const string name = "calculateBreastMass";
            AddSpacer(name, spacing, rightSide);

            var button = _script.CreateButton("Calculate Breast Mass", rightSide);
            button.height = 52;
            elements[name] = button;
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

        private void CreateHeader(JSONStorableString storable, string text, bool rightSide, int spacing = 0)
        {
            AddSpacer(storable.name, spacing, rightSide);
            elements[storable.name] = UIHelpers.HeaderTextField(_script, storable, text, rightSide);
        }

        private void CreateUseAuxBreastCollidersToggle(bool rightSide, int spacing = 0)
        {
            var storable = _hardColliderHandler.useHardColliders;
            AddSpacer(storable.name, spacing, rightSide);

            var toggle = _script.CreateToggle(storable, rightSide);
            toggle.height = 52;
            toggle.label = "Use Hard Colliders";
            elements[storable.name] = toggle;
        }

        private void CreateColliderRadiusSlider(bool rightSide, int spacing = 0)
        {
            var storable = _hardColliderHandler.hardCollidersRadiusMultiplier;
            AddSpacer(storable.name, spacing, rightSide);

            var slider = _script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.label = "Radius Multiplier";
            elements[storable.name] = slider;
        }

        private void CreateColliderMassSlider(bool rightSide, int spacing = 0)
        {
            var storable = _hardColliderHandler.hardCollidersMassMultiplier;
            AddSpacer(storable.name, spacing, rightSide);

            var slider = _script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.label = "Mass Multiplier";
            slider.AddSliderClickMonitor();
            elements[storable.name] = slider;
        }

        private void CreateConfigureHardCollidersButton(bool rightSide, int spacing = 0)
        {
            const string name = "configureHardColliders";
            AddSpacer(name, spacing, rightSide);

            var button = _script.CreateButton("  Configure...", rightSide);
            button.buttonText.alignment = TextAnchor.MiddleLeft;
            button.buttonColor = UIHelpers.darkerGray;
            button.textColor = Color.white;
            button.height = 52;

            UnityAction returnCallback = () =>
            {
                ClearNestedWindow();
                Rebuild();
            };

            button.AddListener(() =>
            {
                ClearSelf();
                _nestedWindowActive = true;
                _hardCollidersWindow.Rebuild(returnCallback);
                _script.EnableCurrentTabRenavigation();
            });

            elements[name] = button;
        }

        private void CreateHardCollidersInfoTextArea(bool rightSide, int spacing = 0)
        {
            var storable = _hardCollidersInfoText;
            AddSpacer(storable.name, spacing, rightSide);

            var textField = _script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.height = 323;
            elements[storable.name] = textField;
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
            var sliders = GetSlidersForRefresh();
            sliders.Add(elements[_hardColliderHandler.hardCollidersMassMultiplier.name] as UIDynamicSlider);
            return sliders;
        }

        public List<UIDynamicSlider> GetSlidersForRefresh()
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
            if(_nestedWindowActive)
                ClearNestedWindow();
            else
                ClearSelf();
        }

        private void ClearSelf()
        {
            foreach(var element in elements)
            {
                _script.RemoveElement(element.Value);
            }
        }

        private void ClearNestedWindow()
        {
            _hardCollidersWindow.Clear();
            _nestedWindowActive = false;
        }
    }
}
