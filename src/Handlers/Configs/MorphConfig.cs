namespace TittyMagic.Configs
{
    internal class MorphConfig
    {
        public bool isNegative { get; }
        public float softnessMultiplier { get; }
        public float massMultiplier { get; }
        public DAZMorph morph { get; }

        public MorphConfig(string name, bool isNegative, float softnessMultiplier, float massMultiplier)
        {
            this.isNegative = isNegative;
            this.softnessMultiplier = softnessMultiplier;
            this.massMultiplier = massMultiplier;
            morph = Utils.GetMorph(name);
        }
    }
}
