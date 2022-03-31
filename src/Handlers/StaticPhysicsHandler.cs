using System.Collections.Generic;
using UnityEngine;
using static TittyMagic.Globals;

namespace TittyMagic
{
    internal class StaticPhysicsHandler
    {
        private List<StaticPhysicsConfig> _mainPhysicsConfigs;
        private List<StaticPhysicsConfig> _softPhysicsConfigs;
        private List<StaticPhysicsConfig> _realMassSoftPhysicsConfigs;
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
            _mainPhysicsConfigs = new List<StaticPhysicsConfig>
            {
                new BreastStaticPhysicsConfig("centerOfGravityPercent", 0.350f, 0.480f, 0.560f)
                {
                },
                new BreastStaticPhysicsConfig("spring", 50f, 64f, 45f)
                {
                    quicknessOffsetConfig = new StaticPhysicsConfigBase(20f, 24f, 18f),
                    slownessOffsetConfig = new StaticPhysicsConfigBase(-13f, -16f, -12f),
                },
                new BreastStaticPhysicsConfig("damper", 1.2f, 1.5f, 0.8f, true)
                {
                    quicknessOffsetConfig = new StaticPhysicsConfigBase(-0.6f, -0.75f, -0.4f),
                    slownessOffsetConfig = new StaticPhysicsConfigBase(0.4f, 0.5f, 0.27f),
                },
                new BreastStaticPhysicsConfig("positionSpringZ", 450f, 550f, 250f)
                {
                    quicknessOffsetConfig = new StaticPhysicsConfigBase(90, 110, 50f),
                    slownessOffsetConfig = new StaticPhysicsConfigBase(-60, -70, -33f),
                },
                new BreastStaticPhysicsConfig("positionDamperZ", 16f, 22f, 9f, true),
            };

            _softPhysicsConfigs = new List<StaticPhysicsConfig>
            {
                new BreastSoftStaticPhysicsConfig("softVerticesCombinedSpring", 240f, 240f, 62f),
                new BreastSoftStaticPhysicsConfig("softVerticesCombinedDamper", 1.50f, 1.80f, 0.90f, true)
                {
                    quicknessOffsetConfig = new StaticPhysicsConfigBase(-0.75f, -0.90f, -0.45f),
                    slownessOffsetConfig = new StaticPhysicsConfigBase(1.125f, 1.35f, 0.675f),
                },
                new BreastSoftStaticPhysicsConfig("softVerticesMass", 0.050f, 0.120f, 0.070f)
                {
                    quicknessOffsetConfig = new StaticPhysicsConfigBase(0.000f, -0.048f, -0.028f),
                    slownessOffsetConfig = new StaticPhysicsConfigBase(0.012f, 0.060f, 0.040f),
                },
                new BreastSoftStaticPhysicsConfig("softVerticesBackForce", 10.4f, 19.5f, 7.0f)
                {
                    quicknessOffsetConfig = new StaticPhysicsConfigBase(-5.2f, -9.25f, -3.5f),
                    slownessOffsetConfig = new StaticPhysicsConfigBase(2.6f, 4.625f, 1.75f),
                },
                new BreastSoftStaticPhysicsConfig("softVerticesBackForceThresholdDistance", 0.001f, -0.0005f, 0.001f),
                new BreastSoftStaticPhysicsConfig("softVerticesBackForceMaxForce", 50f, 50f, 50f),
                new BreastSoftStaticPhysicsConfig("groupASpringMultiplier", 1f, 1f, 1f),
                new BreastSoftStaticPhysicsConfig("groupADamperMultiplier", 1f, 1f, 1f),
                new BreastSoftStaticPhysicsConfig("groupBSpringMultiplier", 1f, 1f, 1f),
                new BreastSoftStaticPhysicsConfig("groupBDamperMultiplier", 1f, 1f, 1f),
                new BreastSoftStaticPhysicsConfig("groupCSpringMultiplier", 2.29f, 1.30f, 2.29f),
                new BreastSoftStaticPhysicsConfig("groupCDamperMultiplier", 1.81f, 1.22f, 1.81f),
            };

            _realMassSoftPhysicsConfigs = new List<StaticPhysicsConfig>
            {
                new BreastSoftStaticPhysicsConfig("softVerticesColliderRadius", 0.024f, 0.034f, 0.024f),
                new BreastSoftStaticPhysicsConfig("softVerticesDistanceLimit", 0.020f, 0.068f, 0.028f)
                {
                    quicknessOffsetConfig = new StaticPhysicsConfigBase(0.000f, 0.000f, 0.024f),
                    slownessOffsetConfig = new StaticPhysicsConfigBase(0.000f, 0.000f, -0.008f),
                },
            };

            _nipplePhysicsConfigs = new List<StaticPhysicsConfig>
            {
                new BreastSoftStaticPhysicsConfig("groupDSpringMultiplier", 2.29f, 1.30f, 2.29f),
                new BreastSoftStaticPhysicsConfig("groupDDamperMultiplier", 1.81f, 1.22f, 1.81f),
            };
        }

        private void LoadPectoralSettings()
        {
            _pectoralPhysicsConfigs = new List<PectoralStaticPhysicsConfig>
            {
                new PectoralStaticPhysicsConfig("centerOfGravityPercent", 0.160f, 0.430f),
                new PectoralStaticPhysicsConfig("spring", 55f, 35f),
                new PectoralStaticPhysicsConfig("damper", 0.3f, 0.8f),
                new PectoralStaticPhysicsConfig("positionSpringZ", 170f, 120f),
                new PectoralStaticPhysicsConfig("positionDamperZ", 25f, 48f),
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
            foreach(var it in _mainPhysicsConfigs)
            {
                it.UpdateVal(massAmount, softnessAmount, quicknessAmount, multiplier);
            }
        }

        public void UpdateRateDependentPhysics(float softnessAmount, float quicknessAmount)
        {
            float multiplier = PhysicsRateMultiplier();
            foreach(var it in _mainPhysicsConfigs)
            {
                if(it.dependOnPhysicsRate)
                {
                    it.UpdateVal(massAmount, softnessAmount, quicknessAmount, multiplier);
                }
            }

            foreach(var it in _softPhysicsConfigs)
            {
                if(it.dependOnPhysicsRate)
                {
                    it.UpdateVal(massAmount, softnessAmount, quicknessAmount, multiplier);
                }
            }
        }

        public void UpdateNipplePhysics(float softnessAmount, float nippleErectionVal)
        {
            foreach(var it in _nipplePhysicsConfigs)
            {
                it.UpdateNippleVal(massAmount, softnessAmount, 1.25f * nippleErectionVal);
            }
        }

        public void FullUpdate(float softnessAmount, float quicknessAmount, float nippleErectionVal)
        {
            float multiplier = PhysicsRateMultiplier();
            foreach(var it in _mainPhysicsConfigs)
            {
                it.UpdateVal(massAmount, softnessAmount, quicknessAmount, multiplier);
            }

            foreach(var it in _softPhysicsConfigs)
            {
                it.UpdateVal(massAmount, softnessAmount, quicknessAmount, multiplier);
            }

            foreach(var it in _realMassSoftPhysicsConfigs)
            {
                it.UpdateVal(realMassAmount, softnessAmount, quicknessAmount, multiplier);
            }

            foreach(var it in _nipplePhysicsConfigs)
            {
                it.UpdateNippleVal(massAmount, softnessAmount, 1.25f * nippleErectionVal);
            }
        }

        public void UpdatePectoralPhysics()
        {
            foreach(var it in _pectoralPhysicsConfigs)
            {
                it.UpdateVal(massAmount);
            }
        }

        // see UserPreferences.cs methods SetPhysics45, 60, 72 etc.
        private static float PhysicsRateMultiplier()
        {
            return 0.01666667f / Time.fixedDeltaTime;
        }
    }
}
