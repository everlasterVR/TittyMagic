using System.Collections.Generic;
using TittyMagic.Configs;
using static TittyMagic.Script;

namespace TittyMagic.Handlers
{
    internal static class GravityOffsetMorphHandler
    {
        private static Dictionary<string, List<MorphConfig>> _configSets;

        public static JSONStorableFloat offsetMorphingJsf { get; private set; }
        public static float upDownExtraMultiplier { get; set; }

        public static void Init()
        {
            offsetMorphingJsf = tittyMagic.NewJSONStorableFloat("gravityOffsetMorphing", 1.00f, 0.00f, 2.00f);
            offsetMorphingJsf.setCallbackFunction = value => tittyMagic.calibration.shouldRun = true;
        }

        public static void LoadSettings() =>
            _configSets = new Dictionary<string, List<MorphConfig>>
            {
                { Direction.DOWN, LoadSettingsFromFile(Direction.DOWN, "upright", true) },
            };

        private static List<MorphConfig> LoadSettingsFromFile(string subDir, string fileName, bool hasSeparateLeftRightConfigs = false)
        {
            string path = $@"{tittyMagic.PluginPath()}\settings\morphmultipliers\offset\{fileName}.json";
            var jsonClass = tittyMagic.LoadJSON(path).AsObject;

            var configs = new List<MorphConfig>();
            foreach(string name in jsonClass.Keys)
            {
                if(hasSeparateLeftRightConfigs)
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

        public static void Update(float roll, float pitch)
        {
            float smoothRoll = Calc.SmoothStep(roll);
            float smoothPitch = 2 * Calc.SmoothStep(pitch);

            AdjustUpDownMorphs(smoothPitch, smoothRoll);
        }

        private static void AdjustUpDownMorphs(float pitch, float roll)
        {
            float multiplier = upDownExtraMultiplier * GravityPhysicsHandler.downMultiplier;
            float upDownEffect = GravityEffectCalc.CalculateUpDownEffect(pitch, roll, multiplier);
            float effect = offsetMorphingJsf.val * upDownEffect;
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

        private static void UpdateMorphs(string configSetName, float effect)
        {
            float mass = MainPhysicsHandler.realMassAmount;
            float softness = tittyMagic.softnessAmount;
            _configSets[configSetName].ForEach(config => UpdateValue(config, effect, mass, softness));
        }

        private static void UpdateValue(MorphConfig config, float effect, float mass, float softness)
        {
            float value =
                softness * config.softnessMultiplier * effect +
                mass * config.massMultiplier * effect;
            bool inRange = config.isNegative ? value < 0 : value > 0;
            config.morph.morphValue = inRange ? Calc.RoundToDecimals(value, 1000f) : 0;
        }

        public static void ResetAll() => _configSets?.Keys.ToList().ForEach(ResetMorphs);

        private static void ResetMorphs(string configSetName) =>
            _configSets[configSetName].ForEach(config => config.morph.morphValue = 0);
    }
}
