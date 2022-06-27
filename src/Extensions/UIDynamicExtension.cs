// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
using System;
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
}
