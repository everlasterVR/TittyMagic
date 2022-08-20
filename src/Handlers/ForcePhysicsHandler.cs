using System.Collections.Generic;
using UnityEngine;
using TittyMagic.Configs;
using static TittyMagic.ParamName;

namespace TittyMagic.Handlers
{
    internal class ForcePhysicsHandler
    {
        private readonly Script _script;

        private List<PhysicsParameterGroup> _mainParamGroups;
        private List<PhysicsParameterGroup> _softParamGroups;

        private readonly TrackNipple _trackLeftNipple;
        private readonly TrackNipple _trackRightNipple;

        // ReSharper disable MemberCanBePrivate.Global
        public JSONStorableFloat baseJsf { get; }
        public JSONStorableFloat upJsf { get; }
        public JSONStorableFloat downJsf { get; }
        public JSONStorableFloat forwardJsf { get; }
        public JSONStorableFloat backJsf { get; }

        public JSONStorableFloat leftRightJsf { get; }
        // ReSharper restore MemberCanBePrivate.Global

        private float upMultiplier => baseJsf.val * upJsf.val;
        private float downMultiplier => baseJsf.val * downJsf.val;
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
            SetupSoftForcePhysicsConfigs();
            _mainParamGroups = _script.mainPhysicsHandler.parameterGroups.Values.ToList();
            _softParamGroups = _script.softPhysicsHandler.parameterGroups.Values.ToList();
        }

        private static Dictionary<string, DynamicPhysicsConfig> NewCenterOfGravityConfigs() =>
            new Dictionary<string, DynamicPhysicsConfig>
            {
                {
                    Direction.BACK, new DynamicPhysicsConfig(
                        massMultiplier: -0.390f,
                        softnessMultiplier: -0.130f,
                        isNegative: true,
                        applyMethod: ApplyMethod.ADDITIVE,
                        massCurve: MainPhysicsHandler.InvertMass,
                        softnessCurve: Curves.ForcePhysicsSoftnessCurve
                    )
                },
                {
                    Direction.FORWARD, new DynamicPhysicsConfig(
                        massMultiplier: 0.390f,
                        softnessMultiplier: 0.130f,
                        isNegative: false,
                        applyMethod: ApplyMethod.ADDITIVE,
                        massCurve: MainPhysicsHandler.InvertMass,
                        softnessCurve: Curves.ForcePhysicsSoftnessCurve
                    )
                },
            };

        private static Dictionary<string, DynamicPhysicsConfig> NewDamperConfigs() =>
            new Dictionary<string, DynamicPhysicsConfig>
            {
                {
                    Direction.BACK, new DynamicPhysicsConfig(
                        massMultiplier: 0.50f,
                        softnessMultiplier: 0.37f,
                        isNegative: false,
                        applyMethod: ApplyMethod.ADDITIVE,
                        softnessCurve: Curves.ForcePhysicsSoftnessCurve
                    )
                },
                {
                    Direction.FORWARD, new DynamicPhysicsConfig(
                        massMultiplier: 0.50f,
                        softnessMultiplier: 0.37f,
                        isNegative: false,
                        applyMethod: ApplyMethod.ADDITIVE,
                        softnessCurve: Curves.ForcePhysicsSoftnessCurve
                    )
                },
            };

        private static Dictionary<string, DynamicPhysicsConfig> NewPositionDamperZConfigs() =>
            new Dictionary<string, DynamicPhysicsConfig>
            {
                {
                    Direction.BACK, new DynamicPhysicsConfig(
                        massMultiplier: 0f,
                        softnessMultiplier: -9f,
                        isNegative: true,
                        applyMethod: ApplyMethod.ADDITIVE,
                        softnessCurve: Curves.ForcePhysicsSoftnessCurve
                    )
                },
                {
                    Direction.FORWARD, new DynamicPhysicsConfig(
                        massMultiplier: 0f,
                        softnessMultiplier: -9f,
                        isNegative: true,
                        applyMethod: ApplyMethod.ADDITIVE,
                        softnessCurve: Curves.ForcePhysicsSoftnessCurve
                    )
                },
            };

        private void SetupMainForcePhysicsConfigs()
        {
            var paramGroups = _script.mainPhysicsHandler.parameterGroups;
            paramGroups[CENTER_OF_GRAVITY_PERCENT].SetForcePhysicsConfigs(NewCenterOfGravityConfigs(), NewCenterOfGravityConfigs());
            paramGroups[DAMPER].SetForcePhysicsConfigs(NewDamperConfigs(), NewDamperConfigs());
            paramGroups[POSITION_DAMPER_Z].SetForcePhysicsConfigs(NewPositionDamperZConfigs(), NewPositionDamperZConfigs());
        }

        private static Dictionary<string, DynamicPhysicsConfig> NewSoftVerticesSpringConfigs() =>
            new Dictionary<string, DynamicPhysicsConfig>
            {
                {
                    Direction.BACK, new DynamicPhysicsConfig(
                        massMultiplier: 0f,
                        softnessMultiplier: -50f,
                        isNegative: true,
                        applyMethod: ApplyMethod.ADDITIVE
                    )
                },
                {
                    Direction.FORWARD, new DynamicPhysicsConfig(
                        massMultiplier: 0f,
                        softnessMultiplier: 100f,
                        isNegative: false,
                        applyMethod: ApplyMethod.ADDITIVE
                    )
                },
            };

        private static Dictionary<string, DynamicPhysicsConfig> NewSoftVerticesBackForceConfigs() =>
            new Dictionary<string, DynamicPhysicsConfig>
            {
                {
                    Direction.BACK, new DynamicPhysicsConfig(
                        massMultiplier: -20f,
                        softnessMultiplier: 0f,
                        isNegative: true,
                        applyMethod: ApplyMethod.ADDITIVE,
                        // https://www.desmos.com/calculator/hnhlbofgmz
                        massCurve: x => Curves.InverseSmoothStep(2 / 3f * x, 1.00f, 0.15f, 0.70f)
                    )
                },
                {
                    Direction.FORWARD, new DynamicPhysicsConfig(
                        massMultiplier: -20f,
                        softnessMultiplier: 0f,
                        isNegative: true,
                        applyMethod: ApplyMethod.ADDITIVE,
                        // https://www.desmos.com/calculator/hnhlbofgmz
                        massCurve: x => Curves.InverseSmoothStep(2 / 3f * x, 1.00f, 0.15f, 0.70f)
                    )
                },
            };

        private void SetupSoftForcePhysicsConfigs()
        {
            var paramGroups = _script.softPhysicsHandler.parameterGroups;
            paramGroups[SOFT_VERTICES_SPRING].SetForcePhysicsConfigs(NewSoftVerticesSpringConfigs(), NewSoftVerticesSpringConfigs());
            paramGroups[SOFT_VERTICES_BACK_FORCE].SetForcePhysicsConfigs(NewSoftVerticesBackForceConfigs(), NewSoftVerticesBackForceConfigs());
        }

        public void Update()
        {
            AdjustLeftRightPhysics();
            AdjustUpPhysics();
            AdjustDownPhysics();
            AdjustForwardPhysics();
            AdjustBackPhysics();
        }

        private void AdjustLeftRightPhysics()
        {
            float multiplier = 0.5f * Curves.QuadraticRegression(leftRightMultiplier);
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
            float multiplier = 0.5f * Curves.QuadraticRegression(upMultiplier);
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
            float multiplier = 0.5f * Curves.QuadraticRegression(downMultiplier);
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
            float multiplier = Curves.QuadraticRegression(forwardMultiplier);
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
            float multiplier = Curves.QuadraticRegression(backMultiplier);
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

        private static float CalculateXEffect(float angle, float multiplier) =>
            // multiplier * _rollMultiplier * Mathf.Abs(angle) / 60;
            multiplier * Curves.ForceEffectCurve(Mathf.Abs(angle) / 40);

        private static float CalculateYEffect(float angle, float multiplier) =>
            // multiplier * _pitchMultiplier * Mathf.Abs(angle) / 75;
            multiplier * Curves.ForceEffectCurve(Mathf.Abs(angle) / 50);

        private static float CalculateZEffect(float distance, float multiplier) =>
            // multiplier * Mathf.Abs(distance) * 12;
            multiplier * Curves.ForceEffectCurve(Mathf.Abs(distance) * 8);

        private void UpdateLeftPhysics(string direction, float effect)
        {
            float mass = _script.mainPhysicsHandler.realMassAmount;
            float softness = _script.softnessAmount;
            _mainParamGroups.ForEach(group => group.left.UpdateForceValue(direction, effect, mass, softness));
            _softParamGroups.ForEach(group => group.left.UpdateForceValue(direction, effect, mass, softness));
        }

        private void UpdateRightPhysics(string direction, float effect)
        {
            float mass = _script.mainPhysicsHandler.realMassAmount;
            float softness = _script.softnessAmount;
            _mainParamGroups.ForEach(group => group.right.UpdateForceValue(direction, effect, mass, softness));
            _softParamGroups.ForEach(group => group.right.UpdateForceValue(direction, effect, mass, softness));
        }

        private void ResetLeftPhysics(string direction)
        {
            _mainParamGroups.ForEach(group => group.left.ResetForceValue(direction));
            _softParamGroups.ForEach(group => group.left.ResetForceValue(direction));
        }

        private void ResetRightPhysics(string direction)
        {
            _mainParamGroups.ForEach(group => group.right.ResetForceValue(direction));
            _softParamGroups.ForEach(group => group.right.ResetForceValue(direction));
        }
    }
}
