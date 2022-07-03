using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TittyMagic.UI
{
    internal class MainWindow : IWindow
    {
        private readonly Script _script;
        private Dictionary<string, UIDynamic> _elements;

        public Dictionary<string, UIDynamic> GetElements() => _elements;

        private readonly JSONStorableString _hardCollidersHeader;
        private readonly JSONStorableString _hardCollidersInfoText;

        private readonly HardCollidersWindow _hardCollidersWindow;

        private bool _nestedWindowActive;
        private readonly JSONStorableString _title;

        public int Id() => 1;

        public MainWindow(Script script)
        {
            _script = script;
            _title = new JSONStorableString("title", "");

            if(Gender.isFemale)
            {
                _hardCollidersWindow = new HardCollidersWindow(script);
                _hardCollidersHeader = new JSONStorableString("hardCollidersHeader", "");
                _hardCollidersInfoText = new JSONStorableString("hardCollidersInfoText", "");

                _hardCollidersInfoText.val = "\n".Size(12) +
                    "Experimental feature." +
                    "\n\nAdjust Scale Offset to match breast size." +
                    "\n\nCollision Force makes breasts easier to move (but also adds weight).";
            }
        }

        public void Rebuild()
        {
            _elements = new Dictionary<string, UIDynamic>();

            CreateTitleTextField(false);
            CreateRecalibratingTextField(true);
            CreateCalculateMassButton(true, spacing: 54);
            CreateAutoUpdateMassToggle(true);
            CreateMassSlider(false);

            if(Gender.isFemale)
            {
                CreateSoftPhysicsOnToggle(false, spacing: 15);
            }

            CreateSoftnessSlider(false);
            CreateQuicknessSlider(true, spacing: Gender.isFemale ? 80 : 0);

            if(Gender.isFemale)
            {
                CreateHeader(_hardCollidersHeader, "Breast Hard Colliders", false, spacing: 15);
                CreateUseAuxBreastCollidersToggle(false);
                CreateColliderScaleSlider(false);
                // CreateColliderRadiusSlider(false);
                // CreateColliderHeightSlider(false);
                CreateColliderForceSlider(false);
                CreateHardCollidersInfoTextArea(true, spacing: 92);
                // CreateConfigureHardCollidersButton(false);
            }
        }

        private void CreateTitleTextField(bool rightSide)
        {
            var textField = UIHelpers.TitleTextField(
                _script,
                _title,
                $"{"\n".Size(12)}{nameof(TittyMagic)}    {Script.VERSION}",
                100,
                rightSide
            );
            textField.UItext.fontSize = 40;
            _elements[_title.name] = textField;
        }

        private void CreateAutoUpdateMassToggle(bool rightSide, int spacing = 0)
        {
            var storable = _script.autoUpdateJsb;
            AddSpacer(storable.name, spacing, rightSide);

            var toggle = _script.CreateToggle(storable, rightSide);
            toggle.height = 52;
            toggle.label = "Auto-Update Mass";
            _elements[storable.name] = toggle;
        }

        private void CreateSoftPhysicsOnToggle(bool rightSide, int spacing = 0)
        {
            var storable = _script.softPhysicsHandler.softPhysicsOn;
            AddSpacer(storable.name, spacing, rightSide);

            var toggle = _script.CreateToggle(storable, rightSide);
            toggle.height = 52;
            toggle.label = "Soft Physics Enabled";
            _elements[storable.name] = toggle;
        }

        private void CreateRecalibratingTextField(bool rightSide, int spacing = 0)
        {
            var storable = _script.statusInfo;
            AddSpacer(storable.name, spacing, rightSide);
            _elements[storable.name] = UIHelpers.NotificationTextField(_script, storable, 32, rightSide);
        }

        private void CreateCalculateMassButton(bool rightSide, int spacing = 0)
        {
            var storable = _script.calculateBreastMass;
            AddSpacer(storable.name, spacing, rightSide);

            var button = _script.CreateButton("Calculate Breast Mass", rightSide);
            storable.RegisterButton(button);
            button.height = 53;
            _elements[storable.name] = button;
        }

        private void CreateMassSlider(bool rightSide, int spacing = 0)
        {
            var storable = _script.mainPhysicsHandler.massJsf;
            AddSpacer(storable.name, spacing, rightSide);

            var slider = _script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F3";
            slider.label = "Breast Mass";
            slider.AddSliderClickMonitor();
            _elements[storable.name] = slider;
        }

        private void CreateSoftnessSlider(bool rightSide, int spacing = 0)
        {
            var storable = _script.softnessJsf;
            AddSpacer(storable.name, spacing, rightSide);

            var slider = _script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F0";
            slider.slider.wholeNumbers = true;
            slider.label = "Breast Softness";
            slider.AddSliderClickMonitor();
            _elements[storable.name] = slider;
        }

        private void CreateQuicknessSlider(bool rightSide, int spacing = 0)
        {
            var storable = _script.quicknessJsf;
            AddSpacer(storable.name, spacing, rightSide);

            var slider = _script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F0";
            slider.slider.wholeNumbers = true;
            slider.label = "Breast Quickness";
            slider.AddSliderClickMonitor();
            _elements[storable.name] = slider;
        }

        private void CreateHeader(JSONStorableString storable, string text, bool rightSide, int spacing = 0)
        {
            AddSpacer(storable.name, spacing, rightSide);
            _elements[storable.name] = UIHelpers.HeaderTextField(_script, storable, text, rightSide);
        }

        private void CreateUseAuxBreastCollidersToggle(bool rightSide, int spacing = 0)
        {
            var storable = _script.hardColliderHandler.enabledJsb;
            AddSpacer(storable.name, spacing, rightSide);

            var toggle = _script.CreateToggle(storable, rightSide);
            toggle.height = 52;
            toggle.label = "Use Hard Colliders";
            _elements[storable.name] = toggle;
        }

        private void CreateColliderScaleSlider(bool rightSide, int spacing = 0)
        {
            var storable = _script.hardColliderHandler.scaleJsf;
            AddSpacer(storable.name, spacing, rightSide);

            var slider = _script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F4";
            slider.label = "Collider Scale Offset";
            _elements[storable.name] = slider;
        }

        private void CreateColliderRadiusSlider(bool rightSide, int spacing = 0)
        {
            var storable = _script.hardColliderHandler.radiusJsf;
            AddSpacer(storable.name, spacing, rightSide);

            var slider = _script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.label = "Radius Multiplier";
            _elements[storable.name] = slider;
        }

        private void CreateColliderHeightSlider(bool rightSide, int spacing = 0)
        {
            var storable = _script.hardColliderHandler.heightJsf;
            AddSpacer(storable.name, spacing, rightSide);

            var slider = _script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.label = "Height Multiplier";
            _elements[storable.name] = slider;
        }

        private void CreateColliderForceSlider(bool rightSide, int spacing = 0)
        {
            var storable = _script.hardColliderHandler.forceJsf;
            AddSpacer(storable.name, spacing, rightSide);

            var slider = _script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.label = "Collision Force Multiplier";
            slider.AddSliderClickMonitor();
            _elements[storable.name] = slider;
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
            });

            _elements[name] = button;
        }

        private void CreateHardCollidersInfoTextArea(bool rightSide, int spacing = 0)
        {
            var storable = _hardCollidersInfoText;
            AddSpacer(storable.name, spacing, rightSide);

            var textField = _script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.height = 323;
            textField.backgroundColor = Color.clear;
            _elements[storable.name] = textField;
        }

        private void AddSpacer(string name, int height, bool rightSide) => _elements[$"{name}Spacer"] = _script.NewSpacer(height, rightSide);

        public List<UIDynamicSlider> GetSliders()
        {
            var sliders = GetSlidersForRefresh();
            if(
                _script.hardColliderHandler.forceJsf != null &&
                _elements.ContainsKey(_script.hardColliderHandler.forceJsf.name)
            )
            {
                sliders.Add(_elements[_script.hardColliderHandler.forceJsf.name] as UIDynamicSlider);
            }

            return sliders;
        }

        public List<UIDynamicSlider> GetSlidersForRefresh()
        {
            var sliders = new List<UIDynamicSlider>();
            if(_elements != null)
            {
                sliders.Add(_elements[_script.mainPhysicsHandler.massJsf.name] as UIDynamicSlider);
                sliders.Add(_elements[_script.softnessJsf.name] as UIDynamicSlider);
                sliders.Add(_elements[_script.quicknessJsf.name] as UIDynamicSlider);
            }

            return sliders;
        }

        public void Clear()
        {
            if(_nestedWindowActive)
            {
                ClearNestedWindow();
            }
            else
            {
                ClearSelf();
            }
        }

        private void ClearSelf() =>
            _elements.ToList().ForEach(element => _script.RemoveElement(element.Value));

        private void ClearNestedWindow()
        {
            _hardCollidersWindow.Clear();
            _nestedWindowActive = false;
        }

        public void ActionsOnWindowClosed()
        {
        }
    }
}
