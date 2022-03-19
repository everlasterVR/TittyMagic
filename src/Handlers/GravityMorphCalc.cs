using UnityEngine;

namespace TittyMagic
{
    public static class GravityMorphCalc
    {
        public static float CalculateRollEffect(float roll, Multiplier multiplier)
        {
            // div by 2 because softness and mass affect equally
            return Mathf.Abs(roll) * multiplier.m.val / 2;
        }

        public static float CalculateUpEffect(float pitch, float roll, Multiplier multiplier, float additionalRollEffect)
        {
            float effect = Mathf.Abs(pitch) * (1 - Mathf.Abs(roll)) / 2;
            return (effect + additionalRollEffect) * multiplier.m.val / 2;
        }

        public static float CalculateDownEffect(float pitch, float roll, Multiplier multiplier)
        {
            return (2 - (Mathf.Abs(pitch) / 2)) * multiplier.m.val * (1 - Mathf.Abs(roll)) / 2;
        }

        public static float CalculateDepthEffect(float pitch, float roll, Multiplier multiplier)
        {
            return DepthAdjustByAngle(pitch) * multiplier.m.val * (1 - Mathf.Abs(roll)) / 2;
        }

        private static float DepthAdjustByAngle(float pitch)
        {
            // leaning forward
            if(pitch >= 0)
            {
                // upright
                if(pitch < 1)
                {
                    return pitch;
                }

                // upside down
                return 2 - pitch;
            }

            // leaning back
            // upright
            if(pitch >= -1)
            {
                return -pitch;
            }

            // upside down
            return 2 + pitch;
        }
    }
}
