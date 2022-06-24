using TittyMagic.UI;

namespace TittyMagic.Extensions
{
    public static class UIDynamicSliderExtension
    {
        public static void AddSliderClickMonitor(this UIDynamicSlider uiDynamicSlider)
        {
            uiDynamicSlider.slider.gameObject.AddComponent<SliderClickMonitor>();
        }

        public static SliderClickMonitor GetSliderClickMonitor(this UIDynamicSlider uiDynamicSlider)
        {
            if(uiDynamicSlider == null || uiDynamicSlider.slider == null)
            {
                return null;
            }
            return uiDynamicSlider.slider.gameObject.GetComponent<SliderClickMonitor>();
        }

        public static bool IsClickDown(this UIDynamicSlider uiDynamicSlider)
        {
            var sliderClickMonitor = uiDynamicSlider.GetSliderClickMonitor();
            return sliderClickMonitor != null && sliderClickMonitor.isDown;
        }
    }
}
