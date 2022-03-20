// #define USE_CONFIGURATOR
using System.Collections.Generic;
using UnityEngine;
using static TittyMagic.Utils;
using static TittyMagic.GravityEffectCalc;

namespace TittyMagic
{
    internal class ForceMorphHandler
    {
        private readonly MVRScript _script;
        private readonly IConfigurator _configurator;

        private readonly TrackNipple _trackLeftNipple;
        private readonly TrackNipple _trackRightNipple;

        public Multiplier xMultiplier { get; set; }
        public Multiplier yMultiplier { get; set; }
        public Multiplier zMultiplier { get; set; }

        private Dictionary<string, List<Config>> _configSets;

        private float _mass;
        private float _softness;

        public ForceMorphHandler(MVRScript script, TrackNipple trackLeftNipple, TrackNipple trackRightNipple)
        {
            _script = script;
            _trackLeftNipple = trackLeftNipple;
            _trackRightNipple = trackRightNipple;
#if USE_CONFIGURATOR
            _configurator = (IConfigurator) FindPluginOnAtom(_script.containingAtom, nameof(ForceMorphConfigurator));
            _configurator.InitMainUI();
            _configurator.enableAdjustment.toggle.onValueChanged.AddListener(
                val =>
                {
                    if(!val)
                    {
                        ResetAll();
                    }
                }
            );
#endif
        }

        public void LoadSettings(string mode)
        {
            _configSets = new Dictionary<string, List<Config>>
            {
                { Direction.UP_L, LoadSettingsFromFile(mode, "upForce", " L") },
                { Direction.UP_R, LoadSettingsFromFile(mode, "upForce", " R") },
                { Direction.UP_C, LoadSettingsFromFile(mode, "upForceCenter") },
                { Direction.BACK_L, LoadSettingsFromFile(mode, "backForce", " L") },
                { Direction.BACK_R, LoadSettingsFromFile(mode, "backForce", " R") },
                { Direction.BACK_C, LoadSettingsFromFile(mode, "backForceCenter") },
                { Direction.FORWARD_L, LoadSettingsFromFile(mode, "forwardForce", " L") },
                { Direction.FORWARD_R, LoadSettingsFromFile(mode, "forwardForce", " R") },
                { Direction.FORWARD_C, LoadSettingsFromFile(mode, "forwardForceCenter") },
                { Direction.LEFT_L, LoadSettingsFromFile(mode, "leftForceL") },
                { Direction.LEFT_R, LoadSettingsFromFile(mode, "leftForceR") },
                { Direction.RIGHT_L, LoadSettingsFromFile(mode, "rightForceL") },
                { Direction.RIGHT_R, LoadSettingsFromFile(mode, "rightForceR") },
            };

            // not working properly yet when changing mode on the fly
            if(_configurator != null)
            {
                _configurator.ResetUISectionGroups();
                // _configurator.InitUISectionGroup(Direction.UP_L, _configSets[Direction.UP_L]);
                // _configurator.InitUISectionGroup(Direction.UP_R, _configSets[Direction.UP_R]);
                // _configurator.InitUISectionGroup(Direction.UP_C, _configSets[Direction.UP_C]);
                // _configurator.InitUISectionGroup(Direction.BACK_L, _configSets[Direction.BACK_L]);
                // _configurator.InitUISectionGroup(Direction.BACK_R, _configSets[Direction.BACK_R]);
                // _configurator.InitUISectionGroup(Direction.BACK_C, _configSets[Direction.BACK_C]);
                // _configurator.InitUISectionGroup(Direction.FORWARD_L, _configSets[Direction.FORWARD_L]);
                // _configurator.InitUISectionGroup(Direction.FORWARD_R, _configSets[Direction.FORWARD_R]);
                // _configurator.InitUISectionGroup(Direction.FORWARD_C, _configSets[Direction.FORWARD_C]);
                // _configurator.InitUISectionGroup(Direction.LEFT_L, _configSets[Direction.LEFT_L]);
                // _configurator.InitUISectionGroup(Direction.LEFT_R, _configSets[Direction.LEFT_R]);
                // _configurator.InitUISectionGroup(Direction.RIGHT_L, _configSets[Direction.RIGHT_L]);
                // _configurator.InitUISectionGroup(Direction.RIGHT_R, _configSets[Direction.RIGHT_R]);
            }
        }

        private List<Config> LoadSettingsFromFile(string mode, string fileName, string morphNameSuffix = null)
        {
            var configs = new List<Config>();
            Persistence.LoadModeMorphSettings(
                _script,
                mode,
                $"{fileName}.json",
                (dir, json) =>
                {
                    foreach(string name in json.Keys)
                    {
                        string morphName = string.IsNullOrEmpty(morphNameSuffix) ? name : name + $"{morphNameSuffix}";
                        configs.Add(
                            new MorphConfig(
                                morphName,
                                json[name]["IsNegative"].AsBool,
                                json[name]["Multiplier1"].AsFloat,
                                json[name]["Multiplier2"].AsFloat
                            )
                        );
                    }
                }
            );
            return configs;
        }

        public bool IsEnabled()
        {
            return _configurator == null || _configurator.enableAdjustment.val;
        }

        public void Update(
            float roll,
            float pitch,
            float mass,
            float softness
        )
        {
            _mass = mass;
            _softness = softness;

            AdjustLeftRightMorphs(CalculateRollExtraMultiplier(roll, xMultiplier.extraMultiplier2));
            AdjustUpMorphs(CalculateUpDownExtraMultiplier(roll, pitch, yMultiplier.extraMultiplier2));
            AdjustDepthMorphs(
                CalculateDepthExtraMultiplier(roll, pitch, yMultiplier.extraMultiplier2),
                CalculateDepthExtraMultiplier(roll, pitch, yMultiplier.oppositeExtraMultiplier2)
            );

            if(_configurator != null)
            {
                _configurator.debugInfo.val =
                    $"{NameValueString("Left depthDiff", _trackLeftNipple.depthDiff)} \n" +
                    $"{NameValueString("Right depthDiff", _trackRightNipple.depthDiff)} \n" +
                    $"{NameValueString("Left angleY", _trackLeftNipple.angleY)} \n" +
                    $"{NameValueString("Right angleY", _trackRightNipple.angleY)} \n" +
                    $"{NameValueString("Left angleX", _trackLeftNipple.angleX)} \n" +
                    $"{NameValueString("Right angleX", _trackRightNipple.angleX)} \n" +
                    $"{NameValueString("Roll", roll)} \n" +
                    $"{NameValueString("Pitch", pitch)}";
            }
        }

        private void AdjustUpMorphs(float extraMultiplier2)
        {
            float multiplier = yMultiplier.mainMultiplier * yMultiplier.extraMultiplier1 * extraMultiplier2;
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

        private void AdjustDepthMorphs(float extraMultiplier2, float oppositeExtraMultiplier2)
        {
            float forwardMultiplier = zMultiplier.mainMultiplier * zMultiplier.extraMultiplier1 * extraMultiplier2;
            float backMultiplier = zMultiplier.mainMultiplier * zMultiplier.oppositeExtraMultiplier1 * oppositeExtraMultiplier2;

            float effectZLeft = CalculateZEffect(_trackLeftNipple.depthDiff, _trackLeftNipple.depthDiff < 0 ? forwardMultiplier : backMultiplier);
            float effectZRight = CalculateZEffect(_trackRightNipple.depthDiff, _trackRightNipple.depthDiff < 0 ? forwardMultiplier : backMultiplier);

            float depthDiffCenter = (_trackLeftNipple.depthDiff + _trackRightNipple.depthDiff) / 2;
            float effectZCenter = CalculateZEffect(depthDiffCenter, depthDiffCenter < 0 ? forwardMultiplier : backMultiplier);

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

        private void AdjustLeftRightMorphs(float extraMultiplier2)
        {
            float multiplier = xMultiplier.mainMultiplier * xMultiplier.extraMultiplier1 * extraMultiplier2;
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

        private static float CalculateYEffect(float angle, float multiplier)
        {
            return multiplier * Mathf.Abs(angle) / 75;
        }

        private static float CalculateZEffect(float distance, float multiplier)
        {
            return multiplier * Mathf.Abs(distance) * 12;
        }

        private static float CalculateXEffect(float angle, float multiplier)
        {
            return multiplier * Mathf.Abs(angle) / 60;
        }

        private void UpdateMorphs(string configSetName, float effect)
        {
            foreach(var config in _configSets[configSetName])
            {
                var morphConfig = (MorphConfig) config;
                UpdateValue(morphConfig, effect);
                if(_configurator != null)
                {
                    _configurator.UpdateValueSlider(configSetName, morphConfig.name, morphConfig.morph.morphValue);
                }
            }
        }

        private void UpdateValue(MorphConfig config, float effect)
        {
            float value =
                (_softness * config.multiplier1 * effect / 2) +
                (_mass * config.multiplier2 * effect / 2);

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
                if(_configurator != null)
                {
                    _configurator.UpdateValueSlider(configSetName, morphConfig.name, 0);
                }
            }
        }
    }
}
