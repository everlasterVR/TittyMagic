// ReSharper disable RedundantCast
using System;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using TittyMagic.Configs;
using static TittyMagic.ParamName;
using static TittyMagic.Utils;

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
        private Dictionary<string, bool> _originalGroupsUseParentSettings;

        private readonly DAZPhysicsMeshSoftVerticesGroup _mainLeft;
        private readonly DAZPhysicsMeshSoftVerticesGroup _mainRight;
        private readonly DAZPhysicsMeshSoftVerticesGroup _outerLeft;
        private readonly DAZPhysicsMeshSoftVerticesGroup _outerRight;
        private readonly DAZPhysicsMeshSoftVerticesGroup _areolaLeft;
        private readonly DAZPhysicsMeshSoftVerticesGroup _areolaRight;
        private readonly DAZPhysicsMeshSoftVerticesGroup _nippleLeft;
        private readonly DAZPhysicsMeshSoftVerticesGroup _nippleRight;

        public Dictionary<string, PhysicsParameterGroup> parameterGroups { get; private set; }

        public JSONStorableBool softPhysicsOn { get; }
        public JSONStorableBool allowSelfCollision { get; }

        private float _baseSpringLeft;
        private float _baseSpringRight;
        private float _baseDamperLeft;
        private float _baseDamperRight;

        public SoftPhysicsHandler(MVRScript script)
        {
            if(Gender.isFemale)
            {
                _breastPhysicsMesh = (DAZPhysicsMesh) script.containingAtom.GetStorableByID("BreastPhysicsMesh");
                _breastPhysicsMeshFloatParamNames = _breastPhysicsMesh.GetFloatParamNames();

                var groups = _breastPhysicsMesh.softVerticesGroups;
                _mainLeft = groups.Find(group => group.name == "left");
                _mainRight = groups.Find(group => group.name == "right");
                _outerLeft = groups.Find(group => group.name == "leftouter");
                _outerRight = groups.Find(group => group.name == "rightouter");
                _areolaLeft = groups.Find(group => group.name == "leftareola");
                _areolaRight = groups.Find(group => group.name == "rightareola");
                _nippleLeft = groups.Find(group => group.name == "leftnipple");
                _nippleRight = groups.Find(group => group.name == "rightnipple");
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

        private SoftGroupPhysicsParameter NewMainSpringParameter(bool left) =>
            new SoftGroupPhysicsParameter(new JSONStorableFloat("Main Base Value", 0, 0, 5))
            {
                config = new StaticPhysicsConfig(5f, 5f, 1f),
                sync = left
                    ? (Action<float>) (value => SyncGroupSpringMultiplier(value, _baseSpringLeft, _mainLeft))
                    : (Action<float>) (value => SyncGroupSpringMultiplier(value, _baseSpringRight, _mainRight)),
            };

        private SoftGroupPhysicsParameter NewOuterSpringParameter(bool left) =>
            new SoftGroupPhysicsParameter(new JSONStorableFloat("Outer Base Value", 0, 0, 5))
            {
                config = new StaticPhysicsConfig(5f, 5f, 1f),
                sync = left
                    ? (Action<float>) (value => SyncGroupSpringMultiplier(value, _baseSpringLeft, _outerLeft))
                    : (Action<float>) (value => SyncGroupSpringMultiplier(value, _baseSpringRight, _outerRight)),
            };

        private SoftGroupPhysicsParameter NewAreolaSpringParameter(bool left) =>
            new SoftGroupPhysicsParameter(new JSONStorableFloat("Areola Base Value", 0, 0, 5))
            {
                config = new StaticPhysicsConfig(2.29f, 1.30f, 2.29f),
                sync = left
                    ? (Action<float>) (value => SyncGroupSpringMultiplier(value, _baseSpringLeft, _areolaLeft))
                    : (Action<float>) (value => SyncGroupSpringMultiplier(value, _baseSpringRight, _areolaRight)),
            };

        private SoftGroupPhysicsParameter NewNippleSpringParameter(bool left) =>
            new SoftGroupPhysicsParameter(new JSONStorableFloat("Nipple Base Value", 0, 0, 5))
            {
                config = new StaticPhysicsConfig(2.29f, 1.30f, 2.29f),
                sync = left
                    ? (Action<float>) (value => SyncGroupSpringMultiplier(value, _baseSpringLeft, _nippleLeft))
                    : (Action<float>) (value => SyncGroupSpringMultiplier(value, _baseSpringRight, _nippleRight)),
            };

        private PhysicsParameter NewSpringParameter(bool left) =>
            new PhysicsParameter(new JSONStorableFloat("Base Value", 0, 0, 500))
            {
                config = new StaticPhysicsConfig(500f, 500f, 62f),
                sync = left
                    ? (Action<float>) (value => _baseSpringLeft = value)
                    : (Action<float>) (value => _baseSpringRight = value),
                groupMultiplierParams = new Dictionary<string, SoftGroupPhysicsParameter>
                {
                    { SoftColliderGroup.MAIN, NewMainSpringParameter(left) },
                    { SoftColliderGroup.OUTER, NewOuterSpringParameter(left) },
                    { SoftColliderGroup.AREOLA, NewAreolaSpringParameter(left) },
                    { SoftColliderGroup.NIPPLE, NewNippleSpringParameter(left) },
                },
            };

        private SoftGroupPhysicsParameter NewMainDamperParameter(bool left) =>
            new SoftGroupPhysicsParameter(new JSONStorableFloat("Main Base Value", 0, 0, 5), currentValueJsf: null)
            {
                config = new StaticPhysicsConfig(1f, 1f, 1f),
                sync = left
                    ? (Action<float>) (value => SyncGroupDamperMultiplier(value, _baseDamperLeft, _mainLeft))
                    : (Action<float>) (value => SyncGroupDamperMultiplier(value, _baseDamperRight, _mainRight)),
            };

        private SoftGroupPhysicsParameter NewOuterDamperParameter(bool left) =>
            new SoftGroupPhysicsParameter(new JSONStorableFloat("Outer Base Value", 0, 0, 5), currentValueJsf: null)
            {
                config = new StaticPhysicsConfig(1f, 1f, 1f),
                sync = left
                    ? (Action<float>) (value => SyncGroupDamperMultiplier(value, _baseDamperLeft, _outerLeft))
                    : (Action<float>) (value => SyncGroupDamperMultiplier(value, _baseDamperRight, _outerRight)),
            };

        private SoftGroupPhysicsParameter NewAreolaDamperParameter(bool left) =>
            new SoftGroupPhysicsParameter(new JSONStorableFloat("Areola Base Value", 0, 0, 5), currentValueJsf: null)
            {
                config = new StaticPhysicsConfig(1.81f, 1.22f, 1.81f),
                sync = left
                    ? (Action<float>) (value => SyncGroupDamperMultiplier(value, _baseDamperLeft, _areolaLeft))
                    : (Action<float>) (value => SyncGroupDamperMultiplier(value, _baseDamperRight, _areolaRight)),
            };

        private SoftGroupPhysicsParameter NewNippleDamperParameter(bool left) =>
            new SoftGroupPhysicsParameter(new JSONStorableFloat("Nipple Base Value", 0, 0, 5), currentValueJsf: null)
            {
                config = new StaticPhysicsConfig(1.81f, 1.22f, 1.81f),
                sync = left
                    ? (Action<float>) (value => SyncGroupDamperMultiplier(value, _baseDamperLeft, _nippleLeft))
                    : (Action<float>) (value => SyncGroupDamperMultiplier(value, _baseDamperRight, _nippleRight)),
            };

        private PhysicsParameter NewDamperParameter(bool left) =>
            new PhysicsParameter(new JSONStorableFloat("Base Value", 0, 0, 10), currentValueJsf: null)
            {
                config = new StaticPhysicsConfig(10.0f, 10.0f, 0.90f),
                quicknessOffsetConfig = new StaticPhysicsConfig(-0.75f, -0.90f, -0.45f),
                slownessOffsetConfig = new StaticPhysicsConfig(1.125f, 1.35f, 0.675f),
                sync = left
                    ? (Action<float>) (value => _baseDamperLeft = value)
                    : (Action<float>) (value => _baseDamperRight = value),
                groupMultiplierParams = new Dictionary<string, SoftGroupPhysicsParameter>
                {
                    { SoftColliderGroup.MAIN, NewMainDamperParameter(left) },
                    { SoftColliderGroup.OUTER, NewOuterDamperParameter(left) },
                    { SoftColliderGroup.AREOLA, NewAreolaDamperParameter(left) },
                    { SoftColliderGroup.NIPPLE, NewNippleDamperParameter(left) },
                },
            };

        private PhysicsParameter NewSoftVerticesMassParameter(bool left) =>
            new PhysicsParameter(new JSONStorableFloat("Base Value", 0, 0.05f, 0.5f), currentValueJsf: null)
            {
                config = new StaticPhysicsConfig(0.050f, 0.130f, 0.085f),
                quicknessOffsetConfig = new StaticPhysicsConfig(0.000f, -0.048f, -0.028f),
                slownessOffsetConfig = new StaticPhysicsConfig(0.012f, 0.060f, 0.040f),
                sync = left
                    ? (Action<float>) SyncMassLeft
                    : (Action<float>) SyncMassRight,
            };

        private PhysicsParameter NewColliderRadiusParameter(bool left) =>
            new PhysicsParameter(new JSONStorableFloat("Base Value", 0, 0, 0.07f), currentValueJsf: null)
            {
                config = new StaticPhysicsConfig(0.024f, 0.037f, 0.028f),
                sync = left
                    ? (Action<float>) SyncColliderRadiusLeft
                    : (Action<float>) SyncColliderRadiusRight,
            };

        private PhysicsParameter NewColliderAdditionalNormalOffsetParameter(bool left) =>
            new PhysicsParameter(new JSONStorableFloat("Base Value", 0, -0.01f, 0.01f), currentValueJsf: null)
            {
                config = new StaticPhysicsConfig(0.001f, 0.001f, 0.001f),
                sync = left
                    ? (Action<float>) SyncAdditionalNormalOffsetLeft
                    : (Action<float>) SyncAdditionalNormalOffsetRight,
            };

        private PhysicsParameter NewDistanceLimitParameter(bool left) =>
            new PhysicsParameter(new JSONStorableFloat("Base Value", 0, 0, 0.1f), currentValueJsf: null)
            {
                config = new StaticPhysicsConfig(0.020f, 0.068f, 0.028f),
                quicknessOffsetConfig = new StaticPhysicsConfig(0.000f, 0.000f, 0.024f),
                slownessOffsetConfig = new StaticPhysicsConfig(0.000f, 0.000f, -0.008f),
                sync = left
                    ? (Action<float>) SyncDistanceLimitLeft
                    : (Action<float>) SyncDistanceLimitRight,
            };

        private PhysicsParameter NewBackForceParameter(bool left) =>
            new PhysicsParameter(new JSONStorableFloat("Base Value", 0, 0, 50), currentValueJsf: null)
            {
                config = new StaticPhysicsConfig(50f, 55.6f, 9.3f),
                quicknessOffsetConfig = new StaticPhysicsConfig(-2.6f, -4f, -2.33f),
                slownessOffsetConfig = new StaticPhysicsConfig(0.8f, 1.33f, 0.77f),
                sync = left
                    ? (Action<float>) SyncBackForceLeft
                    : (Action<float>) SyncBackForceRight,
            };

        private PhysicsParameter NewBackForceMaxForceParameter(bool left) =>
            new PhysicsParameter(new JSONStorableFloat("Base Value", 0, 0, 50), currentValueJsf: null)
            {
                config = new StaticPhysicsConfig(50f, 50f, 50f),
                sync = left
                    ? (Action<float>) SyncBackForceMaxForceLeft
                    : (Action<float>) SyncBackForceMaxForceRight,
            };

        private PhysicsParameter NewBackForceThresholdDistanceParameter(bool left) =>
            new PhysicsParameter(new JSONStorableFloat("Base Value", 0, 0, 0.030f), currentValueJsf: null)
            {
                config = new StaticPhysicsConfig(0f, 0f, 0f),
                sync = left
                    ? (Action<float>) SyncBackForceThresholdDistanceLeft
                    : (Action<float>) SyncBackForceThresholdDistanceRight,
            };

        private void SetupPhysicsParameterGroups()
        {
            var softVerticesSpring = new PhysicsParameterGroup(
                NewSpringParameter(true),
                NewSpringParameter(false),
                "Fat Spring",
                "F2"
            );
            softVerticesSpring.SetLinearCurvesAroundMidpoint(null, slope: 0.41f);
            softVerticesSpring.SetLinearCurvesAroundMidpoint(SoftColliderGroup.MAIN, slope: 0);
            softVerticesSpring.SetLinearCurvesAroundMidpoint(SoftColliderGroup.OUTER, slope: 0);

            var softVerticesDamper = new PhysicsParameterGroup(
                NewDamperParameter(true),
                NewDamperParameter(false),
                "Fat Damper",
                "F3"
            )
            {
                dependOnPhysicsRate = true,
            };
            softVerticesDamper.SetLinearCurvesAroundMidpoint(null, slope: 0.082f);

            var softVerticesMass = new PhysicsParameterGroup(
                NewSoftVerticesMassParameter(true),
                NewSoftVerticesMassParameter(false),
                "Fat Mass",
                "F3"
            );

            var softVerticesColliderRadius = new PhysicsParameterGroup(
                NewColliderRadiusParameter(true),
                NewColliderRadiusParameter(false),
                "Fat Collider Radius",
                "F3"
            )
            {
                useRealMass = true,
            };

            var softVerticesColliderAdditionalNormalOffset = new PhysicsParameterGroup(
                NewColliderAdditionalNormalOffsetParameter(true),
                NewColliderAdditionalNormalOffsetParameter(false),
                "Fat Collider Depth",
                "F3"
            );

            var softVerticesDistanceLimit = new PhysicsParameterGroup(
                NewDistanceLimitParameter(true),
                NewDistanceLimitParameter(false),
                "Fat Distance Limit",
                "F3"
            )
            {
                useRealMass = true,
            };

            var softVerticesBackForce = new PhysicsParameterGroup(
                NewBackForceParameter(true),
                NewBackForceParameter(false),
                "Fat Back Force",
                "F2"
            );
            softVerticesBackForce.SetLinearCurvesAroundMidpoint(null, slope: 0.027f);

            var softVerticesBackForceMaxForce = new PhysicsParameterGroup(
                NewBackForceMaxForceParameter(true),
                NewBackForceMaxForceParameter(false),
                "Fat Bk Force Max Force",
                "F2"
            );

            var softVerticesBackForceThresholdDistance = new PhysicsParameterGroup(
                NewBackForceThresholdDistanceParameter(true),
                NewBackForceThresholdDistanceParameter(false),
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

        private void SyncMassLeft(float value)
        {
            _mainLeft.jointMass = value;
            _outerLeft.jointMass = value;
            _areolaLeft.jointMass = value;
            _nippleLeft.jointMass = value;
        }

        private void SyncMassRight(float value)
        {
            _mainRight.jointMass = value;
            _outerRight.jointMass = value;
            _areolaRight.jointMass = value;
            _nippleRight.jointMass = value;
        }

        private void SyncBackForceLeft(float value)
        {
            _mainLeft.jointBackForce = value;
            _outerLeft.jointBackForce = value;
            _areolaLeft.jointBackForce = value;
            _nippleLeft.jointBackForce = value;
        }

        private void SyncBackForceRight(float value)
        {
            _mainRight.jointBackForce = value;
            _outerRight.jointBackForce = value;
            _areolaRight.jointBackForce = value;
            _nippleRight.jointBackForce = value;
        }

        private void SyncBackForceThresholdDistanceLeft(float value)
        {
            _mainLeft.jointBackForceThresholdDistance = value;
            _outerLeft.jointBackForceThresholdDistance = value;
            _areolaLeft.jointBackForceThresholdDistance = value;
            _nippleLeft.jointBackForceThresholdDistance = value;
        }

        private void SyncBackForceThresholdDistanceRight(float value)
        {
            _mainRight.jointBackForceThresholdDistance = value;
            _outerRight.jointBackForceThresholdDistance = value;
            _areolaRight.jointBackForceThresholdDistance = value;
            _nippleRight.jointBackForceThresholdDistance = value;
        }

        private void SyncBackForceMaxForceLeft(float value)
        {
            _mainLeft.jointBackForceMaxForce = value;
            _outerLeft.jointBackForceMaxForce = value;
            _areolaLeft.jointBackForceMaxForce = value;
            _nippleLeft.jointBackForceMaxForce = value;
        }

        private void SyncBackForceMaxForceRight(float value)
        {
            _mainRight.jointBackForceMaxForce = value;
            _outerRight.jointBackForceMaxForce = value;
            _areolaRight.jointBackForceMaxForce = value;
            _nippleRight.jointBackForceMaxForce = value;
        }

        private void SyncDistanceLimitLeft(float value)
        {
            _mainLeft.normalDistanceLimit = value;
            _outerLeft.normalDistanceLimit = value;
            _areolaLeft.normalDistanceLimit = value;
            _nippleLeft.normalDistanceLimit = value;
        }

        private void SyncDistanceLimitRight(float value)
        {
            _mainRight.normalDistanceLimit = value;
            _outerRight.normalDistanceLimit = value;
            _areolaRight.normalDistanceLimit = value;
            _nippleRight.normalDistanceLimit = value;
        }

        private void SyncColliderRadiusLeft(float value)
        {
            SyncColliderRadius(value, _mainLeft);
            SyncColliderRadius(value, _outerLeft);
            SyncColliderRadius(value, _areolaLeft);
            SyncColliderRadius(value, _nippleLeft);
        }

        private void SyncColliderRadiusRight(float value)
        {
            SyncColliderRadius(value, _mainRight);
            SyncColliderRadius(value, _outerRight);
            SyncColliderRadius(value, _areolaRight);
            SyncColliderRadius(value, _nippleRight);
        }

        private void SyncAdditionalNormalOffsetLeft(float value)
        {
            _mainLeft.colliderAdditionalNormalOffset = value;
            _outerLeft.colliderAdditionalNormalOffset = value;
            _areolaLeft.colliderAdditionalNormalOffset = value;
            _nippleLeft.colliderAdditionalNormalOffset = value;
        }

        private void SyncAdditionalNormalOffsetRight(float value)
        {
            _mainRight.colliderAdditionalNormalOffset = value;
            _outerRight.colliderAdditionalNormalOffset = value;
            _areolaRight.colliderAdditionalNormalOffset = value;
            _nippleRight.colliderAdditionalNormalOffset = value;
        }

        private static void SyncColliderRadius(float value, DAZPhysicsMeshSoftVerticesGroup group)
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

        // Reimplements DAZPhysicsMesh.cs methods SyncGroup[A|B|C|D]SpringMultiplier and SyncSoftVerticesCombinedSpring
        // Circumvents use of softVerticesCombinedSpring value as multiplier on the group specific value, using custom multiplier instead
        private static void SyncGroupSpringMultiplier(
            float multiplier,
            float baseSpring,
            DAZPhysicsMeshSoftVerticesGroup group
        )
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
        private static void SyncGroupDamperMultiplier(
            float multiplier,
            float baseDamper,
            DAZPhysicsMeshSoftVerticesGroup group
        )
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
