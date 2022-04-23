using System;
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
        public string category { get; }
        public string type { get; }
        public float originalValue { get; }
        public float baseValue { get; set; }

        public PhysicsConfig(string name, string category, string type, bool isNegative, float multiplier1, float multiplier2, bool multiplyInvertedMass)
        {
            this.name = name;
            this.category = category;
            this.type = type;
            this.isNegative = isNegative;
            this.multiplier1 = multiplier1;
            this.multiplier2 = multiplier2;
            this.multiplyInvertedMass = multiplyInvertedMass;
            setting = GetSetting();
            originalValue = setting.val;
        }

        private JSONStorableFloat GetSetting()
        {
            JSONStorableFloat storable = null;
            if(category == "main" || category == "pectoral")
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

    internal class StaticPhysicsConfigBase
    {
        protected JSONStorableFloat setting;
        protected float minMminS; // value at min mass and min softness
        protected float maxMminS; // value at max mass and min softness
        protected float minMmaxS; // value at min mass and max softness

        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public Func<float, float> massCurve { private get; set; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public Func<float, float> softnessCurve { private get; set; }

        protected StaticPhysicsConfigBase()
        {
        }

        public StaticPhysicsConfigBase(float minMminS, float maxMminS, float minMmaxS)
        {
            this.minMminS = minMminS;
            this.maxMminS = maxMminS;
            this.minMmaxS = minMmaxS;
        }

        public float Calculate(float mass, float softness)
        {
            float massComponent = (massCurve?.Invoke(mass) ?? mass) * (maxMminS - minMminS);
            float softnessComponent = (softnessCurve?.Invoke(softness) ?? softness) * (minMmaxS - minMminS);
            return minMminS + massComponent + softnessComponent;
        }
    }

    internal class StaticPhysicsConfig : StaticPhysicsConfigBase
    {
        public bool dependOnPhysicsRate { get; set; }
        public bool useRealMass { get; set; }
        public StaticPhysicsConfigBase quicknessOffsetConfig { get; set; }
        public StaticPhysicsConfigBase slownessOffsetConfig { get; set; }

        // input mass, softness and quickness normalized to (0,1) range
        public void UpdateVal(float mass, float softness, float quickness, float multiplier)
        {
            if(dependOnPhysicsRate)
            {
                setting.val = multiplier * Calculate(mass, softness, quickness);
            }
            else
            {
                setting.val = Calculate(mass, softness, quickness);
            }
        }

        private float Calculate(float mass, float softness, float quickness)
        {
            float value = base.Calculate(mass, softness);
            if(quicknessOffsetConfig != null && quickness > 0)
            {
                float maxQuicknessOffset = quicknessOffsetConfig.Calculate(mass, softness);
                value += Mathf.Lerp(0, maxQuicknessOffset, quickness);
            }

            if(slownessOffsetConfig != null && quickness < 0)
            {
                float maxSlownessOffset = slownessOffsetConfig.Calculate(mass, softness);
                value += Mathf.Lerp(0, maxSlownessOffset, -quickness);
            }

            return value;
        }

        public void UpdateNippleVal(float mass, float softness, float addend = 0)
        {
            setting.val = base.Calculate(mass, softness) + addend;
        }
    }

    internal class BreastStaticPhysicsConfig : StaticPhysicsConfig
    {
        public BreastStaticPhysicsConfig(string storableName, float minMminS, float maxMminS, float minMmaxS)
        {
            setting = BREAST_CONTROL.GetFloatJSONParam(storableName);
            this.minMminS = minMminS;
            this.maxMminS = maxMminS;
            this.minMmaxS = minMmaxS;
        }
    }

    internal class BreastSoftStaticPhysicsConfig : StaticPhysicsConfig
    {
        public BreastSoftStaticPhysicsConfig(string storableName, float minMminS, float maxMminS, float minMmaxS)
        {
            setting = BREAST_PHYSICS_MESH.GetFloatJSONParam(storableName);
            this.minMminS = minMminS;
            this.maxMminS = maxMminS;
            this.minMmaxS = minMmaxS;
        }
    }

    internal class PectoralStaticPhysicsConfig
    {
        private readonly JSONStorableFloat _setting;
        private readonly float _minM; // value at min mass
        private readonly float _maxM; // value at max mass

        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public Func<float, float> massCurve { private get; set; }

        public PectoralStaticPhysicsConfig(string storableName, float minM, float maxM)
        {
            _setting = BREAST_CONTROL.GetFloatJSONParam(storableName);
            _minM = minM;
            _maxM = maxM;
        }

        // input mass normalized to (0,1) range
        public void UpdateVal(float mass, float multiplier = 1, float addend = 0)
        {
            _setting.val = (multiplier * Calculate(mass)) + addend;
        }

        private float Calculate(float mass)
        {
            float massComponent = (massCurve?.Invoke(mass) ?? mass) * (_maxM - _minM);
            return _minM + massComponent;
        }
    }
}
