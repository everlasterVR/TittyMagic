using UnityEngine;

namespace TittyMagic
{
    internal static class Curves
    {
        // https://www.desmos.com/calculator/ebvzukk7ps
        public static float QuadraticRegression(float value) =>
            -0.118f * value * value + 0.97f * value;

        // ReSharper disable once UnusedMember.Local
        private static float Polynomial(
            float x,
            float a = 1,
            float b = 1,
            float c = 1,
            float p = 1,
            float q = 1
        ) => a * Mathf.Pow(x, p) + b * Mathf.Pow(x, q) + c * x;

        // https://www.desmos.com/calculator/fe1ym6cf8u
        public static float MorphingCurve(float mass)
        {
            const float a = 10.4f;
            const float b = 2.5f;
            const float c = 2.8f;
            return a / Mathf.Sqrt(mass + b) - c;
        }

        // https://www.desmos.com/calculator/eicoieuczv
        public static float DepthMorphingCurve(float mass)
        {
            const float a = 10.1f;
            const float b = 2.86f;
            const float c = 2.36f;
            return a / Mathf.Sqrt(mass + b) - c;
        }

        public static float SoftnessBaseCurve(float softness) => Exponential1(softness, 6.44f, 1.27f, 1.15f);

        public static float ForcePhysicsMassCurve(float x) => Exponential1(x, 1.91f, 1.7f, 0.82f);

        public static float ForcePhysicsSoftnessCurve(float x) => Exponential1(x, 1.91f, 1.7f, 0.82f);

        // https://www.desmos.com/calculator/b8hxt91gkf
        public static float DeemphasizeMiddle(float x) => Exponential1(x, 3.00f, 3.53f, 1.22f, a: 1.20f, m: 0.72f);

        public static float TargetRotationCurve(float x) => Exponential1(x, 3.00f, 1.35f, 1.00f);

        // https://www.desmos.com/calculator/uejk7yri1f
        public static float Exponential1(float x, float b, float p, float q, float a = 1, float m = 1, float s = 0) =>
            m * ((1 - b) * Mathf.Pow(a * x + s, p) + b * Mathf.Pow(a * x + s, q));

        // https://www.desmos.com/calculator/6pxtrvvyby
        public static float Exponential2(float x, float c, float s = 0)
        {
            float baseValue = (2 - c) * (x + s) - 1 + c / 2;
            return c / (1 + c / 3) + baseValue * baseValue;
        }
    }
}
