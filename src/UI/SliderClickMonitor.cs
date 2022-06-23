using UnityEngine;
using UnityEngine.EventSystems;

namespace TittyMagic.UI
{
    internal class SliderClickMonitor : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public bool isDown;

        public void OnPointerDown(PointerEventData data)
        {
            isDown = true;
        }

        public void OnPointerUp(PointerEventData data)
        {
            isDown = false;
        }
    }
}
