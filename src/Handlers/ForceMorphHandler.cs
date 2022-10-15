using System;
using System.Collections.Generic;
using TittyMagic.Components;
using TittyMagic.Handlers.Configs;
using UnityEngine;
using static TittyMagic.Script;
using static TittyMagic.Direction;

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
                { UP_L, UpForceConfigs("L") },
                { UP_R, UpForceConfigs("R") },
                { UP_C, UpForceCenterConfigs() },
                { BACK_L, BackForceConfigs("L") },
                { BACK_R, BackForceConfigs("R") },
                { BACK_C, BackForceCenterConfigs() },
                { FORWARD_L, ForwardForceConfigs("L") },
                { FORWARD_R, ForwardForceConfigs("R") },
                { FORWARD_C, ForwardForceCenterConfigs() },
                { LEFT_L, LeftForceLeftConfigs() },
                { LEFT_R, LeftForceRightConfigs() },
                { RIGHT_L, RightForceLeftConfigs() },
                { RIGHT_R, RightForceRightConfigs() },
            };

            var directions = new List<string>
            {
                UP,
                UP_C,
                BACK,
                BACK_C,
                FORWARD,
                FORWARD_C,
                LEFT_L,
                LEFT_R,
                RIGHT_L,
                RIGHT_R,
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

        private static float _upSoftnessMultiplier;
        private static float _upMassMultiplier;

        private static float _forwardSoftnessMultiplier;
        private static float _forwardMassMultiplier;

        private static float _backSoftnessMultiplier;
        private static float _backMassMultiplier;

        private static float _leftRightSoftnessMultiplier;
        private static float _leftRightMassMultiplier;

        public static void SetMultipliers(float mass, float softness)
        {
            _upSoftnessMultiplier = Curves.Exponential1(softness, 1.73f, 1.68f, 0.88f, s: 0.56f);
            _upMassMultiplier = 0.493f * Curves.MassMorphingCurve(mass);

            _forwardSoftnessMultiplier = Mathf.Lerp(1.00f, 1.20f, softness);
            _forwardMassMultiplier = 0.90f * Curves.DepthMassMorphingCurve(mass);

            _backSoftnessMultiplier = Mathf.Lerp(0.80f, 1.00f, softness);
            _backMassMultiplier = 0.90f * Curves.DepthMassMorphingCurve(mass);

            _leftRightSoftnessMultiplier = Curves.Exponential1(softness, 1.73f, 1.68f, 0.88f, s: 0.56f);
            _leftRightMassMultiplier = 0.605f * Curves.MassMorphingCurve(mass);
        }

        private static float _rollL;
        private static float _rollR;
        private static float _pitchL;
        private static float _pitchR;
        private static float _rollAngleMultiL;
        private static float _rollAngleMultiR;

        public static void Update()
        {
            var lPectoralRotation = tittyMagic.pectoralRbLeft.rotation;
            var rPectoralRotation = tittyMagic.pectoralRbRight.rotation;
            _rollL = Calc.Roll(lPectoralRotation);
            _rollR = -Calc.Roll(rPectoralRotation);
            _pitchL = 2 * Calc.Pitch(lPectoralRotation);
            _pitchR = 2 * Calc.Pitch(rPectoralRotation);
            _rollAngleMultiL = Mathf.Lerp(1.25f, 1f, Mathf.Abs(_rollL));
            _rollAngleMultiR = Mathf.Lerp(1.25f, 1f, Mathf.Abs(_rollR));

            AdjustUpMorphs();
            AdjustForwardMorphs();
            AdjustBackMorphs();
            AdjustLeftMorphs();
            AdjustRightMorphs();
        }

        private static void AdjustUpMorphs()
        {
            float pitchMultiplierLeft = 0;
            float pitchMultiplierRight = 0;

            Func<float, float, float> calculateEffect = (angle, pitchMultiplier) =>
                _upSoftnessMultiplier
                * _upMassMultiplier
                * Curves.QuadraticRegression(upMultiplier)
                * Curves.YForceEffectCurve(pitchMultiplier * Mathf.Abs(angle) / 75);

            if(_trackLeftBreast.angleY >= 0)
            {
                // up force on left breast
                pitchMultiplierLeft = Mathf.Lerp(0.80f, 1f, GravityEffectCalc.DiffFromHorizontal(_pitchL, _rollL));
                UpdateMorphs(UP_L, calculateEffect(_trackLeftBreast.angleY, pitchMultiplierLeft));
            }
            else
            {
                // down force on left breast
                ResetMorphs(UP_L);
            }

            if(_trackRightBreast.angleY >= 0)
            {
                // up force on right breast
                pitchMultiplierRight = Mathf.Lerp(0.80f, 1f, GravityEffectCalc.DiffFromHorizontal(_pitchR, _rollR));
                UpdateMorphs(UP_R, calculateEffect(_trackRightBreast.angleY, pitchMultiplierRight));
            }
            else
            {
                // down force on right breast
                ResetMorphs(UP_R);
            }

            float angleYCenter = (_trackRightBreast.angleY + _trackLeftBreast.angleY) / 2;
            if(angleYCenter >= 0)
            {
                // up force on average of left and right breast
                float pitchMultiplierCenter = (pitchMultiplierLeft + pitchMultiplierRight) / 2;
                UpdateMorphs(UP_C, calculateEffect(angleYCenter, pitchMultiplierCenter));
            }
            else
            {
                ResetMorphs(UP_C);
            }
        }

        private static void AdjustForwardMorphs()
        {
            Func<float, float> calculateEffect = distance =>
                _forwardSoftnessMultiplier
                * _forwardMassMultiplier
                * Curves.QuadraticRegression(forwardMultiplier)
                * Curves.ZForceEffectCurve(Mathf.Abs(distance) * 13.5f);

            if(_trackLeftBreast.depthDiff < 0)
            {
                // forward force on left breast
                UpdateMorphs(FORWARD_L, calculateEffect(_trackLeftBreast.depthDiff));
            }
            else
            {
                // back force on left breast
                ResetMorphs(FORWARD_L);
            }

            if(_trackRightBreast.depthDiff < 0)
            {
                // forward force on right breast
                // gRight = 2.25f * GravityEffectCalc.DepthEffect(pitchRight, rollRight, forwardMultiplier);
                UpdateMorphs(FORWARD_R, calculateEffect(_trackRightBreast.depthDiff));
            }
            else
            {
                // back force on right breast
                ResetMorphs(FORWARD_R);
            }

            float depthDiffCenter = (_trackLeftBreast.depthDiff + _trackRightBreast.depthDiff) / 2;
            if(depthDiffCenter < 0)
            {
                // forward force on average of left and right breast
                UpdateMorphs(FORWARD_C, calculateEffect(depthDiffCenter));
            }
            else
            {
                // back force on average of left and right breast
                ResetMorphs(FORWARD_C);
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

        private static void AdjustBackMorphs()
        {
            float leanBackFixedMultiLeft = CalculateLeanBackFixerMultiplier(_pitchL, _rollL);
            float leanBackFixedMultiRight = CalculateLeanBackFixerMultiplier(_pitchR, _rollR);

            Func<float, float, float> calculateEffect = (distance, leanBackFixedMulti) =>
                _backSoftnessMultiplier
                * _backMassMultiplier
                * leanBackFixedMulti
                * Curves.QuadraticRegression(backMultiplier)
                * Curves.ZForceEffectCurve(Mathf.Abs(distance) * 13.5f);

            if(_trackLeftBreast.depthDiff < 0)
            {
                // forward force on left breast
                ResetMorphs(BACK_L);
            }
            else
            {
                // back force on left breast
                UpdateMorphs(BACK_L, calculateEffect(_trackLeftBreast.depthDiff, leanBackFixedMultiLeft));
            }

            if(_trackRightBreast.depthDiff < 0)
            {
                // forward force on right breast
                ResetMorphs(BACK_R);
            }
            else
            {
                // back force on right breast
                UpdateMorphs(BACK_R, calculateEffect(_trackRightBreast.depthDiff, leanBackFixedMultiRight));
            }

            float depthDiffCenter = (_trackLeftBreast.depthDiff + _trackRightBreast.depthDiff) / 2;
            float leanBackFixedMultiCenter = (leanBackFixedMultiLeft + leanBackFixedMultiRight) / 2;
            if(depthDiffCenter < 0)
            {
                // forward force on average of left and right breast
                ResetMorphs(BACK_C);
            }
            else
            {
                // back force on average of left and right breast
                UpdateMorphs(BACK_C, calculateEffect(depthDiffCenter, leanBackFixedMultiCenter));
            }
        }

        private static void AdjustLeftMorphs()
        {
            Func<float, float, float> calculateEffect = (angle, rollAngleMulti) =>
                _leftRightSoftnessMultiplier
                * _leftRightMassMultiplier
                * Curves.QuadraticRegression(leftRightMultiplier)
                * Curves.XForceEffectCurve(rollAngleMulti * Mathf.Abs(angle) / 60);

            if(_trackLeftBreast.angleX >= 0)
            {
                // left force on left breast
                UpdateMorphs(RIGHT_L, calculateEffect(_trackLeftBreast.angleX, _rollAngleMultiL));
            }
            else
            {
                // right force on left breast
                ResetMorphs(RIGHT_L);
            }

            if(_trackRightBreast.angleX >= 0)
            {
                // left force on right breast
                UpdateMorphs(RIGHT_R, calculateEffect(_trackRightBreast.angleX, _rollAngleMultiR));
            }
            else
            {
                // right force on right breast
                ResetMorphs(RIGHT_R);
            }
        }

        private static void AdjustRightMorphs()
        {
            Func<float, float, float> calculateEffect = (angle, rollAngleMulti) =>
                _leftRightSoftnessMultiplier
                * _leftRightMassMultiplier
                * Curves.QuadraticRegression(leftRightMultiplier)
                * Curves.XForceEffectCurve(rollAngleMulti * Mathf.Abs(angle) / 60);

            if(_trackLeftBreast.angleX >= 0)
            {
                // left force on left breast
                ResetMorphs(LEFT_L);
            }
            else
            {
                // right force on left breast
                UpdateMorphs(LEFT_L, calculateEffect(_trackLeftBreast.angleX, _rollAngleMultiL));
            }

            if(_trackRightBreast.angleX >= 0)
            {
                // left force on right breast
                ResetMorphs(LEFT_R);
            }
            else
            {
                // right force on right breast
                UpdateMorphs(LEFT_R, calculateEffect(_trackRightBreast.angleX, _rollAngleMultiR));
            }
        }

        private static void UpdateMorphs(string direction, float effect)
        {
            float mass = MainPhysicsHandler.realMassAmount;
            const float softness = 0.62f;
            configSets[direction].ForEach(config => UpdateValue(config, effect, mass, softness));
        }

        private static void UpdateValue(MorphConfig config, float effect, float mass, float softness)
        {
            float value =
                softness * config.softMultiplier * effect / 2 +
                mass * config.massMultiplier * effect / 2;

            bool inRange = config.isNegative ? value < 0 : value > 0;
            config.morph.morphValue = inRange ? Calc.RoundToDecimals(value, 1000f) : 0;
        }

        public static void SimulateUpright()
        {
            _rollL = 0;
            _rollR = 0;
            _pitchL = 0;
            _pitchR = 0;
            _rollAngleMultiL = 0;
            _rollAngleMultiR = 0;
            AdjustUpMorphs();
            AdjustForwardMorphs();
            AdjustBackMorphs();
            AdjustLeftMorphs();
            AdjustRightMorphs();
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
