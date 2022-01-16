using static TittyMagic.Utils;
using static TittyMagic.Globals;

namespace TittyMagic
{
    public class Config
    {
        public string Name { get; set; }
        public bool IsNegative { get; set; }
        public float BaseMultiplier { get; set; }
        public float Multiplier1 { get; set; }
        public float Multiplier2 { get; set; }
    }

    internal class GravityPhysicsConfig : Config
    {
        public JSONStorableFloat Setting { get; }

        public float OriginalValue { get; set; }

        public GravityPhysicsConfig(string name, bool isNegative, float multiplier1, float multiplier2)
        {
            Name = name;
            IsNegative = isNegative;
            Multiplier1 = multiplier1;
            Multiplier2 = multiplier2;
            Setting = BREAST_CONTROL.GetFloatJSONParam(name);
            if(Setting == null)
            {
                LogError($"BreastControl float param with name {name} not found!", nameof(GravityPhysicsConfig));
            }
            OriginalValue = Setting.val;
        }
    }

    internal class MorphConfig : Config
    {
        public DAZMorph Morph { get; }

        public MorphConfig(string name)
        {
            Name = name;
            Morph = GEOMETRY.morphsControlUI.GetMorphByDisplayName(name);
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
            Morph = GEOMETRY.morphsControlUI.GetMorphByDisplayName(name);
            if(Morph == null)
            {
                LogError($"Morph with name {name} not found!", nameof(MorphConfig));
            }
        }
    }
}
