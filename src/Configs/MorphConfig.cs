using static TittyMagic.Utils;

namespace TittyMagic
{
    public class MorphConfig
    {
        public string Name { get; }
        public DAZMorph Morph { get; }
        public float BaseMultiplier { get; set; }
        public float Multiplier1 { get; set; }
        public float Multiplier2 { get; set; }

        public MorphConfig(string name)
        {
            Name = name;
            Morph = Globals.GEOMETRY.morphsControlUI.GetMorphByDisplayName(name);
            if(Morph == null)
            {
                LogError($"Morph with name {name} not found!", nameof(MorphConfig));
            }
        }
    }
}
