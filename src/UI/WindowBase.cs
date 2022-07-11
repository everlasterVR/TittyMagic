using System;
using System.Collections.Generic;

namespace TittyMagic.UI
{
    internal class WindowBase : IWindow
    {
        protected readonly Script script;
        protected Action buildAction;
        protected Action closeAction;

        public int Id() => id;
        protected int id;

        public Dictionary<string, UIDynamic> GetElements() => elements;
        protected Dictionary<string, UIDynamic> elements;

        protected readonly Dictionary<string, IWindow> nestedWindows;

        public IWindow GetActiveNestedWindow() => activeNestedWindow;
        protected IWindow activeNestedWindow;

        protected WindowBase(Script script)
        {
            this.script = script;
            elements = new Dictionary<string, UIDynamic>();
            nestedWindows = new Dictionary<string, IWindow>();
        }

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

        protected void AddSpacer(string name, int height, bool rightSide) =>
            elements[$"{name}Spacer"] = script.NewSpacer(height, rightSide);

        public List<UIDynamicSlider> GetSliders()
        {
            var sliders = new List<UIDynamicSlider>();
            if(elements != null)
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
            if(elements == null)
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
    }
}
