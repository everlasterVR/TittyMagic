using System;
using UnityEngine;

namespace everlaster
{
    public static class Calc
    {
        public static double OblateShperoidVolumeCM3(Vector3 size)
        {
            double equatorialDiameterCM = 100f * (size.x + size.y) / 2;
            double polarDiameterCM = 100f * size.z;
            return (Math.PI/6) * Math.Pow(equatorialDiameterCM, 2) * fixDepth(polarDiameterCM);
        }

        // z depth is too high for small breasts, leading to too much mass.
        // fixed here with an exponential curve
        // minimum size is about 240cm^3 e.g. 30B bra
        // maximum size at 2KG is about 2300cm^3 e.g. 36L bra
        private static double fixDepth(double polarDiameter)
        {
            return Math.Pow((polarDiameter * 0.50), 1.50);
        }

        public static float Roll(Quaternion q)
        {
            return Mathf.Rad2Deg * Mathf.Asin(2 * q.x * q.y + 2 * q.z * q.w);
        }

        public static float Pitch(Quaternion q)
        {
            return Mathf.Rad2Deg* Mathf.Atan2(2 * q.x * q.w - 2 * q.y * q.z, 1 - 2 * q.x * q.x - 2 * q.z * q.z);
        }

        // This is used to scale pitch effect by roll angle's distance from 90/-90 = person is sideways
        //-> if person is sideways, pitch related morphs have less effect
        public static float RollFactor(float roll)
        {
            return (90 - Mathf.Abs(roll)) / 90;
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
