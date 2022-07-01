using System;
using UnityEngine;

namespace TittyMagic
{
    internal class FrequencyRunner
    {
        private float _timeSinceLastCheck;
        private readonly float _frequency;

        public FrequencyRunner(float frequency)
        {
            _frequency = frequency;
        }

        public T Run<T>(Func<T> action)
        {
            _timeSinceLastCheck += Time.unscaledDeltaTime;
            if(_timeSinceLastCheck >= _frequency)
            {
                _timeSinceLastCheck -= _frequency;
                return action();
            }

            return default(T);
        }
    }
}
