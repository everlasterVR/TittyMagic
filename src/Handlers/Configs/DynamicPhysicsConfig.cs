using System;

namespace TittyMagic.Configs
{
    internal class DynamicPhysicsConfig
    {
        public float baseMultiplier { get; set; }
        public bool isNegative { get; }
        public float softnessMultiplier { get; }
        public float massMultiplier { get; }
        public bool multiplyInvertedMass { get; }
        public string applyMethod { get; }
        private readonly Func<float, float> _massCurve;
        private readonly Func<float, float> _softnessCurve;

        public DynamicPhysicsConfig(
            float massMultiplier,
            float softnessMultiplier,
            bool isNegative,
            bool multiplyInvertedMass,
            string applyMethod,
            Func<float, float> massCurve = null,
            Func<float, float> softnessCurve = null
        )
        {
            this.massMultiplier = massMultiplier;
            this.softnessMultiplier = softnessMultiplier;
            this.isNegative = isNegative;
            this.multiplyInvertedMass = multiplyInvertedMass;
            this.applyMethod = applyMethod;
            _massCurve = massCurve ?? (x => x);
            _softnessCurve = softnessCurve ?? (x => x);
        }

        public float Calculate(float effect, float massValue, float softness)
        {
            // hack. 1.5f because 3f is the max mass and massValue is actual mass / 2
            float mass = multiplyInvertedMass ? 1.5f - massValue : massValue;
            float value =
                _softnessCurve(softness) * softnessMultiplier * effect +
                _massCurve(mass) * massMultiplier * effect;

            bool inRange = isNegative ? value < 0 : value > 0;
            return inRange ? value : 0;
        }
    }
}
