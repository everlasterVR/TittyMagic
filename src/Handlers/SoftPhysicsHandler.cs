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
            SetupPhysicsParameterGroups();

            var texts = CreateInfoTexts();
            parameterGroups.ForEach(param => param.Value.infoText = texts[param.Key]);
        }

        #region *** Parameter setup ***

        private SoftGroupPhysicsParameter NewGroupParameter(
            string side,
            string group,
            StaticPhysicsConfig config,
            Action<float, DAZPhysicsMeshSoftVerticesGroup> syncCallback,
            JSONStorableFloat baseValueJsf
        )
        {
            const float min = 0;
            const float max = 5;
            var multiplierJsf = new JSONStorableFloat($"{group} {MULTIPLIER}", 1, min, max);
            var currentJsf = new JSONStorableFloat($"{group} {CURRENT_VALUE}", 1, min, max);
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
                config = new StaticPhysicsConfig(500f, 500f, 62f),
                valueFormat = "F2",
            };

            var groupConfigs = new Dictionary<string, StaticPhysicsConfig>
            {
                { MAIN, new StaticPhysicsConfig(5f, 5f, 1f) },
                { OUTER, new StaticPhysicsConfig(5f, 5f, 1f) },
                { AREOLA, new StaticPhysicsConfig(2.29f, 1.30f, 2.29f) },
                { NIPPLE, new StaticPhysicsConfig(2.29f, 1.30f, 2.29f) },
            };

            parameter.groupMultiplierParams = allGroups.ToDictionary(
                group => group,
                group => NewGroupParameter(
                    side,
                    group,
                    groupConfigs[group],
                    SyncGroupSpring,
                    parameter.valueJsf
                ));

            return parameter;
        }

        private PhysicsParameter NewDamperParameter(string side)
        {
            var parameter = new PhysicsParameter(new JSONStorableFloat(VALUE, 0, 0, 10))
            {
                config = new StaticPhysicsConfig(10.0f, 10.0f, 0.90f),
                quicknessOffsetConfig = new StaticPhysicsConfig(-0.75f, -0.90f, -0.45f),
                slownessOffsetConfig = new StaticPhysicsConfig(1.125f, 1.35f, 0.675f),
                valueFormat = "F3",
            };

            var groupConfigs = new Dictionary<string, StaticPhysicsConfig>
            {
                { MAIN, new StaticPhysicsConfig(1f, 1f, 1f) },
                { OUTER, new StaticPhysicsConfig(1f, 1f, 1f) },
                { AREOLA, new StaticPhysicsConfig(1.81f, 1.22f, 1.81f) },
                { NIPPLE, new StaticPhysicsConfig(1.81f, 1.22f, 1.81f) },
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
            var parameter = new PhysicsParameter(new JSONStorableFloat(VALUE, 0, 0.05f, 0.5f))
            {
                config = new StaticPhysicsConfig(0.050f, 0.130f, 0.085f),
                quicknessOffsetConfig = new StaticPhysicsConfig(0.000f, -0.048f, -0.028f),
                slownessOffsetConfig = new StaticPhysicsConfig(0.012f, 0.060f, 0.040f),
                valueFormat = "F3",
            };

            var groupConfigs = new Dictionary<string, StaticPhysicsConfig>
            {
                { MAIN, new StaticPhysicsConfig(1f, 1f, 1f) },
                { OUTER, new StaticPhysicsConfig(1f, 1f, 1f) },
                { AREOLA, new StaticPhysicsConfig(1f, 1f, 1f) },
                { NIPPLE, new StaticPhysicsConfig(1f, 1f, 1f) },
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
            var parameter = new PhysicsParameter(new JSONStorableFloat(VALUE, 0, 0, 0.07f))
            {
                config = new StaticPhysicsConfig(0.024f, 0.037f, 0.028f),
                valueFormat = "F3",
            };

            var groupConfigs = new Dictionary<string, StaticPhysicsConfig>
            {
                { MAIN, new StaticPhysicsConfig(1f, 1f, 1f) },
                { OUTER, new StaticPhysicsConfig(1f, 1f, 1f) },
                { AREOLA, new StaticPhysicsConfig(1f, 1f, 1f) },
                { NIPPLE, new StaticPhysicsConfig(1f, 1f, 1f) },
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
            var parameter = new PhysicsParameter(new JSONStorableFloat(VALUE, 0, -0.01f, 0.01f))
            {
                config = new StaticPhysicsConfig(0.001f, 0.001f, 0.001f),
                valueFormat = "F3",
            };

            var groupConfigs = new Dictionary<string, StaticPhysicsConfig>
            {
                { MAIN, new StaticPhysicsConfig(1f, 1f, 1f) },
                { OUTER, new StaticPhysicsConfig(1f, 1f, 1f) },
                { AREOLA, new StaticPhysicsConfig(1f, 1f, 1f) },
                { NIPPLE, new StaticPhysicsConfig(1f, 1f, 1f) },
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
            var parameter = new PhysicsParameter(new JSONStorableFloat(VALUE, 0, 0, 0.1f))
            {
                config = new StaticPhysicsConfig(0.020f, 0.068f, 0.028f),
                quicknessOffsetConfig = new StaticPhysicsConfig(0.000f, 0.000f, 0.024f),
                slownessOffsetConfig = new StaticPhysicsConfig(0.000f, 0.000f, -0.008f),
                valueFormat = "F3",
            };

            var groupConfigs = new Dictionary<string, StaticPhysicsConfig>
            {
                { MAIN, new StaticPhysicsConfig(1f, 1f, 1f) },
                { OUTER, new StaticPhysicsConfig(1f, 1f, 1f) },
                { AREOLA, new StaticPhysicsConfig(1f, 1f, 1f) },
                { NIPPLE, new StaticPhysicsConfig(1f, 1f, 1f) },
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
            var parameter = new PhysicsParameter(new JSONStorableFloat(VALUE, 0, 0, 50))
            {
                config = new StaticPhysicsConfig(50f, 55.6f, 9.3f),
                quicknessOffsetConfig = new StaticPhysicsConfig(-2.6f, -4f, -2.33f),
                slownessOffsetConfig = new StaticPhysicsConfig(0.8f, 1.33f, 0.77f),
                valueFormat = "F2",
            };

            var groupConfigs = new Dictionary<string, StaticPhysicsConfig>
            {
                { MAIN, new StaticPhysicsConfig(1f, 1f, 1f) },
                { OUTER, new StaticPhysicsConfig(1f, 1f, 1f) },
                { AREOLA, new StaticPhysicsConfig(1f, 1f, 1f) },
                { NIPPLE, new StaticPhysicsConfig(1f, 1f, 1f) },
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
            var parameter = new PhysicsParameter(new JSONStorableFloat(VALUE, 0, 0, 50))
            {
                config = new StaticPhysicsConfig(50f, 50f, 50f),
                valueFormat = "F2",
            };

            var groupConfigs = new Dictionary<string, StaticPhysicsConfig>
            {
                { MAIN, new StaticPhysicsConfig(1f, 1f, 1f) },
                { OUTER, new StaticPhysicsConfig(1f, 1f, 1f) },
                { AREOLA, new StaticPhysicsConfig(1f, 1f, 1f) },
                { NIPPLE, new StaticPhysicsConfig(1f, 1f, 1f) },
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
                config = new StaticPhysicsConfig(0f, 0f, 0f),
                valueFormat = "F2",
            };

            var groupConfigs = new Dictionary<string, StaticPhysicsConfig>
            {
                { MAIN, new StaticPhysicsConfig(1f, 1f, 1f) },
                { OUTER, new StaticPhysicsConfig(1f, 1f, 1f) },
                { AREOLA, new StaticPhysicsConfig(1f, 1f, 1f) },
                { NIPPLE, new StaticPhysicsConfig(1f, 1f, 1f) },
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
                NewSpringParameter(LEFT),
                NewSpringParameter(RIGHT),
                "Fat Spring"
            );
            softVerticesSpring.SetLinearCurvesAroundMidpoint(null, slope: 0.41f);
            softVerticesSpring.SetLinearCurvesAroundMidpoint(MAIN, slope: 0);
            softVerticesSpring.SetLinearCurvesAroundMidpoint(OUTER, slope: 0);

            var softVerticesDamper = new PhysicsParameterGroup(
                NewDamperParameter(LEFT),
                NewDamperParameter(RIGHT),
                "Fat Damper"
            )
            {
                dependOnPhysicsRate = true,
            };
            softVerticesDamper.SetLinearCurvesAroundMidpoint(null, slope: 0.082f);

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
            softVerticesBackForce.SetLinearCurvesAroundMidpoint(null, slope: 0.027f);

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
