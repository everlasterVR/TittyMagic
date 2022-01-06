using UnityEngine;

namespace TittyMagic
{
    public static class Calc
    {
        // value between -1 and +1
        // +1 = leaning 90 degrees left
        // -1 = leaning 90 degrees right
        public static float Roll(Quaternion q)
        {
            return 2 * InverseLerpToPi(Mathf.Asin(2 * q.x * q.y + 2 * q.z * q.w));
        }

        // value between -2 and 2
        // +2 = upright
        // +1 = horizontal, on stomach
        // -1 = horizontal, on back
        // -2 = upside down
        public static float Pitch(Quaternion q)
        {
            return 2 * InverseLerpToPi(Mathf.Atan2(2 * q.x * q.w - 2 * q.y * q.z, 1 - 2 * q.x * q.x - 2 * q.z * q.z));
        }

        private static float InverseLerpToPi(float val)
        {
            if(val > 0)
            {
                return Mathf.InverseLerp(0, Mathf.PI, val);
            }

            return -Mathf.InverseLerp(0, -Mathf.PI, val);
        }

        public static float RoundToDecimals(float value, float roundFactor)
        {
            return Mathf.Round(value * roundFactor) / roundFactor;
        }
    }
}
