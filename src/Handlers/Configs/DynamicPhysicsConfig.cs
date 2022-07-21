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

        public DynamicPhysicsConfig(
            float softnessMultiplier,
            float massMultiplier,
            bool isNegative,
            bool multiplyInvertedMass,
            string applyMethod
        )
        {
            this.softnessMultiplier = softnessMultiplier;
            this.massMultiplier = massMultiplier;
            this.isNegative = isNegative;
            this.multiplyInvertedMass = multiplyInvertedMass;
            this.applyMethod = applyMethod;
        }

        public float Calculate(float effect, float massValue, float softness)
        {
            float mass = multiplyInvertedMass ? 1 - massValue : massValue;
            float value =
                softness * softnessMultiplier * effect +
                mass * massMultiplier * effect;

            bool inRange = isNegative ? value < 0 : value > 0;
            return inRange ? value : 0;
        }
    }
}
