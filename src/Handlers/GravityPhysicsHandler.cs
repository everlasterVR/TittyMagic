using System;
using System.Collections.Generic;
using UnityEngine;

namespace TittyMagic
{
    internal class GravityPhysicsHandler
    {
        private readonly MVRScript _script;
        private readonly IConfigurator _configurator;
        private readonly bool _useConfigurator;

        private float _mass;
        private float _amount;

        private Dictionary<string, List<Config>> _configSets;

        public GravityPhysicsHandler(MVRScript script)
        {
            Globals.BREAST_CONTROL.invertJoint2RotationY = false;

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
            _configSets = LoadSettingsFromFile(mode);

            // not working properly yet when changing mode on the fly
            if(_useConfigurator)
            {
                _configurator.ResetUISectionGroups();
                // _configurator.InitUISectionGroup(Direction.DOWN, _configSets[Direction.DOWN]);
                // _configurator.InitUISectionGroup(Direction.UP, _configSets[Direction.UP]);
                // _configurator.InitUISectionGroup(Direction.BACK, _configSets[Direction.BACK]);
                // _configurator.InitUISectionGroup(Direction.FORWARD, _configSets[Direction.FORWARD]);
                // _configurator.InitUISectionGroup(Direction.LEFT, _configSets[Direction.LEFT]);
                // _configurator.InitUISectionGroup(Direction.RIGHT, _configSets[Direction.RIGHT]);
                _configurator.AddButtonListeners();
            }
        }

        private Dictionary<string, List<Config>> LoadSettingsFromFile(string mode)
        {
            var configSets = new Dictionary<string, List<Config>>();
            Persistence.LoadModePhysicsSettings(
                _script,
                mode,
                (dir, json) =>
                {
                    foreach(string direction in json.Keys)
                    {
                        var configs = new List<Config>();
                        var groupJson = json[direction].AsObject;
                        foreach(string name in groupJson.Keys)
                        {
                            configs.Add(
                                new PhysicsConfig(
                                    name,
                                    groupJson[name]["Category"],
                                    groupJson[name]["Type"],
                                    groupJson[name]["IsNegative"].AsBool,
                                    groupJson[name]["Multiplier1"].AsFloat,
                                    groupJson[name]["Multiplier2"].AsFloat
                                )
                            );
                        }

                        configSets[direction] = configs;
                    }
                }
            );
            return configSets;
        }

        public void SetBaseValues()
        {
            foreach(var kvp in _configSets)
            {
                foreach(var config in kvp.Value)
                {
                    var gravityPhysicsConfig = (PhysicsConfig) config;
                    if(gravityPhysicsConfig.type == "additive")
                    {
                        gravityPhysicsConfig.baseValue = gravityPhysicsConfig.setting.val;
                    }
                }
            }
        }

        public bool IsEnabled()
        {
            return !_useConfigurator || _configurator.enableAdjustment.val;
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
            foreach(var config in _configSets[configSetName])
            {
                var gravityPhysicsConfig = (PhysicsConfig) config;
                UpdateValue(gravityPhysicsConfig, effect);
                if(_useConfigurator)
                {
                    _configurator.UpdateValueSlider(configSetName, gravityPhysicsConfig.name, gravityPhysicsConfig.setting.val);
                }
            }
        }

        private void UpdatePitchPhysics(string configSetName, float effect, float roll)
        {
            float adjusted = effect * (1 - Mathf.Abs(roll));
            foreach(var config in _configSets[configSetName])
            {
                var gravityPhysicsConfig = (PhysicsConfig) config;
                UpdateValue(gravityPhysicsConfig, adjusted);
                if(_useConfigurator)
                {
                    _configurator.UpdateValueSlider(configSetName, gravityPhysicsConfig.name, gravityPhysicsConfig.setting.val);
                }
            }
        }

        private void UpdateValue(PhysicsConfig config, float effect)
        {
            float value =
                (_amount * config.multiplier1 * effect / 2) +
                (_mass * config.multiplier2 * effect / 2);

            bool inRange = config.isNegative ? value < 0 : value > 0;

            if(config.type == "direct")
            {
                config.setting.val = inRange ? value : 0;
            }
            else if(config.type == "additive")
            {
                config.setting.val = inRange ? config.baseValue + value : config.baseValue;
            }
        }

        public void ResetAll()
        {
            _configSets?.Keys.ToList().ForEach(ResetPhysics);
        }

        private void ResetPhysics(string configSetName)
        {
            foreach(var config in _configSets[configSetName])
            {
                var gravityPhysicsConfig = (PhysicsConfig) config;
                float newValue = gravityPhysicsConfig.type == "additive" ? gravityPhysicsConfig.baseValue : gravityPhysicsConfig.originalValue;
                gravityPhysicsConfig.setting.val = newValue;
                if(_useConfigurator)
                {
                    _configurator.UpdateValueSlider(configSetName, gravityPhysicsConfig.name, newValue);
                }
            }
        }
    }
}
