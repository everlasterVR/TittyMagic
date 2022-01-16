using System;
using SimpleJSON;
using System.Collections.Generic;
using UnityEngine;

namespace TittyMagic
{
    internal class GravityPhysicsHandler
    {
        private MVRScript _script;
        private GravityPhysicsConfigurator _configurator;

        private bool _useConfigurator;

        private float mass;
        private float gravity;

        private Dictionary<string, List<GravityPhysicsConfig>> _configSets;

        private List<GravityPhysicsConfig> _uprightConfigs;

        private List<GravityPhysicsConfig> _upsideDownConfigs;

        private List<GravityPhysicsConfig> _rollLeftConfigs;

        private List<GravityPhysicsConfig> _rollRightConfigs;

        public GravityPhysicsHandler(MVRScript script)
        {
            // Right/left angle target moves both breasts in the same direction
            Globals.BREAST_CONTROL.invertJoint2RotationY = false;

            _script = script;
            try
            {
                _configurator = (GravityPhysicsConfigurator) _script;
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
            _uprightConfigs = new List<GravityPhysicsConfig>();
            _upsideDownConfigs = new List<GravityPhysicsConfig>();
            _rollLeftConfigs = new List<GravityPhysicsConfig>();
            _rollRightConfigs = new List<GravityPhysicsConfig>();
            LoadSettingsFromFile(mode);
            _configSets = new Dictionary<string, List<GravityPhysicsConfig>>
            {
                { Direction.DOWN, _uprightConfigs },
                { Direction.UP, _upsideDownConfigs },
                { Direction.LEFT, _rollLeftConfigs },
                { Direction.RIGHT, _rollRightConfigs },
            };

            //not working properly yet when changing mode on the fly
            if(_useConfigurator)
            {
                _configurator.ResetUISectionGroups();
                _configurator.InitUISectionGroup(Direction.DOWN, _uprightConfigs);
                _configurator.InitUISectionGroup(Direction.UP, _upsideDownConfigs);
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
                    List<GravityPhysicsConfig> configs = null;
                    if(key == Direction.DOWN)
                        configs = _uprightConfigs;
                    else if(key == Direction.UP)
                        configs = _upsideDownConfigs;
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
                                groupJson[name]["IsNegative"].AsBool,
                                groupJson[name]["Multiplier1"].AsFloat,
                                groupJson[name]["Multiplier2"].AsFloat
                            ));
                        }
                    }
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

        public void Update(
            float roll,
            float pitch,
            float mass,
            float gravity
        )
        {
            this.mass = mass;
            this.gravity = gravity;

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

            // leaning forward
            if(pitch >= 0)
            {
                // upright
                if(pitch < 1)
                {
                    ResetPhysics(Direction.UP);
                    UpdatePitchPhysics(Direction.DOWN, 1 - pitch, roll);
                }
                // upside down
                else
                {
                    ResetPhysics(Direction.DOWN);
                    UpdatePitchPhysics(Direction.UP, pitch - 1, roll);
                }
            }
            // leaning back
            else
            {
                // upright
                if(pitch >= -1)
                {
                    ResetPhysics(Direction.UP);
                    UpdatePitchPhysics(Direction.DOWN, 1 + pitch, roll);
                }
                // upside down
                else
                {
                    ResetPhysics(Direction.DOWN);
                    UpdatePitchPhysics(Direction.UP, -pitch - 1, roll);
                }
            }
        }

        private void UpdateRollPhysics(string configSetName, float effect)
        {
            foreach(var config in _configSets[configSetName])
            {
                UpdateValue(config, effect, mass, gravity);
                if(_useConfigurator)
                {
                    _configurator.UpdateValueSlider(configSetName, config.Name, config.Setting.val);
                }
            }
        }

        private void UpdatePitchPhysics(string configSetName, float effect, float roll)
        {
            float adjusted = effect * (1 - Mathf.Abs(roll));
            foreach(var config in _configSets[configSetName])
            {
                UpdateValue(config, adjusted, mass, gravity);
                if(_useConfigurator)
                {
                    _configurator.UpdateValueSlider(configSetName, config.Name, config.Setting.val);
                }
            }
        }

        private void UpdateValue(GravityPhysicsConfig config, float effect, float mass, float gravity)
        {
            float value =
                gravity * config.Multiplier1 * effect / 2 +
                mass * config.Multiplier2 * effect / 2;

            bool inRange = config.IsNegative ? value < 0 : value > 0;
            config.Setting.val = inRange ? value : 0;
        }

        public void ResetAll()
        {
            ResetPhysics(Direction.DOWN);
            ResetPhysics(Direction.UP);
            ResetPhysics(Direction.LEFT);
            ResetPhysics(Direction.RIGHT);
        }

        private void ResetPhysics(string configSetName)
        {
            foreach(var config in _configSets[configSetName])
            {
                config.Setting.val = config.OriginalValue;
                if(_useConfigurator)
                {
                    _configurator.UpdateValueSlider(configSetName, config.Name, config.OriginalValue);
                }
            }
        }
    }
}
