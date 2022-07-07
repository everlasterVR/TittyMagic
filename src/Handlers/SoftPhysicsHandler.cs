using System;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using TittyMagic.Configs;
using static TittyMagic.ParamName;
using static TittyMagic.SoftColliderGroup;
using static TittyMagic.Intl;

namespace TittyMagic
{
    internal class SoftPhysicsHandler
    {
        private readonly DAZPhysicsMesh _breastPhysicsMesh;
        private readonly List<string> _breastPhysicsMeshFloatParamNames;
        private Dictionary<string, float> _originalBreastPhysicsMeshFloats;
        private bool _originalSoftPhysicsOn;
        private bool _originalAllowSelfCollision;
        private bool _originalAutoFatColliderRadius;

        //Group name -> Value
        private Dictionary<string, bool> _originalGroupsUseParentSettings;

        //Left/Right -> Parameter name -> Value
        private Dictionary<string, Dictionary<string, float>> _baseValues;

        //Left/Right -> Group name -> Group
        private readonly Dictionary<string, Dictionary<string, DAZPhysicsMeshSoftVerticesGroup>> _softVerticesGroups;

        //Group name -> Group
        public Dictionary<string, PhysicsParameterGroup> parameterGroups { get; private set; }

        public JSONStorableBool softPhysicsOn { get; }
        public JSONStorableBool allowSelfCollision { get; }

        public SoftPhysicsHandler(MVRScript script)
        {
            if(Gender.isFemale)
            {
                _breastPhysicsMesh = (DAZPhysicsMesh) script.containingAtom.GetStorableByID("BreastPhysicsMesh");
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

            softPhysicsOn = script.NewJSONStorableBool(SOFT_PHYSICS_ON, Gender.isFemale, register: Gender.isFemale);
            softPhysicsOn.setCallbackFunction = SyncSoftPhysicsOn;

            allowSelfCollision = script.NewJSONStorableBool(ALLOW_SELF_COLLISION, Gender.isFemale, register: Gender.isFemale);
            allowSelfCollision.setCallbackFunction = SyncAllowSelfCollision;

            if(!Gender.isFemale)
            {
                softPhysicsOn.val = false;
                allowSelfCollision.val = false;
            }

            SaveOriginalPhysicsAndSetPluginDefaults();
        }

        public void LoadSettings()
        {
            _baseValues = new Dictionary<string, Dictionary<string, float>>
            {
                { LEFT, new Dictionary<string, float>() },
                { RIGHT, new Dictionary<string, float>() },
            };
            SetupPhysicsParameterGroups();

            var texts = CreateInfoTexts();
            parameterGroups.ForEach(param => param.Value.infoText = texts[param.Key]);
        }

        #region *** Parameter setup ***

        private SoftGroupPhysicsParameter NewGroupParameter(
            string side,
            string group,
            string parameterName,
            StaticPhysicsConfig config,
            Action<float, float, DAZPhysicsMeshSoftVerticesGroup> syncCallback,
            StaticPhysicsConfig quicknessOffsetConfig = null,
            StaticPhysicsConfig slownessOffsetConfig = null
        )
        {
            var baseStorable = new JSONStorableFloat($"{group} {MULTIPLIER}", 1, 0, 5);
            var currentStorable = new JSONStorableFloat($"{group} {CURRENT_VALUE}", baseStorable.defaultVal, baseStorable.min, baseStorable.max);
            var offsetStorable = new JSONStorableFloat($"{group} {OFFSET}", 0, -baseStorable.max, baseStorable.max);
            return new SoftGroupPhysicsParameter(baseStorable, currentStorable, offsetStorable)
            {
                config = config,
                quicknessOffsetConfig = quicknessOffsetConfig,
                slownessOffsetConfig = slownessOffsetConfig,
                sync = value => syncCallback(value, _baseValues[side][parameterName], _softVerticesGroups[side][group]),
            };
        }

        private PhysicsParameter NewSpringParameter(string parameterName, string side) =>
            new PhysicsParameter(new JSONStorableFloat(CURRENT_VALUE, 0, 0, 500))
            {
                config = new StaticPhysicsConfig(500f, 500f, 62f),
                sync = value => _baseValues[side][parameterName] = value,
                groupMultiplierParams = new Dictionary<string, SoftGroupPhysicsParameter>
                {
                    {
                        MAIN, NewGroupParameter(
                            side,
                            MAIN,
                            parameterName,
                            new StaticPhysicsConfig(5f, 5f, 1f),
                            SyncGroupSpringMultiplier
                        )
                    },
                    {
                        OUTER, NewGroupParameter(
                            side,
                            OUTER,
                            parameterName,
                            new StaticPhysicsConfig(5f, 5f, 1f),
                            SyncGroupSpringMultiplier
                        )
                    },
                    {
                        AREOLA, NewGroupParameter(
                            side,
                            AREOLA,
                            parameterName,
                            new StaticPhysicsConfig(2.29f, 1.30f, 2.29f),
                            SyncGroupSpringMultiplier
                        )
                    },
                    {
                        NIPPLE, NewGroupParameter(
                            side,
                            NIPPLE,
                            parameterName,
                            new StaticPhysicsConfig(2.29f, 1.30f, 2.29f),
                            SyncGroupSpringMultiplier
                        )
                    },
                },
            };

        private PhysicsParameter NewDamperParameter(string parameterName, string side) =>
            new PhysicsParameter(new JSONStorableFloat(CURRENT_VALUE, 0, 0, 10))
            {
                config = new StaticPhysicsConfig(10.0f, 10.0f, 0.90f),
                quicknessOffsetConfig = new StaticPhysicsConfig(-0.75f, -0.90f, -0.45f),
                slownessOffsetConfig = new StaticPhysicsConfig(1.125f, 1.35f, 0.675f),
                sync = value => _baseValues[side][parameterName] = value,
                groupMultiplierParams = new Dictionary<string, SoftGroupPhysicsParameter>
                {
                    {
                        MAIN, NewGroupParameter(
                            side,
                            MAIN,
                            parameterName,
                            new StaticPhysicsConfig(1f, 1f, 1f),
                            SyncGroupDamperMultiplier
                        )
                    },
                    {
                        OUTER, NewGroupParameter(
                            side,
                            OUTER,
                            parameterName,
                            new StaticPhysicsConfig(1f, 1f, 1f),
                            SyncGroupDamperMultiplier
                        )
                    },
                    {
                        AREOLA, NewGroupParameter(
                            side,
                            AREOLA,
                            parameterName,
                            new StaticPhysicsConfig(1.81f, 1.22f, 1.81f),
                            SyncGroupDamperMultiplier
                        )
                    },
                    {
                        NIPPLE, NewGroupParameter(
                            side,
                            NIPPLE,
                            parameterName,
                            new StaticPhysicsConfig(1.81f, 1.22f, 1.81f),
                            SyncGroupDamperMultiplier
                        )
                    },
                },
            };

        private PhysicsParameter NewSoftVerticesMassParameter(string parameterName, string side) =>
            new PhysicsParameter(new JSONStorableFloat(CURRENT_VALUE, 0, 0.05f, 0.5f))
            {
                config = new StaticPhysicsConfig(0.050f, 0.130f, 0.085f),
                quicknessOffsetConfig = new StaticPhysicsConfig(0.000f, -0.048f, -0.028f),
                slownessOffsetConfig = new StaticPhysicsConfig(0.012f, 0.060f, 0.040f),
                sync = value => _baseValues[side][parameterName] = value,
                groupMultiplierParams = new Dictionary<string, SoftGroupPhysicsParameter>
                {
                    {
                        MAIN, NewGroupParameter(
                            side,
                            MAIN,
                            parameterName,
                            new StaticPhysicsConfig(1f, 1f, 1f),
                            SyncGroupMassMultiplier
                        )
                    },
                    {
                        OUTER, NewGroupParameter(
                            side,
                            OUTER,
                            parameterName,
                            new StaticPhysicsConfig(1f, 1f, 1f),
                            SyncGroupMassMultiplier
                        )
                    },
                    {
                        AREOLA, NewGroupParameter(
                            side,
                            AREOLA,
                            parameterName,
                            new StaticPhysicsConfig(1f, 1f, 1f),
                            SyncGroupMassMultiplier
                        )
                    },
                    {
                        NIPPLE, NewGroupParameter(
                            side,
                            NIPPLE,
                            parameterName,
                            new StaticPhysicsConfig(1f, 1f, 1f),
                            SyncGroupMassMultiplier
                        )
                    },
                },
            };

        private PhysicsParameter NewColliderRadiusParameter(string parameterName, string side) =>
            new PhysicsParameter(new JSONStorableFloat(CURRENT_VALUE, 0, 0, 0.07f))
            {
                config = new StaticPhysicsConfig(0.024f, 0.037f, 0.028f),
                sync = value => _baseValues[side][parameterName] = value,
                groupMultiplierParams = new Dictionary<string, SoftGroupPhysicsParameter>
                {
                    {
                        MAIN, NewGroupParameter(
                            side,
                            MAIN,
                            parameterName,
                            new StaticPhysicsConfig(1f, 1f, 1f),
                            SyncGroupColliderRadiusMultiplier
                        )
                    },
                    {
                        OUTER, NewGroupParameter(
                            side,
                            OUTER,
                            parameterName,
                            new StaticPhysicsConfig(1f, 1f, 1f),
                            SyncGroupColliderRadiusMultiplier
                        )
                    },
                    {
                        AREOLA, NewGroupParameter(
                            side,
                            AREOLA,
                            parameterName,
                            new StaticPhysicsConfig(1f, 1f, 1f),
                            SyncGroupColliderRadiusMultiplier
                        )
                    },
                    {
                        NIPPLE, NewGroupParameter(
                            side,
                            NIPPLE,
                            parameterName,
                            new StaticPhysicsConfig(1f, 1f, 1f),
                            SyncGroupColliderRadiusMultiplier
                        )
                    },
                },
            };

        private PhysicsParameter NewColliderAdditionalNormalOffsetParameter(string parameterName, string side) =>
            new PhysicsParameter(new JSONStorableFloat(CURRENT_VALUE, 0, -0.01f, 0.01f))
            {
                config = new StaticPhysicsConfig(0.001f, 0.001f, 0.001f),
                sync = value => _baseValues[side][SOFT_VERTICES_COLLIDER_ADDITIONAL_NORMAL_OFFSET] = value,
                groupMultiplierParams = new Dictionary<string, SoftGroupPhysicsParameter>
                {
                    {
                        MAIN, NewGroupParameter(
                            side,
                            MAIN,
                            parameterName,
                            new StaticPhysicsConfig(1f, 1f, 1f),
                            SyncGroupAdditionalNormalOffsetMultiplier
                        )
                    },
                    {
                        OUTER, NewGroupParameter(
                            side,
                            OUTER,
                            parameterName,
                            new StaticPhysicsConfig(1f, 1f, 1f),
                            SyncGroupAdditionalNormalOffsetMultiplier
                        )
                    },
                    {
                        AREOLA, NewGroupParameter(
                            side,
                            AREOLA,
                            parameterName,
                            new StaticPhysicsConfig(1f, 1f, 1f),
                            SyncGroupAdditionalNormalOffsetMultiplier
                        )
                    },
                    {
                        NIPPLE, NewGroupParameter(
                            side,
                            NIPPLE,
                            parameterName,
                            new StaticPhysicsConfig(1f, 1f, 1f),
                            SyncGroupAdditionalNormalOffsetMultiplier
                        )
                    },
                },
            };

        private PhysicsParameter NewDistanceLimitParameter(string parameterName, string side) =>
            new PhysicsParameter(new JSONStorableFloat(CURRENT_VALUE, 0, 0, 0.1f))
            {
                config = new StaticPhysicsConfig(0.020f, 0.068f, 0.028f),
                quicknessOffsetConfig = new StaticPhysicsConfig(0.000f, 0.000f, 0.024f),
                slownessOffsetConfig = new StaticPhysicsConfig(0.000f, 0.000f, -0.008f),
                sync = value => _baseValues[side][SOFT_VERTICES_DISTANCE_LIMIT] = value,
                groupMultiplierParams = new Dictionary<string, SoftGroupPhysicsParameter>
                {
                    {
                        MAIN, NewGroupParameter(
                            side,
                            MAIN,
                            parameterName,
                            new StaticPhysicsConfig(1f, 1f, 1f),
                            SyncGroupDistanceLimitMultiplier
                        )
                    },
                    {
                        OUTER, NewGroupParameter(
                            side,
                            OUTER,
                            parameterName,
                            new StaticPhysicsConfig(1f, 1f, 1f),
                            SyncGroupDistanceLimitMultiplier
                        )
                    },
                    {
                        AREOLA, NewGroupParameter(
                            side,
                            AREOLA,
                            parameterName,
                            new StaticPhysicsConfig(1f, 1f, 1f),
                            SyncGroupDistanceLimitMultiplier
                        )
                    },
                    {
                        NIPPLE, NewGroupParameter(
                            side,
                            NIPPLE,
                            parameterName,
                            new StaticPhysicsConfig(1f, 1f, 1f),
                            SyncGroupDistanceLimitMultiplier
                        )
                    },
                },
            };

        private PhysicsParameter NewBackForceParameter(string parameterName, string side) =>
            new PhysicsParameter(new JSONStorableFloat(CURRENT_VALUE, 0, 0, 50))
            {
                config = new StaticPhysicsConfig(50f, 55.6f, 9.3f),
                quicknessOffsetConfig = new StaticPhysicsConfig(-2.6f, -4f, -2.33f),
                slownessOffsetConfig = new StaticPhysicsConfig(0.8f, 1.33f, 0.77f),
                sync = value => _baseValues[side][parameterName] = value,
                groupMultiplierParams = new Dictionary<string, SoftGroupPhysicsParameter>
                {
                    {
                        MAIN, NewGroupParameter(
                            side,
                            MAIN,
                            parameterName,
                            new StaticPhysicsConfig(1f, 1f, 1f),
                            SyncGroupBackForceMultiplier
                        )
                    },
                    {
                        OUTER, NewGroupParameter(
                            side,
                            OUTER,
                            parameterName,
                            new StaticPhysicsConfig(1f, 1f, 1f),
                            SyncGroupBackForceMultiplier
                        )
                    },
                    {
                        AREOLA, NewGroupParameter(
                            side,
                            AREOLA,
                            parameterName,
                            new StaticPhysicsConfig(1f, 1f, 1f),
                            SyncGroupBackForceMultiplier
                        )
                    },
                    {
                        NIPPLE, NewGroupParameter(
                            side,
                            NIPPLE,
                            parameterName,
                            new StaticPhysicsConfig(1f, 1f, 1f),
                            SyncGroupBackForceMultiplier
                        )
                    },
                },
            };

        private PhysicsParameter NewBackForceMaxForceParameter(string parameterName, string side) =>
            new PhysicsParameter(new JSONStorableFloat(CURRENT_VALUE, 0, 0, 50))
            {
                config = new StaticPhysicsConfig(50f, 50f, 50f),
                sync = value => _baseValues[side][parameterName] = value,
                groupMultiplierParams = new Dictionary<string, SoftGroupPhysicsParameter>
                {
                    {
                        MAIN, NewGroupParameter(
                            side,
                            MAIN,
                            parameterName,
                            new StaticPhysicsConfig(1f, 1f, 1f),
                            SyncGroupBackForceMaxForceMultiplier
                        )
                    },
                    {
                        OUTER, NewGroupParameter(
                            side,
                            OUTER,
                            parameterName,
                            new StaticPhysicsConfig(1f, 1f, 1f),
                            SyncGroupBackForceMaxForceMultiplier
                        )
                    },
                    {
                        AREOLA, NewGroupParameter(
                            side,
                            AREOLA,
                            parameterName,
                            new StaticPhysicsConfig(1f, 1f, 1f),
                            SyncGroupBackForceMaxForceMultiplier
                        )
                    },
                    {
                        NIPPLE, NewGroupParameter(
                            side,
                            NIPPLE,
                            parameterName,
                            new StaticPhysicsConfig(1f, 1f, 1f),
                            SyncGroupBackForceMaxForceMultiplier
                        )
                    },
                },
            };

        private PhysicsParameter NewBackForceThresholdDistanceParameter(string parameterName, string side) =>
            new PhysicsParameter(new JSONStorableFloat(CURRENT_VALUE, 0, 0, 0.030f))
            {
                config = new StaticPhysicsConfig(0f, 0f, 0f),
                sync = value => _baseValues[side][SOFT_VERTICES_BACK_FORCE_THRESHOLD_DISTANCE] = value,
                groupMultiplierParams = new Dictionary<string, SoftGroupPhysicsParameter>
                {
                    {
                        MAIN, NewGroupParameter(
                            side,
                            MAIN,
                            parameterName,
                            new StaticPhysicsConfig(1f, 1f, 1f),
                            SyncGroupBackForceThresholdDistanceMultiplier
                        )
                    },
                    {
                        OUTER, NewGroupParameter(
                            side,
                            OUTER,
                            parameterName,
                            new StaticPhysicsConfig(1f, 1f, 1f),
                            SyncGroupBackForceThresholdDistanceMultiplier
                        )
                    },
                    {
                        AREOLA, NewGroupParameter(
                            side,
                            AREOLA,
                            parameterName,
                            new StaticPhysicsConfig(1f, 1f, 1f),
                            SyncGroupBackForceThresholdDistanceMultiplier
                        )
                    },
                    {
                        NIPPLE, NewGroupParameter(
                            side,
                            NIPPLE,
                            parameterName,
                            new StaticPhysicsConfig(1f, 1f, 1f),
                            SyncGroupBackForceThresholdDistanceMultiplier
                        )
                    },
                },
            };

        private void SetupPhysicsParameterGroups()
        {
            var softVerticesSpring = new PhysicsParameterGroup(
                NewSpringParameter(SOFT_VERTICES_SPRING, LEFT),
                NewSpringParameter(SOFT_VERTICES_SPRING, RIGHT),
                "Fat Spring",
                "F2"
            );
            softVerticesSpring.SetLinearCurvesAroundMidpoint(null, slope: 0.41f);
            softVerticesSpring.SetLinearCurvesAroundMidpoint(MAIN, slope: 0);
            softVerticesSpring.SetLinearCurvesAroundMidpoint(OUTER, slope: 0);

            var softVerticesDamper = new PhysicsParameterGroup(
                NewDamperParameter(SOFT_VERTICES_DAMPER, LEFT),
                NewDamperParameter(SOFT_VERTICES_DAMPER, RIGHT),
                "Fat Damper",
                "F3"
            )
            {
                dependOnPhysicsRate = true,
            };
            softVerticesDamper.SetLinearCurvesAroundMidpoint(null, slope: 0.082f);

            var softVerticesMass = new PhysicsParameterGroup(
                NewSoftVerticesMassParameter(SOFT_VERTICES_MASS, LEFT),
                NewSoftVerticesMassParameter(SOFT_VERTICES_MASS, RIGHT),
                "Fat Mass",
                "F3"
            );

            var softVerticesColliderRadius = new PhysicsParameterGroup(
                NewColliderRadiusParameter(SOFT_VERTICES_COLLIDER_RADIUS, LEFT),
                NewColliderRadiusParameter(SOFT_VERTICES_COLLIDER_RADIUS, RIGHT),
                "Fat Collider Radius",
                "F3"
            )
            {
                useRealMass = true,
            };

            var softVerticesColliderAdditionalNormalOffset = new PhysicsParameterGroup(
                NewColliderAdditionalNormalOffsetParameter(SOFT_VERTICES_COLLIDER_ADDITIONAL_NORMAL_OFFSET, LEFT),
                NewColliderAdditionalNormalOffsetParameter(SOFT_VERTICES_COLLIDER_ADDITIONAL_NORMAL_OFFSET, RIGHT),
                "Fat Collider Depth",
                "F3"
            );

            var softVerticesDistanceLimit = new PhysicsParameterGroup(
                NewDistanceLimitParameter(SOFT_VERTICES_DISTANCE_LIMIT, LEFT),
                NewDistanceLimitParameter(SOFT_VERTICES_DISTANCE_LIMIT, RIGHT),
                "Fat Distance Limit",
                "F3"
            )
            {
                useRealMass = true,
            };

            var softVerticesBackForce = new PhysicsParameterGroup(
                NewBackForceParameter(SOFT_VERTICES_BACK_FORCE, LEFT),
                NewBackForceParameter(SOFT_VERTICES_BACK_FORCE, RIGHT),
                "Fat Back Force",
                "F2"
            );
            softVerticesBackForce.SetLinearCurvesAroundMidpoint(null, slope: 0.027f);

            var softVerticesBackForceMaxForce = new PhysicsParameterGroup(
                NewBackForceMaxForceParameter(SOFT_VERTICES_BACK_FORCE_MAX_FORCE, LEFT),
                NewBackForceMaxForceParameter(SOFT_VERTICES_BACK_FORCE_MAX_FORCE, RIGHT),
                "Fat Bk Force Max Force",
                "F2"
            );

            var softVerticesBackForceThresholdDistance = new PhysicsParameterGroup(
                NewBackForceThresholdDistanceParameter(SOFT_VERTICES_BACK_FORCE_THRESHOLD_DISTANCE, LEFT),
                NewBackForceThresholdDistanceParameter(SOFT_VERTICES_BACK_FORCE_THRESHOLD_DISTANCE, RIGHT),
                "Fat Bk Force Threshold",
                "F3"
            );

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
        private static void SyncGroupSpringMultiplier(float multiplier, float baseSpring, DAZPhysicsMeshSoftVerticesGroup group)
        {
            float spring = baseSpring * multiplier;
            group.jointSpringNormal = spring;
            group.jointSpringTangent = spring;
            group.jointSpringTangent2 = spring;
            if(group.tieLinkJointSpringAndDamperToNormalSpringAndDamper)
            {
                group.linkSpring = spring;
            }
        }

        // Reimplements DAZPhysicsMesh.cs methods SyncGroup[A|B|C|D]DamperMultiplier and SyncSoftVerticesCombinedDamper
        // Circumvents use of softVerticesCombinedDamper value as multiplier on the group specific value, using custom multiplier instead
        private static void SyncGroupDamperMultiplier(float multiplier, float baseDamper, DAZPhysicsMeshSoftVerticesGroup group)
        {
            float damper = baseDamper * multiplier;
            group.jointDamperNormal = damper;
            group.jointDamperTangent = damper;
            group.jointDamperTangent2 = damper;
            if(group.tieLinkJointSpringAndDamperToNormalSpringAndDamper)
            {
                group.linkDamper = damper;
            }
        }

        private static void SyncGroupMassMultiplier(float multiplier, float baseMass, DAZPhysicsMeshSoftVerticesGroup group)
        {
            group.jointMass = baseMass * multiplier;
        }

        private static void SyncGroupColliderRadiusMultiplier(float multiplier, float baseRadius, DAZPhysicsMeshSoftVerticesGroup group)
        {
            float colliderRadius = baseRadius * multiplier;
            if(group.useParentColliderSettings)
            {
                group.colliderRadiusNoSync = colliderRadius;
                group.colliderNormalOffsetNoSync = colliderRadius;
            }

            if(group.useParentColliderSettingsForSecondCollider)
            {
                group.secondColliderRadiusNoSync = colliderRadius;
                group.secondColliderNormalOffsetNoSync = colliderRadius;
            }

            if(group.colliderSyncDirty)
            {
                group.SyncColliders();
            }
        }

        private static void SyncGroupBackForceMultiplier(float multiplier, float baseForce, DAZPhysicsMeshSoftVerticesGroup group)
        {
            group.jointBackForce = baseForce * multiplier;
        }

        private static void SyncGroupAdditionalNormalOffsetMultiplier(float multiplier, float baseNormalOffset, DAZPhysicsMeshSoftVerticesGroup group)
        {
            group.colliderAdditionalNormalOffset = baseNormalOffset * multiplier;
        }

        private static void SyncGroupDistanceLimitMultiplier(float multiplier, float baseDistanceLimit, DAZPhysicsMeshSoftVerticesGroup group)
        {
            group.normalDistanceLimit = baseDistanceLimit * multiplier;
        }

        private static void SyncGroupBackForceMaxForceMultiplier(float multiplier, float baseMaxForce, DAZPhysicsMeshSoftVerticesGroup group)
        {
            group.jointBackForceMaxForce = baseMaxForce * multiplier;
        }

        private static void SyncGroupBackForceThresholdDistanceMultiplier(
            float multiplier,
            float baseForceThreshold,
            DAZPhysicsMeshSoftVerticesGroup group
        )
        {
            group.jointBackForceThresholdDistance = baseForceThreshold * multiplier;
        }

        public void ReverseSyncSoftPhysicsOn()
        {
            if(!Gender.isFemale)
            {
                return;
            }

            if(softPhysicsOn.val != _breastPhysicsMesh.on)
            {
                softPhysicsOn.val = _breastPhysicsMesh.on;
            }
        }

        public void ReverseSyncSyncAllowSelfCollision()
        {
            if(!Gender.isFemale)
            {
                return;
            }

            if(allowSelfCollision.val != _breastPhysicsMesh.allowSelfCollision)
            {
                allowSelfCollision.val = _breastPhysicsMesh.allowSelfCollision;
            }
        }

        private void SyncSoftPhysicsOn(bool value)
        {
            if(!Gender.isFemale)
            {
                return;
            }

            _breastPhysicsMesh.on = value;
        }

        private void SyncAllowSelfCollision(bool value)
        {
            if(!Gender.isFemale)
            {
                return;
            }

            _breastPhysicsMesh.allowSelfCollision = value;
        }

        #endregion *** Sync functions ***

        public void UpdatePhysics(
            float massAmount,
            float realMassAmount,
            float softnessAmount,
            float quicknessAmount
        )
        {
            if(!Gender.isFemale)
            {
                return;
            }

            parameterGroups.Values
                .ToList()
                .ForEach(paramGroup =>
                {
                    float massValue = paramGroup.useRealMass ? realMassAmount : massAmount;
                    paramGroup.UpdateValue(massValue, softnessAmount, quicknessAmount);
                });
        }

        public void UpdateRateDependentPhysics(
            float massAmount,
            float realMassAmount,
            float softnessAmount,
            float quicknessAmount
        )
        {
            if(!Gender.isFemale)
            {
                return;
            }

            parameterGroups.Values
                .Where(paramGroup => paramGroup.dependOnPhysicsRate)
                .ToList()
                .ForEach(paramGroup =>
                {
                    float massValue = paramGroup.useRealMass ? realMassAmount : massAmount;
                    paramGroup.UpdateValue(massValue, softnessAmount, quicknessAmount);
                });
        }

        public void UpdateNipplePhysics(float massAmount, float softnessAmount, float nippleErection)
        {
            if(!Gender.isFemale)
            {
                return;
            }

            parameterGroups.Values
                .ToList()
                .ForEach(paramGroup => paramGroup.UpdateNippleValue(massAmount, softnessAmount, nippleErection));
        }

        public void SaveOriginalPhysicsAndSetPluginDefaults()
        {
            _originalBreastPhysicsMeshFloats = new Dictionary<string, float>();
            _originalGroupsUseParentSettings = new Dictionary<string, bool>();
            if(_breastPhysicsMesh != null)
            {
                _originalSoftPhysicsOn = _breastPhysicsMesh.on;
                SyncSoftPhysicsOn(softPhysicsOn.val);

                _originalAllowSelfCollision = _breastPhysicsMesh.allowSelfCollision;
                SyncAllowSelfCollision(allowSelfCollision.val);

                // auto fat collider radius off (no effect)
                _originalAutoFatColliderRadius = _breastPhysicsMesh.softVerticesUseAutoColliderRadius;
                _breastPhysicsMesh.softVerticesUseAutoColliderRadius = false;

                // prevent settings in F Breast Physics 2 from having effect
                foreach(var group in _breastPhysicsMesh.softVerticesGroups)
                {
                    _originalGroupsUseParentSettings[group.name] = group.useParentSettings;
                    group.useParentSettings = false;
                }

                foreach(string jsonParamName in _breastPhysicsMeshFloatParamNames)
                {
                    var paramJsf = _breastPhysicsMesh.GetFloatJSONParam(jsonParamName);
                    _originalBreastPhysicsMeshFloats[jsonParamName] = paramJsf.val;
                    paramJsf.val = 0;
                }
            }
        }

        public void RestoreOriginalPhysics()
        {
            if(!Gender.isFemale)
            {
                return;
            }

            _breastPhysicsMesh.on = _originalSoftPhysicsOn;
            _breastPhysicsMesh.allowSelfCollision = _originalAllowSelfCollision;
            _breastPhysicsMesh.softVerticesUseAutoColliderRadius = _originalAutoFatColliderRadius;

            foreach(string jsonParamName in _breastPhysicsMeshFloatParamNames)
            {
                _breastPhysicsMesh.GetFloatJSONParam(jsonParamName).val = _originalBreastPhysicsMeshFloats[jsonParamName];
            }

            foreach(var group in _breastPhysicsMesh.softVerticesGroups)
            {
                group.useParentSettings = _originalGroupsUseParentSettings[group.name];
            }
        }

        public JSONClass Serialize()
        {
            var jsonClass = new JSONClass();
            if(Gender.isFemale)
            {
                jsonClass[SOFT_PHYSICS_ON].AsBool = _originalSoftPhysicsOn;
                jsonClass[ALLOW_SELF_COLLISION].AsBool = _originalAllowSelfCollision;
                jsonClass["breastPhysicsMeshFloats"] = JSONUtils.JSONArrayFromDictionary(_originalBreastPhysicsMeshFloats);
                jsonClass[SOFT_VERTICES_USE_AUTO_COLLIDER_RADIUS].AsBool = _originalAutoFatColliderRadius;
                jsonClass["groupsUseParentSettings"] = JSONUtils.JSONArrayFromDictionary(_originalGroupsUseParentSettings);
            }

            return jsonClass;
        }

        public void RestoreFromJSON(JSONClass originalJson)
        {
            if(originalJson.HasKey(SOFT_PHYSICS_ON))
            {
                _originalSoftPhysicsOn = originalJson[SOFT_PHYSICS_ON].AsBool;
            }

            if(originalJson.HasKey(ALLOW_SELF_COLLISION))
            {
                _originalAllowSelfCollision = originalJson[ALLOW_SELF_COLLISION].AsBool;
            }

            if(originalJson.HasKey(SOFT_VERTICES_USE_AUTO_COLLIDER_RADIUS))
            {
                _originalAutoFatColliderRadius = originalJson[SOFT_VERTICES_USE_AUTO_COLLIDER_RADIUS].AsBool;
            }

            if(originalJson.HasKey("breastPhysicsMeshFloats"))
            {
                var breastPhysicsMeshFloats = originalJson["breastPhysicsMeshFloats"].AsArray;
                foreach(JSONClass json in breastPhysicsMeshFloats)
                {
                    _originalBreastPhysicsMeshFloats[json["paramName"].Value] = json["value"].AsFloat;
                }
            }

            if(originalJson.HasKey("groupsUseParentSettings"))
            {
                var groupsUseParentSettings = originalJson["groupsUseParentSettings"].AsArray;
                foreach(JSONClass json in groupsUseParentSettings)
                {
                    _originalGroupsUseParentSettings[json["paramName"].Value] = json["value"].AsBool;
                }
            }
        }

        private static Dictionary<string, string> CreateInfoTexts()
        {
            var texts = new Dictionary<string, string>();

            texts[SOFT_VERTICES_SPRING] =
                $"";

            texts[SOFT_VERTICES_DAMPER] =
                $"";

            texts[SOFT_VERTICES_MASS] =
                $"";

            texts[SOFT_VERTICES_BACK_FORCE] =
                $"";

            texts[SOFT_VERTICES_BACK_FORCE_THRESHOLD_DISTANCE] =
                $"";

            texts[SOFT_VERTICES_BACK_FORCE_MAX_FORCE] =
                $"";

            texts[SOFT_VERTICES_COLLIDER_RADIUS] =
                $"";

            texts[SOFT_VERTICES_COLLIDER_ADDITIONAL_NORMAL_OFFSET] =
                $"";

            texts[SOFT_VERTICES_DISTANCE_LIMIT] =
                $"";

            return texts;
        }
    }
}
