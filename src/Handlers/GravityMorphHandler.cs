﻿using System;
using System.Collections.Generic;
using UnityEngine;
using static TittyMagic.Utils;

namespace TittyMagic
{
    internal class GravityMorphHandler
    {
        private MVRScript _script;
        private IConfigurator _configurator;

        private bool _useConfigurator;

        private float _mass;
        private float _amount;
        private float _additionalRollEffect;

        private Dictionary<string, List<Config>> _configSets;

        public GravityMorphHandler(MVRScript script)
        {
            _script = script;
            try
            {
                _configurator = (IConfigurator) _script;
                _configurator.InitMainUI();
                _configurator.EnableAdjustment.toggle.onValueChanged.AddListener((bool val) =>
                {
                    if(!val)
                    {
                        ResetAll();
                    }
                });
                _useConfigurator = true;
            }
            catch(Exception)
            {
                _useConfigurator = false;
            }
        }

        public void LoadSettings(string mode)
        {
            _configSets = new Dictionary<string, List<Config>>();

            if(mode != Mode.ANIM_OPTIMIZED)
            {
                _configSets.Add(Direction.DOWN, LoadSettingsFromFile(mode, "upright"));
                _configSets.Add(Direction.UP, LoadSettingsFromFile(mode, "upsideDown"));
                _configSets.Add(Direction.LEFT, LoadSettingsFromFile(mode, "rollLeft"));
                _configSets.Add(Direction.RIGHT, LoadSettingsFromFile(mode, "rollRight"));
            }
            _configSets.Add(Direction.BACK, LoadSettingsFromFile(mode, "leanBack"));
            _configSets.Add(Direction.FORWARD, LoadSettingsFromFile(mode, "leanForward"));

            //not working properly yet when changing mode on the fly
            if(_useConfigurator)
            {
                _configurator.ResetUISectionGroups();
                if(mode != Mode.ANIM_OPTIMIZED)
                {
                    //_configurator.InitUISectionGroup(Direction.DOWN, _configSets[Direction.DOWN]);
                    //_configurator.InitUISectionGroup(Direction.UP, _configSets[Direction.UP]);

                    //_configurator.InitUISectionGroup(Direction.LEFT, _configSets[Direction.LEFT]);
                    //_configurator.InitUISectionGroup(Direction.RIGHT, _configSets[Direction.RIGHT]);
                }
                //_configurator.InitUISectionGroup(Direction.BACK, _configSets[Direction.BACK]);
                //_configurator.InitUISectionGroup(Direction.FORWARD, _configSets[Direction.FORWARD]);
            }
        }

        private List<Config> LoadSettingsFromFile(string mode, string fileName)
        {
            var configs = new List<Config>();
            Persistence.LoadModeMorphSettings(_script, mode, $"{fileName}.json", (dir, json) =>
            {
                foreach(string name in json.Keys)
                {
                    configs.Add(new MorphConfig(
                        name,
                        json[name]["IsNegative"].AsBool,
                        json[name]["Multiplier1"].AsFloat,
                        json[name]["Multiplier2"].AsFloat
                    ));
                }
            });
            return configs;
        }

        public bool IsEnabled()
        {
            if(!_useConfigurator)
            {
                return true;
            }
            return _configurator.EnableAdjustment.val;
        }

        public void UpdateDebugInfo(string text)
        {
            if(!_useConfigurator)
            {
                return;
            }
            _configurator.DebugInfo.val = text;
        }

        public void Update(
            string mode,
            float roll,
            float pitch,
            float mass,
            float amount
        )
        {
            _mass = mass;
            _amount = amount;

            float smoothRoll = Calc.SmoothStep(roll);
            float smoothPitch = 2 * Calc.SmoothStep(pitch);
            _additionalRollEffect = 0.4f * Mathf.Abs(smoothRoll);

            if(mode != Mode.ANIM_OPTIMIZED)
            {
                AdjustUpDownMorphs(smoothPitch, smoothRoll);
                AdjustRollMorphs(smoothRoll);
            }
            AdjustForwardBackMorphs(smoothPitch, smoothRoll);

            string infoText =
                $"{NameValueString("Pitch", pitch, 100f, 15)} {Calc.RoundToDecimals(smoothPitch, 100f)}\n" +
                $"{NameValueString("Roll", roll, 100f, 15)} {Calc.RoundToDecimals(smoothRoll, 100f)}\n" +
                $"{_additionalRollEffect}";
            UpdateDebugInfo(infoText);
        }

        private void AdjustRollMorphs(float roll)
        {
            // left
            if(roll >= 0)
            {
                ResetMorphs(Direction.RIGHT);
                UpdateMorphs(Direction.LEFT, roll);
            }
            // right
            else
            {
                ResetMorphs(Direction.LEFT);
                UpdateMorphs(Direction.RIGHT, -roll);
            }
        }

        private void AdjustUpDownMorphs(float pitch, float roll)
        {
            // leaning forward
            if(pitch >= 0)
            {
                UpdateMorphs(Direction.UP, pitch/2, roll, _additionalRollEffect);
                UpdateMorphs(Direction.DOWN, (2 - pitch)/2, roll);
            }
            // leaning back
            else
            {
                UpdateMorphs(Direction.UP, -pitch/2, roll, _additionalRollEffect);
                UpdateMorphs(Direction.DOWN, (2 + pitch)/2, roll);
            }
        }

        private void AdjustForwardBackMorphs(float pitch, float roll)
        {
            // leaning forward
            if(pitch >= 0)
            {
                ResetMorphs(Direction.BACK);
                // upright
                if(pitch < 1)
                {
                    UpdateMorphs(Direction.FORWARD, pitch, roll);
                }
                // upside down
                else
                {
                    UpdateMorphs(Direction.FORWARD, 2 - pitch, roll);
                }
            }
            // leaning back
            else
            {
                ResetMorphs(Direction.FORWARD);
                // upright
                if(pitch >= -1)
                {
                    UpdateMorphs(Direction.BACK, -pitch, roll);
                }
                // upside down
                else
                {
                    UpdateMorphs(Direction.BACK, 2 + pitch, roll);
                }
            }
        }

        private void UpdateMorphs(string configSetName, float effect, float? roll = null, float? additional = null)
        {
            if(roll.HasValue)
            {
                effect = effect * (1 - Mathf.Abs(roll.Value));
            }
            if(additional.HasValue)
            {
                effect = effect + additional.Value;
            }
            foreach(MorphConfig config in _configSets[configSetName])
            {
                UpdateValue(config, effect);
                if(_useConfigurator)
                {
                    _configurator.UpdateValueSlider(configSetName, config.Name, config.Morph.morphValue);
                }
            }
        }

        private void UpdateValue(MorphConfig config, float effect)
        {
            float value =
                _amount * config.Multiplier1 * effect / 2 +
                _mass * config.Multiplier2 * effect / 2;

            bool inRange = config.IsNegative ? value < 0 : value > 0;
            config.Morph.morphValue = inRange ? value : 0;
        }

        public void ResetAll()
        {
            foreach(var configSet in _configSets)
            {
                ResetMorphs(configSet.Key);
            }
        }

        private void ResetMorphs(string configSetName)
        {
            foreach(MorphConfig config in _configSets[configSetName])
            {
                config.Morph.morphValue = 0;
                if(_useConfigurator)
                {
                    _configurator.UpdateValueSlider(configSetName, config.Name, 0);
                }
            }
        }
    }
}
