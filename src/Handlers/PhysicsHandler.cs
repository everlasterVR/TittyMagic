using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static TittyMagic.Globals;

namespace TittyMagic
{
    internal class PhysicsHandler
    {
        private readonly bool _isFemale;
        private readonly ConfigurableJoint _jointLeft;
        private readonly ConfigurableJoint _jointRight;
        private readonly Rigidbody _pectoralRbLeft;
        private readonly Rigidbody _pectoralRbRight;

        private readonly List<string> _adjustJointsFloatParamNames;
        private readonly List<string> _breastPhysicsMeshFloatParamNames;
        private Dictionary<string, float> _originalAdjustJointsFloatValues;
        private Dictionary<string, float> _originalBreastPhysicsMeshFloatValues;
        private bool _originalPectoralRbLeftDetectCollisions;
        private bool _originalPectoralRbRightDetectCollisions;
        private bool _originalAutoFatColliderRadius;
        private bool _originalHardColliders;
        private bool _originalSelfCollision;
        private Dictionary<string, bool> _originalGroupsUseParentSettings;

        // AdjustJoints.joint1.slerpDrive.maximumForce value logged on plugin Init
        private const float DEFAULT_SLERP_MAX_FORCE = 500;

        private List<StaticPhysicsConfig> _mainPhysicsConfigs;
        private List<StaticPhysicsConfig> _softPhysicsConfigs;
        private List<StaticPhysicsConfig> _nipplePhysicsConfigs;

        public float realMassAmount { get; private set; }
        public float massAmount { get; private set; }

        public JSONStorableFloat mass { get; set; }
        private float _centerOfGravityLeft;
        private float _centerOfGravityRight;
        private float _springLeft;
        private float _springRight;
        private float _damperLeft;
        private float _damperRight;
        private float _positionSpringLeft;
        private float _positionSpringRight;
        private float _positionDamperLeft;
        private float _positionDamperRight;
        private float _targetRotationYLeft;
        private float _targetRotationYRight;
        private float _targetRotationXLeft;
        private float _targetRotationXRight;

        private float _combinedSpringNew;
        private float _combinedDamperNew;

        public PhysicsHandler(bool isFemale, Rigidbody pectoralRbLeft, Rigidbody pectoralRbRight)
        {
            _isFemale = isFemale;
            _jointLeft = BREAST_CONTROL.joint2;
            _jointRight = BREAST_CONTROL.joint1;
            _pectoralRbLeft = pectoralRbLeft;
            _pectoralRbRight = pectoralRbRight;

            _adjustJointsFloatParamNames = new List<string>
            {
                "mass",
                "centerOfGravityPercent",
                "spring",
                "damper",
                "positionSpringZ",
                "positionDamperZ",
                "targetRotationX",
                "targetRotationY",
            };
            if(_isFemale)
            {
                _breastPhysicsMeshFloatParamNames = BREAST_PHYSICS_MESH.GetFloatParamNames();
            }
            SaveOriginalPhysicsAndSetPluginDefaults();
        }

        public void UpdateMassValueAndAmounts(bool useNewMass, float volume)
        {
            float newMass = Mathf.Clamp(
                Mathf.Pow(0.78f * volume, 1.5f),
                mass.min,
                mass.max
            );
            realMassAmount = newMass / 2;
            if(useNewMass)
            {
                massAmount = realMassAmount;
                mass.val = newMass;
            }
            else
            {
                massAmount = mass.val / 2;
            }

            SyncMass(_pectoralRbLeft, mass.val);
            SyncMass(_pectoralRbRight, mass.val);
        }

        public void AddToLeftCenterOfGravity(float value)
        {
            SyncCenterOfGravity(_pectoralRbLeft, _centerOfGravityLeft + value);
        }

        public void AddToRightCenterOfGravity(float value)
        {
            SyncCenterOfGravity(_pectoralRbRight, _centerOfGravityRight + value);
        }

        public void AddToLeftJointSpring(float value)
        {
            SyncJoint(_jointLeft, _pectoralRbLeft, _springLeft + value, _damperLeft);
        }

        public void AddToRightJointSpring(float value)
        {
            SyncJoint(_jointRight, _pectoralRbRight, _springRight + value, _damperLeft);
        }

        public void AddToLeftJointDamper(float value)
        {
            SyncJoint(_jointLeft, _pectoralRbLeft, _springLeft, _damperLeft + value);
        }

        public void AddToRightJointDamper(float value)
        {
            SyncJoint(_jointRight, _pectoralRbRight, _springRight, _damperLeft + value);
        }

        public void AddToLeftJointPositionSpringZ(float value)
        {
            SyncJointPositionZDrive(_jointLeft, _pectoralRbLeft, _positionSpringLeft + value, _positionDamperLeft);
        }

        public void AddToRightJointPositionSpringZ(float value)
        {
            SyncJointPositionZDrive(_jointRight, _pectoralRbRight, _positionSpringRight + value, _positionDamperRight);
        }

        public void AddToLeftJointPositionDamperZ(float value)
        {
            SyncJointPositionZDrive(_jointLeft, _pectoralRbLeft, _positionSpringLeft, _positionDamperLeft + value);
        }

        public void AddToRightJointPositionDamperZ(float value)
        {
            SyncJointPositionZDrive(_jointRight, _pectoralRbRight, _positionSpringRight, _positionDamperRight + value);
        }

        public void SetTargetRotationYLeft(float value)
        {
            _targetRotationYLeft = value;
            SyncTargetRotationLeft(_targetRotationXLeft, _targetRotationYLeft);
        }

        public void SetTargetRotationYRight(float value)
        {
            _targetRotationYRight = value;
            SyncTargetRotationRight(_targetRotationXRight, _targetRotationYRight);
        }

        public void SetTargetRotationXLeft(float value)
        {
            _targetRotationXLeft = value;
            SyncTargetRotationLeft(_targetRotationXLeft, _targetRotationYLeft);
        }

        public void SetTargetRotationXRight(float value)
        {
            _targetRotationXRight = value;
            SyncTargetRotationRight(_targetRotationXRight, _targetRotationYRight);
        }

        public void LoadSettings(bool softPhysicsEnabled)
        {
            LoadMainPhysicsSettings(softPhysicsEnabled);

            if(softPhysicsEnabled)
            {
                LoadSoftPhysicsSettings();
                LoadNipplePhysicsSettings();
            }
        }

        private void LoadMainPhysicsSettings(bool softPhysicsEnabled)
        {
            BreastStaticPhysicsConfig centerOfGravityPercent;
            BreastStaticPhysicsConfig damper;
            if(softPhysicsEnabled)
            {
                centerOfGravityPercent = new BreastStaticPhysicsConfig(0.350f, 0.480f, 0.560f);

                damper = new BreastStaticPhysicsConfig(2.4f, 2.8f, 0.9f)
                {
                    dependOnPhysicsRate = true,
                    quicknessOffsetConfig = new StaticPhysicsConfigBase(-0.6f, -0.75f, -0.4f),
                    slownessOffsetConfig = new StaticPhysicsConfigBase(0.4f, 0.5f, 0.27f),
                };
                damper.SetLinearCurvesAroundMidpoint(slope: 0.2f);
            }
            else
            {
                centerOfGravityPercent = new BreastStaticPhysicsConfig(0.525f, 0.750f, 0.900f);

                damper = new BreastStaticPhysicsConfig(1.8f, 2.1f, 0.675f)
                {
                    dependOnPhysicsRate = true,
                    quicknessOffsetConfig = new StaticPhysicsConfigBase(-0.45f, -0.56f, -0.3f),
                    slownessOffsetConfig = new StaticPhysicsConfigBase(0.3f, 0.38f, 0.2f),
                };
                damper.SetLinearCurvesAroundMidpoint(slope: 0.2f);
            }

            var spring = new BreastStaticPhysicsConfig(82f, 96f, 45f)
            {
                quicknessOffsetConfig = new StaticPhysicsConfigBase(20f, 24f, 18f),
                slownessOffsetConfig = new StaticPhysicsConfigBase(-13f, -16f, -12f),
            };
            spring.SetLinearCurvesAroundMidpoint(slope: 0.135f);

            var positionSpringZ = new BreastStaticPhysicsConfig(850f, 950f, 250f)
            {
                quicknessOffsetConfig = new StaticPhysicsConfigBase(90, 110, 50f),
                slownessOffsetConfig = new StaticPhysicsConfigBase(-60, -70, -33f),
            };
            positionSpringZ.SetLinearCurvesAroundMidpoint(slope: 0.33f);

            var positionDamperZ = new BreastStaticPhysicsConfig(16f, 22f, 9f)
            {
                dependOnPhysicsRate = true,
            };

            centerOfGravityPercent.updateFunction = value =>
            {
                _centerOfGravityLeft = value;
                _centerOfGravityRight = value;
                SyncCenterOfGravity(_pectoralRbLeft, _centerOfGravityLeft);
                SyncCenterOfGravity(_pectoralRbRight, _centerOfGravityRight);
            };
            spring.updateFunction = value =>
            {
                _springLeft = value;
                _springRight = value;
                SyncJoint(_jointLeft, _pectoralRbLeft, _springLeft, _damperLeft);
                SyncJoint(_jointRight, _pectoralRbRight, _springRight, _damperRight);
            };
            damper.updateFunction = value =>
            {
                _damperLeft = value;
                _damperRight = value;
                SyncJoint(_jointLeft, _pectoralRbLeft, _springLeft, _damperLeft);
                SyncJoint(_jointRight, _pectoralRbRight, _springRight, _damperRight);
            };
            positionSpringZ.updateFunction = value =>
            {
                _positionSpringLeft = value;
                _positionSpringRight = value;
                SyncJointPositionZDrive(_jointLeft, _pectoralRbLeft, _positionSpringLeft, _positionDamperLeft);
                SyncJointPositionZDrive(_jointRight, _pectoralRbRight, _positionSpringRight, _positionDamperRight);
            };
            positionDamperZ.updateFunction = value =>
            {
                _positionDamperLeft = value;
                _positionDamperRight = value;
                SyncJointPositionZDrive(_jointLeft, _pectoralRbLeft, _positionSpringLeft, _positionDamperLeft);
                SyncJointPositionZDrive(_jointRight, _pectoralRbRight, _positionSpringRight, _positionDamperRight);
            };

            _mainPhysicsConfigs = new List<StaticPhysicsConfig>
            {
                centerOfGravityPercent,
                spring,
                damper,
                positionSpringZ,
                positionDamperZ,
            };
        }

        // Reimplements AdjustJoints.cs method SyncMass
        private static void SyncMass(Rigidbody rb, float value)
        {
            if(Math.Abs(rb.mass - value) > 0.001f)
            {
                rb.mass = value;
                rb.WakeUp();
            }
        }

        // Reimplements AdjustJoints.cs method SyncCenterOfGravity
        private static void SyncCenterOfGravity(Rigidbody rb, float value)
        {
            var newCenterOfMass = Vector3.Lerp(BREAST_CONTROL.lowCenterOfGravity, BREAST_CONTROL.highCenterOfGravity, value);
            if(rb.centerOfMass != newCenterOfMass)
            {
                rb.centerOfMass = newCenterOfMass;
                rb.WakeUp();
            }
        }

        // Reimplements AdjustJoints.cs method SyncJoint
        private static void SyncJoint(ConfigurableJoint joint, Rigidbody rb, float spring, float damper)
        {
            // see AdjustJoints.cs method ScaleChanged
            float scalePow = Mathf.Pow(1.7f, BREAST_CONTROL.scale - 1f);

            float scaledSpring = spring * scalePow;
            float scaledDamper = damper * scalePow;

            var slerpDrive = joint.slerpDrive;
            slerpDrive.positionSpring = scaledSpring;
            slerpDrive.positionDamper = scaledDamper;

            slerpDrive.maximumForce = DEFAULT_SLERP_MAX_FORCE * scalePow;
            joint.slerpDrive = slerpDrive;

            var angularXDrive = joint.angularXDrive;
            angularXDrive.positionSpring = scaledSpring;
            angularXDrive.positionDamper = scaledDamper;
            joint.angularXDrive = angularXDrive;

            var angularYZDrive = joint.angularYZDrive;
            angularYZDrive.positionSpring = scaledSpring;
            angularYZDrive.positionDamper = scaledDamper;
            joint.angularYZDrive = angularYZDrive;

            var angularXLimitSpring = joint.angularXLimitSpring;
            angularXLimitSpring.spring = scaledSpring * BREAST_CONTROL.limitSpringMultiplier;
            angularXLimitSpring.damper = scaledDamper * BREAST_CONTROL.limitDamperMultiplier;
            joint.angularXLimitSpring = angularXLimitSpring;

            var angularYZLimitSpring = joint.angularYZLimitSpring;
            angularYZLimitSpring.spring = scaledSpring * BREAST_CONTROL.limitSpringMultiplier;
            angularYZLimitSpring.damper = scaledDamper * BREAST_CONTROL.limitDamperMultiplier;
            joint.angularYZLimitSpring = angularYZLimitSpring;

            rb.WakeUp();
        }

        // Reimplements AdjustJoints.cs method SyncJointPositionZDrive
        private static void SyncJointPositionZDrive(ConfigurableJoint joint, Rigidbody rb, float spring, float damper)
        {
            var zDrive = joint.zDrive;
            zDrive.positionSpring = spring;
            zDrive.positionDamper = damper;
            joint.zDrive = zDrive;
            rb.WakeUp();
        }


        // Reimplements AdjustJoints.cs methods SyncTargetRotation and SetTargetRotation
        private void SyncTargetRotationLeft(float targetRotationX, float targetRotationY)
        {
            BREAST_CONTROL.smoothedJoint2TargetRotation.x = targetRotationX;
            BREAST_CONTROL.smoothedJoint2TargetRotation.y = targetRotationY;

            var dazBone = _jointLeft.GetComponent<DAZBone>();
            Vector3 rotation = BREAST_CONTROL.smoothedJoint2TargetRotation;
            rotation.x = -rotation.x;
            dazBone.baseJointRotation = rotation;
        }

        // Reimplements AdjustJoints.cs methods SyncTargetRotation and SetTargetRotation
        // Circumvents default invertJoint2RotationX = true
        private void SyncTargetRotationRight(float targetRotationX, float targetRotationY)
        {
            BREAST_CONTROL.smoothedJoint1TargetRotation.x = targetRotationX;
            BREAST_CONTROL.smoothedJoint1TargetRotation.y = targetRotationY;

            var dazBone = _jointRight.GetComponent<DAZBone>();
            Vector3 rotation = BREAST_CONTROL.smoothedJoint1TargetRotation;
            rotation.x = -rotation.x;
            dazBone.baseJointRotation = rotation;
        }

        private void LoadSoftPhysicsSettings()
        {
            var softVerticesCombinedSpring = new BreastStaticPhysicsConfig(500f, 500f, 62f);
            softVerticesCombinedSpring.SetLinearCurvesAroundMidpoint(slope: 0.41f);

            var softVerticesCombinedDamper = new BreastStaticPhysicsConfig(10.0f, 10.0f, 0.90f)
            {
                dependOnPhysicsRate = true,
                quicknessOffsetConfig = new StaticPhysicsConfigBase(-0.75f, -0.90f, -0.45f),
                slownessOffsetConfig = new StaticPhysicsConfigBase(1.125f, 1.35f, 0.675f),
            };
            softVerticesCombinedDamper.SetLinearCurvesAroundMidpoint(slope: 0.082f);

            var softVerticesMass = new BreastStaticPhysicsConfig(0.050f, 0.130f, 0.085f)
            {
                quicknessOffsetConfig = new StaticPhysicsConfigBase(0.000f, -0.048f, -0.028f),
                slownessOffsetConfig = new StaticPhysicsConfigBase(0.012f, 0.060f, 0.040f),
            };
            var softVerticesColliderRadius = new BreastStaticPhysicsConfig(0.024f, 0.037f, 0.028f)
            {
                useRealMass = true,
            };
            var softVerticesNormalLimit = new BreastStaticPhysicsConfig(0.020f, 0.068f, 0.028f)
            {
                useRealMass = true,
                quicknessOffsetConfig = new StaticPhysicsConfigBase(0.000f, 0.000f, 0.024f),
                slownessOffsetConfig = new StaticPhysicsConfigBase(0.000f, 0.000f, -0.008f),
            };
            var softVerticesBackForce = new BreastStaticPhysicsConfig(50f, 55.6f, 9.3f)
            {
                quicknessOffsetConfig = new StaticPhysicsConfigBase(-2.6f, -4f, -2.33f),
                slownessOffsetConfig = new StaticPhysicsConfigBase(0.8f, 1.33f, 0.77f),
            };
            softVerticesBackForce.SetLinearCurvesAroundMidpoint(slope: 0.027f);

            var softVerticesBackForceThresholdDistance = new BreastStaticPhysicsConfig(0f, 0f, 0f);
            var softVerticesBackForceMaxForce = new BreastStaticPhysicsConfig(50f, 50f, 50f);
            var groupASpringMultiplier = new BreastStaticPhysicsConfig(5f, 5f, 1f);
            groupASpringMultiplier.SetLinearCurvesAroundMidpoint(slope: 0);
            var groupADamperMultiplier = new BreastStaticPhysicsConfig(1f, 1f, 1f);
            var groupBSpringMultiplier = new BreastStaticPhysicsConfig(5f, 5f, 1f);
            groupBSpringMultiplier.SetLinearCurvesAroundMidpoint(slope: 0);
            var groupBDamperMultiplier = new BreastStaticPhysicsConfig(1f, 1f, 1f);
            var groupCSpringMultiplier = new BreastStaticPhysicsConfig(2.29f, 1.30f, 2.29f);
            var groupCDamperMultiplier = new BreastStaticPhysicsConfig(1.81f, 1.22f, 1.81f);

            softVerticesCombinedSpring.updateFunction = value =>
            {
                _combinedSpringNew = value;
            };
            softVerticesCombinedDamper.updateFunction = value =>
            {
                _combinedDamperNew = value;
            };
            softVerticesMass.updateFunction = value =>
            {
                BREAST_PHYSICS_MESH.softVerticesGroups
                    .ForEach(group => group.jointMass = value);
            };
            softVerticesColliderRadius.updateFunction = value =>
            {
                BREAST_PHYSICS_MESH.softVerticesGroups.ForEach(group =>
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
                        group.SyncColliders();
                });
            };
            softVerticesNormalLimit.updateFunction = value =>
            {
                BREAST_PHYSICS_MESH.softVerticesGroups
                    .ForEach(group => group.normalDistanceLimit = value);
            };
            softVerticesBackForce.updateFunction = value =>
            {
                BREAST_PHYSICS_MESH.softVerticesGroups
                    .ForEach(group => group.jointBackForce = value);
            };
            softVerticesBackForceThresholdDistance.updateFunction = value =>
            {
                BREAST_PHYSICS_MESH.softVerticesGroups
                    .ForEach(group => group.jointBackForceThresholdDistance = value);
            };
            softVerticesBackForceMaxForce.updateFunction = value =>
            {
                BREAST_PHYSICS_MESH.softVerticesGroups
                    .ForEach(group => group.jointBackForceMaxForce = value);
            };
            groupASpringMultiplier.updateFunction = value =>
            {
                foreach(int slot in BREAST_PHYSICS_MESH.groupASlots)
                {
                    SyncGroupSpringMultiplier(slot, value);
                }
            };
            groupADamperMultiplier.updateFunction = value =>
            {
                foreach(int slot in BREAST_PHYSICS_MESH.groupASlots)
                {
                    SyncGroupDamperMultiplier(slot, value);
                }
            };
            groupBSpringMultiplier.updateFunction = value =>
            {
                foreach(int slot in BREAST_PHYSICS_MESH.groupBSlots)
                {
                    SyncGroupSpringMultiplier(slot, value);
                }
            };
            groupBDamperMultiplier.updateFunction = value =>
            {
                foreach(int slot in BREAST_PHYSICS_MESH.groupBSlots)
                {
                    SyncGroupDamperMultiplier(slot, value);
                }
            };
            groupCSpringMultiplier.updateFunction = value =>
            {
                foreach(int slot in BREAST_PHYSICS_MESH.groupCSlots)
                {
                    SyncGroupSpringMultiplier(slot, value);
                }
            };
            groupCDamperMultiplier.updateFunction = value =>
            {
                foreach(int slot in BREAST_PHYSICS_MESH.groupCSlots)
                {
                    SyncGroupDamperMultiplier(slot, value);
                }
            };

            _softPhysicsConfigs = new List<StaticPhysicsConfig>
            {
                softVerticesCombinedSpring,
                softVerticesCombinedDamper,
                softVerticesMass,
                softVerticesColliderRadius,
                softVerticesNormalLimit,
                softVerticesBackForce,
                softVerticesBackForceThresholdDistance,
                softVerticesBackForceMaxForce,
                groupASpringMultiplier,
                groupADamperMultiplier,
                groupBSpringMultiplier,
                groupBDamperMultiplier,
                groupCSpringMultiplier,
                groupCDamperMultiplier,
            };
        }

        private void LoadNipplePhysicsSettings()
        {
            var groupDSpringMultiplier = new BreastStaticPhysicsConfig(2.29f, 1.30f, 2.29f);
            var groupDDamperMultiplier = new BreastStaticPhysicsConfig(1.81f, 1.22f, 1.81f);

            groupDSpringMultiplier.updateFunction = value =>
            {
                foreach(int slot in BREAST_PHYSICS_MESH.groupDSlots)
                {
                    SyncGroupSpringMultiplier(slot, value);
                }
            };
            groupDDamperMultiplier.updateFunction = value =>
            {
                foreach(int slot in BREAST_PHYSICS_MESH.groupDSlots)
                {
                    SyncGroupDamperMultiplier(slot, value);
                }
            };

            _nipplePhysicsConfigs = new List<StaticPhysicsConfig>
            {
                groupDSpringMultiplier,
                groupDDamperMultiplier,
            };
        }

        // Reimplements DAZPhysicsMesh.cs methods SyncGroup[A|B|C|D]SpringMultiplier and SyncSoftVerticesCombinedSpring
        // Circumvents use of softVerticesCombinedSpring value as multiplier on the group specific value, using custom multiplier instead
        private void SyncGroupSpringMultiplier(int slot, float value)
        {
            var group = BREAST_PHYSICS_MESH.softVerticesGroups[slot];
            float combinedValue = _combinedSpringNew * value;
            group.jointSpringNormal = combinedValue;
            group.jointSpringTangent = combinedValue;
            group.jointSpringTangent2 = combinedValue;
            if(group.tieLinkJointSpringAndDamperToNormalSpringAndDamper)
            {
                group.linkSpring = combinedValue;
            }
        }

        // Reimplements DAZPhysicsMesh.cs methods SyncGroup[A|B|C|D]DamperMultiplier and SyncSoftVerticesCombinedDamper
        // Circumvents use of softVerticesCombinedDamper value as multiplier on the group specific value, using custom multiplier instead
        private void SyncGroupDamperMultiplier(int slot, float value)
        {
            var group = BREAST_PHYSICS_MESH.softVerticesGroups[slot];
            float combinedValue = _combinedDamperNew * value;
            group.jointDamperNormal = combinedValue;
            group.jointDamperTangent = combinedValue;
            group.jointDamperTangent2 = combinedValue;
            if(group.tieLinkJointSpringAndDamperToNormalSpringAndDamper)
            {
                group.linkDamper = combinedValue;
            }
        }

        public void UpdateMainPhysics(float softnessAmount, float quicknessAmount)
        {
            _mainPhysicsConfigs.ForEach(
                config => config.updateFunction(NewValue(config, softnessAmount, quicknessAmount))
            );
        }

        public void UpdateSoftPhysics(float softnessAmount, float quicknessAmount)
        {
            _softPhysicsConfigs.ForEach(
                config => config.updateFunction(NewValue(config, softnessAmount, quicknessAmount))
            );
        }

        public void UpdateNipplePhysics(float softnessAmount, float nippleErectionVal)
        {
            _nipplePhysicsConfigs.ForEach(
                config => config.updateFunction(NewNippleValue(config, softnessAmount, 1.25f * nippleErectionVal))
            );
        }

        public void UpdateRateDependentPhysics(float softnessAmount, float quicknessAmount)
        {
            _mainPhysicsConfigs
                .Concat(_softPhysicsConfigs)
                .Where(config => config.dependOnPhysicsRate)
                .ToList()
                .ForEach(
                    config => config.updateFunction(NewValue(config, softnessAmount, quicknessAmount))
                );
        }

        // input mass, softness and quickness normalized to (0,1) range
        private float NewValue(StaticPhysicsConfig config, float softness, float quickness)
        {
            float result = config.Calculate(
                config.useRealMass ? realMassAmount : massAmount,
                softness,
                quickness
            );
            return config.dependOnPhysicsRate ? PhysicsRateMultiplier() * result : result;
        }

        private static float NewNippleValue(StaticPhysicsConfigBase config, float mass, float softness, float addend = 0)
        {
            return config.Calculate(mass, softness) + addend;
        }

        // see UserPreferences.cs methods SetPhysics45, 60, 72 etc.
        private static float PhysicsRateMultiplier()
        {
            return 0.01666667f / Time.fixedDeltaTime;
        }

        public void SaveOriginalPhysicsAndSetPluginDefaults()
        {
            if(_isFemale)
            {
                // auto fat collider radius off (no effect)
                _originalAutoFatColliderRadius = BREAST_PHYSICS_MESH.softVerticesUseAutoColliderRadius;
                BREAST_PHYSICS_MESH.softVerticesUseAutoColliderRadius = false;
                // hard colliders off
                _originalHardColliders = GEOMETRY.useAuxBreastColliders;
                GEOMETRY.useAuxBreastColliders = false;
                // self colliders off
                _originalSelfCollision = BREAST_PHYSICS_MESH.allowSelfCollision;
                BREAST_PHYSICS_MESH.allowSelfCollision = true;
                // TODO configurable
                BREAST_PHYSICS_MESH.softVerticesColliderAdditionalNormalOffset = 0.001f;
                // prevent settings in F Breast Physics 2 from having effect
                _originalGroupsUseParentSettings = new Dictionary<string, bool>();
                foreach(var group in BREAST_PHYSICS_MESH.softVerticesGroups)
                {
                    _originalGroupsUseParentSettings[group.name] = group.useParentSettings;
                    group.useParentSettings = false;
                }

                _originalBreastPhysicsMeshFloatValues = new Dictionary<string, float>();
                foreach(string name in _breastPhysicsMeshFloatParamNames)
                {
                    var param = BREAST_PHYSICS_MESH.GetFloatJSONParam(name);
                    _originalBreastPhysicsMeshFloatValues[name] = param.val;
                    param.val = 0;
                }
            }

            // disable pectoral collisions, they cause breasts to "jump" when touched
            _originalPectoralRbLeftDetectCollisions = _pectoralRbLeft.detectCollisions;
            _originalPectoralRbRightDetectCollisions = _pectoralRbRight.detectCollisions;
            _pectoralRbLeft.detectCollisions = false;
            _pectoralRbRight.detectCollisions = false;

            _originalAdjustJointsFloatValues = new Dictionary<string, float>();
            foreach(string name in _adjustJointsFloatParamNames)
            {
                var param = BREAST_CONTROL.GetFloatJSONParam(name);
                _originalAdjustJointsFloatValues[name] = param.val;
                param.val = 0;
            }
        }

        private void RestoreOriginalPhysics()
        {
            if(_isFemale)
            {
                BREAST_PHYSICS_MESH.softVerticesUseAutoColliderRadius = _originalAutoFatColliderRadius;
                GEOMETRY.useAuxBreastColliders = _originalHardColliders;
                BREAST_PHYSICS_MESH.allowSelfCollision = _originalSelfCollision;
                foreach(var group in BREAST_PHYSICS_MESH.softVerticesGroups)
                {
                    group.useParentSettings = _originalGroupsUseParentSettings[group.name];
                }
            }

            _pectoralRbLeft.detectCollisions = _originalPectoralRbLeftDetectCollisions;
            _pectoralRbRight.detectCollisions = _originalPectoralRbRightDetectCollisions;

            foreach(string name in _adjustJointsFloatParamNames)
            {
                BREAST_CONTROL.GetFloatJSONParam(name).val = _originalAdjustJointsFloatValues[name];
            }
            foreach(string name in _breastPhysicsMeshFloatParamNames)
            {
                BREAST_PHYSICS_MESH.GetFloatJSONParam(name).val = _originalBreastPhysicsMeshFloatValues[name];
            }
        }

        public void Reset()
        {
            RestoreOriginalPhysics();
        }
    }
}
