using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using Image = UnityEngine.UI.Image;

namespace TittyMagic.UI
{
    internal class HardCollidersWindow : WindowBase, IWindow
    {
        private readonly UnityAction _returnToParent;

        public Dictionary<string, UIDynamic> colliderSectionElements { get; }

        private readonly JSONStorableString _colliderInfoText;

        public HardCollidersWindow(Script script, UnityAction onReturnToParent) : base(script)
        {
            buildAction = BuildSelf;
            closeAction = ActionsOnClose;
            colliderSectionElements = new Dictionary<string, UIDynamic>();

            _returnToParent = () =>
            {
                Clear();
                onReturnToParent();
            };
            _colliderInfoText = new JSONStorableString("colliderInfoText", "");
        }

        private void BuildSelf()
        {
            CreateBackButton(false);
            CreateTitle(false);
            CreateColliderGroupChooser(true);

            CreateCombinedColliderForceSlider(false, spacing: 8);

            CreateScalingInfoTextArea(false, spacing: 10);

            CreateShowHardCollidersChooser(false, spacing: 10);
            CreateXRayVisualizationToggle(false);
            CreateHighlightAllToggle(false);
            AddShowPreviewsPopupChangeHandler();

            script.colliderVisualizer.enabled = true;
            script.colliderVisualizer.ShowPreviewsJSON.val = true;
            script.hardColliderHandler.SyncAllOffsets();
            AddColliderPopupChangeHandler();
        }

        private void CreateBackButton(bool rightSide)
        {
            var button = script.CreateButton("Return", rightSide);

            button.textColor = Color.white;
            var colors = button.button.colors;
            colors.normalColor = UIHelpers.sliderGray;
            colors.highlightedColor = Color.grey;
            colors.pressedColor = Color.grey;
            button.button.colors = colors;

            button.AddListener(_returnToParent);
            elements["backButton"] = button;
        }

        private void CreateTitle(bool rightSide)
        {
            var storable = new JSONStorableString("hardCollidersTitle", "");
            var textField = UIHelpers.TitleTextField(script, storable, "Configure Hard Colliders", 62, rightSide);
            textField.UItext.fontSize = 32;
            elements[storable.name] = textField;
        }

        private void CreateColliderGroupChooser(bool rightSide, int spacing = 0)
        {
            var storable = script.hardColliderHandler.colliderGroupsJsc;
            elements[$"{storable.name}Spacer"] = script.NewSpacer(spacing, rightSide);

            var chooser = script.CreatePopupAuto(storable, rightSide, 360f);
            chooser.popup.labelText.color = Color.black;

            elements[storable.name] = chooser;
        }

        private void CreateCombinedColliderForceSlider(bool rightSide, int spacing = 0)
        {
            var storable = script.hardColliderHandler.baseForceJsf;
            elements[$"{storable.name}Spacer"] = script.NewSpacer(spacing, rightSide);

            var slider = script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.label = "Base Collision Force";
            slider.AddSliderClickMonitor();
            elements[storable.name] = slider;
        }

        private void CreateScalingInfoTextArea(bool rightSide, int spacing = 0)
        {
            var sb = new StringBuilder();
            sb.Append("<b><i>Collision force</i></b> determines how easily collision");
            sb.Append(" causes breasts to move.");
            sb.Append("\n\n");
            sb.Append("<b><i>Radius</i></b> and <b><i>length</i></b> adjust the size and");
            sb.Append(" shape of the selected collider.");
            sb.Append("\n\n");
            sb.Append("<b><i>X, Y and Z offsets</i></b> can be used to modify the position");
            sb.Append(" of the selected collider.");
            sb.Append("\n\n");
            sb.Append("The closer the collider matches the volume of the breast, the more");
            sb.Append(" accurately it responds to touch.");

            _colliderInfoText.val = sb.ToString();
            elements[$"{_colliderInfoText.name}Spacer"] = script.NewSpacer(spacing, rightSide);

            var textField = script.CreateTextField(_colliderInfoText, rightSide);
            textField.UItext.fontSize = 28;
            textField.height = 550;
            textField.backgroundColor = Color.clear;
            elements[_colliderInfoText.name] = textField;
        }

        private void CreateShowHardCollidersChooser(bool rightSide, int spacing = 0)
        {
            var storable = script.colliderVisualizer.GroupsJSON;
            elements[$"{storable.name}Spacer"] = script.NewSpacer(spacing, rightSide);

            var chooser = script.CreatePopup(storable, rightSide);
            chooser.label = "Show Previews";
            chooser.popup.labelText.color = Color.black;

            chooser.popup.popupPanel.offsetMin += new Vector2(5, chooser.popup.popupPanelHeight + 110);
            chooser.popup.popupPanel.offsetMax += new Vector2(5, chooser.popup.popupPanelHeight + 110);

            elements[storable.name] = chooser;
        }

        private void CreateXRayVisualizationToggle(bool rightSide, int spacing = 0)
        {
            var storable = script.colliderVisualizer.XRayPreviewsJSON;
            elements[$"{storable.name}Spacer"] = script.NewSpacer(spacing, rightSide);

            var toggle = script.CreateToggle(storable, rightSide);
            toggle.height = 52;
            toggle.label = "Use XRay Previews";

            elements[storable.name] = toggle;
        }

        private void CreateHighlightAllToggle(bool rightSide, int spacing = 0)
        {
            var storable = script.hardColliderHandler.highlightAllJsb;
            elements[$"{storable.name}Spacer"] = script.NewSpacer(spacing, rightSide);

            var toggle = script.CreateToggle(storable, rightSide);
            toggle.height = 52;
            toggle.label = "Highlight All Colliders";

            elements[storable.name] = toggle;
        }

        private void AddShowPreviewsPopupChangeHandler()
        {
            var element = elements[script.colliderVisualizer.GroupsJSON.name];
            var uiDynamicPopup = element as UIDynamicPopup;
            if(uiDynamicPopup != null)
            {
                uiDynamicPopup.popup.onValueChangeHandlers += OnShowPreviewsPopupValueChanged;

                elements[script.colliderVisualizer.XRayPreviewsJSON.name]
                    .SetActiveStyle(uiDynamicPopup.popup.currentValue != "Off");
            }
        }

        private void RemoveShowPreviewsPopupChangeHandler()
        {
            var element = elements[script.colliderVisualizer.GroupsJSON.name];
            var uiDynamicPopup = element as UIDynamicPopup;
            if(uiDynamicPopup != null)
            {
                uiDynamicPopup.popup.visible = false;
                uiDynamicPopup.popup.onValueChangeHandlers -= OnShowPreviewsPopupValueChanged;
            }
        }

        private void OnShowPreviewsPopupValueChanged(string value)
        {
            var popupElement = elements[script.colliderVisualizer.GroupsJSON.name];
            var uiDynamicPopup = popupElement as UIDynamicPopup;
            if(uiDynamicPopup != null)
            {
                uiDynamicPopup.popup.labelTextColor = value == "Off" ? Color.red : Color.black;
            }

            elements[script.colliderVisualizer.XRayPreviewsJSON.name].SetActiveStyle(value != "Off");
            elements[script.hardColliderHandler.highlightAllJsb.name].SetActiveStyle(value != "Off");
        }

        private void RemoveColliderPopupChangeHandler()
        {
            var element = elements[script.hardColliderHandler.colliderGroupsJsc.name];
            var uiDynamicPopup = element as UIDynamicPopup;
            if(uiDynamicPopup != null)
            {
                uiDynamicPopup.popup.visible = false;
                uiDynamicPopup.popup.onValueChangeHandlers -= OnColliderPopupValueChanged;
            }
        }

        private void AddColliderPopupChangeHandler()
        {
            var element = elements[script.hardColliderHandler.colliderGroupsJsc.name];
            var uiDynamicPopup = element as UIDynamicPopup;
            if(uiDynamicPopup != null)
            {
                uiDynamicPopup.popup.onValueChangeHandlers += OnColliderPopupValueChanged;
                RebuildColliderSection(script.hardColliderHandler.colliderGroupsJsc.val);
            }
        }

        private void OnColliderPopupValueChanged(string value)
        {
            ClearColliderSection();
            RebuildColliderSection(value);
        }

        private void RebuildColliderSection(string colliderId)
        {
            var colliderConfigGroup = script.hardColliderHandler.colliderConfigs
                .Find(config => config.visualizerEditableId == colliderId);

            CreateColliderForceSlider(colliderConfigGroup.forceJsf, true, spacing: 15);
            CreateColliderRadiusSlider(colliderConfigGroup.radiusJsf, true, spacing: 15);
            CreateColliderLengthSlider(colliderConfigGroup.lengthJsf, true);

            CreateColliderRightSlider(colliderConfigGroup.rightJsf, true, spacing: 15);
            CreateColliderUpSlider(colliderConfigGroup.upJsf, true);
            CreateColliderLookSlider(colliderConfigGroup.lookJsf, true);

            var baseSlider = elements[script.hardColliderHandler.baseForceJsf.name];
            baseSlider.AddListener(UpdateAllSliderColors);
            UpdateAllSliderColors(0);
        }

        private void CreateColliderForceSlider(JSONStorableFloat storable, bool rightSide, int spacing = 0)
        {
            colliderSectionElements[$"{storable.name}Spacer"] = script.NewSpacer(spacing, rightSide);
            var slider = script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.label = "Collision Force Multiplier";
            slider.AddSliderClickMonitor();
            slider.AddListener((float value) => UpdateSliderColor(storable));
            colliderSectionElements[storable.name] = slider;
        }

        private void CreateColliderRadiusSlider(JSONStorableFloat storable, bool rightSide, int spacing = 0)
        {
            colliderSectionElements[$"{storable.name}Spacer"] = script.NewSpacer(spacing, rightSide);
            var slider = script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.label = "Radius";
            colliderSectionElements[storable.name] = slider;
        }

        private void CreateColliderLengthSlider(JSONStorableFloat storable, bool rightSide, int spacing = 0)
        {
            colliderSectionElements[$"{storable.name}Spacer"] = script.NewSpacer(spacing, rightSide);
            var slider = script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.label = "Length";
            colliderSectionElements[storable.name] = slider;
        }

        private void CreateColliderRightSlider(JSONStorableFloat storable, bool rightSide, int spacing = 0)
        {
            colliderSectionElements[$"{storable.name}Spacer"] = script.NewSpacer(spacing, rightSide);
            var slider = script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.label = "X Offset";
            colliderSectionElements[storable.name] = slider;
        }

        private void CreateColliderUpSlider(JSONStorableFloat storable, bool rightSide, int spacing = 0)
        {
            colliderSectionElements[$"{storable.name}Spacer"] = script.NewSpacer(spacing, rightSide);
            var slider = script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.label = "Y Offset";
            colliderSectionElements[storable.name] = slider;
        }

        private void CreateColliderLookSlider(JSONStorableFloat storable, bool rightSide, int spacing = 0)
        {
            colliderSectionElements[$"{storable.name}Spacer"] = script.NewSpacer(spacing, rightSide);
            var slider = script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.label = "Z Offset";
            colliderSectionElements[storable.name] = slider;
        }

        private void UpdateAllSliderColors(float value)
        {
            foreach(var config in script.hardColliderHandler.colliderConfigs)
            {
                if(colliderSectionElements.ContainsKey(config.forceJsf.name))
                {
                    UpdateSliderColor(config.forceJsf);
                }
            }
        }

        private void UpdateSliderColor(JSONStorableFloat storable)
        {
            var slider = (UIDynamicSlider) colliderSectionElements[storable.name];
            var images = slider.slider.gameObject.transform.GetComponentsInChildren<Image>();
            var fillImage = images.First(image => image.name == "Fill");
            var handleImage = images.First(image => image.name == "Handle");
            var color = MultiplierSliderColor(script.hardColliderHandler.baseForceJsf.val * storable.val);
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
            GetSliders().ForEach(slider => Object.Destroy(slider.GetSliderClickMonitor()));

            base.Clear();
            ClearColliderSection();

            RemoveColliderPopupChangeHandler();
            RemoveShowPreviewsPopupChangeHandler();
        }

        private List<UIDynamicSlider> GetColliderSectionSliders() => colliderSectionElements.Aggregate(
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

        private void ClearColliderSection()
        {
            GetColliderSectionSliders()
                .ForEach(slider => Object.Destroy(slider.GetSliderClickMonitor()));
            colliderSectionElements
                .ToList()
                .ForEach(element => script.RemoveElement(element.Value));
            colliderSectionElements.Clear();
        }

        private void ActionsOnClose()
        {
            script.colliderVisualizer.ShowPreviewsJSON.val = false;
            script.colliderVisualizer.enabled = false;
        }

        public void UpdateCollidersDebugInfo(string configId)
        {
            var debugColliderConfigGroup = script.hardColliderHandler.colliderConfigs.Find(config => config.id == configId);
            var debugColliderConfig = debugColliderConfigGroup.left;
            var capsuleCollider = (CapsuleCollider) debugColliderConfig.autoCollider.jointCollider;
            var center = capsuleCollider.center;
            var position = Calc.RelativePosition(debugColliderConfig.autoCollider.jointRB, center);
            _colliderInfoText.val = $"{configId}" +
                $"\n{Calc.RoundToDecimals(center.x, 1000f)}" +
                $" {Calc.RoundToDecimals(center.y, 1000f)}" +
                $" {Calc.RoundToDecimals(center.z, 1000f)}" +
                $"\n{Calc.RoundToDecimals(position.x, 1000f)}" +
                $" {Calc.RoundToDecimals(position.y, 1000f)}" +
                $" {Calc.RoundToDecimals(position.z, 1000f)}";
        }
    }
}
