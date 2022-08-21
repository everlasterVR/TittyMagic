﻿using UnityEngine;
using UnityEngine.EventSystems;

namespace TittyMagic.Components
{
    public class PointerUpDownListener : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public bool isDown;

        public void OnPointerDown(PointerEventData data) => isDown = true;

        public void OnPointerUp(PointerEventData data) => isDown = false;
    }
}
