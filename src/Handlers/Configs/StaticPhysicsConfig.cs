using System;

namespace TittyMagic.Configs
{
    internal class StaticPhysicsConfig
    {
        private readonly float _baseValue; // value at min mass and min softness

        private readonly bool _multiplyInvertedMass;
        private readonly Func<float, float> _massCurve;
        private readonly Func<float, float> _softnessCurve;

        public StaticPhysicsConfig(
            float baseValue,
            bool multiplyInvertedMass = false,
            Func<float, float> massCurve = null,
            Func<float, float> softnessCurve = null
        )
        {
            _baseValue = baseValue;
            _multiplyInvertedMass = multiplyInvertedMass;
            _massCurve = massCurve ?? (x => 0);
            _softnessCurve = softnessCurve ?? (x => 0);
        }

        public float Calculate(float massValue, float softness)
        {
            float mass = _multiplyInvertedMass ? 1.5f - massValue : massValue;
            return _baseValue * (1 + _massCurve(mass) + _softnessCurve(softness));
        }
    }
}
