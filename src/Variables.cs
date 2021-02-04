namespace TittyMagic
{
    public static class Const
    {
        public static float SOFTNESS_MIN = 0.5f;
        public static float SOFTNESS_MAX = 3.0f;

        public static float GRAVITY_MIN = 0.5f;
        public static float GRAVITY_MAX = 3.0f;
    }

    public static class Globals
    {

        public static AdjustJoints BREAST_CONTROL { get; set; }
        public static DAZPhysicsMesh BREAST_PHYSICS_MESH { get; set; }
        public static GenerateDAZMorphsControlUI MORPH_UI { get; set; }
    }
}
