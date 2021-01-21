using UnityEngine;

namespace everlaster
{
    class StaticPhysicsHandler
    {
        private AdjustJoints Control;
        private DAZPhysicsMesh Mesh;

        private JSONStorableFloat mass;
        private JSONStorableFloat centerOfGravityPercent;
        private JSONStorableFloat spring;
        private JSONStorableFloat damper;
        private JSONStorableFloat positionSpringZ;
        private JSONStorableFloat positionDamperZ;

        private JSONStorableFloat softVerticesCombinedSpring;
        private JSONStorableFloat softVerticesCombinedDamper;
        private JSONStorableFloat softVerticesMass;
        private JSONStorableFloat softVerticesBackForce;
        private JSONStorableFloat softVerticesBackForceThresholdDistance;
        private JSONStorableFloat softVerticesBackForceMaxForce;
        private JSONStorableFloat softVerticesColliderRadius;
        private JSONStorableFloat softVerticesDistanceLimit;

        private JSONStorableFloat groupASpringMultiplier;
        private JSONStorableFloat groupADamperMultiplier;
        private JSONStorableFloat groupBSpringMultiplier;
        private JSONStorableFloat groupBDamperMultiplier;
        private JSONStorableFloat groupCSpringMultiplier;
        private JSONStorableFloat groupCDamperMultiplier;
        private JSONStorableFloat groupDSpringMultiplier;
        private JSONStorableFloat groupDDamperMultiplier;

        public StaticPhysicsHandler(AdjustJoints breastControl, DAZPhysicsMesh breastPhysicsMesh)
        {
            Control = breastControl;
            Mesh = breastPhysicsMesh;

            mass = Control.GetFloatJSONParam("mass");
            centerOfGravityPercent = Control.GetFloatJSONParam("centerOfGravityPercent");
            spring = Control.GetFloatJSONParam("spring");
            damper = Control.GetFloatJSONParam("damper");
            positionSpringZ = Control.GetFloatJSONParam("positionSpringZ");
            positionDamperZ = Control.GetFloatJSONParam("positionDamperZ");

            softVerticesCombinedSpring = Mesh.GetFloatJSONParam("softVerticesCombinedSpring");
            softVerticesCombinedDamper = Mesh.GetFloatJSONParam("softVerticesCombinedDamper");
            softVerticesMass = Mesh.GetFloatJSONParam("softVerticesMass");
            softVerticesBackForce = Mesh.GetFloatJSONParam("softVerticesBackForce");
            softVerticesBackForceThresholdDistance = Mesh.GetFloatJSONParam("softVerticesBackForceThresholdDistance");
            softVerticesBackForceMaxForce = Mesh.GetFloatJSONParam("softVerticesBackForceMaxForce");
            softVerticesColliderRadius = Mesh.GetFloatJSONParam("softVerticesColliderRadius");
            softVerticesDistanceLimit = Mesh.GetFloatJSONParam("softVerticesDistanceLimit");

            groupASpringMultiplier = Mesh.GetFloatJSONParam("groupASpringMultiplier");
            groupADamperMultiplier = Mesh.GetFloatJSONParam("groupADamperMultiplier");
            groupBSpringMultiplier = Mesh.GetFloatJSONParam("groupBSpringMultiplier");
            groupBDamperMultiplier = Mesh.GetFloatJSONParam("groupBDamperMultiplier");
            groupCSpringMultiplier = Mesh.GetFloatJSONParam("groupCSpringMultiplier");
            groupCDamperMultiplier = Mesh.GetFloatJSONParam("groupCDamperMultiplier");
            groupDSpringMultiplier = Mesh.GetFloatJSONParam("groupDSpringMultiplier");
            groupDDamperMultiplier = Mesh.GetFloatJSONParam("groupDDamperMultiplier");
        }

        public void SetPhysicsDefaults()
        {
            // Right/left angle target moves both breasts in the same direction
            Control.invertJoint2RotationY = false;
            // Soft physics on
            Mesh.on = true;
            // Auto collider radius off
            Mesh.softVerticesUseAutoColliderRadius = false;
            // Collider depth
            Mesh.softVerticesColliderAdditionalNormalOffset = 0.001f;
        }

        public void Update(
            float scaleVal,
            float scaleMin,
            float softnessVal,
            float softnessMax,
            float atomScaleFactor,
            float nippleErectionVal
        )
        {
            float hardnessVal = softnessMax - softnessVal; // 0.00 .. 2.50 for softness 3.00 .. 0.50
            float scaleFactor = atomScaleFactor * (scaleVal - scaleMin);

            //                                    base      size adjustment         softness adjustment
            mass.val                            = 0.20f  + (0.621f * scaleFactor);
            spring.val                          = 34f    + (10f    * scaleFactor) + (10f    * hardnessVal);
            damper.val                          = 0.75f  + (0.66f  * scaleFactor);
            positionSpringZ.val                 = 250f   + (200f   * scaleFactor);
            positionDamperZ.val                 = 5f     + (3.0f   * scaleFactor) + (3.0f   * softnessVal);
            softVerticesColliderRadius.val      = 0.022f + (0.005f * scaleFactor);
            softVerticesCombinedSpring.val      = 80f    + (60f    * scaleFactor) + (45f    * softnessVal);
            softVerticesCombinedDamper.val      = 1.00f  + (1.20f  * scaleFactor) + (0.30f  * softnessVal);
            softVerticesMass.val                = 0.08f  + (0.10f  * scaleFactor);
            softVerticesBackForce.val           = 2.0f   + (5.0f   * scaleFactor) + (4.0f   * hardnessVal);
            softVerticesBackForceMaxForce.val   = 2.0f   + (1.5f   * scaleFactor) + (1.0f   * hardnessVal);
            softVerticesDistanceLimit.val       = 0.010f + (0.010f * scaleFactor) + (0.003f * softnessVal);

            groupASpringMultiplier.val  = (1.00f + (0.15f * softnessVal) - (0.10f * scaleFactor)) / softnessVal;
            groupADamperMultiplier.val  = groupASpringMultiplier.val;
            groupBSpringMultiplier.val  = (1.80f + (0.20f * softnessVal) - (0.10f * scaleFactor)) / softnessVal;
            groupBDamperMultiplier.val  = groupBSpringMultiplier.val;
            groupCSpringMultiplier.val  = (1.10f + (0.25f * softnessVal)) / softnessVal;
            groupCDamperMultiplier.val  = groupCSpringMultiplier.val;
            groupDSpringMultiplier.val  = nippleErectionVal + groupCSpringMultiplier.val;
            groupDDamperMultiplier.val  = nippleErectionVal + groupCSpringMultiplier.val;

            softVerticesBackForceThresholdDistance.val = (float) Calc.RoundToDecimals(
                0.0015f + (0.002f * scaleFactor) - (0.001f * softnessVal),
                1000f
            );
        }

        public string GetStatus()
        {
            return (
                $"{Formatting.NameValueString("mass", mass.val, padRight: 25)}\n" +
                $"{Formatting.NameValueString("center of g", centerOfGravityPercent.val, padRight: 25)}\n" +
                $"{Formatting.NameValueString("spring", spring.val, padRight: 25)}\n" +
                $"{Formatting.NameValueString("damper", damper.val, padRight: 25)}\n" +
                $"{Formatting.NameValueString("in/out spr", positionSpringZ.val, padRight: 25)}\n" +
                $"{Formatting.NameValueString("in/out dmp", positionDamperZ.val, padRight: 25)}\n" +
                $"{Formatting.NameValueString("collider radius", softVerticesColliderRadius.val, padRight: 25)}\n" +
                $"{Formatting.NameValueString("back force", softVerticesBackForce.val, padRight: 25)}\n" +
                $"{Formatting.NameValueString("back force max", softVerticesBackForceMaxForce.val, padRight: 25)}\n" +
                $"{Formatting.NameValueString("back force thres", softVerticesBackForceThresholdDistance.val, padRight: 25)}\n" +
                $"{Formatting.NameValueString("fat spring", softVerticesCombinedSpring.val, padRight: 25)}\n" +
                $"{Formatting.NameValueString("fat damper", softVerticesCombinedDamper.val, padRight: 25)}\n" +
                $"{Formatting.NameValueString("fat mass", softVerticesMass.val, padRight: 25)}\n" +
                $"{Formatting.NameValueString("distance limit", softVerticesDistanceLimit.val, padRight: 25)}\n" +
                $"{Formatting.NameValueString("main spring", groupASpringMultiplier.val, padRight: 25)}\n" +
                $"{Formatting.NameValueString("main damper", groupADamperMultiplier.val, padRight: 25)}\n" +
                $"{Formatting.NameValueString("outer spring", groupBSpringMultiplier.val, padRight: 25)}\n" +
                $"{Formatting.NameValueString("outer damper", groupBDamperMultiplier.val, padRight: 25)}\n" +
                $"{Formatting.NameValueString("areola spring", groupCSpringMultiplier.val, padRight: 25)}\n" +
                $"{Formatting.NameValueString("areola damper", groupCDamperMultiplier.val, padRight: 25)}\n" +
                $"{Formatting.NameValueString("nipple spring", groupDSpringMultiplier.val, padRight: 25)}\n" +
                $"{Formatting.NameValueString("nipple damper", groupDDamperMultiplier.val, padRight: 25)}\n"
            );
        }
    }
}
