using SimpleJSON;
using System.Collections.Generic;
using UnityEngine;

namespace TittyMagic
{
    internal class StaticPhysicsHandler
    {
        private string settingsDir;

        private HashSet<PhysicsConfig> mainPhysicsConfigs;
        private HashSet<RateDependentPhysicsConfig> rateDependentPhysicsConfigs;
        private HashSet<PhysicsConfig> softPhysicsConfigs;
        private HashSet<NipplePhysicsConfig> nipplePhysicsConfigs;

        public JSONStorableStringChooser modeChooser;

        public StaticPhysicsHandler(string packagePath)
        {
            settingsDir = packagePath + @"Custom\Scripts\everlaster\TittyMagic\src\Settings";
            SetPhysicsDefaults();
        }

        public void LoadSettingsFromFile()
        {
            string modeDir = $@"{settingsDir}\{modeChooser.val}\";

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
            if(val == "Balanced")
            {
                Globals.GEOMETRY.useAuxBreastColliders = true;
            }
            else if(val == "TouchOptimized")
            {
                Globals.GEOMETRY.useAuxBreastColliders = false;
            }

            LoadSettingsFromFile();
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

        public void UpdateMainPhysics(
            float massEstimate,
            float softnessVal
        )
        {
            float mass = NormalizedMass(massEstimate);
            float softness = NormalizedSoftness(softnessVal);

            if(modeChooser.val != "Touch optimized")
            {
                Globals.BREAST_CONTROL.mass = massEstimate;
            }

            foreach(var it in mainPhysicsConfigs)
                it.UpdateVal(mass, softness);
            foreach(var it in rateDependentPhysicsConfigs)
                it.UpdateVal(mass, softness, PhysicsRateMultiplier());
        }

        public void UpdateRateDependentPhysics(
            float massEstimate,
            float softnessVal
        )
        {
            float mass = NormalizedMass(massEstimate);
            float softness = NormalizedSoftness(softnessVal);

            foreach(var it in rateDependentPhysicsConfigs)
                it.UpdateVal(mass, softness, PhysicsRateMultiplier());
        }

        public void UpdateNipplePhysics(
            float massEstimate,
            float softnessVal,
            float nippleErectionVal
        )
        {
            float mass = NormalizedMass(massEstimate);
            float softness = NormalizedSoftness(softnessVal);

            foreach(var it in nipplePhysicsConfigs)
                it.UpdateVal(mass, softness, nippleErectionVal);
        }

        public void FullUpdate(
            float massEstimate,
            float softnessVal,
            float nippleErectionVal
        )
        {
            float mass = NormalizedMass(massEstimate);
            float softness = NormalizedSoftness(softnessVal);

            if(modeChooser.val != "Touch optimized")
            {
                Globals.BREAST_CONTROL.mass = massEstimate;
            }

            foreach(var it in mainPhysicsConfigs)
                it.UpdateVal(mass, softness);
            foreach(var it in rateDependentPhysicsConfigs)
                it.UpdateVal(mass, softness, PhysicsRateMultiplier());
            foreach(var it in softPhysicsConfigs)
                it.UpdateVal(mass, softness);
            foreach(var it in nipplePhysicsConfigs)
                it.UpdateVal(mass, softness, nippleErectionVal);
        }

        public string GetStatus()
        {
            string text = "MAIN PHYSICS\n";
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

        private float NormalizedMass(float massEstimate)
        {
            return (massEstimate - Const.MASS_MIN)/(Const.MASS_MAX - Const.MASS_MIN);
        }

        private float NormalizedSoftness(float softnessVal)
        {
            return (softnessVal - Const.SOFTNESS_MIN)/(Const.SOFTNESS_MAX - Const.SOFTNESS_MIN);
        }

        //see UserPreferences.cs methods SetPhysics45, 60, 72 etc.
        private float PhysicsRateMultiplier()
        {
            return 0.01666667f/Time.fixedDeltaTime;
        }
    }
}
