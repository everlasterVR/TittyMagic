using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TittyMagic.UI
{
    internal class WindowBase : IWindow
    {
        protected readonly Script script;
        protected Action buildAction;
        protected Action closeAction;

        public Dictionary<string, UIDynamic> GetElements() => elements;
        protected readonly Dictionary<string, UIDynamic> elements;

        protected readonly List<IWindow> nestedWindows;

        public IWindow GetActiveNestedWindow() => activeNestedWindow;
        protected IWindow activeNestedWindow;

        protected WindowBase(Script script)
        {
            this.script = script;
            elements = new Dictionary<string, UIDynamic>();
            nestedWindows = new List<IWindow>();
        }

        #region Common elements

        protected void AddSpacer(string name, int height, bool rightSide) =>
            elements[$"{name}Spacer"] = script.NewSpacer(height, rightSide);

        protected void CreateRecalibrateButton(JSONStorableAction storable, bool rightSide, int spacing = 0)
        {
            AddSpacer(storable.name, spacing, rightSide);
            string label = storable == script.calculateBreastMass ? "Calculate Breast Mass" : "Recalibrate Physics";
            var button = script.CreateButton(label, rightSide);
            storable.RegisterButton(button);
            button.height = 52;
            elements[storable.name] = button;
        }

        protected void CreateBaseMultiplierSlider(JSONStorableFloat storable, bool rightSide, int spacing = 0)
        {
            AddSpacer(storable.name, spacing, rightSide);
            var slider = script.CreateSlider(storable, rightSide);
            slider.valueFormat = "F2";
            slider.label = "Base Multiplier";
            elements[storable.name] = slider;
        }

        protected void CreateOtherSettingsHeader(bool rightSide, int spacing = 0)
        {
            var storable = new JSONStorableString("otherSettingsHeader", "");
            AddSpacer(storable.name, spacing, rightSide);
            elements[storable.name] = UIHelpers.HeaderTextField(script, storable, "Other", rightSide);
        }

        protected void CreateBackButton(bool rightSide)
        {
            var button = script.CreateButton("Return", rightSide);
            button.textColor = Color.white;
            var colors = button.button.colors;
            colors.normalColor = UIHelpers.sliderGray;
            colors.highlightedColor = Color.grey;
            colors.pressedColor = Color.grey;
            button.button.colors = colors;
            elements["backButton"] = button;
        }

        #endregion Common elements

        #region Life cycle

        public void Rebuild()
        {
            if(activeNestedWindow != null)
            {
                activeNestedWindow.Rebuild();
            }
            else
            {
                elements.Clear();
                buildAction();
            }
        }

        public List<UIDynamicSlider> GetSliders()
        {
            var sliders = new List<UIDynamicSlider>();
            if(elements.Any())
            {
                foreach(var element in elements)
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

        public void ClosePopups()
        {
            if(!elements.Any())
            {
                return;
            }

            foreach(var element in elements)
            {
                var uiDynamicPopup = element.Value as UIDynamicPopup;
                if(uiDynamicPopup != null)
                {
                    uiDynamicPopup.popup.visible = false;
                }
            }
        }

        public void Clear()
        {
            if(activeNestedWindow != null)
            {
                activeNestedWindow.Clear();
            }
            else
            {
                ClearSelf();
            }

            closeAction?.Invoke();
        }

        protected void ClearSelf() =>
            elements.ToList().ForEach(element => script.RemoveElement(element.Value));

        #endregion Life cycle
    }
}
