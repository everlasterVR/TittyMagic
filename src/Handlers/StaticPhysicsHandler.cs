using SimpleJSON;
using System.Collections.Generic;

namespace TittyMagic
{
    internal class StaticPhysicsHandler
    {
        private readonly Log log = new Log(nameof(StaticPhysicsHandler));
        private readonly string settingsDir = @"Custom\Scripts\everlaster\TittyMagic\src\Settings\";

        private HashSet<PhysicsConfig> mainPhysicsConfigs;
        private HashSet<PhysicsConfig> softPhysicsConfigs;

        public StaticPhysicsHandler()
        {
            JSONArray massMinSoftnessMin = SuperController.singleton.LoadJSON(settingsDir + "massMinSoftnessMin.json").AsArray;
            JSONArray massMaxSoftnessMin = SuperController.singleton.LoadJSON(settingsDir + "massMaxSoftnessMin.json").AsArray;
            JSONArray massMinSoftnessMax = SuperController.singleton.LoadJSON(settingsDir + "massMinSoftnessMax.json").AsArray;

            //assume same param names in all json files
            HashSet<string> breastControlParams = new HashSet<string>(massMinSoftnessMin[0].AsObject.Keys);
            HashSet<string> breastPhysicsMeshParams = new HashSet<string>(massMinSoftnessMin[1].AsObject.Keys);

            mainPhysicsConfigs = new HashSet<PhysicsConfig>();
            foreach(string paramName in breastControlParams)
            {
                mainPhysicsConfigs.Add(new PhysicsConfig(
                    Globals.BREAST_CONTROL.GetFloatJSONParam(paramName),
                    massMinSoftnessMin[0][paramName].AsFloat,
                    massMaxSoftnessMin[0][paramName].AsFloat,
                    massMinSoftnessMax[0][paramName].AsFloat
                ));
            }

            softPhysicsConfigs = new HashSet<PhysicsConfig>();
            foreach(string paramName in breastPhysicsMeshParams)
            {
                softPhysicsConfigs.Add(new PhysicsConfig(
                    Globals.BREAST_PHYSICS_MESH.GetFloatJSONParam(paramName),
                    massMinSoftnessMin[1][paramName].AsFloat,
                    massMaxSoftnessMin[1][paramName].AsFloat,
                    massMinSoftnessMax[1][paramName].AsFloat
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
            mainPhysicsConfigs.ToList().ForEach(config => config.UpdateVal(massN, softnessN));
        }

        //public void UpdateNipplePhysics(float softnessVal, float nippleErectionVal)
        //{
        //    float baseVal = (1.10f + (0.25f * softnessVal)) / softnessVal;
        //    groupDSpringMultiplier.val  = nippleErectionVal + baseVal;
        //    groupDDamperMultiplier.val  = 1.50f * nippleErectionVal + baseVal;
        //}

        public void FullUpdate(
            float massEstimate,
            float softnessVal,
            float nippleErectionVal
        )
        {
            float massN = (massEstimate - 0.1f)/(2.0f - 0.1f);
            float softnessN = (softnessVal - 0.5f)/(3.0f - 0.5f);

            Globals.BREAST_CONTROL.mass = massEstimate;
            mainPhysicsConfigs.ToList().ForEach(config => config.UpdateVal(massN, softnessN));
            softPhysicsConfigs.ToList().ForEach(config => config.UpdateVal(massN, softnessN));
        }

        public string GetStatus()
        {
            string text = "MAIN PHYSICS\n";
            mainPhysicsConfigs.ToList().ForEach((it) => text = text + it.GetStatus());
            text = text + "\nSOFT PHYSICS\n";
            softPhysicsConfigs.ToList().ForEach((it) => text = text + it.GetStatus());
            return text;
        }
    }
}
