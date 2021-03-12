using UnityEngine;

namespace TittyMagic
{
    public static class Const
    {
        public static float MASS_MIN = 0.1f;
        public static float MASS_MAX = 2.0f;

        public static float LEGACY_MIN = 0.5f; //since v2.1
        public static float LEGACY_MAX = 3.0f; //since v2.1

        public static float SOFTNESS_MIN = 0f;
        public static float SOFTNESS_MAX = 100f;

        public static float GRAVITY_MIN = 0f;
        public static float GRAVITY_MAX = 100f;

        public static float ConvertFromLegacyVal(float legacyVal)
        {
            float normalized = (legacyVal - LEGACY_MIN)/(LEGACY_MAX - LEGACY_MIN);
            return Calc.RoundToDecimals(Mathf.Lerp(SOFTNESS_MIN, SOFTNESS_MAX, normalized), 1f);
        }

        public static float ConvertToLegacyVal(float val)
        {
            float normalized = (val - SOFTNESS_MIN)/(SOFTNESS_MAX - SOFTNESS_MIN);
            return Mathf.Lerp(LEGACY_MIN, LEGACY_MAX, normalized);
        }
    }

    public static class Globals
    {
        public static AdjustJoints BREAST_CONTROL { get; set; }
        public static DAZPhysicsMesh BREAST_PHYSICS_MESH { get; set; }
        public static DAZCharacterSelector GEOMETRY { get; set; }
    }
}
