using System.Collections.Generic;
using UnityEngine;
using static TittyMagic.Utils;

namespace TittyMagic
{
    public class GravityMorphHandler
    {
        private readonly MVRScript _script;
        private readonly IConfigurator _configurator;
        private readonly bool _useConfigurator;

        private float _mass;
        private float _amount;
        private float _additionalRollEffect;

        private Dictionary<string, List<Config>> _configSets;

        public GravityMorphHandler(MVRScript script)
        {
            _script = script;
            // _useConfigurator = true;

            if(_useConfigurator)
            {
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
            }
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
            if(_useConfigurator)
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
            return !_useConfigurator || _configurator.enableAdjustment.val;
        }

        private void UpdateDebugInfo(string text)
        {
            if(!_useConfigurator)
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

            AdjustUpDownMorphs(smoothPitch, smoothRoll);
            AdjustRollMorphs(smoothRoll);
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
                UpdateMorphs(Direction.LEFT, roll);
            }
            // right
            else
            {
                ResetMorphs(Direction.LEFT);
                UpdateMorphs(Direction.RIGHT, -roll);
            }
        }

        private void AdjustUpDownMorphs(float pitch, float roll)
        {
            // leaning forward
            if(pitch >= 0)
            {
                UpdateMorphs(Direction.UP, pitch / 2, roll, _additionalRollEffect);
                UpdateMorphs(Direction.UP_C, pitch / 2, roll, _additionalRollEffect);
                UpdateMorphs(Direction.DOWN, (2 - pitch) / 2, roll);
            }
            // leaning back
            else
            {
                UpdateMorphs(Direction.UP, -pitch / 2, roll, _additionalRollEffect);
                UpdateMorphs(Direction.UP_C, -pitch / 2, roll, _additionalRollEffect);
                UpdateMorphs(Direction.DOWN, (2 + pitch) / 2, roll);
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
                    UpdateMorphs(Direction.FORWARD, pitch, roll);
                    UpdateMorphs(Direction.FORWARD_C, pitch, roll);
                }
                // upside down
                else
                {
                    UpdateMorphs(Direction.FORWARD, 2 - pitch, roll);
                    UpdateMorphs(Direction.FORWARD_C, 2 - pitch, roll);
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
                    UpdateMorphs(Direction.BACK, -pitch, roll);
                    UpdateMorphs(Direction.BACK_C, -pitch, roll);
                }
                // upside down
                else
                {
                    UpdateMorphs(Direction.BACK, 2 + pitch, roll);
                    UpdateMorphs(Direction.BACK_C, 2 + pitch, roll);
                }
            }
        }

        private void UpdateMorphs(string configSetName, float effect, float? roll = null, float? additional = null)
        {
            if(roll.HasValue)
            {
                effect = effect * (1 - Mathf.Abs(roll.Value));
            }

            if(additional.HasValue)
            {
                effect = effect + additional.Value;
            }

            foreach(var config in _configSets[configSetName])
            {
                var morphConfig = (MorphConfig) config;
                UpdateValue(morphConfig, effect);
                if(_useConfigurator)
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
                if(_useConfigurator)
                {
                    _configurator.UpdateValueSlider(configSetName, morphConfig.name, 0);
                }
            }
        }
    }
}
