//#define USE_CONFIGURATOR
using System.Collections.Generic;
using UnityEngine;
using static TittyMagic.Utils;
using static TittyMagic.GravityEffectCalc;

namespace TittyMagic
{
    public class GravityMorphHandler
    {
        private readonly MVRScript _script;
        private readonly IConfigurator _configurator;

        private float _mass;
        private float _amount;

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

        public void LoadSettings()
        {
            _configSets = new Dictionary<string, List<Config>>
            {
                { Direction.DOWN, LoadSettingsFromFile("upright", true) },
                { Direction.UP, LoadSettingsFromFile("upsideDown", true) },
                { Direction.UP_C, LoadSettingsFromFile("upsideDownCenter") },
                { Direction.LEFT, LoadSettingsFromFile("rollLeft") },
                { Direction.RIGHT, LoadSettingsFromFile("rollRight") },
                { Direction.BACK, LoadSettingsFromFile("leanBack", true) },
                { Direction.BACK_C, LoadSettingsFromFile("leanBackCenter") },
                { Direction.FORWARD, LoadSettingsFromFile("leanForward", true) },
                { Direction.FORWARD_C, LoadSettingsFromFile("leanForwardCenter") },
            };

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

        private List<Config> LoadSettingsFromFile(string fileName, bool separateLeftRight = false)
        {
            var configs = new List<Config>();
            Persistence.LoadFromPath(
                _script,
                $@"{Globals.PLUGIN_PATH}settings\morphmultipliers\futa\{fileName}.json",
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
            float additionalRollEffect = 0.4f * Mathf.Abs(smoothRoll);

            AdjustRollMorphs(smoothRoll);
            AdjustUpDownMorphs(smoothPitch, smoothRoll, additionalRollEffect);
            AdjustDepthMorphs(smoothPitch, smoothRoll);

            if(_configurator != null)
            {
                _configurator.debugInfo.val =
                    $"{NameValueString("Pitch", pitch, 100f)} {Calc.RoundToDecimals(smoothPitch, 100f)}\n" +
                    $"{NameValueString("Roll", roll, 100f)} {Calc.RoundToDecimals(smoothRoll, 100f)}\n";
            }
        }

        private void AdjustRollMorphs(float roll)
        {
            float effect = CalculateRollEffect(roll, xMultiplier);
            // left
            if(roll >= 0)
            {
                ResetMorphs(Direction.RIGHT);
                UpdateMorphs(Direction.LEFT, effect);
            }
            // right
            else
            {
                ResetMorphs(Direction.LEFT);
                UpdateMorphs(Direction.RIGHT, effect);
            }
        }

        private void AdjustUpDownMorphs(float pitch, float roll, float additionalRollEffect)
        {
            float upEffect = CalculateUpEffect(pitch, roll, yMultiplier, additionalRollEffect);
            float downEffect = CalculateDownEffect(pitch, roll, yMultiplier);

            // leaning forward
            if(pitch >= 0)
            {
                UpdateMorphs(Direction.UP, upEffect);
                UpdateMorphs(Direction.UP_C, upEffect);
                UpdateMorphs(Direction.DOWN, downEffect);
            }
            // leaning back
            else
            {
                UpdateMorphs(Direction.UP, upEffect);
                UpdateMorphs(Direction.UP_C, upEffect);
                UpdateMorphs(Direction.DOWN, downEffect);
            }
        }

        private void AdjustDepthMorphs(float pitch, float roll)
        {
            float effect = CalculateDepthEffect(pitch, roll, zMultiplier);
            // leaning forward
            if(pitch >= 0)
            {
                ResetMorphs(Direction.BACK);
                ResetMorphs(Direction.BACK_C);
                // upright
                if(pitch < 1)
                {
                    UpdateMorphs(Direction.FORWARD, effect);
                    UpdateMorphs(Direction.FORWARD_C, effect);
                }
                // upside down
                else
                {
                    UpdateMorphs(Direction.FORWARD, effect);
                    UpdateMorphs(Direction.FORWARD_C, effect);
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
                    UpdateMorphs(Direction.BACK, effect);
                    UpdateMorphs(Direction.BACK_C, effect);
                }
                // upside down
                else
                {
                    UpdateMorphs(Direction.BACK, effect);
                    UpdateMorphs(Direction.BACK_C, effect);
                }
            }
        }

        private void UpdateMorphs(string configSetName, float effect)
        {
            foreach(var config in _configSets[configSetName])
            {
                var morphConfig = (MorphConfig) config;
                UpdateValue(morphConfig, effect);
                if(_configurator != null)
                {
                    _configurator.UpdateValueSlider(configSetName, morphConfig.name, morphConfig.morph.morphValue);
                }
            }
        }

        private void UpdateValue(MorphConfig config, float effect)
        {
            float value =
                (_amount * config.multiplier1 * effect) +
                (_mass * config.multiplier2 * effect);

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
