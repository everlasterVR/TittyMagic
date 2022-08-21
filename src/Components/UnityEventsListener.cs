// ReSharper disable MemberCanBePrivate.Global
using UnityEngine;
using UnityEngine.Events;

namespace TittyMagic.Components
{
    public class UnityEventsListener : MonoBehaviour
    {
        public readonly UnityEvent onDisable = new UnityEvent();
        public readonly UnityEvent onEnable = new UnityEvent();

        public void OnDisable()
        {
            onDisable.Invoke();
        }

        public void OnEnable()
        {
            onEnable.Invoke();
        }

        private void OnDestroy()
        {
            onDisable.RemoveAllListeners();
        }
    }
}
