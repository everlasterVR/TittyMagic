using System;

namespace TittyMagic.Configs
{
    internal class StaticPhysicsConfig
    {
        private readonly float _minMminS; // value at min mass and min softness
        private readonly float _maxMminS; // value at max mass and min softness
        private readonly float _minMmaxS; // value at min mass and max softness

        public Func<float, float> massCurve { get; set; }
        public Func<float, float> softnessCurve { get; set; }

        public StaticPhysicsConfig(float minMminS, float maxMminS, float minMmaxS)
        {
            _minMminS = minMminS;
            _maxMminS = maxMminS;
            _minMmaxS = minMmaxS;
        }

        public float Calculate(float mass, float softness)
        {
            float effectiveMass = massCurve?.Invoke(mass) ?? mass;
            float effectiveSoftness = softnessCurve?.Invoke(softness) ?? softness;
            return ProportionalSum(effectiveMass, effectiveSoftness);
        }

        private float ProportionalSum(float mass, float softness) =>
            _minMminS +
            mass * (_maxMminS - _minMminS) +
            softness * (_minMmaxS - _minMminS);
    }
}
