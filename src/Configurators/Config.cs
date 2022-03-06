using static TittyMagic.Utils;
using static TittyMagic.Globals;

namespace TittyMagic
{
    public class Config
    {
        public string name { get; protected set; }
        public bool isNegative { get; protected set; }
        public float baseMultiplier { get; set; }
        public float multiplier1 { get; set; }
        public float multiplier2 { get; set; }
    }

    internal class MainPhysicsConfig : Config
    {
        public JSONStorableFloat setting { get; }

        public float originalValue { get; }

        public float baseValue { get; set; }

        public string type { get; }

        public MainPhysicsConfig(string name, string type, bool isNegative, float multiplier1, float multiplier2)
        {
            this.name = name;
            this.type = type;
            this.isNegative = isNegative;
            this.multiplier1 = multiplier1;
            this.multiplier2 = multiplier2;
            setting = BREAST_CONTROL.GetFloatJSONParam(name);
            if(setting == null)
            {
                LogError($"BreastControl float param with name {name} not found!", nameof(MainPhysicsConfig));
                return;
            }

            originalValue = setting.val;
        }
    }

    internal class MorphConfig : Config
    {
        public DAZMorph morph { get; }

        public MorphConfig(string name)
        {
            this.name = name;
            string uid = MORPHS_PATH + $"{name}.vmi";
            morph = MORPHS_CONTROL_UI.GetMorphByUid(uid);
            if(morph == null)
            {
                LogError($"Morph with uid '{uid}' not found!", nameof(MorphConfig));
            }
        }

        public MorphConfig(string name, bool isNegative, float multiplier1, float multiplier2)
        {
            this.name = name;
            this.isNegative = isNegative;
            this.multiplier1 = multiplier1;
            this.multiplier2 = multiplier2;
            string dir = name.Substring(0, 2); // e.g. UP morphs are in UP/ dir
            string uid = MORPHS_PATH + $"{dir}/{name}.vmi";
            morph = MORPHS_CONTROL_UI.GetMorphByUid(uid);
            if(morph == null)
            {
                LogError($"Morph with uid '{uid}' not found!", nameof(MorphConfig));
            }
        }
    }
}
