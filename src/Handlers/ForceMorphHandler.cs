using System.Collections.Generic;
using System.Linq;
using TittyMagic.Extensions;
using UnityEngine;
using static TittyMagic.Utils;
using static TittyMagic.GravityEffectCalc;

namespace TittyMagic
{
    internal class ForceMorphHandler
    {
        private readonly Script _script;

        private readonly TrackNipple _trackLeftNipple;
        private readonly TrackNipple _trackRightNipple;

        public Multiplier xMultiplier { get; }
        public Multiplier yMultiplier { get; }
        public Multiplier zMultiplier { get; }

        private Dictionary<string, List<Config>> _configSets;

        private const float SOFTNESS = 0.62f;
        private float _mass;
        private float _pitchMultiplier;
        private float _rollMultiplier;

        public ForceMorphHandler(Script script, TrackNipple trackLeftNipple, TrackNipple trackRightNipple)
        {
            _script = script;
            _trackLeftNipple = trackLeftNipple;
            _trackRightNipple = trackRightNipple;
            xMultiplier = new Multiplier();
            yMultiplier = new Multiplier();
            zMultiplier = new Multiplier();
        }

        public void LoadSettings()
        {
            _configSets = new Dictionary<string, List<Config>>
            {
                { Direction.UP_L, LoadSettingsFromFile(Direction.UP, "upForce", " L") },
                { Direction.UP_R, LoadSettingsFromFile(Direction.UP, "upForce", " R") },
                { Direction.UP_C, LoadSettingsFromFile(Direction.UP, "upForceCenter") },
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
        }

        private List<Config> LoadSettingsFromFile(string subDir, string fileName, string morphNameSuffix = null)
        {
            string path = $@"{_script.PluginPath()}\settings\morphmultipliers\female\{fileName}.json";
            var json = _script.LoadJSON(path).AsObject;

            return json.Keys.Select(name =>
            {
                string morphName = string.IsNullOrEmpty(morphNameSuffix) ? name : name + $"{morphNameSuffix}";
                return new MorphConfig(
                    $"{subDir}/{morphName}",
                    json[name]["IsNegative"].AsBool,
                    json[name]["Multiplier1"].AsFloat,
                    json[name]["Multiplier2"].AsFloat
                ) as Config;
            }).ToList();
        }

        public void Update(
            float roll,
            float pitch,
            float mass
        )
        {
            _rollMultiplier = CalculateRollMultiplier(roll);
            _pitchMultiplier = CalculatePitchMultiplier(pitch, roll);
            _mass = mass;

            AdjustUpMorphs();
            AdjustDepthMorphs();
            AdjustLeftRightMorphs();
        }

        private void AdjustUpMorphs()
        {
            float multiplier = yMultiplier.mainMultiplier * (yMultiplier.extraMultiplier ?? 1);
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

        private void AdjustDepthMorphs()
        {
            float forwardMultiplier = zMultiplier.mainMultiplier * (zMultiplier.extraMultiplier ?? 1);
            float backMultiplier = zMultiplier.mainMultiplier * (zMultiplier.oppositeExtraMultiplier ?? 1);

            float leftMultiplier = _trackLeftNipple.depthDiff < 0 ? forwardMultiplier : backMultiplier;
            float rightMultiplier = _trackRightNipple.depthDiff < 0 ? forwardMultiplier : backMultiplier;

            float effectZLeft = CalculateZEffect(_trackLeftNipple.depthDiff, leftMultiplier);
            float effectZRight = CalculateZEffect(_trackRightNipple.depthDiff, rightMultiplier);

            float depthDiffCenter = (_trackLeftNipple.depthDiff + _trackRightNipple.depthDiff) / 2;
            float centerMultiplier = depthDiffCenter < 0 ? forwardMultiplier : backMultiplier;
            float effectZCenter = CalculateZEffect(depthDiffCenter, centerMultiplier);

            // forward force on left breast
            if(_trackLeftNipple.depthDiff <= 0)
            {
                ResetMorphs(Direction.BACK_L);
                UpdateMorphs(Direction.FORWARD_L, effectZLeft);
            }
            // back force on left breast
            else
            {
                ResetMorphs(Direction.FORWARD_L);
                UpdateMorphs(Direction.BACK_L, effectZLeft);
            }

            // forward force on right breast
            if(_trackRightNipple.depthDiff <= 0)
            {
                ResetMorphs(Direction.BACK_R);
                UpdateMorphs(Direction.FORWARD_R, effectZRight);
            }
            // back force on right breast
            else
            {
                ResetMorphs(Direction.FORWARD_R);
                UpdateMorphs(Direction.BACK_R, effectZRight);
            }

            // forward force on average of left and right breast
            if(depthDiffCenter <= 0)
            {
                ResetMorphs(Direction.BACK_C);
                UpdateMorphs(Direction.FORWARD_C, effectZCenter);
            }
            // back force on average of left and right breast
            else
            {
                ResetMorphs(Direction.FORWARD_C);
                UpdateMorphs(Direction.BACK_C, effectZCenter);
            }
        }

        private void AdjustLeftRightMorphs()
        {
            float multiplier = xMultiplier.mainMultiplier * (xMultiplier.extraMultiplier ?? 1);
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

        private static float CalculatePitchMultiplier(float pitch, float roll)
        {
            float effect = CalculateDiffFromHorizontal(pitch, roll);
            // upright
            if(effect >= 0)
            {
                return Mathf.Lerp(0.72f, 1f, effect);
            }

            // upside down
            return Mathf.Lerp(0.72f, 1f, effect);
        }

        private static float CalculateRollMultiplier(float roll)
        {
            return Mathf.Lerp(1.25f, 1f, Mathf.Abs(roll));
        }

        private float CalculateYEffect(float angle, float multiplier)
        {
            return multiplier * Curve(_pitchMultiplier * Mathf.Abs(angle) / 75);
        }

        private static float CalculateZEffect(float distance, float multiplier)
        {
            return multiplier * Curve(Mathf.Abs(distance) * 12);
        }

        private float CalculateXEffect(float angle, float multiplier)
        {
            return multiplier * Curve(_rollMultiplier * Mathf.Abs(angle) / 60);
        }

        // https://www.desmos.com/calculator/ykxswso5ie
        private static float Curve(float effect)
        {
            return Calc.InverseSmoothStep(effect, 10, 0.8f, 0f);
        }

        private void UpdateMorphs(string configSetName, float effect)
        {
            foreach(var config in _configSets[configSetName])
            {
                var morphConfig = (MorphConfig) config;
                UpdateValue(morphConfig, effect);
            }
        }

        private void UpdateValue(MorphConfig config, float effect)
        {
            float value =
                SOFTNESS * config.softnessMultiplier * effect / 2 +
                _mass * config.massMultiplier * effect / 2;

            bool inRange = config.isNegative ? value < 0 : value > 0;
            config.morph.morphValue = inRange ? Calc.RoundToDecimals(value, 1000f) : 0;
        }

        public void ResetAll()
        {
            _configSets?.Keys.ToList().ForEach(ResetMorphs);
        }

        private void ResetMorphs(string configSetName)
        {
            foreach(var config in _configSets[configSetName])
            {
                var morphConfig = (MorphConfig) config;
                morphConfig.morph.morphValue = 0;
            }
        }
    }
}
