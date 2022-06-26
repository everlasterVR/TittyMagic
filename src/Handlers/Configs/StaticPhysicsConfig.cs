using System;
using UnityEngine;

namespace TittyMagic.Configs
{
    internal class StaticPhysicsConfigBase
    {
        protected float minMminS; // value at min mass and min softness
        protected float maxMminS; // value at max mass and min softness
        protected float minMmaxS; // value at min mass and max softness

        public Action<float> updateFunction { get; set; }

        protected Func<float, float, float> calculationFunction { private get; set; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public Func<float, float> massCurveLower { private get; set; }
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public Func<float, float> massCurveUpper { private get; set; }
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public float? massCurveCutoff { private get; set; }

        private Func<float, float> softnessCurveLower { get; set; }
        private Func<float, float> softnessCurveUpper { get; set; }
        private float? softnessCurveCutoff { get; set; }

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

        // Transition between two linear functions at mid point. 0.6285f is softnessAmount at slider 50
        // https://www.desmos.com/calculator/traqqeb88h
        // slope is decided such that after mid point, the value behaves as before minMminS and maxMminS were increased
        public void SetLinearCurvesAroundMidpoint(float slope, float cutoff = 0.6285f)
        {
            softnessCurveCutoff = cutoff;
            softnessCurveUpper = x => slope * x + (1 - slope);
            softnessCurveLower = x => softnessCurveUpper(cutoff) / cutoff * x;
        }

        public float Calculate(float mass, float softness)
        {
            float effectiveMass = mass;
            if(massCurveCutoff.HasValue)
            {
                effectiveMass = mass > massCurveCutoff
                    ? massCurveUpper(mass)
                    : massCurveLower(mass);
            }

            float effectiveSoftness = softness;
            if(softnessCurveCutoff.HasValue)
            {
                effectiveSoftness = softness > softnessCurveCutoff
                    ? softnessCurveUpper(softness)
                    : softnessCurveLower(softness);
            }

            return calculationFunction(effectiveMass, effectiveSoftness);
        }

        protected float CalculateByProportionalSum(float mass, float softness)
        {
            return minMminS +
                mass * (maxMminS - minMminS) +
                softness * (minMmaxS - minMminS);
        }
    }

    internal class StaticPhysicsConfig : StaticPhysicsConfigBase
    {
        public bool dependOnPhysicsRate { get; set; }
        public bool useRealMass { get; set; }
        public StaticPhysicsConfigBase quicknessOffsetConfig { get; set; }
        public StaticPhysicsConfigBase slownessOffsetConfig { get; set; }

        public StaticPhysicsConfig(float minMminS, float maxMminS, float minMmaxS)
        {
            this.minMminS = minMminS;
            this.maxMminS = maxMminS;
            this.minMmaxS = minMmaxS;
            calculationFunction = CalculateByProportionalSum;
        }

        public float Calculate(float mass, float softness, float quickness)
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
    }
}
