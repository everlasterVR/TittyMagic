using System.Collections.Generic;
using TittyMagic.Configs;
using UnityEngine;
using static TittyMagic.ParamName;
using static TittyMagic.GravityEffectCalc;

namespace TittyMagic
{
    internal class ForcePhysicsHandler
    {
        private readonly Script _script;

        private List<PhysicsParameterGroup> _mainParamGroups;

        private readonly TrackNipple _trackLeftNipple;
        private readonly TrackNipple _trackRightNipple;

        private float _pitchMultiplier;
        private float _rollMultiplier;

        public JSONStorableFloat baseJsf { get; }
        public JSONStorableFloat upJsf { get; }
        public JSONStorableFloat downJsf { get; }
        public JSONStorableFloat forwardJsf { get; }
        public JSONStorableFloat backJsf { get; }
        public JSONStorableFloat leftRightJsf { get; }

        public float upDownExtraMultiplier { get; set; }
        public float forwardExtraMultiplier { get; set; }
        public float backExtraMultiplier { get; set; }
        public float leftRightExtraMultiplier { get; set; }

        private float upMultiplier => baseJsf.val * upJsf.val;
        public float downMultiplier => baseJsf.val * downJsf.val;
        private float forwardMultiplier => baseJsf.val * forwardJsf.val;
        private float backMultiplier => baseJsf.val * backJsf.val;
        private float leftRightMultiplier => baseJsf.val * leftRightJsf.val;

        public ForcePhysicsHandler(
            Script script,
            TrackNipple trackLeftNipple,
            TrackNipple trackRightNipple
        )
        {
            _script = script;
            _trackLeftNipple = trackLeftNipple;
            _trackRightNipple = trackRightNipple;

            baseJsf = script.NewJSONStorableFloat("forcePhysicsBase", 1.00f, 0.00f, 2.00f);
            upJsf = script.NewJSONStorableFloat("forcePhysicsUp", 1.00f, 0.00f, 2.00f);
            downJsf = script.NewJSONStorableFloat("forcePhysicsDown", 1.00f, 0.00f, 2.00f);
            forwardJsf = script.NewJSONStorableFloat("forcePhysicsForward", 1.00f, 0.00f, 2.00f);
            backJsf = script.NewJSONStorableFloat("forcePhysicsBack", 1.00f, 0.00f, 2.00f);
            leftRightJsf = script.NewJSONStorableFloat("forcePhysicsLeftRight", 1.00f, 0.00f, 2.00f);
        }

        public void LoadSettings()
        {
            SetupMainForcePhysicsConfigs();
            _mainParamGroups = _script.mainPhysicsHandler.parameterGroups.Values.ToList();
        }

        private static Dictionary<string, DynamicPhysicsConfig> NewCenterOfGravityConfigs() =>
            new Dictionary<string, DynamicPhysicsConfig>
            {
                {
                    Direction.BACK, new DynamicPhysicsConfig(
                        -0.280f,
                        -0.420f,
                        isNegative: true,
                        multiplyInvertedMass: true,
                        applyMethod: ApplyMethod.ADDITIVE
                    )
                },
                {
                    Direction.FORWARD, new DynamicPhysicsConfig(
                        0.280f,
                        0.420f,
                        isNegative: false,
                        multiplyInvertedMass: true,
                        applyMethod: ApplyMethod.ADDITIVE
                    )
                },
            };

        private static Dictionary<string, DynamicPhysicsConfig> NewSpringConfigs() =>
            new Dictionary<string, DynamicPhysicsConfig>
            {
                {
                    Direction.UP, new DynamicPhysicsConfig(
                        -72.0f,
                        -12f,
                        isNegative: true,
                        multiplyInvertedMass: false,
                        applyMethod: ApplyMethod.ADDITIVE
                    )
                },
                {
                    Direction.BACK, new DynamicPhysicsConfig(
                        -24.0f,
                        -4f,
                        isNegative: true,
                        multiplyInvertedMass: false,
                        applyMethod: ApplyMethod.ADDITIVE
                    )
                },
                {
                    Direction.FORWARD, new DynamicPhysicsConfig(
                        -36.0f,
                        -6f,
                        isNegative: true,
                        multiplyInvertedMass: false,
                        applyMethod: ApplyMethod.ADDITIVE
                    )
                },
                {
                    Direction.LEFT, new DynamicPhysicsConfig(
                        -48.0f,
                        -8f,
                        isNegative: true,
                        multiplyInvertedMass: false,
                        applyMethod: ApplyMethod.ADDITIVE
                    )
                },
                {
                    Direction.RIGHT, new DynamicPhysicsConfig(
                        -48.0f,
                        -8f,
                        isNegative: true,
                        multiplyInvertedMass: false,
                        applyMethod: ApplyMethod.ADDITIVE
                    )
                },
            };

        private static Dictionary<string, DynamicPhysicsConfig> NewPositionDamperZConfigs() =>
            new Dictionary<string, DynamicPhysicsConfig>
            {
                {
                    Direction.BACK, new DynamicPhysicsConfig(
                        -12f,
                        0f,
                        isNegative: true,
                        multiplyInvertedMass: true,
                        applyMethod: ApplyMethod.ADDITIVE
                    )
                },
                {
                    Direction.FORWARD, new DynamicPhysicsConfig(
                        -12f,
                        0f,
                        isNegative: false,
                        multiplyInvertedMass: false,
                        applyMethod: ApplyMethod.ADDITIVE
                    )
                },
            };

        private void SetupMainForcePhysicsConfigs()
        {
            var paramGroups = _script.mainPhysicsHandler.parameterGroups;
            paramGroups[CENTER_OF_GRAVITY_PERCENT].SetForcePhysicsConfigs(NewCenterOfGravityConfigs(), NewCenterOfGravityConfigs());
            paramGroups[SPRING].SetForcePhysicsConfigs(NewSpringConfigs(), NewSpringConfigs());
            paramGroups[POSITION_DAMPER_Z].SetForcePhysicsConfigs(NewPositionDamperZConfigs(), NewPositionDamperZConfigs());
        }

        public void Update()
        {
            _rollMultiplier = 1;
            _pitchMultiplier = 1;

            AdjustLeftRightPhysics();
            AdjustUpPhysics();
            AdjustDownPhysics();
            AdjustForwardPhysics();
            AdjustBackPhysics();
        }

        private void AdjustLeftRightPhysics()
        {
            float multiplier = 0.5f * Curves.QuadraticRegression(leftRightMultiplier) * leftRightExtraMultiplier;
            float effectXLeft = CalculateXEffect(_trackLeftNipple.angleX, multiplier);
            float effectXRight = CalculateXEffect(_trackRightNipple.angleX, multiplier);

            // left force on left breast
            if(_trackLeftNipple.angleX >= 0)
            {
                ResetLeftPhysics(Direction.LEFT);
                UpdateLeftPhysics(Direction.RIGHT, effectXLeft);
            }
            // right force on left breast
            else
            {
                ResetLeftPhysics(Direction.RIGHT);
                UpdateLeftPhysics(Direction.RIGHT, effectXLeft);
            }

            // // left force on right breast
            if(_trackRightNipple.angleX >= 0)
            {
                ResetRightPhysics(Direction.LEFT);
                UpdateRightPhysics(Direction.RIGHT, effectXRight);
            }
            // right force on right breast
            else
            {
                ResetRightPhysics(Direction.RIGHT);
                UpdateRightPhysics(Direction.LEFT, effectXRight);
            }
        }

        private void AdjustUpPhysics()
        {
            float multiplier = 0.5f * Curves.QuadraticRegression(upMultiplier) * upDownExtraMultiplier;
            float effectYLeft = CalculateYEffect(_trackLeftNipple.angleY, multiplier);
            float effectYRight = CalculateYEffect(_trackRightNipple.angleY, multiplier);

            // up force on left breast
            if(_trackLeftNipple.angleY >= 0)
            {
                UpdateLeftPhysics(Direction.UP, effectYLeft);
            }
            else
            {
                ResetLeftPhysics(Direction.UP);
            }

            // up force on right breast
            if(_trackRightNipple.angleY >= 0)
            {
                UpdateRightPhysics(Direction.UP, effectYRight);
            }
            else
            {
                ResetRightPhysics(Direction.UP);
            }
        }

        private void AdjustDownPhysics()
        {
            float multiplier = 0.5f * Curves.QuadraticRegression(downMultiplier) * upDownExtraMultiplier;
            float effectYLeft = CalculateYEffect(_trackLeftNipple.angleY, multiplier);
            float effectYRight = CalculateYEffect(_trackRightNipple.angleY, multiplier);

            // down force on left breast
            if(_trackLeftNipple.angleY < 0)
            {
                UpdateLeftPhysics(Direction.DOWN, effectYLeft);
            }
            else
            {
                ResetLeftPhysics(Direction.DOWN);
            }

            // down force on right breast
            if(_trackRightNipple.angleY < 0)
            {
                UpdateRightPhysics(Direction.DOWN, effectYRight);
            }
            else
            {
                ResetRightPhysics(Direction.DOWN);
            }
        }

        private void AdjustForwardPhysics()
        {
            float multiplier = Curves.QuadraticRegression(forwardMultiplier) * forwardExtraMultiplier;
            float effectZLeft = CalculateZEffect(_trackLeftNipple.depthDiff, multiplier);
            float effectZRight = CalculateZEffect(_trackRightNipple.depthDiff, multiplier);

            // forward force on left breast
            if(_trackLeftNipple.depthDiff <= 0)
            {
                UpdateLeftPhysics(Direction.FORWARD, effectZLeft);
            }
            else
            {
                ResetLeftPhysics(Direction.FORWARD);
            }

            // forward force on right breast
            if(_trackRightNipple.depthDiff <= 0)
            {
                UpdateRightPhysics(Direction.FORWARD, effectZRight);
            }
            else
            {
                ResetRightPhysics(Direction.FORWARD);
            }
        }

        private void AdjustBackPhysics()
        {
            float multiplier = Curves.QuadraticRegression(backMultiplier) * backExtraMultiplier;
            float effectZLeft = CalculateZEffect(_trackLeftNipple.depthDiff, multiplier);
            float effectZRight = CalculateZEffect(_trackRightNipple.depthDiff, multiplier);

            // back force on left breast
            if(_trackLeftNipple.depthDiff > 0)
            {
                UpdateLeftPhysics(Direction.BACK, effectZLeft);
            }
            else
            {
                ResetLeftPhysics(Direction.BACK);
            }

            // back force on right breast
            if(_trackRightNipple.depthDiff > 0)
            {
                UpdateRightPhysics(Direction.BACK, effectZRight);
            }
            else
            {
                ResetRightPhysics(Direction.BACK);
            }
        }

        private float CalculateXEffect(float angle, float multiplier) =>
            // multiplier * _rollMultiplier * Mathf.Abs(angle) / 60;
            multiplier * Curve(_rollMultiplier * Mathf.Abs(angle) / 40);

        private float CalculateYEffect(float angle, float multiplier) =>
            // multiplier * _pitchMultiplier * Mathf.Abs(angle) / 75;
            multiplier * Curve(_pitchMultiplier * Mathf.Abs(angle) / 50);

        private static float CalculateZEffect(float distance, float multiplier) =>
            // multiplier * Mathf.Abs(distance) * 12;
            multiplier * Curve(Mathf.Abs(distance) * 8);

        // https://www.desmos.com/calculator/ykxswso5ie
        private static float Curve(float effect) => Calc.InverseSmoothStep(effect, 10, 0.8f, 0f);

        private void UpdateLeftPhysics(string direction, float effect)
        {
            float mass = _script.mainPhysicsHandler.realMassAmount;
            _mainParamGroups.ForEach(group => group.left.UpdateForceValue(direction, effect, mass));
        }

        private void UpdateRightPhysics(string direction, float effect)
        {
            float mass = _script.mainPhysicsHandler.realMassAmount;
            _mainParamGroups.ForEach(group => group.right.UpdateForceValue(direction, effect, mass));
        }

        private void ResetLeftPhysics(string direction)
        {
            _mainParamGroups.ForEach(group => group.left.ResetForceValue(direction));
        }

        private void ResetRightPhysics(string direction)
        {
            _mainParamGroups.ForEach(group => group.right.ResetForceValue(direction));
        }
    }
}
