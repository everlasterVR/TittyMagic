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
            _configSets = new Dictionary<string, List<Config>>
            {
                { Direction.DOWN, LoadSettingsFromFile(mode, "downForce") },
                { Direction.UP, LoadSettingsFromFile(mode, "upForce") },
                //{ Direction.BACK, LoadSettingsFromFile(mode, "backForce") },
                //{ Direction.FORWARD, LoadSettingsFromFile(mode, "forwardForce") },
                { Direction.LEFT_L, LoadSettingsFromFile(mode, "leftForceL") },
                { Direction.LEFT_R, LoadSettingsFromFile(mode, "leftForceR") },
                { Direction.RIGHT_L, LoadSettingsFromFile(mode, "rightForceL") },
                { Direction.RIGHT_R, LoadSettingsFromFile(mode, "rightForceR") },
            };

            //not working properly yet when changing mode on the fly
            if(_useConfigurator)
            {
                _configurator.ResetUISectionGroups();
                //_configurator.InitUISectionGroup(Direction.DOWN, _configSets[Direction.DOWN]);
                //_configurator.InitUISectionGroup(Direction.UP, _configSets[Direction.UP]);
                //_configurator.InitUISectionGroup(Direction.BACK, _configSets[Direction.BACK]);
                //_configurator.InitUISectionGroup(Direction.FORWARD, _configSets[Direction.FORWARD]);
                //_configurator.InitUISectionGroup(Direction.LEFT_L, _configSets[Direction.LEFT_L]);
                //_configurator.InitUISectionGroup(Direction.LEFT_R, _configSets[Direction.LEFT_R]);
                //_configurator.InitUISectionGroup(Direction.RIGHT_L, _configSets[Direction.RIGHT_L]);
                //_configurator.InitUISectionGroup(Direction.RIGHT_R, _configSets[Direction.RIGHT_R]);
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
                ResetMorphs(Direction.LEFT_L);
                ResetMorphs(Direction.LEFT_R);
                UpdateMorphs(Direction.RIGHT_L, effectX);
                UpdateMorphs(Direction.RIGHT_R, effectX);
            }
            // right
            else
            {
                ResetMorphs(Direction.RIGHT_L);
                ResetMorphs(Direction.RIGHT_R);
                UpdateMorphs(Direction.LEFT_L, effectX);
                UpdateMorphs(Direction.LEFT_R, effectX);
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
