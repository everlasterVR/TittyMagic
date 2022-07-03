namespace TittyMagic.Configs
{
    public class Multiplier
    {
        public float mainMultiplier { get; set; }
        public float? extraMultiplier { get; set; }
        public float? oppositeExtraMultiplier { get; set; }

        public Multiplier(float mainMultiplier)
        {
            this.mainMultiplier = mainMultiplier;
        }
    }
}
