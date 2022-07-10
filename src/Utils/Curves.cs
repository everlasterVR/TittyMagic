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

        // https://www.desmos.com/calculator/2nzb2miloz
        public static float Exponential1(float x, float b, float p, float q) =>
            (1 - b) * Mathf.Pow(x, p) + b * Mathf.Pow(x, q);
    }
}
