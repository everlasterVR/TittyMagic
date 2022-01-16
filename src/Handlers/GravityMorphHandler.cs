using System;
using System.Collections.Generic;
using UnityEngine;
using static TittyMagic.Utils;

namespace TittyMagic
{
    internal class GravityMorphHandler
    {
        private MVRScript _script;
        private GravityMorphConfigurator _configurator;

        private bool _useConfigurator;

        private float mass;
        private float gravity;

        private Dictionary<string, List<MorphConfig>> _configSets;

        //private List<MorphConfig> _gravityOffsetMorphs = new List<MorphConfig>
        //{
        //    { new MorphConfig("TM_UprightSmootherOffset") },
        //    { new MorphConfig("UPR_Breast Under Smoother1") },
        //    { new MorphConfig("UPR_Breast Under Smoother3") },
        //    { new MorphConfig("UPR_Breast Under Smoother4") },
        //};

        private List<MorphConfig> _uprightConfigs;

        private List<MorphConfig> _upsideDownConfigs;

        private List<MorphConfig> _leanBackConfigs;

        private List<MorphConfig> _leanForwardConfigs;

        private List<MorphConfig> _rollLeftConfigs;

        private List<MorphConfig> _rollRightConfigs;

        public GravityMorphHandler(MVRScript script)
        {
            _script = script;
            try
            {
                _configurator = (GravityMorphConfigurator) _script;
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
            _configSets = new Dictionary<string, List<MorphConfig>>();

            if(mode != Mode.ANIM_OPTIMIZED)
            {
                _uprightConfigs = new List<MorphConfig>();
                _upsideDownConfigs = new List<MorphConfig>();
                LoadSettingsFromFile(mode, "upright", _uprightConfigs);
                LoadSettingsFromFile(mode, "upsideDown", _upsideDownConfigs);
                _configSets.Add(Direction.DOWN, _uprightConfigs);
                _configSets.Add(Direction.UP, _upsideDownConfigs);
            }

            _leanBackConfigs = new List<MorphConfig>();
            _leanForwardConfigs = new List<MorphConfig>();
            LoadSettingsFromFile(mode, "leanBack", _leanBackConfigs);
            LoadSettingsFromFile(mode, "leanForward", _leanForwardConfigs);
            _configSets.Add(Direction.BACK, _leanBackConfigs);
            _configSets.Add(Direction.FORWARD, _leanForwardConfigs);

            _rollLeftConfigs = new List<MorphConfig>();
            _rollRightConfigs = new List<MorphConfig>();
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

        private void LoadSettingsFromFile(string mode, string fileName, List<MorphConfig> configs)
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
            float roll,
            float pitch,
            float mass,
            float gravity
        )
        {
            this.mass = mass;
            this.gravity = gravity;

            //foreach(var it in gravityOffsetMorphs)
            //{
            //    it.UpdateVal();
            //}
            //scaling reduces the effect the smaller the breast

            AdjustMorphsForRoll(roll);
            AdjustMorphsForPitch(pitch, roll);

            string infoText =
                $"{NameValueString("Pitch", pitch, 100f, 15)}\n" +
                $"{NameValueString("Roll", roll, 100f, 15)}\n" +
                $"";
            UpdateDebugInfo(infoText);
        }

        public void UpdateRoll(
            float roll,
            float mass,
            float gravity
        )
        {
            this.mass = mass;
            this.gravity = gravity;

            AdjustMorphsForRoll(roll);

            string infoText =
                $"{NameValueString("Roll", roll, 100f, 15)}\n" +
                $"";
            UpdateDebugInfo(infoText);
        }

        private void AdjustMorphsForRoll(float roll)
        {
            // left
            if(roll >= 0)
            {
                ResetMorphs(Direction.RIGHT);
                UpdateRollMorphs(Direction.LEFT, roll);
            }
            // right
            else
            {
                ResetMorphs(Direction.LEFT);
                UpdateRollMorphs(Direction.RIGHT, -roll);
            }
        }

        private void AdjustMorphsForPitch(float pitch, float roll)
        {
            // leaning forward
            if(pitch >= 0)
            {
                ResetMorphs(Direction.BACK);
                // upright
                if(pitch < 1)
                {
                    ResetMorphs(Direction.UP);
                    UpdatePitchMorphs(Direction.FORWARD, pitch, roll);
                    UpdatePitchMorphs(Direction.DOWN, 1 - pitch, roll);
                }
                // upside down
                else
                {
                    ResetMorphs(Direction.DOWN);
                    UpdatePitchMorphs(Direction.FORWARD, 2 - pitch, roll);
                    UpdatePitchMorphs(Direction.UP, pitch - 1, roll);
                }
            }
            // leaning back
            else
            {
                ResetMorphs(Direction.FORWARD);
                // upright
                if(pitch >= -1)
                {
                    ResetMorphs(Direction.UP);
                    UpdatePitchMorphs(Direction.BACK, -pitch, roll);
                    UpdatePitchMorphs(Direction.DOWN, 1 + pitch, roll);
                }
                // upside down
                else
                {
                    ResetMorphs(Direction.DOWN);
                    UpdatePitchMorphs(Direction.BACK, 2 + pitch, roll);
                    UpdatePitchMorphs(Direction.UP, -pitch - 1, roll);
                }
            }
        }

        private void UpdateRollMorphs(string configSetName, float effect)
        {
            foreach(var config in _configSets[configSetName])
            {
                UpdateValue(config, effect, mass, gravity);
                if(_useConfigurator)
                {
                    _configurator.UpdateValueSlider(configSetName, config.Name, config.Morph.morphValue);
                }
            }
        }

        private void UpdatePitchMorphs(string configSetName, float effect, float roll)
        {
            float adjusted = effect * (1 - Mathf.Abs(roll));
            foreach(var config in _configSets[configSetName])
            {
                UpdateValue(config, adjusted, mass, gravity);
                if(_useConfigurator)
                {
                    _configurator.UpdateValueSlider(configSetName, config.Name, config.Morph.morphValue);
                }
            }
        }

        private void UpdateValue(MorphConfig config, float effect, float mass, float gravity)
        {
            float value =
                gravity * config.Multiplier1 * effect / 2 +
                mass * config.Multiplier2 * effect / 2;

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
