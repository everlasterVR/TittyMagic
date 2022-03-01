﻿using System;
using System.Collections.Generic;
using UnityEngine;
using static TittyMagic.Utils;

namespace TittyMagic
{
    internal class RelativePosMorphHandler
    {
        private readonly MVRScript _script;
        private readonly IConfigurator _configurator;
        private readonly bool _useConfigurator;
        private float _yAngleMultiplier;
        private float _xAngleMultiplier;
        private float _backDepthMultiplier;
        private float _forwardDepthMultiplier;
        private float _mass;
        private float _mobility;

        private Dictionary<string, List<Config>> _configSets;

        public RelativePosMorphHandler(MVRScript script)
        {
            _script = script;

            try
            {
                _configurator = (IConfigurator) _script;
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
                _useConfigurator = true;
            }
            catch(Exception)
            {
                _useConfigurator = false;
            }
        }

        public void LoadSettings(string mode)
        {
            _configSets = new Dictionary<string, List<Config>>
            {
                { Direction.DOWN_L, LoadSettingsFromFile(mode, "downForce", " L") },
                { Direction.DOWN_R, LoadSettingsFromFile(mode, "downForce", " R") },
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
            if(_useConfigurator)
            {
                _configurator.ResetUISectionGroups();
                // _configurator.InitUISectionGroup(Direction.DOWN_L, _configSets[Direction.DOWN_L]);
                // _configurator.InitUISectionGroup(Direction.DOWN_R, _configSets[Direction.DOWN_R]);
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
            return !_useConfigurator || _configurator.enableAdjustment.val;
        }

        private void UpdateDebugInfo(string text)
        {
            if(!_useConfigurator)
            {
                return;
            }

            _configurator.debugInfo.val = text;
        }

        public void Update(
            float angleYLeft,
            float angleYRight,
            float depthDiffLeft,
            float depthDiffRight,
            float angleXLeft,
            float angleXRight,
            float yAngleMultiplier,
            float xAngleMultiplier,
            float backDepthMultiplier,
            float forwardDepthMultiplier,
            float mass,
            float mobility
        )
        {
            _yAngleMultiplier = yAngleMultiplier;
            _xAngleMultiplier = xAngleMultiplier;
            _backDepthMultiplier = backDepthMultiplier;
            _forwardDepthMultiplier = forwardDepthMultiplier;
            _mass = mass;
            _mobility = mobility;

            float effectYLeft = CalculateYAngleEffect(angleYLeft);
            float effectYRight = CalculateYAngleEffect(angleYRight);
            float angleYCenter = (angleYRight + angleYLeft) / 2;
            float effectYCenter = CalculateYAngleEffect(angleYCenter);

            float effectZLeft = CalculateDepthEffect(depthDiffLeft);
            float effectZRight = CalculateDepthEffect(depthDiffRight);
            float depthDiffCenter = (depthDiffLeft + depthDiffRight) / 2;
            float effectZCenter = CalculateDepthEffect(depthDiffCenter);

            float effectXLeft = CalculateXAngleEffect(angleXLeft);
            float effectXRight = CalculateXAngleEffect(angleXRight);

            // up force on left breast
            if(angleYLeft >= 0)
            {
                ResetMorphs(Direction.DOWN_L);
                UpdateMorphs(Direction.UP_L, effectYLeft);
            }
            // down force on left breast
            else
            {
                ResetMorphs(Direction.UP_L);
                UpdateMorphs(Direction.DOWN_L, effectYLeft);
            }

            // up force on right breast
            if(angleYRight >= 0)
            {
                ResetMorphs(Direction.DOWN_R);
                UpdateMorphs(Direction.UP_R, effectYRight);
            }
            // down force on right breast
            else
            {
                ResetMorphs(Direction.UP_R);
                UpdateMorphs(Direction.DOWN_R, effectYRight);
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

            // forward force on left breast
            if(depthDiffLeft <= 0)
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
            if(depthDiffRight <= 0)
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

            // left force on left breast
            if(angleXLeft >= 0)
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
            if(angleXRight >= 0)
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

            string infoText =
                $"{NameValueString("depthDiffLeft", depthDiffLeft)} \n" +
                $"{NameValueString("depthDiffRight", depthDiffRight)} \n" +
                // $"{NameValueString("angleYLeft", angleYLeft)} \n" +
                // $"{NameValueString("angleYRight", angleYRight)} \n" +
                $"{NameValueString("angleXLeft", angleXLeft)} \n" +
                $"{NameValueString("angleXRight", angleXRight)} \n";
            UpdateDebugInfo(infoText);
        }

        private float CalculateYAngleEffect(float value)
        {
            return _yAngleMultiplier * (Mathf.Abs(value) / 80);
        }

        private float CalculateDepthEffect(float value)
        {
            float multiplier = value < 0 ? _forwardDepthMultiplier : _backDepthMultiplier;
            return multiplier * Mathf.Abs(value);
        }

        private float CalculateXAngleEffect(float value)
        {
            return _xAngleMultiplier * (Mathf.Abs(value) / 65);
        }

        private void UpdateMorphs(string configSetName, float effect)
        {
            foreach(var config in _configSets[configSetName])
            {
                var morphConfig = (MorphConfig) config;
                UpdateValue(morphConfig, effect);
                if(_useConfigurator)
                {
                    _configurator.UpdateValueSlider(configSetName, morphConfig.name, morphConfig.morph.morphValue);
                }
            }
        }

        private void UpdateValue(MorphConfig config, float effect)
        {
            float value =
                (_mobility * config.multiplier1 * effect / 2) +
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
                if(_useConfigurator)
                {
                    _configurator.UpdateValueSlider(configSetName, morphConfig.name, 0);
                }
            }
        }
    }
}
