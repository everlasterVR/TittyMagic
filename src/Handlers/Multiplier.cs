namespace TittyMagic
{
    public class Multiplier
    {
        public readonly JSONStorableFloat m;
        public float extraMultiplier1 { get; set; }
        public float oppositeExtraMultiplier1 { get; set; }
        public float? extraMultiplier2 { get; set; }
        public float? oppositeExtraMultiplier2 { get; set; }

        public Multiplier(JSONStorableFloat m)
        {
            this.m = m;
        }
    }
}
