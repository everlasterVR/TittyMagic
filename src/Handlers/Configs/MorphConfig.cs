namespace TittyMagic.Handlers.Configs
{
    public class MorphConfigBase
    {
        public float multiplier { get; }
        public DAZMorph morph { get; }

        public MorphConfigBase(string name, float multiplier)
        {
            this.multiplier = multiplier;
            morph = Utils.GetMorph(name);
        }
    }

    public class MorphConfig
    {
        public DAZMorph morph { get; }
        public bool isNegative { get; }
        public float softnessMultiplier { get; }
        public float massMultiplier { get; }

        public MorphConfig(string name, bool isNegative, float softnessMultiplier, float massMultiplier)
        {
            morph = Utils.GetMorph(name);
            this.isNegative = isNegative;
            this.softnessMultiplier = softnessMultiplier;
            this.massMultiplier = massMultiplier;
        }
    }
}
