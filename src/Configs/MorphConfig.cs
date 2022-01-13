using static TittyMagic.Utils;

namespace TittyMagic
{
    public class MorphConfig
    {
        public string Name { get; }
        public DAZMorph Morph { get; }
        public bool IsNegative { get; set; }
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

        public MorphConfig(string name, bool isNegative, float multiplier1, float multiplier2)
        {
            Name = name;
            IsNegative = isNegative;
            Multiplier1 = multiplier1;
            Multiplier2 = multiplier2;
            Morph = Globals.GEOMETRY.morphsControlUI.GetMorphByDisplayName(name);
            if(Morph == null)
            {
                LogError($"Morph with name {name} not found!", nameof(MorphConfig));
            }
        }
    }
}
