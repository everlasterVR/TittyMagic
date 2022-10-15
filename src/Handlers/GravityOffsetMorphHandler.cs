using System.Collections.Generic;
using TittyMagic.Handlers.Configs;
using static TittyMagic.Script;
using static TittyMagic.Direction;

namespace TittyMagic.Handlers
{
    public static class GravityOffsetMorphHandler
    {
        private static Dictionary<string, List<MorphConfig>> _configSets;

        public static JSONStorableFloat offsetMorphingJsf { get; private set; }

        public static void Init()
        {
            offsetMorphingJsf = tittyMagic.NewJSONStorableFloat("gravityOffsetMorphing", 1.00f, 0.00f, 2.00f);
            offsetMorphingJsf.setCallbackFunction = value => tittyMagic.calibrationHelper.shouldRun = true;
        }

        public static void LoadSettings() => _configSets = new Dictionary<string, List<MorphConfig>>
        {
            {
                DOWN, new List<MorphConfig>
                {
                    new MorphConfig("DN/DN Breast Rotate Up L",
                        false,
                        new JSONStorableFloat("softnessMultiplier", 0.76f, 0, 3),
                        new JSONStorableFloat("massMultiplier", 0.57f, 0, 3)
                    ),
                    new MorphConfig("DN/DN Breast Rotate Up R",
                        false,
                        new JSONStorableFloat("softnessMultiplier", 0.76f, 0, 3),
                        new JSONStorableFloat("massMultiplier", 0.57f, 0, 3)
                    ),
                    new MorphConfig("DN/DN Breasts Natural Reverse L",
                        false,
                        new JSONStorableFloat("softnessMultiplier", 0.36f, 0, 3),
                        new JSONStorableFloat("massMultiplier", 0.27f, 0, 3)
                    ),
                    new MorphConfig("DN/DN Breasts Natural Reverse R",
                        false,
                        new JSONStorableFloat("softnessMultiplier", 0.37f, 0, 3),
                        new JSONStorableFloat("massMultiplier", 0.27f, 0, 3)
                    ),
                }
            },
        };

        private static float _upDownExtraMultiplier;

        public static void SetMultipliers(float mass)
        {
            // https://www.desmos.com/calculator/z10fwnpvul
            _upDownExtraMultiplier = 0.50f * Curves.Exponential1(
                mass,
                0.26f,
                2.60f,
                5.38f,
                a: 0.84f,
                s: 0.33f
            );
        }

        public static void Update()
        {
            var rotation = MainPhysicsHandler.chestRb.rotation;
            float roll = Calc.SmoothStep(Calc.Roll(rotation));
            float pitch = 2 * Calc.SmoothStep(Calc.Pitch(rotation));
            AdjustUpDownMorphs(roll, pitch);
        }

        private static void AdjustUpDownMorphs(float roll, float pitch)
        {
            float multiplier = _upDownExtraMultiplier * GravityPhysicsHandler.downMultiplier;
            float effect = offsetMorphingJsf.val * GravityEffectCalc.UpDownEffect(pitch, roll, multiplier);
            if(pitch > 0)
            {
                // leaning forward
                if(pitch < 1)
                {
                    // upright
                    UpdateMorphs(DOWN, effect);
                }
                else
                {
                    // upside down
                    ResetMorphs(DOWN);
                }
            }
            else
            {
                // leaning back
                if(pitch > -1)
                {
                    // upright
                    UpdateMorphs(DOWN, effect);
                }
                else
                {
                    // upside down
                    ResetMorphs(DOWN);
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
                softness * config.softMultiplier * effect +
                mass * config.massMultiplier * effect;
            bool inRange = config.isNegative ? value < 0 : value > 0;
            config.morph.morphValue = inRange ? Calc.RoundToDecimals(value, 1000f) : 0;
        }

        public static void SimulateUpright() => AdjustUpDownMorphs(0, 0);

        public static void ResetAll() => _configSets?.Keys.ToList().ForEach(ResetMorphs);

        private static void ResetMorphs(string configSetName) =>
            _configSets[configSetName].ForEach(config => config.morph.morphValue = 0);

        public static void Destroy()
        {
            _configSets = null;
            offsetMorphingJsf = null;
        }
    }
}
