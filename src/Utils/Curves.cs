using UnityEngine;

namespace TittyMagic
{
    internal static class Curves
    {
        public static float QuadraticRegression(float f) =>
            -0.173f * f * f + 1.142f * f;

        public static float QuadraticRegressionLesser(float f) =>
            -0.115f * f * f + 1.12f * f;

        // ReSharper disable once UnusedMember.Local
        private static float Polynomial(
            float x,
            float a = 1,
            float b = 1,
            float c = 1,
            float p = 1,
            float q = 1
        ) => a * Mathf.Pow(x, p) + b * Mathf.Pow(x, q) + c * x;
    }
}
