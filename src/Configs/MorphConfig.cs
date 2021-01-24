namespace everlaster
{
    class MorphConfig
    {
        public string Name { get; set; }
        public DAZMorph Morph { get; set; }
        public float BaseMulti { get; set; }
        public float StartValue { get; set; }

        public MorphConfig(string name, float baseMulti, float startValue)
        {
            Name = name;
            Morph = Globals.MORPH_UI.GetMorphByDisplayName(name);
            BaseMulti = baseMulti;
            StartValue = startValue;
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
            Morph.morphValue = multiplier * BaseMulti;
        }
    }

    class SizeMorphConfig : MorphConfig
    {
        public SizeMorphConfig(
            string name,
            float baseMulti,
            float startValue = 0.00f
        ) : base(name, baseMulti, startValue) { }

        public void UpdateVal(float scale)
        {
            Morph.morphValue = StartValue + BaseMulti * scale;
        }
    }
}
