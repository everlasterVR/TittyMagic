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

    class ExampleMorphConfig : MorphConfig
    {
        public ExampleMorphConfig(
            string name,
            float baseMulti,
            float startValue = 0.00f
        ) : base(name, baseMulti, startValue) { }

        public void UpdateVal()
        {
            Morph.morphValue = BaseMulti;
        }
    }

    class NippleErectionMorphConfig : MorphConfig
    {
        public NippleErectionMorphConfig(
            string name,
            float baseMulti,
            float startValue = 0.00f
        ) : base(name, baseMulti, startValue) { }

        public void UpdateVal(float nippleErection)
        {
            Morph.morphValue = StartValue + BaseMulti * nippleErection;
        }
    }
}
