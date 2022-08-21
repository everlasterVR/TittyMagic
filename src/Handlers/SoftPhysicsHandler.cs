using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TittyMagic.Configs;
using static TittyMagic.ParamName;
using static TittyMagic.Script;
using static TittyMagic.Side;
using static TittyMagic.SoftColliderGroup;

namespace TittyMagic.Handlers
{
    internal static class SoftPhysicsHandler
    {
        private static DAZPhysicsMesh _breastPhysicsMesh;
        private static List<string> _breastPhysicsMeshFloatParamNames;

        // Left/Right -> Group name -> Group
        private static Dictionary<string, Dictionary<string, DAZPhysicsMeshSoftVerticesGroup>> _softVerticesGroups;

        // Group name -> Group
        public static Dictionary<string, PhysicsParameterGroup> parameterGroups { get; private set; }
        public static JSONStorableBool softPhysicsOn { get; private set; }
        public static JSONStorableBool allowSelfCollision { get; private set; }

        private static bool _initDone;

        public static void Init()
        {
            if(Gender.isFemale)
            {
                _breastPhysicsMesh = (DAZPhysicsMesh) tittyMagic.containingAtom.GetStorableByID("BreastPhysicsMesh");
                _breastPhysicsMeshFloatParamNames = _breastPhysicsMesh.GetFloatParamNames();

                var groups = _breastPhysicsMesh.softVerticesGroups;
                _softVerticesGroups = new Dictionary<string, Dictionary<string, DAZPhysicsMeshSoftVerticesGroup>>
                {
                    {
                        LEFT, new Dictionary<string, DAZPhysicsMeshSoftVerticesGroup>
                        {
                            { MAIN, groups.Find(group => group.name == "left") },
                            { OUTER, groups.Find(group => group.name == "leftouter") },
                            { AREOLA, groups.Find(group => group.name == "leftareola") },
                            { NIPPLE, groups.Find(group => group.name == "leftnipple") },
                        }
                    },
                    {
                        RIGHT, new Dictionary<string, DAZPhysicsMeshSoftVerticesGroup>
                        {
                            { MAIN, groups.Find(group => group.name == "right") },
                            { OUTER, groups.Find(group => group.name == "rightouter") },
                            { AREOLA, groups.Find(group => group.name == "rightareola") },
                            { NIPPLE, groups.Find(group => group.name == "rightnipple") },
                        }
                    },
                };
            }

            softPhysicsOn = tittyMagic.NewJSONStorableBool(SOFT_PHYSICS_ON, Gender.isFemale, register: Gender.isFemale);
            softPhysicsOn.setCallbackFunction = SyncSoftPhysicsOn;

            allowSelfCollision = tittyMagic.NewJSONStorableBool(ALLOW_SELF_COLLISION, Gender.isFemale, register: Gender.isFemale);
            allowSelfCollision.setCallbackFunction = SyncAllowSelfCollision;

            if(_breastPhysicsMesh == null)
            {
                softPhysicsOn.valNoCallback = false;
                allowSelfCollision.valNoCallback = false;
            }

            _initDone = true;
        }

        public static void LoadSettings()
        {
            SetupPhysicsParameterGroups();

            var texts = CreateInfoTexts();
            foreach(var param in parameterGroups)
            {
                param.Value.infoText = texts[param.Key];
            }
        }

        #region *** Parameter setup ***

        private static PhysicsParameter NewPhysicsParameter(string paramName, string side, float startingValue, float minValue, float maxValue)
        {
            string jsfName = $"{paramName}{(side == LEFT ? "" : side)}";
            var valueJsf = new JSONStorableFloat($"{jsfName}Value", startingValue, minValue, maxValue);
            return new PhysicsParameter(
                valueJsf,
                baseValueJsf: new JSONStorableFloat($"{jsfName}BaseValue", valueJsf.val, valueJsf.min, valueJsf.max),
                offsetJsf: tittyMagic.NewJSONStorableFloat($"{jsfName}Offset", 0, -valueJsf.max, valueJsf.max, register: side == LEFT)
            );
        }

        private static SoftGroupPhysicsParameter NewSoftGroupPhysicsParameter(string paramName, string side, string group)
        {
            string jsfName = $"{paramName}{(side == LEFT ? "" : side)}{group}";
            var valueJsf = new JSONStorableFloat($"{jsfName}Value", 1, 0, 5);
            return new SoftGroupPhysicsParameter(
                valueJsf,
                baseValueJsf: new JSONStorableFloat($"{jsfName}BaseValue", valueJsf.val, valueJsf.min, valueJsf.max),
                offsetJsf: tittyMagic.NewJSONStorableFloat($"{jsfName}Offset", 0, -valueJsf.max, valueJsf.max, register: side == LEFT)
            );
        }

        private static PhysicsParameter NewSpringParameter(string side)
        {
            var parameter = NewPhysicsParameter(SOFT_VERTICES_SPRING, side, 0, 0, 500);
            parameter.config = new StaticPhysicsConfig(
                180f,
                softnessCurve: x => -0.62f * Curves.Exponential1(x, 1.90f, 1.74f, 1.17f)
            );
            parameter.valueFormat = "F0";

            Func<float, float> groupSoftnessCurve = x => Curves.Exponential1(x, 1.90f, 1.74f, 1.17f);
            var groupConfigs = new Dictionary<string, StaticPhysicsConfig>
            {
                {
                    MAIN, new StaticPhysicsConfig(
                        3.80f,
                        softnessCurve: x => (1 / 3.80f - 1) * groupSoftnessCurve(x)
                    )
                },
                {
                    OUTER, new StaticPhysicsConfig(
                        4.60f,
                        softnessCurve: x => (1 / 4.60f - 1) * groupSoftnessCurve(x)
                    )
                },
                {
                    AREOLA, new StaticPhysicsConfig(
                        4.80f,
                        softnessCurve: x => (1 / 4.80f - 1) * groupSoftnessCurve(x)
                    )
                },
                {
                    NIPPLE, new StaticPhysicsConfig(
                        4.80f,
                        softnessCurve: x => (1 / 2.40f - 1) * groupSoftnessCurve(x)
                    )
                },
            };

            foreach(string group in allGroups)
            {
                var groupParam = NewSoftGroupPhysicsParameter(SOFT_VERTICES_SPRING, side, group);
                groupParam.config = groupConfigs[group];
                groupParam.sync = value => SyncGroupSpring(parameter.valueJsf.val * value, _softVerticesGroups[side][group]);
                parameter.groupMultiplierParams[group] = groupParam;
            }

            return parameter;
        }

        private static PhysicsParameter NewDamperParameter(string side)
        {
            var parameter = NewPhysicsParameter(SOFT_VERTICES_DAMPER, side, 0, 0, 5.00f);
            parameter.config = new StaticPhysicsConfig(
                0.85f,
                massCurve: x => 0.40f * Curves.Exponential2(x / 1.5f, c: 0.04f, s: 0.04f),
                softnessCurve: x => -0.50f * Curves.Exponential1(x, 1.90f, 1.74f, 1.17f)
            );
            parameter.quicknessOffsetConfig = new StaticPhysicsConfig(
                -0.15f,
                massCurve: x => -0.40f * Curves.Exponential2(x / 1.5f, c: 0.04f, s: 0.04f),
                softnessCurve: x => 0.50f * Curves.Exponential1(x, 1.90f, 1.74f, 1.17f)
            );
            parameter.slownessOffsetConfig = new StaticPhysicsConfig(
                0.15f,
                massCurve: x => 0.40f * Curves.Exponential2(x / 1.5f, c: 0.04f, s: 0.04f),
                softnessCurve: x => -0.50f * Curves.Exponential1(x, 1.90f, 1.74f, 1.17f)
            );
            parameter.valueFormat = "F2";

            var groupConfigs = new Dictionary<string, StaticPhysicsConfig>
            {
                { MAIN, new StaticPhysicsConfig(1.00f) },
                {
                    OUTER, new StaticPhysicsConfig(
                        1.00f,
                        softnessCurve: x => 0.20f * Curves.DeemphasizeMiddle(x)
                    )
                },
                {
                    AREOLA, new StaticPhysicsConfig(
                        1.25f,
                        softnessCurve: x => 1.00f * Curves.DeemphasizeMiddle(x)
                    )
                },
                {
                    NIPPLE, new StaticPhysicsConfig(
                        1.40f,
                        softnessCurve: x => 1.00f * Curves.DeemphasizeMiddle(x)
                    )
                },
            };

            foreach(string group in allGroups)
            {
                var groupParam = NewSoftGroupPhysicsParameter(SOFT_VERTICES_DAMPER, side, group);
                groupParam.config = groupConfigs[group];
                groupParam.sync = value => SyncGroupDamper(parameter.valueJsf.val * value, _softVerticesGroups[side][group]);
                parameter.groupMultiplierParams[group] = groupParam;
            }

            return parameter;
        }

        private static PhysicsParameter NewSoftVerticesMassParameter(string side)
        {
            var parameter = NewPhysicsParameter(SOFT_VERTICES_MASS, side, 0, 0.001f, 0.300f);
            parameter.config = new StaticPhysicsConfig(
                0.040f,
                // https://www.desmos.com/calculator/inmadsqhj2
                softnessCurve: x => 1.00f * Curves.Exponential1(x, 2.30f, 1.74f, 1.17f),
                // https://www.desmos.com/calculator/gsyidpluyg
                massCurve: x => 2.25f * Curves.Exponential1(2 / 3f * x, 1.91f, 1.7f, 0.82f)
            );
            parameter.quicknessOffsetConfig = new StaticPhysicsConfig(
                -0.022f,
                softnessCurve: x => 0.50f * x
            );
            parameter.slownessOffsetConfig = new StaticPhysicsConfig(
                0.066f,
                softnessCurve: x => 0.50f * x
            );
            parameter.valueFormat = "F3";

            var groupConfigs = new Dictionary<string, StaticPhysicsConfig>
            {
                {
                    MAIN, new StaticPhysicsConfig(
                        1.00f,
                        softnessCurve: x => 0.13f * Curves.DeemphasizeMiddle(x)
                    )
                },
                {
                    OUTER, new StaticPhysicsConfig(
                        1.00f,
                        softnessCurve: x => -0.20f * Curves.DeemphasizeMiddle(x)
                    )
                },
                {
                    AREOLA, new StaticPhysicsConfig(1.00f)
                },
                {
                    NIPPLE, new StaticPhysicsConfig(
                        1.00f,
                        softnessCurve: x => -0.13f * Curves.DeemphasizeMiddle(x)
                    )
                },
            };

            foreach(string group in allGroups)
            {
                var groupParam = NewSoftGroupPhysicsParameter(SOFT_VERTICES_MASS, side, group);
                groupParam.config = groupConfigs[group];
                groupParam.sync = value => SyncGroupMass(parameter.valueJsf.val * value, _softVerticesGroups[side][group]);
                parameter.groupMultiplierParams[group] = groupParam;
            }

            return parameter;
        }

        private static PhysicsParameter NewColliderRadiusParameter(string side)
        {
            var parameter = NewPhysicsParameter(SOFT_VERTICES_COLLIDER_RADIUS, side, 0, 0, 0.060f);
            parameter.config = new StaticPhysicsConfig(
                0.016f,
                // https://www.desmos.com/calculator/rotof03irg
                massCurve: x => 1.54f * Curves.Exponential1(2 / 3f * x, 1.42f, 4.25f, 1.17f),
                softnessCurve: x => 0.18f * x
            );
            parameter.valueFormat = "F3";

            var groupConfigs = new Dictionary<string, StaticPhysicsConfig>
            {
                { MAIN, new StaticPhysicsConfig(1.00f) },
                { OUTER, new StaticPhysicsConfig(1.00f) },
                { AREOLA, new StaticPhysicsConfig(1.15f) },
                { NIPPLE, new StaticPhysicsConfig(0.00f) },
            };

            foreach(string group in allGroups)
            {
                var groupParam = NewSoftGroupPhysicsParameter(SOFT_VERTICES_COLLIDER_RADIUS, side, group);
                groupParam.config = groupConfigs[group];
                groupParam.sync = value => SyncGroupColliderRadius(parameter.valueJsf.val * value, _softVerticesGroups[side][group]);
                parameter.groupMultiplierParams[group] = groupParam;
            }

            return parameter;
        }

        private static PhysicsParameter NewColliderAdditionalNormalOffsetParameter(string side)
        {
            var parameter = NewPhysicsParameter(SOFT_VERTICES_COLLIDER_ADDITIONAL_NORMAL_OFFSET, side, 0, -0.010f, 0.010f);
            parameter.config = new StaticPhysicsConfig(0.001f);
            parameter.valueFormat = "F3";

            var groupConfigs = new Dictionary<string, StaticPhysicsConfig>
            {
                { MAIN, new StaticPhysicsConfig(1.00f) },
                { OUTER, new StaticPhysicsConfig(1.00f) },
                { AREOLA, new StaticPhysicsConfig(1.00f) },
                { NIPPLE, new StaticPhysicsConfig(1.00f) },
            };

            foreach(string group in allGroups)
            {
                var groupParam = NewSoftGroupPhysicsParameter(SOFT_VERTICES_COLLIDER_ADDITIONAL_NORMAL_OFFSET, side, group);
                groupParam.config = groupConfigs[group];
                groupParam.sync = value => SyncGroupAdditionalNormalOffset(parameter.valueJsf.val * value, _softVerticesGroups[side][group]);
                parameter.groupMultiplierParams[group] = groupParam;
            }

            return parameter;
        }

        private static PhysicsParameter NewDistanceLimitParameter(string side)
        {
            var parameter = NewPhysicsParameter(SOFT_VERTICES_DISTANCE_LIMIT, side, 0, 0, 0.100f);
            parameter.config = new StaticPhysicsConfig(
                0.019f,
                massCurve: x => 2.4f * x,
                softnessCurve: x => 0.4f * x
            );
            parameter.quicknessOffsetConfig = new StaticPhysicsConfig(
                0.003f,
                softnessCurve: _ => 4.0f
            );
            parameter.slownessOffsetConfig = new StaticPhysicsConfig(
                -0.001f,
                softnessCurve: _ => 4.0f
            );
            parameter.valueFormat = "F3";

            var groupConfigs = new Dictionary<string, StaticPhysicsConfig>
            {
                { MAIN, new StaticPhysicsConfig(1.00f) },
                { OUTER, new StaticPhysicsConfig(1.00f) },
                { AREOLA, new StaticPhysicsConfig(1.10f) },
                { NIPPLE, new StaticPhysicsConfig(1.20f) },
            };

            foreach(string group in allGroups)
            {
                var groupParam = NewSoftGroupPhysicsParameter(SOFT_VERTICES_DISTANCE_LIMIT, side, group);
                groupParam.config = groupConfigs[group];
                groupParam.sync = value => SyncGroupDistanceLimit(parameter.valueJsf.val * value, _softVerticesGroups[side][group]);
                parameter.groupMultiplierParams[group] = groupParam;
            }

            return parameter;
        }

        private static PhysicsParameter NewBackForceParameter(string side)
        {
            var parameter = NewPhysicsParameter(SOFT_VERTICES_BACK_FORCE, side, 0, 0, 50.00f);
            parameter.config = new StaticPhysicsConfig(
                15.00f,
                // https://www.desmos.com/calculator/ww9lp03k6o
                massCurve: x => 0.90f * Curves.InverseSmoothStep(2 / 3f * x, 1.00f, 0.00f, 0.50f),
                // https://www.desmos.com/calculator/uwfattbhdg
                softnessCurve: x => -0.82f * Curves.Exponential1(x, 2.34f, 1.76f, 1.01f)
            );
            parameter.quicknessOffsetConfig = new StaticPhysicsConfig(
                -2.00f,
                massCurve: x => -0.90f * Curves.InverseSmoothStep(2 / 3f * x, 1.00f, 0.00f, 0.50f),
                softnessCurve: x => 0.82f * Curves.Exponential1(x, 2.34f, 1.76f, 1.01f)
            );
            parameter.slownessOffsetConfig = new StaticPhysicsConfig(
                2.00f,
                massCurve: x => 0.90f * Curves.InverseSmoothStep(2 / 3f * x, 1.00f, 0.00f, 0.50f),
                softnessCurve: x => -0.82f * Curves.Exponential1(x, 2.34f, 1.76f, 1.01f)
            );
            parameter.valueFormat = "F2";

            var groupConfigs = new Dictionary<string, StaticPhysicsConfig>
            {
                { MAIN, new StaticPhysicsConfig(1.00f) },
                { OUTER, new StaticPhysicsConfig(1.00f) },
                {
                    AREOLA, new StaticPhysicsConfig(
                        0.25f,
                        softnessCurve: x => (1 / 0.25f - 1) * x
                    )
                },
                {
                    NIPPLE, new StaticPhysicsConfig(
                        0.08f,
                        softnessCurve: x => (1 / 0.08f - 1) * x
                    )
                },
            };

            foreach(string group in allGroups)
            {
                var groupParam = NewSoftGroupPhysicsParameter(SOFT_VERTICES_BACK_FORCE, side, group);
                groupParam.config = groupConfigs[group];
                groupParam.sync = value => SyncGroupBackForce(parameter.valueJsf.val * value, _softVerticesGroups[side][group]);
                parameter.groupMultiplierParams[group] = groupParam;
            }

            return parameter;
        }

        private static PhysicsParameter NewBackForceMaxForceParameter(string side)
        {
            var parameter = NewPhysicsParameter(SOFT_VERTICES_BACK_FORCE_MAX_FORCE, side, 0, 0, 50.00f);
            parameter.config = new StaticPhysicsConfig(50.00f);
            parameter.valueFormat = "F2";

            var groupConfigs = new Dictionary<string, StaticPhysicsConfig>
            {
                { MAIN, new StaticPhysicsConfig(1.00f) },
                { OUTER, new StaticPhysicsConfig(1.00f) },
                { AREOLA, new StaticPhysicsConfig(1.00f) },
                { NIPPLE, new StaticPhysicsConfig(1.00f) },
            };

            foreach(string group in allGroups)
            {
                var groupParam = NewSoftGroupPhysicsParameter(SOFT_VERTICES_BACK_FORCE_MAX_FORCE, side, group);
                groupParam.config = groupConfigs[group];
                groupParam.sync = value => SyncGroupBackForceMaxForce(parameter.valueJsf.val * value, _softVerticesGroups[side][group]);
                parameter.groupMultiplierParams[group] = groupParam;
            }

            return parameter;
        }

        private static PhysicsParameter NewBackForceThresholdDistanceParameter(string side)
        {
            var parameter = NewPhysicsParameter(SOFT_VERTICES_BACK_FORCE_THRESHOLD_DISTANCE, side, 0, 0, 0.030f);
            parameter.config = new StaticPhysicsConfig(0.001f);
            parameter.valueFormat = "F3";

            var groupConfigs = new Dictionary<string, StaticPhysicsConfig>
            {
                { MAIN, new StaticPhysicsConfig(1.00f) },
                { OUTER, new StaticPhysicsConfig(1.00f) },
                { AREOLA, new StaticPhysicsConfig(1.00f) },
                { NIPPLE, new StaticPhysicsConfig(1.00f) },
            };

            foreach(string group in allGroups)
            {
                var groupParam = NewSoftGroupPhysicsParameter(SOFT_VERTICES_BACK_FORCE_THRESHOLD_DISTANCE, side, group);
                groupParam.config = groupConfigs[group];
                groupParam.sync = value => SyncGroupBackForceThresholdDistance(parameter.valueJsf.val * value, _softVerticesGroups[side][group]);
                parameter.groupMultiplierParams[group] = groupParam;
            }

            return parameter;
        }

        private static void SetupPhysicsParameterGroups()
        {
            var softVerticesSpring = new PhysicsParameterGroup(
                NewSpringParameter(LEFT),
                NewSpringParameter(RIGHT),
                "Fat Spring"
            );

            var softVerticesDamper = new PhysicsParameterGroup(
                NewDamperParameter(LEFT),
                NewDamperParameter(RIGHT),
                "Fat Damper"
            )
            {
                dependOnPhysicsRate = true,
            };

            var softVerticesMass = new PhysicsParameterGroup(
                NewSoftVerticesMassParameter(LEFT),
                NewSoftVerticesMassParameter(RIGHT),
                "Fat Mass"
            );

            var softVerticesColliderRadius = new PhysicsParameterGroup(
                NewColliderRadiusParameter(LEFT),
                NewColliderRadiusParameter(RIGHT),
                "Fat Collider Radius"
            )
            {
                useRealMass = true,
            };

            var softVerticesColliderAdditionalNormalOffset = new PhysicsParameterGroup(
                NewColliderAdditionalNormalOffsetParameter(LEFT),
                NewColliderAdditionalNormalOffsetParameter(RIGHT),
                "Fat Collider Depth"
            );

            var softVerticesDistanceLimit = new PhysicsParameterGroup(
                NewDistanceLimitParameter(LEFT),
                NewDistanceLimitParameter(RIGHT),
                "Fat Distance Limit"
            )
            {
                useRealMass = true,
            };

            var softVerticesBackForce = new PhysicsParameterGroup(
                NewBackForceParameter(LEFT),
                NewBackForceParameter(RIGHT),
                "Fat Back Force"
            );

            var softVerticesBackForceMaxForce = new PhysicsParameterGroup(
                NewBackForceMaxForceParameter(LEFT),
                NewBackForceMaxForceParameter(RIGHT),
                "Fat Bk Force Max Force"
            );

            var softVerticesBackForceThresholdDistance = new PhysicsParameterGroup(
                NewBackForceThresholdDistanceParameter(LEFT),
                NewBackForceThresholdDistanceParameter(RIGHT),
                "Fat Bk Force Threshold"
            );

            softVerticesSpring.SetOffsetCallbackFunctions();
            softVerticesDamper.SetOffsetCallbackFunctions();
            softVerticesMass.SetOffsetCallbackFunctions();
            softVerticesBackForce.SetOffsetCallbackFunctions();
            softVerticesBackForceMaxForce.SetOffsetCallbackFunctions();
            softVerticesBackForceThresholdDistance.SetOffsetCallbackFunctions();
            softVerticesColliderRadius.SetOffsetCallbackFunctions();
            softVerticesColliderAdditionalNormalOffset.SetOffsetCallbackFunctions();
            softVerticesDistanceLimit.SetOffsetCallbackFunctions();

            parameterGroups = new Dictionary<string, PhysicsParameterGroup>
            {
                { SOFT_VERTICES_SPRING, softVerticesSpring },
                { SOFT_VERTICES_DAMPER, softVerticesDamper },
                { SOFT_VERTICES_MASS, softVerticesMass },
                { SOFT_VERTICES_BACK_FORCE, softVerticesBackForce },
                { SOFT_VERTICES_BACK_FORCE_MAX_FORCE, softVerticesBackForceMaxForce },
                { SOFT_VERTICES_BACK_FORCE_THRESHOLD_DISTANCE, softVerticesBackForceThresholdDistance },
                { SOFT_VERTICES_COLLIDER_RADIUS, softVerticesColliderRadius },
                { SOFT_VERTICES_COLLIDER_ADDITIONAL_NORMAL_OFFSET, softVerticesColliderAdditionalNormalOffset },
                { SOFT_VERTICES_DISTANCE_LIMIT, softVerticesDistanceLimit },
            };
        }

        #endregion *** Parameter setup ***

        #region *** Sync functions ***

        // Reimplements DAZPhysicsMesh.cs methods SyncGroup[A|B|C|D]SpringMultiplier and SyncSoftVerticesCombinedSpring
        // Circumvents use of softVerticesCombinedSpring value as multiplier on the group specific value, using custom multiplier instead
        private static void SyncGroupSpring(float value, DAZPhysicsMeshSoftVerticesGroup group)
        {
            group.jointSpringNormal = value;
            group.jointSpringTangent = value;
            group.jointSpringTangent2 = value;
            if(group.tieLinkJointSpringAndDamperToNormalSpringAndDamper)
            {
                group.linkSpring = value;
            }
        }

        // Reimplements DAZPhysicsMesh.cs methods SyncGroup[A|B|C|D]DamperMultiplier and SyncSoftVerticesCombinedDamper
        // Circumvents use of softVerticesCombinedDamper value as multiplier on the group specific value, using custom multiplier instead
        private static void SyncGroupDamper(float value, DAZPhysicsMeshSoftVerticesGroup group)
        {
            group.jointDamperNormal = value;
            group.jointDamperTangent = value;
            group.jointDamperTangent2 = value;
            if(group.tieLinkJointSpringAndDamperToNormalSpringAndDamper)
            {
                group.linkDamper = value;
            }
        }

        private static void SyncGroupMass(float value, DAZPhysicsMeshSoftVerticesGroup group)
        {
            group.jointMass = value;
        }

        private static void SyncGroupColliderRadius(float value, DAZPhysicsMeshSoftVerticesGroup group)
        {
            if(group.useParentColliderSettings)
            {
                group.colliderRadiusNoSync = value;
                group.colliderNormalOffsetNoSync = value;
            }

            if(group.useParentColliderSettingsForSecondCollider)
            {
                group.secondColliderRadiusNoSync = value;
                group.secondColliderNormalOffsetNoSync = value;
            }

            if(group.colliderSyncDirty)
            {
                group.SyncColliders();
            }
        }

        private static void SyncGroupBackForce(float value, DAZPhysicsMeshSoftVerticesGroup group)
        {
            group.jointBackForce = value;
        }

        private static void SyncGroupAdditionalNormalOffset(float value, DAZPhysicsMeshSoftVerticesGroup group)
        {
            group.colliderAdditionalNormalOffset = value;
        }

        private static void SyncGroupDistanceLimit(float value, DAZPhysicsMeshSoftVerticesGroup group)
        {
            group.normalDistanceLimit = value;
        }

        private static void SyncGroupBackForceMaxForce(float value, DAZPhysicsMeshSoftVerticesGroup group)
        {
            group.jointBackForceMaxForce = value;
        }

        private static void SyncGroupBackForceThresholdDistance(float value, DAZPhysicsMeshSoftVerticesGroup group)
        {
            group.jointBackForceThresholdDistance = value;
        }

        public static void ReverseSyncSoftPhysicsOn()
        {
            if(_breastPhysicsMesh != null)
            {
                softPhysicsOn.valNoCallback = _breastPhysicsMesh.on;
            }
        }

        public static void ReverseSyncAllowSelfCollision()
        {
            if(_breastPhysicsMesh != null)
            {
                allowSelfCollision.valNoCallback = _breastPhysicsMesh.allowSelfCollision;
            }
        }

        private static void SyncSoftPhysicsOn(bool value)
        {
            if(_breastPhysicsMesh != null)
            {
                _breastPhysicsMesh.on = value;
            }
        }

        private static void SyncAllowSelfCollision(bool value)
        {
            if(_breastPhysicsMesh != null)
            {
                _breastPhysicsMesh.allowSelfCollision = value;
            }
        }

        #endregion *** Sync functions ***

        public static void SyncSoftPhysics()
        {
            if(!Gender.isFemale)
            {
                return;
            }

            SyncSoftPhysicsOn(softPhysicsOn.val);
            SyncAllowSelfCollision(allowSelfCollision.val);
        }

        public static void UpdatePhysics()
        {
            if(!tittyMagic.settingsMonitor.softPhysicsEnabled)
            {
                return;
            }

            float softness = tittyMagic.softnessAmount;
            float quickness = tittyMagic.quicknessAmount;
            parameterGroups.Values
                .ToList()
                .ForEach(paramGroup =>
                {
                    float mass = paramGroup.useRealMass ? MainPhysicsHandler.realMassAmount : MainPhysicsHandler.massAmount;
                    paramGroup.UpdateValue(mass, softness, quickness);
                });
        }

        public static void UpdateRateDependentPhysics()
        {
            if(!tittyMagic.settingsMonitor.softPhysicsEnabled)
            {
                return;
            }

            float softness = tittyMagic.softnessAmount;
            float quickness = tittyMagic.quicknessAmount;
            parameterGroups.Values
                .Where(paramGroup => paramGroup.dependOnPhysicsRate)
                .ToList()
                .ForEach(paramGroup =>
                {
                    float mass = paramGroup.useRealMass ? MainPhysicsHandler.realMassAmount : MainPhysicsHandler.massAmount;
                    paramGroup.UpdateValue(mass, softness, quickness);
                });
        }

        private static bool _originalSoftPhysicsOn;
        private static bool _originalAllowSelfCollision;

        public static void SaveOriginalBoolParamValues()
        {
            if(_breastPhysicsMesh == null)
            {
                return;
            }

            _originalSoftPhysicsOn = _breastPhysicsMesh.on;
            _originalAllowSelfCollision = _breastPhysicsMesh.allowSelfCollision;
        }

        public static void RestoreOriginalPhysics()
        {
            if(!_initDone || _breastPhysicsMesh == null)
            {
                return;
            }

            _breastPhysicsMesh.on = _originalSoftPhysicsOn;
            _breastPhysicsMesh.allowSelfCollision = _originalAllowSelfCollision;

            foreach(string name in _breastPhysicsMeshFloatParamNames)
            {
                /* Set a value that is different from the original, then restore the original
                 * in order to trigger VaM's internal sync
                 */
                var paramJsf = _breastPhysicsMesh.GetFloatJSONParam(name);
                float original = paramJsf.val;
                paramJsf.valNoCallback = Math.Abs(paramJsf.val - paramJsf.min) > 0.01f
                    ? paramJsf.min
                    : paramJsf.max;
                paramJsf.val = original;
            }
        }

        private static Dictionary<string, string> CreateInfoTexts()
        {
            Func<string> springText = () =>
            {
                var sb = new StringBuilder();
                sb.Append("Magnitude of the spring that holds each soft joint in its target position.");
                sb.Append("\n\n");
                sb.Append("Low fat spring makes breast fat soft and slow. High fat spring makes it rigid and");
                sb.Append(" quick to return to its normal shape.");
                return sb.ToString();
            };

            Func<string> damperText = () =>
            {
                var sb = new StringBuilder();
                sb.Append("Magnitude of the damper that reduces oscillation of each soft joint around");
                sb.Append(" its target position.");
                sb.Append("\n\n");
                sb.Append("Low fat damper makes breast fat jiggle more easily.");
                return sb.ToString();
            };

            Func<string> massText = () =>
            {
                var sb = new StringBuilder();
                sb.Append("Mass of each soft joint.");
                sb.Append("\n\n");
                sb.Append("Higher mass makes the breast tissue more dense. The value is an absolute value,");
                sb.Append(" so increasing breast size while keeping fat mass the same reduces density.");
                return sb.ToString();
            };

            Func<string> backForceText = () =>
            {
                var sb = new StringBuilder();
                sb.Append("Force applied to the pectoral joint based on movement of each soft joint.");
                sb.Append("\n\n");
                sb.Append("Low back force (not 0) helps move the breast with collision, and adds a dampening effect.");
                sb.Append(" High force can create a feedback loop that spirals out of control.");
                return sb.ToString();
            };

            Func<string> backForceThresholdDistanceText = () =>
            {
                var sb = new StringBuilder();
                sb.Append("Minimum distance each soft joint needs to move for back force to be applied.");
                sb.Append("\n\n");
                sb.Append("Ensures that small movements of soft joints don't cause the whole breast");
                sb.Append(" to move. Along with Fat Bk Force Threshold, this can be used to prevent");
                sb.Append(" an out of control feedback loop.");
                return sb.ToString();
            };

            Func<string> backForceMaxForceText = () =>
            {
                var sb = new StringBuilder();
                sb.Append("Upper limit on the magnitude of back force.");
                sb.Append("\n\n");
                sb.Append("Along with Fat Bk Force Threshold, this can be used to prevent an out of control feedback loop.");
                return sb.ToString();
            };

            Func<string> colliderRadiusText = () =>
            {
                var sb = new StringBuilder();
                sb.Append("Radius of each soft collider.");
                sb.Append("\n\n");
                sb.Append("Since the number of soft colliders is fixed, the radius scales with breast size.");
                return sb.ToString();
            };

            Func<string> colliderAdditionalNormalOffsetText = () =>
            {
                var sb = new StringBuilder();
                sb.Append("Offset of soft collider positions relative to skin surface.");
                sb.Append("\n\n");
                sb.Append("Negative values pull colliders out from the breast, positive values push them into the breast.");
                return sb.ToString();
            };

            Func<string> distanceLimitText = () =>
            {
                var sb = new StringBuilder();
                sb.Append("The maximum distance each soft joint can move away from its target position.");
                return sb.ToString();
            };

            return new Dictionary<string, string>
            {
                { SOFT_VERTICES_SPRING, springText() },
                { SOFT_VERTICES_DAMPER, damperText() },
                { SOFT_VERTICES_MASS, massText() },
                { SOFT_VERTICES_BACK_FORCE, backForceText() },
                { SOFT_VERTICES_BACK_FORCE_THRESHOLD_DISTANCE, backForceThresholdDistanceText() },
                { SOFT_VERTICES_BACK_FORCE_MAX_FORCE, backForceMaxForceText() },
                { SOFT_VERTICES_COLLIDER_RADIUS, colliderRadiusText() },
                { SOFT_VERTICES_COLLIDER_ADDITIONAL_NORMAL_OFFSET, colliderAdditionalNormalOffsetText() },
                { SOFT_VERTICES_DISTANCE_LIMIT, distanceLimitText() },
            };
        }
    }
}
