using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TittyMagic.Components
{
    public class PointerUpDownListener : MonoBehaviour, IPointerUpHandler, IPointerDownHandler
    {
        public bool isDown;

        public Action pointerUpAction { private get; set; }
        public Action pointerDownAction { private get; set; }

        public void OnPointerUp(PointerEventData data)
        {
            isDown = false;
            pointerUpAction?.Invoke();
        }

        public void OnPointerDown(PointerEventData data)
        {
            isDown = true;
            pointerDownAction?.Invoke();
        }
    }
}
