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
                SuperController.LogError($"everlaster.TittyMagic.{nameof(MorphConfig)}: Morph with name {name} not found!");
            }
        }

        public void Reset()
        {
            Morph.morphValue = 0;
        }
    }
}
