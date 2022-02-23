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

        public GravityPhysicsHandler(MVRScript script)
        {
            SetInvertJoint2RotationY(false);

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
            _configSets = LoadSettingsFromFile(mode);

            //not working properly yet when changing mode on the fly
            if(_useConfigurator)
            {
                _configurator.ResetUISectionGroups();
                //_configurator.InitUISectionGroup(Direction.DOWN, _configSets[Direction.DOWN]);
                //_configurator.InitUISectionGroup(Direction.UP, _configSets[Direction.UP]);
                //_configurator.InitUISectionGroup(Direction.BACK, _configSets[Direction.BACK]);
                //_configurator.InitUISectionGroup(Direction.FORWARD, _configSets[Direction.FORWARD]);
                //_configurator.InitUISectionGroup(Direction.LEFT, _configSets[Direction.LEFT]);
                //_configurator.InitUISectionGroup(Direction.RIGHT, _configSets[Direction.RIGHT]);
                _configurator.AddButtonListeners();
            }
        }

        private Dictionary<string, List<Config>> LoadSettingsFromFile(string mode)
        {
            var configSets = new Dictionary<string, List<Config>>();
            Persistence.LoadModeGravityPhysicsSettings(_script, mode, (dir, json) =>
            {
                foreach(string direction in json.Keys)
                {
                    var configs = new List<Config>();
                    JSONClass groupJson = json[direction].AsObject;
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
                    configSets[direction] = configs;
                }
            });
            return configSets;
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
            _configSets?.Keys.ToList().ForEach(key => ZeroPhysics(key));
        }

        public void ResetAll()
        {
            _configSets?.Keys.ToList().ForEach(key => ResetPhysics(key));
        }

        public void SetInvertJoint2RotationY(bool value)
        {
            // false: Right/left angle target moves both breasts in the same direction
            Globals.BREAST_CONTROL.invertJoint2RotationY = value;
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
