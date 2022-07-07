using System;

namespace TittyMagic.Configs
{
    internal class Scaler
    {
        private readonly float _offset;
        private readonly float _min;
        private readonly float _max;
        private readonly Func<float, float> _func;

        public Scaler(float offset, float min = 0, float max = 1, Func<float, float> func = null)
        {
            _offset = offset;
            _min = min;
            _max = max;
            _func = func == null ? x => x : Validate(func);
        }

        private static Func<float, float> Validate(Func<float, float> function)
        {
            float resultAt0 = function(0);
            if(resultAt0 != 0)
            {
                throw new ArgumentException($"Function must pass through origin. Value returned at 0: {resultAt0}", nameof(function));
            }

            return function;
        }

        // normalizes to value to [min, max] range and applies scaling func
        public float Scale(float value) =>
            _func((_offset + value - _min) / (_max - _min));

        // clamped at lower end, unclamped at upper end
        public float ScaleFromMin(float value)
        {
            if(value <= _min)
            {
                return 0;
            }

            return Scale(value);
        }
    }
}
