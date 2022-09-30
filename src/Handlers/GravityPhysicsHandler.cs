using System.Collections.Generic;
using TittyMagic.Handlers.Configs;
using TittyMagic.Models;
using static TittyMagic.Script;
using static TittyMagic.ParamName;

namespace TittyMagic.Handlers
{
    public static class GravityPhysicsHandler
    {
        private static List<PhysicsParameterGroup> _paramGroups;

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

            baseJsf.setCallbackFunction = _ => tittyMagic.calibration.shouldRun = true;
            upJsf.setCallbackFunction = _ => tittyMagic.calibration.shouldRun = true;
            downJsf.setCallbackFunction = _ => tittyMagic.calibration.shouldRun = true;
            forwardJsf.setCallbackFunction = _ => tittyMagic.calibration.shouldRun = true;
            backJsf.setCallbackFunction = _ => tittyMagic.calibration.shouldRun = true;
            leftRightJsf.setCallbackFunction = _ => tittyMagic.calibration.shouldRun = true;
        }

        public static void LoadSettings()
        {
            var paramGroups = MainPhysicsHandler.parameterGroups;
            paramGroups[SPRING].SetGravityPhysicsConfigs(NewSpringConfigs(), NewSpringConfigs());
            paramGroups[DAMPER].SetGravityPhysicsConfigs(NewDamperConfigs(), NewDamperConfigs());
            paramGroups[POSITION_SPRING_Z].SetGravityPhysicsConfigs(NewPositionSpringZConfigs(), NewPositionSpringZConfigs());
            paramGroups[TARGET_ROTATION_X].SetGravityPhysicsConfigs(NewPositionTargetRotationXConfigs(), NewPositionTargetRotationXConfigs());
            paramGroups[TARGET_ROTATION_Y].SetGravityPhysicsConfigs(NewPositionTargetRotationYConfigs(), NewPositionTargetRotationYConfigs());
            _paramGroups = MainPhysicsHandler.parameterGroups.Values.ToList();
        }

        private static Dictionary<string, DynamicPhysicsConfig> NewSpringConfigs() =>
            new Dictionary<string, DynamicPhysicsConfig>
            {
                {
                    Direction.UP, new DynamicPhysicsConfig(
                        massMultiplier: -6.4f,
                        softnessMultiplier: -25.6f,
                        isNegative: true,
                        applyMethod: ApplyMethod.ADDITIVE,
                        massCurve: MainPhysicsHandler.InvertMass
                    )
                },
                {
                    Direction.BACK, new DynamicPhysicsConfig(
                        massMultiplier: -8.5f,
                        softnessMultiplier: -36f,
                        isNegative: true,
                        applyMethod: ApplyMethod.ADDITIVE,
                        massCurve: MainPhysicsHandler.InvertMass
                    )
                },
                {
                    Direction.FORWARD, new DynamicPhysicsConfig(
                        massMultiplier: -4.0f,
                        softnessMultiplier: -16.0f,
                        isNegative: true,
                        applyMethod: ApplyMethod.ADDITIVE,
                        massCurve: MainPhysicsHandler.InvertMass
                    )
                },
                {
                    Direction.LEFT, new DynamicPhysicsConfig(
                        massMultiplier: -6.4f,
                        softnessMultiplier: -25.6f,
                        isNegative: true,
                        applyMethod: ApplyMethod.ADDITIVE,
                        massCurve: MainPhysicsHandler.InvertMass
                    )
                },
                {
                    Direction.RIGHT, new DynamicPhysicsConfig(
                        massMultiplier: -6.4f,
                        softnessMultiplier: -25.6f,
                        isNegative: true,
                        applyMethod: ApplyMethod.ADDITIVE,
                        massCurve: MainPhysicsHandler.InvertMass
                    )
                },
            };

        private static Dictionary<string, DynamicPhysicsConfig> NewDamperConfigs() =>
            new Dictionary<string, DynamicPhysicsConfig>
            {
                {
                    Direction.UP, new DynamicPhysicsConfig(
                        massMultiplier: 0.25f,
                        softnessMultiplier: 1.00f,
                        isNegative: false,
                        applyMethod: ApplyMethod.ADDITIVE,
                        massCurve: MainPhysicsHandler.InvertMass
                    )
                },
                {
                    Direction.BACK, new DynamicPhysicsConfig(
                        massMultiplier: 0.25f,
                        softnessMultiplier: 1.00f,
                        isNegative: false,
                        applyMethod: ApplyMethod.ADDITIVE,
                        massCurve: MainPhysicsHandler.InvertMass
                    )
                },
                {
                    Direction.FORWARD, new DynamicPhysicsConfig(
                        massMultiplier: 0.25f,
                        softnessMultiplier: 1.00f,
                        isNegative: false,
                        applyMethod: ApplyMethod.ADDITIVE,
                        massCurve: MainPhysicsHandler.InvertMass
                    )
                },
                {
                    Direction.LEFT, new DynamicPhysicsConfig(
                        massMultiplier: 0.25f,
                        softnessMultiplier: 1.00f,
                        isNegative: false,
                        applyMethod: ApplyMethod.ADDITIVE,
                        massCurve: MainPhysicsHandler.InvertMass
                    )
                },
                {
                    Direction.RIGHT, new DynamicPhysicsConfig(
                        massMultiplier: 0.25f,
                        softnessMultiplier: 1.00f,
                        isNegative: false,
                        applyMethod: ApplyMethod.ADDITIVE,
                        massCurve: MainPhysicsHandler.InvertMass
                    )
                },
            };

        private static Dictionary<string, DynamicPhysicsConfig> NewPositionSpringZConfigs() =>
            new Dictionary<string, DynamicPhysicsConfig>
            {
                {
                    Direction.BACK, new DynamicPhysicsConfig(
                        massMultiplier: -230f,
                        softnessMultiplier: -170f,
                        isNegative: true,
                        applyMethod: ApplyMethod.ADDITIVE,
                        massCurve: MainPhysicsHandler.InvertMass,
                        softnessCurve: Curves.SpringZSoftnessCurve
                    )
                },
                {
                    Direction.FORWARD, new DynamicPhysicsConfig(
                        massMultiplier: -230f,
                        softnessMultiplier: -170f,
                        isNegative: true,
                        applyMethod: ApplyMethod.ADDITIVE,
                        massCurve: MainPhysicsHandler.InvertMass,
                        softnessCurve: Curves.SpringZSoftnessCurve
                    )
                },
            };

        private static Dictionary<string, DynamicPhysicsConfig> NewPositionTargetRotationXConfigs() =>
            new Dictionary<string, DynamicPhysicsConfig>
            {
                {
                    Direction.DOWN, new DynamicPhysicsConfig(
                        massMultiplier: -7.20f,
                        softnessMultiplier: -11.16f,
                        isNegative: true,
                        applyMethod: ApplyMethod.ADDITIVE,
                        massCurve: x => 1.5f * Curves.DownTargetRotationMassCurve(x),
                        softnessCurve: Curves.TargetRotationSoftnessCurve
                    )
                },
                {
                    Direction.UP, new DynamicPhysicsConfig(
                        massMultiplier: 9.00f,
                        softnessMultiplier: 14.00f,
                        isNegative: false,
                        applyMethod: ApplyMethod.ADDITIVE,
                        massCurve: x => 1.5f * Curves.TargetRotationMassCurve(x),
                        softnessCurve: Curves.TargetRotationSoftnessCurve
                    )
                },
            };

        private static Dictionary<string, DynamicPhysicsConfig> NewPositionTargetRotationYConfigs() =>
            new Dictionary<string, DynamicPhysicsConfig>
            {
                {
                    Direction.LEFT, new DynamicPhysicsConfig(
                        massMultiplier: -7.20f,
                        softnessMultiplier: -11.16f,
                        isNegative: true,
                        applyMethod: ApplyMethod.ADDITIVE,
                        massCurve: x => 1.5f * Curves.TargetRotationMassCurve(x),
                        softnessCurve: Curves.TargetRotationSoftnessCurve
                    )
                },
                {
                    Direction.RIGHT, new DynamicPhysicsConfig(
                        massMultiplier: 7.20f,
                        softnessMultiplier: 11.16f,
                        isNegative: false,
                        applyMethod: ApplyMethod.ADDITIVE,
                        massCurve: x => 1.5f * Curves.TargetRotationMassCurve(x),
                        softnessCurve: Curves.TargetRotationSoftnessCurve
                    )
                },
            };

        public static void Update(float roll, float pitch)
        {
            float smoothRoll = Calc.SmoothStep(roll);
            float smoothPitch = 2 * Calc.SmoothStep(pitch);

            // for some reason, if left right is adjusted after down, down physics is not correctly applied
            AdjustLeftRightPhysics(smoothRoll);
            AdjustUpPhysics(smoothPitch, smoothRoll);
            AdjustDownPhysics(smoothPitch, smoothRoll);
            AdjustForwardPhysics(smoothPitch, smoothRoll);
            AdjustBackPhysics(smoothPitch, smoothRoll);
        }

        private static void AdjustLeftRightPhysics(float roll)
        {
            float effect = GravityEffectCalc.CalculateRollEffect(roll, leftRightMultiplier);
            if(roll >= 0)
            {
                // left
                ResetPhysics(Direction.RIGHT);
                UpdatePhysics(Direction.LEFT, effect);
            }
            else
            {
                // right
                ResetPhysics(Direction.LEFT);
                UpdatePhysics(Direction.RIGHT, effect);
            }
        }

        private static void AdjustUpPhysics(float pitch, float roll)
        {
            float effect = GravityEffectCalc.CalculateUpDownEffect(pitch, roll, upMultiplier);
            if(pitch >= 1)
            {
                // leaning forward,  upside down
                UpdatePhysics(Direction.UP, effect);
            }
            else if(pitch < -1)
            {
                // leaning back, upside down
                UpdatePhysics(Direction.UP, effect);
            }
            else
            {
                ResetPhysics(Direction.UP);
            }
        }

        private static void AdjustDownPhysics(float pitch, float roll)
        {
            float effect = GravityEffectCalc.CalculateUpDownEffect(pitch, roll, downMultiplier);
            if(pitch >= 0 && pitch < 1)
            {
                // leaning forward, upright
                UpdatePhysics(Direction.DOWN, effect);
            }
            else if(pitch >= -1 && pitch < 0)
            {
                // leaning back
                UpdatePhysics(Direction.DOWN, effect);
            }
            else
            {
                ResetPhysics(Direction.DOWN);
            }
        }

        private static void AdjustForwardPhysics(float pitch, float roll)
        {
            float effect = GravityEffectCalc.CalculateDepthEffect(pitch, roll, forwardMultiplier);
            if(pitch >= 0)
            {
                // leaning forward
                if(pitch < 1)
                {
                    // upright
                    UpdatePhysics(Direction.FORWARD, effect);
                }
                else
                {
                    // upside down
                    UpdatePhysics(Direction.FORWARD, effect);
                }
            }
            else
            {
                // leaning back
                ResetPhysics(Direction.FORWARD);
            }
        }

        private static void AdjustBackPhysics(float pitch, float roll)
        {
            float effect = GravityEffectCalc.CalculateDepthEffect(pitch, roll, backMultiplier);
            if(pitch < 0)
            {
                // leaning back
                if(pitch >= -1)
                {
                    // upright
                    UpdatePhysics(Direction.BACK, effect);
                }
                else
                {
                    // upside down
                    UpdatePhysics(Direction.BACK, effect);
                }
            }
            else
            {
                ResetPhysics(Direction.BACK);
            }
        }

        private static void UpdatePhysics(string direction, float effect)
        {
            float mass = MainPhysicsHandler.massAmount;
            float softness = tittyMagic.softnessAmount;
            _paramGroups.ForEach(paramGroup =>
                paramGroup.UpdateGravityValue(direction, effect, mass, softness)
            );
        }

        private static void ResetPhysics(string direction) =>
            _paramGroups.ForEach(paramGroup => paramGroup.ResetGravityValue(direction));

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
