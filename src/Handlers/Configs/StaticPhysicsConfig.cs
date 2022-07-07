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

        private Func<float, float> _softnessCurveLower;
        private Func<float, float> _softnessCurveUpper;
        private float? softnessCurveCutoff { get; set; }

        public StaticPhysicsConfig(float minMminS, float maxMminS, float minMmaxS)
        {
            _minMminS = minMminS;
            _maxMminS = maxMminS;
            _minMmaxS = minMmaxS;
        }

        // Transition between two linear functions at mid point. 0.6285f is softnessAmount at slider 50
        // https://www.desmos.com/calculator/traqqeb88h
        // slope is decided such that after mid point, the value behaves as before minMminS and maxMminS were increased
        public void SetLinearCurvesAroundMidpoint(float slope, float cutoff)
        {
            softnessCurveCutoff = cutoff;
            _softnessCurveUpper = x => slope * x + (1 - slope);
            _softnessCurveLower = x => _softnessCurveUpper(cutoff) / cutoff * x;
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
                    ? _softnessCurveUpper(softness)
                    : _softnessCurveLower(softness);
            }

            return ProportionalSum(effectiveMass, effectiveSoftness);
        }

        private float ProportionalSum(float mass, float softness) =>
            _minMminS +
            mass * (_maxMminS - _minMminS) +
            softness * (_minMmaxS - _minMminS);
    }
}
