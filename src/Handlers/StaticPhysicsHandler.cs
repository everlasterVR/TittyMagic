using System.Collections.Generic;
using UnityEngine;
using static TittyMagic.Globals;

namespace TittyMagic
{
    internal class StaticPhysicsHandler
    {
        private HashSet<BreastStaticPhysicsConfig> _mainPhysicsConfigs;
        private HashSet<BreastStaticPhysicsConfig> _softPhysicsConfigs;
        private HashSet<BreastStaticPhysicsConfig> _nipplePhysicsConfigs;
        private HashSet<PectoralStaticPhysicsConfig> _pectoralPhysicsConfigs;

        private float _massVal;

        public StaticPhysicsHandler(bool isFemale)
        {
            if(isFemale)
            {
                SetBreastPhysicsDefaults();
            }
        }

        public void LoadSettings(MVRScript script, string mode)
        {
            GEOMETRY.useAuxBreastColliders = mode != Mode.TOUCH_OPTIMIZED && mode != Mode.FUTA;
            if(mode == Mode.FUTA)
            {
                LoadPectoralSettings(script);
            }
            else
            {
                LoadSettingsFromFiles(script, mode);
            }
        }

        private void LoadSettingsFromFiles(MVRScript script, string mode)
        {
            _mainPhysicsConfigs = new HashSet<BreastStaticPhysicsConfig>();
            _softPhysicsConfigs = new HashSet<BreastStaticPhysicsConfig>();
            _nipplePhysicsConfigs = new HashSet<BreastStaticPhysicsConfig>();

            Persistence.LoadModeStaticPhysicsSettings(
                script,
                mode,
                @"mainPhysics.json",
                (path, json) =>
                {
                    foreach(string param in json.Keys)
                    {
                        var paramSettings = json[param].AsObject;
                        _mainPhysicsConfigs.Add(
                            new BreastStaticPhysicsConfig(
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

            Persistence.LoadModeStaticPhysicsSettings(
                script,
                mode,
                @"softPhysics.json",
                (path, json) =>
                {
                    foreach(string param in json.Keys)
                    {
                        var paramSettings = json[param].AsObject;
                        _softPhysicsConfigs.Add(
                            new BreastStaticPhysicsConfig(
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

            Persistence.LoadModeStaticPhysicsSettings(
                script,
                mode,
                @"nipplePhysics.json",
                (path, json) =>
                {
                    foreach(string param in json.Keys)
                    {
                        var paramSettings = json[param].AsObject;
                        _nipplePhysicsConfigs.Add(
                            new BreastStaticPhysicsConfig(
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

        private void LoadPectoralSettings(MVRScript script)
        {
            _pectoralPhysicsConfigs = new HashSet<PectoralStaticPhysicsConfig>();

            Persistence.LoadFromPath(
                script,
                $@"{PLUGIN_PATH}settings\staticphysics\pectoralPhysics.json",
                (path, json) =>
                {
                    foreach(string param in json.Keys)
                    {
                        var paramSettings = json[param].AsObject;
                        _pectoralPhysicsConfigs.Add(
                            new PectoralStaticPhysicsConfig(
                                BREAST_CONTROL.GetFloatJSONParam(param),
                                paramSettings["minM"].AsFloat,
                                paramSettings["maxM"].AsFloat
                            )
                        );
                    }
                }
            );
        }

        private static void SetBreastPhysicsDefaults()
        {
            // Self colliders off
            BREAST_PHYSICS_MESH.allowSelfCollision = true;
            // Auto collider radius off
            BREAST_PHYSICS_MESH.softVerticesUseAutoColliderRadius = false;
            // Collider depth
            BREAST_PHYSICS_MESH.softVerticesColliderAdditionalNormalOffset = 0.001f;
        }

        public float SetAndReturnMassVal(float massEstimate)
        {
            BREAST_CONTROL.mass = massEstimate;
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

        public void UpdatePectoralPhysics()
        {
            foreach(var it in _pectoralPhysicsConfigs)
            {
                it.UpdateVal(_massVal);
            }
        }

        // see UserPreferences.cs methods SetPhysics45, 60, 72 etc.
        private static float PhysicsRateMultiplier()
        {
            return 0.01666667f / Time.fixedDeltaTime;
        }
    }
}
