using System;
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

        public static void Init()
        {
            _trackLeftBreast = tittyMagic.trackLeftBreast;
            _trackRightBreast = tittyMagic.trackRightBreast;

            _baseJsf = tittyMagic.NewJSONStorableFloat("forcePhysicsBase", 1.00f, 0.00f, 2.00f);
            _upJsf = tittyMagic.NewJSONStorableFloat("forcePhysicsUp", 1.00f, 0.00f, 2.00f);
            _downJsf = tittyMagic.NewJSONStorableFloat("forcePhysicsDown", 1.00f, 0.00f, 2.00f);
            _forwardJsf = tittyMagic.NewJSONStorableFloat("forcePhysicsForward", 1.00f, 0.00f, 2.00f);
            _backJsf = tittyMagic.NewJSONStorableFloat("forcePhysicsBack", 1.00f, 0.00f, 2.00f);
            _leftRightJsf = tittyMagic.NewJSONStorableFloat("forcePhysicsLeftRight", 1.00f, 0.00f, 2.00f);
        }

        public static void LoadSettings()
        {
            /* Setup main force physics configs */
            {
                var paramGroups = MainPhysicsHandler.parameterGroups;
                paramGroups[CENTER_OF_GRAVITY_PERCENT].SetForcePhysicsConfigs(NewCenterOfGravityConfigs(), NewCenterOfGravityConfigs());
                paramGroups[POSITION_DAMPER_Z].SetForcePhysicsConfigs(NewPositionDamperZConfigs(), NewPositionDamperZConfigs());
                _mainParamGroups = MainPhysicsHandler.parameterGroups.Values.ToList();
            }

            if(personIsFemale)
            {
                /* Setup soft force physics configs */
                var paramGroups = SoftPhysicsHandler.parameterGroups;
                paramGroups[SOFT_VERTICES_SPRING].SetForcePhysicsConfigs(NewSoftVerticesSpringConfigs(), NewSoftVerticesSpringConfigs());
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
                        negative: true,
                        applyMethod: ApplyMethod.ADDITIVE,
                        massCurve: MainPhysicsHandler.InvertMass,
                        softnessCurve: Curves.ForcePhysicsSoftnessCurve
                    )
                },
                {
                    Direction.FORWARD, new DynamicPhysicsConfig(
                        massMultiplier: 0.390f,
                        softnessMultiplier: 0.130f,
                        negative: false,
                        applyMethod: ApplyMethod.ADDITIVE,
                        massCurve: MainPhysicsHandler.InvertMass,
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
                        negative: true,
                        applyMethod: ApplyMethod.ADDITIVE,
                        softnessCurve: Curves.ForcePhysicsSoftnessCurve
                    )
                },
                {
                    Direction.FORWARD, new DynamicPhysicsConfig(
                        massMultiplier: 0f,
                        softnessMultiplier: -9f,
                        negative: true,
                        applyMethod: ApplyMethod.ADDITIVE,
                        softnessCurve: Curves.ForcePhysicsSoftnessCurve
                    )
                },
            };

        private static Dictionary<string, DynamicPhysicsConfig> NewSoftVerticesSpringConfigs() =>
            new Dictionary<string, DynamicPhysicsConfig>
            {
                {
                    Direction.BACK, new DynamicPhysicsConfig(
                        massMultiplier: 0f,
                        softnessMultiplier: -50f,
                        negative: true,
                        applyMethod: ApplyMethod.ADDITIVE
                    )
                },
                {
                    Direction.FORWARD, new DynamicPhysicsConfig(
                        massMultiplier: 0f,
                        softnessMultiplier: 100f,
                        negative: false,
                        applyMethod: ApplyMethod.ADDITIVE
                    )
                },
            };

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
            Func<float, float> calculateEffect = angle =>
                0.325f
                * Curves.QuadraticRegression(leftRightMultiplier)
                * Curves.XForceEffectCurve(Mathf.Abs(angle) / 40);

            float effectXLeft = calculateEffect(_trackLeftBreast.angleX);
            if(_trackLeftBreast.angleX >= 0)
            {
                // left force on left breast
                ResetLeftPhysics(Direction.LEFT);
                UpdateLeftPhysics(Direction.RIGHT, effectXLeft);
            }
            else
            {
                // right force on left breast
                ResetLeftPhysics(Direction.RIGHT);
                UpdateLeftPhysics(Direction.RIGHT, effectXLeft);
            }

            float effectXRight = calculateEffect(_trackRightBreast.angleX);
            if(_trackRightBreast.angleX >= 0)
            {
                // left force on right breast
                ResetRightPhysics(Direction.LEFT);
                UpdateRightPhysics(Direction.RIGHT, effectXRight);
            }
            else
            {
                // right force on right breast
                ResetRightPhysics(Direction.RIGHT);
                UpdateRightPhysics(Direction.LEFT, effectXRight);
            }
        }

        private static void AdjustUpPhysics()
        {
            Func<float, float> calculateEffect = angle =>
                0.265f
                * Curves.QuadraticRegression(upMultiplier)
                * Curves.YForceEffectCurve(Mathf.Abs(angle) / 40);

            if(_trackLeftBreast.angleY >= 0)
            {
                // up force on left breast
                UpdateLeftPhysics(Direction.UP, calculateEffect(_trackLeftBreast.angleY));
            }
            else
            {
                ResetLeftPhysics(Direction.UP);
            }

            if(_trackRightBreast.angleY >= 0)
            {
                // up force on right breast
                UpdateRightPhysics(Direction.UP, calculateEffect(_trackRightBreast.angleY));
            }
            else
            {
                ResetRightPhysics(Direction.UP);
            }
        }

        private static void AdjustDownPhysics()
        {
            Func<float, float> calculateEffect = angle =>
                0.265f
                * Curves.QuadraticRegression(downMultiplier)
                * Curves.YForceEffectCurve(Mathf.Abs(angle) / 40);

            if(_trackLeftBreast.angleY < 0)
            {
                // down force on left breast
                UpdateLeftPhysics(Direction.DOWN, calculateEffect(_trackLeftBreast.angleY));
            }
            else
            {
                ResetLeftPhysics(Direction.DOWN);
            }

            if(_trackRightBreast.angleY < 0)
            {
                // down force on right breast
                UpdateRightPhysics(Direction.DOWN, calculateEffect(_trackRightBreast.angleY));
            }
            else
            {
                ResetRightPhysics(Direction.DOWN);
            }
        }

        private static void AdjustForwardPhysics()
        {
            Func<float, float> calculateEffect = distance =>
                0.90f
                * Curves.QuadraticRegression(forwardMultiplier)
                * Curves.ZForceEffectCurve(Mathf.Abs(distance) * 10);

            if(_trackLeftBreast.depthDiff <= 0)
            {
                // forward force on left breast
                UpdateLeftPhysics(Direction.FORWARD, calculateEffect(_trackLeftBreast.depthDiff));
            }
            else
            {
                ResetLeftPhysics(Direction.FORWARD);
            }

            if(_trackRightBreast.depthDiff <= 0)
            {
                // forward force on right breast
                UpdateRightPhysics(Direction.FORWARD, calculateEffect(_trackRightBreast.depthDiff));
            }
            else
            {
                ResetRightPhysics(Direction.FORWARD);
            }
        }

        private static void AdjustBackPhysics()
        {
            Func<float, float> calculateEffect = distance =>
                0.90f
                * Curves.QuadraticRegression(backMultiplier)
                * Curves.ZForceEffectCurve(Mathf.Abs(distance) * 10);

            if(_trackLeftBreast.depthDiff > 0)
            {
                // back force on left breast
                UpdateLeftPhysics(Direction.BACK, calculateEffect(_trackLeftBreast.depthDiff));
            }
            else
            {
                ResetLeftPhysics(Direction.BACK);
            }

            if(_trackRightBreast.depthDiff > 0)
            {
                // back force on right breast
                UpdateRightPhysics(Direction.BACK, calculateEffect(_trackRightBreast.depthDiff));
            }
            else
            {
                ResetRightPhysics(Direction.BACK);
            }
        }

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
