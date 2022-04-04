using System.Collections.Generic;
using UnityEngine;
using static TittyMagic.Utils;
using static TittyMagic.GravityEffectCalc;

namespace TittyMagic
{
    public class GravityOffsetMorphHandler
    {
        private readonly MVRScript _script;

        private float _mass;
        private float _softness;
        private float _morphing;

        public Multiplier yMultiplier { get; set; }

        private Dictionary<string, List<Config>> _configSets;

        public GravityOffsetMorphHandler(MVRScript script)
        {
            _script = script;
        }

        public void LoadSettings()
        {
            _configSets = new Dictionary<string, List<Config>>
            {
                { Direction.DOWN, LoadSettingsFromFile("upright", true) },
            };
        }

        private List<Config> LoadSettingsFromFile(string fileName, bool separateLeftRight = false)
        {
            var configs = new List<Config>();
            Persistence.LoadFromPath(
                _script,
                $@"{Globals.PLUGIN_PATH}settings\morphmultipliers\offset\{fileName}.json",
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

        public void Update(
            float roll,
            float pitch,
            float mass,
            float softness,
            float morphing
        )
        {
            _softness = softness;
            _mass = mass;
            _morphing = morphing;

            float smoothRoll = Calc.SmoothStep(roll);
            float smoothPitch = 2 * Calc.SmoothStep(pitch);

            AdjustUpDownMorphs(smoothPitch, smoothRoll);
        }

        private void AdjustUpDownMorphs(float pitch, float roll)
        {
            float effect = _morphing * CalculateUpDownEffect(pitch, roll, yMultiplier);
            // leaning forward
            if(pitch >= 0)
            {
                // upright
                if(pitch < 1)
                {
                    UpdateMorphs(Direction.DOWN, effect);
                }
                // upside down
                else
                {
                    ResetMorphs(Direction.DOWN);
                }
            }
            // leaning back
            else
            {
                // upright
                if(pitch >= -1)
                {
                    UpdateMorphs(Direction.DOWN, effect);
                }
                // upside down
                else
                {
                    ResetMorphs(Direction.DOWN);
                }
            }
        }

        private void UpdateMorphs(string configSetName, float effect)
        {
            foreach(var config in _configSets[configSetName])
            {
                var morphConfig = (MorphConfig) config;
                UpdateValue(morphConfig, effect);
            }
        }

        private void UpdateValue(MorphConfig config, float effect)
        {
            float value =
                (_softness * config.multiplier1 * effect) +
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
            }
        }
    }
}
