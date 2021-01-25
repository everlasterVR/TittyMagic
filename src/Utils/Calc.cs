using UnityEngine;

namespace everlaster
{
    public static class Calc
    {
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

        public static float RoundToDecimals(float value, float roundFactor)
        {
            return Mathf.Round(value * roundFactor) / roundFactor;
        }

        // UNUSED
        public static float SinCurveMultiplier(float x, float midPoint = 0.5f)
        {
            return (float) (
                midPoint * (Mathf.Sin(Mathf.PI * x - Mathf.PI/2) + 1)
            );
        }
    }
}
