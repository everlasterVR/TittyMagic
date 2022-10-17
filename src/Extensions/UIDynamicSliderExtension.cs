// ReSharper disable MemberCanBePrivate.Global UnusedMember.Global UnusedMethodReturnValue.Global UnusedType.Global
using System;
using TittyMagic.Components;

public static class UIDynamicSliderExtension
{
    public static void AddPointerUpDownListener(
        this UIDynamicSlider uiDynamicSlider,
        Action pointerUpAction = null,
        Action pointerDownAction = null
    )
    {
        var listener = uiDynamicSlider.slider.gameObject.AddComponent<PointerUpDownListener>();
        listener.pointerUpAction = pointerUpAction;
        listener.pointerDownAction = pointerDownAction;
    }

    public static PointerUpDownListener GetPointerUpDownListener(this UIDynamicSlider uiDynamicSlider)
    {
        if(uiDynamicSlider == null || uiDynamicSlider.slider == null)
        {
            return null;
        }

        return uiDynamicSlider.slider.gameObject.GetComponent<PointerUpDownListener>();
    }

    public static bool PointerDown(this UIDynamicSlider uiDynamicSlider)
    {
        var sliderClickMonitor = uiDynamicSlider.GetPointerUpDownListener();
        return sliderClickMonitor != null && sliderClickMonitor.isDown;
    }
}
