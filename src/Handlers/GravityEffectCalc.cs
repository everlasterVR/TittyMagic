using UnityEngine;

namespace TittyMagic.Handlers
{
    public static class GravityEffectCalc
    {
        public static float pectoralRollL { get; private set; }
        public static float pectoralPitchL { get; private set; }
        public static float pectoralRollR { get; private set; }
        public static float pectoralPitchR { get; private set; }
        public static float chestRoll { get; private set; }
        public static float chestPitch { get; private set; }

        public static void CalculateAngles(Rigidbody pectoralRbLeft, Rigidbody pectoralRbRight)
        {
            var lPectoralRotation = pectoralRbLeft.rotation;
            var rPectoralRotation = pectoralRbRight.rotation;

            pectoralRollL = Roll(lPectoralRotation);
            pectoralPitchL = Pitch(lPectoralRotation);
            pectoralRollR = Roll(rPectoralRotation);
            pectoralPitchR = Pitch(rPectoralRotation);
            chestRoll = Roll(MainPhysicsHandler.chestRb.rotation);
            chestPitch = Pitch(MainPhysicsHandler.chestRb.rotation);
        }

        private static float Roll(Quaternion q) =>
            2 * InverseLerpToPi(Mathf.Asin(2 * q.x * q.y + 2 * q.z * q.w));

        private static float Pitch(Quaternion q) =>
            InverseLerpToPi(Mathf.Atan2(2 * q.x * q.w - 2 * q.y * q.z, 1 - 2 * q.x * q.x - 2 * q.z * q.z));

        private static float InverseLerpToPi(float val) =>
            val > 0
                ? Mathf.InverseLerp(0, Mathf.PI, val)
                : -Mathf.InverseLerp(0, Mathf.PI, -val);

        // div by 2 because softness and mass affect equally
        public static float RollEffect(float roll, float multiplier) =>
            Mathf.Abs(roll) * multiplier / 2;

        // ReSharper disable once UnusedMember.Global
        public static float UpEffect(
            float pitch,
            float roll,
            float multiplier,
            float additionalRollEffect
        )
        {
            float effect = Mathf.Abs(pitch) * RollMultiplier(roll) / 2;
            return (effect + additionalRollEffect) * multiplier / 2;
        }

        // ReSharper disable once UnusedMember.Global
        public static float DownEffect(float pitch, float roll, float multiplier) =>
            (2 - Mathf.Abs(pitch) / 2) * RollMultiplier(roll) * multiplier / 2;

        public static float DepthAdjustByAngle(float pitch)
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

        public static float UpDownAdjustByAngle(float pitch)
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

        public static float DiffFromHorizontal(float pitch, float roll) =>
            Mathf.Abs(Mathf.Abs(pitch) - 1f) * RollMultiplier(roll);

        public static float RollMultiplier(float roll) => 1 - Mathf.Abs(roll);
    }
}
