namespace everlaster
{
    class MorphConfig
    {
        public string Name { get; set; }
        public DAZMorph Morph { get; set; }
        public float baseMulti;
        public float startValue;

        public MorphConfig(string name, float baseMulti, float startValue)
        {
            Name = name;
            Morph = Globals.MORPH_UI.GetMorphByDisplayName(name);
            this.baseMulti = baseMulti;
            this.startValue = startValue;
            if(Morph == null)
            {
                Log.Error($"Morph with name {name} not found!", nameof(MorphConfig));
            }
        }

        public void Reset()
        {
            Morph.morphValue = 0;
        }
    }

    class BasicMorphConfig : MorphConfig
    {
        public BasicMorphConfig(
            string name,
            float baseMulti,
            float startValue = 0.00f
        ) : base(name, baseMulti, startValue) { }

        public void UpdateVal(float multiplier = 1f)
        {
            Morph.morphValue = multiplier * baseMulti;
        }
    }
}
