using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static TittyMagic.Globals;

namespace TittyMagic
{
    internal class StaticPhysicsHandler
    {
        private List<StaticPhysicsConfig> _mainPhysicsConfigs;
        private List<StaticPhysicsConfig> _softPhysicsConfigs;
        private List<StaticPhysicsConfig> _nipplePhysicsConfigs;

        public float realMassAmount { get; set; }
        public float massAmount { get; set; }

        public StaticPhysicsHandler(bool isFemale)
        {
            if(isFemale)
            {
                // hard colliders off
                GEOMETRY.useAuxBreastColliders = false;
                // Self colliders off
                BREAST_PHYSICS_MESH.allowSelfCollision = true;
                // Auto collider radius off
                BREAST_PHYSICS_MESH.softVerticesUseAutoColliderRadius = false;
                // Collider depth
                BREAST_PHYSICS_MESH.softVerticesColliderAdditionalNormalOffset = 0.001f;
            }
        }

        public void LoadSettings(bool softPhysicsEnabled)
        {
            if(softPhysicsEnabled)
            {
                LoadMainPhysicsSettings();
                LoadSoftPhysicsSettings();
                LoadNipplePhysicsSettings();
            }
            else
            {
                LoadAltMainPhysicsSettings();
            }
        }

        private void LoadMainPhysicsSettings()
        {
            var centerOfGravityPercent = new BreastStaticPhysicsConfig(0.350f, 0.480f, 0.560f);
            var spring = new BreastStaticPhysicsConfig(82f, 96f, 45f)
            {
                quicknessOffsetConfig = new StaticPhysicsConfigBase(20f, 24f, 18f),
                slownessOffsetConfig = new StaticPhysicsConfigBase(-13f, -16f, -12f),
            };
            spring.SetLinearCurvesAroundMidpoint(slope: 0.135f);

            var damper = new BreastStaticPhysicsConfig(2.4f, 2.8f, 0.9f)
            {
                dependOnPhysicsRate = true,
                quicknessOffsetConfig = new StaticPhysicsConfigBase(-0.6f, -0.75f, -0.4f),
                slownessOffsetConfig = new StaticPhysicsConfigBase(0.4f, 0.5f, 0.27f),
            };
            damper.SetLinearCurvesAroundMidpoint(slope: 0.2f);

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

            centerOfGravityPercent.updateFunction = value => BREAST_CONTROL.centerOfGravityPercent = value;
            spring.updateFunction = value => BREAST_CONTROL.spring = value;
            damper.updateFunction = value => BREAST_CONTROL.damper = value;
            positionSpringZ.updateFunction = value => BREAST_CONTROL.positionSpringZ = value;
            positionDamperZ.updateFunction = value => BREAST_CONTROL.positionDamperZ = value;

            _mainPhysicsConfigs = new List<StaticPhysicsConfig>
            {
                centerOfGravityPercent,
                spring,
                damper,
                positionSpringZ,
                positionDamperZ,
            };
        }

        private void LoadAltMainPhysicsSettings()
        {
            var centerOfGravityPercent = new BreastStaticPhysicsConfig(0.525f, 0.750f, 0.900f);
            var spring = new BreastStaticPhysicsConfig(82f, 96f, 45f)
            {
                quicknessOffsetConfig = new StaticPhysicsConfigBase(20f, 24f, 18f),
                slownessOffsetConfig = new StaticPhysicsConfigBase(-13f, -16f, -12f),
            };
            spring.SetLinearCurvesAroundMidpoint(slope: 0.135f);
            var damper = new BreastStaticPhysicsConfig(1.8f, 2.1f, 0.675f)
            {
                dependOnPhysicsRate = true,
                quicknessOffsetConfig = new StaticPhysicsConfigBase(-0.45f, -0.56f, -0.3f),
                slownessOffsetConfig = new StaticPhysicsConfigBase(0.3f, 0.38f, 0.2f),
            };
            damper.SetLinearCurvesAroundMidpoint(slope: 0.2f);
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

            centerOfGravityPercent.updateFunction = value => BREAST_CONTROL.centerOfGravityPercent = value;
            spring.updateFunction = value => BREAST_CONTROL.spring = value;
            damper.updateFunction = value => BREAST_CONTROL.damper = value;
            positionSpringZ.updateFunction = value => BREAST_CONTROL.positionSpringZ = value;
            positionDamperZ.updateFunction = value => BREAST_CONTROL.positionDamperZ = value;

            _mainPhysicsConfigs = new List<StaticPhysicsConfig>
            {
                centerOfGravityPercent,
                spring,
                damper,
                positionSpringZ,
                positionDamperZ,
            };
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
                BREAST_PHYSICS_MESH.softVerticesCombinedSpring = value;
            };
            softVerticesCombinedDamper.updateFunction = value =>
            {
                BREAST_PHYSICS_MESH.softVerticesCombinedDamper = value;
            };
            softVerticesMass.updateFunction = value =>
            {
                BREAST_PHYSICS_MESH.softVerticesMass = value;
            };
            softVerticesColliderRadius.updateFunction = value =>
            {
                BREAST_PHYSICS_MESH.softVerticesColliderRadius = value;
            };
            softVerticesNormalLimit.updateFunction = value =>
            {
                BREAST_PHYSICS_MESH.softVerticesNormalLimit = value;
            };
            softVerticesBackForce.updateFunction = value =>
            {
                BREAST_PHYSICS_MESH.softVerticesBackForce = value;
            };
            softVerticesBackForceThresholdDistance.updateFunction = value =>
            {
                BREAST_PHYSICS_MESH.softVerticesBackForceThresholdDistance = value;
            };
            softVerticesBackForceMaxForce.updateFunction = value =>
            {
                BREAST_PHYSICS_MESH.softVerticesBackForceMaxForce = value;
            };
            groupASpringMultiplier.updateFunction = value =>
            {
                SyncGroupSpringMultiplier(BREAST_PHYSICS_MESH.groupASlots, value);
            };
            groupADamperMultiplier.updateFunction = value =>
            {
                SyncGroupDamperMultiplier(BREAST_PHYSICS_MESH.groupASlots, value);
            };
            groupBSpringMultiplier.updateFunction = value =>
            {
                SyncGroupSpringMultiplier(BREAST_PHYSICS_MESH.groupBSlots, value);
            };
            groupBDamperMultiplier.updateFunction = value =>
            {
                SyncGroupDamperMultiplier(BREAST_PHYSICS_MESH.groupBSlots, value);
            };
            groupCSpringMultiplier.updateFunction = value =>
            {
                SyncGroupSpringMultiplier(BREAST_PHYSICS_MESH.groupCSlots, value);
            };
            groupCDamperMultiplier.updateFunction = value =>
            {
                SyncGroupDamperMultiplier(BREAST_PHYSICS_MESH.groupCSlots, value);
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
                SyncGroupSpringMultiplier(BREAST_PHYSICS_MESH.groupDSlots, value);
            };
            groupDDamperMultiplier.updateFunction = value =>
            {
                SyncGroupDamperMultiplier(BREAST_PHYSICS_MESH.groupDSlots, value);
            };

            _nipplePhysicsConfigs = new List<StaticPhysicsConfig>
            {
                groupDSpringMultiplier,
                groupDDamperMultiplier,
            };
        }

        // combines protected SyncGroupASpringMultiplier, B etc. with SyncSoftVerticesCombinedSpring from DAZPhysicsMesh.cs
        private static void SyncGroupSpringMultiplier(IEnumerable<int> slots, float f)
        {
            foreach(int slot in slots)
            {
                if(slot >= BREAST_PHYSICS_MESH.softVerticesGroups.Count)
                {
                    continue;
                }
                DAZPhysicsMeshSoftVerticesGroup group = BREAST_PHYSICS_MESH.softVerticesGroups[slot];
                if(group == null)
                {
                    continue;
                }
                group.parentSettingSpringMultiplier = f;
                if(group.useParentSettings)
                {
                    float num = BREAST_PHYSICS_MESH.softVerticesCombinedSpring * f;
                    group.jointSpringNormal = num;
                    group.jointSpringTangent = num;
                    group.jointSpringTangent2 = num;
                    if(group.tieLinkJointSpringAndDamperToNormalSpringAndDamper)
                    {
                        group.linkSpring = num;
                    }
                }
            }
        }

        // combines protected SyncGroupADamperMultiplier, B etc. with SyncSoftVerticesCombinedDamper from DAZPhysicsMesh.cs
        private static void SyncGroupDamperMultiplier(IEnumerable<int> slots, float f)
        {
            foreach(int slot in slots)
            {
                if(slot >= BREAST_PHYSICS_MESH.softVerticesGroups.Count)
                {
                    continue;
                }
                DAZPhysicsMeshSoftVerticesGroup group = BREAST_PHYSICS_MESH.softVerticesGroups[slot];
                if(group == null)
                {
                    continue;
                }
                group.parentSettingDamperMultiplier = f;
                if(group.useParentSettings)
                {
                    float num = BREAST_PHYSICS_MESH.softVerticesCombinedDamper * f;
                    group.jointDamperNormal = num;
                    group.jointDamperTangent = num;
                    group.jointDamperTangent2 = num;
                    if(group.tieLinkJointSpringAndDamperToNormalSpringAndDamper)
                    {
                        group.linkDamper = num;
                    }
                }
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
            float mass = config.useRealMass ? realMassAmount : massAmount;
            float result = config.Calculate(mass, softness, quickness);
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
    }
}
