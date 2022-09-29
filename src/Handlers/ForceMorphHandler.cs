using System;
using System.Collections.Generic;
using System.Linq;
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

        private static Dictionary<string, List<MorphConfig>> _configSets;

        public static JSONStorableFloat baseJsf { get; private set; }
        public static JSONStorableFloat upJsf { get; private set; }
        public static JSONStorableFloat downJsf { get; private set; }
        public static JSONStorableFloat forwardJsf { get; private set; }
        public static JSONStorableFloat backJsf { get; private set; }
        public static JSONStorableFloat leftRightJsf { get; private set; }

        public static float upDownExtraMultiplier { get; set; }
        public static float forwardExtraMultiplier { get; set; }
        public static float backExtraMultiplier { get; set; }
        public static float leftRightExtraMultiplier { get; set; }

        private static float upMultiplier => baseJsf.val * upJsf.val;
        private static float downMultiplier => baseJsf.val * downJsf.val;
        private static float forwardMultiplier => baseJsf.val * forwardJsf.val;
        private static float backMultiplier => baseJsf.val * backJsf.val;
        private static float leftRightMultiplier => baseJsf.val * leftRightJsf.val;

        public static void Init(TrackBreast trackLeftBreast, TrackBreast trackRightBreast)
        {
            _trackLeftBreast = trackLeftBreast;
            _trackRightBreast = trackRightBreast;

            baseJsf = tittyMagic.NewJSONStorableFloat("forceMorphingBase", 1.00f, 0.00f, 2.00f);
            upJsf = tittyMagic.NewJSONStorableFloat("forceMorphingUp", 1.00f, 0.00f, 2.00f);
            downJsf = tittyMagic.NewJSONStorableFloat("forceMorphingDown", 1.00f, 0.00f, 2.00f);
            forwardJsf = tittyMagic.NewJSONStorableFloat("forceMorphingForward", 1.00f, 0.00f, 2.00f);
            backJsf = tittyMagic.NewJSONStorableFloat("forceMorphingBack", 1.00f, 0.00f, 2.00f);
            leftRightJsf = tittyMagic.NewJSONStorableFloat("forceMorphingLeftRight", 1.00f, 0.00f, 2.00f);
        }

        public static void LoadSettings() =>
            _configSets = new Dictionary<string, List<MorphConfig>>
            {
                { Direction.UP_L, LoadSettingsFromFile(Direction.UP, "upForce", " L") },
                { Direction.UP_R, LoadSettingsFromFile(Direction.UP, "upForce", " R") },
                { Direction.UP_C, LoadSettingsFromFile(Direction.UP, "upForceCenter") },
                { Direction.DOWN_L, LoadSettingsFromFile(Direction.DOWN, "downForce", " L") },
                { Direction.DOWN_R, LoadSettingsFromFile(Direction.DOWN, "downForce", " R") },
                { Direction.BACK_L, LoadSettingsFromFile(Direction.BACK, "backForce", " L") },
                { Direction.BACK_R, LoadSettingsFromFile(Direction.BACK, "backForce", " R") },
                { Direction.BACK_C, LoadSettingsFromFile(Direction.BACK, "backForceCenter") },
                { Direction.FORWARD_L, LoadSettingsFromFile(Direction.FORWARD, "forwardForce", " L") },
                { Direction.FORWARD_R, LoadSettingsFromFile(Direction.FORWARD, "forwardForce", " R") },
                { Direction.FORWARD_C, LoadSettingsFromFile(Direction.FORWARD, "forwardForceCenter") },
                { Direction.LEFT_L, LoadSettingsFromFile(Direction.LEFT, "leftForceL") },
                { Direction.LEFT_R, LoadSettingsFromFile(Direction.LEFT, "leftForceR") },
                { Direction.RIGHT_L, LoadSettingsFromFile(Direction.RIGHT, "rightForceL") },
                { Direction.RIGHT_R, LoadSettingsFromFile(Direction.RIGHT, "rightForceR") },
            };

        private static List<MorphConfig> LoadSettingsFromFile(string subDir, string fileName, string morphNameSuffix = null)
        {
            string path = $@"{tittyMagic.PluginPath()}\settings\morphmultipliers\female\{fileName}.json";
            var jsonClass = tittyMagic.LoadJSON(path).AsObject;

            return jsonClass.Keys.Select(name =>
                {
                    string morphName = string.IsNullOrEmpty(morphNameSuffix) ? name : name + $"{morphNameSuffix}";
                    return new MorphConfig(
                        $"{subDir}/{morphName}",
                        jsonClass[name]["IsNegative"].AsBool,
                        jsonClass[name]["Multiplier1"].AsFloat,
                        jsonClass[name]["Multiplier2"].AsFloat
                    );
                })
                .ToList();
        }

        public static void Update(float roll, float pitch)
        {
            float pitchMultiplier = Mathf.Lerp(0.80f, 1f, GravityEffectCalc.CalculateDiffFromHorizontal(pitch, roll));
            float rollMultiplier = Mathf.Lerp(1.25f, 1f, Mathf.Abs(roll));
            float leanBackFixerMultiplier = CalculateLeanBackFixerMultiplier(pitch, roll);

            AdjustUpMorphs(pitchMultiplier);
            AdjustDownMorphs(pitchMultiplier);
            AdjustForwardMorphs();
            AdjustBackMorphs(leanBackFixerMultiplier);
            AdjustLeftRightMorphs(rollMultiplier);
        }

        private static void AdjustUpMorphs(float pitchMultiplier)
        {
            Func<float, float> calculateEffect = angle =>
                upDownExtraMultiplier
                * Curves.QuadraticRegression(upMultiplier)
                * Curves.ForceEffectCurve(pitchMultiplier * Mathf.Abs(angle) / 75);

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

        private static void AdjustDownMorphs(float pitchMultiplier)
        {
            Func<float, float> calculateEffect = angle =>
                upDownExtraMultiplier
                * Curves.QuadraticRegression(downMultiplier)
                * Curves.ForceEffectCurve(pitchMultiplier * Mathf.Abs(angle) / 75);

            if(_trackLeftBreast.angleY >= 0)
            {
                // up force on left breast
                ResetMorphs(Direction.DOWN_L);
            }
            else
            {
                // down force on left breast
                UpdateMorphs(Direction.DOWN_L, calculateEffect(_trackLeftBreast.angleY));
            }

            if(_trackRightBreast.angleY >= 0)
            {
                // up force on right breast
                ResetMorphs(Direction.DOWN_R);
            }
            else
            {
                // down force on right breast
                UpdateMorphs(Direction.DOWN_R, calculateEffect(_trackRightBreast.angleY));
            }
        }

        private static void AdjustForwardMorphs()
        {
            Func<float, float> calculateEffect = distance =>
                forwardExtraMultiplier
                * Curves.QuadraticRegression(forwardMultiplier)
                * Curves.ForceEffectCurve(Mathf.Abs(distance) * 10.8f);

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

        private static void AdjustBackMorphs(float leanBackFixerMultiplier)
        {
            Func<float, float> calculateEffect = distance =>
                backExtraMultiplier
                * leanBackFixerMultiplier
                * Curves.QuadraticRegression(backMultiplier)
                * Curves.ForceEffectCurve(Mathf.Abs(distance) * 10.8f);

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

        private static void AdjustLeftRightMorphs(float rollMultiplier)
        {
            Func<float, float> calculateEffect = angle =>
                leftRightExtraMultiplier
                * Curves.QuadraticRegression(leftRightMultiplier)
                * Curves.ForceEffectCurve(rollMultiplier * Mathf.Abs(angle) / 60);

            float effectXLeft = calculateEffect(_trackLeftBreast.angleX);
            if(_trackLeftBreast.angleX >= 0)
            {
                // left force on left breast
                ResetMorphs(Direction.LEFT_L);
                UpdateMorphs(Direction.RIGHT_L, effectXLeft);
            }
            else
            {
                // right force on left breast
                ResetMorphs(Direction.RIGHT_L);
                UpdateMorphs(Direction.LEFT_L, effectXLeft);
            }

            float effectXRight = calculateEffect(_trackRightBreast.angleX);
            if(_trackRightBreast.angleX >= 0)
            {
                // left force on right breast
                ResetMorphs(Direction.LEFT_R);
                UpdateMorphs(Direction.RIGHT_R, effectXRight);
            }
            else
            {
                // right force on right breast
                ResetMorphs(Direction.RIGHT_R);
                UpdateMorphs(Direction.LEFT_R, effectXRight);
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
            _configSets[configSetName].ForEach(config => UpdateValue(config, effect, mass, softness));
        }

        private static void UpdateValue(MorphConfig config, float effect, float mass, float softness)
        {
            float value =
                softness * config.softnessMultiplier * effect / 2 +
                mass * config.massMultiplier * effect / 2;

            bool inRange = config.isNegative ? value < 0 : value > 0;
            config.morph.morphValue = inRange ? Calc.RoundToDecimals(value, 1000f) : 0;
        }

        public static void ResetAll() => _configSets?.Keys.ToList().ForEach(ResetMorphs);

        private static void ResetMorphs(string configSetName) =>
            _configSets[configSetName].ForEach(config => config.morph.morphValue = 0);

        public static void Destroy()
        {
            _trackLeftBreast = null;
            _trackRightBreast = null;
            _configSets = null;
            baseJsf = null;
            upJsf = null;
            downJsf = null;
            forwardJsf = null;
            backJsf = null;
            leftRightJsf = null;
        }
    }
}
