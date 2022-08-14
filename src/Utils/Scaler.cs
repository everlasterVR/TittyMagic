using System;

namespace TittyMagic.Configs
{
    internal class Scaler
    {
        private readonly float _offset;
        private readonly float _min;
        private readonly float _max;

        private readonly Func<float, float> _massCurve;
        private readonly Func<float, float> _softnessCurve;

        public Scaler(
            float offset,
            // ReSharper disable once SuggestBaseTypeForParameterInConstructor
            float[] range,
            Func<float, float> massCurve = null,
            Func<float, float> softnessCurve = null
        )
        {
            _offset = offset;
            _min = range[0];
            _max = range[1];
            _massCurve = massCurve ?? (x => 0);
            _softnessCurve = softnessCurve ?? (x => 0);
        }

        public float Scale(float value, float massValue, float softness)
        {
            float totalOffset = _massCurve(massValue) + _softnessCurve(softness) + _offset;
            return (totalOffset + value - _min) / (_max - _min);
        }
    }
}
