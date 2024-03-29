﻿using System.Collections.Generic;
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
        public static JSONStorableFloat sidewaysInJsf { get; private set; }
        public static JSONStorableFloat sidewaysOutJsf { get; private set; }

        private static float upMultiplier => baseJsf.val * upJsf.val;
        private static float forwardMultiplier => baseJsf.val * forwardJsf.val;
        private static float backMultiplier => baseJsf.val * backJsf.val;
        private static float sidewaysInMultiplier => baseJsf.val * sidewaysInJsf.val;
        private static float sidewaysOutMultiplier => baseJsf.val * sidewaysOutJsf.val;

        public static JSONStorableStringChooser directionChooser { get; private set; }

        public static void Init()
        {
            _trackLeftBreast = tittyMagic.trackLeftBreast;
            _trackRightBreast = tittyMagic.trackRightBreast;

            baseJsf = tittyMagic.NewJSONStorableFloat("forceMorphingBase", 1.00f, 0.00f, 2.00f);
            upJsf = tittyMagic.NewJSONStorableFloat("forceMorphingUp", 1.00f, 0.00f, 2.00f);
            forwardJsf = tittyMagic.NewJSONStorableFloat("forceMorphingForward", 1.00f, 0.00f, 2.00f);
            backJsf = tittyMagic.NewJSONStorableFloat("forceMorphingBack", 1.00f, 0.00f, 2.00f);
            sidewaysInJsf = tittyMagic.NewJSONStorableFloat("forceMorphingSidewaysIn", 1.00f, 0.00f, 2.00f);
            sidewaysOutJsf = tittyMagic.NewJSONStorableFloat("forceMorphingSidewaysOut", 1.00f, 0.00f, 2.00f);
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
                new JSONStorableFloat("massMultiplier", 0.50f, -3.00f, 3.00f)
            ),
            new MorphConfig($"UP/UP Breast Top Curve2 {side}",
                true,
                new JSONStorableFloat("softMultiplier", -1.20f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.60f, -3.00f, 3.00f)
            ),
            new MorphConfig($"UP/UP Breasts Implants {side}",
                false,
                new JSONStorableFloat("softMultiplier", 0.36f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.12f, -3.00f, 3.00f)
            ),
            new MorphConfig($"UP/UP Breast Diameter {side}",
                false,
                new JSONStorableFloat("softMultiplier", 2.00f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.50f, -3.00f, 3.00f)
            ),
            new MorphConfig($"UP/UP Breast Diameter(Pose) {side}",
                false,
                new JSONStorableFloat("softMultiplier", 1.50f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.50f, -3.00f, 3.00f)
            ),
            new MorphConfig($"UP/UP Breast Width {side}",
                false,
                new JSONStorableFloat("softMultiplier", 0.21f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.09f, -3.00f, 3.00f)
            ),
            new MorphConfig($"UP/UP Breast Zero {side}",
                false,
                new JSONStorableFloat("softMultiplier", 0.24f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.08f, -3.00f, 3.00f)
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
                new JSONStorableFloat("massMultiplier", 0.60f, -3.00f, 3.00f)
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
                new JSONStorableFloat("softMultiplier", 0.25f, -1.50f, 1.50f),
                new JSONStorableFloat("massMultiplier", 0.00f, -1.50f, 1.50f)
            ),
            new MorphConfig($"UP/UP Breast Under Smoother2 {side}",
                false,
                new JSONStorableFloat("softMultiplier", 0.40f, -1.50f, 1.50f),
                new JSONStorableFloat("massMultiplier", 0.20f, -1.50f, 1.50f)
            ),
            new MorphConfig($"UP/UP Breast Under Smoother3 {side}",
                false,
                new JSONStorableFloat("softMultiplier", 0.15f, -1.50f, 1.50f),
                new JSONStorableFloat("massMultiplier", 0.15f, -1.50f, 1.50f)
            ),
            new MorphConfig($"UP/UP Breast Under Smoother4 {side}",
                false,
                new JSONStorableFloat("softMultiplier", 0.30f, -1.50f, 1.50f),
                new JSONStorableFloat("massMultiplier", 0.10f, -1.50f, 1.50f)
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
                new JSONStorableFloat("softMultiplier", 1.10f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.37f, -3.00f, 3.00f)
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
                new JSONStorableFloat("softMultiplier", 0.70f, -1.50f, 1.50f),
                new JSONStorableFloat("massMultiplier", 0.35f, -1.50f, 1.50f)
            ),
            new MorphConfig($"BK/BK Breast Under Smoother2 {side}",
                false,
                new JSONStorableFloat("softMultiplier", 0.07f, -1.50f, 1.50f),
                new JSONStorableFloat("massMultiplier", 0.21f, -1.50f, 1.50f)
            ),
            new MorphConfig($"BK/BK Breast Under Smoother3 {side}",
                false,
                new JSONStorableFloat("softMultiplier", 0.60f, -1.50f, 1.50f),
                new JSONStorableFloat("massMultiplier", 0.20f, -1.50f, 1.50f)
            ),
            new MorphConfig($"BK/BK Breast Zero {side}",
                false,
                new JSONStorableFloat("softMultiplier", 0.24f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.08f, -3.00f, 3.00f)
            ),
            new MorphConfig($"BK/BK Breasts Flatten {side}",
                false,
                new JSONStorableFloat("softMultiplier", 1.60f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.53f, -3.00f, 3.00f)
            ),
            new MorphConfig($"BK/BK Breasts Hang Forward {side}",
                true,
                new JSONStorableFloat("softMultiplier", -0.40f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.10f, -3.00f, 3.00f)
            ),
            new MorphConfig($"BK/BK Breasts Implants {side}",
                true,
                new JSONStorableFloat("softMultiplier", -0.09f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.03f, -3.00f, 3.00f)
            ),
            new MorphConfig($"BK/BK Breasts TogetherApart {side}",
                false,
                new JSONStorableFloat("softMultiplier", 0.15f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.15f, -3.00f, 3.00f)
            ),
            new MorphConfig($"BK/BK Depth Squash {side}",
                false,
                new JSONStorableFloat("softMultiplier", 1.50f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.50f, -3.00f, 3.00f)
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
                new JSONStorableFloat("massMultiplier", -0.10f, -3.00f, 3.00f)
            ),
            new MorphConfig($"FW/FW Breast Rotate Down {side}",
                true,
                new JSONStorableFloat("softMultiplier", -0.40f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.10f, -3.00f, 3.00f)
            ),
            new MorphConfig($"FW/FW Breast Width {side}",
                true,
                new JSONStorableFloat("softMultiplier", -0.24f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.08f, -3.00f, 3.00f)
            ),
            new MorphConfig($"FW/FW Breast Depth {side}",
                false,
                new JSONStorableFloat("softMultiplier", 1.60f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.08f, -3.00f, 3.00f)
            ),
            new MorphConfig($"FW/FW Breast Small {side}",
                false,
                new JSONStorableFloat("softMultiplier", 0.64f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.18f, -3.00f, 3.00f)
            ),
            new MorphConfig($"FW/FW Breast Round {side}",
                true,
                new JSONStorableFloat("softMultiplier", -1.80f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.60f, -3.00f, 3.00f)
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
                new JSONStorableFloat("massMultiplier", 0.50f, -3.00f, 3.00f)
            ),
            new MorphConfig("LT/LT Breasts Hang Forward L",
                true,
                new JSONStorableFloat("softMultiplier", -1.00f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.80f, -3.00f, 3.00f)
            ),
            new MorphConfig("LT/LT Breast Width L",
                true,
                new JSONStorableFloat("softMultiplier", -0.60f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.20f, -3.00f, 3.00f)
            ),
            new MorphConfig("LT/LT Breast Rotate X Out L",
                true,
                new JSONStorableFloat("softMultiplier", -0.60f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.20f, -3.00f, 3.00f)
            ),
            new MorphConfig("LT/LT Breast Rotate X In L",
                true,
                new JSONStorableFloat("softMultiplier", -0.30f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.10f, -3.00f, 3.00f)
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
                new JSONStorableFloat("softMultiplier", 1.50f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.30f, -3.00f, 3.00f)
            ),
            new MorphConfig("LT/LT Breast Rotate X Out R",
                true,
                new JSONStorableFloat("softMultiplier", -0.84f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.28f, -3.00f, 3.00f)
            ),
            new MorphConfig("LT/LT Breasts Flatten R",
                true,
                new JSONStorableFloat("softMultiplier", -0.40f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.20f, -3.00f, 3.00f)
            ),
            new MorphConfig("LT/LT Breast Width R",
                false,
                new JSONStorableFloat("softMultiplier", 0.90f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.50f, -3.00f, 3.00f)
            ),
            new MorphConfig("LT/LT Breast Rotate X In R",
                false,
                new JSONStorableFloat("softMultiplier", 0.20f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.00f, -3.00f, 3.00f)
            ),
            new MorphConfig("LT/LT Breasts Hang Forward R",
                false,
                new JSONStorableFloat("softMultiplier", 0.20f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.07f, -3.00f, 3.00f)
            ),
            new MorphConfig("LT/LT Areola S2S R",
                false,
                new JSONStorableFloat("softMultiplier", 0.60f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 1.80f, -3.00f, 3.00f)
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
                new JSONStorableFloat("softMultiplier", 1.50f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.30f, -3.00f, 3.00f)
            ),
            new MorphConfig("RT/RT Breast Rotate X Out L",
                true,
                new JSONStorableFloat("softMultiplier", -0.84f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.28f, -3.00f, 3.00f)
            ),
            new MorphConfig("RT/RT Breasts Flatten L",
                true,
                new JSONStorableFloat("softMultiplier", -0.40f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.20f, -3.00f, 3.00f)
            ),
            new MorphConfig("RT/RT Breast Width L",
                false,
                new JSONStorableFloat("softMultiplier", 0.90f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.50f, -3.00f, 3.00f)
            ),
            new MorphConfig("RT/RT Breast Rotate X In L",
                false,
                new JSONStorableFloat("softMultiplier", 0.20f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.00f, -3.00f, 3.00f)
            ),
            new MorphConfig("RT/RT Breasts Hang Forward L",
                false,
                new JSONStorableFloat("softMultiplier", 0.20f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.07f, -3.00f, 3.00f)
            ),
            new MorphConfig("RT/RT Areola S2S L",
                false,
                new JSONStorableFloat("softMultiplier", 0.60f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 1.80f, -3.00f, 3.00f)
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
                new JSONStorableFloat("massMultiplier", 0.50f, -3.00f, 3.00f)
            ),
            new MorphConfig("RT/RT Breasts Hang Forward R",
                true,
                new JSONStorableFloat("softMultiplier", -1.00f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.80f, -3.00f, 3.00f)
            ),
            new MorphConfig("RT/RT Breast Width R",
                true,
                new JSONStorableFloat("softMultiplier", -0.40f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.13f, -3.00f, 3.00f)
            ),
            new MorphConfig("RT/RT Breast Rotate X Out R",
                true,
                new JSONStorableFloat("softMultiplier", -0.60f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", 0.20f, -3.00f, 3.00f)
            ),
            new MorphConfig("RT/RT Breast Rotate X In R",
                true,
                new JSONStorableFloat("softMultiplier", -0.30f, -3.00f, 3.00f),
                new JSONStorableFloat("massMultiplier", -0.10f, -3.00f, 3.00f)
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

        private const float MIN_MASS = 0.10f;
        private const float MAX_MASS = 1.50f;

        private static float _upBaseMassFactor;
        private static float _forwardBaseMassFactor;
        private static float _backBaseMassFactor;
        private static float _leftRightBaseMassFactor;

        private static float UpBaseMassFactor(float mass)
        {
            const float cutoff1 = 0.27f;
            const float cutoff2 = 0.70f;

            if(mass < cutoff1)
            {
                float ratioToCutoff1 = (mass - MIN_MASS) / (cutoff1 - MIN_MASS);
                return Mathf.Lerp(37.0f, 50.0f, ratioToCutoff1);
            }

            if(mass < cutoff2)
            {
                float ratioFromCutoff1To2 = (mass - cutoff1) / (cutoff2 - cutoff1);
                return Mathf.Lerp(50.0f, 55.4f, Curves.Exponential1(ratioFromCutoff1To2, 1.76f, 1.43f, 0.67f));
            }

            float ratioFromCutoff2 = (mass - cutoff2) / (MAX_MASS - cutoff2);
            return Mathf.Lerp(47.10f, 55.40f, 1 - ratioFromCutoff2);
        }

        private static float ForwardBaseMassFactor(float mass)
        {
            const float cutoff1 = 0.27f;
            const float cutoff2 = 0.45f;

            if(mass < cutoff1)
            {
                float ratioToCutoff1 = (mass - MIN_MASS) / (cutoff1 - MIN_MASS);
                return Mathf.Lerp(21.6f, 31.0f, 1 - ratioToCutoff1);
            }

            if(mass > cutoff2)
            {
                float ratioFromCutoff2 = (mass - cutoff2) / (MAX_MASS - cutoff2);
                return Mathf.Lerp(21.6f, 29.0f, Mathf.Pow(ratioFromCutoff2, 1.75f));
            }

            return 21.6f;
        }

        // ReSharper disable once UnusedParameter.Local
        private static float BackBaseMassFactor(float _) =>
            0.8f * _forwardBaseMassFactor;

        // https://www.desmos.com/calculator/fusmae0li0
        private static float LeftRightBaseMassFactor(float mass)
        {
            const float cutoff1 = 0.45f;
            if(mass < cutoff1)
            {
                float ratioToCutoff1 = (mass - MIN_MASS) / (cutoff1 - MIN_MASS);
                // https://www.desmos.com/calculator/sz5l5hsiw3
                return Mathf.Lerp(33.4f, 40.0f, Curves.InverseSmoothStep(ratioToCutoff1, 1, -0.16f, 0));
            }

            float ratioFromCutoff1 = (mass - cutoff1) / (MAX_MASS - cutoff1);
            // https://www.desmos.com/calculator/fbihhkssnl
            return Mathf.Lerp(27.8f, 40.0f, 1 - Curves.InverseSmoothStep(ratioFromCutoff1, 1, -0.33f, 0));
        }

        private static float _upSoftnessMultiplier;
        private static float _upMassMultiplier;

        private static float _forwardSoftnessMultiplier;
        private static float _forwardMassMultiplier;

        private static float _backSoftnessMultiplier;
        private static float _backMassMultiplier;

        private static float _sidewaysSoftnessMultiplier;
        private static float _sidewaysMassMultiplier;

        // https://www.desmos.com/calculator/kkbbepyp4j
        private static float SoftnessCurve(float softness) => 1.04f * Curves.Exponential1(softness, 1.76f, 1.67f, 0.74f, a: 0.9f, s: 0.13f);

        // https://www.desmos.com/calculator/6e11w5zfn7
        private static float BackSoftnessCurve(float softness) => 1.04f * Curves.Exponential1(softness, 2.58f, 1.38f, 0.67f, a: 0.7f, s: 0.01f);

        public static void SetMultipliers(float mass, float softness)
        {
            _upBaseMassFactor = UpBaseMassFactor(mass);
            _forwardBaseMassFactor = ForwardBaseMassFactor(mass);
            _backBaseMassFactor = BackBaseMassFactor(mass);
            _leftRightBaseMassFactor = LeftRightBaseMassFactor(mass);

            _upSoftnessMultiplier = SoftnessCurve(softness);
            _upMassMultiplier = 0.0069f * _upBaseMassFactor * Curves.MassMorphingCurve(mass);

            _forwardSoftnessMultiplier = 1.16f * SoftnessCurve(softness);
            _forwardMassMultiplier = 12.50f / _forwardBaseMassFactor * Curves.ForwardMassMorphingCurve(mass);

            _backSoftnessMultiplier = BackSoftnessCurve(softness);
            _backMassMultiplier = 12.50f / _backBaseMassFactor * Curves.BackMassMorphingCurve(mass);

            _sidewaysSoftnessMultiplier = SoftnessCurve(softness);
            _sidewaysMassMultiplier = 0.0109f * _leftRightBaseMassFactor * Curves.MassMorphingCurve(mass);
        }

        private static float _rollL;
        private static float _rollR;
        private static float _pitchL;
        private static float _pitchR;
        private static float _rollAngleMultiL;
        private static float _rollAngleMultiR;

        public static void Update()
        {
            _rollL = GravityEffectCalc.pectoralRollL;
            _rollR = -GravityEffectCalc.pectoralRollR;
            _pitchL = 2 * GravityEffectCalc.pectoralPitchL;
            _pitchR = 2 * GravityEffectCalc.pectoralPitchR;
            _rollAngleMultiL = Mathf.Lerp(1.25f, 1f, Mathf.Abs(_rollL));
            _rollAngleMultiR = Mathf.Lerp(1.25f, 1f, Mathf.Abs(_rollR));

            AdjustUpMorphs();
            AdjustForwardMorphs();
            AdjustBackMorphs();
            AdjustSidewaysMorphs();
        }

        private static float UpEffect(float angle, float pitchMultiplier) =>
            pitchMultiplier
            * _upSoftnessMultiplier
            * _upMassMultiplier
            * Curves.QuadraticRegression(upMultiplier)
            * Curves.YForceEffectCurve(Mathf.Abs(angle) / _upBaseMassFactor);

        private static void AdjustUpMorphs()
        {
            float pitchMultiplierLeft = 0;
            float pitchMultiplierRight = 0;

            if(_trackLeftBreast.angleY >= 0)
            {
                // up force on left breast
                pitchMultiplierLeft = Mathf.Lerp(0.67f, 1f, GravityEffectCalc.DiffFromHorizontal(_pitchL, _rollL));
                UpdateMorphs(UP_L, UpEffect(_trackLeftBreast.angleY, pitchMultiplierLeft));
            }
            else
            {
                // down force on left breast
                ResetMorphs(UP_L);
            }

            if(_trackRightBreast.angleY >= 0)
            {
                // up force on right breast
                pitchMultiplierRight = Mathf.Lerp(0.67f, 1f, GravityEffectCalc.DiffFromHorizontal(_pitchR, _rollR));
                UpdateMorphs(UP_R, UpEffect(_trackRightBreast.angleY, pitchMultiplierRight));
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
                UpdateMorphs(UP_C, UpEffect(angleYCenter, pitchMultiplierCenter));
            }
            else
            {
                ResetMorphs(UP_C);
            }
        }

        private static float CalculateUpsideDownFixerMultiplier(float pitch, float roll)
        {
            if(pitch > -1 && pitch < 1)
            {
                return 1;
            }

            // return 1;
            float effect = GravityEffectCalc.UpDownAdjustByAngle(pitch) * GravityEffectCalc.RollMultiplier(roll) / 2;
            return 1 - effect;
        }

        private static float ForwardEffect(float depthDiff, float upsideDownFixerMulti) =>
            _forwardSoftnessMultiplier
            * _forwardMassMultiplier
            * upsideDownFixerMulti
            * Curves.QuadraticRegression(forwardMultiplier)
            * 1.1f * Curves.ZForceEffectCurve(Mathf.Abs(depthDiff) * _forwardBaseMassFactor);

        private static void AdjustForwardMorphs()
        {
            float upsideDownFixerMultiLeft = CalculateUpsideDownFixerMultiplier(_pitchL, _rollL);
            float upsideDownFixerMultiRight = CalculateUpsideDownFixerMultiplier(_pitchR, _rollR);

            if(_trackLeftBreast.depthDiff < 0)
            {
                // forward force on left breast
                UpdateMorphs(FORWARD_L, ForwardEffect(_trackLeftBreast.depthDiff, upsideDownFixerMultiLeft));
            }
            else
            {
                // back force on left breast
                ResetMorphs(FORWARD_L);
            }

            if(_trackRightBreast.depthDiff < 0)
            {
                // forward force on right breast
                UpdateMorphs(FORWARD_R, ForwardEffect(_trackRightBreast.depthDiff, upsideDownFixerMultiRight));
            }
            else
            {
                // back force on right breast
                ResetMorphs(FORWARD_R);
            }

            float depthDiffCenter = (_trackLeftBreast.depthDiff + _trackRightBreast.depthDiff) / 2;
            float upsideDownFixerMultiCenter = (upsideDownFixerMultiLeft + upsideDownFixerMultiRight) / 2;
            if(depthDiffCenter < 0)
            {
                // forward force on average of left and right breast
                UpdateMorphs(FORWARD_C, ForwardEffect(depthDiffCenter, upsideDownFixerMultiCenter));
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

        private static float BackEffect(float depthDiff, float leanBackFixedMulti) =>
            _backSoftnessMultiplier
            * _backMassMultiplier
            * leanBackFixedMulti
            * Curves.QuadraticRegression(backMultiplier)
            * Curves.ZForceEffectCurve(Mathf.Abs(depthDiff) * _backBaseMassFactor);

        private static void AdjustBackMorphs()
        {
            float leanBackFixerMultiLeft = CalculateLeanBackFixerMultiplier(_pitchL, _rollL);
            float leanBackFixerMultiRight = CalculateLeanBackFixerMultiplier(_pitchR, _rollR);

            if(_trackLeftBreast.depthDiff < 0)
            {
                // forward force on left breast
                ResetMorphs(BACK_L);
            }
            else
            {
                // back force on left breast
                UpdateMorphs(BACK_L, BackEffect(_trackLeftBreast.depthDiff, leanBackFixerMultiLeft));
            }

            if(_trackRightBreast.depthDiff < 0)
            {
                // forward force on right breast
                ResetMorphs(BACK_R);
            }
            else
            {
                // back force on right breast
                UpdateMorphs(BACK_R, BackEffect(_trackRightBreast.depthDiff, leanBackFixerMultiRight));
            }

            float depthDiffCenter = (_trackLeftBreast.depthDiff + _trackRightBreast.depthDiff) / 2;
            float leanBackFixedMultiCenter = (leanBackFixerMultiLeft + leanBackFixerMultiRight) / 2;
            if(depthDiffCenter < 0)
            {
                // forward force on average of left and right breast
                ResetMorphs(BACK_C);
            }
            else
            {
                // back force on average of left and right breast
                UpdateMorphs(BACK_C, BackEffect(depthDiffCenter, leanBackFixedMultiCenter));
            }
        }

        private static float SidewaysEffect(float angle, float multiplier, float rollAngleMulti) =>
            _sidewaysSoftnessMultiplier
            * _sidewaysMassMultiplier
            * Curves.QuadraticRegression(multiplier)
            * Curves.XForceEffectCurve(rollAngleMulti * Mathf.Abs(angle) / _leftRightBaseMassFactor);

        private static void AdjustSidewaysMorphs()
        {
            if(_trackLeftBreast.angleX >= 0)
            {
                // right force on left breast
                ResetMorphs(LEFT_L);
                UpdateMorphs(RIGHT_L, SidewaysEffect(_trackLeftBreast.angleX, sidewaysInMultiplier, _rollAngleMultiL));
            }
            else
            {
                // left force on left breast
                ResetMorphs(RIGHT_L);
                UpdateMorphs(LEFT_L, SidewaysEffect(_trackLeftBreast.angleX, sidewaysOutMultiplier, _rollAngleMultiL));
            }

            if(_trackRightBreast.angleX >= 0)
            {
                // right force on right breast
                ResetMorphs(LEFT_R);
                UpdateMorphs(RIGHT_R, SidewaysEffect(_trackRightBreast.angleX, sidewaysOutMultiplier, _rollAngleMultiR));
            }
            else
            {
                // left force on right breast
                ResetMorphs(RIGHT_R);
                UpdateMorphs(LEFT_R, SidewaysEffect(_trackRightBreast.angleX, sidewaysInMultiplier, _rollAngleMultiR));
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
            config.morph.morphValue = inRange ? Calc.RoundToDecimals(value) : 0;
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
            AdjustSidewaysMorphs();
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
            sidewaysInJsf = null;
            sidewaysOutJsf = null;
        }
    }
}
