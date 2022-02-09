using System;
using System.Collections.Generic;
using UnityEngine;
using static TittyMagic.Utils;

namespace TittyMagic
{
    internal class RelativePosMorphHandler
    {
        private MVRScript _script;
        private IConfigurator _configurator;

        private bool _useConfigurator;

        private float _mass;
        private float _mobility;

        private Dictionary<string, List<Config>> _configSets;

        private List<Config> _downForceConfigs;

        private List<Config> _upForceConfigs;

        //private List<Config> _backForceConfigs;

        //private List<Config> _forwardForceConfigs;

        private List<Config> _leftForceConfigs;

        private List<Config> _rightForceConfigs;

        public RelativePosMorphHandler(MVRScript script)
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
            _downForceConfigs = new List<Config>();
            _upForceConfigs = new List<Config>();
            //_backForceConfigs = new List<Config>();
            //_forwardForceConfigs = new List<Config>();
            _leftForceConfigs = new List<Config>();
            _rightForceConfigs = new List<Config>();
            LoadSettingsFromFile(mode, "downForce", _downForceConfigs);
            LoadSettingsFromFile(mode, "upForce", _upForceConfigs);
            //LoadSettingsFromFile(mode, "backForce", _backForceConfigs);
            //LoadSettingsFromFile(mode, "forwardForce", _forwardForceConfigs);
            LoadSettingsFromFile(mode, "leftForce", _leftForceConfigs);
            LoadSettingsFromFile(mode, "rightForce", _rightForceConfigs);
            _configSets = new Dictionary<string, List<Config>>
            {
                { Direction.DOWN, _downForceConfigs },
                { Direction.UP, _upForceConfigs },
                //{ Direction.BACK, _backForceConfigs },
                //{ Direction.FORWARD, _forwardForceConfigs },
                { Direction.LEFT, _leftForceConfigs },
                { Direction.RIGHT, _rightForceConfigs },
            };

            //not working properly yet when changing mode on the fly
            if(_useConfigurator)
            {
                _configurator.ResetUISectionGroups();
                //_configurator.InitUISectionGroup(Direction.DOWN, _downForceConfigs);
                //_configurator.InitUISectionGroup(Direction.UP, _upForceConfigs);
                //_configurator.InitUISectionGroup(Direction.BACK, _backForceConfigs);
                //_configurator.InitUISectionGroup(Direction.FORWARD, _forwardForceConfigs);
                //_configurator.InitUISectionGroup(Direction.LEFT, _leftForceConfigs);
                //_configurator.InitUISectionGroup(Direction.RIGHT, _rightForceConfigs);
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
            float scaledAngleY,
            float positionDiffZ,
            float scaledAngleX,
            float mass,
            float mobility
        )
        {
            _mass = mass;
            _mobility = mobility;

            float effectY = Calc.RoundToDecimals(Mathf.InverseLerp(0, 75, Mathf.Abs(scaledAngleY)), 1000f);
            //float effectZ = Calc.RoundToDecimals(Mathf.InverseLerp(0, 0.060f, Mathf.Abs(positionDiffZ)), 1000f);
            float effectX = Calc.RoundToDecimals(Mathf.InverseLerp(0, 60, Mathf.Abs(scaledAngleX)), 1000f);

            // up
            if(scaledAngleY >= 0)
            {
                ResetMorphs(Direction.DOWN);
                UpdateMorphs(Direction.UP, effectY);
            }
            // down
            else
            {
                ResetMorphs(Direction.UP);
                UpdateMorphs(Direction.DOWN, effectY);
            }

            //TODO delete or use
            //forward
            //if(positionDiffZ <= 0)
            //{
            //    ResetMorphs(Direction.BACK);
            //    UpdateMorphs(Direction.FORWARD, effectZ);
            //}
            //// back
            //else
            //{
            //    ResetMorphs(Direction.FORWARD);
            //    UpdateMorphs(Direction.BACK, effectZ);
            //}

            //left
            if(scaledAngleX >= 0)
            {
                ResetMorphs(Direction.LEFT);
                UpdateMorphs(Direction.RIGHT, effectX);
            }
            // right
            else
            {
                ResetMorphs(Direction.RIGHT);
                UpdateMorphs(Direction.LEFT, effectX);
            }

            string infoText =
                    $"{NameValueString("scaledAngleY", scaledAngleY, 1000f)} \n" +
                    $"{NameValueString("effectY", effectY, 1000f)} \n" +
                    $"{NameValueString("scaledAngleX", scaledAngleX, 1000f)} \n" +
                    $"{NameValueString("effectX", effectX, 1000f)} \n" +
                    $"";
            UpdateDebugInfo(infoText);
        }

        private void UpdateMorphs(string configSetName, float effect)
        {
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
                _mobility * config.Multiplier1 * effect / 2 +
                _mass * config.Multiplier2 * effect / 2;

            bool inRange = config.IsNegative ? value < 0 : value > 0;
            config.Morph.morphValue = inRange ? value : 0;
        }

        public void ResetAll()
        {
            ResetMorphs(Direction.DOWN);
            ResetMorphs(Direction.UP);
            //ResetMorphs(Direction.BACK);
            //ResetMorphs(Direction.FORWARD);
            ResetMorphs(Direction.LEFT);
            ResetMorphs(Direction.RIGHT);
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
