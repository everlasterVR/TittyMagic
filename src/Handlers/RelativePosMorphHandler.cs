using System;
using System.Collections.Generic;
using UnityEngine;
using static TittyMagic.Utils;

namespace TittyMagic
{
    internal class RelativePosMorphHandler
    {
        private MVRScript _script;
        private RelativePosMorphConfigurator _configurator;

        private bool _useConfigurator;

        private float mass;
        private float softness;

        private Dictionary<string, List<MorphConfig>> _configSets;

        private List<MorphConfig> _downForceConfigs;

        private List<MorphConfig> _upForceConfigs;

        private List<MorphConfig> _backForceConfigs;

        private List<MorphConfig> _forwardForceConfigs;

        private List<MorphConfig> _leftForceConfigs;

        private List<MorphConfig> _rightForceConfigs;

        public RelativePosMorphHandler(MVRScript script)
        {
            _script = script;

            try
            {
                _configurator = (RelativePosMorphConfigurator) _script;
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
            _downForceConfigs = new List<MorphConfig>();
            _upForceConfigs = new List<MorphConfig>();
            _backForceConfigs = new List<MorphConfig>();
            _forwardForceConfigs = new List<MorphConfig>();
            _leftForceConfigs = new List<MorphConfig>();
            _rightForceConfigs = new List<MorphConfig>();
            LoadSettingsFromFile(mode, "downForce", _downForceConfigs);
            LoadSettingsFromFile(mode, "upForce", _upForceConfigs);
            LoadSettingsFromFile(mode, "backForce", _backForceConfigs);
            LoadSettingsFromFile(mode, "forwardForce", _forwardForceConfigs);
            LoadSettingsFromFile(mode, "leftForce", _leftForceConfigs);
            LoadSettingsFromFile(mode, "rightForce", _rightForceConfigs);
            _configSets = new Dictionary<string, List<MorphConfig>>
            {
                { Direction.DOWN, _downForceConfigs },
                { Direction.UP, _upForceConfigs },
                { Direction.BACK, _backForceConfigs },
                { Direction.FORWARD, _forwardForceConfigs },
                { Direction.LEFT, _leftForceConfigs },
                { Direction.RIGHT, _rightForceConfigs },
            };

            //not working properly yet when changing mode on the fly
            if(_useConfigurator)
            {
                _configurator.ResetUISectionGroups();
                _configurator.InitUISectionGroup(Direction.DOWN, _downForceConfigs);
                _configurator.InitUISectionGroup(Direction.UP, _upForceConfigs);
                _configurator.InitUISectionGroup(Direction.BACK, _backForceConfigs);
                _configurator.InitUISectionGroup(Direction.FORWARD, _forwardForceConfigs);
                _configurator.InitUISectionGroup(Direction.LEFT, _leftForceConfigs);
                _configurator.InitUISectionGroup(Direction.RIGHT, _rightForceConfigs);
            }
        }

        private void LoadSettingsFromFile(string mode, string fileName, List<MorphConfig> configs)
        {
            Persistence.LoadModeMorphSettings(_script, mode, $"{fileName}.json", (dir, json) =>
            {
                foreach(string name in json.Keys)
                {
                    configs.Add(new MorphConfig(name)
                    {
                        Multiplier1 = json[name]["Multiplier1"].AsFloat,
                        Multiplier2 = json[name]["Multiplier2"].AsFloat
                    });
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
            Vector3 positionDiff,
            float mass,
            float softness
        )
        {
            this.mass = mass;
            this.softness = softness;
            float x = positionDiff.x;
            float y = positionDiff.y;
            float z = positionDiff.z;

            // TODO separate l/r morphs only, separate calculation of diff
            //left
            if(x <= 0)
            {
                ResetMorphs(Direction.LEFT);
                UpdateMorphs(Direction.RIGHT, -x);
            }
            // right
            else
            {
                ResetMorphs(Direction.RIGHT);
                UpdateMorphs(Direction.LEFT, x);
            }

            // up
            if(y <= 0)
            {
                ResetMorphs(Direction.DOWN);
                UpdateMorphs(Direction.UP, -y);
            }
            // down
            else
            {
                ResetMorphs(Direction.UP);
                UpdateMorphs(Direction.DOWN, y);
            }

            // forward
            if(z <= 0)
            {
                ResetMorphs(Direction.BACK);
                UpdateMorphs(Direction.FORWARD, -z);
            }
            // back
            else
            {
                ResetMorphs(Direction.FORWARD);
                UpdateMorphs(Direction.BACK, z);
            }
        }

        private void UpdateMorphs(string configSetName, float diff)
        {
            float cubeRt = Mathf.Pow(diff, 1/3f);
            float diffVal = Calc.InverseSmoothStep(1, cubeRt, 0.15f, 0.5f);
            foreach(var config in _configSets[configSetName])
            {
                UpdateValue(config, diffVal, mass, softness);
                if(_useConfigurator)
                {
                    _configurator.UpdateValueSlider(configSetName, config.Name, config.Morph.morphValue);
                }
            }
        }

        private void UpdateValue(MorphConfig config, float effect, float mass, float softness)
        {
            config.Morph.morphValue =
                softness * config.Multiplier1 * effect / 2 +
                mass* config.Multiplier2 * effect / 2;
        }

        public void ResetAll()
        {
            ResetMorphs(Direction.DOWN);
            ResetMorphs(Direction.UP);
            ResetMorphs(Direction.BACK);
            ResetMorphs(Direction.FORWARD);
            ResetMorphs(Direction.LEFT);
            ResetMorphs(Direction.RIGHT);
        }

        private void ResetMorphs(string configSetName)
        {
            foreach(var config in _configSets[configSetName])
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
