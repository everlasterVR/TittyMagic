using UnityEngine;
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

    internal class BreastStaticPhysicsConfig
    {
        private readonly JSONStorableFloat _setting;
        private readonly float _valMinMS; // value at min mass and min softness
        private readonly float _valMaxM; // value at max mass and min softness
        private readonly float _valMaxS; // value at min mass and max softness
        public bool dependOnPhysicsRate { get; }

        public BreastStaticPhysicsConfig(JSONStorableFloat setting, float valMinMS, float valMaxM, float valMaxS, bool dependOnPhysicsRate)
        {
            _setting = setting;
            _valMinMS = valMinMS;
            _valMaxM = valMaxM;
            _valMaxS = valMaxS;
            this.dependOnPhysicsRate = dependOnPhysicsRate;
        }

        // input mass and softness normalized to (0,1) range
        public void UpdateVal(float mass, float softness, float multiplier = 1, float addend = 0)
        {
            _setting.val = (multiplier * Calculate(mass, softness)) + addend;
        }

        private float Calculate(float mass, float softness)
        {
            return Mathf.Lerp(_valMinMS, _valMaxM, mass) + Mathf.Lerp(_valMinMS, _valMaxS, softness) - _valMinMS;
        }
    }

    internal class PectoralStaticPhysicsConfig
    {
        private readonly JSONStorableFloat _setting;
        private readonly float _valMinM; // value at min mass
        private readonly float _valMaxM; // value at max mass

        public PectoralStaticPhysicsConfig(JSONStorableFloat setting, float valMinM, float valMaxM)
        {
            _setting = setting;
            _valMinM = valMinM;
            _valMaxM = valMaxM;
        }

        // input mass normalized to (0,1) range
        public void UpdateVal(float mass, float multiplier = 1, float addend = 0)
        {
            _setting.val = (multiplier * Calculate(mass)) + addend;
        }

        private float Calculate(float mass)
        {
            return Mathf.Lerp(_valMinM, _valMaxM, mass);
        }
    }
}
