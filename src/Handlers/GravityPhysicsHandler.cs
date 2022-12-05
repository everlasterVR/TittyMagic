using System;
using System.Collections.Generic;
using System.Linq;
using TittyMagic.Handlers.Configs;
using TittyMagic.Models;
using static TittyMagic.Script;
using static TittyMagic.ParamName;
using static TittyMagic.Direction;

namespace TittyMagic.Handlers
{
    public static class GravityPhysicsHandler
    {
        private static Dictionary<string, PhysicsParameterGroup[]> _paramGroups;

        public static JSONStorableFloat baseJsf { get; private set; }
        public static JSONStorableFloat upJsf { get; private set; }
        public static JSONStorableFloat downJsf { get; private set; }
        public static JSONStorableFloat forwardJsf { get; private set; }
        public static JSONStorableFloat backJsf { get; private set; }
        public static JSONStorableFloat leftRightJsf { get; private set; }
        public static JSONStorableFloat targetRotationZJsf { get; private set; }

        private static float upMultiplier => baseJsf.val * upJsf.val;
        public static float downMultiplier => baseJsf.val * downJsf.val;
        private static float forwardMultiplier => baseJsf.val * forwardJsf.val;
        private static float backMultiplier => baseJsf.val * backJsf.val;
        private static float leftRightMultiplier => baseJsf.val * leftRightJsf.val;

        public static void Init()
        {
            baseJsf = tittyMagic.NewJSONStorableFloat("gravityPhysicsBase", 1.00f, 0.00f, 2.00f);
            upJsf = tittyMagic.NewJSONStorableFloat("gravityPhysicsUp", 1.00f, 0.00f, 2.00f);
            downJsf = tittyMagic.NewJSONStorableFloat("gravityPhysicsDown", 1.00f, 0.00f, 2.00f);
            forwardJsf = tittyMagic.NewJSONStorableFloat("gravityPhysicsForward", 1.00f, 0.00f, 2.00f);
            backJsf = tittyMagic.NewJSONStorableFloat("gravityPhysicsBack", 1.00f, 0.00f, 2.00f);
            leftRightJsf = tittyMagic.NewJSONStorableFloat("gravityPhysicsLeftRight", 1.00f, 0.00f, 2.00f);
            targetRotationZJsf = tittyMagic.NewJSONStorableFloat("targetRotationZ", 0.00f, -30.00f, 30.00f);

            baseJsf.setCallbackFunction = _ => tittyMagic.calibrationHelper.shouldRun = true;
            upJsf.setCallbackFunction = _ => tittyMagic.calibrationHelper.shouldRun = true;
            downJsf.setCallbackFunction = _ => tittyMagic.calibrationHelper.shouldRun = true;
            forwardJsf.setCallbackFunction = _ => tittyMagic.calibrationHelper.shouldRun = true;
            backJsf.setCallbackFunction = _ => tittyMagic.calibrationHelper.shouldRun = true;
            leftRightJsf.setCallbackFunction = _ => tittyMagic.calibrationHelper.shouldRun = true;
        }

        public static void LoadSettings()
        {
            string[] directions = { UP, DOWN, FORWARD, BACK, LEFT, RIGHT };
            var groupsByDirection = directions.ToDictionary(dir => dir, _ => new List<PhysicsParameterGroup>());

            var allConfigs = new Dictionary<string, Dictionary<string, Dictionary<string, DynamicPhysicsConfig>>>();
            allConfigs[SPRING] = ConfigPair(NewSpringConfigs(), NewSpringConfigs());
            allConfigs[DAMPER] = ConfigPair(NewDamperConfigs(), NewDamperConfigs());
            allConfigs[POSITION_SPRING_Z] = ConfigPair(NewPositionSpringZConfigs(), NewPositionSpringZConfigs());
            allConfigs[TARGET_ROTATION_X] = ConfigPair(NewRotationXConfigs(), NewRotationXConfigs());
            allConfigs[TARGET_ROTATION_Y] = TargetRotationYConfigs();
            allConfigs[TARGET_ROTATION_Z] = TargetRotationZConfigs();

            foreach(var kvp in allConfigs)
            {
                string paramName = kvp.Key;
                var paramConfigs = kvp.Value;
                foreach(string dir in directions)
                {
                    if(paramConfigs[Side.LEFT].ContainsKey(dir))
                    {
                        groupsByDirection[dir].Add(MainPhysicsHandler.parameterGroups[paramName]);
                    }
                }
            }

            string[] paramNames = { SPRING, DAMPER, POSITION_SPRING_Z, TARGET_ROTATION_X, TARGET_ROTATION_Y, TARGET_ROTATION_Z };
            foreach(string paramName in paramNames)
            {
                MainPhysicsHandler.parameterGroups[paramName].SetGravityPhysicsConfigs(allConfigs[paramName]);
            }

            _paramGroups = groupsByDirection.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray());
        }

        private static DynamicPhysicsConfig NewSpringConfig() => new DynamicPhysicsConfig
        {
            massMultiplier = -3.0f,
            softnessMultiplier = -12.0f,
            applyMethod = ApplyMethod.ADDITIVE,
            massCurve = MainPhysicsHandler.InvertMass,
            negative = true,
        };

        private static Dictionary<string, DynamicPhysicsConfig> NewSpringConfigs() =>
            new Dictionary<string, DynamicPhysicsConfig>
            {
                { UP, NewSpringConfig() },
                { BACK, NewSpringConfig() },
                { FORWARD, NewSpringConfig() },
                { LEFT, NewSpringConfig() },
                { RIGHT, NewSpringConfig() },
            };

        private static DynamicPhysicsConfig NewDamperConfig() => new DynamicPhysicsConfig
        {
            massMultiplier = 0.25f,
            softnessMultiplier = 1.00f,
            applyMethod = ApplyMethod.ADDITIVE,
            massCurve = MainPhysicsHandler.InvertMass,
        };

        private static Dictionary<string, DynamicPhysicsConfig> NewDamperConfigs() =>
            new Dictionary<string, DynamicPhysicsConfig>
            {
                { UP, NewDamperConfig() },
                { BACK, NewDamperConfig() },
                { FORWARD, NewDamperConfig() },
                { LEFT, NewDamperConfig() },
                { RIGHT, NewDamperConfig() },
            };

        private static Dictionary<string, DynamicPhysicsConfig> NewPositionSpringZConfigs()
        {
            Func<float, float> normalizeInvertMass = x => MainPhysicsHandler.NormalizeMass(MainPhysicsHandler.InvertMass(x));
            return new Dictionary<string, DynamicPhysicsConfig>
            {
                {
                    BACK, new DynamicPhysicsConfig
                    {
                        massMultiplier = -125f,
                        softnessMultiplier = -50f,
                        applyMethod = ApplyMethod.ADDITIVE,
                        // https://www.desmos.com/calculator/po08nsvnfa
                        massCurve = x => Curves.Exponential1(normalizeInvertMass(x), 0.3f, 10f, 0.84f, a: 1.01f),
                        softnessCurve = Curves.SpringZSoftnessCurve,
                        baseMultiplier = -125f,
                        negative = true,
                    }
                },
                {
                    FORWARD, new DynamicPhysicsConfig
                    {
                        massMultiplier = -125f,
                        softnessMultiplier = -50f,
                        applyMethod = ApplyMethod.ADDITIVE,
                        massCurve = x => Curves.Exponential1(normalizeInvertMass(x), 0.3f, 10f, 0.84f, a: 1.01f),
                        softnessCurve = Curves.SpringZSoftnessCurve,
                        baseMultiplier = -125f,
                        negative = true,
                    }
                },
            };
        }

        private static Dictionary<string, DynamicPhysicsConfig> NewRotationXConfigs() =>
            new Dictionary<string, DynamicPhysicsConfig>
            {
                {
                    UP, new DynamicPhysicsConfig
                    {
                        softnessMultiplier = 11.00f,
                        applyMethod = ApplyMethod.ADDITIVE,
                        softnessCurve = Curves.TargetRotationSoftnessCurve,
                        baseMultiplier = 1.60f,
                    }
                },
                {
                    DOWN, new DynamicPhysicsConfig
                    {
                        softnessMultiplier = -9.40f,
                        applyMethod = ApplyMethod.ADDITIVE,
                        softnessCurve = Curves.TargetRotationSoftnessCurve,
                        baseMultiplier = -1.34f,
                        negative = true,
                    }
                },
            };

        private static Dictionary<string, Dictionary<string, DynamicPhysicsConfig>> TargetRotationYConfigs() =>
            ConfigPair(
                new Dictionary<string, DynamicPhysicsConfig>
                {
                    {
                        LEFT, new DynamicPhysicsConfig
                        {
                            softnessMultiplier = -11.00f,
                            applyMethod = ApplyMethod.ADDITIVE,
                            softnessCurve = Curves.TargetRotationSoftnessCurve,
                            baseMultiplier = -1.60f,
                            negative = true,
                        }
                    },
                    {
                        RIGHT, new DynamicPhysicsConfig
                        {
                            softnessMultiplier = 16.50f,
                            applyMethod = ApplyMethod.ADDITIVE,
                            softnessCurve = Curves.TargetRotationSoftnessCurve,
                            baseMultiplier = 1.60f,
                        }
                    },
                },
                new Dictionary<string, DynamicPhysicsConfig>
                {
                    {
                        LEFT, new DynamicPhysicsConfig
                        {
                            softnessMultiplier = -16.50f,
                            applyMethod = ApplyMethod.ADDITIVE,
                            softnessCurve = Curves.TargetRotationSoftnessCurve,
                            baseMultiplier = -1.60f,
                            negative = true,
                        }
                    },
                    {
                        RIGHT, new DynamicPhysicsConfig
                        {
                            softnessMultiplier = 11.00f,
                            applyMethod = ApplyMethod.ADDITIVE,
                            softnessCurve = Curves.TargetRotationSoftnessCurve,
                            baseMultiplier = 1.60f,
                        }
                    },
                }
            );

        private static Dictionary<string, DynamicPhysicsConfig> NewRotationZConfigs() =>
            new Dictionary<string, DynamicPhysicsConfig>
            {
                {
                    UP, new DynamicPhysicsConfig
                    {
                        applyMethod = ApplyMethod.ADDITIVE,
                    }
                },
                {
                    DOWN, new DynamicPhysicsConfig
                    {
                        applyMethod = ApplyMethod.ADDITIVE,
                    }
                },
            };

        private static Dictionary<string, Dictionary<string, DynamicPhysicsConfig>> TargetRotationZConfigs()
        {
            var left = NewRotationZConfigs();
            var right = NewRotationZConfigs();

            targetRotationZJsf.setCallbackFunction = value =>
            {
                float rounded = Calc.RoundToDecimals(2 * value);
                left[UP].baseMultiplier = -rounded;
                left[DOWN].baseMultiplier = rounded;
                right[UP].baseMultiplier = rounded;
                right[DOWN].baseMultiplier = -rounded;
            };

            return ConfigPair(left, right);
        }

        private static Dictionary<string, Dictionary<string, DynamicPhysicsConfig>> ConfigPair(
            Dictionary<string, DynamicPhysicsConfig> left,
            Dictionary<string, DynamicPhysicsConfig> right
        ) =>
            new Dictionary<string, Dictionary<string, DynamicPhysicsConfig>>
            {
                { Side.LEFT, left },
                { Side.RIGHT, right },
            };

        private static float _mass;
        private static float _softness;
        private static float _rollL;
        private static float _rollR;
        private static float _pitchL;
        private static float _pitchR;

        public static void Update()
        {
            _mass = MainPhysicsHandler.massAmount;
            _softness = tittyMagic.softnessAmount;
            _rollL = Calc.SmoothStep((GravityEffectCalc.pectoralRollL + GravityEffectCalc.chestRoll) / 2);
            _rollR = Calc.SmoothStep((GravityEffectCalc.pectoralRollR + GravityEffectCalc.chestRoll) / 2);
            _pitchL = 2 * Calc.SmoothStep(GravityEffectCalc.pectoralPitchL);
            _pitchR = 2 * Calc.SmoothStep(GravityEffectCalc.pectoralPitchR);

            AdjustLeftRightPhysics();
            AdjustVerticalPhysics();
            AdjustHorizontalPhysics();
        }

        private static void AdjustLeftRightPhysics()
        {
            float effectL = GravityEffectCalc.RollEffect(_rollL, leftRightMultiplier);
            if(_rollL > 0)
            {
                ResetLeftBreast(RIGHT);
                UpdateLeftBreast(LEFT, effectL);
            }
            else
            {
                ResetLeftBreast(LEFT);
                UpdateLeftBreast(RIGHT, effectL);
            }

            float effectR = GravityEffectCalc.RollEffect(_rollR, leftRightMultiplier);
            if(_rollR > 0)
            {
                ResetRightBreast(RIGHT);
                UpdateRightBreast(LEFT, effectR);
            }
            else
            {
                ResetRightBreast(LEFT);
                UpdateRightBreast(RIGHT, effectR);
            }
        }

        private static void AdjustVerticalPhysics()
        {
            float effectL = GravityEffectCalc.UpDownAdjustByAngle(_pitchL) * GravityEffectCalc.RollMultiplier(_rollL) / 2;
            float effectR = GravityEffectCalc.UpDownAdjustByAngle(_pitchR) * GravityEffectCalc.RollMultiplier(_rollR) / 2;

            if(_pitchL > 1 || _pitchL < -1)
            {
                // upside down, leaning forward or back
                ResetLeftBreast(DOWN);
                UpdateLeftBreast(UP, effectL * upMultiplier);
            }
            else
            {
                ResetLeftBreast(UP);
                UpdateLeftBreast(DOWN, effectL * downMultiplier);
            }

            if(_pitchR > 1 || _pitchR < -1)
            {
                // upside down, leaning forward or back
                ResetRightBreast(DOWN);
                UpdateRightBreast(UP, effectR * upMultiplier);
            }
            else
            {
                ResetRightBreast(UP);
                UpdateRightBreast(DOWN, effectR * downMultiplier);
            }
        }

        private static void AdjustHorizontalPhysics()
        {
            float effectL = GravityEffectCalc.DepthAdjustByAngle(_pitchL) * GravityEffectCalc.RollMultiplier(_rollL) / 2;
            float effectR = GravityEffectCalc.DepthAdjustByAngle(_pitchR) * GravityEffectCalc.RollMultiplier(_rollR) / 2;

            if(_pitchL > 0)
            {
                // leaning forward, upright or upside down
                ResetLeftBreast(BACK);
                UpdateLeftBreast(FORWARD, effectL * forwardMultiplier);
            }
            else
            {
                // leaning back
                ResetLeftBreast(FORWARD);
                UpdateLeftBreast(BACK, effectL * backMultiplier);
            }

            if(_pitchR > 0)
            {
                // leaning forward, upright or upside down
                ResetRightBreast(BACK);
                UpdateRightBreast(FORWARD, effectR * forwardMultiplier);
            }
            else
            {
                // leaning back
                UpdateRightBreast(BACK, effectR * backMultiplier);
                ResetRightBreast(FORWARD);
            }
        }

        private static void UpdateLeftBreast(string direction, float effect)
        {
            foreach(var group in _paramGroups[direction])
            {
                group.left.UpdateGravityValue(direction, effect, _mass, _softness);
            }
        }

        private static void UpdateRightBreast(string direction, float effect)
        {
            foreach(var group in _paramGroups[direction])
            {
                group.right.UpdateGravityValue(direction, effect, _mass, _softness);
            }
        }

        public static void SimulateUpright()
        {
            _mass = MainPhysicsHandler.massAmount;
            _softness = tittyMagic.softnessAmount;
            _rollL = 0;
            _rollR = 0;
            _pitchL = 0;
            _pitchR = 0;

            AdjustLeftRightPhysics();
            AdjustVerticalPhysics();
            AdjustHorizontalPhysics();
        }

        private static void ResetLeftBreast(string direction)
        {
            foreach(var group in _paramGroups[direction])
            {
                group.left.ResetGravityValue(direction);
            }
        }

        private static void ResetRightBreast(string direction)
        {
            foreach(var group in _paramGroups[direction])
            {
                group.right.ResetGravityValue(direction);
            }
        }

        public static void Destroy()
        {
            _paramGroups = null;
            baseJsf = null;
            upJsf = null;
            downJsf = null;
            forwardJsf = null;
            backJsf = null;
            leftRightJsf = null;
            targetRotationZJsf = null;
        }
    }
}
