namespace everlaster
{
    public static class Globals
    {
        public static AdjustJoints BREAST_CONTROL { get; set; }
        public static DAZPhysicsMesh BREAST_PHYSICS_MESH { get; set; }
        public static GenerateDAZMorphsControlUI MORPH_UI { get; set; }
    }

    public static class AngleTypes
    {
        public const string LEAN_FORWARD = "leanForward";
        public const string LEAN_BACK = "leanBack";
        public const string UPSIDE_DOWN = "upsideDown";
        public const string ROLL_RIGHT = "rollRight";
        public const string ROLL_LEFT = "rollLeft";
        public const string UPRIGHT = "upright";
        public const string PITCH = "pitch";
        public const string ROLL = "roll";
    }
}
