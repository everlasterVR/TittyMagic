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

        // https://www.desmos.com/calculator/eicoieuczv
        public static float DepthMorphingCurve(float mass)
        {
            const float a = 10.1f;
            const float b = 2.86f;
            const float c = 2.36f;
            return a / Mathf.Sqrt(mass + b) - c;
        }

        public static float SoftnessBaseCurve(float x) => Exponential1(x, 6.44f, 1.27f, 1.15f);

        // https://www.desmos.com/calculator/shbc4eckoe
        public static float ForcePhysicsSoftnessCurve(float x) => Exponential1(x, 1.60f, 2.29f, 1.00f);

        // https://www.desmos.com/calculator/b8hxt91gkf
        public static float DeemphasizeMiddle(float x) => Exponential1(x, 3.00f, 3.53f, 1.22f, a: 1.20f, m: 0.72f);

        // https://www.desmos.com/calculator/lofyvjzy6l
        public static float TargetRotationMassCurve(float x) => Exponential1(2 / 3f * x, 4.00f, 0.96f, 0.78f);

        // https://www.desmos.com/calculator/ldejemzr2a
        public static float TargetRotationSoftnessCurve(float x) => Exponential1(x, 3.00f, 1.35f, 1.00f);

        // https://www.desmos.com/calculator/clksuexht8
        public static float SpringZSoftnessCurve(float x) => Exponential1(x, 2.11f, 1.21f, 0.88f);

        // https://www.desmos.com/calculator/uejk7yri1f
        public static float Exponential1(float x, float b, float p, float q, float a = 1, float m = 1, float s = 0) =>
            m * ((1 - b) * Mathf.Pow(a * x + s, p) + b * Mathf.Pow(a * x + s, q));

        // https://www.desmos.com/calculator/6pxtrvvyby
        public static float Exponential2(float x, float c, float s = 0)
        {
            float baseValue = (2 - c) * (x + s) - 1 + c / 2;
            return c / (1 + c / 3) + baseValue * baseValue;
        }

        // https://www.desmos.com/calculator/mpmm8lzvs2
        public static float MorphingCurve(float mass) => Exponential3(mass, 2.17f, 1.59f, 1.50f, 1.30f, 1.70f);

        // https://www.desmos.com/calculator/mpmm8lzvs2
        // ReSharper disable once MemberCanBePrivate.Global
        public static float Exponential3(float x, float c, float s, float l, float q, float p)
        {
            float xToLimitPow = Mathf.Pow(x / l, q);
            return c + s * Mathf.Pow(1 - xToLimitPow, p);
        }

        // https://www.desmos.com/calculator/ykxswso5ie
        public static float ForceEffectCurve(float x) => InverseSmoothStep(x, 10, 0.8f, 0f);

        // https://www.desmos.com/calculator/crrr1uryep
        // ReSharper disable once UnusedMember.Global
        public static float InverseSmoothStep(float value, float b, float curvature, float midpoint)
        {
            float result;
            if(value < 0)
            {
                result = 0;
            }
            else if(value > b)
            {
                result = 1;
            }
            else
            {
                float s = curvature < -2.99f ? -2.99f : curvature > 0.99f ? 0.99f : curvature;
                float p = midpoint * b;
                p = p < 0 ? 0 : p > b ? b : p;
                float c = 2 / (1 - s) - p / b;

                if(value < p)
                {
                    result = F1(value, b, p, c);
                }
                else
                {
                    result = 1 - F1(b - value, b, b - p, c);
                }
            }

            return result;
        }

        private static float F1(float value, float b, float n, float c) =>
            Mathf.Pow(value, c) / (b * Mathf.Pow(n, c - 1));
    }
}
