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

        public float massAmount { get; set; }

        public StaticPhysicsHandler(bool isFemale)
        {
            if(isFemale)
            {
                SetBreastPhysicsDefaults();
            }
        }

        public void LoadSettings(MVRScript script, bool isFemale)
        {
            if(isFemale)
            {
                LoadFemaleBreastSettings(script);
            }
            else
            {
                LoadPectoralSettings(script);
            }
        }

        private void LoadFemaleBreastSettings(MVRScript script)
        {
            _mainPhysicsConfigs = new HashSet<BreastStaticPhysicsConfig>();
            _softPhysicsConfigs = new HashSet<BreastStaticPhysicsConfig>();
            _nipplePhysicsConfigs = new HashSet<BreastStaticPhysicsConfig>();

            Persistence.LoadFromPath(
                script,
                $@"{PLUGIN_PATH}settings\staticphysics\touchoptimized\mainPhysics.json",
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

            Persistence.LoadFromPath(
                script,
                $@"{PLUGIN_PATH}settings\staticphysics\touchoptimized\softPhysics.json",
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

            Persistence.LoadFromPath(
                script,
                $@"{PLUGIN_PATH}settings\staticphysics\touchoptimized\nipplePhysics.json",
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
            // hard colliders off
            GEOMETRY.useAuxBreastColliders = false;
            // Self colliders off
            BREAST_PHYSICS_MESH.allowSelfCollision = true;
            // Auto collider radius off
            BREAST_PHYSICS_MESH.softVerticesUseAutoColliderRadius = false;
            // Collider depth
            BREAST_PHYSICS_MESH.softVerticesColliderAdditionalNormalOffset = 0.001f;
        }

        public void UpdateMainPhysics(float softnessVal)
        {
            float physicsRateMultiplier = PhysicsRateMultiplier();
            foreach(var it in _mainPhysicsConfigs)
            {
                if(it.dependOnPhysicsRate)
                    it.UpdateVal(massAmount, softnessVal, physicsRateMultiplier);
                else
                    it.UpdateVal(massAmount, softnessVal);
            }
        }

        public void UpdateRateDependentPhysics(float softnessVal)
        {
            float physicsRateMultiplier = PhysicsRateMultiplier();
            foreach(var it in _mainPhysicsConfigs)
            {
                if(it.dependOnPhysicsRate)
                    it.UpdateVal(massAmount, softnessVal, physicsRateMultiplier);
            }

            foreach(var it in _softPhysicsConfigs)
            {
                if(it.dependOnPhysicsRate)
                    it.UpdateVal(massAmount, softnessVal, physicsRateMultiplier);
            }
        }

        public void UpdateNipplePhysics(float softnessVal, float nippleErectionVal)
        {
            foreach(var it in _nipplePhysicsConfigs)
            {
                it.UpdateVal(massAmount, softnessVal, 1, 1.25f * nippleErectionVal);
            }
        }

        public void FullUpdate(float softnessVal, float nippleErectionVal)
        {
            float physicsRateMultiplier = PhysicsRateMultiplier();
            foreach(var it in _mainPhysicsConfigs)
            {
                if(it.dependOnPhysicsRate)
                    it.UpdateVal(massAmount, softnessVal, physicsRateMultiplier);
                else
                    it.UpdateVal(massAmount, softnessVal);
            }

            foreach(var it in _softPhysicsConfigs)
            {
                if(it.dependOnPhysicsRate)
                    it.UpdateVal(massAmount, softnessVal, physicsRateMultiplier);
                else
                    it.UpdateVal(massAmount, softnessVal);
            }

            foreach(var it in _nipplePhysicsConfigs)
            {
                it.UpdateVal(massAmount, softnessVal, 1, 1.25f * nippleErectionVal);
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
