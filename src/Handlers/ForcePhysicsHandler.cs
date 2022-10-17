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
        private static JSONStorableFloat _forwardJsf;
        private static JSONStorableFloat _backJsf;

        private static float forwardMultiplier => _baseJsf.val * _forwardJsf.val;
        private static float backMultiplier => _baseJsf.val * _backJsf.val;

        public static void Init()
        {
            _trackLeftBreast = tittyMagic.trackLeftBreast;
            _trackRightBreast = tittyMagic.trackRightBreast;
            _baseJsf = tittyMagic.NewJSONStorableFloat("forcePhysicsBase", 1.00f, 0.00f, 2.00f);
            _forwardJsf = tittyMagic.NewJSONStorableFloat("forcePhysicsForward", 1.00f, 0.00f, 2.00f);
            _backJsf = tittyMagic.NewJSONStorableFloat("forcePhysicsBack", 1.00f, 0.00f, 2.00f);
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
                        massMultiplier: -0.130f,
                        softnessMultiplier: -0.130f,
                        negative: true,
                        applyMethod: ApplyMethod.ADDITIVE,
                        massCurve: MainPhysicsHandler.InvertMass,
                        softnessCurve: Curves.ForcePhysicsSoftnessCurve
                    )
                },
                {
                    Direction.FORWARD, new DynamicPhysicsConfig(
                        massMultiplier: 0.130f,
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
                        softnessMultiplier: -30f,
                        negative: true,
                        applyMethod: ApplyMethod.ADDITIVE
                    )
                },
                {
                    Direction.FORWARD, new DynamicPhysicsConfig(
                        massMultiplier: 0f,
                        softnessMultiplier: 40f,
                        negative: false,
                        applyMethod: ApplyMethod.ADDITIVE
                    )
                },
            };

        private static float _forwardMultiplier;
        private static float _backMultiplier;

        public static void SetMultipliers()
        {
            _forwardMultiplier = 0.90f;
            _backMultiplier = 0.90f;
        }

        private static float _mass;
        private static float _softness;

        public static void Update()
        {
            _mass = MainPhysicsHandler.realMassAmount;
            _softness = tittyMagic.softnessAmount;
            AdjustForwardPhysics();
            AdjustBackPhysics();
        }

        private static float ForwardEffect(float distance) =>
            _forwardMultiplier
            * Curves.QuadraticRegression(forwardMultiplier)
            * Curves.ZForceEffectCurve(Mathf.Abs(distance) * 27f);

        private static void AdjustForwardPhysics()
        {
            if(_trackLeftBreast.depthDiff <= 0)
            {
                // forward force on left breast
                UpdateLeftBreast(Direction.FORWARD, ForwardEffect(_trackLeftBreast.depthDiff));
            }
            else
            {
                ResetLeftBreast(Direction.FORWARD);
            }

            if(_trackRightBreast.depthDiff <= 0)
            {
                // forward force on right breast
                UpdateRightBreast(Direction.FORWARD, ForwardEffect(_trackRightBreast.depthDiff));
            }
            else
            {
                ResetRightBreast(Direction.FORWARD);
            }
        }

        private static float BackEffect(float distance) =>
            _backMultiplier
            * Curves.QuadraticRegression(backMultiplier)
            * Curves.ZForceEffectCurve(Mathf.Abs(distance) * 20.50f);

        private static void AdjustBackPhysics()
        {
            if(_trackLeftBreast.depthDiff > 0)
            {
                // back force on left breast
                UpdateLeftBreast(Direction.BACK, BackEffect(_trackLeftBreast.depthDiff));
            }
            else
            {
                ResetLeftBreast(Direction.BACK);
            }

            if(_trackRightBreast.depthDiff > 0)
            {
                // back force on right breast
                UpdateRightBreast(Direction.BACK, BackEffect(_trackRightBreast.depthDiff));
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
            _forwardJsf = null;
            _backJsf = null;
        }
    }
}
