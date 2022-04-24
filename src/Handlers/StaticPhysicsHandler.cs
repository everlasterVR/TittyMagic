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
        private List<PectoralStaticPhysicsConfig> _pectoralPhysicsConfigs;

        public float realMassAmount { get; set; }
        public float massAmount { get; set; }

        public StaticPhysicsHandler(bool isFemale)
        {
            if(isFemale)
            {
                SetBreastPhysicsDefaults();
            }
        }

        public void LoadSettings(bool isFemale)
        {
            if(isFemale)
            {
                LoadFemaleBreastSettings();
            }
            else
            {
                LoadPectoralSettings();
            }
        }

        private void LoadFemaleBreastSettings()
        {
            LoadMainPhysicsSettings();
            LoadSoftPhysicsSettings();
            LoadNipplePhysicsSettings();
        }

        private void LoadMainPhysicsSettings()
        {
            var centerOfGravityPercent = new BreastStaticPhysicsConfig("centerOfGravityPercent", 0.350f, 0.480f, 0.560f);
            var spring = new BreastStaticPhysicsConfig("spring", 50f, 64f, 45f)
            {
                quicknessOffsetConfig = new StaticPhysicsConfigBase(20f, 24f, 18f),
                slownessOffsetConfig = new StaticPhysicsConfigBase(-13f, -16f, -12f),
            };
            var damper = new BreastStaticPhysicsConfig("damper", 1.2f, 1.6f, 0.9f)
            {
                dependOnPhysicsRate = true,
                quicknessOffsetConfig = new StaticPhysicsConfigBase(-0.6f, -0.75f, -0.4f),
                slownessOffsetConfig = new StaticPhysicsConfigBase(0.4f, 0.5f, 0.27f),
            };
            var positionSpringZ = new BreastStaticPhysicsConfig("positionSpringZ", 450f, 550f, 250f)
            {
                quicknessOffsetConfig = new StaticPhysicsConfigBase(90, 110, 50f),
                slownessOffsetConfig = new StaticPhysicsConfigBase(-60, -70, -33f),
            };
            var positionDamperZ = new BreastStaticPhysicsConfig("positionDamperZ", 16f, 22f, 9f)
            {
                dependOnPhysicsRate = true,
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

        private void LoadSoftPhysicsSettings()
        {
            var softVerticesCombinedSpring = new BreastSoftStaticPhysicsConfig("softVerticesCombinedSpring", 240f, 240f, 62f);
            var softVerticesCombinedDamper = new BreastSoftStaticPhysicsConfig("softVerticesCombinedDamper", 1.50f, 1.80f, 0.90f)
            {
                dependOnPhysicsRate = true,
                quicknessOffsetConfig = new StaticPhysicsConfigBase(-0.75f, -0.90f, -0.45f),
                slownessOffsetConfig = new StaticPhysicsConfigBase(1.125f, 1.35f, 0.675f),
            };
            var softVerticesMass = new BreastSoftStaticPhysicsConfig("softVerticesMass", 0.050f, 0.130f, 0.085f)
            {
                quicknessOffsetConfig = new StaticPhysicsConfigBase(0.000f, -0.048f, -0.028f),
                slownessOffsetConfig = new StaticPhysicsConfigBase(0.012f, 0.060f, 0.040f),
            };
            var softVerticesColliderRadius = new BreastSoftStaticPhysicsConfig("softVerticesColliderRadius", 0.024f, 0.037f, 0.028f)
            {
                useRealMass = true,
            };
            var softVerticesDistanceLimit = new BreastSoftStaticPhysicsConfig("softVerticesDistanceLimit", 0.020f, 0.068f, 0.028f)
            {
                useRealMass = true,
                quicknessOffsetConfig = new StaticPhysicsConfigBase(0.000f, 0.000f, 0.024f),
                slownessOffsetConfig = new StaticPhysicsConfigBase(0.000f, 0.000f, -0.008f),
            };
            var softVerticesBackForce = new BreastSoftStaticPhysicsConfig("softVerticesBackForce", 10.4f, 16.0f, 9.3f)
            {
                quicknessOffsetConfig = new StaticPhysicsConfigBase(-2.6f, -4f, -2.33f),
                slownessOffsetConfig = new StaticPhysicsConfigBase(0.8f, 1.33f, 0.77f),
            };
            var softVerticesBackForceThresholdDistance = new BreastSoftStaticPhysicsConfig("softVerticesBackForceThresholdDistance", 0f, 0f, 0f);
            var softVerticesBackForceMaxForce = new BreastSoftStaticPhysicsConfig("softVerticesBackForceMaxForce", 50f, 50f, 50f);
            var groupASpringMultiplier = new BreastSoftStaticPhysicsConfig("groupASpringMultiplier", 1f, 1f, 1f);
            var groupADamperMultiplier = new BreastSoftStaticPhysicsConfig("groupADamperMultiplier", 1f, 1f, 1f);
            var groupBSpringMultiplier = new BreastSoftStaticPhysicsConfig("groupBSpringMultiplier", 1f, 1f, 1f);
            var groupBDamperMultiplier = new BreastSoftStaticPhysicsConfig("groupBDamperMultiplier", 1f, 1f, 1f);
            var groupCSpringMultiplier = new BreastSoftStaticPhysicsConfig("groupCSpringMultiplier", 2.29f, 1.30f, 2.29f);
            var groupCDamperMultiplier = new BreastSoftStaticPhysicsConfig("groupCDamperMultiplier", 1.81f, 1.22f, 1.81f);

            _softPhysicsConfigs = new List<StaticPhysicsConfig>
            {
                softVerticesCombinedSpring,
                softVerticesCombinedDamper,
                softVerticesMass,
                softVerticesColliderRadius,
                softVerticesDistanceLimit,
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
            var groupDSpringMultiplier = new BreastSoftStaticPhysicsConfig("groupDSpringMultiplier", 2.29f, 1.30f, 2.29f);
            var groupDDamperMultiplier = new BreastSoftStaticPhysicsConfig("groupDDamperMultiplier", 1.81f, 1.22f, 1.81f);

            _nipplePhysicsConfigs = new List<StaticPhysicsConfig>
            {
                groupDSpringMultiplier,
                groupDDamperMultiplier,
            };
        }

        private void LoadPectoralSettings()
        {
            var centerOfGravityPercent = new PectoralStaticPhysicsConfig("centerOfGravityPercent", 0.460f, 0.590f);
            var spring = new PectoralStaticPhysicsConfig("spring", 48f, 62f);
            var damper = new PectoralStaticPhysicsConfig("damper", 1.0f, 1.3f);
            var positionSpringZ = new PectoralStaticPhysicsConfig("positionSpringZ", 350f, 450f);
            var positionDamperZ = new PectoralStaticPhysicsConfig("positionDamperZ", 13f, 19f);

            _pectoralPhysicsConfigs = new List<PectoralStaticPhysicsConfig>
            {
                centerOfGravityPercent,
                spring,
                damper,
                positionSpringZ,
                positionDamperZ,
            };
        }

        private static void SetBreastPhysicsDefaults()
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

        public void UpdateMainPhysics(float softnessAmount, float quicknessAmount)
        {
            float multiplier = PhysicsRateMultiplier();
            _mainPhysicsConfigs.ForEach(
                config => config.UpdateVal(
                    config.useRealMass ? realMassAmount : massAmount,
                    softnessAmount,
                    quicknessAmount,
                    multiplier
                )
            );
        }

        public void UpdateSoftPhysics(float softnessAmount, float quicknessAmount)
        {
            float multiplier = PhysicsRateMultiplier();
            _softPhysicsConfigs.ForEach(
                config => config.UpdateVal(
                    config.useRealMass ? realMassAmount : massAmount,
                    softnessAmount,
                    quicknessAmount,
                    multiplier
                )
            );
        }

        public void UpdateNipplePhysics(float softnessAmount, float nippleErectionVal)
        {
            _nipplePhysicsConfigs.ForEach(
                config => config.UpdateNippleVal(
                    config.useRealMass ? realMassAmount : massAmount,
                    softnessAmount,
                    1.25f * nippleErectionVal
                )
            );
        }

        public void UpdateRateDependentPhysics(float softnessAmount, float quicknessAmount)
        {
            float multiplier = PhysicsRateMultiplier();

            _mainPhysicsConfigs
                .Concat(_softPhysicsConfigs)
                .Where(config => config.dependOnPhysicsRate)
                .ToList()
                .ForEach(
                    config => config.UpdateVal(
                        config.useRealMass ? realMassAmount : massAmount,
                        softnessAmount,
                        quicknessAmount,
                        multiplier
                    )
                );
        }

        public void UpdatePectoralPhysics()
        {
            _pectoralPhysicsConfigs.ForEach(
                config => config.UpdateVal(massAmount)
            );
        }

        // see UserPreferences.cs methods SetPhysics45, 60, 72 etc.
        private static float PhysicsRateMultiplier()
        {
            return 0.01666667f / Time.fixedDeltaTime;
        }
    }
}
