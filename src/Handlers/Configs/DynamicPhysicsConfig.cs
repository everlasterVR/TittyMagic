using System;

namespace TittyMagic.Handlers.Configs
{
    public class DynamicPhysicsConfig
    {
        public float baseMultiplier { get; set; }
        public float softnessMultiplier { private get; set; }
        public float massMultiplier { private get; set; }
        public string applyMethod { get; set; }
        public Func<float, float> massCurve { private get; set; }
        public Func<float, float> softnessCurve { private get; set; }
        public bool? negative { get; set; }

        public DynamicPhysicsConfig()
        {
            massCurve = x => x;
            softnessCurve = x => x;
        }

        public float Calculate(float effect, float mass, float softness)
        {
            float value = effect * (
                baseMultiplier +
                softnessCurve(softness) * softnessMultiplier +
                massCurve(mass) * massMultiplier
            );

            return negative.HasValue ? LimitToRange(value, negative.Value) : value;
        }

        public float CalculateNippleGroupValue(float effect, float mass, float softness)
        {
            float value = effect * (
                baseMultiplier +
                softnessCurve(softness) * softnessMultiplier +
                massCurve(mass) * massMultiplier
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
