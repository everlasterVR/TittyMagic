using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Image = UnityEngine.UI.Image;

namespace TittyMagic.UI
{
    internal class HardCollidersWindow
    {
        private readonly Script _script;
        private Dictionary<string, UIDynamic> _elements;
        private Dictionary<string, UIDynamic> _colliderSectionElements;

        public Dictionary<string, UIDynamic> GetElements() => _elements;
        public Dictionary<string, UIDynamic> GetColliderSectionElements() => _colliderSectionElements;

        private readonly JSONStorableString _title;
        private readonly JSONStorableString _baseForceInfoText;
        private readonly JSONStorableString _mainInfoText;
        private readonly JSONStorableString _selectColliderText;
        private readonly JSONStorableString _scalingHeaderText;
        private readonly JSONStorableString _scalingInfoText;
        private readonly JSONStorableString _centerHeaderText;
        private readonly JSONStorableString _centerInfoText;

        public HardCollidersWindow(Script script)
        {
            _script = script;

            _title = new JSONStorableString("hardCollidersTitle", "");
            _baseForceInfoText = new JSONStorableString("hardCollidersBaseForceInfoText", "");
            _mainInfoText = new JSONStorableString("hardCollidersInfoText", "");
            _selectColliderText = new JSONStorableString("selectColliderText", "");
            _scalingHeaderText = new JSONStorableString("colliderScalingHeaderText", "");
            _scalingInfoText = new JSONStorableString("colliderScalingInfoText", "");
            _centerHeaderText = new JSONStorableString("colliderCenterHeaderText", "");
            _centerInfoText = new JSONStorableString("colliderCenterInfoText", "");

            _baseForceInfoText.val = "\n".Size(12) +
                "Base force multiplier combined with the collider's own force multiplier produce the total force for that collider.";
            _mainInfoText.val = "\n".Size(12) +
                "Hard colliders make breasts move as a uniform shape when touched." +
                "\n\nThey make breasts both easier to move and better at maintaining their volume." +
                "\n\nThe end result also depends on collision force, collider size and shape," +
                " but also on the amount of morphing and on breast physics settings." +
                "\n\nThey have almost no impact on animation that doesn't involve collision.";
            _selectColliderText.val = "Select a collider above to continue.";
            _scalingInfoText.val = "\n".Size(12) +
                "Adjust the size and shape of the selected collider." +
                "\n\nThe closer the collider is to the skin, the more easily it will react to touch.";
            _centerInfoText.val = "\n".Size(12) +
                "Adjust the position of the selected collider." +
                "\n\nCombined with the size and chape, the position determines how well the collider fits inside the breast.";
        }

        public void Rebuild(UnityAction backButtonListener)
        {
            _elements = new Dictionary<string, UIDynamic>();

            CreateBackButton(backButtonListener, false);
            CreateTitle(false);
            CreateColliderGroupChooser(true);

            CreateCombinedColliderForceSlider(false, spacing: 8);

            CreateHeader(_scalingHeaderText, "Scaling Offsets", false);
            CreateScalingInfoTextArea(false);
            CreateHeader(_centerHeaderText, "Center Offsets", false, spacing: 10);
            CreateCenterInfoTextArea(false);

            CreateShowHardCollidersChooser(false, spacing: 35);
            CreateXRayVisualizationToggle(false);
            AddShowPreviewsPopupChangeHandler();
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

        private void CreateTitle(bool rightSide)
        {
            var textField = UIHelpers.TitleTextField(_script, _title, "Configure Hard Colliders", 62, rightSide);
            textField.UItext.fontSize = 32;
            _elements[_title.name] = textField;
        }

        private void CreateColliderGroupChooser(bool rightSide, int spacing = 0)
        {
            var storable = _script.hardColliderHandler.colliderGroupsJsc;
            _elements[$"{storable.name}Spacer"] = _script.NewSpacer(spacing, rightSide);

            var chooser = _script.CreatePopupAuto(storable, rightSide, 360f);
            chooser.popup.labelText.color = Color.black;

            _elements[storable.name] = chooser;
        }

        private void CreateCombinedColliderForceSlider(bool rightSide, int spacing = 0)
        {
            var storable = _script.hardColliderHandler.baseForceJsf;
            _elements[$"{storable.name}Spacer"] = _script.NewSpacer(spacing, rightSide);

            var slider = _script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.label = "Base Force Multiplier";
            slider.AddSliderClickMonitor();
            _elements[storable.name] = slider;
        }

        private void CreateHeader(JSONStorableString storable, string text, bool rightSide, int spacing = 0)
        {
            _elements[$"{storable.name}Spacer"] = _script.NewSpacer(spacing, rightSide);
            _elements[storable.name] = UIHelpers.HeaderTextField(_script, storable, text, rightSide);
        }

        private void CreateScalingInfoTextArea(bool rightSide, int spacing = 0)
        {
            var storable = _scalingInfoText;
            _elements[$"{storable.name}Spacer"] = _script.NewSpacer(spacing, rightSide);

            var textField = _script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.height = 187;
            _elements[storable.name] = textField;
        }

        private void CreateCenterInfoTextArea(bool rightSide, int spacing = 0)
        {
            var storable = _centerInfoText;
            _elements[$"{storable.name}Spacer"] = _script.NewSpacer(spacing, rightSide);

            var textField = _script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.height = 222;
            _elements[storable.name] = textField;
        }

        private void CreateShowHardCollidersChooser(bool rightSide, int spacing = 0)
        {
            var storable = _script.colliderVisualizer.GroupsJSON;
            _elements[$"{storable.name}Spacer"] = _script.NewSpacer(spacing, rightSide);

            var chooser = _script.CreatePopup(storable, rightSide);
            chooser.label = "Show Previews";
            chooser.popup.labelText.color = Color.black;

            chooser.popup.popupPanel.offsetMin += new Vector2(5, chooser.popup.popupPanelHeight + 110);
            chooser.popup.popupPanel.offsetMax += new Vector2(5, chooser.popup.popupPanelHeight + 110);

            _elements[storable.name] = chooser;
        }

        private void CreateXRayVisualizationToggle(bool rightSide, int spacing = 0)
        {
            var storable = _script.colliderVisualizer.XRayPreviewsJSON;
            _elements[$"{storable.name}Spacer"] = _script.NewSpacer(spacing, rightSide);

            var toggle = _script.CreateToggle(storable, rightSide);
            toggle.height = 52;
            toggle.label = "Use XRay Previews";
            _elements[storable.name] = toggle;
        }

        // ReSharper disable MemberCanBePrivate.Global
        public List<UIDynamicSlider> GetSliders() => GetSliders(_elements);
        public List<UIDynamicSlider> GetColliderSectionSliders() => GetSliders(_colliderSectionElements);

        // ReSharper restore MemberCanBePrivate.Global

        private static List<UIDynamicSlider> GetSliders(Dictionary<string, UIDynamic> elements) => elements.Aggregate(
            new List<UIDynamicSlider>(),
            (list, element) =>
            {
                var uiDynamicSlider = element.Value as UIDynamicSlider;
                if(uiDynamicSlider != null)
                {
                    list.Add(uiDynamicSlider);
                }

                return list;
            }
        );

        public void Clear()
        {
            GetSliders().ForEach(slider => Object.Destroy(slider.GetSliderClickMonitor()));
            _elements.ToList().ForEach(element => _script.RemoveElement(element.Value));
            ClearColliderSection();
            RemoveColliderPopupChangeHandler();
            RemoveShowPreviewsPopupChangeHandler();
            _script.colliderVisualizer.ShowPreviewsJSON.val = false;
            _script.colliderVisualizer.enabled = false;
        }

        private void ClearColliderSection()
        {
            GetColliderSectionSliders()
                .ForEach(slider => Object.Destroy(slider.GetSliderClickMonitor()));
            _colliderSectionElements
                .ToList()
                .ForEach(element => _script.RemoveElement(element.Value));
        }

        private void RemoveColliderPopupChangeHandler()
        {
            var element = _elements[_script.hardColliderHandler.colliderGroupsJsc.name];
            var uiDynamicPopup = element as UIDynamicPopup;
            if(uiDynamicPopup != null)
            {
                uiDynamicPopup.popup.visible = false;
                uiDynamicPopup.popup.onValueChangeHandlers -= OnColliderPopupValueChanged;
            }
        }

        private void AddShowPreviewsPopupChangeHandler()
        {
            var element = _elements[_script.colliderVisualizer.GroupsJSON.name];
            var uiDynamicPopup = element as UIDynamicPopup;
            if(uiDynamicPopup != null)
            {
                uiDynamicPopup.popup.onValueChangeHandlers += OnShowPreviewsPopupValueChanged;

                _elements[_script.colliderVisualizer.XRayPreviewsJSON.name]
                    .SetActiveStyle(uiDynamicPopup.popup.currentValue != "Off");
            }
        }

        private void RemoveShowPreviewsPopupChangeHandler()
        {
            var element = _elements[_script.colliderVisualizer.GroupsJSON.name];
            var uiDynamicPopup = element as UIDynamicPopup;
            if(uiDynamicPopup != null)
            {
                uiDynamicPopup.popup.visible = false;
                uiDynamicPopup.popup.onValueChangeHandlers -= OnShowPreviewsPopupValueChanged;
            }
        }

        public void ClosePopups()
        {
            foreach(var element in _elements)
            {
                var uiDynamicPopup = element.Value as UIDynamicPopup;
                if(uiDynamicPopup != null)
                {
                    uiDynamicPopup.popup.visible = false;
                }
            }
        }

        private void OnShowPreviewsPopupValueChanged(string value)
        {
            var popupElement = _elements[_script.colliderVisualizer.GroupsJSON.name];
            var uiDynamicPopup = popupElement as UIDynamicPopup;
            if(uiDynamicPopup != null)
            {
                uiDynamicPopup.popup.labelTextColor = value == "Off" ? Color.red : Color.black;
            }

            _elements[_script.colliderVisualizer.XRayPreviewsJSON.name].SetActiveStyle(value != "Off");
        }

        public void OnColliderPopupValueChanged(string value)
        {
            ClearColliderSection();
            RebuildColliderSection(value);
        }

        public void RebuildColliderSection(string colliderId)
        {
            _colliderSectionElements = new Dictionary<string, UIDynamic>();
            if(colliderId == HardColliderHandler.ALL_OPTION)
            {
                CreateBaseForceInfoTextArea(true, spacing: 15);
                CreateHardCollidersInfoTextArea(true, spacing: 50);
                CreateSelectColliderTextArea(true, spacing: 15);
            }
            else
            {
                _selectColliderText.val = "";

                var colliderConfigGroup = _script.hardColliderHandler.colliderConfigs
                    .Find(config => config.visualizerEditableId == colliderId);

                CreateColliderForceSlider(colliderConfigGroup.forceJsf, true, spacing: 15);
                CreateColliderRadiusSlider(colliderConfigGroup.radiusJsf, true, spacing: 15);
                CreateColliderLengthSlider(colliderConfigGroup.lengthJsf, true);

                CreateColliderRightSlider(colliderConfigGroup.rightJsf, true, spacing: 15);
                CreateColliderUpSlider(colliderConfigGroup.upJsf, true);
                CreateColliderLookSlider(colliderConfigGroup.lookJsf, true);

                var baseSlider = _elements[_script.hardColliderHandler.baseForceJsf.name];
                baseSlider.AddListener(UpdateAllSliderColors);
                UpdateAllSliderColors(0);
            }
        }

        private void CreateBaseForceInfoTextArea(bool rightSide, int spacing = 0)
        {
            var storable = _baseForceInfoText;
            _colliderSectionElements[$"{storable.name}Spacer"] = _script.NewSpacer(spacing, rightSide);

            var textField = _script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.height = 120;
            _colliderSectionElements[storable.name] = textField;
        }

        private void CreateHardCollidersInfoTextArea(bool rightSide, int spacing = 0)
        {
            var storable = _mainInfoText;
            _colliderSectionElements[$"{storable.name}Spacer"] = _script.NewSpacer(spacing, rightSide);

            var textField = _script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.height = 450;
            textField.backgroundColor = Color.clear;
            _colliderSectionElements[storable.name] = textField;
        }

        private void CreateSelectColliderTextArea(bool rightSide, int spacing = 0)
        {
            var storable = _selectColliderText;
            _colliderSectionElements[$"{storable.name}Spacer"] = _script.NewSpacer(spacing, rightSide);

            var textField = _script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.UItext.alignment = TextAnchor.UpperCenter;
            textField.height = 100;
            textField.backgroundColor = Color.clear;
            _colliderSectionElements[storable.name] = textField;
        }

        private void CreateColliderForceSlider(JSONStorableFloat storable, bool rightSide, int spacing = 0)
        {
            _colliderSectionElements[$"{storable.name}Spacer"] = _script.NewSpacer(spacing, rightSide);
            var slider = _script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.label = "Collision Force Multiplier";
            slider.AddSliderClickMonitor();
            slider.AddListener((float value) => UpdateSliderColor(storable));
            _colliderSectionElements[storable.name] = slider;
        }

        private void CreateColliderRadiusSlider(JSONStorableFloat storable, bool rightSide, int spacing = 0)
        {
            _colliderSectionElements[$"{storable.name}Spacer"] = _script.NewSpacer(spacing, rightSide);
            var slider = _script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.label = "Radius";
            _colliderSectionElements[storable.name] = slider;
        }

        private void CreateColliderLengthSlider(JSONStorableFloat storable, bool rightSide, int spacing = 0)
        {
            _colliderSectionElements[$"{storable.name}Spacer"] = _script.NewSpacer(spacing, rightSide);
            var slider = _script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.label = "Length";
            _colliderSectionElements[storable.name] = slider;
        }

        private void CreateColliderRightSlider(JSONStorableFloat storable, bool rightSide, int spacing = 0)
        {
            _colliderSectionElements[$"{storable.name}Spacer"] = _script.NewSpacer(spacing, rightSide);
            var slider = _script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.label = "X Offset";
            _colliderSectionElements[storable.name] = slider;
        }

        private void CreateColliderUpSlider(JSONStorableFloat storable, bool rightSide, int spacing = 0)
        {
            _colliderSectionElements[$"{storable.name}Spacer"] = _script.NewSpacer(spacing, rightSide);
            var slider = _script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.label = "Y Offset";
            _colliderSectionElements[storable.name] = slider;
        }

        private void CreateColliderLookSlider(JSONStorableFloat storable, bool rightSide, int spacing = 0)
        {
            _colliderSectionElements[$"{storable.name}Spacer"] = _script.NewSpacer(spacing, rightSide);
            var slider = _script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.label = "Z Offset";
            _colliderSectionElements[storable.name] = slider;
        }

        private void UpdateAllSliderColors(float value)
        {
            foreach(var config in _script.hardColliderHandler.colliderConfigs)
            {
                if(_colliderSectionElements.ContainsKey(config.forceJsf.name))
                {
                    UpdateSliderColor(config.forceJsf);
                }
            }
        }

        private void UpdateSliderColor(JSONStorableFloat storable)
        {
            var slider = (UIDynamicSlider) _colliderSectionElements[storable.name];
            var images = slider.slider.gameObject.transform.GetComponentsInChildren<Image>();
            var fillImage = images.First(image => image.name == "Fill");
            var handleImage = images.First(image => image.name == "Handle");
            var color = MultiplierSliderColor(_script.hardColliderHandler.baseForceJsf.val * storable.val);
            fillImage.color = color;
            handleImage.color = color;
        }

        private static Color MultiplierSliderColor(float value)
        {
            if(value <= 1 / 4f)
            {
                return Color.Lerp(new Color(1, 1, 1, 0.25f), Color.white, 4 * value);
            }

            if(value <= 1 / 3f)
            {
                return Color.white;
            }

            if(value <= 2 / 3f)
            {
                return Color.Lerp(Color.white, new Color(1.0f, 0.6f, 0.2f), Mathf.InverseLerp(1 / 3f, 2 / 3f, value));
            }

            return Color.Lerp(new Color(1.0f, 0.6f, 0.2f), new Color(1.0f, 0.2f, 0.2f), Mathf.InverseLerp(2 / 3f, 1, value));
        }
    }
}
