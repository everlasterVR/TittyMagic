using System.Collections.Generic;
using UnityEngine;

namespace TittyMagic.UI
{
    internal class MainWindow : IWindow
    {
        private readonly Script _script;

        public Dictionary<string, UIDynamic> GetElements() => _elements;
        private Dictionary<string, UIDynamic> _elements;

        private readonly Dictionary<string, IWindow> _nestedWindows;

        public IWindow GetActiveNestedWindow() => _activeNestedWindow;
        private IWindow _activeNestedWindow;

        private readonly JSONStorableString _title;

        public int Id() => 1;

        public MainWindow(Script script)
        {
            _script = script;
            _nestedWindows = new Dictionary<string, IWindow>();

            _title = new JSONStorableString("title", "");
            if(Gender.isFemale)
            {
                var hardCollidersWindow = new HardCollidersWindow(script,
                    () =>
                    {
                        _activeNestedWindow = null;
                        RebuildSelf();
                    });
                _nestedWindows[$"{hardCollidersWindow.Id()}"] = hardCollidersWindow;
            }
        }

        public void Rebuild()
        {
            if(_activeNestedWindow != null)
            {
                _activeNestedWindow.Rebuild();
            }
            else
            {
                RebuildSelf();
            }
        }

        private void RebuildSelf()
        {
            _elements = new Dictionary<string, UIDynamic>();

            CreateTitleTextField(false);
            CreateRecalibratingTextField(true);
            CreateCalculateMassButton(true, spacing: 54);
            CreateAutoUpdateMassToggle(true);
            CreateMassSlider(true);

            if(Gender.isFemale)
            {
                CreateSoftPhysicsOnToggle(true, spacing: 15);
                CreateSoftnessSlider(true);
            }
            else
            {
                CreateSoftnessSlider(true, spacing: 15);
            }

            CreateQuicknessSlider(true);

            if(Gender.isFemale)
            {
                CreateConfigureHardCollidersButton(true, spacing: 15);
            }

            _elements[_script.autoUpdateJsb.name].AddListener(value =>
                _elements[_script.mainPhysicsHandler.massJsf.name].SetActiveStyle(!value, true)
            );
            _elements[_script.mainPhysicsHandler.massJsf.name].SetActiveStyle(!_script.autoUpdateJsb.val, true);
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

        private void CreateConfigureHardCollidersButton(bool rightSide, int spacing = 0)
        {
            var storable = _script.configureHardColliders;
            AddSpacer(storable.name, spacing, rightSide);

            var button = _script.CreateButton(storable.name, rightSide);
            storable.RegisterButton(button);
            button.buttonText.alignment = TextAnchor.MiddleLeft;
            button.label = "  Configure Hard Colliders...";
            button.height = 52;

            button.AddListener(() =>
            {
                ClearSelf();
                _activeNestedWindow = _nestedWindows["0"];
                _activeNestedWindow.Rebuild();
            });

            _elements[storable.name] = button;
        }

        private void AddSpacer(string name, int height, bool rightSide) =>
            _elements[$"{name}Spacer"] = _script.NewSpacer(height, rightSide);

        public List<UIDynamicSlider> GetSliders()
        {
            var sliders = new List<UIDynamicSlider>();
            if(_elements != null)
            {
                foreach(var element in _elements)
                {
                    var uiDynamicSlider = element.Value as UIDynamicSlider;
                    if(uiDynamicSlider != null)
                    {
                        sliders.Add(uiDynamicSlider);
                    }
                }
            }

            return sliders;
        }

        public IEnumerable<UIDynamicSlider> GetSlidersForRefresh()
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
            if(_activeNestedWindow != null)
            {
                _activeNestedWindow.Clear();
            }
            else
            {
                ClearSelf();
            }
        }

        public void ClosePopups()
        {
        }

        private void ClearSelf() =>
            _elements.ToList().ForEach(element => _script.RemoveElement(element.Value));
    }
}
