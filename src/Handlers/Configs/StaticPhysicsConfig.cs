using System;

namespace TittyMagic.Configs
{
    internal class StaticPhysicsConfig
    {
        private readonly float _baseValue; // value at min mass and min softness

        private readonly Func<float, float> _massCurve;
        private readonly Func<float, float> _softnessCurve;

        public StaticPhysicsConfig(
            float baseValue,
            Func<float, float> massCurve = null,
            Func<float, float> softnessCurve = null
        )
        {
            _baseValue = baseValue;
            _massCurve = massCurve ?? (x => 0);
            _softnessCurve = softnessCurve ?? (x => 0);
        }

        public float Calculate(float mass, float softness) =>
            _baseValue * (1 + _massCurve(mass) + _softnessCurve(softness));
    }
}
