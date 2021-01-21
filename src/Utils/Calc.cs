using System;
using UnityEngine;

namespace everlaster
{
    public static class Calc
    {
        // Experimentally determined that this somewhat accurately scales the Breast scale 
        // slider's effective value to the apparent breast size when body is scaled down/up.
        // Multiply by this when scaling down, divide when scaling up.
        public static float AtomScaleAdjustment(float value)
        {
            return 1 - (float) Math.Abs(Math.Log10(Math.Pow(value, 3)));
        }

        public static float Roll(Quaternion q)
        {
            return Mathf.Rad2Deg * Mathf.Asin(2 * q.x * q.y + 2 * q.z * q.w);
        }

        public static float Pitch(Quaternion q)
        {
            return Mathf.Rad2Deg* Mathf.Atan2(2 * q.x * q.w - 2 * q.y * q.z, 1 - 2 * q.x * q.x - 2 * q.z * q.z);
        }

        public static float Remap(float angle, float effect)
        {
            return angle * effect / 90;
        }

        public static double RoundToDecimals(float value, float roundFactor)
        {
            return Math.Round(value * roundFactor) / roundFactor;
        }

        // UNUSED
        public static float SinCurveMultiplier(float x, double midPoint = 0.5)
        {
            return (float) (
                midPoint * (Math.Sin(Math.PI * x - Math.PI/2) + 1)
            );
        }
    }
}
