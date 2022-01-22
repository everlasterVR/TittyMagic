using System;
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

        private List<Config> _uprightConfigs;

        private List<Config> _upsideDownConfigs;

        private List<Config> _leanBackConfigs;

        private List<Config> _leanForwardConfigs;

        private List<Config> _rollLeftConfigs;

        private List<Config> _rollRightConfigs;

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
                _uprightConfigs = new List<Config>();
                _upsideDownConfigs = new List<Config>();
                LoadSettingsFromFile(mode, "upright", _uprightConfigs);
                LoadSettingsFromFile(mode, "upsideDown", _upsideDownConfigs);
                _configSets.Add(Direction.DOWN, _uprightConfigs);
                _configSets.Add(Direction.UP, _upsideDownConfigs);
            }
            _leanBackConfigs = new List<Config>();
            _leanForwardConfigs = new List<Config>();
            LoadSettingsFromFile(mode, "leanBack", _leanBackConfigs);
            LoadSettingsFromFile(mode, "leanForward", _leanForwardConfigs);
            _configSets.Add(Direction.BACK, _leanBackConfigs);
            _configSets.Add(Direction.FORWARD, _leanForwardConfigs);

            _rollLeftConfigs = new List<Config>();
            _rollRightConfigs = new List<Config>();
            LoadSettingsFromFile(mode, "rollLeft", _rollLeftConfigs);
            LoadSettingsFromFile(mode, "rollRight", _rollRightConfigs);
            _configSets.Add(Direction.LEFT, _rollLeftConfigs);
            _configSets.Add(Direction.RIGHT, _rollRightConfigs);

            //not working properly yet when changing mode on the fly
            if(_useConfigurator)
            {
                _configurator.ResetUISectionGroups();
                if(mode != Mode.ANIM_OPTIMIZED)
                {
                    _configurator.InitUISectionGroup(Direction.DOWN, _uprightConfigs);
                    _configurator.InitUISectionGroup(Direction.UP, _upsideDownConfigs);
                }
                _configurator.InitUISectionGroup(Direction.BACK, _leanBackConfigs);
                _configurator.InitUISectionGroup(Direction.FORWARD, _leanForwardConfigs);

                _configurator.InitUISectionGroup(Direction.LEFT, _rollLeftConfigs);
                _configurator.InitUISectionGroup(Direction.RIGHT, _rollRightConfigs);
            }
        }

        private void LoadSettingsFromFile(string mode, string fileName, List<Config> configs)
        {
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
            }
            AdjustForwardBackMorphs(smoothPitch, smoothRoll);
            AdjustRollMorphs(smoothRoll);

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
            //foreach(var it in gravityOffsetMorphs)
            //    it.Reset();
            ResetMorphs(Direction.DOWN);
            ResetMorphs(Direction.UP);
            ResetMorphs(Direction.BACK);
            ResetMorphs(Direction.FORWARD);
            ResetMorphs(Direction.LEFT);
            ResetMorphs(Direction.RIGHT);
        }

        private void ResetMorphs(string configSetName)
        {
            if(!_configSets.ContainsKey(configSetName))
            {
                return;
            }

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
