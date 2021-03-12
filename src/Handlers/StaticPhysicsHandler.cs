using SimpleJSON;
using System.Collections.Generic;

namespace TittyMagic
{
    internal class StaticPhysicsHandler
    {
        private readonly string settingsDir = @"Custom\Scripts\everlaster\TittyMagic\src\Settings\";

        private HashSet<PhysicsConfig> mainPhysicsConfigs;
        private HashSet<PhysicsConfig> softPhysicsConfigs;
        private HashSet<NipplePhysicsConfig> nipplePhysicsConfigs;

        public StaticPhysicsHandler()
        {
            JSONClass mainPhysicsSettings = SuperController.singleton.LoadJSON(settingsDir + "mainPhysics.json").AsObject;
            JSONClass softPhysicsSettings = SuperController.singleton.LoadJSON(settingsDir + "softPhysics.json").AsObject;
            JSONClass nipplePhysicsSettings = SuperController.singleton.LoadJSON(settingsDir + "nipplePhysics.json").AsObject;

            mainPhysicsConfigs = new HashSet<PhysicsConfig>();
            softPhysicsConfigs = new HashSet<PhysicsConfig>();
            nipplePhysicsConfigs = new HashSet<NipplePhysicsConfig>();

            foreach(string param in mainPhysicsSettings.Keys)
            {
                JSONClass paramSettings = mainPhysicsSettings[param].AsObject;
                mainPhysicsConfigs.Add(new PhysicsConfig(
                    Globals.BREAST_CONTROL.GetFloatJSONParam(param),
                    paramSettings["minMminS"].AsFloat,
                    paramSettings["maxMminS"].AsFloat,
                    paramSettings["minMmaxS"].AsFloat,
                    paramSettings["maxMmaxS"].AsFloat
                ));
            }

            foreach(string param in softPhysicsSettings.Keys)
            {
                JSONClass paramSettings = softPhysicsSettings[param].AsObject;
                softPhysicsConfigs.Add(new PhysicsConfig(
                    Globals.BREAST_PHYSICS_MESH.GetFloatJSONParam(param),
                    paramSettings["minMminS"].AsFloat,
                    paramSettings["maxMminS"].AsFloat,
                    paramSettings["minMmaxS"].AsFloat,
                    paramSettings["maxMmaxS"].AsFloat
                ));
            }

            foreach(string param in nipplePhysicsSettings.Keys)
            {
                JSONClass paramSettings = nipplePhysicsSettings[param].AsObject;
                nipplePhysicsConfigs.Add(new NipplePhysicsConfig(
                    Globals.BREAST_PHYSICS_MESH.GetFloatJSONParam(param),
                    paramSettings["minMminS"].AsFloat,
                    paramSettings["maxMminS"].AsFloat,
                    paramSettings["minMmaxS"].AsFloat,
                    paramSettings["maxMmaxS"].AsFloat
                ));
            }
        }

        public void SetPhysicsDefaults()
        {
            //Soft physics on
            Globals.BREAST_PHYSICS_MESH.on = true;
            //Self colliders on
            Globals.BREAST_PHYSICS_MESH.allowSelfCollision = true;
            //Auto collider radius off
            Globals.BREAST_PHYSICS_MESH.softVerticesUseAutoColliderRadius = false;
            //Collider depth
            Globals.BREAST_PHYSICS_MESH.softVerticesColliderAdditionalNormalOffset = 0.001f;
        }

        public void UpdateMainPhysics(
            float massEstimate,
            float softnessVal
        )
        {
            float massN = (massEstimate - 0.1f)/(2.0f - 0.1f);
            float softnessN = (softnessVal - 0.5f)/(3.0f - 0.5f);

            Globals.BREAST_CONTROL.mass = massEstimate;
            foreach(var it in mainPhysicsConfigs)
                it.UpdateVal(massN, softnessN);
        }

        public void UpdateNipplePhysics(
            float massEstimate,
            float softnessVal,
            float nippleErectionVal
        )
        {
            float massN = (massEstimate - 0.1f)/(2.0f - 0.1f);
            float softnessN = (softnessVal - 0.5f)/(3.0f - 0.5f);

            foreach(var it in nipplePhysicsConfigs)
                it.UpdateVal(massN, softnessN, nippleErectionVal);
        }

        public void FullUpdate(
            float massEstimate,
            float softnessVal,
            float nippleErectionVal
        )
        {
            float massN = (massEstimate - 0.1f)/(2.0f - 0.1f);
            float softnessN = (softnessVal - 0.5f)/(3.0f - 0.5f);

            Globals.BREAST_CONTROL.mass = massEstimate;
            foreach(var it in mainPhysicsConfigs)
                it.UpdateVal(massN, softnessN);
            foreach(var it in softPhysicsConfigs)
                it.UpdateVal(massN, softnessN);
            foreach(var it in nipplePhysicsConfigs)
                it.UpdateVal(massN, softnessN, nippleErectionVal);
        }

        public string GetStatus()
        {
            string text = "MAIN PHYSICS\n";
            foreach(var it in mainPhysicsConfigs)
                text += it.GetStatus();

            text += "\nSOFT PHYSICS\n";
            foreach(var it in softPhysicsConfigs)
                text += it.GetStatus();
            foreach(var it in nipplePhysicsConfigs)
                text += it.GetStatus();

            return text;
        }
    }
}
