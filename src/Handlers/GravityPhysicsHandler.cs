﻿using System.Collections.Generic;
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
            paramGroups[SPRING].SetGravityPhysicsConfigs(SpringConfigs());
            paramGroups[DAMPER].SetGravityPhysicsConfigs(DamperConfigs());
            paramGroups[POSITION_SPRING_Z].SetGravityPhysicsConfigs(PositionSpringZConfigs());
            paramGroups[TARGET_ROTATION_X].SetGravityPhysicsConfigs(TargetRotationXConfigs());
            paramGroups[TARGET_ROTATION_Y].SetGravityPhysicsConfigs(TargetRotationYConfigs());
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

        private static Dictionary<string, Dictionary<string, DynamicPhysicsConfig>> SpringConfigs()
        {
            var upConfig = new DynamicPhysicsConfig(
                massMultiplier: -4.0f,
                softnessMultiplier: -16.0f,
                applyMethod: ApplyMethod.ADDITIVE,
                massCurve: MainPhysicsHandler.InvertMass
            )
            {
                negative = true,
            };
            var backConfig = new DynamicPhysicsConfig(
                massMultiplier: -4.0f,
                softnessMultiplier: -16.0f,
                applyMethod: ApplyMethod.ADDITIVE,
                massCurve: MainPhysicsHandler.InvertMass
            )
            {
                negative = true,
            };
            var forwardConfig = new DynamicPhysicsConfig(
                massMultiplier: -4.0f,
                softnessMultiplier: -16.0f,
                applyMethod: ApplyMethod.ADDITIVE,
                massCurve: MainPhysicsHandler.InvertMass
            )
            {
                negative = true,
            };
            var leftConfig = new DynamicPhysicsConfig(
                massMultiplier: -4.0f,
                softnessMultiplier: -16.0f,
                applyMethod: ApplyMethod.ADDITIVE,
                massCurve: MainPhysicsHandler.InvertMass
            )
            {
                negative = true,
            };
            var rightConfig = new DynamicPhysicsConfig(
                massMultiplier: -4.0f,
                softnessMultiplier: -16.0f,
                applyMethod: ApplyMethod.ADDITIVE,
                massCurve: MainPhysicsHandler.InvertMass
            )
            {
                negative = true,
            };
            var leftBreast = new Dictionary<string, DynamicPhysicsConfig>
            {
                { UP, upConfig },
                { BACK, backConfig },
                { FORWARD, forwardConfig },
                { LEFT, leftConfig },
                { RIGHT, rightConfig },
            };
            var rightBreast = new Dictionary<string, DynamicPhysicsConfig>
            {
                { UP, upConfig },
                { BACK, backConfig },
                { FORWARD, forwardConfig },
                { LEFT, leftConfig },
                { RIGHT, rightConfig },
            };
            return new Dictionary<string, Dictionary<string, DynamicPhysicsConfig>>
            {
                { Side.LEFT, leftBreast },
                { Side.RIGHT, rightBreast },
            };
        }

        private static Dictionary<string, Dictionary<string, DynamicPhysicsConfig>> DamperConfigs()
        {
            var upConfig = new DynamicPhysicsConfig(
                massMultiplier: 0.25f,
                softnessMultiplier: 1.00f,
                applyMethod: ApplyMethod.ADDITIVE,
                massCurve: MainPhysicsHandler.InvertMass
            )
            {
                negative = false,
            };
            var backConfig = new DynamicPhysicsConfig(
                massMultiplier: 0.25f,
                softnessMultiplier: 1.00f,
                applyMethod: ApplyMethod.ADDITIVE,
                massCurve: MainPhysicsHandler.InvertMass
            )
            {
                negative = false,
            };
            var forwardConfig = new DynamicPhysicsConfig(
                massMultiplier: 0.25f,
                softnessMultiplier: 1.00f,
                applyMethod: ApplyMethod.ADDITIVE,
                massCurve: MainPhysicsHandler.InvertMass
            )
            {
                negative = false,
            };
            var leftConfig = new DynamicPhysicsConfig(
                massMultiplier: 0.25f,
                softnessMultiplier: 1.00f,
                applyMethod: ApplyMethod.ADDITIVE,
                massCurve: MainPhysicsHandler.InvertMass
            )
            {
                negative = false,
            };
            var rightConfig = new DynamicPhysicsConfig(
                massMultiplier: 0.25f,
                softnessMultiplier: 1.00f,
                applyMethod: ApplyMethod.ADDITIVE,
                massCurve: MainPhysicsHandler.InvertMass
            )
            {
                negative = false,
            };
            var leftBreast = new Dictionary<string, DynamicPhysicsConfig>
            {
                { UP, upConfig },
                { BACK, backConfig },
                { FORWARD, forwardConfig },
                { LEFT, leftConfig },
                { RIGHT, rightConfig },
            };
            var rightBreast = new Dictionary<string, DynamicPhysicsConfig>
            {
                { UP, upConfig },
                { BACK, backConfig },
                { FORWARD, forwardConfig },
                { LEFT, leftConfig },
                { RIGHT, rightConfig },
            };
            return new Dictionary<string, Dictionary<string, DynamicPhysicsConfig>>
            {
                { Side.LEFT, leftBreast },
                { Side.RIGHT, rightBreast },
            };
        }

        private static Dictionary<string, Dictionary<string, DynamicPhysicsConfig>> PositionSpringZConfigs()
        {
            var backConfig = new DynamicPhysicsConfig(
                massMultiplier: -230f,
                softnessMultiplier: -170f,
                applyMethod: ApplyMethod.ADDITIVE,
                massCurve: MainPhysicsHandler.InvertMass,
                softnessCurve: Curves.SpringZSoftnessCurve
            )
            {
                negative = true,
            };
            var forwardConfig = new DynamicPhysicsConfig(
                massMultiplier: -230f,
                softnessMultiplier: -170f,
                applyMethod: ApplyMethod.ADDITIVE,
                massCurve: MainPhysicsHandler.InvertMass,
                softnessCurve: Curves.SpringZSoftnessCurve
            )
            {
                negative = true,
            };
            var leftBreast = new Dictionary<string, DynamicPhysicsConfig>
            {
                { BACK, backConfig },
                { FORWARD, forwardConfig },
            };
            var rightBreast = new Dictionary<string, DynamicPhysicsConfig>
            {
                { BACK, backConfig },
                { FORWARD, forwardConfig },
            };
            return new Dictionary<string, Dictionary<string, DynamicPhysicsConfig>>
            {
                { Side.LEFT, leftBreast },
                { Side.RIGHT, rightBreast },
            };
        }

        private static Dictionary<string, Dictionary<string, DynamicPhysicsConfig>> TargetRotationXConfigs()
        {
            var upConfig = new DynamicPhysicsConfig(
                massMultiplier: 0,
                softnessMultiplier: 16.50f,
                applyMethod: ApplyMethod.ADDITIVE,
                softnessCurve: Curves.TargetRotationSoftnessCurve
            )
            {
                baseMultiplier = 2.36f,
                negative = false,
            };
            var downConfig = new DynamicPhysicsConfig(
                massMultiplier: 0,
                softnessMultiplier: -14.00f,
                applyMethod: ApplyMethod.ADDITIVE,
                softnessCurve: Curves.TargetRotationSoftnessCurve
            )
            {
                baseMultiplier = -2.00f,
                negative = true,
            };
            var leftBreast = new Dictionary<string, DynamicPhysicsConfig>
            {
                { UP, upConfig },
                { DOWN, downConfig },
            };
            var rightBreast = new Dictionary<string, DynamicPhysicsConfig>
            {
                { UP, upConfig },
                { DOWN, downConfig },
            };
            return new Dictionary<string, Dictionary<string, DynamicPhysicsConfig>>
            {
                { Side.LEFT, leftBreast },
                { Side.RIGHT, rightBreast },
            };
        }

        private static Dictionary<string, Dictionary<string, DynamicPhysicsConfig>> TargetRotationYConfigs()
        {
            var leftConfig = new DynamicPhysicsConfig(
                massMultiplier: 0,
                softnessMultiplier: -16.50f,
                applyMethod: ApplyMethod.ADDITIVE,
                softnessCurve: Curves.TargetRotationSoftnessCurve
            )
            {
                baseMultiplier = -2.36f,
                negative = true,
            };
            var rightConfig = new DynamicPhysicsConfig(
                massMultiplier: 0,
                softnessMultiplier: 16.50f,
                applyMethod: ApplyMethod.ADDITIVE,
                softnessCurve: Curves.TargetRotationSoftnessCurve
            )
            {
                baseMultiplier = 2.36f,
                negative = false,
            };
            var leftBreast = new Dictionary<string, DynamicPhysicsConfig>
            {
                { LEFT, leftConfig },
                { RIGHT, rightConfig },
            };
            var rightBreast = new Dictionary<string, DynamicPhysicsConfig>
            {
                { LEFT, leftConfig },
                { RIGHT, rightConfig },
            };
            return new Dictionary<string, Dictionary<string, DynamicPhysicsConfig>>
            {
                { Side.LEFT, leftBreast },
                { Side.RIGHT, rightBreast },
            };
        }

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
        }
    }
}
