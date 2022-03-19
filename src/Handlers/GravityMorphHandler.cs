//#define USE_CONFIGURATOR
using System.Collections.Generic;
using UnityEngine;
using static TittyMagic.Utils;

namespace TittyMagic
{
    public class GravityMorphHandler
    {
        private readonly MVRScript _script;
        private readonly IConfigurator _configurator;

        private float _mass;
        private float _amount;
        private float _additionalRollEffect;

        public Multiplier xMultiplier { get; set; }
        public Multiplier yMultiplier { get; set; }
        public Multiplier zMultiplier { get; set; }

        private Dictionary<string, List<Config>> _configSets;

        public GravityMorphHandler(MVRScript script)
        {
            _script = script;
#if USE_CONFIGURATOR
            _configurator = (IConfigurator) FindPluginOnAtom(_script.containingAtom, nameof(GravityMorphConfigurator));
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
#endif
        }

        public void LoadSettings(string mode)
        {
            _configSets = new Dictionary<string, List<Config>>
            {
                { Direction.DOWN, LoadSettingsFromFile(mode, "upright", true) },
                { Direction.UP, LoadSettingsFromFile(mode, "upsideDown", true) },
                { Direction.UP_C, LoadSettingsFromFile(mode, "upsideDownCenter") },
                { Direction.LEFT, LoadSettingsFromFile(mode, "rollLeft") },
                { Direction.RIGHT, LoadSettingsFromFile(mode, "rollRight") },
                { Direction.BACK, LoadSettingsFromFile(mode, "leanBack", true) },
                { Direction.BACK_C, LoadSettingsFromFile(mode, "leanBackCenter") },
                { Direction.FORWARD, LoadSettingsFromFile(mode, "leanForward", true) },
                { Direction.FORWARD_C, LoadSettingsFromFile(mode, "leanForwardCenter") },
            };

            // not working properly yet when changing mode on the fly
            if(_configurator != null)
            {
                _configurator.ResetUISectionGroups();
                // _configurator.InitUISectionGroup(Direction.DOWN, _configSets[Direction.DOWN]);
                // _configurator.InitUISectionGroup(Direction.UP, _configSets[Direction.UP]);
                // _configurator.InitUISectionGroup(Direction.UP_C, _configSets[Direction.UP_C]);
                //
                // _configurator.InitUISectionGroup(Direction.LEFT, _configSets[Direction.LEFT]);
                // _configurator.InitUISectionGroup(Direction.RIGHT, _configSets[Direction.RIGHT]);
                //
                // _configurator.InitUISectionGroup(Direction.BACK, _configSets[Direction.BACK]);
                // _configurator.InitUISectionGroup(Direction.BACK_C, _configSets[Direction.BACK_C]);
                // _configurator.InitUISectionGroup(Direction.FORWARD, _configSets[Direction.FORWARD]);
                // _configurator.InitUISectionGroup(Direction.FORWARD_C, _configSets[Direction.FORWARD_C]);
            }
        }

        private List<Config> LoadSettingsFromFile(string mode, string fileName, bool separateLeftRight = false)
        {
            var configs = new List<Config>();
            Persistence.LoadModeMorphSettings(
                _script,
                mode,
                $"{fileName}.json",
                (dir, json) =>
                {
                    foreach(string name in json.Keys)
                    {
                        if(separateLeftRight)
                        {
                            configs.Add(
                                new MorphConfig(
                                    $"{name} L",
                                    json[name]["IsNegative"].AsBool,
                                    json[name]["Multiplier1"].AsFloat,
                                    json[name]["Multiplier2"].AsFloat
                                )
                            );
                            configs.Add(
                                new MorphConfig(
                                    $"{name} R",
                                    json[name]["IsNegative"].AsBool,
                                    json[name]["Multiplier1"].AsFloat,
                                    json[name]["Multiplier2"].AsFloat
                                )
                            );
                        }
                        else
                        {
                            configs.Add(
                                new MorphConfig(
                                    name,
                                    json[name]["IsNegative"].AsBool,
                                    json[name]["Multiplier1"].AsFloat,
                                    json[name]["Multiplier2"].AsFloat
                                )
                            );
                        }
                    }
                }
            );
            return configs;
        }

        public bool IsEnabled()
        {
            return _configurator == null || _configurator.enableAdjustment.val;
        }

        private void UpdateDebugInfo(string text)
        {
            if(_configurator == null)
            {
                return;
            }

            _configurator.debugInfo.val = text;
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
            _additionalRollEffect = 0.4f * Mathf.Abs(smoothRoll);

            AdjustRollMorphs(smoothRoll);
            AdjustUpDownMorphs(smoothPitch, smoothRoll);
            AdjustForwardBackMorphs(smoothPitch, smoothRoll);

            string infoText =
                $"{NameValueString("Pitch", pitch, 100f)} {Calc.RoundToDecimals(smoothPitch, 100f)}\n" +
                $"{NameValueString("Roll", roll, 100f)} {Calc.RoundToDecimals(smoothRoll, 100f)}\n" +
                $"{_additionalRollEffect}";
            UpdateDebugInfo(infoText);
        }

        private void AdjustRollMorphs(float roll)
        {
            // left
            if(roll >= 0)
            {
                ResetMorphs(Direction.RIGHT);
                UpdateLeftRightMorphs(Direction.LEFT, roll);
            }
            // right
            else
            {
                ResetMorphs(Direction.LEFT);
                UpdateLeftRightMorphs(Direction.RIGHT, -roll);
            }
        }

        private void AdjustUpDownMorphs(float pitch, float roll)
        {
            // leaning forward
            if(pitch >= 0)
            {
                UpdateUpDownMorphs(Direction.UP, pitch / 2, roll, _additionalRollEffect);
                UpdateUpDownMorphs(Direction.UP_C, pitch / 2, roll, _additionalRollEffect);
                UpdateUpDownMorphs(Direction.DOWN, (2 - pitch) / 2, roll);
            }
            // leaning back
            else
            {
                UpdateUpDownMorphs(Direction.UP, -pitch / 2, roll, _additionalRollEffect);
                UpdateUpDownMorphs(Direction.UP_C, -pitch / 2, roll, _additionalRollEffect);
                UpdateUpDownMorphs(Direction.DOWN, (2 + pitch) / 2, roll);
            }
        }

        private void AdjustForwardBackMorphs(float pitch, float roll)
        {
            // leaning forward
            if(pitch >= 0)
            {
                ResetMorphs(Direction.BACK);
                ResetMorphs(Direction.BACK_C);
                // upright
                if(pitch < 1)
                {
                    UpdateForwardBackMorphs(Direction.FORWARD, pitch, roll);
                    UpdateForwardBackMorphs(Direction.FORWARD_C, pitch, roll);
                }
                // upside down
                else
                {
                    UpdateForwardBackMorphs(Direction.FORWARD, 2 - pitch, roll);
                    UpdateForwardBackMorphs(Direction.FORWARD_C, 2 - pitch, roll);
                }
            }
            // leaning back
            else
            {
                ResetMorphs(Direction.FORWARD);
                ResetMorphs(Direction.FORWARD_C);
                // upright
                if(pitch >= -1)
                {
                    UpdateForwardBackMorphs(Direction.BACK, -pitch, roll);
                    UpdateForwardBackMorphs(Direction.BACK_C, -pitch, roll);
                }
                // upside down
                else
                {
                    UpdateForwardBackMorphs(Direction.BACK, 2 + pitch, roll);
                    UpdateForwardBackMorphs(Direction.BACK_C, 2 + pitch, roll);
                }
            }
        }

        private void UpdateLeftRightMorphs(string configSetName, float effect)
        {
            foreach(var config in _configSets[configSetName])
            {
                var morphConfig = (MorphConfig) config;
                UpdateValue(morphConfig, xMultiplier.m.val * effect);
                if(_configurator != null)
                {
                    _configurator.UpdateValueSlider(configSetName, morphConfig.name, morphConfig.morph.morphValue);
                }
            }
        }

        private void UpdateUpDownMorphs(string configSetName, float effect, float roll, float? additional = null)
        {
            effect *= 1 - Mathf.Abs(roll);
            if(additional.HasValue)
            {
                effect += additional.Value;
            }

            foreach(var config in _configSets[configSetName])
            {
                var morphConfig = (MorphConfig) config;
                UpdateValue(morphConfig, yMultiplier.m.val * effect);
                if(_configurator != null)
                {
                    _configurator.UpdateValueSlider(configSetName, morphConfig.name, morphConfig.morph.morphValue);
                }
            }
        }

        private void UpdateForwardBackMorphs(string configSetName, float effect, float roll)
        {
            effect = effect * (1 - Mathf.Abs(roll));
            foreach(var config in _configSets[configSetName])
            {
                var morphConfig = (MorphConfig) config;
                UpdateValue(morphConfig, zMultiplier.m.val * effect);
                if(_configurator != null)
                {
                    _configurator.UpdateValueSlider(configSetName, morphConfig.name, morphConfig.morph.morphValue);
                }
            }
        }

        private void UpdateValue(MorphConfig config, float effect)
        {
            float value =
                (_amount * config.multiplier1 * effect / 2) +
                (_mass * config.multiplier2 * effect / 2);

            bool inRange = config.isNegative ? value < 0 : value > 0;
            config.morph.morphValue = inRange ? Calc.RoundToDecimals(value, 1000f) : 0;
        }

        public void ResetAll()
        {
            _configSets?.Keys.ToList().ForEach(ResetMorphs);
        }

        private void ResetMorphs(string configSetName)
        {
            foreach(var config in _configSets[configSetName])
            {
                var morphConfig = (MorphConfig) config;
                morphConfig.morph.morphValue = 0;
                if(_configurator != null)
                {
                    _configurator.UpdateValueSlider(configSetName, morphConfig.name, 0);
                }
            }
        }
    }
}
