using System.Collections.Generic;
using System.Linq;
using System.Text;
using TittyMagic.Handlers;
using UnityEngine;
using UnityEngine.Events;
using Image = UnityEngine.UI.Image;
using static TittyMagic.Script;

namespace TittyMagic.UI
{
    public class HardCollidersWindow : WindowBase, IWindow
    {
        private readonly Dictionary<string, UIDynamic> _colliderSectionElements;

        private readonly JSONStorableString _colliderInfoText;

        private readonly ColliderVisualizer _visualizer = HardColliderHandler.colliderVisualizer;

        public HardCollidersWindow(string id, UnityAction onReturnToParent) : base(id)
        {
            buildAction = () =>
            {
                CreateBackButton(false);
                elements["backButton"].AddListener(onReturnToParent);
                CreateTitle(false);
                CreateColliderGroupChooser(true);

                CreateBaseCollisionForceSlider(false, spacing: 8);

                CreateScalingInfoTextArea(false, spacing: 10);

                CreateShowHardCollidersChooser(false, spacing: 10);
                CreateXRayVisualizationToggle(false);
                CreateHighlightAllToggle(false);
                AddShowPreviewsPopupChangeHandler();

                _visualizer.enabled = true;
                _visualizer.ShowPreviewsJSON.val = true;
                AddColliderPopupChangeHandler();
            };

            closeAction = () =>
            {
                _visualizer.ShowPreviewsJSON.val = false;
                _visualizer.enabled = false;
            };

            _colliderSectionElements = new Dictionary<string, UIDynamic>();
            _colliderInfoText = new JSONStorableString("colliderInfoText", "");
        }

        private void CreateTitle(bool rightSide)
        {
            var storable = new JSONStorableString("hardCollidersTitle", "");
            var textField = UIHelpers.TitleTextField(storable, "Configure Hard Colliders", 62, rightSide);
            textField.UItext.fontSize = 32;
            elements[storable.name] = textField;
        }

        private void CreateColliderGroupChooser(bool rightSide, int spacing = 0)
        {
            var storable = HardColliderHandler.colliderGroupsJsc;
            elements[$"{storable.name}Spacer"] = tittyMagic.NewSpacer(spacing, rightSide);

            var chooser = tittyMagic.CreatePopupAuto(storable, rightSide, 360f);
            chooser.popup.labelText.color = Color.black;

            elements[storable.name] = chooser;
        }

        private void CreateBaseCollisionForceSlider(bool rightSide, int spacing = 0)
        {
            var storable = HardColliderHandler.baseForceJsf;
            elements[$"{storable.name}Spacer"] = tittyMagic.NewSpacer(spacing, rightSide);

            var slider = tittyMagic.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.label = "Base Collision Force";
            elements[storable.name] = slider;
        }

        private void CreateScalingInfoTextArea(bool rightSide, int spacing = 0)
        {
            var sb = new StringBuilder();
            sb.Append("<b><i>Collision force</i></b> determines how easily collision");
            sb.Append(" causes breasts to move.");
            sb.Append("\n\n");
            sb.Append("<b><i>Radius</i></b> adjusts the size of the selected collider.");
            sb.Append("\n\n");
            sb.Append("<b><i>X, Y and Z offsets</i></b> can be used to modify the position");
            sb.Append(" of the selected collider.");
            sb.Append("\n\n");
            sb.Append("The closer the collider matches the volume of the breast, the more");
            sb.Append(" accurately it responds to touch.");

            _colliderInfoText.val = sb.ToString();
            elements[$"{_colliderInfoText.name}Spacer"] = tittyMagic.NewSpacer(spacing, rightSide);

            var textField = tittyMagic.CreateTextField(_colliderInfoText, rightSide);
            textField.UItext.fontSize = 28;
            textField.height = 550;
            textField.backgroundColor = Color.clear;
            elements[_colliderInfoText.name] = textField;
        }

        private void CreateShowHardCollidersChooser(bool rightSide, int spacing = 0)
        {
            var storable = _visualizer.GroupsJSON;
            elements[$"{storable.name}Spacer"] = tittyMagic.NewSpacer(spacing, rightSide);

            var chooser = tittyMagic.CreatePopup(storable, rightSide);
            chooser.label = "Show Previews";
            chooser.popup.labelText.color = Color.black;

            chooser.popup.popupPanel.offsetMin += new Vector2(5, chooser.popup.popupPanelHeight + 110);
            chooser.popup.popupPanel.offsetMax += new Vector2(5, chooser.popup.popupPanelHeight + 110);

            elements[storable.name] = chooser;
        }

        private void CreateXRayVisualizationToggle(bool rightSide, int spacing = 0)
        {
            var storable = _visualizer.XRayPreviewsJSON;
            elements[$"{storable.name}Spacer"] = tittyMagic.NewSpacer(spacing, rightSide);

            var toggle = tittyMagic.CreateToggle(storable, rightSide);
            toggle.height = 52;
            toggle.label = "Use XRay Previews";

            elements[storable.name] = toggle;
        }

        private void CreateHighlightAllToggle(bool rightSide, int spacing = 0)
        {
            var storable = HardColliderHandler.highlightAllJsb;
            elements[$"{storable.name}Spacer"] = tittyMagic.NewSpacer(spacing, rightSide);

            var toggle = tittyMagic.CreateToggle(storable, rightSide);
            toggle.height = 52;
            toggle.label = "Highlight All Colliders";

            elements[storable.name] = toggle;
        }

        private void AddShowPreviewsPopupChangeHandler()
        {
            var element = elements[_visualizer.GroupsJSON.name];
            var uiDynamicPopup = element as UIDynamicPopup;
            if(uiDynamicPopup != null)
            {
                uiDynamicPopup.popup.onValueChangeHandlers += OnShowPreviewsPopupValueChanged;

                elements[_visualizer.XRayPreviewsJSON.name]
                    .SetActiveStyle(uiDynamicPopup.popup.currentValue != "Off");
            }
        }

        private void RemoveShowPreviewsPopupChangeHandler()
        {
            var element = elements[_visualizer.GroupsJSON.name];
            var uiDynamicPopup = element as UIDynamicPopup;
            if(uiDynamicPopup != null)
            {
                uiDynamicPopup.popup.visible = false;
                uiDynamicPopup.popup.onValueChangeHandlers -= OnShowPreviewsPopupValueChanged;
            }
        }

        private void OnShowPreviewsPopupValueChanged(string value)
        {
            var popupElement = elements[_visualizer.GroupsJSON.name];
            var uiDynamicPopup = popupElement as UIDynamicPopup;
            if(uiDynamicPopup != null)
            {
                uiDynamicPopup.popup.labelTextColor = value == "Off" ? Color.red : Color.black;
            }

            elements[_visualizer.XRayPreviewsJSON.name].SetActiveStyle(value != "Off");
            elements[HardColliderHandler.highlightAllJsb.name].SetActiveStyle(value != "Off");
        }

        private void RemoveColliderPopupChangeHandler()
        {
            var element = elements[HardColliderHandler.colliderGroupsJsc.name];
            var uiDynamicPopup = element as UIDynamicPopup;
            if(uiDynamicPopup != null)
            {
                uiDynamicPopup.popup.visible = false;
                uiDynamicPopup.popup.onValueChangeHandlers -= OnColliderPopupValueChanged;
            }
        }

        private void AddColliderPopupChangeHandler()
        {
            var element = elements[HardColliderHandler.colliderGroupsJsc.name];
            var uiDynamicPopup = element as UIDynamicPopup;
            if(uiDynamicPopup != null)
            {
                uiDynamicPopup.popup.onValueChangeHandlers += OnColliderPopupValueChanged;
                RebuildColliderSection(HardColliderHandler.colliderGroupsJsc.val);
            }
        }

        private void OnColliderPopupValueChanged(string value)
        {
            ClearColliderSection();
            RebuildColliderSection(value);
        }

        private void RebuildColliderSection(string colliderId)
        {
            var hardColliderGroup = HardColliderHandler.hardColliderGroups
                .Find(group => group.visualizerId == colliderId);

            CreateCollisionForceSlider(hardColliderGroup.forceJsf, true, spacing: 15);
            CreateColliderRadiusSlider(hardColliderGroup.radiusJsf, true, spacing: 15);

            CreateColliderRightSlider(hardColliderGroup.rightJsf, true, spacing: 15);
            CreateColliderUpSlider(hardColliderGroup.upJsf, true);
            CreateColliderLookSlider(hardColliderGroup.lookJsf, true);

            var baseSlider = elements[HardColliderHandler.baseForceJsf.name];
            baseSlider.AddListener(UpdateAllSliderColors);
            UpdateAllSliderColors(0);
        }

        private void CreateCollisionForceSlider(JSONStorableFloat storable, bool rightSide, int spacing = 0)
        {
            _colliderSectionElements[$"{storable.name}Spacer"] = tittyMagic.NewSpacer(spacing, rightSide);
            var slider = tittyMagic.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.label = "Collision Force Multiplier";
            slider.AddListener((float value) => UpdateSliderColor(storable));
            _colliderSectionElements[storable.name] = slider;
        }

        private void CreateColliderRadiusSlider(JSONStorableFloat storable, bool rightSide, int spacing = 0)
        {
            _colliderSectionElements[$"{storable.name}Spacer"] = tittyMagic.NewSpacer(spacing, rightSide);
            var slider = tittyMagic.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.label = "Radius Offset";
            _colliderSectionElements[storable.name] = slider;
        }

        private void CreateColliderRightSlider(JSONStorableFloat storable, bool rightSide, int spacing = 0)
        {
            _colliderSectionElements[$"{storable.name}Spacer"] = tittyMagic.NewSpacer(spacing, rightSide);
            var slider = tittyMagic.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.label = "X Offset";
            _colliderSectionElements[storable.name] = slider;
        }

        private void CreateColliderUpSlider(JSONStorableFloat storable, bool rightSide, int spacing = 0)
        {
            _colliderSectionElements[$"{storable.name}Spacer"] = tittyMagic.NewSpacer(spacing, rightSide);
            var slider = tittyMagic.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.label = "Y Offset";
            _colliderSectionElements[storable.name] = slider;
        }

        private void CreateColliderLookSlider(JSONStorableFloat storable, bool rightSide, int spacing = 0)
        {
            _colliderSectionElements[$"{storable.name}Spacer"] = tittyMagic.NewSpacer(spacing, rightSide);
            var slider = tittyMagic.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.label = "Z Offset";
            _colliderSectionElements[storable.name] = slider;
        }

        private void UpdateAllSliderColors(float value)
        {
            foreach(var group in HardColliderHandler.hardColliderGroups)
            {
                if(_colliderSectionElements.ContainsKey(group.forceJsf.name))
                {
                    UpdateSliderColor(group.forceJsf);
                }
            }
        }

        private void UpdateSliderColor(JSONStorableFloat storable)
        {
            var slider = (UIDynamicSlider) _colliderSectionElements[storable.name];
            var images = slider.slider.gameObject.transform.GetComponentsInChildren<Image>();
            var fillImage = images.First(image => image.name == "Fill");
            var handleImage = images.First(image => image.name == "Handle");
            var color = MultiplierSliderColor(HardColliderHandler.baseForceJsf.val * storable.val / 1.5f);
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

        public new void Clear()
        {
            base.Clear();
            ClearColliderSection();

            RemoveColliderPopupChangeHandler();
            RemoveShowPreviewsPopupChangeHandler();
        }

        private void ClearColliderSection()
        {
            _colliderSectionElements
                .ToList()
                .ForEach(element => tittyMagic.RemoveElement(element.Value));
            _colliderSectionElements.Clear();
        }
    }
}
