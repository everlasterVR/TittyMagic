using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TittyMagic.UI
{
    internal class ParameterWindow
    {
        private readonly Script _script;
        private readonly PhysicsParameterGroup _parameterGroup;

        // ReSharper disable once MemberCanBePrivate.Global
        public Dictionary<string, UIDynamic> elements { get; private set; }

        private readonly JSONStorableString _title;
        private readonly JSONStorableString _infoText;

        public ParameterWindow(Script script, PhysicsParameterGroup parameterGroup)
        {
            _script = script;
            _parameterGroup = parameterGroup;

            _title = new JSONStorableString("title", "");
            _infoText = new JSONStorableString("infoText", "");

            _infoText.val = "\n".Size(12) + _parameterGroup.infoText;
        }

        public void Rebuild(UnityAction backButtonListener)
        {
            elements = new Dictionary<string, UIDynamic>();

            CreateBackButton(backButtonListener, false);
            CreateInfoTextArea(true);

            CreateTitle(false);
            elements["headerMargin"] = _script.NewSpacer(20);

            foreach(var kvp in _parameterGroup.valueJsfs)
            {
                if(kvp.Key.EndsWith("Base") && kvp.Value != null)
                {
                    CreateBaseValueSlider(kvp.Value, _parameterGroup.valueFormat, false);
                }
                else if(kvp.Key.EndsWith("Curr") && kvp.Value != null)
                {
                    CreateCurrentValueSlider(kvp.Value, _parameterGroup.valueFormat, false);
                }
            }
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
            elements["backButton"] = button;
        }

        private void CreateCurrentValueSlider(JSONStorableFloat storable, string valueFormat, bool rightSide, int spacing = 0)
        {
            AddSpacer(storable.name, spacing, rightSide);

            var slider = _script.CreateSlider(storable, rightSide);
            slider.valueFormat = valueFormat;
            slider.slider.interactable = false;
            slider.quickButtonsEnabled = false;
            slider.defaultButtonEnabled = false;
            elements[storable.name] = slider;
        }

        private void CreateBaseValueSlider(JSONStorableFloat storable, string valueFormat, bool rightSide, int spacing = 0)
        {
            AddSpacer(storable.name, spacing, rightSide);

            var slider = _script.CreateSlider(storable, rightSide);
            slider.valueFormat = valueFormat;
            slider.slider.interactable = false;
            slider.quickButtonsEnabled = false;
            slider.defaultButtonEnabled = false;
            elements[storable.name] = slider;
        }

        private void CreateInfoTextArea(bool rightSide, int spacing = 0)
        {
            var storable = _infoText;
            AddSpacer(storable.name, spacing, rightSide);

            var textField = _script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.height = 430;
            textField.backgroundColor = Color.clear;
            elements[storable.name] = textField;
        }

        private void CreateTitle(bool rightSide)
        {
            var textField = UIHelpers.TitleTextField(_script, _title, _parameterGroup.displayName, 62, rightSide);
            textField.UItext.fontSize = 32;
            elements[_title.name] = textField;
        }

        private void AddSpacer(string name, int height, bool rightSide) =>
            elements[$"{name}Spacer"] = _script.NewSpacer(height, rightSide);

        public List<UIDynamicSlider> GetSliders()
        {
            var sliders = new List<UIDynamicSlider>();
            if(elements != null)
            {
                //TODO
            }

            return sliders;
        }

        public void Clear() =>
            elements.ToList().ForEach(element => _script.RemoveElement(element.Value));
    }
}
