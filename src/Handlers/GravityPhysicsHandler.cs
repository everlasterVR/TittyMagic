using System;
using SimpleJSON;
using System.Collections.Generic;
using UnityEngine;

namespace TittyMagic
{
    internal class GravityPhysicsHandler
    {
        private MVRScript _script;
        private IConfigurator _configurator;

        private bool _useConfigurator;

        private float _mass;
        private float _amount;

        private Dictionary<string, List<Config>> _configSets;

        private List<Config> _uprightConfigs;

        private List<Config> _upsideDownConfigs;

        private List<Config> _leanBackConfigs;

        private List<Config> _leanForwardConfigs;

        private List<Config> _rollLeftConfigs;

        private List<Config> _rollRightConfigs;

        public GravityPhysicsHandler(MVRScript script)
        {
            // Right/left angle target moves both breasts in the same direction
            Globals.BREAST_CONTROL.invertJoint2RotationY = false;

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
            _uprightConfigs = new List<Config>();
            _upsideDownConfigs = new List<Config>();
            _leanBackConfigs = new List<Config>();
            _leanForwardConfigs = new List<Config>();
            _rollLeftConfigs = new List<Config>();
            _rollRightConfigs = new List<Config>();
            LoadSettingsFromFile(mode);
            _configSets = new Dictionary<string, List<Config>>
            {
                { Direction.DOWN, _uprightConfigs },
                { Direction.UP, _upsideDownConfigs },
                { Direction.BACK, _leanBackConfigs },
                { Direction.FORWARD, _leanForwardConfigs },
                { Direction.LEFT, _rollLeftConfigs },
                { Direction.RIGHT, _rollRightConfigs },
            };

            //not working properly yet when changing mode on the fly
            if(_useConfigurator)
            {
                _configurator.ResetUISectionGroups();
                _configurator.InitUISectionGroup(Direction.DOWN, _uprightConfigs);
                _configurator.InitUISectionGroup(Direction.UP, _upsideDownConfigs);
                _configurator.InitUISectionGroup(Direction.BACK, _leanBackConfigs);
                _configurator.InitUISectionGroup(Direction.FORWARD, _leanForwardConfigs);
                _configurator.InitUISectionGroup(Direction.LEFT, _rollLeftConfigs);
                _configurator.InitUISectionGroup(Direction.RIGHT, _rollRightConfigs);
                _configurator.AddButtonListeners();
            }
        }

        private void LoadSettingsFromFile(string mode)
        {
            Persistence.LoadModeGravityPhysicsSettings(_script, mode, (dir, json) =>
            {
                foreach(string key in json.Keys)
                {
                    List<Config> configs = null;
                    if(key == Direction.DOWN)
                        configs = _uprightConfigs;
                    else if(key == Direction.UP)
                        configs = _upsideDownConfigs;
                    else if(key == Direction.BACK)
                        configs = _leanBackConfigs;
                    else if(key == Direction.FORWARD)
                        configs = _leanForwardConfigs;
                    else if(key == Direction.LEFT)
                        configs = _rollLeftConfigs;
                    else if(key == Direction.RIGHT)
                        configs = _rollRightConfigs;
                    if(configs != null)
                    {
                        JSONClass groupJson = json[key].AsObject;
                        foreach(string name in groupJson.Keys)
                        {
                            configs.Add(new GravityPhysicsConfig(
                                name,
                                groupJson[name]["Type"],
                                groupJson[name]["IsNegative"].AsBool,
                                groupJson[name]["Multiplier1"].AsFloat,
                                groupJson[name]["Multiplier2"].AsFloat
                            ));
                        }
                    }
                }
            });
        }

        public void SetBaseValues()
        {
            foreach(var kvp in _configSets)
            {
                foreach(GravityPhysicsConfig config in kvp.Value)
                {
                    if(config.Type == "additive")
                    {
                        config.BaseValue = config.Setting.val;
                    }
                }
            }
        }

        public bool IsEnabled()
        {
            if(!_useConfigurator)
            {
                return true;
            }
            return _configurator.EnableAdjustment.val;
        }

        public void Update(
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

            AdjustRollPhysics(smoothRoll);
            AdjustPitchPhysics(smoothPitch, smoothRoll);
        }

        private void AdjustRollPhysics(float roll)
        {
            // left
            if(roll >= 0)
            {
                ResetPhysics(Direction.RIGHT);
                UpdateRollPhysics(Direction.LEFT, roll);
            }
            // right
            else
            {
                ResetPhysics(Direction.LEFT);
                UpdateRollPhysics(Direction.RIGHT, -roll);
            }
        }

        private void AdjustPitchPhysics(float pitch, float roll)
        {
            // leaning forward
            if(pitch >= 0)
            {
                ResetPhysics(Direction.BACK);
                // upright
                if(pitch < 1)
                {
                    ResetPhysics(Direction.UP);
                    UpdatePitchPhysics(Direction.DOWN, 1 - pitch, roll);
                    UpdatePitchPhysics(Direction.FORWARD, pitch, roll);
                }
                // upside down
                else
                {
                    ResetPhysics(Direction.DOWN);
                    UpdatePitchPhysics(Direction.UP, pitch - 1, roll);
                    UpdatePitchPhysics(Direction.FORWARD, 2 - pitch, roll);
                }
            }
            // leaning back
            else
            {
                ResetPhysics(Direction.FORWARD);
                // upright
                if(pitch >= -1)
                {
                    ResetPhysics(Direction.UP);
                    UpdatePitchPhysics(Direction.DOWN, 1 + pitch, roll);
                    UpdatePitchPhysics(Direction.BACK, -pitch, roll);
                }
                // upside down
                else
                {
                    ResetPhysics(Direction.DOWN);
                    UpdatePitchPhysics(Direction.UP, -pitch - 1, roll);
                    UpdatePitchPhysics(Direction.BACK, 2 + pitch, roll);
                }
            }
        }

        private void UpdateRollPhysics(string configSetName, float effect)
        {
            foreach(GravityPhysicsConfig config in _configSets[configSetName])
            {
                UpdateValue(config, effect);
                if(_useConfigurator)
                {
                    _configurator.UpdateValueSlider(configSetName, config.Name, config.Setting.val);
                }
            }
        }

        private void UpdatePitchPhysics(string configSetName, float effect, float roll)
        {
            float adjusted = effect * (1 - Mathf.Abs(roll));
            foreach(GravityPhysicsConfig config in _configSets[configSetName])
            {
                UpdateValue(config, adjusted);
                if(_useConfigurator)
                {
                    _configurator.UpdateValueSlider(configSetName, config.Name, config.Setting.val);
                }
            }
        }

        private void UpdateValue(GravityPhysicsConfig config, float effect)
        {
            float value =
                _amount * config.Multiplier1 * effect / 2 +
                _mass * config.Multiplier2 * effect / 2;

            bool inRange = config.IsNegative ? value < 0 : value > 0;

            if(config.Type == "direct")
            {
                config.Setting.val = inRange ? value : 0;
            }
            else if(config.Type == "additive")
            {
                config.Setting.val = inRange ? config.BaseValue + value : config.BaseValue;
            }
        }

        public void ZeroAll()
        {
            ZeroPhysics(Direction.DOWN);
            ZeroPhysics(Direction.UP);
            ZeroPhysics(Direction.BACK);
            ZeroPhysics(Direction.FORWARD);
            ZeroPhysics(Direction.LEFT);
            ZeroPhysics(Direction.RIGHT);
        }

        public void ResetAll()
        {
            ResetPhysics(Direction.DOWN);
            ResetPhysics(Direction.UP);
            ResetPhysics(Direction.BACK);
            ResetPhysics(Direction.FORWARD);
            ResetPhysics(Direction.LEFT);
            ResetPhysics(Direction.RIGHT);
        }

        private void ZeroPhysics(string configSetName)
        {
            foreach(GravityPhysicsConfig config in _configSets[configSetName])
            {
                if(config.Type == "additive")
                {
                    return;
                }

                float newValue = 0f;
                config.Setting.val = newValue;
                if(_useConfigurator)
                {
                    _configurator.UpdateValueSlider(configSetName, config.Name, newValue);
                }
            }
        }

        private void ResetPhysics(string configSetName)
        {
            foreach(GravityPhysicsConfig config in _configSets[configSetName])
            {
                float newValue = config.Type == "additive" ? config.BaseValue : config.OriginalValue;
                config.Setting.val = newValue;
                if(_useConfigurator)
                {
                    _configurator.UpdateValueSlider(configSetName, config.Name, newValue);
                }
            }
        }
    }
}
