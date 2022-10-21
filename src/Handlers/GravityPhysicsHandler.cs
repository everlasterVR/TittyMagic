using System.Collections.Generic;
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

            baseJsf.setCallbackFunction = _ => tittyMagic.calibrationHelper.shouldRun = true;
            upJsf.setCallbackFunction = _ => tittyMagic.calibrationHelper.shouldRun = true;
            downJsf.setCallbackFunction = _ => tittyMagic.calibrationHelper.shouldRun = true;
            forwardJsf.setCallbackFunction = _ => tittyMagic.calibrationHelper.shouldRun = true;
            backJsf.setCallbackFunction = _ => tittyMagic.calibrationHelper.shouldRun = true;
            leftRightJsf.setCallbackFunction = _ => tittyMagic.calibrationHelper.shouldRun = true;
        }

        public static void LoadSettings()
        {
            var paramGroups = MainPhysicsHandler.parameterGroups;
            paramGroups[SPRING].SetGravityPhysicsConfigs(NewSpringConfigs(), NewSpringConfigs());
            paramGroups[DAMPER].SetGravityPhysicsConfigs(NewDamperConfigs(), NewDamperConfigs());
            paramGroups[POSITION_SPRING_Z].SetGravityPhysicsConfigs(NewPositionSpringZConfigs(), NewPositionSpringZConfigs());
            paramGroups[TARGET_ROTATION_X].SetGravityPhysicsConfigs(NewTargetRotationXConfigs(), NewTargetRotationXConfigs());
            paramGroups[TARGET_ROTATION_Y].SetGravityPhysicsConfigs(NewTargetRotationYConfigs(), NewTargetRotationYConfigs());
            _paramGroups = new Dictionary<string, PhysicsParameterGroup[]>();
            _paramGroups[UP] = new[]
            {
                paramGroups[SPRING],
                paramGroups[DAMPER],
                paramGroups[TARGET_ROTATION_X],
            };
            _paramGroups[DOWN] = new[]
            {
                paramGroups[TARGET_ROTATION_X],
            };
            _paramGroups[FORWARD] = new[]
            {
                paramGroups[SPRING],
                paramGroups[DAMPER],
                paramGroups[POSITION_SPRING_Z],
            };
            _paramGroups[BACK] = new[]
            {
                paramGroups[SPRING],
                paramGroups[DAMPER],
                paramGroups[POSITION_SPRING_Z],
            };
            _paramGroups[LEFT] = new[]
            {
                paramGroups[SPRING],
                paramGroups[DAMPER],
                paramGroups[TARGET_ROTATION_Y],
            };
            _paramGroups[RIGHT] = new[]
            {
                paramGroups[SPRING],
                paramGroups[DAMPER],
                paramGroups[TARGET_ROTATION_Y],
            };
        }

        private static Dictionary<string, DynamicPhysicsConfig> NewSpringConfigs() =>
            new Dictionary<string, DynamicPhysicsConfig>
            {
                {
                    UP, new DynamicPhysicsConfig(
                        massMultiplier: -4.0f,
                        softnessMultiplier: -16.0f,
                        negative: true,
                        applyMethod: ApplyMethod.ADDITIVE,
                        massCurve: MainPhysicsHandler.InvertMass
                    )
                },
                {
                    BACK, new DynamicPhysicsConfig(
                        massMultiplier: -4.0f,
                        softnessMultiplier: -16.0f,
                        negative: true,
                        applyMethod: ApplyMethod.ADDITIVE,
                        massCurve: MainPhysicsHandler.InvertMass
                    )
                },
                {
                    FORWARD, new DynamicPhysicsConfig(
                        massMultiplier: -4.0f,
                        softnessMultiplier: -16.0f,
                        negative: true,
                        applyMethod: ApplyMethod.ADDITIVE,
                        massCurve: MainPhysicsHandler.InvertMass
                    )
                },
                {
                    LEFT, new DynamicPhysicsConfig(
                        massMultiplier: -4.0f,
                        softnessMultiplier: -16.0f,
                        negative: true,
                        applyMethod: ApplyMethod.ADDITIVE,
                        massCurve: MainPhysicsHandler.InvertMass
                    )
                },
                {
                    RIGHT, new DynamicPhysicsConfig(
                        massMultiplier: -4.0f,
                        softnessMultiplier: -16.0f,
                        negative: true,
                        applyMethod: ApplyMethod.ADDITIVE,
                        massCurve: MainPhysicsHandler.InvertMass
                    )
                },
            };

        private static Dictionary<string, DynamicPhysicsConfig> NewDamperConfigs() =>
            new Dictionary<string, DynamicPhysicsConfig>
            {
                {
                    UP, new DynamicPhysicsConfig(
                        massMultiplier: 0.25f,
                        softnessMultiplier: 1.00f,
                        negative: false,
                        applyMethod: ApplyMethod.ADDITIVE,
                        massCurve: MainPhysicsHandler.InvertMass
                    )
                },
                {
                    BACK, new DynamicPhysicsConfig(
                        massMultiplier: 0.25f,
                        softnessMultiplier: 1.00f,
                        negative: false,
                        applyMethod: ApplyMethod.ADDITIVE,
                        massCurve: MainPhysicsHandler.InvertMass
                    )
                },
                {
                    FORWARD, new DynamicPhysicsConfig(
                        massMultiplier: 0.25f,
                        softnessMultiplier: 1.00f,
                        negative: false,
                        applyMethod: ApplyMethod.ADDITIVE,
                        massCurve: MainPhysicsHandler.InvertMass
                    )
                },
                {
                    LEFT, new DynamicPhysicsConfig(
                        massMultiplier: 0.25f,
                        softnessMultiplier: 1.00f,
                        negative: false,
                        applyMethod: ApplyMethod.ADDITIVE,
                        massCurve: MainPhysicsHandler.InvertMass
                    )
                },
                {
                    RIGHT, new DynamicPhysicsConfig(
                        massMultiplier: 0.25f,
                        softnessMultiplier: 1.00f,
                        negative: false,
                        applyMethod: ApplyMethod.ADDITIVE,
                        massCurve: MainPhysicsHandler.InvertMass
                    )
                },
            };

        private static Dictionary<string, DynamicPhysicsConfig> NewPositionSpringZConfigs() =>
            new Dictionary<string, DynamicPhysicsConfig>
            {
                {
                    BACK, new DynamicPhysicsConfig(
                        massMultiplier: -230f,
                        softnessMultiplier: -170f,
                        negative: true,
                        applyMethod: ApplyMethod.ADDITIVE,
                        massCurve: MainPhysicsHandler.InvertMass,
                        softnessCurve: Curves.SpringZSoftnessCurve
                    )
                },
                {
                    FORWARD, new DynamicPhysicsConfig(
                        massMultiplier: -230f,
                        softnessMultiplier: -170f,
                        negative: true,
                        applyMethod: ApplyMethod.ADDITIVE,
                        massCurve: MainPhysicsHandler.InvertMass,
                        softnessCurve: Curves.SpringZSoftnessCurve
                    )
                },
            };

        private static Dictionary<string, DynamicPhysicsConfig> NewTargetRotationXConfigs() =>
            new Dictionary<string, DynamicPhysicsConfig>
            {
                {
                    DOWN, new DynamicPhysicsConfig(
                        massMultiplier: 0,
                        softnessMultiplier: -14.00f,
                        negative: true,
                        applyMethod: ApplyMethod.ADDITIVE,
                        softnessCurve: Curves.TargetRotationSoftnessCurve
                    )
                    {
                        baseMultiplier = -2.00f,
                    }
                },
                {
                    UP, new DynamicPhysicsConfig(
                        massMultiplier: 0,
                        softnessMultiplier: 16.50f,
                        negative: false,
                        applyMethod: ApplyMethod.ADDITIVE,
                        softnessCurve: Curves.TargetRotationSoftnessCurve
                    )
                    {
                        baseMultiplier = 2.36f,
                    }
                },
            };

        private static Dictionary<string, DynamicPhysicsConfig> NewTargetRotationYConfigs() =>
            new Dictionary<string, DynamicPhysicsConfig>
            {
                {
                    LEFT, new DynamicPhysicsConfig(
                        massMultiplier: 0,
                        softnessMultiplier: -16.50f,
                        negative: true,
                        applyMethod: ApplyMethod.ADDITIVE,
                        softnessCurve: Curves.TargetRotationSoftnessCurve
                    )
                    {
                        baseMultiplier = -2.36f,
                    }
                },
                {
                    RIGHT, new DynamicPhysicsConfig(
                        massMultiplier: 0,
                        softnessMultiplier: 16.50f,
                        negative: false,
                        applyMethod: ApplyMethod.ADDITIVE,
                        softnessCurve: Curves.TargetRotationSoftnessCurve
                    )
                    {
                        baseMultiplier = 2.36f,
                    }
                },
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

            // for some reason, if left right is adjusted after down, down physics is not correctly applied
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
        }
    }
}
