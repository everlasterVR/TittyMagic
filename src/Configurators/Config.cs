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
        public bool multiplyInvertedMass { get; protected set; }
    }

    internal class PhysicsConfig : Config
    {
        public JSONStorableFloat setting { get; }

        public float originalValue { get; }

        public float baseValue { get; set; }

        public string type { get; }

        public PhysicsConfig(string name, string category, string type, bool isNegative, float multiplier1, float multiplier2, bool multiplyInvertedMass)
        {
            this.name = name;
            this.type = type;
            this.isNegative = isNegative;
            this.multiplier1 = multiplier1;
            this.multiplier2 = multiplier2;
            this.multiplyInvertedMass = multiplyInvertedMass;
            setting = GetSetting(category);
            originalValue = setting.val;
        }

        private JSONStorableFloat GetSetting(string category)
        {
            JSONStorableFloat storable = null;
            if(category == "main")
            {
                storable = BREAST_CONTROL.GetFloatJSONParam(name);
                if(storable == null)
                {
                    LogError($"BreastControl float param with name {name} not found!");
                }
            }
            else if(category == "soft")
            {
                storable = BREAST_PHYSICS_MESH.GetFloatJSONParam(name);
                if(storable == null)
                {
                    LogError($"BreastPhysicsMesh float param with name {name} not found!");
                }
            }

            return storable;
        }
    }

    internal class MorphConfig : Config
    {
        public DAZMorph morph { get; }

        public MorphConfig(string name)
        {
            this.name = name;
            morph = GetMorph();
        }

        public MorphConfig(string name, bool isNegative, float multiplier1, float multiplier2)
        {
            this.name = name;
            this.isNegative = isNegative;
            this.multiplier1 = multiplier1;
            this.multiplier2 = multiplier2;
            morph = GetMorphFromSubDir();
        }

        private DAZMorph GetMorph()
        {
            string uid = MORPHS_PATH + $"{name}.vmi";
            var dazMorph = MORPHS_CONTROL_UI.GetMorphByUid(uid);
            if(dazMorph == null)
            {
                LogError($"Morph with uid '{uid}' not found!");
            }

            return dazMorph;
        }

        private DAZMorph GetMorphFromSubDir()
        {
            string dir = name.Substring(0, 2); // e.g. UP morphs are in UP/ dir
            string uid = MORPHS_PATH + $"{dir}/{name}.vmi";
            var dazMorph = MORPHS_CONTROL_UI.GetMorphByUid(uid);
            if(dazMorph == null)
            {
                LogError($"Morph with uid '{uid}' not found!");
            }

            return dazMorph;
        }
    }

    internal class BreastStaticPhysicsConfig
    {
        private readonly JSONStorableFloat _setting;
        private readonly float _valMinMS; // value at min mass and min softness
        private readonly float _valMaxM; // value at max mass and min softness
        private readonly float _valMaxS; // value at min mass and max softness
        public bool dependOnPhysicsRate { get; }

        public BreastStaticPhysicsConfig(string storableName, float valMinMS, float valMaxM, float valMaxS, bool dependOnPhysicsRate = false)
        {
            _setting = BREAST_CONTROL.GetFloatJSONParam(storableName);
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

    internal class BreastSoftStaticPhysicsConfig
    {
        private readonly JSONStorableFloat _setting;
        private readonly float _valMinMS; // value at min mass and min softness
        private readonly float _valMaxM; // value at max mass and min softness
        private readonly float _valMaxS; // value at min mass and max softness
        public bool dependOnPhysicsRate { get; }

        public BreastSoftStaticPhysicsConfig(string storableName, float valMinMS, float valMaxM, float valMaxS, bool dependOnPhysicsRate = false)
        {
            _setting = BREAST_PHYSICS_MESH.GetFloatJSONParam(storableName);
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

        public PectoralStaticPhysicsConfig(string storableName, float valMinM, float valMaxM)
        {
            _setting = BREAST_CONTROL.GetFloatJSONParam(storableName);
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
