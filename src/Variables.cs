using System.Collections.Generic;
using UnityEngine;

namespace TittyMagic
{
    public static class Const
    {
        public const float MASS_MIN = 0.1f;
        public const float MASS_MAX = 2.0f;

        public const float LEGACY_MIN = 0.5f; //since v2.1
        public const float LEGACY_MAX = 3.0f; //since v2.1

        public const float SOFTNESS_MIN = 0f;
        public const float SOFTNESS_MAX = 100f;

        public const float GRAVITY_MIN = 0f;
        public const float GRAVITY_MAX = 100f;

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

    public static class Mode
    {
        public const string ANIM_OPTIMIZED = "Animation optimized";
        public const string BALANCED = "Balanced";
        public const string TOUCH_OPTIMIZED = "Touch optimized";
    }

    public static class RefreshStatus
    {
        public const int DONE = 0;
        public const int WAITING = 1;
        public const int MASS_STARTED = 2;
        public const int MASS_OK = 3;
        public const int NEUTRALPOS_STARTED = 4;
        public const int NEUTRALPOS_OK = 5;
    }
}
