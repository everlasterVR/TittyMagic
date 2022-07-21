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

        //https://www.desmos.com/calculator/fe1ym6cf8u
        public static float MorphingCurve(float mass)
        {
            const float a = 10.4f;
            const float b = 2.5f;
            const float c = 2.8f;
            return a / Mathf.Sqrt(mass + b) - c;
        }

        public static float DepthMorphingCurve(float mass)
        {
            const float a = 10.1f;
            const float b = 2.86f;
            const float c = 2.36f;
            return a / Mathf.Sqrt(mass + b) - c;
        }

        public static float SoftnessBaseCurve(float softness) => Exponential1(softness, 6.44f, 1.27f, 1.15f);

        public static float PositionSpringZCurve(float x) => Exponential1(x, 1.93f, 2.1f, 0.85f, m: 0.98f);

        public static float ForcePhysicsSoftnessCurve(float x) => Exponential1(x, 1.91f, 1.7f, 0.82f);

        // https://www.desmos.com/calculator/9iy1ftweij
        public static float Exponential1(float x, float b, float p, float q, float m = 1, float s = 0) =>
            m * ((1 - b) * Mathf.Pow(x + s, p) + b * Mathf.Pow(x + s, q));
    }
}
