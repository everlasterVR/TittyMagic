using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimpleJSON;
using TittyMagic.Configs;
using static TittyMagic.ParamName;
using static TittyMagic.SoftColliderGroup;
using static TittyMagic.Intl;

namespace TittyMagic
{
    internal class SoftPhysicsHandler
    {
        private readonly Script _script;
        private readonly DAZPhysicsMesh _breastPhysicsMesh;
        private readonly List<string> _breastPhysicsMeshFloatParamNames;
        private Dictionary<string, float> _originalBreastPhysicsMeshFloats;
        private bool _originalSoftPhysicsOn;
        private bool _originalAllowSelfCollision;
        private bool _originalAutoFatColliderRadius;

        //Left/Right -> Group name -> Group
        private readonly Dictionary<string, Dictionary<string, DAZPhysicsMeshSoftVerticesGroup>> _softVerticesGroups;

        //Group name -> Group
        public Dictionary<string, PhysicsParameterGroup> parameterGroups { get; private set; }

        public JSONStorableBool softPhysicsOn { get; }
        public JSONStorableBool allowSelfCollision { get; }

        public SoftPhysicsHandler(Script script)
        {
            _script = script;
            if(Gender.isFemale)
            {
                _breastPhysicsMesh = (DAZPhysicsMesh) _script.containingAtom.GetStorableByID("BreastPhysicsMesh");
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

            softPhysicsOn = _script.NewJSONStorableBool(SOFT_PHYSICS_ON, Gender.isFemale, register: Gender.isFemale);
            softPhysicsOn.setCallbackFunction = SyncSoftPhysicsOn;

            allowSelfCollision = _script.NewJSONStorableBool(ALLOW_SELF_COLLISION, Gender.isFemale, register: Gender.isFemale);
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
            SetupPhysicsParameterGroups();

            var texts = CreateInfoTexts();
            foreach(var param in parameterGroups)
            {
                param.Value.infoText = texts[param.Key];
            }
        }

        #region *** Parameter setup ***

        private SoftGroupPhysicsParameter NewGroupParameter(
            string side,
            string group,
            StaticPhysicsConfig config,
            Action<float, DAZPhysicsMeshSoftVerticesGroup> syncCallback,
            JSONStorableFloat baseValueJsf,
            float max = 5
        )
        {
            var multiplierJsf = new JSONStorableFloat($"{group} {MULTIPLIER}", 1, 0, max);
            var currentJsf = new JSONStorableFloat($"{group} {CURRENT_VALUE}", 1, 0, max);
            var offsetJsf = new JSONStorableFloat($"{group} {MULTIPLIER} {OFFSET}", 0, -max, max);
            return new SoftGroupPhysicsParameter(multiplierJsf, currentJsf, offsetJsf)
            {
                config = config,
                sync = value => syncCallback(baseValueJsf.val * value, _softVerticesGroups[side][group]),
            };
        }

        private PhysicsParameter NewSpringParameter(string side)
        {
            var parameter = new PhysicsParameter(new JSONStorableFloat(VALUE, 0, 0, 500))
            {
                config = new StaticPhysicsConfig(
                    180f,
                    softnessCurve: x => -0.62f * Curves.Exponential1(x, 1.90f, 1.74f, 1.17f)
                ),
                valueFormat = "F0",
            };

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

            parameter.groupMultiplierParams = allGroups.ToDictionary(
                group => group,
                group => NewGroupParameter(
                    side,
                    group,
                    groupConfigs[group],
                    SyncGroupSpring,
                    parameter.valueJsf,
                    max: group == NIPPLE ? 10 : 5
                ));

            return parameter;
        }

        private PhysicsParameter NewDamperParameter(string side)
        {
            var parameter = new PhysicsParameter(new JSONStorableFloat(VALUE, 0, 0, 5.00f))
            {
                config = new StaticPhysicsConfig(
                    0.70f,
                    massCurve: x => 0.50f * Curves.Exponential2(x / 1.5f, c: 0.04f, s: 0.04f),
                    softnessCurve: x => -0.67f * Curves.Exponential1(x, 1.90f, 1.74f, 1.17f)
                ),
                quicknessOffsetConfig = new StaticPhysicsConfig(
                    -0.40f,
                    softnessCurve: x => -0.50f * x
                ),
                slownessOffsetConfig = new StaticPhysicsConfig(
                    0.40f,
                    softnessCurve: x => -0.50f * x
                ),
                valueFormat = "F2",
            };

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

            parameter.groupMultiplierParams = allGroups.ToDictionary(
                group => group,
                group => NewGroupParameter(
                    side,
                    group,
                    groupConfigs[group],
                    SyncGroupDamper,
                    parameter.valueJsf
                ));

            return parameter;
        }

        private PhysicsParameter NewSoftVerticesMassParameter(string side)
        {
            var parameter = new PhysicsParameter(new JSONStorableFloat(VALUE, 0, 0.001f, 0.300f))
            {
                config = new StaticPhysicsConfig(
                    0.050f,
                    softnessCurve: x => 1.40f * Curves.Exponential1(x, 2.30f, 1.74f, 1.17f)
                ),
                quicknessOffsetConfig = new StaticPhysicsConfig(
                    -0.012f,
                    softnessCurve: x => 2.33f * x
                ),
                slownessOffsetConfig = new StaticPhysicsConfig(
                    0.012f,
                    softnessCurve: x => 2.33f * x
                ),
                valueFormat = "F3",
            };

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
                    AREOLA, new StaticPhysicsConfig(
                        1.00f,
                        softnessCurve: x => -0.12f * Curves.DeemphasizeMiddle(x)
                    )
                },
                {
                    NIPPLE, new StaticPhysicsConfig(
                        1.00f,
                        softnessCurve: x => -0.25f * Curves.DeemphasizeMiddle(x)
                    )
                },
            };

            parameter.groupMultiplierParams = allGroups.ToDictionary(
                group => group,
                group => NewGroupParameter(
                    side,
                    group,
                    groupConfigs[group],
                    SyncGroupMass,
                    parameter.valueJsf
                ));

            return parameter;
        }

        private PhysicsParameter NewColliderRadiusParameter(string side)
        {
            var parameter = new PhysicsParameter(new JSONStorableFloat(VALUE, 0, 0, 0.060f))
            {
                config = new StaticPhysicsConfig(
                    0.016f,
                    // https://www.desmos.com/calculator/rotof03irg
                    massCurve: x => 1.54f * Curves.Exponential1(2 / 3f * x, 1.42f, 4.25f, 1.17f),
                    softnessCurve: x => 0.18f * x
                ),
                valueFormat = "F3",
            };

            var groupConfigs = new Dictionary<string, StaticPhysicsConfig>
            {
                { MAIN, new StaticPhysicsConfig(1.00f) },
                { OUTER, new StaticPhysicsConfig(1.00f) },
                { AREOLA, new StaticPhysicsConfig(1.15f) },
                { NIPPLE, new StaticPhysicsConfig(0.00f) },
            };

            parameter.groupMultiplierParams = allGroups.ToDictionary(
                group => group,
                group => NewGroupParameter(
                    side,
                    group,
                    groupConfigs[group],
                    SyncGroupColliderRadius,
                    parameter.valueJsf
                ));

            return parameter;
        }

        private PhysicsParameter NewColliderAdditionalNormalOffsetParameter(string side)
        {
            var parameter = new PhysicsParameter(new JSONStorableFloat(VALUE, 0, -0.010f, 0.010f))
            {
                config = new StaticPhysicsConfig(0.001f),
                valueFormat = "F3",
            };

            var groupConfigs = new Dictionary<string, StaticPhysicsConfig>
            {
                { MAIN, new StaticPhysicsConfig(1.00f) },
                { OUTER, new StaticPhysicsConfig(1.00f) },
                { AREOLA, new StaticPhysicsConfig(1.00f) },
                { NIPPLE, new StaticPhysicsConfig(1.00f) },
            };

            parameter.groupMultiplierParams = allGroups.ToDictionary(
                group => group,
                group => NewGroupParameter(
                    side,
                    group,
                    groupConfigs[group],
                    SyncGroupAdditionalNormalOffset,
                    parameter.valueJsf
                ));

            return parameter;
        }

        private PhysicsParameter NewDistanceLimitParameter(string side)
        {
            var parameter = new PhysicsParameter(new JSONStorableFloat(VALUE, 0, 0, 0.100f))
            {
                config = new StaticPhysicsConfig(
                    0.020f,
                    massCurve: x => 2.4f * x,
                    softnessCurve: x => 0.4f * x
                ),
                quicknessOffsetConfig = new StaticPhysicsConfig(
                    0.000f,
                    softnessCurve: _ => 0.024f
                ),
                slownessOffsetConfig = new StaticPhysicsConfig(
                    0.000f,
                    softnessCurve: _ => -0.008f
                ),
                valueFormat = "F3",
            };

            var groupConfigs = new Dictionary<string, StaticPhysicsConfig>
            {
                { MAIN, new StaticPhysicsConfig(1.00f) },
                { OUTER, new StaticPhysicsConfig(1.00f) },
                { AREOLA, new StaticPhysicsConfig(1.10f) },
                { NIPPLE, new StaticPhysicsConfig(1.20f) },
            };

            parameter.groupMultiplierParams = allGroups.ToDictionary(
                group => group,
                group => NewGroupParameter(
                    side,
                    group,
                    groupConfigs[group],
                    SyncGroupDistanceLimit,
                    parameter.valueJsf
                ));

            return parameter;
        }

        private PhysicsParameter NewBackForceParameter(string side)
        {
            var parameter = new PhysicsParameter(new JSONStorableFloat(VALUE, 0, 0, 50.00f))
            {
                config = new StaticPhysicsConfig(
                    15.00f,
                    // https://www.desmos.com/calculator/hnhlbofgmz
                    massCurve: x => 0.66f * Curves.InverseSmoothStep(2 / 3f * x, 1.00f, 0.15f, 0.70f),
                    // https://www.desmos.com/calculator/uwfattbhdg
                    softnessCurve: x => -0.89f * Curves.Exponential1(x, 2.34f, 1.76f, 1.01f)
                ),
                quicknessOffsetConfig = new StaticPhysicsConfig(
                    -2.60f,
                    massCurve: x => -0.35f * x,
                    softnessCurve: x => -0.11f * x
                ),
                //TODO curves similar to quickness?
                slownessOffsetConfig = new StaticPhysicsConfig(
                    0.80f,
                    massCurve: x => 0.66f * x,
                    softnessCurve: x => -0.04f * x
                ),
                valueFormat = "F2",
            };

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

            parameter.groupMultiplierParams = allGroups.ToDictionary(
                group => group,
                group => NewGroupParameter(
                    side,
                    group,
                    groupConfigs[group],
                    SyncGroupBackForce,
                    parameter.valueJsf
                ));

            return parameter;
        }

        private PhysicsParameter NewBackForceMaxForceParameter(string side)
        {
            var parameter = new PhysicsParameter(new JSONStorableFloat(VALUE, 0, 0, 50.00f))
            {
                config = new StaticPhysicsConfig(50.00f),
                valueFormat = "F2",
            };

            var groupConfigs = new Dictionary<string, StaticPhysicsConfig>
            {
                { MAIN, new StaticPhysicsConfig(1.00f) },
                { OUTER, new StaticPhysicsConfig(1.00f) },
                { AREOLA, new StaticPhysicsConfig(1.00f) },
                { NIPPLE, new StaticPhysicsConfig(1.00f) },
            };

            parameter.groupMultiplierParams = allGroups.ToDictionary(
                group => group,
                group => NewGroupParameter(
                    side,
                    group,
                    groupConfigs[group],
                    SyncGroupBackForceMaxForce,
                    parameter.valueJsf
                ));

            return parameter;
        }

        private PhysicsParameter NewBackForceThresholdDistanceParameter(string side)
        {
            var parameter = new PhysicsParameter(new JSONStorableFloat(VALUE, 0, 0, 0.030f))
            {
                config = new StaticPhysicsConfig(0.001f),
                valueFormat = "F3",
            };

            var groupConfigs = new Dictionary<string, StaticPhysicsConfig>
            {
                { MAIN, new StaticPhysicsConfig(1.00f) },
                { OUTER, new StaticPhysicsConfig(1.00f) },
                { AREOLA, new StaticPhysicsConfig(1.00f) },
                { NIPPLE, new StaticPhysicsConfig(1.00f) },
            };

            parameter.groupMultiplierParams = allGroups.ToDictionary(
                group => group,
                group => NewGroupParameter(
                    side,
                    group,
                    groupConfigs[group],
                    SyncGroupBackForceThresholdDistance,
                    parameter.valueJsf
                ));

            return parameter;
        }

        private void SetupPhysicsParameterGroups()
        {
            var softVerticesSpring = new PhysicsParameterGroup(
                SOFT_VERTICES_SPRING,
                NewSpringParameter(LEFT),
                NewSpringParameter(RIGHT),
                "Fat Spring"
            );

            var softVerticesDamper = new PhysicsParameterGroup(
                SOFT_VERTICES_DAMPER,
                NewDamperParameter(LEFT),
                NewDamperParameter(RIGHT),
                "Fat Damper"
            )
            {
                dependOnPhysicsRate = true,
            };

            var softVerticesMass = new PhysicsParameterGroup(
                SOFT_VERTICES_MASS,
                NewSoftVerticesMassParameter(LEFT),
                NewSoftVerticesMassParameter(RIGHT),
                "Fat Mass"
            );

            var softVerticesColliderRadius = new PhysicsParameterGroup(
                SOFT_VERTICES_COLLIDER_RADIUS,
                NewColliderRadiusParameter(LEFT),
                NewColliderRadiusParameter(RIGHT),
                "Fat Collider Radius"
            )
            {
                useRealMass = true,
            };

            var softVerticesColliderAdditionalNormalOffset = new PhysicsParameterGroup(
                SOFT_VERTICES_COLLIDER_ADDITIONAL_NORMAL_OFFSET,
                NewColliderAdditionalNormalOffsetParameter(LEFT),
                NewColliderAdditionalNormalOffsetParameter(RIGHT),
                "Fat Collider Depth"
            );

            var softVerticesDistanceLimit = new PhysicsParameterGroup(
                SOFT_VERTICES_DISTANCE_LIMIT,
                NewDistanceLimitParameter(LEFT),
                NewDistanceLimitParameter(RIGHT),
                "Fat Distance Limit"
            )
            {
                useRealMass = true,
            };

            var softVerticesBackForce = new PhysicsParameterGroup(
                SOFT_VERTICES_BACK_FORCE,
                NewBackForceParameter(LEFT),
                NewBackForceParameter(RIGHT),
                "Fat Back Force"
            );

            var softVerticesBackForceMaxForce = new PhysicsParameterGroup(
                SOFT_VERTICES_BACK_FORCE_MAX_FORCE,
                NewBackForceMaxForceParameter(LEFT),
                NewBackForceMaxForceParameter(RIGHT),
                "Fat Bk Force Max Force"
            );

            var softVerticesBackForceThresholdDistance = new PhysicsParameterGroup(
                SOFT_VERTICES_BACK_FORCE_THRESHOLD_DISTANCE,
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

        public void UpdatePhysics()
        {
            if(!_script.settingsMonitor.softPhysicsEnabled)
            {
                return;
            }

            float softness = _script.softnessAmount;
            float quickness = _script.quicknessAmount;
            parameterGroups.Values
                .ToList()
                .ForEach(paramGroup =>
                {
                    float mass = paramGroup.useRealMass ? _script.mainPhysicsHandler.realMassAmount : _script.mainPhysicsHandler.massAmount;
                    paramGroup.UpdateValue(mass, softness, quickness);
                });
        }

        public void UpdateRateDependentPhysics()
        {
            if(!_script.settingsMonitor.softPhysicsEnabled)
            {
                return;
            }

            float softness = _script.softnessAmount;
            float quickness = _script.quicknessAmount;
            parameterGroups.Values
                .Where(paramGroup => paramGroup.dependOnPhysicsRate)
                .ToList()
                .ForEach(paramGroup =>
                {
                    float mass = paramGroup.useRealMass ? _script.mainPhysicsHandler.realMassAmount : _script.mainPhysicsHandler.massAmount;
                    paramGroup.UpdateValue(mass, softness, quickness);
                });
        }

        public void SaveOriginalPhysicsAndSetPluginDefaults()
        {
            _originalBreastPhysicsMeshFloats = new Dictionary<string, float>();
            if(_breastPhysicsMesh != null)
            {
                _originalSoftPhysicsOn = _breastPhysicsMesh.on;
                SyncSoftPhysicsOn(softPhysicsOn.val);

                _originalAllowSelfCollision = _breastPhysicsMesh.allowSelfCollision;
                SyncAllowSelfCollision(allowSelfCollision.val);

                // auto fat collider radius off (no effect)
                _originalAutoFatColliderRadius = _breastPhysicsMesh.softVerticesUseAutoColliderRadius;
                _breastPhysicsMesh.softVerticesUseAutoColliderRadius = false;

                foreach(var group in _breastPhysicsMesh.softVerticesGroups)
                {
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

            foreach(var group in _breastPhysicsMesh.softVerticesGroups)
            {
                group.useParentSettings = true;
            }

            foreach(string jsonParamName in _breastPhysicsMeshFloatParamNames)
            {
                _breastPhysicsMesh.GetFloatJSONParam(jsonParamName).val = _originalBreastPhysicsMeshFloats[jsonParamName];
            }
        }

        public JSONClass GetJSON()
        {
            var jsonClass = new JSONClass();
            jsonClass["originals"] = OriginalsJSON();
            jsonClass["parameters"] = ParametersJSONArray();
            return jsonClass;
        }

        private JSONClass OriginalsJSON()
        {
            var jsonClass = new JSONClass();
            jsonClass[SOFT_PHYSICS_ON].AsBool = _originalSoftPhysicsOn;
            jsonClass[ALLOW_SELF_COLLISION].AsBool = _originalAllowSelfCollision;
            jsonClass["breastPhysicsMeshFloats"] = JSONUtils.JSONArrayFromDictionary(_originalBreastPhysicsMeshFloats);
            jsonClass[SOFT_VERTICES_USE_AUTO_COLLIDER_RADIUS].AsBool = _originalAutoFatColliderRadius;
            return jsonClass;
        }

        private JSONArray ParametersJSONArray()
        {
            var jsonArray = new JSONArray();
            foreach(var group in parameterGroups)
            {
                var groupJsonClass = group.Value.GetJSON();
                if(groupJsonClass != null)
                {
                    jsonArray.Add(groupJsonClass);
                }
            }

            return jsonArray;
        }

        public void RestoreFromJSON(JSONClass jsonClass)
        {
            if(jsonClass.HasKey("originals"))
            {
                RestoreOriginalsFromJSON(jsonClass["originals"].AsObject);
            }

            if(jsonClass.HasKey("parameters"))
            {
                RestoreParametersFromJSON(jsonClass["parameters"].AsArray);
            }
        }

        private void RestoreOriginalsFromJSON(JSONClass jsonClass)
        {
            if(jsonClass.HasKey(SOFT_PHYSICS_ON))
            {
                _originalSoftPhysicsOn = jsonClass[SOFT_PHYSICS_ON].AsBool;
            }

            if(jsonClass.HasKey(ALLOW_SELF_COLLISION))
            {
                _originalAllowSelfCollision = jsonClass[ALLOW_SELF_COLLISION].AsBool;
            }

            if(jsonClass.HasKey(SOFT_VERTICES_USE_AUTO_COLLIDER_RADIUS))
            {
                _originalAutoFatColliderRadius = jsonClass[SOFT_VERTICES_USE_AUTO_COLLIDER_RADIUS].AsBool;
            }

            if(jsonClass.HasKey("breastPhysicsMeshFloats"))
            {
                var breastPhysicsMeshFloats = jsonClass["breastPhysicsMeshFloats"].AsArray;
                foreach(JSONClass jc in breastPhysicsMeshFloats)
                {
                    _originalBreastPhysicsMeshFloats[jc["id"].Value] = jc["value"].AsFloat;
                }
            }
        }

        private void RestoreParametersFromJSON(JSONArray jsonArray)
        {
            foreach(JSONClass jc in jsonArray)
            {
                parameterGroups[jc["id"].Value].RestoreFromJSON(jc);
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

            return new Dictionary<string, string>()
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
