using TittyMagic.Components;

public static class UIDynamicSliderExtension
{
    public static void AddPointerUpDownListener(this UIDynamicSlider uiDynamicSlider) =>
        uiDynamicSlider.slider.gameObject.AddComponent<PointerUpDownListener>();

    public static PointerUpDownListener GetPointerUpDownListener(this UIDynamicSlider uiDynamicSlider)
    {
        if(uiDynamicSlider == null || uiDynamicSlider.slider == null)
        {
            return null;
        }

        return uiDynamicSlider.slider.gameObject.GetComponent<PointerUpDownListener>();
    }

    public static bool IsClickDown(this UIDynamicSlider uiDynamicSlider)
    {
        var sliderClickMonitor = uiDynamicSlider.GetPointerUpDownListener();
        return sliderClickMonitor != null && sliderClickMonitor.isDown;
    }
}
