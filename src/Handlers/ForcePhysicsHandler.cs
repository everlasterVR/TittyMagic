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

        private static float _leftRightMultiplier;
        private static float _upMultiplier;
        private static float _downMultiplier;
        private static float _forwardMultiplier;
        private static float _backMultiplier;

        public static void SetMultipliers()
        {
            _leftRightMultiplier = 0.325f;
            _upMultiplier = 0.265f;
            _downMultiplier = 0.265f;
            _forwardMultiplier = 0.90f;
            _backMultiplier = 0.90f;
        }

        private static float _mass;
        private static float _softness;

        public static void Update()
        {
            _mass = MainPhysicsHandler.realMassAmount;
            _softness = tittyMagic.softnessAmount;
            AdjustLeftRightPhysics();
            AdjustUpPhysics();
            AdjustDownPhysics();
            AdjustForwardPhysics();
            AdjustBackPhysics();
        }

        private static void AdjustLeftRightPhysics()
        {
            Func<float, float> calculateEffect = angle =>
                _leftRightMultiplier
                * Curves.QuadraticRegression(leftRightMultiplier)
                * Curves.XForceEffectCurve(Mathf.Abs(angle) / 40);

            float effectXLeft = calculateEffect(_trackLeftBreast.angleX);
            if(_trackLeftBreast.angleX > 0)
            {
                // left force on left breast
                ResetLeftBreast(Direction.LEFT);
                UpdateLeftBreast(Direction.RIGHT, effectXLeft);
            }
            else
            {
                // right force on left breast
                ResetLeftBreast(Direction.RIGHT);
                UpdateLeftBreast(Direction.RIGHT, effectXLeft);
            }

            float effectXRight = calculateEffect(_trackRightBreast.angleX);
            if(_trackRightBreast.angleX > 0)
            {
                // left force on right breast
                ResetRightBreast(Direction.LEFT);
                UpdateRightBreast(Direction.RIGHT, effectXRight);
            }
            else
            {
                // right force on right breast
                ResetRightBreast(Direction.RIGHT);
                UpdateRightBreast(Direction.LEFT, effectXRight);
            }
        }

        private static void AdjustUpPhysics()
        {
            Func<float, float> calculateEffect = angle =>
                _upMultiplier
                * Curves.QuadraticRegression(upMultiplier)
                * Curves.YForceEffectCurve(Mathf.Abs(angle) / 40);

            if(_trackLeftBreast.angleY >= 0)
            {
                // up force on left breast
                UpdateLeftBreast(Direction.UP, calculateEffect(_trackLeftBreast.angleY));
            }
            else
            {
                ResetLeftBreast(Direction.UP);
            }

            if(_trackRightBreast.angleY >= 0)
            {
                // up force on right breast
                UpdateRightBreast(Direction.UP, calculateEffect(_trackRightBreast.angleY));
            }
            else
            {
                ResetRightBreast(Direction.UP);
            }
        }

        private static void AdjustDownPhysics()
        {
            Func<float, float> calculateEffect = angle =>
                _downMultiplier
                * Curves.QuadraticRegression(downMultiplier)
                * Curves.YForceEffectCurve(Mathf.Abs(angle) / 40);

            if(_trackLeftBreast.angleY < 0)
            {
                // down force on left breast
                UpdateLeftBreast(Direction.DOWN, calculateEffect(_trackLeftBreast.angleY));
            }
            else
            {
                ResetLeftBreast(Direction.DOWN);
            }

            if(_trackRightBreast.angleY < 0)
            {
                // down force on right breast
                UpdateRightBreast(Direction.DOWN, calculateEffect(_trackRightBreast.angleY));
            }
            else
            {
                ResetRightBreast(Direction.DOWN);
            }
        }

        private static void AdjustForwardPhysics()
        {
            Func<float, float> calculateEffect = distance =>
                _forwardMultiplier
                * Curves.QuadraticRegression(forwardMultiplier)
                * Curves.ZForceEffectCurve(Mathf.Abs(distance) * 10);

            if(_trackLeftBreast.depthDiff <= 0)
            {
                // forward force on left breast
                UpdateLeftBreast(Direction.FORWARD, calculateEffect(_trackLeftBreast.depthDiff));
            }
            else
            {
                ResetLeftBreast(Direction.FORWARD);
            }

            if(_trackRightBreast.depthDiff <= 0)
            {
                // forward force on right breast
                UpdateRightBreast(Direction.FORWARD, calculateEffect(_trackRightBreast.depthDiff));
            }
            else
            {
                ResetRightBreast(Direction.FORWARD);
            }
        }

        private static void AdjustBackPhysics()
        {
            Func<float, float> calculateEffect = distance =>
                _backMultiplier
                * Curves.QuadraticRegression(backMultiplier)
                * Curves.ZForceEffectCurve(Mathf.Abs(distance) * 10);

            if(_trackLeftBreast.depthDiff > 0)
            {
                // back force on left breast
                UpdateLeftBreast(Direction.BACK, calculateEffect(_trackLeftBreast.depthDiff));
            }
            else
            {
                ResetLeftBreast(Direction.BACK);
            }

            if(_trackRightBreast.depthDiff > 0)
            {
                // back force on right breast
                UpdateRightBreast(Direction.BACK, calculateEffect(_trackRightBreast.depthDiff));
            }
            else
            {
                ResetRightBreast(Direction.BACK);
            }
        }

        private static void UpdateLeftBreast(string direction, float effect)
        {
            _mainParamGroups.ForEach(group => group.left.UpdateForceValue(direction, effect, _mass, _softness));
            _softParamGroups?.ForEach(group => group.left.UpdateForceValue(direction, effect, _mass, _softness));
        }

        private static void UpdateRightBreast(string direction, float effect)
        {
            _mainParamGroups.ForEach(group => group.right.UpdateForceValue(direction, effect, _mass, _softness));
            _softParamGroups?.ForEach(group => group.right.UpdateForceValue(direction, effect, _mass, _softness));
        }

        private static void ResetLeftBreast(string direction)
        {
            _mainParamGroups.ForEach(group => group.left.ResetForceValue(direction));
            _softParamGroups?.ForEach(group => group.left.ResetForceValue(direction));
        }

        private static void ResetRightBreast(string direction)
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
