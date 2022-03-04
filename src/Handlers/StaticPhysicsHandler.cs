using System.Collections.Generic;
using UnityEngine;
using static TittyMagic.Globals;

namespace TittyMagic
{
    internal class StaticPhysicsHandler
    {
        private HashSet<BreastPhysicsConfig> _mainPhysicsConfigs;
        private HashSet<BreastPhysicsConfig> _softPhysicsConfigs;
        private HashSet<BreastPhysicsConfig> _nipplePhysicsConfigs;

        private HashSet<PectoralPhysicsConfig> _pectoralPhysicsConfigs;

        public JSONStorableStringChooser modeChooser; // TODO male

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
            _mainPhysicsConfigs = new HashSet<BreastPhysicsConfig>();
            _softPhysicsConfigs = new HashSet<BreastPhysicsConfig>();
            _nipplePhysicsConfigs = new HashSet<BreastPhysicsConfig>();

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
                            new BreastPhysicsConfig(
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
                            new BreastPhysicsConfig(
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
                            new BreastPhysicsConfig(
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
            _pectoralPhysicsConfigs = new HashSet<PectoralPhysicsConfig>();

            Persistence.LoadFromPath(
                script,
                $@"{PLUGIN_PATH}settings\staticphysics\pectoralPhysics.json",
                (path, json) =>
                {
                    foreach(string param in json.Keys)
                    {
                        var paramSettings = json[param].AsObject;
                        _pectoralPhysicsConfigs.Add(
                            new PectoralPhysicsConfig(
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
            BREAST_PHYSICS_MESH.softVerticesColliderAdditionalNormalOffset = 0.002f;
        }

        public float SetAndReturnMassVal(float massEstimate)
        {
            if(modeChooser?.val != Mode.TOUCH_OPTIMIZED)
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
