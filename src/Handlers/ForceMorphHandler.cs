using System;
using System.Collections.Generic;
using TittyMagic.Components;
using TittyMagic.Handlers.Configs;
using UnityEngine;
using static TittyMagic.Script;

namespace TittyMagic.Handlers
{
    public static class ForceMorphHandler
    {
        private static TrackBreast _trackLeftBreast;
        private static TrackBreast _trackRightBreast;

        public static Dictionary<string, List<MorphConfig>> configSets { get; private set; }

        public static JSONStorableFloat baseJsf { get; private set; }
        public static JSONStorableFloat upJsf { get; private set; }
        public static JSONStorableFloat forwardJsf { get; private set; }
        public static JSONStorableFloat backJsf { get; private set; }
        public static JSONStorableFloat leftRightJsf { get; private set; }

        public static float upDownExtraMultiplier { get; set; }
        public static float forwardExtraMultiplier { get; set; }
        public static float backExtraMultiplier { get; set; }
        public static float leftRightExtraMultiplier { get; set; }

        private static float upMultiplier => baseJsf.val * upJsf.val;
        private static float forwardMultiplier => baseJsf.val * forwardJsf.val;
        private static float backMultiplier => baseJsf.val * backJsf.val;
        private static float leftRightMultiplier => baseJsf.val * leftRightJsf.val;

        public static JSONStorableStringChooser directionChooser { get; private set; }

        public static void Init()
        {
            _trackLeftBreast = tittyMagic.trackLeftBreast;
            _trackRightBreast = tittyMagic.trackRightBreast;

            baseJsf = tittyMagic.NewJSONStorableFloat("forceMorphingBase", 1.00f, 0.00f, 2.00f);
            upJsf = tittyMagic.NewJSONStorableFloat("forceMorphingUp", 1.00f, 0.00f, 2.00f);
            forwardJsf = tittyMagic.NewJSONStorableFloat("forceMorphingForward", 1.00f, 0.00f, 2.00f);
            backJsf = tittyMagic.NewJSONStorableFloat("forceMorphingBack", 1.00f, 0.00f, 2.00f);
            leftRightJsf = tittyMagic.NewJSONStorableFloat("forceMorphingLeftRight", 1.00f, 0.00f, 2.00f);
        }

        public static void LoadSettings()
        {
            configSets = new Dictionary<string, List<MorphConfig>>
            {
                { Direction.UP_L, UpForceConfigs("L") },
                { Direction.UP_R, UpForceConfigs("R") },
                { Direction.UP_C, UpForceCenterConfigs() },
                { Direction.BACK_L, BackForceConfigs("L") },
                { Direction.BACK_R, BackForceConfigs("R") },
                { Direction.BACK_C, BackForceCenterConfigs() },
                { Direction.FORWARD_L, ForwardForceConfigs("L") },
                { Direction.FORWARD_R, ForwardForceConfigs("R") },
                { Direction.FORWARD_C, ForwardForceCenterConfigs() },
                { Direction.LEFT_L, LeftForceLeftConfigs() },
                { Direction.LEFT_R, LeftForceRightConfigs() },
                { Direction.RIGHT_L, RightForceLeftConfigs() },
                { Direction.RIGHT_R, RightForceRightConfigs() },
            };

            var directions = new List<string>
            {
                Direction.UP,
                Direction.UP_C,
                Direction.BACK,
                Direction.BACK_C,
                Direction.FORWARD,
                Direction.FORWARD_C,
                Direction.LEFT_L,
                Direction.LEFT_R,
                Direction.RIGHT_L,
                Direction.RIGHT_R,
            };
            directionChooser = new JSONStorableStringChooser("direction", directions, directions[0], "Direction");
        }

        #region Morph configs

        private static List<MorphConfig> UpForceConfigs(string side) => new List<MorphConfig>
        {
            new MorphConfig($"UP/UP Breast Height {side}",
                false,
                new JSONStorableFloat("softMultiplier", 0.70f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.35f, -3.00f, 3.00f)
            ),
            new MorphConfig($"UP/UP Breast Sag1 {side}",
                true,
                new JSONStorableFloat("softMultiplier", -0.75f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.25f, -3.00f, 3.00f)
            ),
            new MorphConfig($"UP/UP Breast Sag2 {side}",
                true,
                new JSONStorableFloat("softMultiplier", -1.00f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.33f, -3.00f, 3.00f)
            ),
            new MorphConfig($"UP/UP Breasts Natural {side}",
                true,
                new JSONStorableFloat("softMultiplier", -0.75f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.25f, -3.00f, 3.00f)
            ),
            new MorphConfig($"UP/UP Breast Rotate Up {side}",
                false,
                new JSONStorableFloat("softMultiplier", 0.80f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.20f, -3.00f, 3.00f)
            ),
            new MorphConfig($"UP/UP Breast Top Curve2 {side}",
                true,
                new JSONStorableFloat("softMultiplier", -1.20f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.60f, -3.00f, 3.00f)
            ),
            new MorphConfig($"UP/UP Breasts Implants {side}",
                false,
                new JSONStorableFloat("softMultiplier", 0.25f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.05f, -3.00f, 3.00f)
            ),
            new MorphConfig($"UP/UP Breast Diameter {side}",
                false,
                new JSONStorableFloat("softMultiplier", 0.48f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.16f, -3.00f, 3.00f)
            ),
            new MorphConfig($"UP/UP Breast Diameter(Pose) {side}",
                false,
                new JSONStorableFloat("softMultiplier", 1.50f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.50f, -3.00f, 3.00f)
            ),
            new MorphConfig($"UP/UP Breast Width {side}",
                false,
                new JSONStorableFloat("softMultiplier", 0.21f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.07f, -3.00f, 3.00f)
            ),
            new MorphConfig($"UP/UP Breast Zero {side}",
                false,
                new JSONStorableFloat("softMultiplier", 0.36f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.18f, -3.00f, 3.00f)
            ),
            new MorphConfig($"UP/UP Breast flat(Fixed) {side}",
                false,
                new JSONStorableFloat("softMultiplier", 0.65f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.10f, -3.00f, 3.00f)
            ),
            new MorphConfig($"UP/UP BreastsShape2 {side}",
                false,
                new JSONStorableFloat("softMultiplier", 1.80f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.35f, -3.00f, 3.00f)
            ),
            new MorphConfig($"UP/UP Breast Height Lower {side}",
                true,
                new JSONStorableFloat("softMultiplier", -1.50f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.40f, -3.00f, 3.00f)
            ),
            new MorphConfig($"UP/UP Breast Move Up {side}",
                false,
                new JSONStorableFloat("softMultiplier", 1.60f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.40f, -3.00f, 3.00f)
            ),
            new MorphConfig($"UP/UP Breasts Flatten {side}",
                false,
                new JSONStorableFloat("softMultiplier", 0.60f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.60f, -3.00f, 3.00f)
            ),
            new MorphConfig($"UP/UP Breasts TogetherApart {side}",
                false,
                new JSONStorableFloat("softMultiplier", 0.90f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.30f, -3.00f, 3.00f)
            ),
            new MorphConfig($"UP/UP Breast Top Curve1 {side}",
                true,
                new JSONStorableFloat("softMultiplier", -0.75f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.25f, -3.00f, 3.00f)
            ),
            new MorphConfig($"UP/UP Breasts Under Curve {side}",
                true,
                new JSONStorableFloat("softMultiplier", -1.50f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.50f, -3.00f, 3.00f)
            ),
            new MorphConfig($"UP/UP ChestUnderBreast {side}",
                false,
                new JSONStorableFloat("softMultiplier", 0.80f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.25f, -3.00f, 3.00f)
            ),
            new MorphConfig($"UP/UP Breast Under Smoother1 {side}",
                false,
                new JSONStorableFloat("softMultiplier", 0.25f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.00f, -3.00f, 3.00f)
            ),
            new MorphConfig($"UP/UP Breast Under Smoother2 {side}",
                false,
                new JSONStorableFloat("softMultiplier", 0.40f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.20f, -3.00f, 3.00f)
            ),
            new MorphConfig($"UP/UP Breast Under Smoother3 {side}",
                false,
                new JSONStorableFloat("softMultiplier", 0.15f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.15f, -3.00f, 3.00f)
            ),
            new MorphConfig($"UP/UP Breast Under Smoother4 {side}",
                false,
                new JSONStorableFloat("softMultiplier", 0.30f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.10f, -3.00f, 3.00f)
            ),
            new MorphConfig($"UP/UP Breast Height Upper {side}",
                false,
                new JSONStorableFloat("softMultiplier", 0.50f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.50f, -3.00f, 3.00f)
            ),
            new MorphConfig($"UP/UP Breasts Upward Slope {side}",
                false,
                new JSONStorableFloat("softMultiplier", 1.30f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 2.00f, -3.00f, 3.00f)
            ),
            new MorphConfig($"UP/UP Breast upper down {side}",
                false,
                new JSONStorableFloat("softMultiplier", 0.15f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.05f, -3.00f, 3.00f)
            ),
            new MorphConfig($"UP/UP Breasts Small Top Slope {side}",
                false,
                new JSONStorableFloat("softMultiplier", 0.06f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.02f, -3.00f, 3.00f)
            ),
            new MorphConfig($"UP/UP Areolae Depth {side}",
                false,
                new JSONStorableFloat("softMultiplier", 0.30f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.10f, -3.00f, 3.00f)
            ),
        };

        private static List<MorphConfig> UpForceCenterConfigs() => new List<MorphConfig>
        {
            new MorphConfig("UP/UP Center Gap Smooth",
                false,
                new JSONStorableFloat("softMultiplier", 1.70f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.20f, -3.00f, 3.00f)
            ),
            new MorphConfig("UP/UP Center Gap Depth",
                true,
                new JSONStorableFloat("softMultiplier", -0.50f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.80f, -3.00f, 3.00f)
            ),
            new MorphConfig("UP/UP Center Gap Height",
                false,
                new JSONStorableFloat("softMultiplier", 1.20f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.50f, -3.00f, 3.00f)
            ),
            new MorphConfig("UP/UP Centre Gap Wide",
                false,
                new JSONStorableFloat("softMultiplier", 0.50f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.25f, -3.00f, 3.00f)
            ),
            new MorphConfig("UP/UP Center Gap UpDown",
                false,
                new JSONStorableFloat("softMultiplier", 2.00f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.30f, -3.00f, 3.00f)
            ),
            new MorphConfig("UP/UP Chest Smoother",
                false,
                new JSONStorableFloat("softMultiplier", 3.00f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.75f, -3.00f, 3.00f)
            ),
            new MorphConfig("UP/UP ChestSmoothCenter",
                false,
                new JSONStorableFloat("softMultiplier", 3.00f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.84f, -3.00f, 3.00f)
            ),
        };

        private static List<MorphConfig> BackForceConfigs(string side) => new List<MorphConfig>
        {
            new MorphConfig($"BK/BK Breast Diameter {side}",
                false,
                new JSONStorableFloat("softMultiplier", 1.20f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.40f, -3.00f, 3.00f)
            ),
            new MorphConfig($"BK/BK Breast flat(Fixed) {side}",
                false,
                new JSONStorableFloat("softMultiplier", 0.54f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.18f, -3.00f, 3.00f)
            ),
            new MorphConfig($"BK/BK Breast Pointed {side}",
                true,
                new JSONStorableFloat("softMultiplier", -0.40f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.13f, -3.00f, 3.00f)
            ),
            new MorphConfig($"BK/BK Breast Top Curve1 {side}",
                true,
                new JSONStorableFloat("softMultiplier", -0.66f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.22f, -3.00f, 3.00f)
            ),
            new MorphConfig($"BK/BK Breast Top Curve2 {side}",
                false,
                new JSONStorableFloat("softMultiplier", 0.30f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.10f, -3.00f, 3.00f)
            ),
            new MorphConfig($"BK/BK Breast Under Smoother1 {side}",
                false,
                new JSONStorableFloat("softMultiplier", 0.70f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.35f, -3.00f, 3.00f)
            ),
            new MorphConfig($"BK/BK Breast Under Smoother2 {side}",
                false,
                new JSONStorableFloat("softMultiplier", 0.07f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.21f, -3.00f, 3.00f)
            ),
            new MorphConfig($"BK/BK Breast Under Smoother3 {side}",
                false,
                new JSONStorableFloat("softMultiplier", 0.60f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.20f, -3.00f, 3.00f)
            ),
            new MorphConfig($"BK/BK Breast Zero {side}",
                false,
                new JSONStorableFloat("softMultiplier", 0.24f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.08f, -3.00f, 3.00f)
            ),
            new MorphConfig($"BK/BK Breasts Flatten {side}",
                false,
                new JSONStorableFloat("softMultiplier", 1.50f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.50f, -3.00f, 3.00f)
            ),
            new MorphConfig($"BK/BK Breasts Hang Forward {side}",
                true,
                new JSONStorableFloat("softMultiplier", -0.20f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.07f, -3.00f, 3.00f)
            ),
            new MorphConfig($"BK/BK Breasts Implants {side}",
                true,
                new JSONStorableFloat("softMultiplier", -0.06f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.04f, -3.00f, 3.00f)
            ),
            new MorphConfig($"BK/BK Breasts TogetherApart {side}",
                false,
                new JSONStorableFloat("softMultiplier", 0.15f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.15f, -3.00f, 3.00f)
            ),
            new MorphConfig($"BK/BK Depth Squash {side}",
                false,
                new JSONStorableFloat("softMultiplier", 1.80f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.60f, -3.00f, 3.00f)
            ),
        };

        private static List<MorphConfig> BackForceCenterConfigs() => new List<MorphConfig>
        {
            new MorphConfig("BK/BK Sternum Depth",
                true,
                new JSONStorableFloat("softMultiplier", 0.10f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.40f, -3.00f, 3.00f)
            ),
            new MorphConfig("BK/BK Sternum Width",
                true,
                new JSONStorableFloat("softMultiplier", -0.30f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.30f, -3.00f, 3.00f)
            ),
            new MorphConfig("BK/BK Center Gap Depth",
                true,
                new JSONStorableFloat("softMultiplier", 0.00f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.40f, -3.00f, 3.00f)
            ),
        };

        private static List<MorphConfig> ForwardForceConfigs(string side) => new List<MorphConfig>
        {
            new MorphConfig($"FW/FW Breast Diameter(Pose) {side}",
                false,
                new JSONStorableFloat("softMultiplier", 0.40f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.20f, -3.00f, 3.00f)
            ),
            new MorphConfig($"FW/FW Breast Height2 {side}",
                true,
                new JSONStorableFloat("softMultiplier", -0.40f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.20f, -3.00f, 3.00f)
            ),
            new MorphConfig($"FW/FW Breast Rotate Down {side}",
                true,
                new JSONStorableFloat("softMultiplier", -0.20f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.10f, -3.00f, 3.00f)
            ),
            new MorphConfig($"FW/FW Breast Width {side}",
                true,
                new JSONStorableFloat("softMultiplier", -0.25f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.12f, -3.00f, 3.00f)
            ),
            new MorphConfig($"FW/FW Breast Depth {side}",
                false,
                new JSONStorableFloat("softMultiplier", 0.88f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.44f, -3.00f, 3.00f)
            ),
            new MorphConfig($"FW/FW Breast Small {side}",
                false,
                new JSONStorableFloat("softMultiplier", 0.50f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.25f, -3.00f, 3.00f)
            ),
            new MorphConfig($"FW/FW Breast Round {side}",
                true,
                new JSONStorableFloat("softMultiplier", -1.10f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.55f, -3.00f, 3.00f)
            ),
        };

        private static List<MorphConfig> ForwardForceCenterConfigs() => new List<MorphConfig>
        {
            new MorphConfig("FW/FW Sternum Width",
                false,
                new JSONStorableFloat("softMultiplier", 0.41f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.14f, -3.00f, 3.00f)
            ),
            new MorphConfig("FW/FW ChestSeparateBreasts",
                false,
                new JSONStorableFloat("softMultiplier", 0.00f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.30f, -3.00f, 3.00f)
            ),
        };

        private static List<MorphConfig> LeftForceLeftConfigs() => new List<MorphConfig>
        {
            new MorphConfig("LT/LT Breasts Shift S2S Left L",
                false,
                new JSONStorableFloat("softMultiplier", 1.50f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.30f, -3.00f, 3.00f)
            ),
            new MorphConfig("LT/LT Breasts Hang Forward L",
                true,
                new JSONStorableFloat("softMultiplier", -1.00f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.80f, -3.00f, 3.00f)
            ),
            new MorphConfig("LT/LT Breast Width L",
                true,
                new JSONStorableFloat("softMultiplier", -0.30f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.10f, -3.00f, 3.00f)
            ),
            new MorphConfig("LT/LT Breast Rotate X Out L",
                true,
                new JSONStorableFloat("softMultiplier", -0.60f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.30f, -3.00f, 3.00f)
            ),
            new MorphConfig("LT/LT Breast Rotate X In L",
                true,
                new JSONStorableFloat("softMultiplier", -0.30f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.10f, -3.00f, 3.00f)
            ),
            new MorphConfig("LT/LT Breast Move S2S Out L",
                false,
                new JSONStorableFloat("softMultiplier", 0.30f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.10f, -3.00f, 3.00f)
            ),
            new MorphConfig("LT/LT Breast Diameter L",
                true,
                new JSONStorableFloat("softMultiplier", -0.20f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.20f, -3.00f, 3.00f)
            ),
            new MorphConfig("LT/LT Breast Under Smoother2 L",
                false,
                new JSONStorableFloat("softMultiplier", 0.10f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.40f, -3.00f, 3.00f)
            ),
            new MorphConfig("LT/LT Breast Under Smoother3 L",
                false,
                new JSONStorableFloat("softMultiplier", 0.40f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.40f, -3.00f, 3.00f)
            ),
        };

        private static List<MorphConfig> LeftForceRightConfigs() => new List<MorphConfig>
        {
            new MorphConfig("LT/LT Breasts Shift S2S Left R",
                false,
                new JSONStorableFloat("softMultiplier", 2.00f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.50f, -3.00f, 3.00f)
            ),
            new MorphConfig("LT/LT Breast Rotate X Out R",
                true,
                new JSONStorableFloat("softMultiplier", -1.50f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.25f, -3.00f, 3.00f)
            ),
            new MorphConfig("LT/LT Breasts Flatten R",
                true,
                new JSONStorableFloat("softMultiplier", -0.40f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.20f, -3.00f, 3.00f)
            ),
            new MorphConfig("LT/LT Breast Width R",
                false,
                new JSONStorableFloat("softMultiplier", 0.30f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.40f, -3.00f, 3.00f)
            ),
            new MorphConfig("LT/LT Breast Rotate X In R",
                false,
                new JSONStorableFloat("softMultiplier", 0.20f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.00f, -3.00f, 3.00f)
            ),
            new MorphConfig("LT/LT Breasts Hang Forward R",
                false,
                new JSONStorableFloat("softMultiplier", 0.30f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.00f, -3.00f, 3.00f)
            ),
            new MorphConfig("LT/LT Areola S2S R",
                false,
                new JSONStorableFloat("softMultiplier", 0.50f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 2.00f, -3.00f, 3.00f)
            ),
            new MorphConfig("LT/LT Breast Diameter R",
                true,
                new JSONStorableFloat("softMultiplier", -0.20f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.20f, -3.00f, 3.00f)
            ),
            new MorphConfig("LT/LT Breast Depth Squash R",
                false,
                new JSONStorableFloat("softMultiplier", 0.20f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 2.00f, -3.00f, 3.00f)
            ),
            new MorphConfig("LT/LT Breast Move S2S In R",
                false,
                new JSONStorableFloat("softMultiplier", 0.00f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.30f, -3.00f, 3.00f)
            ),
            new MorphConfig("LT/LT Breasts Implants R",
                true,
                new JSONStorableFloat("softMultiplier", 0.10f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.10f, -3.00f, 3.00f)
            ),
            new MorphConfig("LT/LT Breast Under Smoother2 R",
                false,
                new JSONStorableFloat("softMultiplier", 0.10f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.40f, -3.00f, 3.00f)
            ),
            new MorphConfig("LT/LT Breast Under Smoother3 R",
                false,
                new JSONStorableFloat("softMultiplier", 0.40f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.40f, -3.00f, 3.00f)
            ),
        };

        private static List<MorphConfig> RightForceLeftConfigs() => new List<MorphConfig>
        {
            new MorphConfig("RT/RT Breasts Shift S2S Right L",
                false,
                new JSONStorableFloat("softMultiplier", 2.00f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.50f, -3.00f, 3.00f)
            ),
            new MorphConfig("RT/RT Breast Rotate X Out L",
                true,
                new JSONStorableFloat("softMultiplier", -1.50f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.25f, -3.00f, 3.00f)
            ),
            new MorphConfig("RT/RT Breasts Flatten L",
                true,
                new JSONStorableFloat("softMultiplier", -0.40f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.20f, -3.00f, 3.00f)
            ),
            new MorphConfig("RT/RT Breast Width L",
                false,
                new JSONStorableFloat("softMultiplier", 0.30f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.40f, -3.00f, 3.00f)
            ),
            new MorphConfig("RT/RT Breast Rotate X In L",
                false,
                new JSONStorableFloat("softMultiplier", 0.20f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.00f, -3.00f, 3.00f)
            ),
            new MorphConfig("RT/RT Breasts Hang Forward L",
                false,
                new JSONStorableFloat("softMultiplier", 0.30f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.00f, -3.00f, 3.00f)
            ),
            new MorphConfig("RT/RT Areola S2S L",
                false,
                new JSONStorableFloat("softMultiplier", 0.50f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 2.00f, -3.00f, 3.00f)
            ),
            new MorphConfig("RT/RT Breast Diameter L",
                true,
                new JSONStorableFloat("softMultiplier", -0.20f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.20f, -3.00f, 3.00f)
            ),
            new MorphConfig("RT/RT Breast Depth Squash L",
                false,
                new JSONStorableFloat("softMultiplier", 0.20f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 2.00f, -3.00f, 3.00f)
            ),
            new MorphConfig("RT/RT Breast Move S2S In L",
                false,
                new JSONStorableFloat("softMultiplier", 0.00f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.30f, -3.00f, 3.00f)
            ),
            new MorphConfig("RT/RT Breasts Implants L",
                true,
                new JSONStorableFloat("softMultiplier", 0.10f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.10f, -3.00f, 3.00f)
            ),
            new MorphConfig("RT/RT Breast Under Smoother2 L",
                false,
                new JSONStorableFloat("softMultiplier", 0.10f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.40f, -3.00f, 3.00f)
            ),
            new MorphConfig("RT/RT Breast Under Smoother3 L",
                false,
                new JSONStorableFloat("softMultiplier", 0.40f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.40f, -3.00f, 3.00f)
            ),
        };

        private static List<MorphConfig> RightForceRightConfigs() => new List<MorphConfig>
        {
            new MorphConfig("RT/RT Breasts Shift S2S Right R",
                false,
                new JSONStorableFloat("softMultiplier", 1.50f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.30f, -3.00f, 3.00f)
            ),
            new MorphConfig("RT/RT Breasts Hang Forward R",
                true,
                new JSONStorableFloat("softMultiplier", -1.00f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.80f, -3.00f, 3.00f)
            ),
            new MorphConfig("RT/RT Breast Width R",
                true,
                new JSONStorableFloat("softMultiplier", -0.30f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.10f, -3.00f, 3.00f)
            ),
            new MorphConfig("RT/RT Breast Rotate X Out R",
                true,
                new JSONStorableFloat("softMultiplier", -0.60f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.30f, -3.00f, 3.00f)
            ),
            new MorphConfig("RT/RT Breast Rotate X In R",
                true,
                new JSONStorableFloat("softMultiplier", -0.30f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.10f, -3.00f, 3.00f)
            ),
            new MorphConfig("RT/RT Breast Move S2S Out R",
                false,
                new JSONStorableFloat("softMultiplier", 0.30f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.10f, -3.00f, 3.00f)
            ),
            new MorphConfig("RT/RT Breast Diameter R",
                true,
                new JSONStorableFloat("softMultiplier", -0.20f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.20f, -3.00f, 3.00f)
            ),
            new MorphConfig("RT/RT Breast Under Smoother2 R",
                false,
                new JSONStorableFloat("softMultiplier", 0.10f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.40f, -3.00f, 3.00f)
            ),
            new MorphConfig("RT/RT Breast Under Smoother3 R",
                false,
                new JSONStorableFloat("softMultiplier", 0.40f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.40f, -3.00f, 3.00f)
            ),
        };

        #endregion Morph configs

        public static void Update(float roll, float pitch)
        {
            float diffFromHorizontal = GravityEffectCalc.DiffFromHorizontal(pitch, roll);
            AdjustUpMorphs(diffFromHorizontal);
            AdjustForwardMorphs();
            AdjustBackMorphs(roll, pitch);
            AdjustLeftMorphs(Mathf.Abs(roll));
            AdjustRightMorphs(Mathf.Abs(roll));
        }

        private static void AdjustUpMorphs(float diffFromHorizontal)
        {
            float pitchMultiplier = Mathf.Lerp(0.80f, 1f, diffFromHorizontal);
            Func<float, float> calculateEffect = angle =>
                upDownExtraMultiplier
                * Curves.QuadraticRegression(upMultiplier)
                * Curves.YForceEffectCurve(pitchMultiplier * Mathf.Abs(angle) / 75);

            if(_trackLeftBreast.angleY >= 0)
            {
                // up force on left breast
                UpdateMorphs(Direction.UP_L, calculateEffect(_trackLeftBreast.angleY));
            }
            else
            {
                // down force on left breast
                ResetMorphs(Direction.UP_L);
            }

            if(_trackRightBreast.angleY >= 0)
            {
                // up force on right breast
                UpdateMorphs(Direction.UP_R, calculateEffect(_trackRightBreast.angleY));
            }
            else
            {
                // down force on right breast
                ResetMorphs(Direction.UP_R);
            }

            float angleYCenter = (_trackRightBreast.angleY + _trackLeftBreast.angleY) / 2;
            if(angleYCenter >= 0)
            {
                // up force on average of left and right breast
                UpdateMorphs(Direction.UP_C, calculateEffect(angleYCenter));
            }
            else
            {
                ResetMorphs(Direction.UP_C);
            }
        }

        private static void AdjustForwardMorphs()
        {
            Func<float, float> calculateEffect = distance =>
                forwardExtraMultiplier
                * Curves.QuadraticRegression(forwardMultiplier)
                * Curves.ZForceEffectCurve(Mathf.Abs(distance) * 13.5f);

            if(_trackLeftBreast.depthDiff <= 0)
            {
                // forward force on left breast
                UpdateMorphs(Direction.FORWARD_L, calculateEffect(_trackLeftBreast.depthDiff));
            }
            else
            {
                // back force on left breast
                ResetMorphs(Direction.FORWARD_L);
            }

            if(_trackRightBreast.depthDiff <= 0)
            {
                // forward force on right breast
                UpdateMorphs(Direction.FORWARD_R, calculateEffect(_trackRightBreast.depthDiff));
            }
            else
            {
                // back force on right breast
                ResetMorphs(Direction.FORWARD_R);
            }

            float depthDiffCenter = (_trackLeftBreast.depthDiff + _trackRightBreast.depthDiff) / 2;
            if(depthDiffCenter <= 0)
            {
                // forward force on average of left and right breast
                UpdateMorphs(Direction.FORWARD_C, calculateEffect(depthDiffCenter));
            }
            else
            {
                // back force on average of left and right breast
                ResetMorphs(Direction.FORWARD_C);
            }
        }

        private static void AdjustBackMorphs(float roll, float pitch)
        {
            Func<float, float> calculateEffect = distance =>
                backExtraMultiplier
                * CalculateLeanBackFixerMultiplier(pitch, roll)
                * Curves.QuadraticRegression(backMultiplier)
                * Curves.ZForceEffectCurve(Mathf.Abs(distance) * 13.5f);

            if(_trackLeftBreast.depthDiff <= 0)
            {
                // forward force on left breast
                ResetMorphs(Direction.BACK_L);
            }
            else
            {
                // back force on left breast
                UpdateMorphs(Direction.BACK_L, calculateEffect(_trackLeftBreast.depthDiff));
            }

            if(_trackRightBreast.depthDiff <= 0)
            {
                // forward force on right breast
                ResetMorphs(Direction.BACK_R);
            }
            else
            {
                // back force on right breast
                UpdateMorphs(Direction.BACK_R, calculateEffect(_trackRightBreast.depthDiff));
            }

            float depthDiffCenter = (_trackLeftBreast.depthDiff + _trackRightBreast.depthDiff) / 2;
            if(depthDiffCenter <= 0)
            {
                // forward force on average of left and right breast
                ResetMorphs(Direction.BACK_C);
            }
            else
            {
                // back force on average of left and right breast
                UpdateMorphs(Direction.BACK_C, calculateEffect(depthDiffCenter));
            }
        }

        private static void AdjustLeftMorphs(float roll)
        {
            float rollAngleMulti = Mathf.Lerp(1.25f, 1f, roll);

            Func<float, float> calculateEffect = angle =>
                leftRightExtraMultiplier
                * Curves.QuadraticRegression(leftRightMultiplier)
                * Curves.XForceEffectCurve(rollAngleMulti * Mathf.Abs(angle) / 60);

            if(_trackLeftBreast.angleX >= 0)
            {
                // left force on left breast
                UpdateMorphs(Direction.RIGHT_L, calculateEffect(_trackLeftBreast.angleX));
            }
            else
            {
                // right force on left breast
                ResetMorphs(Direction.RIGHT_L);
            }

            if(_trackRightBreast.angleX >= 0)
            {
                // left force on right breast
                UpdateMorphs(Direction.RIGHT_R, calculateEffect(_trackRightBreast.angleX));
            }
            else
            {
                // right force on right breast
                ResetMorphs(Direction.RIGHT_R);
            }
        }

        private static void AdjustRightMorphs(float roll)
        {
            float rollAngleMulti = Mathf.Lerp(1.25f, 1f, roll);

            Func<float, float> calculateEffect = angle =>
                leftRightExtraMultiplier
                * Curves.QuadraticRegression(leftRightMultiplier)
                * Curves.XForceEffectCurve(rollAngleMulti * Mathf.Abs(angle) / 60);

            if(_trackLeftBreast.angleX >= 0)
            {
                // left force on left breast
                ResetMorphs(Direction.LEFT_L);
            }
            else
            {
                // right force on left breast
                UpdateMorphs(Direction.LEFT_L, calculateEffect(_trackLeftBreast.angleX));
            }

            if(_trackRightBreast.angleX >= 0)
            {
                // left force on right breast
                ResetMorphs(Direction.LEFT_R);
            }
            else
            {
                // right force on right breast
                UpdateMorphs(Direction.LEFT_R, calculateEffect(_trackRightBreast.angleX));
            }
        }

        private static float CalculateLeanBackFixerMultiplier(float pitch, float roll)
        {
            if(pitch < -0.5f || pitch > 0)
            {
                return 1;
            }

            float diff = 4 * Mathf.Abs(-0.25f - pitch);
            float minTarget1 = Mathf.Lerp(0.25f, 1.00f, MainPhysicsHandler.normalizedInvertedMass);
            float minTarget2 = Mathf.Lerp(minTarget1, 1.00f, Mathf.Abs(roll * roll));
            return Mathf.Lerp(minTarget2, 1.00f, diff);
        }

        private static void UpdateMorphs(string configSetName, float effect)
        {
            float mass = MainPhysicsHandler.realMassAmount;
            const float softness = 0.62f;
            configSets[configSetName].ForEach(config => UpdateValue(config, effect, mass, softness));
        }

        private static void UpdateValue(MorphConfig config, float effect, float mass, float softness)
        {
            float value =
                softness * config.softMultiplier * effect / 2 +
                mass * config.massMultiplier * effect / 2;

            bool inRange = config.isNegative ? value < 0 : value > 0;
            config.morph.morphValue = inRange ? Calc.RoundToDecimals(value, 1000f) : 0;
        }

        public static void ResetAll() => configSets?.Keys.ToList().ForEach(ResetMorphs);

        private static void ResetMorphs(string configSetName) =>
            configSets[configSetName].ForEach(config => config.morph.morphValue = 0);

        public static void Destroy()
        {
            _trackLeftBreast = null;
            _trackRightBreast = null;
            configSets = null;
            baseJsf = null;
            upJsf = null;
            forwardJsf = null;
            backJsf = null;
            leftRightJsf = null;
        }
    }
}
