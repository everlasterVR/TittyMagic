using System.Collections.Generic;
using System.Linq;
using TittyMagic.Configs;
using UnityEngine;
using static TittyMagic.GravityEffectCalc;

namespace TittyMagic
{
    internal class ForceMorphHandler
    {
        private readonly Script _script;

        private readonly TrackNipple _trackLeftNipple;
        private readonly TrackNipple _trackRightNipple;

        private float _pitchMultiplier;
        private float _rollMultiplier;
        private float _leanBackFixerMultiplier;

        private Dictionary<string, List<MorphConfig>> _configSets;

        public JSONStorableFloat baseJsf { get; }
        public JSONStorableFloat upJsf { get; }
        public JSONStorableFloat downJsf { get; }
        public JSONStorableFloat forwardJsf { get; }
        public JSONStorableFloat backJsf { get; }
        public JSONStorableFloat leftRightJsf { get; }

        public float upDownExtraMultiplier { get; set; }
        public float forwardExtraMultiplier { get; set; }
        public float backExtraMultiplier { get; set; }
        public float leftRightExtraMultiplier { get; set; }

        private float upMultiplier => baseJsf.val * upJsf.val;
        private float downMultiplier => baseJsf.val * downJsf.val;
        private float forwardMultiplier => baseJsf.val * forwardJsf.val;
        private float backMultiplier => baseJsf.val * backJsf.val;
        private float leftRightMultiplier => baseJsf.val * leftRightJsf.val;

        public ForceMorphHandler(Script script, TrackNipple trackLeftNipple, TrackNipple trackRightNipple)
        {
            _script = script;
            _trackLeftNipple = trackLeftNipple;
            _trackRightNipple = trackRightNipple;

            baseJsf = script.NewJSONStorableFloat("forceMorphingBase", 1.00f, 0.00f, 2.00f);
            upJsf = script.NewJSONStorableFloat("forceMorphingUp", 1.00f, 0.00f, 2.00f);
            downJsf = script.NewJSONStorableFloat("forceMorphingDown", 1.00f, 0.00f, 2.00f);
            forwardJsf = script.NewJSONStorableFloat("forceMorphingForward", 1.00f, 0.00f, 2.00f);
            backJsf = script.NewJSONStorableFloat("forceMorphingBack", 1.00f, 0.00f, 2.00f);
            leftRightJsf = script.NewJSONStorableFloat("forceMorphingLeftRight", 1.00f, 0.00f, 2.00f);
        }

        public void LoadSettings() =>
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

        private List<MorphConfig> LoadSettingsFromFile(string subDir, string fileName, string morphNameSuffix = null)
        {
            string path = $@"{_script.PluginPath()}\settings\morphmultipliers\female\{fileName}.json";
            var jsonClass = _script.LoadJSON(path).AsObject;

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

        public void Update(float roll, float pitch)
        {
            _rollMultiplier = CalculateRollMultiplier(roll);
            _pitchMultiplier = CalculatePitchMultiplier(pitch, roll);
            _leanBackFixerMultiplier = CalculateLeanBackFixerMultiplier(pitch, roll);

            AdjustUpMorphs();
            AdjustDownMorphs();
            AdjustForwardMorphs();
            AdjustBackMorphs();
            AdjustLeftRightMorphs();
        }

        private void AdjustUpMorphs()
        {
            float multiplier = Curves.QuadraticRegression(upMultiplier) * upDownExtraMultiplier;
            float effectYLeft = CalculateYEffect(_trackLeftNipple.angleY, multiplier);
            float effectYRight = CalculateYEffect(_trackRightNipple.angleY, multiplier);
            float angleYCenter = (_trackRightNipple.angleY + _trackLeftNipple.angleY) / 2;
            float effectYCenter = CalculateYEffect(angleYCenter, multiplier);

            // up force on left breast
            if(_trackLeftNipple.angleY >= 0)
            {
                UpdateMorphs(Direction.UP_L, effectYLeft);
            }
            // down force on left breast
            else
            {
                ResetMorphs(Direction.UP_L);
            }

            // up force on right breast
            if(_trackRightNipple.angleY >= 0)
            {
                UpdateMorphs(Direction.UP_R, effectYRight);
            }
            // down force on right breast
            else
            {
                ResetMorphs(Direction.UP_R);
            }

            // up force on average of left and right breast
            if(angleYCenter >= 0)
            {
                UpdateMorphs(Direction.UP_C, effectYCenter);
            }
            else
            {
                ResetMorphs(Direction.UP_C);
            }
        }

        private void AdjustDownMorphs()
        {
            float multiplier = Curves.QuadraticRegression(downMultiplier) * upDownExtraMultiplier;
            float effectYLeft = CalculateYEffect(_trackLeftNipple.angleY, multiplier);
            float effectYRight = CalculateYEffect(_trackRightNipple.angleY, multiplier);

            // up force on left breast
            if(_trackLeftNipple.angleY >= 0)
            {
                ResetMorphs(Direction.DOWN_L);
            }
            // down force on left breast
            else
            {
                UpdateMorphs(Direction.DOWN_L, effectYLeft);
            }

            // up force on right breast
            if(_trackRightNipple.angleY >= 0)
            {
                ResetMorphs(Direction.DOWN_R);
            }
            // down force on right breast
            else
            {
                UpdateMorphs(Direction.DOWN_R, effectYRight);
            }
        }

        private void AdjustForwardMorphs()
        {
            float multiplier = Curves.QuadraticRegression(forwardMultiplier) * forwardExtraMultiplier;
            float effectZLeft = CalculateZEffect(_trackLeftNipple.depthDiff, multiplier);
            float effectZRight = CalculateZEffect(_trackRightNipple.depthDiff, multiplier);
            float depthDiffCenter = (_trackLeftNipple.depthDiff + _trackRightNipple.depthDiff) / 2;
            float effectZCenter = CalculateZEffect(depthDiffCenter, multiplier);

            // forward force on left breast
            if(_trackLeftNipple.depthDiff <= 0)
            {
                UpdateMorphs(Direction.FORWARD_L, effectZLeft);
            }
            // back force on left breast
            else
            {
                ResetMorphs(Direction.FORWARD_L);
            }

            // forward force on right breast
            if(_trackRightNipple.depthDiff <= 0)
            {
                UpdateMorphs(Direction.FORWARD_R, effectZRight);
            }
            // back force on right breast
            else
            {
                ResetMorphs(Direction.FORWARD_R);
            }

            // forward force on average of left and right breast
            if(depthDiffCenter <= 0)
            {
                UpdateMorphs(Direction.FORWARD_C, effectZCenter);
            }
            // back force on average of left and right breast
            else
            {
                ResetMorphs(Direction.FORWARD_C);
            }
        }

        private void AdjustBackMorphs()
        {
            float multiplier = _leanBackFixerMultiplier * Curves.QuadraticRegression(backMultiplier) * backExtraMultiplier;
            float effectZLeft = CalculateZEffect(_trackLeftNipple.depthDiff, multiplier);
            float effectZRight = CalculateZEffect(_trackRightNipple.depthDiff, multiplier);
            float depthDiffCenter = (_trackLeftNipple.depthDiff + _trackRightNipple.depthDiff) / 2;
            float effectZCenter = CalculateZEffect(depthDiffCenter, multiplier);

            // forward force on left breast
            if(_trackLeftNipple.depthDiff <= 0)
            {
                ResetMorphs(Direction.BACK_L);
            }
            // back force on left breast
            else
            {
                UpdateMorphs(Direction.BACK_L, effectZLeft);
            }

            // forward force on right breast
            if(_trackRightNipple.depthDiff <= 0)
            {
                ResetMorphs(Direction.BACK_R);
            }
            // back force on right breast
            else
            {
                UpdateMorphs(Direction.BACK_R, effectZRight);
            }

            // forward force on average of left and right breast
            if(depthDiffCenter <= 0)
            {
                ResetMorphs(Direction.BACK_C);
            }
            // back force on average of left and right breast
            else
            {
                UpdateMorphs(Direction.BACK_C, effectZCenter);
            }
        }

        private void AdjustLeftRightMorphs()
        {
            float multiplier = Curves.QuadraticRegression(leftRightMultiplier) * leftRightExtraMultiplier;
            float effectXLeft = CalculateXEffect(_trackLeftNipple.angleX, multiplier);
            float effectXRight = CalculateXEffect(_trackRightNipple.angleX, multiplier);

            // left force on left breast
            if(_trackLeftNipple.angleX >= 0)
            {
                ResetMorphs(Direction.LEFT_L);
                UpdateMorphs(Direction.RIGHT_L, effectXLeft);
            }
            // right force on left breast
            else
            {
                ResetMorphs(Direction.RIGHT_L);
                UpdateMorphs(Direction.LEFT_L, effectXLeft);
            }

            // left force on right breast
            if(_trackRightNipple.angleX >= 0)
            {
                ResetMorphs(Direction.LEFT_R);
                UpdateMorphs(Direction.RIGHT_R, effectXRight);
            }
            // right force on right breast
            else
            {
                ResetMorphs(Direction.RIGHT_R);
                UpdateMorphs(Direction.LEFT_R, effectXRight);
            }
        }

        private static float CalculateRollMultiplier(float roll) =>
            Mathf.Lerp(1.25f, 1f, Mathf.Abs(roll));

        private static float CalculatePitchMultiplier(float pitch, float roll) =>
            Mathf.Lerp(0.72f, 1f, CalculateDiffFromHorizontal(pitch, roll)); // same for upright and upside down

        private float CalculateLeanBackFixerMultiplier(float pitch, float roll)
        {
            if(pitch < -0.5f || pitch > 0)
            {
                return 1;
            }

            float diff = 4 * Mathf.Abs(-0.25f - pitch);
            float minTarget1 = Mathf.Lerp(0.25f, 1.00f, _script.mainPhysicsHandler.normalizedInvertedMass);
            float minTarget2 = Mathf.Lerp(minTarget1, 1.00f, Mathf.Abs(roll * roll));
            return Mathf.Lerp(minTarget2, 1.00f, diff);
        }

        private float CalculateYEffect(float angle, float multiplier) =>
            multiplier * Curve(_pitchMultiplier * Mathf.Abs(angle) / 75);

        private static float CalculateZEffect(float distance, float multiplier) =>
            multiplier * Curve(Mathf.Abs(distance) * 10.8f);

        private float CalculateXEffect(float angle, float multiplier) =>
            multiplier * Curve(_rollMultiplier * Mathf.Abs(angle) / 60);

        // https://www.desmos.com/calculator/ykxswso5ie
        private static float Curve(float effect) => Curves.InverseSmoothStep(effect, 10, 0.8f, 0f);

        private void UpdateMorphs(string configSetName, float effect)
        {
            float mass = _script.mainPhysicsHandler.realMassAmount;
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

        public void ResetAll() => _configSets?.Keys.ToList().ForEach(ResetMorphs);

        private void ResetMorphs(string configSetName) =>
            _configSets[configSetName].ForEach(config => config.morph.morphValue = 0);
    }
}
