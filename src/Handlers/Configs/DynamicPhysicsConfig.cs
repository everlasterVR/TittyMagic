using System;

namespace TittyMagic.Handlers.Configs
{
    public class DynamicPhysicsConfig
    {
        public float baseMultiplier { get; set; }
        public bool? negative { get; set; }
        private float softnessMultiplier { get; }
        private float massMultiplier { get; }
        public string applyMethod { get; }
        private readonly Func<float, float> _massCurve;
        private readonly Func<float, float> _softnessCurve;

        public DynamicPhysicsConfig(
            float massMultiplier,
            float softnessMultiplier,
            string applyMethod,
            Func<float, float> massCurve = null,
            Func<float, float> softnessCurve = null
        )
        {
            this.massMultiplier = massMultiplier;
            this.softnessMultiplier = softnessMultiplier;
            this.applyMethod = applyMethod;
            _massCurve = massCurve ?? (x => x);
            _softnessCurve = softnessCurve ?? (x => x);
        }

        public float Calculate(float effect, float mass, float softness)
        {
            float value = effect * (
                baseMultiplier +
                _softnessCurve(softness) * softnessMultiplier +
                _massCurve(mass) * massMultiplier
            );

            return negative.HasValue ? LimitToRange(value, negative.Value) : value;
        }

        public float CalculateNippleGroupValue(float effect, float mass, float softness)
        {
            float value = effect * (
                baseMultiplier +
                _softnessCurve(softness) * softnessMultiplier +
                _massCurve(mass) * massMultiplier
            );

            return negative.HasValue ? LimitToRange(value, negative.Value) : value;
        }

        private static float LimitToRange(float value, bool isNegative)
        {
            bool inRange = isNegative ? value < 0 : value > 0;
            return inRange ? value : 0;
        }
    }
}
