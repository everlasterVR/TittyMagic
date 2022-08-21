using System;

namespace TittyMagic.Configs
{
    internal class DynamicPhysicsConfig
    {
        public float baseMultiplier { get; set; }
        private bool isNegative { get; }
        private float softnessMultiplier { get; }
        private float massMultiplier { get; }
        public string applyMethod { get; }
        private readonly Func<float, float> _massCurve;
        private readonly Func<float, float> _softnessCurve;

        public DynamicPhysicsConfig(
            float massMultiplier,
            float softnessMultiplier,
            bool isNegative,
            string applyMethod,
            Func<float, float> massCurve = null,
            Func<float, float> softnessCurve = null
        )
        {
            this.massMultiplier = massMultiplier;
            this.softnessMultiplier = softnessMultiplier;
            this.isNegative = isNegative;
            this.applyMethod = applyMethod;
            _massCurve = massCurve ?? (x => x);
            _softnessCurve = softnessCurve ?? (x => x);
        }

        public float Calculate(float effect, float mass, float softness)
        {
            float value = effect * (
                _softnessCurve(softness) * softnessMultiplier +
                _massCurve(mass) * massMultiplier
            );

            return LimitToRange(value);
        }

        public float CalculateNippleGroupValue(float effect, float mass, float softness)
        {
            float value = effect * (
                baseMultiplier +
                _softnessCurve(softness) * softnessMultiplier +
                _massCurve(mass) * massMultiplier
            );

            return LimitToRange(value);
        }

        private float LimitToRange(float value)
        {
            bool isInRange = isNegative ? value < 0 : value > 0;
            return isInRange ? value : 0;
        }
    }
}
