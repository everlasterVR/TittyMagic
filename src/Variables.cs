namespace TittyMagic
{
    internal static class Const
    {
        public const float MASS_MIN = 0.1f;
        public const float MASS_MAX = 2.0f;
    }

    internal static class Globals
    {
        public static string SAVES_DIR { get; set; }
        public static string MORPHMULTIPLIERS_DIRNAME { get; set; }
        public static string PLUGIN_PATH { get; set; }
        public static string MORPHS_PATH { get; set; }
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

    public static class Direction
    {
        public const string DOWN = "DOWN";
        public const string DOWN_L = "DOWN_L";
        public const string DOWN_R = "DOWN_R";
        public const string UP = "UP";
        public const string UP_L = "UP_L";
        public const string UP_R = "UP_R";
        public const string UP_C = "UP_C";
        public const string BACK = "BACK";
        public const string FORWARD = "FORWARD";
        public const string LEFT = "LEFT";
        public const string LEFT_L = "LEFT_L";
        public const string LEFT_R = "LEFT_R";
        public const string RIGHT = "RIGHT";
        public const string RIGHT_L = "RIGHT_L";
        public const string RIGHT_R = "RIGHT_R";
    }

    internal static class RefreshStatus
    {
        public const int DONE = 0;
        public const int WAITING = 1;
        public const int MASS_STARTED = 2;
        public const int MASS_OK = 3;
        public const int NEUTRALPOS_STARTED = 4;
        public const int NEUTRALPOS_OK = 5;
    }
}
