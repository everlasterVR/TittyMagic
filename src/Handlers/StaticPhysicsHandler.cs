using System.Collections.Generic;
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
                    quicknessOffsetConfig = new StaticPhysicsConfigBase(-0.200f, -0.050f, -0.410f),
                },
                new BreastStaticPhysicsConfig("spring", 50f, 64f, 45f)
                {
                    quicknessOffsetConfig = new StaticPhysicsConfigBase(30f, -4f, 20f),
                },
                new BreastStaticPhysicsConfig("damper", 1.2f, 1.5f, 0.8f, true)
                {
                    quicknessOffsetConfig = new StaticPhysicsConfigBase(0.3f, 1.0f, -0.5f),
                },
                new BreastStaticPhysicsConfig("positionSpringZ", 450f, 550f, 250f)
                {
                    quicknessOffsetConfig = new StaticPhysicsConfigBase(-90, -220f, -50f),
                },
                new BreastStaticPhysicsConfig("positionDamperZ", 16f, 22f, 9f, true)
                {
                    quicknessOffsetConfig = new StaticPhysicsConfigBase(9f, 0f, 4f),
                },
            };

            _softPhysicsConfigs = new List<StaticPhysicsConfig>
            {
                new BreastSoftStaticPhysicsConfig("softVerticesCombinedSpring", 240f, 240f, 62f)
                {
                    quicknessOffsetConfig = new StaticPhysicsConfigBase(-80f, -95f, -16f),
                },
                new BreastSoftStaticPhysicsConfig("softVerticesCombinedDamper", 1.50f, 1.80f, 0.90f, true)
                {
                    quicknessOffsetConfig = new StaticPhysicsConfigBase(-0.125f, -0.175f, -0.75f),
                },
                new BreastSoftStaticPhysicsConfig("softVerticesMass", 0.050f, 0.120f, 0.070f)
                {
                    quicknessOffsetConfig = new StaticPhysicsConfigBase(0f, -0.020f, 0.002f),
                },
                new BreastSoftStaticPhysicsConfig("softVerticesBackForce", 10.4f, 19.5f, 7.0f),
                new BreastSoftStaticPhysicsConfig("softVerticesBackForceThresholdDistance", 0.001f, -0.0005f, 0.001f),
                new BreastSoftStaticPhysicsConfig("softVerticesBackForceMaxForce", 50f, 50f, 50f),
                new BreastSoftStaticPhysicsConfig("softVerticesColliderRadius", 0.024f, 0.034f, 0.024f),
                new BreastSoftStaticPhysicsConfig("softVerticesDistanceLimit", 0.020f, 0.068f, 0.028f),
                new BreastSoftStaticPhysicsConfig("groupASpringMultiplier", 1f, 1f, 1f),
                new BreastSoftStaticPhysicsConfig("groupADamperMultiplier", 1f, 1f, 1f),
                new BreastSoftStaticPhysicsConfig("groupBSpringMultiplier", 1f, 1f, 1f),
                new BreastSoftStaticPhysicsConfig("groupBDamperMultiplier", 1f, 1f, 1f),
                new BreastSoftStaticPhysicsConfig("groupCSpringMultiplier", 2.29f, 1.30f, 2.29f),
                new BreastSoftStaticPhysicsConfig("groupCDamperMultiplier", 1.81f, 1.22f, 1.81f),
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
