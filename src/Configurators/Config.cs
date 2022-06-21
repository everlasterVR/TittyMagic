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
        public float multiplier1 { get; set; }
        public float multiplier2 { get; set; }
    }

    internal class GravityPhysicsConfig : Config
    {
        public JSONStorableFloat setting { get; }
        public string type { get; }
        public float originalValue { get; }
        public float baseValue { get; set; }
        public bool multiplyInvertedMass { get; }

        public GravityPhysicsConfig(string name, string type, bool isNegative, float multiplier1, float multiplier2, bool multiplyInvertedMass)
        {
            setting = BREAST_CONTROL.GetFloatJSONParam(name);
            this.name = name;
            this.type = type;
            this.isNegative = isNegative;
            this.multiplier1 = multiplier1;
            this.multiplier2 = multiplier2;
            this.multiplyInvertedMass = multiplyInvertedMass;
            originalValue = setting.val;
        }
    }

    internal class MorphConfig : Config
    {
        public float baseMultiplier { get; set; }
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

        // ReSharper disable once MemberCanBePrivate.Global
        public Func<float, float, float> calculationFunction { private get; set; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public Func<float, float> massCurveA { private get; set; }
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public Func<float, float> massCurveB { private get; set; }
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public float? massCurveCutoff { private get; set; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public Func<float, float> softnessCurveA { private get; set; }
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public Func<float, float> softnessCurveB { private get; set; }
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public float? softnessCurveCutoff { private get; set; }

        protected StaticPhysicsConfigBase()
        {
        }

        public StaticPhysicsConfigBase(float minMminS, float maxMminS, float minMmaxS)
        {
            this.minMminS = minMminS;
            this.maxMminS = maxMminS;
            this.minMmaxS = minMmaxS;
            calculationFunction = CalculateByProportionalSum;
        }

        public float Calculate(float mass, float softness)
        {
            float effectiveMass = mass > massCurveCutoff
                ? massCurveB(mass)
                : massCurveA?.Invoke(mass) ?? mass;
            float effectiveSoftness = softness > softnessCurveCutoff
                ? softnessCurveB(softness)
                : softnessCurveA?.Invoke(softness) ?? softness;
            return calculationFunction(effectiveMass, effectiveSoftness);
        }

        protected float CalculateByProportionalSum(float mass, float softness)
        {
            return minMminS + MassComponent(mass) + SoftnessComponent(softness);
        }

        private float MassComponent(float mass)
        {
            return mass * (maxMminS - minMminS);
        }

        private float SoftnessComponent(float softness)
        {
            return softness * (minMmaxS - minMminS);
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
            calculationFunction = CalculateByProportionalSum;
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
            calculationFunction = CalculateByProportionalSum;
        }
    }
}
