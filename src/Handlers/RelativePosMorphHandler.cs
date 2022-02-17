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

        private float _multiplier;
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
                { Direction.DOWN_L, LoadSettingsFromFile(mode, "downForce", " L") },
                { Direction.DOWN_R, LoadSettingsFromFile(mode, "downForce", " R") },
                { Direction.UP_L, LoadSettingsFromFile(mode, "upForce", " L") },
                { Direction.UP_R, LoadSettingsFromFile(mode, "upForce", " R") },
                { Direction.UP_C, LoadSettingsFromFile(mode, "upForceCenter") },
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
                //_configurator.InitUISectionGroup(Direction.DOWN_L, _configSets[Direction.DOWN_L]);
                //_configurator.InitUISectionGroup(Direction.DOWN_R, _configSets[Direction.DOWN_R]);
                //_configurator.InitUISectionGroup(Direction.UP_L, _configSets[Direction.UP_L]);
                //_configurator.InitUISectionGroup(Direction.UP_R, _configSets[Direction.UP_R]);
                //_configurator.InitUISectionGroup(Direction.UP_C, _configSets[Direction.UP_C]);
                //_configurator.InitUISectionGroup(Direction.BACK, _configSets[Direction.BACK]);
                //_configurator.InitUISectionGroup(Direction.FORWARD, _configSets[Direction.FORWARD]);
                //_configurator.InitUISectionGroup(Direction.LEFT_L, _configSets[Direction.LEFT_L]);
                //_configurator.InitUISectionGroup(Direction.LEFT_R, _configSets[Direction.LEFT_R]);
                //_configurator.InitUISectionGroup(Direction.RIGHT_L, _configSets[Direction.RIGHT_L]);
                //_configurator.InitUISectionGroup(Direction.RIGHT_R, _configSets[Direction.RIGHT_R]);
            }
        }

        private List<Config> LoadSettingsFromFile(string mode, string fileName, string morphNameSuffix = null)
        {
            var configs = new List<Config>();
            Persistence.LoadModeMorphSettings(_script, mode, $"{fileName}.json", (dir, json) =>
            {
                foreach(string name in json.Keys)
                {
                    string morphName = string.IsNullOrEmpty(morphNameSuffix) ? name : name + $"{morphNameSuffix}";
                    configs.Add(new MorphConfig(
                        morphName,
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
            float angleYLeft,
            float angleYRight,
            float positionDiffZRight, //todo use
            float angleXLeft,
            float angleXRight,
            float multiplier,
            float mass,
            float mobility
        )
        {
            _multiplier = multiplier;
            _mass = mass;
            _mobility = mobility;

            float effectYLeft = CalculateEffect(angleYLeft, 75);
            float effectYRight = CalculateEffect(angleYRight, 75);
            float angleYCenter = (angleYRight + angleYLeft) / 2;
            float effectYCenter = CalculateEffect(angleYCenter, 75);
            //float effectZRight = CalculateEffect(positionDiffZRight, 0.060f);
            float effectXLeft = CalculateEffect(angleXLeft, 60);
            float effectXRight = CalculateEffect(angleXRight, 60);

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

            //up force on average of left and right breast
            if(angleYCenter >= 0)
            {
                UpdateMorphs(Direction.UP_C, effectYCenter);
            }
            else
            {
                ResetMorphs(Direction.UP_C);
            }

            //TODO delete or use
            //forward
            //if(positionDiffZRight <= 0)
            //{
            //    ResetMorphs(Direction.BACK);
            //    UpdateMorphs(Direction.FORWARD, effectZRight);
            //}
            //// back
            //else
            //{
            //    ResetMorphs(Direction.FORWARD);
            //    UpdateMorphs(Direction.BACK, effectZRight);
            //}

            //left force on left breast
            if(angleXLeft >= 0)
            {
                ResetMorphs(Direction.LEFT_L);
                UpdateMorphs(Direction.RIGHT_L, effectXLeft);
            }
            //right force on left breast
            else
            {
                ResetMorphs(Direction.RIGHT_L);
                UpdateMorphs(Direction.LEFT_L, effectXLeft);
            }

            //left force on right breast
            if(angleXRight >= 0)
            {
                ResetMorphs(Direction.LEFT_R);
                UpdateMorphs(Direction.RIGHT_R, effectXRight);
            }
            //right force on right breast
            else
            {
                ResetMorphs(Direction.RIGHT_R);
                UpdateMorphs(Direction.LEFT_R, effectXRight);
            }

            string infoText =
                    $"{NameValueString("angleYLeft", angleYLeft, 1000f)} \n" +
                    $"{NameValueString("angleYRight", angleYRight, 1000f)} \n" +
                    $"{NameValueString("angleXLeft", angleXLeft, 1000f)} \n" +
                    $"{NameValueString("angleXRight", angleXRight, 1000f)} \n";
            UpdateDebugInfo(infoText);
        }

        private float CalculateEffect(float value, float max)
        {
            return Calc.RoundToDecimals(
                Mathf.InverseLerp(0, max, Mathf.Abs(value * _multiplier)),
                1000f
            );
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
