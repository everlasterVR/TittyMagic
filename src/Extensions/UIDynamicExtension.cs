// ReSharper disable MemberCanBePrivate.Global UnusedMember.Global UnusedMethodReturnValue.Global UnusedType.Global
using System;
using UnityEngine;
using UnityEngine.Events;

public static class UIDynamicExtension
{
    public static void AddListener(this UIDynamic element, UnityAction<bool> callback)
    {
        var uiDynamicToggle = element as UIDynamicToggle;
        if(uiDynamicToggle == null)
        {
            throw new ArgumentException($"UIDynamic {element.name} was null or not an UIDynamicToggle");
        }

        uiDynamicToggle.toggle.onValueChanged.AddListener(callback);
    }

    public static void AddListener(this UIDynamic element, UnityAction callback)
    {
        var uiDynamicButton = element as UIDynamicButton;
        if(uiDynamicButton == null)
        {
            throw new ArgumentException($"UIDynamic {element.name} was null or not an UIDynamicButton");
        }

        uiDynamicButton.button.onClick.AddListener(callback);
    }

    public static void AddListener(this UIDynamic element, UnityAction<float> callback)
    {
        var uiDynamicSlider = element as UIDynamicSlider;
        if(uiDynamicSlider == null)
        {
            throw new ArgumentException($"UIDynamic {element.name} was null or not an UIDynamicSlider");
        }

        uiDynamicSlider.slider.onValueChanged.AddListener(callback);
    }

    public static void SetActiveStyle(this UIDynamic element, bool isActive, bool setsInteractable = false)
    {
        var color = isActive ? Color.black : new Color(0.4f, 0.4f, 0.4f);
        var uiDynamicSlider = element as UIDynamicSlider;
        if(uiDynamicSlider != null)
        {
            uiDynamicSlider.slider.interactable = !setsInteractable || isActive;
            uiDynamicSlider.quickButtonsEnabled = !setsInteractable || isActive;
            uiDynamicSlider.defaultButtonEnabled = !setsInteractable || isActive;
            uiDynamicSlider.labelText.color = color;
            return;
        }

        var uiDynamicToggle = element as UIDynamicToggle;
        if(uiDynamicToggle != null)
        {
            uiDynamicToggle.toggle.interactable = !setsInteractable || isActive;
            uiDynamicToggle.labelText.color = color;
            return;
        }

        var uiDynamicButton = element as UIDynamicButton;
        if(uiDynamicButton != null)
        {
            uiDynamicButton.button.interactable = !setsInteractable || isActive;
            var colors = uiDynamicButton.button.colors;
            colors.disabledColor = colors.normalColor;
            uiDynamicButton.button.colors = colors;
            uiDynamicButton.textColor = color;
            return;
        }

        throw new ArgumentException($"UIDynamic {element.name} was null, or not an expected type");
    }
}
