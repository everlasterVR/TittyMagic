using SimpleJSON;
using System.Collections.Generic;
using UnityEngine;
using static TittyMagic.Utils;
using static TittyMagic.Globals;

namespace TittyMagic
{
    internal class StaticPhysicsHandler
    {
        private HashSet<PhysicsConfig> mainPhysicsConfigs;
        private HashSet<RateDependentPhysicsConfig> rateDependentPhysicsConfigs;
        private HashSet<PhysicsConfig> softPhysicsConfigs;
        private HashSet<NipplePhysicsConfig> nipplePhysicsConfigs;

        public JSONStorableStringChooser modeChooser;

        private float massVal;

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
            mainPhysicsConfigs = new HashSet<PhysicsConfig>();
            rateDependentPhysicsConfigs = new HashSet<RateDependentPhysicsConfig>();
            softPhysicsConfigs = new HashSet<PhysicsConfig>();
            nipplePhysicsConfigs = new HashSet<NipplePhysicsConfig>();

            Persistence.LoadModePhysicsSettings(script, mode, @"mainPhysics.json", (path, json) =>
            {
                foreach(string param in json.Keys)
                {
                    JSONClass paramSettings = json[param].AsObject;
                    if(param == "damper")
                    {
                        rateDependentPhysicsConfigs.Add(new RateDependentPhysicsConfig(
                            BREAST_CONTROL.GetFloatJSONParam(param),
                            paramSettings["minMminS"].AsFloat,
                            paramSettings["maxMminS"].AsFloat,
                            paramSettings["minMmaxS"].AsFloat
                        ));
                        continue;
                    }

                    mainPhysicsConfigs.Add(new PhysicsConfig(
                        BREAST_CONTROL.GetFloatJSONParam(param),
                        paramSettings["minMminS"].AsFloat,
                        paramSettings["maxMminS"].AsFloat,
                        paramSettings["minMmaxS"].AsFloat
                    ));
                }
            });

            Persistence.LoadModePhysicsSettings(script, mode, @"softPhysics.json", (path, json) =>
            {
                foreach(string param in json.Keys)
                {
                    JSONClass paramSettings = json[param].AsObject;
                    softPhysicsConfigs.Add(new PhysicsConfig(
                        BREAST_PHYSICS_MESH.GetFloatJSONParam(param),
                        paramSettings["minMminS"].AsFloat,
                        paramSettings["maxMminS"].AsFloat,
                        paramSettings["minMmaxS"].AsFloat
                    ));
                }
            });

            Persistence.LoadModePhysicsSettings(script, mode, @"nipplePhysics.json", (path, json) =>
            {
                foreach(string param in json.Keys)
                {
                    JSONClass paramSettings = json[param].AsObject;
                    nipplePhysicsConfigs.Add(new NipplePhysicsConfig(
                        BREAST_PHYSICS_MESH.GetFloatJSONParam(param),
                        paramSettings["minMminS"].AsFloat,
                        paramSettings["maxMminS"].AsFloat,
                        paramSettings["minMmaxS"].AsFloat
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
            massVal = Mathf.InverseLerp(0, Const.MASS_MAX, massEstimate);
            return massVal;
        }

        public void UpdateMainPhysics(float softnessVal)
        {
            foreach(var it in mainPhysicsConfigs)
                it.UpdateVal(massVal, softnessVal);
            foreach(var it in rateDependentPhysicsConfigs)
                it.UpdateVal(massVal, softnessVal, PhysicsRateMultiplier());
        }

        public void UpdateRateDependentPhysics(float softnessVal)
        {
            foreach(var it in rateDependentPhysicsConfigs)
                it.UpdateVal(massVal, softnessVal, PhysicsRateMultiplier());
        }

        public void UpdateNipplePhysics(float softnessVal, float nippleErectionVal)
        {
            foreach(var it in nipplePhysicsConfigs)
                it.UpdateVal(massVal, softnessVal, nippleErectionVal);
        }

        public void FullUpdate(float softnessVal, float nippleErectionVal)
        {
            foreach(var it in mainPhysicsConfigs)
                it.UpdateVal(massVal, softnessVal);
            foreach(var it in rateDependentPhysicsConfigs)
                it.UpdateVal(massVal, softnessVal, PhysicsRateMultiplier());
            foreach(var it in softPhysicsConfigs)
                it.UpdateVal(massVal, softnessVal);
            foreach(var it in nipplePhysicsConfigs)
                it.UpdateVal(massVal, softnessVal, nippleErectionVal);
        }

        public string GetStatus()
        {
            string text = "MAIN PHYSICS\n";
            text += NameValueString("mass", BREAST_CONTROL.mass, padRight: 25) + "\n";
            foreach(var it in mainPhysicsConfigs)
                text += it.GetStatus();
            foreach(var it in rateDependentPhysicsConfigs)
                text += it.GetStatus();

            text += "\nSOFT PHYSICS\n";
            foreach(var it in softPhysicsConfigs)
                text += it.GetStatus();
            foreach(var it in nipplePhysicsConfigs)
                text += it.GetStatus();

            return text;
        }

        //see UserPreferences.cs methods SetPhysics45, 60, 72 etc.
        private float PhysicsRateMultiplier()
        {
            return 0.01666667f/Time.fixedDeltaTime;
        }
    }
}
