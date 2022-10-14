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
            _paramGroups = MainPhysicsHandler.parameterGroups.Values.ToList();
        }

        private static Dictionary<string, DynamicPhysicsConfig> NewSpringConfigs() =>
            new Dictionary<string, DynamicPhysicsConfig>
            {
                {
                    Direction.UP, new DynamicPhysicsConfig(
                        massMultiplier: -4.0f,
                        softnessMultiplier: -16.0f,
                        negative: true,
                        applyMethod: ApplyMethod.ADDITIVE,
                        massCurve: MainPhysicsHandler.InvertMass
                    )
                },
                {
                    Direction.BACK, new DynamicPhysicsConfig(
                        massMultiplier: -4.0f,
                        softnessMultiplier: -16.0f,
                        negative: true,
                        applyMethod: ApplyMethod.ADDITIVE,
                        massCurve: MainPhysicsHandler.InvertMass
                    )
                },
                {
                    Direction.FORWARD, new DynamicPhysicsConfig(
                        massMultiplier: -4.0f,
                        softnessMultiplier: -16.0f,
                        negative: true,
                        applyMethod: ApplyMethod.ADDITIVE,
                        massCurve: MainPhysicsHandler.InvertMass
                    )
                },
                {
                    Direction.LEFT, new DynamicPhysicsConfig(
                        massMultiplier: -4.0f,
                        softnessMultiplier: -16.0f,
                        negative: true,
                        applyMethod: ApplyMethod.ADDITIVE,
                        massCurve: MainPhysicsHandler.InvertMass
                    )
                },
                {
                    Direction.RIGHT, new DynamicPhysicsConfig(
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
                    Direction.UP, new DynamicPhysicsConfig(
                        massMultiplier: 0.25f,
                        softnessMultiplier: 1.00f,
                        negative: false,
                        applyMethod: ApplyMethod.ADDITIVE,
                        massCurve: MainPhysicsHandler.InvertMass
                    )
                },
                {
                    Direction.BACK, new DynamicPhysicsConfig(
                        massMultiplier: 0.25f,
                        softnessMultiplier: 1.00f,
                        negative: false,
                        applyMethod: ApplyMethod.ADDITIVE,
                        massCurve: MainPhysicsHandler.InvertMass
                    )
                },
                {
                    Direction.FORWARD, new DynamicPhysicsConfig(
                        massMultiplier: 0.25f,
                        softnessMultiplier: 1.00f,
                        negative: false,
                        applyMethod: ApplyMethod.ADDITIVE,
                        massCurve: MainPhysicsHandler.InvertMass
                    )
                },
                {
                    Direction.LEFT, new DynamicPhysicsConfig(
                        massMultiplier: 0.25f,
                        softnessMultiplier: 1.00f,
                        negative: false,
                        applyMethod: ApplyMethod.ADDITIVE,
                        massCurve: MainPhysicsHandler.InvertMass
                    )
                },
                {
                    Direction.RIGHT, new DynamicPhysicsConfig(
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
                    Direction.BACK, new DynamicPhysicsConfig(
                        massMultiplier: -230f,
                        softnessMultiplier: -170f,
                        negative: true,
                        applyMethod: ApplyMethod.ADDITIVE,
                        massCurve: MainPhysicsHandler.InvertMass,
                        softnessCurve: Curves.SpringZSoftnessCurve
                    )
                },
                {
                    Direction.FORWARD, new DynamicPhysicsConfig(
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
                    Direction.DOWN, new DynamicPhysicsConfig(
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
                    Direction.UP, new DynamicPhysicsConfig(
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
                    Direction.LEFT, new DynamicPhysicsConfig(
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
                    Direction.RIGHT, new DynamicPhysicsConfig(
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

        public static void Update()
        {
            var rotationLeft = tittyMagic.pectoralRbLeft.rotation;
            var rotationRight = tittyMagic.pectoralRbRight.rotation;
            float rollLeft = Calc.SmoothStep(Calc.Roll(rotationLeft));
            float rollRight = Calc.SmoothStep(-Calc.Roll(rotationRight));
            float pitchLeft = 2 * Calc.SmoothStep(Calc.Pitch(rotationLeft));
            float pitchRight = 2 * Calc.SmoothStep(Calc.Pitch(rotationRight));

            // for some reason, if left right is adjusted after down, down physics is not correctly applied
            AdjustLeftRightPhysics(rollLeft, rollRight);
            AdjustUpPhysics(pitchLeft, pitchRight, rollLeft, rollRight);
            AdjustDownPhysics(pitchLeft, pitchRight, rollLeft, rollRight);
            AdjustForwardPhysics(pitchLeft, pitchRight, rollLeft, rollRight);
            AdjustBackPhysics(pitchLeft, pitchRight, rollLeft, rollRight);
        }

        private static void AdjustLeftRightPhysics(float rollLeft, float rollRight)
        {
            float effectLeft = GravityEffectCalc.RollEffect(rollLeft, leftRightMultiplier);
            float effectRight = GravityEffectCalc.RollEffect(rollRight, leftRightMultiplier);
            if(rollLeft >= 0)
            {
                // left
                ResetPhysics(Direction.RIGHT);
                UpdatePhysics(Direction.LEFT, effectLeft, effectRight);
            }
            else
            {
                // right
                ResetPhysics(Direction.LEFT);
                UpdatePhysics(Direction.RIGHT, effectLeft, effectRight);
            }
        }

        private static void AdjustUpPhysics(float pitchLeft, float pitchRight, float rollLeft, float rollRight)
        {
            float effectLeft = GravityEffectCalc.UpDownEffect(pitchLeft, rollLeft, upMultiplier);
            float effectRight = GravityEffectCalc.UpDownEffect(pitchRight, rollRight, upMultiplier);
            if(pitchLeft >= 1)
            {
                // leaning forward,  upside down
                UpdatePhysics(Direction.UP, effectLeft, effectRight);
            }
            else if(pitchLeft < -1)
            {
                // leaning back, upside down
                UpdatePhysics(Direction.UP, effectLeft, effectRight);
            }
            else
            {
                ResetPhysics(Direction.UP);
            }
        }

        private static void AdjustDownPhysics(float pitchLeft, float pitchRight, float rollLeft, float rollRight)
        {
            float effectLeft = GravityEffectCalc.UpDownEffect(pitchLeft, rollLeft, downMultiplier);
            float effectRight = GravityEffectCalc.UpDownEffect(pitchRight, rollRight, downMultiplier);
            if(pitchLeft >= 0 && pitchLeft < 1)
            {
                // leaning forward, upright
                UpdatePhysics(Direction.DOWN, effectLeft, effectRight);
            }
            else if(pitchLeft >= -1 && pitchLeft < 0)
            {
                // leaning back
                UpdatePhysics(Direction.DOWN, effectLeft, effectRight);
            }
            else
            {
                ResetPhysics(Direction.DOWN);
            }
        }

        private static void AdjustForwardPhysics(float pitchLeft, float pitchRight, float rollLeft, float rollRight)
        {
            float effectLeft = GravityEffectCalc.DepthEffect(pitchLeft, rollLeft, forwardMultiplier);
            float effectRight = GravityEffectCalc.DepthEffect(pitchRight, rollRight, forwardMultiplier);
            if(pitchLeft >= 0)
            {
                // leaning forward
                if(pitchLeft < 1)
                {
                    // upright
                    UpdatePhysics(Direction.FORWARD, effectLeft, effectRight);
                }
                else
                {
                    // upside down
                    UpdatePhysics(Direction.FORWARD, effectLeft, effectRight);
                }
            }
            else
            {
                // leaning back
                ResetPhysics(Direction.FORWARD);
            }
        }

        private static void AdjustBackPhysics(float pitchLeft, float pitchRight, float rollLeft, float rollRight)
        {
            float effectLeft = GravityEffectCalc.DepthEffect(pitchLeft, rollLeft, backMultiplier);
            float effectRight = GravityEffectCalc.DepthEffect(pitchRight, rollRight, backMultiplier);
            if(pitchLeft < 0)
            {
                // leaning back
                if(pitchLeft >= -1)
                {
                    // upright
                    UpdatePhysics(Direction.BACK, effectLeft, effectRight);
                }
                else
                {
                    // upside down
                    UpdatePhysics(Direction.BACK, effectLeft, effectRight);
                }
            }
            else
            {
                ResetPhysics(Direction.BACK);
            }
        }

        private static void UpdatePhysics(string direction, float effectLeft, float effectRight)
        {
            float mass = MainPhysicsHandler.massAmount;
            float softness = tittyMagic.softnessAmount;
            _paramGroups.ForEach(paramGroup =>
                paramGroup.UpdateGravityValue(direction, effectLeft, effectRight, mass, softness)
            );
        }

        public static void SimulateUpright()
        {
            const float rollLeft = 0;
            const float rollRight = 0;
            const float pitchLeft = 0;
            const float pitchRight = 0;

            AdjustLeftRightPhysics(rollLeft, rollRight);
            AdjustUpPhysics(pitchLeft, pitchRight, rollLeft, rollRight);
            AdjustDownPhysics(pitchLeft, pitchRight, rollLeft, rollRight);
            AdjustForwardPhysics(pitchLeft, pitchRight, rollLeft, rollRight);
            AdjustBackPhysics(pitchLeft, pitchRight, rollLeft, rollRight);
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
