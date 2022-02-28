using static TittyMagic.Utils;
using static TittyMagic.Globals;

namespace TittyMagic
{
    public class Config
    {
        public string Name { get; protected set; }
        public bool IsNegative { get; protected set; }
        public float BaseMultiplier { get; set; }
        public float Multiplier1 { get; set; }
        public float Multiplier2 { get; set; }
    }

    internal class GravityPhysicsConfig : Config
    {
        public JSONStorableFloat Setting { get; }

        public float OriginalValue { get; }

        public float BaseValue { get; set; }

        public string Type { get; }

        public GravityPhysicsConfig(string name, string type, bool isNegative, float multiplier1, float multiplier2)
        {
            Name = name;
            Type = type;
            IsNegative = isNegative;
            Multiplier1 = multiplier1;
            Multiplier2 = multiplier2;
            Setting = BREAST_CONTROL.GetFloatJSONParam(name);
            if(Setting == null)
            {
                LogError($"BreastControl float param with name {name} not found!", nameof(GravityPhysicsConfig));
                return;
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
            string uid = MORPHS_PATH + $"{name}.vmi";
            Morph = GEOMETRY.morphsControlUI.GetMorphByUid(uid);
            if(Morph == null)
            {
                LogError($"Morph with uid '{uid}' not found!", nameof(MorphConfig));
            }
        }

        public MorphConfig(string name, bool isNegative, float multiplier1, float multiplier2)
        {
            Name = name;
            IsNegative = isNegative;
            Multiplier1 = multiplier1;
            Multiplier2 = multiplier2;
            string dir = name.Substring(0, 2); // e.g. UP morphs are in UP/ dir
            string uid = MORPHS_PATH + $"{dir}/{name}.vmi";
            Morph = GEOMETRY.morphsControlUI.GetMorphByUid(uid);
            if(Morph == null)
            {
                LogError($"Morph with uid '{uid}' not found!", nameof(MorphConfig));
            }
        }
    }
}
