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
    }
}
