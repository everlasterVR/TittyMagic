using System.Collections.Generic;
using TittyMagic.Configs;
using static TittyMagic.ParamName;
using static TittyMagic.GravityEffectCalc;

namespace TittyMagic
{
    internal class GravityPhysicsHandler
    {
        private readonly Script _script;
        private List<PhysicsParameterGroup> _paramGroups;

        public JSONStorableFloat baseJsf { get; }
        public JSONStorableFloat upJsf { get; }
        public JSONStorableFloat downJsf { get; }
        public JSONStorableFloat forwardJsf { get; }
        public JSONStorableFloat backJsf { get; }
        public JSONStorableFloat leftRightJsf { get; }

        private float upMultiplier => baseJsf.val * upJsf.val;
        public float downMultiplier => baseJsf.val * downJsf.val;
        private float forwardMultiplier => baseJsf.val * forwardJsf.val;
        private float backMultiplier => baseJsf.val * backJsf.val;
        private float leftRightMultiplier => baseJsf.val * leftRightJsf.val;

        public GravityPhysicsHandler(Script script)
        {
            _script = script;

            baseJsf = script.NewJSONStorableFloat("gravityPhysicsBase", 1.00f, 0.00f, 2.00f);
            upJsf = script.NewJSONStorableFloat("gravityPhysicsUp", 1.00f, 0.00f, 2.00f);
            downJsf = script.NewJSONStorableFloat("gravityPhysicsDown", 1.00f, 0.00f, 2.00f);
            forwardJsf = script.NewJSONStorableFloat("gravityPhysicsForward", 1.00f, 0.00f, 2.00f);
            backJsf = script.NewJSONStorableFloat("gravityPhysicsBack", 1.00f, 0.00f, 2.00f);
            leftRightJsf = script.NewJSONStorableFloat("gravityPhysicsLeftRight", 1.00f, 0.00f, 2.00f);

            baseJsf.setCallbackFunction = _ => _script.recalibrationNeeded = true;
            upJsf.setCallbackFunction = _ => _script.recalibrationNeeded = true;
            downJsf.setCallbackFunction = _ => _script.recalibrationNeeded = true;
            forwardJsf.setCallbackFunction = _ => _script.recalibrationNeeded = true;
            backJsf.setCallbackFunction = _ => _script.recalibrationNeeded = true;
            leftRightJsf.setCallbackFunction = _ => _script.recalibrationNeeded = true;
        }

        public void LoadSettings()
        {
            SetupGravityPhysicsConfigs();
            _paramGroups = _script.mainPhysicsHandler.parameterGroups.Values.ToList();
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

        private void SetupGravityPhysicsConfigs()
        {
            var paramGroups = _script.mainPhysicsHandler.parameterGroups;
            paramGroups[SPRING].SetGravityPhysicsConfigs(NewSpringConfigs(), NewSpringConfigs());
            paramGroups[POSITION_SPRING_Z].SetGravityPhysicsConfigs(NewPositionSpringZConfigs(), NewPositionSpringZConfigs());
            paramGroups[TARGET_ROTATION_X].SetGravityPhysicsConfigs(NewPositionTargetRotationXConfigs(), NewPositionTargetRotationXConfigs());
            paramGroups[TARGET_ROTATION_Y].SetGravityPhysicsConfigs(NewPositionTargetRotationYConfigs(), NewPositionTargetRotationYConfigs());
        }

        public void Update(float roll, float pitch)
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

        private void AdjustLeftRightPhysics(float roll)
        {
            float effect = CalculateRollEffect(roll, leftRightMultiplier);
            // left
            if(roll >= 0)
            {
                ResetPhysics(Direction.RIGHT);
                UpdatePhysics(Direction.LEFT, effect);
            }
            // right
            else
            {
                ResetPhysics(Direction.LEFT);
                UpdatePhysics(Direction.RIGHT, effect);
            }
        }

        private void AdjustUpPhysics(float pitch, float roll)
        {
            float effect = CalculateUpDownEffect(pitch, roll, upMultiplier);
            // leaning forward,  upside down
            if(pitch >= 1)
            {
                UpdatePhysics(Direction.UP, effect);
            }
            // leaning back, upside down
            else if(pitch < -1)
            {
                UpdatePhysics(Direction.UP, effect);
            }
            else
            {
                ResetPhysics(Direction.UP);
            }
        }

        private void AdjustDownPhysics(float pitch, float roll)
        {
            float effect = CalculateUpDownEffect(pitch, roll, downMultiplier);
            // leaning forward, upright
            if(pitch >= 0 && pitch < 1)
            {
                UpdatePhysics(Direction.DOWN, effect);
            }
            // leaning back
            else if(pitch >= -1 && pitch < 0)
            {
                UpdatePhysics(Direction.DOWN, effect);
            }
            else
            {
                ResetPhysics(Direction.DOWN);
            }
        }

        private void AdjustForwardPhysics(float pitch, float roll)
        {
            float effect = CalculateDepthEffect(pitch, roll, forwardMultiplier);
            // leaning forward
            if(pitch >= 0)
            {
                // upright
                if(pitch < 1)
                {
                    UpdatePhysics(Direction.FORWARD, effect);
                }
                // upside down
                else
                {
                    UpdatePhysics(Direction.FORWARD, effect);
                }
            }
            else
            {
                ResetPhysics(Direction.FORWARD);
            }
        }

        private void AdjustBackPhysics(float pitch, float roll)
        {
            float effect = CalculateDepthEffect(pitch, roll, backMultiplier);
            // leaning back
            if(pitch < 0)
            {
                // upright
                if(pitch >= -1)
                {
                    UpdatePhysics(Direction.BACK, effect);
                }
                // upside down
                else
                {
                    UpdatePhysics(Direction.BACK, effect);
                }
            }
            else
            {
                ResetPhysics(Direction.BACK);
            }
        }

        private void UpdatePhysics(string direction, float effect)
        {
            float mass = _script.mainPhysicsHandler.massAmount;
            float softness = _script.softnessAmount;
            _paramGroups.ForEach(paramGroup =>
                paramGroup.UpdateGravityValue(direction, effect, mass, softness)
            );
        }

        private void ResetPhysics(string direction) =>
            _paramGroups.ForEach(paramGroup => paramGroup.ResetGravityValue(direction));
    }
}
