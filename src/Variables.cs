using System.Collections.Generic;

namespace TittyMagic
{
    internal static class Const
    {
        public const float MASS_MIN = 0.1f;
        public const float MASS_MAX = 2.0f;
    }

    internal static class VertexIndexGroups
    {
        // excludes areola and nipple vertices
        public static readonly HashSet<int> BREASTS = new HashSet<int> { 8, 16, 17, 93, 135, 136, 137, 138, 2401, 2402, 2403, 2404, 2405, 2406, 2407, 2408, 2409, 2410, 2411, 2412, 2413, 2414, 2415, 2416, 2417, 2418, 2419, 2590, 2591, 2592, 2594, 2596, 2597, 2598, 2871, 2872, 2873, 7216, 7218, 7219, 7220, 7230, 7231, 7232, 7233, 7234, 7235, 7236, 7237, 7238, 7239, 7240, 7241, 7242, 7243, 7244, 7245, 7246, 7247, 7248, 8833, 8834, 8836, 8837, 8838, 8839, 8840, 8848, 8849, 8850, 8851, 8852, 8853, 8854, 10936, 10944, 10945, 11021, 11063, 11064, 11065, 11066, 13231, 13232, 13233, 13234, 13235, 13236, 13237, 13238, 13239, 13240, 13241, 13242, 13243, 13244, 13245, 13246, 13247, 13248, 13249, 13408, 13409, 13410, 13411, 13412, 13413, 13414, 13672, 13673, 13674, 17923, 17924, 17925, 17926, 17936, 17937, 17938, 17939, 17940, 17941, 17942, 17943, 17944, 17945, 17946, 17947, 17948, 17949, 17950, 17951, 17952, 17953, 17954, 19504, 19505, 19507, 19508, 19509, 19510, 19511, 19519, 19520, 19521, 19522, 19523, 19524, 19525, 19639 };

        // excludes nipple vertices
        // excludes indexes that (for some reason) don't get updated vertex positions in DAZSkinV2.rawSkinnedVertices when male skinned person is moved around or morphed
        public static readonly List<int> LEFT_BREAST = new List<int> { 2403, 2411, 2592, 7197, 7208, 8902, 8904, 8905, 8908, 8909, 8911, 8915, 8917, 8922, 8924, 8925, 8926, 8928, 8930, 8933, 8934, 8935, 8939, 8941, 8942, 8943, 8945, 8946, 8947, 8948, 8949, 8951, 8952, 8954, 8959, 8969, 8970 };
        public static readonly List<int> RIGHT_BREAST = new List<int> { 13233, 13241, 13410, 17904, 17915, 19568, 19570, 19571, 19574, 19575, 19577, 19581, 19583, 19588, 19590, 19591, 19592, 19594, 19596, 19599, 19600, 19601, 19605, 19607, 19608, 19609, 19611, 19612, 19613, 19614, 19615, 19617, 19618, 19620, 19625, 19635, 19636 };

        // subsets of LEFT_BREAST and RIGHT_BREAST - vertices closest to center
        public static readonly List<int> LEFT_BREAST_CENTER = new List<int> { 7197, 7208, 8902, 8904, 8905, 8908, 8909, 8911, 8915, 8917, 8922, 8924 };
        public static readonly List<int> RIGHT_BREAST_CENTER = new List<int> { 17904, 17915, 19568, 19570, 19571, 19574, 19575, 19577, 19581, 19583, 19588, 19590 };
    }

    // ReSharper disable InconsistentNaming
    internal static class Globals
    {
        public static string SAVES_DIR { get; set; }
        public static string PLUGIN_PATH { get; set; }
        public static string MORPHS_PATH { get; set; }
        public static AdjustJoints BREAST_CONTROL { get; set; }
        public static DAZPhysicsMesh BREAST_PHYSICS_MESH { get; set; }
        public static DAZCharacterSelector GEOMETRY { get; set; }
        public static GenerateDAZMorphsControlUI MORPHS_CONTROL_UI { get; set; }
    }
    // ReSharper restore InconsistentNaming

    public static class Direction
    {
        public const string DOWN = "DOWN";
        public const string UP = "UP";
        public const string UP_L = "UP_L";
        public const string UP_R = "UP_R";
        public const string UP_C = "UP_C";
        public const string BACK = "BACK";
        public const string BACK_L = "BACK_L";
        public const string BACK_R = "BACK_R";
        public const string BACK_C = "BACK_C";
        public const string FORWARD = "FORWARD";
        public const string FORWARD_L = "FORWARD_L";
        public const string FORWARD_R = "FORWARD_R";
        public const string FORWARD_C = "FORWARD_C";
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
        public const int PRE_REFRESH_STARTED = 2;
        public const int PRE_REFRESH_OK = 3;
        public const int MASS_STARTED = 4;
        public const int MASS_OK = 5;
        public const int NEUTRALPOS_STARTED = 6;
        public const int NEUTRALPOS_OK = 7;
    }
}
