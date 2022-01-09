using SimpleJSON;
using System.Collections.Generic;
using UnityEngine;

namespace TittyMagic
{
    internal class StaticPhysicsHandler
    {
        private string settingsDir;

        private Dictionary<string, string> settingsSubDirNames = new Dictionary<string, string>
        {
            { Mode.ANIM_OPTIMIZED, "AnimOptimized" },
            { Mode.BALANCED, "Balanced" },
            { Mode.TOUCH_OPTIMIZED, "TouchOptimized" },
        };

        private HashSet<PhysicsConfig> mainPhysicsConfigs;
        private HashSet<RateDependentPhysicsConfig> rateDependentPhysicsConfigs;
        private HashSet<PhysicsConfig> softPhysicsConfigs;
        private HashSet<NipplePhysicsConfig> nipplePhysicsConfigs;

        public JSONStorableStringChooser modeChooser;

        private float massVal;

        public StaticPhysicsHandler(string packagePath)
        {
            settingsDir = packagePath + @"Custom\Scripts\everlaster\TittyMagic\src\Settings";
            SetPhysicsDefaults();
        }

        public void LoadSettingsFromFile(string val)
        {
            string modeDir = $@"{settingsDir}\{settingsSubDirNames[val]}\";

            JSONClass mainPhysicsSettings = SuperController.singleton.LoadJSON(modeDir + "mainPhysics.json").AsObject;
            JSONClass softPhysicsSettings = SuperController.singleton.LoadJSON(modeDir + "softPhysics.json").AsObject;
            JSONClass nipplePhysicsSettings = SuperController.singleton.LoadJSON(modeDir + "nipplePhysics.json").AsObject;

            mainPhysicsConfigs = new HashSet<PhysicsConfig>();
            rateDependentPhysicsConfigs = new HashSet<RateDependentPhysicsConfig>();
            softPhysicsConfigs = new HashSet<PhysicsConfig>();
            nipplePhysicsConfigs = new HashSet<NipplePhysicsConfig>();

            foreach(string param in mainPhysicsSettings.Keys)
            {
                JSONClass paramSettings = mainPhysicsSettings[param].AsObject;
                if(param == "damper")
                {
                    rateDependentPhysicsConfigs.Add(new RateDependentPhysicsConfig(
                        Globals.BREAST_CONTROL.GetFloatJSONParam(param),
                        paramSettings["minMminS"].AsFloat,
                        paramSettings["maxMminS"].AsFloat,
                        paramSettings["minMmaxS"].AsFloat
                    ));
                    continue;
                }

                mainPhysicsConfigs.Add(new PhysicsConfig(
                    Globals.BREAST_CONTROL.GetFloatJSONParam(param),
                    paramSettings["minMminS"].AsFloat,
                    paramSettings["maxMminS"].AsFloat,
                    paramSettings["minMmaxS"].AsFloat
                ));
            }

            foreach(string param in softPhysicsSettings.Keys)
            {
                JSONClass paramSettings = softPhysicsSettings[param].AsObject;
                softPhysicsConfigs.Add(new PhysicsConfig(
                    Globals.BREAST_PHYSICS_MESH.GetFloatJSONParam(param),
                    paramSettings["minMminS"].AsFloat,
                    paramSettings["maxMminS"].AsFloat,
                    paramSettings["minMmaxS"].AsFloat
                ));
            }

            foreach(string param in nipplePhysicsSettings.Keys)
            {
                JSONClass paramSettings = nipplePhysicsSettings[param].AsObject;
                nipplePhysicsConfigs.Add(new NipplePhysicsConfig(
                    Globals.BREAST_PHYSICS_MESH.GetFloatJSONParam(param),
                    paramSettings["minMminS"].AsFloat,
                    paramSettings["maxMminS"].AsFloat,
                    paramSettings["minMmaxS"].AsFloat
                ));
            }
        }

        public void LoadSettings(string val)
        {
            if(val == Mode.ANIM_OPTIMIZED || val == Mode.BALANCED)
            {
                Globals.GEOMETRY.useAuxBreastColliders = true;
            }
            else if(val == Mode.TOUCH_OPTIMIZED)
            {
                Globals.GEOMETRY.useAuxBreastColliders = false;
            }

            LoadSettingsFromFile(val);
        }

        private void SetPhysicsDefaults()
        {
            //Soft physics on
            Globals.BREAST_PHYSICS_MESH.on = true;
            //Self colliders off
            Globals.BREAST_PHYSICS_MESH.allowSelfCollision = true;
            //Auto collider radius off
            Globals.BREAST_PHYSICS_MESH.softVerticesUseAutoColliderRadius = false;
            //Collider depth
            Globals.BREAST_PHYSICS_MESH.softVerticesColliderAdditionalNormalOffset = 0.002f;
        }

        public float SetAndReturnMassVal(float massEstimate)
        {
            if(modeChooser.val != Mode.TOUCH_OPTIMIZED)
            {
                Globals.BREAST_CONTROL.mass = massEstimate;
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
            text += Formatting.NameValueString("mass", Globals.BREAST_CONTROL.mass, padRight: 25) + "\n";
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
