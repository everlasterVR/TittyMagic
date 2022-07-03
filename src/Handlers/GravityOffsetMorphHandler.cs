// ReSharper disable RedundantUsingDirective
using System.Collections.Generic;
using TittyMagic.Configs;
using UnityEngine;
using static TittyMagic.Utils;
using static TittyMagic.GravityEffectCalc;

namespace TittyMagic
{
    internal class GravityOffsetMorphHandler
    {
        private readonly Script _script;

        private float _mass;
        private float _softness;

        private Dictionary<string, List<MorphConfig>> _configSets;

        public JSONStorableFloat offsetMorphingJsf { get; }
        public Multiplier yMultiplier { get; }

        public GravityOffsetMorphHandler(Script script)
        {
            _script = script;
            offsetMorphingJsf = script.NewJSONStorableFloat("gravityOffsetMorphing", 1.00f, 0.00f, 2.00f);
            yMultiplier = new Multiplier(script.gravityPhysicsHandler.yMultiplierJsf.val);
        }

        public void LoadSettings() =>
            _configSets = new Dictionary<string, List<MorphConfig>>
            {
                { Direction.DOWN, LoadSettingsFromFile(Direction.DOWN, "upright", true) },
            };

        private List<MorphConfig> LoadSettingsFromFile(string subDir, string fileName, bool separateLeftRight = false)
        {
            string path = $@"{_script.PluginPath()}\settings\morphmultipliers\offset\{fileName}.json";
            var jsonClass = _script.LoadJSON(path).AsObject;

            var configs = new List<MorphConfig>();
            foreach(string name in jsonClass.Keys)
            {
                if(separateLeftRight)
                {
                    configs.Add(
                        new MorphConfig(
                            $"{subDir}/{name} L",
                            jsonClass[name]["IsNegative"].AsBool,
                            jsonClass[name]["Multiplier1"].AsFloat,
                            jsonClass[name]["Multiplier2"].AsFloat
                        )
                    );
                    configs.Add(
                        new MorphConfig(
                            $"{subDir}/{name} R",
                            jsonClass[name]["IsNegative"].AsBool,
                            jsonClass[name]["Multiplier1"].AsFloat,
                            jsonClass[name]["Multiplier2"].AsFloat
                        )
                    );
                }
                else
                {
                    configs.Add(
                        new MorphConfig(
                            $"{subDir}/{name}",
                            jsonClass[name]["IsNegative"].AsBool,
                            jsonClass[name]["Multiplier1"].AsFloat,
                            jsonClass[name]["Multiplier2"].AsFloat
                        )
                    );
                }
            }

            return configs;
        }

        public void Update(
            float roll,
            float pitch,
            float mass,
            float softness
        )
        {
            _softness = softness;
            _mass = mass;

            float smoothRoll = Calc.SmoothStep(roll);
            float smoothPitch = 2 * Calc.SmoothStep(pitch);

            AdjustUpDownMorphs(smoothPitch, smoothRoll);
        }

        private void AdjustUpDownMorphs(float pitch, float roll)
        {
            float effect = offsetMorphingJsf.val * CalculateUpDownEffect(pitch, roll, _script.gravityPhysicsHandler.yMultiplier);
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

        private void UpdateMorphs(string configSetName, float effect) =>
            _configSets[configSetName].ForEach(config => UpdateValue(config, effect));

        private void UpdateValue(MorphConfig config, float effect)
        {
            float value =
                _softness * config.softnessMultiplier * effect +
                _mass * config.massMultiplier * effect;
            bool inRange = config.isNegative ? value < 0 : value > 0;
            config.morph.morphValue = inRange ? Calc.RoundToDecimals(value, 1000f) : 0;
        }

        public void ResetAll() => _configSets?.Keys.ToList().ForEach(ResetMorphs);

        private void ResetMorphs(string configSetName) =>
            _configSets[configSetName].ForEach(config => config.morph.morphValue = 0);
    }
}
