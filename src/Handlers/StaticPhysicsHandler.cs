using SimpleJSON;
using System.Collections.Generic;
using UnityEngine;
using static TittyMagic.Utils;
using static TittyMagic.Globals;

namespace TittyMagic
{
    internal class StaticPhysicsHandler
    {
        private HashSet<PhysicsConfig> _mainPhysicsConfigs;
        private HashSet<PhysicsConfig> _softPhysicsConfigs;
        private HashSet<PhysicsConfig> _nipplePhysicsConfigs;

        public JSONStorableStringChooser modeChooser;

        private float _massVal;

        public StaticPhysicsHandler()
        {
            SetPhysicsDefaults();
        }

        public void LoadSettings(MVRScript script, string mode)
        {
            if(mode == Mode.BALANCED)
            {
                GEOMETRY.useAuxBreastColliders = true;
            }
            else if(mode == Mode.TOUCH_OPTIMIZED || mode == Mode.ANIM_OPTIMIZED)
            {
                GEOMETRY.useAuxBreastColliders = false;
            }

            LoadSettingsFromFiles(script, mode);
        }

        private void LoadSettingsFromFiles(MVRScript script, string mode)
        {
            _mainPhysicsConfigs = new HashSet<PhysicsConfig>();
            _softPhysicsConfigs = new HashSet<PhysicsConfig>();
            _nipplePhysicsConfigs = new HashSet<PhysicsConfig>();

            Persistence.LoadModePhysicsSettings(script, mode, @"mainPhysics.json", (path, json) =>
            {
                foreach(string param in json.Keys)
                {
                    JSONClass paramSettings = json[param].AsObject;
                    _mainPhysicsConfigs.Add(new PhysicsConfig(
                        BREAST_CONTROL.GetFloatJSONParam(param),
                        paramSettings["minMminS"].AsFloat,
                        paramSettings["maxMminS"].AsFloat,
                        paramSettings["minMmaxS"].AsFloat,
                        paramSettings["dependOnPhysicsRate"].AsBool
                    ));
                }
            });

            Persistence.LoadModePhysicsSettings(script, mode, @"softPhysics.json", (path, json) =>
            {
                foreach(string param in json.Keys)
                {
                    JSONClass paramSettings = json[param].AsObject;
                    _softPhysicsConfigs.Add(new PhysicsConfig(
                        BREAST_PHYSICS_MESH.GetFloatJSONParam(param),
                        paramSettings["minMminS"].AsFloat,
                        paramSettings["maxMminS"].AsFloat,
                        paramSettings["minMmaxS"].AsFloat,
                        paramSettings["dependOnPhysicsRate"].AsBool
                    ));
                }
            });

            Persistence.LoadModePhysicsSettings(script, mode, @"nipplePhysics.json", (path, json) =>
            {
                foreach(string param in json.Keys)
                {
                    JSONClass paramSettings = json[param].AsObject;
                    _nipplePhysicsConfigs.Add(new PhysicsConfig(
                        BREAST_PHYSICS_MESH.GetFloatJSONParam(param),
                        paramSettings["minMminS"].AsFloat,
                        paramSettings["maxMminS"].AsFloat,
                        paramSettings["minMmaxS"].AsFloat,
                        paramSettings["dependOnPhysicsRate"].AsBool
                    ));
                }
            });
        }

        private void SetPhysicsDefaults()
        {
            //Soft physics on
            BREAST_PHYSICS_MESH.on = true;
            //Self colliders off
            BREAST_PHYSICS_MESH.allowSelfCollision = true;
            //Auto collider radius off
            BREAST_PHYSICS_MESH.softVerticesUseAutoColliderRadius = false;
            //Collider depth
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
            var physicsRateMultiplier = PhysicsRateMultiplier();
            foreach(var it in _mainPhysicsConfigs)
            {
                if(it.DependOnPhysicsRate)
                    it.UpdateVal(_massVal, softnessVal, physicsRateMultiplier);
                else
                    it.UpdateVal(_massVal, softnessVal);
            }
        }

        public void UpdateRateDependentPhysics(float softnessVal)
        {
            var physicsRateMultiplier = PhysicsRateMultiplier();
            foreach(var it in _mainPhysicsConfigs)
            {
                if(it.DependOnPhysicsRate)
                    it.UpdateVal(_massVal, softnessVal, physicsRateMultiplier);
            }
            foreach(var it in _softPhysicsConfigs)
            {
                if(it.DependOnPhysicsRate)
                    it.UpdateVal(_massVal, softnessVal, physicsRateMultiplier);
            }
        }

        public void UpdateNipplePhysics(float softnessVal, float nippleErectionVal)
        {
            foreach(var it in _nipplePhysicsConfigs)
                it.UpdateVal(_massVal, softnessVal, 1, 1.25f * nippleErectionVal);
        }

        public void FullUpdate(float softnessVal, float nippleErectionVal)
        {
            var physicsRateMultiplier = PhysicsRateMultiplier();
            foreach(var it in _mainPhysicsConfigs)
            {
                if(it.DependOnPhysicsRate)
                    it.UpdateVal(_massVal, softnessVal, physicsRateMultiplier);
                else
                    it.UpdateVal(_massVal, softnessVal);
            }
            foreach(var it in _softPhysicsConfigs)
            {
                if(it.DependOnPhysicsRate)
                    it.UpdateVal(_massVal, softnessVal, physicsRateMultiplier);
                else
                    it.UpdateVal(_massVal, softnessVal);
            }
            foreach(var it in _nipplePhysicsConfigs)
            {
                it.UpdateVal(_massVal, softnessVal, 1, 1.25f * nippleErectionVal);
            }
        }

        //see UserPreferences.cs methods SetPhysics45, 60, 72 etc.
        private float PhysicsRateMultiplier()
        {
            return 0.01666667f/Time.fixedDeltaTime;
        }
    }
}
