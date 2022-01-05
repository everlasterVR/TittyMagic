using UnityEngine;
using UnityEngine.EventSystems;

namespace TittyMagic
{
    internal class SliderClickMonitor : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public bool isDown = false;

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
