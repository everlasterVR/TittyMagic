using System.Collections.Generic;
using UnityEngine;
using TittyMagic.Components;
using TittyMagic.Handlers.Configs;
using TittyMagic.Models;
using static TittyMagic.Script;
using static TittyMagic.ParamName;

namespace TittyMagic.Handlers
{
    public static class ForcePhysicsHandler
    {
        private static List<PhysicsParameterGroup> _mainParamGroups;
        private static List<PhysicsParameterGroup> _softParamGroups;

        private static TrackBreast _trackLeftBreast;
        private static TrackBreast _trackRightBreast;

        private static JSONStorableFloat _baseJsf;
        private static JSONStorableFloat _upJsf;
        private static JSONStorableFloat _downJsf;
        private static JSONStorableFloat _forwardJsf;
        private static JSONStorableFloat _backJsf;
        private static JSONStorableFloat _leftRightJsf;

        private static float upMultiplier => _baseJsf.val * _upJsf.val;
        private static float downMultiplier => _baseJsf.val * _downJsf.val;
        private static float forwardMultiplier => _baseJsf.val * _forwardJsf.val;
        private static float backMultiplier => _baseJsf.val * _backJsf.val;
        private static float leftRightMultiplier => _baseJsf.val * _leftRightJsf.val;

        public static void Init(
            TrackBreast trackLeftBreast,
            TrackBreast trackRightBreast
        )
        {
            _trackLeftBreast = trackLeftBreast;
            _trackRightBreast = trackRightBreast;

            _baseJsf = tittyMagic.NewJSONStorableFloat("forcePhysicsBase", 1.00f, 0.00f, 2.00f);
            _upJsf = tittyMagic.NewJSONStorableFloat("forcePhysicsUp", 1.00f, 0.00f, 2.00f);
            _downJsf = tittyMagic.NewJSONStorableFloat("forcePhysicsDown", 1.00f, 0.00f, 2.00f);
            _forwardJsf = tittyMagic.NewJSONStorableFloat("forcePhysicsForward", 1.00f, 0.00f, 2.00f);
            _backJsf = tittyMagic.NewJSONStorableFloat("forcePhysicsBack", 1.00f, 0.00f, 2.00f);
            _leftRightJsf = tittyMagic.NewJSONStorableFloat("forcePhysicsLeftRight", 1.00f, 0.00f, 2.00f);
        }

        public static void LoadSettings()
        {
            SetupMainForcePhysicsConfigs();
            _mainParamGroups = MainPhysicsHandler.parameterGroups.Values.ToList();

            if(personIsFemale)
            {
                SetupSoftForcePhysicsConfigs();
                _softParamGroups = SoftPhysicsHandler.parameterGroups.Values.ToList();
            }
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

        private static void SetupMainForcePhysicsConfigs()
        {
            var paramGroups = MainPhysicsHandler.parameterGroups;
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

        private static void SetupSoftForcePhysicsConfigs()
        {
            var paramGroups = SoftPhysicsHandler.parameterGroups;
            paramGroups[SOFT_VERTICES_SPRING].SetForcePhysicsConfigs(NewSoftVerticesSpringConfigs(), NewSoftVerticesSpringConfigs());
            paramGroups[SOFT_VERTICES_BACK_FORCE].SetForcePhysicsConfigs(NewSoftVerticesBackForceConfigs(), NewSoftVerticesBackForceConfigs());
        }

        public static void Update()
        {
            AdjustLeftRightPhysics();
            AdjustUpPhysics();
            AdjustDownPhysics();
            AdjustForwardPhysics();
            AdjustBackPhysics();
        }

        private static void AdjustLeftRightPhysics()
        {
            float multiplier = 0.5f * Curves.QuadraticRegression(leftRightMultiplier);
            float effectXLeft = CalculateXEffect(_trackLeftBreast.angleX, multiplier);
            float effectXRight = CalculateXEffect(_trackRightBreast.angleX, multiplier);

            // left force on left breast
            if(_trackLeftBreast.angleX >= 0)
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
            if(_trackRightBreast.angleX >= 0)
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

        private static void AdjustUpPhysics()
        {
            float multiplier = 0.5f * Curves.QuadraticRegression(upMultiplier);
            float effectYLeft = CalculateYEffect(_trackLeftBreast.angleY, multiplier);
            float effectYRight = CalculateYEffect(_trackRightBreast.angleY, multiplier);

            // up force on left breast
            if(_trackLeftBreast.angleY >= 0)
            {
                UpdateLeftPhysics(Direction.UP, effectYLeft);
            }
            else
            {
                ResetLeftPhysics(Direction.UP);
            }

            // up force on right breast
            if(_trackRightBreast.angleY >= 0)
            {
                UpdateRightPhysics(Direction.UP, effectYRight);
            }
            else
            {
                ResetRightPhysics(Direction.UP);
            }
        }

        private static void AdjustDownPhysics()
        {
            float multiplier = 0.5f * Curves.QuadraticRegression(downMultiplier);
            float effectYLeft = CalculateYEffect(_trackLeftBreast.angleY, multiplier);
            float effectYRight = CalculateYEffect(_trackRightBreast.angleY, multiplier);

            // down force on left breast
            if(_trackLeftBreast.angleY < 0)
            {
                UpdateLeftPhysics(Direction.DOWN, effectYLeft);
            }
            else
            {
                ResetLeftPhysics(Direction.DOWN);
            }

            // down force on right breast
            if(_trackRightBreast.angleY < 0)
            {
                UpdateRightPhysics(Direction.DOWN, effectYRight);
            }
            else
            {
                ResetRightPhysics(Direction.DOWN);
            }
        }

        private static void AdjustForwardPhysics()
        {
            float multiplier = Curves.QuadraticRegression(forwardMultiplier);
            float effectZLeft = CalculateZEffect(_trackLeftBreast.depthDiff, multiplier);
            float effectZRight = CalculateZEffect(_trackRightBreast.depthDiff, multiplier);

            // forward force on left breast
            if(_trackLeftBreast.depthDiff <= 0)
            {
                UpdateLeftPhysics(Direction.FORWARD, effectZLeft);
            }
            else
            {
                ResetLeftPhysics(Direction.FORWARD);
            }

            // forward force on right breast
            if(_trackRightBreast.depthDiff <= 0)
            {
                UpdateRightPhysics(Direction.FORWARD, effectZRight);
            }
            else
            {
                ResetRightPhysics(Direction.FORWARD);
            }
        }

        private static void AdjustBackPhysics()
        {
            float multiplier = Curves.QuadraticRegression(backMultiplier);
            float effectZLeft = CalculateZEffect(_trackLeftBreast.depthDiff, multiplier);
            float effectZRight = CalculateZEffect(_trackRightBreast.depthDiff, multiplier);

            // back force on left breast
            if(_trackLeftBreast.depthDiff > 0)
            {
                UpdateLeftPhysics(Direction.BACK, effectZLeft);
            }
            else
            {
                ResetLeftPhysics(Direction.BACK);
            }

            // back force on right breast
            if(_trackRightBreast.depthDiff > 0)
            {
                UpdateRightPhysics(Direction.BACK, effectZRight);
            }
            else
            {
                ResetRightPhysics(Direction.BACK);
            }
        }

        private static float CalculateXEffect(float angle, float multiplier) =>
            multiplier * Curves.ForceEffectCurve(Mathf.Abs(angle) / 40);

        private static float CalculateYEffect(float angle, float multiplier) =>
            multiplier * Curves.ForceEffectCurve(Mathf.Abs(angle) / 50);

        private static float CalculateZEffect(float distance, float multiplier) =>
            multiplier * Curves.ForceEffectCurve(Mathf.Abs(distance) * 8);

        private static void UpdateLeftPhysics(string direction, float effect)
        {
            float mass = MainPhysicsHandler.realMassAmount;
            float softness = tittyMagic.softnessAmount;
            _mainParamGroups.ForEach(group => group.left.UpdateForceValue(direction, effect, mass, softness));
            _softParamGroups?.ForEach(group => group.left.UpdateForceValue(direction, effect, mass, softness));
        }

        private static void UpdateRightPhysics(string direction, float effect)
        {
            float mass = MainPhysicsHandler.realMassAmount;
            float softness = tittyMagic.softnessAmount;
            _mainParamGroups.ForEach(group => group.right.UpdateForceValue(direction, effect, mass, softness));
            _softParamGroups?.ForEach(group => group.right.UpdateForceValue(direction, effect, mass, softness));
        }

        private static void ResetLeftPhysics(string direction)
        {
            _mainParamGroups.ForEach(group => group.left.ResetForceValue(direction));
            _softParamGroups?.ForEach(group => group.left.ResetForceValue(direction));
        }

        private static void ResetRightPhysics(string direction)
        {
            _mainParamGroups.ForEach(group => group.right.ResetForceValue(direction));
            _softParamGroups?.ForEach(group => group.right.ResetForceValue(direction));
        }

        public static void Destroy()
        {
            _mainParamGroups = null;
            _softParamGroups = null;
            _trackLeftBreast = null;
            _trackRightBreast = null;
            _baseJsf = null;
            _upJsf = null;
            _downJsf = null;
            _forwardJsf = null;
            _backJsf = null;
            _leftRightJsf = null;
        }
    }
}
