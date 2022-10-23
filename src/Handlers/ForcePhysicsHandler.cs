using System.Collections.Generic;
using UnityEngine;
using TittyMagic.Components;
using TittyMagic.Handlers.Configs;
using TittyMagic.Models;
using static TittyMagic.Script;
using static TittyMagic.ParamName;
using static TittyMagic.Direction;

namespace TittyMagic.Handlers
{
    public static class ForcePhysicsHandler
    {
        private static Dictionary<string, PhysicsParameterGroup[]> _mainParamGroups;
        private static Dictionary<string, PhysicsParameterGroup[]> _softParamGroups;

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
                paramGroups[CENTER_OF_GRAVITY_PERCENT].SetForcePhysicsConfigs(CenterOfGravityConfigs());
                paramGroups[POSITION_DAMPER_Z].SetForcePhysicsConfigs(PositionDamperZConfigs());
                _mainParamGroups = new Dictionary<string, PhysicsParameterGroup[]>();
                _mainParamGroups[BACK] = new[]
                {
                    paramGroups[CENTER_OF_GRAVITY_PERCENT],
                    paramGroups[POSITION_DAMPER_Z],
                };
                _mainParamGroups[FORWARD] = new[]
                {
                    paramGroups[CENTER_OF_GRAVITY_PERCENT],
                    paramGroups[POSITION_DAMPER_Z],
                };
            }

            if(personIsFemale)
            {
                /* Setup soft force physics configs */
                var paramGroups = SoftPhysicsHandler.parameterGroups;
                paramGroups[SOFT_VERTICES_SPRING].SetForcePhysicsConfigs(SoftVerticesSpringConfigs());
                _softParamGroups = new Dictionary<string, PhysicsParameterGroup[]>();
                _softParamGroups[BACK] = new[]
                {
                    paramGroups[SOFT_VERTICES_SPRING],
                };
                _softParamGroups[FORWARD] = new[]
                {
                    paramGroups[SOFT_VERTICES_SPRING],
                };
            }
        }

        private static Dictionary<string, Dictionary<string, DynamicPhysicsConfig>> CenterOfGravityConfigs()
        {
            var backConfig = new DynamicPhysicsConfig(
                massMultiplier: -0.130f,
                softnessMultiplier: -0.130f,
                applyMethod: ApplyMethod.ADDITIVE,
                massCurve: MainPhysicsHandler.InvertMass,
                softnessCurve: Curves.ForcePhysicsSoftnessCurve
            )
            {
                negative = true,
            };
            var forwardConfig = new DynamicPhysicsConfig(
                massMultiplier: 0.130f,
                softnessMultiplier: 0.130f,
                applyMethod: ApplyMethod.ADDITIVE,
                massCurve: MainPhysicsHandler.InvertMass,
                softnessCurve: Curves.ForcePhysicsSoftnessCurve
            )
            {
                negative = false,
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

        private static Dictionary<string, Dictionary<string, DynamicPhysicsConfig>> PositionDamperZConfigs()
        {
            var backConfig = new DynamicPhysicsConfig(
                massMultiplier: 4f,
                softnessMultiplier: -8f,
                applyMethod: ApplyMethod.ADDITIVE,
                softnessCurve: Curves.ForcePhysicsSoftnessCurve
            )
            {
                negative = true,
            };
            var forwardConfig = new DynamicPhysicsConfig(
                massMultiplier: 4f,
                softnessMultiplier: -8f,
                applyMethod: ApplyMethod.ADDITIVE,
                softnessCurve: Curves.ForcePhysicsSoftnessCurve
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

        private static Dictionary<string, Dictionary<string, DynamicPhysicsConfig>> SoftVerticesSpringConfigs()
        {
            var backConfig = new DynamicPhysicsConfig(
                massMultiplier: 0f,
                softnessMultiplier: -30f,
                applyMethod: ApplyMethod.ADDITIVE
            )
            {
                negative = true,
            };
            var forwardConfig = new DynamicPhysicsConfig(
                massMultiplier: 0f,
                softnessMultiplier: 40f,
                applyMethod: ApplyMethod.ADDITIVE
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
                UpdateLeftBreast(FORWARD, ForwardEffect(_trackLeftBreast.depthDiff));
            }
            else
            {
                ResetLeftBreast(FORWARD);
            }

            if(_trackRightBreast.depthDiff <= 0)
            {
                // forward force on right breast
                UpdateRightBreast(FORWARD, ForwardEffect(_trackRightBreast.depthDiff));
            }
            else
            {
                ResetRightBreast(FORWARD);
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
                UpdateLeftBreast(BACK, BackEffect(_trackLeftBreast.depthDiff));
            }
            else
            {
                ResetLeftBreast(BACK);
            }

            if(_trackRightBreast.depthDiff > 0)
            {
                // back force on right breast
                UpdateRightBreast(BACK, BackEffect(_trackRightBreast.depthDiff));
            }
            else
            {
                ResetRightBreast(BACK);
            }
        }

        private static void UpdateLeftBreast(string direction, float effect)
        {
            foreach(var group in _mainParamGroups[direction])
            {
                group.left.UpdateForceValue(direction, effect, _mass, _softness);
            }

            if(_softParamGroups != null)
            {
                foreach(var group in _softParamGroups[direction])
                {
                    group.left.UpdateForceValue(direction, effect, _mass, _softness);
                }
            }
        }

        private static void UpdateRightBreast(string direction, float effect)
        {
            foreach(var group in _mainParamGroups[direction])
            {
                group.right.UpdateForceValue(direction, effect, _mass, _softness);
            }

            if(_softParamGroups != null)
            {
                foreach(var group in _softParamGroups[direction])
                {
                    group.right.UpdateForceValue(direction, effect, _mass, _softness);
                }
            }
        }

        private static void ResetLeftBreast(string direction)
        {
            foreach(var group in _mainParamGroups[direction])
            {
                group.left.ResetForceValue(direction);
            }

            if(_softParamGroups != null)
            {
                foreach(var group in _softParamGroups[direction])
                {
                    group.left.ResetForceValue(direction);
                }
            }
        }

        private static void ResetRightBreast(string direction)
        {
            foreach(var group in _mainParamGroups[direction])
            {
                group.right.ResetForceValue(direction);
            }

            if(_softParamGroups != null)
            {
                foreach(var group in _softParamGroups[direction])
                {
                    group.right.ResetForceValue(direction);
                }
            }
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
