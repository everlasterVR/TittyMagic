using System;

namespace TittyMagic.Configs
{
    internal class StaticPhysicsConfig
    {
        private readonly float _minMminS; // value at min mass and min softness
        private readonly float _maxMminS; // value at max mass and min softness
        private readonly float _minMmaxS; // value at min mass and max softness

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

        public bool dependOnPhysicsRate { get; set; }
        public bool useRealMass { get; set; }

        public StaticPhysicsConfig(float minMminS, float maxMminS, float minMmaxS)
        {
            _minMminS = minMminS;
            _maxMminS = maxMminS;
            _minMmaxS = minMmaxS;
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

            return ProportionalSum(effectiveMass, effectiveSoftness);
        }

        private float ProportionalSum(float mass, float softness) =>
            _minMminS +
            mass * (_maxMminS - _minMminS) +
            softness * (_minMmaxS - _minMminS);
    }
}
