namespace everlaster
{
    class MorphConfig
    {
        public string name;
        public DAZMorph morph;
        public float baseMulti;
        public float startValue;

        public MorphConfig(string name, float baseMulti, float startValue)
        {
            this.name = name;
            morph = Globals.MORPH_UI.GetMorphByDisplayName(name);
            this.baseMulti = baseMulti;
            this.startValue = startValue;
            if(morph == null)
            {
                Log.Error($"Morph with name {name} not found!", nameof(MorphConfig));
            }
        }

        public void Reset()
        {
            morph.morphValue = 0;
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
            morph.morphValue = multiplier * baseMulti;
        }
    }
}
