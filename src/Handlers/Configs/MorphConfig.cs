namespace TittyMagic.Configs
{
    internal class MorphConfigBase
    {
        public float multiplier { get; }
        public DAZMorph morph { get; }

        public MorphConfigBase(string name, float multiplier)
        {
            this.multiplier = multiplier;
            morph = Utils.GetMorph(name);
        }
    }

    internal class MorphConfig : MorphConfigBase
    {
        public bool isNegative { get; }
        public float softnessMultiplier { get; }
        public float massMultiplier { get; }

        public MorphConfig(string name, bool isNegative, float softnessMultiplier, float massMultiplier) : base(name, 1)
        {
            this.isNegative = isNegative;
            this.softnessMultiplier = multiplier * softnessMultiplier;
            this.massMultiplier = multiplier * massMultiplier;
        }
    }
}
