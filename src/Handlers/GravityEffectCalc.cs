using UnityEngine;
using TittyMagic.Configs;

namespace TittyMagic
{
    public static class GravityEffectCalc
    {
        // div by 2 because softness and mass affect equally
        public static float CalculateRollEffect(float roll, Multiplier multiplier) =>
            Mathf.Abs(roll) * multiplier.mainMultiplier / 2;

        // ReSharper disable once UnusedMember.Global
        public static float CalculateUpEffect(
            float pitch,
            float roll,
            Multiplier multiplier,
            float additionalRollEffect
        )
        {
            float effect = Mathf.Abs(pitch) * RollMultiplier(roll) / 2;
            return (effect + additionalRollEffect) * multiplier.mainMultiplier / 2;
        }

        // ReSharper disable once UnusedMember.Global
        public static float CalculateDownEffect(float pitch, float roll, Multiplier multiplier) =>
            (2 - Mathf.Abs(pitch) / 2) * RollMultiplier(roll) * multiplier.mainMultiplier / 2;

        public static float CalculateDepthEffect(float pitch, float roll, Multiplier multiplier) =>
            DepthAdjustByAngle(pitch) * RollMultiplier(roll) * multiplier.mainMultiplier / 2;

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

        public static float CalculateUpDownEffect(float pitch, float roll, Multiplier multiplier) =>
            UpDownAdjustByAngle(pitch) *
            RollMultiplier(roll) *
            multiplier.mainMultiplier *
            (multiplier.extraMultiplier ?? 1) / 2;

        private static float UpDownAdjustByAngle(float pitch)
        {
            // leaning forward
            if(pitch >= 0)
            {
                // upright
                if(pitch < 1)
                {
                    return 1 - pitch;
                }

                // upside down
                return pitch - 1;
            }

            // leaning back
            // upright
            if(pitch >= -1)
            {
                return 1 + pitch;
            }

            // upside down
            return -pitch - 1;
        }

        public static float CalculateDiffFromHorizontal(float pitch, float roll)
        {
            float diff = pitch >= 0 ? 0.5f - pitch : 0.5f + pitch;
            return 2 * diff * RollMultiplier(roll);
        }

        private static float RollMultiplier(float roll) => 1 - Mathf.Abs(roll);
    }
}
