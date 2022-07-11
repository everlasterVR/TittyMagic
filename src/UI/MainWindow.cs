using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace TittyMagic.UI
{
    internal class MainWindow : IWindow
    {
        private readonly Script _script;
        private Dictionary<string, UIDynamic> _elements;

        public Dictionary<string, UIDynamic> GetElements() => _elements;

        public HardCollidersWindow nestedWindow { get; }
        public bool nestedWindowActive { get; private set; }

        private readonly JSONStorableString _title;

        public int Id() => 1;

        public MainWindow(Script script)
        {
            _script = script;
            _title = new JSONStorableString("title", "");

            if(Gender.isFemale)
            {
                nestedWindow = new HardCollidersWindow(script);
            }
        }

        public void Rebuild()
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

            UnityAction returnCallback = () =>
            {
                ClearNestedWindow();
                Rebuild();
                _script.PostNavigateToMainWindow();
            };

            button.AddListener(() =>
            {
                ClearSelf();
                nestedWindowActive = true;
                nestedWindow.Rebuild(returnCallback);
                PostNavigateToHardCollidersWindow();
            });

            _elements[storable.name] = button;
        }

        private void PostNavigateToHardCollidersWindow()
        {
            _script.colliderVisualizer.enabled = true;
            _script.colliderVisualizer.ShowPreviewsJSON.val = true;
            _script.hardColliderHandler.SyncAllOffsets();
            AddColliderPopupChangeHandler();
        }

        private void AddColliderPopupChangeHandler()
        {
            var elements = nestedWindow.GetElements();
            var element = elements[_script.hardColliderHandler.colliderGroupsJsc.name];
            var uiDynamicPopup = element as UIDynamicPopup;
            if(uiDynamicPopup != null)
            {
                uiDynamicPopup.popup.onValueChangeHandlers += nestedWindow.OnColliderPopupValueChanged;
                nestedWindow.RebuildColliderSection(_script.hardColliderHandler.colliderGroupsJsc.val);
            }
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
            if(nestedWindowActive)
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
            nestedWindow.Clear();
            nestedWindowActive = false;
        }

        public void ActionsOnWindowClosed()
        {
        }
    }
}
