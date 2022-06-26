using System;

namespace TittyMagic.Configs
{
    internal class DynamicPhysicsConfig
    {
        public bool isNegative { get; }
        public float softnessMultiplier { get; }
        public float massMultiplier { get; }

        public bool multiplyInvertedMass { get; }

        public bool additive { get; }

        public DynamicPhysicsConfig(float softnessMultiplier, float massMultiplier, bool isNegative = false, bool multiplyInvertedMass = false, bool additive = true)
        {
            this.softnessMultiplier = softnessMultiplier;
            this.massMultiplier = massMultiplier;
            this.isNegative = isNegative;
            this.multiplyInvertedMass = multiplyInvertedMass;
            this.additive = additive;
        }
    }
}
