using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TittyMagic.UI
{
    internal class HardCollidersWindow
    {
        private readonly Script _script;
        private Dictionary<string, UIDynamic> _elements;

        public Dictionary<string, UIDynamic> GetElements() => _elements;

        private readonly JSONStorableString _header;
        private readonly JSONStorableString _infoText;

        public HardCollidersWindow(Script script)
        {
            _script = script;

            _header = new JSONStorableString("hardCollidersHeader", "");
            _infoText = new JSONStorableString("hardCollidersInfoText", "");

            _infoText.val = "\n".Size(12) +
                "Experimental feature." +
                "\n\nAdjust Scale Offset to match breast size." +
                "\n\nCollision Force makes breasts easier to move (but also adds weight).";
        }

        public void Rebuild(UnityAction backButtonListener)
        {
            _elements = new Dictionary<string, UIDynamic>();

            CreateBackButton(backButtonListener, false);
            CreateHeader(false);
            CreateHardCollidersInfoTextArea(true, spacing: 117);
            CreateColliderScaleSlider(false);
            // CreateColliderRadiusSlider(false);
            // CreateColliderHeightSlider(false);
            CreateColliderForceSlider(false);
            CreateShowHardCollidersChooser(false, spacing: 15);
            CreateXRayVisualizationToggle(false);
            CreatePreviewOpacitySlider(false);
        }

        private void CreateBackButton(UnityAction backButtonListener, bool rightSide)
        {
            var button = _script.CreateButton("Return", rightSide);

            button.textColor = Color.white;
            var colors = button.button.colors;
            colors.normalColor = UIHelpers.sliderGray;
            colors.highlightedColor = Color.grey;
            colors.pressedColor = Color.grey;
            button.button.colors = colors;

            button.AddListener(backButtonListener);
            _elements["backButton"] = button;
        }

        private void CreateHeader(bool rightSide)
        {
            var textField = UIHelpers.HeaderTextField(_script, _header, "Breast Hard Colliders", rightSide);
            _elements[_header.name] = textField;
        }

        private void CreateHardCollidersInfoTextArea(bool rightSide, int spacing = 0)
        {
            var storable = _infoText;
            AddSpacer(storable.name, spacing, rightSide);

            var textField = _script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.height = 323;
            textField.backgroundColor = Color.clear;
            _elements[storable.name] = textField;
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

        private void CreateShowHardCollidersChooser(bool rightSide, int spacing = 0)
        {
            var storable = _script.colliderVisualizer.GroupsJSON;
            AddSpacer(storable.name, spacing, rightSide);

            var chooser = _script.CreatePopup(storable, rightSide);
            chooser.label = "Show Previews";
            chooser.popup.labelText.color = Color.black;
            _elements[storable.name] = chooser;
        }

        private void CreateXRayVisualizationToggle(bool rightSide, int spacing = 0)
        {
            var storable = _script.colliderVisualizer.XRayPreviewsJSON;
            AddSpacer(storable.name, spacing, rightSide);

            var toggle = _script.CreateToggle(storable, rightSide);
            toggle.height = 52;
            toggle.label = "Use XRay Previews";
            _elements[storable.name] = toggle;
        }

        private void CreatePreviewOpacitySlider(bool rightSide, int spacing = 0)
        {
            var storable = _script.colliderVisualizer.PreviewOpacityJSON;
            AddSpacer(storable.name, spacing, rightSide);

            var slider = _script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.label = "Preview Opacity";
            _elements[storable.name] = slider;
        }

        private void AddSpacer(string name, int height, bool rightSide) =>
            _elements[$"{name}Spacer"] = _script.NewSpacer(height, rightSide);

        public List<UIDynamicSlider> GetSliders()
        {
            var sliders = new List<UIDynamicSlider>();
            if(_elements != null)
            {
                //TODO
            }

            return sliders;
        }

        public void Clear()
        {
            _elements.ToList().ForEach(element => _script.RemoveElement(element.Value));
            ActionsOnWindowClosed();
        }

        private void ActionsOnWindowClosed()
        {
            var element = _elements[_script.colliderVisualizer.GroupsJSON.name];
            var groupsPopup = element as UIDynamicPopup;
            if(groupsPopup != null)
            {
                groupsPopup.popup.onValueChangeHandlers -= OnGroupsPopupValueChanged;
            }

            _script.colliderVisualizer.ShowPreviewsJSON.val = false;
        }

        public void OnGroupsPopupValueChanged(string value)
        {
            _elements[_script.colliderVisualizer.XRayPreviewsJSON.name].SetActiveStyle(value != "Off");
            _elements[_script.colliderVisualizer.PreviewOpacityJSON.name].SetActiveStyle(value != "Off");
        }
    }
}
