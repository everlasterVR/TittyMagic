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

        private readonly UnityAction _onReturnToParent;

        public HardCollidersWindow(string id, UnityAction onReturnToParent) : base(id)
        {
            _onReturnToParent = onReturnToParent;
            _colliderSectionElements = new Dictionary<string, UIDynamic>();
            _colliderInfoText = new JSONStorableString("colliderInfoText", "");
        }

        protected override void OnBuild()
        {
            CreateBackButton(false, _onReturnToParent);

            CreateTitleTextField(
                new JSONStorableString("hardCollidersTitle", "\n".Size(12) + "Configure Hard Colliders".Bold()),
                fontSize: 32,
                height: 62,
                rightSide: false
            );

            /* Collider group chooser */
            {
                var storable = HardColliderHandler.colliderGroupsJsc;
                var chooser = tittyMagic.CreatePopupAuto(storable, true, 360f);
                chooser.popup.labelText.color = Color.black;
                elements[storable.name] = chooser;
            }

            /* Collision force slider */
            {
                var storable = HardColliderHandler.baseForceJsf;
                elements[$"{storable.name}Spacer"] = tittyMagic.NewSpacer(8);
                var slider = tittyMagic.CreateSlider(storable);
                slider.valueFormat = "F2";
                slider.label = "Base Collision Force";
                elements[storable.name] = slider;
            }

            /* Scaling info text area */
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
                elements[$"{_colliderInfoText.name}Spacer"] = tittyMagic.NewSpacer(10);

                var textField = tittyMagic.CreateTextField(_colliderInfoText);
                textField.UItext.fontSize = 28;
                textField.height = 550;
                textField.backgroundColor = Color.clear;
                elements[_colliderInfoText.name] = textField;
            }

            /* Show hard colliders chooser */
            {
                var storable = _visualizer.GroupsJSON;
                elements[$"{storable.name}Spacer"] = tittyMagic.NewSpacer(10);

                var chooser = tittyMagic.CreatePopup(storable);
                chooser.label = "Show Previews";
                chooser.popup.labelText.color = Color.black;
                chooser.popup.popupPanel.offsetMin += new Vector2(5, chooser.popup.popupPanelHeight + 110);
                chooser.popup.popupPanel.offsetMax += new Vector2(5, chooser.popup.popupPanelHeight + 110);
                elements[storable.name] = chooser;
            }

            /* Xray visualization toggle */
            {
                var storable = _visualizer.XRayPreviewsJSON;
                var toggle = tittyMagic.CreateToggle(storable);
                toggle.height = 52;
                toggle.label = "Use XRay Previews";
                elements[storable.name] = toggle;
            }

            /* Highlight all toggle */
            {
                var storable = HardColliderHandler.highlightAllJsb;
                var toggle = tittyMagic.CreateToggle(storable);
                toggle.height = 52;
                toggle.label = "Highlight All Colliders";

                elements[storable.name] = toggle;
            }

            /* Show previews popup change handler */
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

            _visualizer.enabled = true;
            _visualizer.ShowPreviewsJSON.val = true;

            /* Collider popup change handler */
            {
                var element = elements[HardColliderHandler.colliderGroupsJsc.name];
                var uiDynamicPopup = element as UIDynamicPopup;
                if(uiDynamicPopup != null)
                {
                    uiDynamicPopup.popup.onValueChangeHandlers += OnColliderPopupValueChanged;
                    RebuildColliderSection(HardColliderHandler.colliderGroupsJsc.val);
                }
            }
        }

        protected override void OnClose()
        {
            _visualizer.ShowPreviewsJSON.val = false;
            _visualizer.enabled = false;
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

        private void OnColliderPopupValueChanged(string value)
        {
            ClearColliderSection();
            RebuildColliderSection(value);
        }

        private void RebuildColliderSection(string colliderId) //TODO?
        {
            var hardColliderGroup = HardColliderHandler.hardColliderGroups
                .Find(group => group.visualizerId == colliderId);

            /* Collision force slider */
            {
                var storable = hardColliderGroup.forceJsf;
                _colliderSectionElements[$"{storable.name}Spacer"] = tittyMagic.NewSpacer(15, true);
                var slider = tittyMagic.CreateSlider(storable, true);
                slider.valueFormat = "F2";
                slider.label = "Collision Force Multiplier";
                slider.AddListener((float value) => UpdateSliderColor(storable));
                _colliderSectionElements[storable.name] = slider;
            }

            /* Collider radius slider */
            {
                var storable = hardColliderGroup.radiusJsf;
                _colliderSectionElements[$"{storable.name}Spacer"] = tittyMagic.NewSpacer(15, true);
                var slider = tittyMagic.CreateSlider(storable, true);
                slider.valueFormat = "F2";
                slider.label = "Radius Offset";
                _colliderSectionElements[storable.name] = slider;
            }

            /* Collider right offset slider */
            {
                var storable = hardColliderGroup.rightJsf;
                _colliderSectionElements[$"{storable.name}Spacer"] = tittyMagic.NewSpacer(15, true);
                var slider = tittyMagic.CreateSlider(storable, true);
                slider.valueFormat = "F2";
                slider.label = "X Offset";
                _colliderSectionElements[storable.name] = slider;
            }

            /* Collider up offset slider */
            {
                var storable = hardColliderGroup.upJsf;
                var slider = tittyMagic.CreateSlider(storable, true);
                slider.valueFormat = "F2";
                slider.label = "Y Offset";
                _colliderSectionElements[storable.name] = slider;
            }

            /* Collider look offset slider */
            {
                var storable = hardColliderGroup.lookJsf;
                var slider = tittyMagic.CreateSlider(storable, true);
                slider.valueFormat = "F2";
                slider.label = "Z Offset";
                _colliderSectionElements[storable.name] = slider;
            }

            var baseSlider = elements[HardColliderHandler.baseForceJsf.name];
            baseSlider.AddListener(UpdateAllSliderColors);
            UpdateAllSliderColors(0);
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

            /* Remove collider popup change handler */
            {
                var element = elements[HardColliderHandler.colliderGroupsJsc.name];
                var uiDynamicPopup = element as UIDynamicPopup;
                if(uiDynamicPopup != null)
                {
                    uiDynamicPopup.popup.visible = false;
                    uiDynamicPopup.popup.onValueChangeHandlers -= OnColliderPopupValueChanged;
                }
            }

            /* Remove show previews popup change handler */
            {
                var element = elements[_visualizer.GroupsJSON.name];
                var uiDynamicPopup = element as UIDynamicPopup;
                if(uiDynamicPopup != null)
                {
                    uiDynamicPopup.popup.visible = false;
                    uiDynamicPopup.popup.onValueChangeHandlers -= OnShowPreviewsPopupValueChanged;
                }
            }
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
