using System;

namespace TittyMagic.Configs
{
    internal class GravityPhysicsConfig
    {
        public bool isNegative { get; }
        public float softnessMultiplier { get; }
        public float massMultiplier { get; }

        public bool multiplyInvertedMass { get; }

        public Action<float> updateFunction { get; set; }

        public GravityPhysicsConfig(float softnessMultiplier, float massMultiplier, bool isNegative = false, bool multiplyInvertedMass = false)
        {
            this.softnessMultiplier = softnessMultiplier;
            this.massMultiplier = massMultiplier;
            this.isNegative = isNegative;
            this.multiplyInvertedMass = multiplyInvertedMass;
        }
    }
}
