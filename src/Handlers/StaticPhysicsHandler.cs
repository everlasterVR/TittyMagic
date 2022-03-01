using System.Collections.Generic;
using UnityEngine;
using static TittyMagic.Globals;

namespace TittyMagic
{
    internal class StaticPhysicsHandler
    {
        private HashSet<PhysicsConfig> _mainPhysicsConfigs;
        private HashSet<PhysicsConfig> _softPhysicsConfigs;
        private HashSet<PhysicsConfig> _nipplePhysicsConfigs;

        public JSONStorableStringChooser modeChooser; // TODO male

        private float _massVal;

        public StaticPhysicsHandler()
        {
            SetPhysicsDefaults();
        }

        public void LoadSettings(MVRScript script, string mode)
        {
            GEOMETRY.useAuxBreastColliders = mode != Mode.TOUCH_OPTIMIZED;
            LoadSettingsFromFiles(script, mode);
        }

        private void LoadSettingsFromFiles(MVRScript script, string mode)
        {
            _mainPhysicsConfigs = new HashSet<PhysicsConfig>();
            _softPhysicsConfigs = new HashSet<PhysicsConfig>();
            _nipplePhysicsConfigs = new HashSet<PhysicsConfig>();

            Persistence.LoadModePhysicsSettings(
                script,
                mode,
                @"mainPhysics.json",
                (path, json) =>
                {
                    foreach(string param in json.Keys)
                    {
                        var paramSettings = json[param].AsObject;
                        _mainPhysicsConfigs.Add(
                            new PhysicsConfig(
                                BREAST_CONTROL.GetFloatJSONParam(param),
                                paramSettings["minMminS"].AsFloat,
                                paramSettings["maxMminS"].AsFloat,
                                paramSettings["minMmaxS"].AsFloat,
                                paramSettings["dependOnPhysicsRate"].AsBool
                            )
                        );
                    }
                }
            );

            Persistence.LoadModePhysicsSettings(
                script,
                mode,
                @"softPhysics.json",
                (path, json) =>
                {
                    foreach(string param in json.Keys)
                    {
                        var paramSettings = json[param].AsObject;
                        _softPhysicsConfigs.Add(
                            new PhysicsConfig(
                                BREAST_PHYSICS_MESH.GetFloatJSONParam(param),
                                paramSettings["minMminS"].AsFloat,
                                paramSettings["maxMminS"].AsFloat,
                                paramSettings["minMmaxS"].AsFloat,
                                paramSettings["dependOnPhysicsRate"].AsBool
                            )
                        );
                    }
                }
            );

            Persistence.LoadModePhysicsSettings(
                script,
                mode,
                @"nipplePhysics.json",
                (path, json) =>
                {
                    foreach(string param in json.Keys)
                    {
                        var paramSettings = json[param].AsObject;
                        _nipplePhysicsConfigs.Add(
                            new PhysicsConfig(
                                BREAST_PHYSICS_MESH.GetFloatJSONParam(param),
                                paramSettings["minMminS"].AsFloat,
                                paramSettings["maxMminS"].AsFloat,
                                paramSettings["minMmaxS"].AsFloat,
                                paramSettings["dependOnPhysicsRate"].AsBool
                            )
                        );
                    }
                }
            );
        }

        private static void SetPhysicsDefaults()
        {
            // Self colliders off
            BREAST_PHYSICS_MESH.allowSelfCollision = true;
            // Auto collider radius off
            BREAST_PHYSICS_MESH.softVerticesUseAutoColliderRadius = false;
            // Collider depth
            BREAST_PHYSICS_MESH.softVerticesColliderAdditionalNormalOffset = 0.002f;
        }

        public float SetAndReturnMassVal(float massEstimate)
        {
            if(modeChooser.val != Mode.TOUCH_OPTIMIZED)
            {
                BREAST_CONTROL.mass = massEstimate;
            }

            _massVal = Mathf.InverseLerp(0, Const.MASS_MAX, massEstimate);
            return _massVal;
        }

        public void UpdateMainPhysics(float softnessVal)
        {
            float physicsRateMultiplier = PhysicsRateMultiplier();
            foreach(var it in _mainPhysicsConfigs)
            {
                if(it.dependOnPhysicsRate)
                    it.UpdateVal(_massVal, softnessVal, physicsRateMultiplier);
                else
                    it.UpdateVal(_massVal, softnessVal);
            }
        }

        public void UpdateRateDependentPhysics(float softnessVal)
        {
            float physicsRateMultiplier = PhysicsRateMultiplier();
            foreach(var it in _mainPhysicsConfigs)
            {
                if(it.dependOnPhysicsRate)
                    it.UpdateVal(_massVal, softnessVal, physicsRateMultiplier);
            }

            foreach(var it in _softPhysicsConfigs)
            {
                if(it.dependOnPhysicsRate)
                    it.UpdateVal(_massVal, softnessVal, physicsRateMultiplier);
            }
        }

        public void UpdateNipplePhysics(float softnessVal, float nippleErectionVal)
        {
            foreach(var it in _nipplePhysicsConfigs)
            {
                it.UpdateVal(_massVal, softnessVal, 1, 1.25f * nippleErectionVal);
            }
        }

        public void FullUpdate(float softnessVal, float nippleErectionVal)
        {
            float physicsRateMultiplier = PhysicsRateMultiplier();
            foreach(var it in _mainPhysicsConfigs)
            {
                if(it.dependOnPhysicsRate)
                    it.UpdateVal(_massVal, softnessVal, physicsRateMultiplier);
                else
                    it.UpdateVal(_massVal, softnessVal);
            }

            foreach(var it in _softPhysicsConfigs)
            {
                if(it.dependOnPhysicsRate)
                    it.UpdateVal(_massVal, softnessVal, physicsRateMultiplier);
                else
                    it.UpdateVal(_massVal, softnessVal);
            }

            foreach(var it in _nipplePhysicsConfigs)
            {
                it.UpdateVal(_massVal, softnessVal, 1, 1.25f * nippleErectionVal);
            }
        }

        // see UserPreferences.cs methods SetPhysics45, 60, 72 etc.
        private static float PhysicsRateMultiplier()
        {
            return 0.01666667f / Time.fixedDeltaTime;
        }
    }
}
