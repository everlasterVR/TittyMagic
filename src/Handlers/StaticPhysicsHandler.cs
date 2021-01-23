namespace everlaster
{
    class StaticPhysicsHandler
    {
        private AdjustJoints BC;
        private DAZPhysicsMesh BPH;

        private JSONStorableFloat groupASpringMultiplier;
        private JSONStorableFloat groupADamperMultiplier;
        private JSONStorableFloat groupBSpringMultiplier;
        private JSONStorableFloat groupBDamperMultiplier;
        private JSONStorableFloat groupCSpringMultiplier;
        private JSONStorableFloat groupCDamperMultiplier;
        private JSONStorableFloat groupDSpringMultiplier;
        private JSONStorableFloat groupDDamperMultiplier;

        public StaticPhysicsHandler()
        {
            BC = Globals.BREAST_CONTROL;
            BPH = Globals.BREAST_PHYSICS_MESH;

            groupASpringMultiplier = BPH.GetFloatJSONParam("groupASpringMultiplier");
            groupADamperMultiplier = BPH.GetFloatJSONParam("groupADamperMultiplier");
            groupBSpringMultiplier = BPH.GetFloatJSONParam("groupBSpringMultiplier");
            groupBDamperMultiplier = BPH.GetFloatJSONParam("groupBDamperMultiplier");
            groupCSpringMultiplier = BPH.GetFloatJSONParam("groupCSpringMultiplier");
            groupCDamperMultiplier = BPH.GetFloatJSONParam("groupCDamperMultiplier");
            groupDSpringMultiplier = BPH.GetFloatJSONParam("groupDSpringMultiplier");
            groupDDamperMultiplier = BPH.GetFloatJSONParam("groupDDamperMultiplier");
        }

        public void SetPhysicsDefaults()
        {
            // Right/left angle target moves both breasts in the same direction
            BC.invertJoint2RotationY = false;
            // Soft physics on
            BPH.on = true;
            // Auto collider radius off
            BPH.softVerticesUseAutoColliderRadius = false;
            // Collider depth
            BPH.softVerticesColliderAdditionalNormalOffset = 0.001f;
        }

        public void UpdateMainPhysics(float breastMass, float softnessVal, float softnessMax)
        {
            // roughly estimate the legacy scale value from automatically calculated mass
            float scaleVal = (breastMass - 0.20f) * 1.60f;
            float hardnessVal = softnessMax - softnessVal; // 0.00 .. 2.50 for softness 3.00 .. 0.50

            //                                    base      size adjustment         softness adjustment
            BC.mass                             = breastMass;
            BC.spring                           = 34f    + (10f    * scaleVal) + (10f    * hardnessVal);
            BC.damper                           = 0.75f  + (0.66f  * scaleVal);
            BC.positionSpringZ                  = 250f   + (200f   * scaleVal);
            BC.positionDamperZ                  = 5f     + (3.0f   * scaleVal) + (3.0f   * softnessVal);
        }

        public void FullUpdate(
            float breastMass,
            float softnessVal,
            float softnessMax,
            float nippleErectionVal
        )
        {
            // roughly estimate the legacy scale value from automatically calculated mass
            float scaleVal = (breastMass - 0.20f) * 1.60f;
            float hardnessVal = softnessMax - softnessVal; // 0.00 .. 2.50 for softness 3.00 .. 0.50

            //                                    base      size adjustment         softness adjustment
            BC.mass                             = breastMass;
            BC.spring                           = 34f    + (10f    * scaleVal) + (10f    * hardnessVal);
            BC.damper                           = 0.75f  + (0.66f  * scaleVal);
            BC.positionSpringZ                  = 250f   + (200f   * scaleVal);
            BC.positionDamperZ                  = 5f     + (3.0f   * scaleVal) + (3.0f   * softnessVal);
            BPH.softVerticesColliderRadius      = 0.022f + (0.005f * scaleVal);
            BPH.softVerticesCombinedSpring      = 80f    + (60f    * scaleVal) + (45f    * softnessVal);
            BPH.softVerticesCombinedDamper      = 1.00f  + (1.20f  * scaleVal) + (0.30f  * softnessVal);
            BPH.softVerticesMass                = 0.08f  + (0.10f  * scaleVal);
            BPH.softVerticesBackForce           = 2.0f   + (5.0f   * scaleVal) + (4.0f   * hardnessVal);
            BPH.softVerticesBackForceMaxForce   = 2.0f   + (1.5f   * scaleVal) + (1.0f   * hardnessVal);
            BPH.softVerticesNormalLimit         = 0.010f + (0.010f * scaleVal) + (0.003f * softnessVal);

            groupASpringMultiplier.val  = (1.00f + (0.15f * softnessVal) - (0.10f * scaleVal)) / softnessVal;
            groupADamperMultiplier.val  = groupASpringMultiplier.val;
            groupBSpringMultiplier.val  = (1.80f + (0.20f * softnessVal) - (0.10f * scaleVal)) / softnessVal;
            groupBDamperMultiplier.val  = groupBSpringMultiplier.val;
            groupCSpringMultiplier.val  = (1.10f + (0.25f * softnessVal)) / softnessVal;
            groupCDamperMultiplier.val  = groupCSpringMultiplier.val;
            groupDSpringMultiplier.val  = nippleErectionVal + groupCSpringMultiplier.val;
            groupDDamperMultiplier.val  = nippleErectionVal + groupCSpringMultiplier.val;

            BPH.softVerticesBackForceThresholdDistance = (float) Calc.RoundToDecimals(
                0.0015f + (0.002f * scaleVal) - (0.001f * softnessVal),
                1000f
            );
        }

        public string GetStatus()
        {
            return (
                $"{Formatting.NameValueString("mass", BC.mass, padRight: 25)}\n" +
                $"{Formatting.NameValueString("center of g", BC.centerOfGravityPercent, padRight: 25)}\n" +
                $"{Formatting.NameValueString("spring", BC.spring, padRight: 25)}\n" +
                $"{Formatting.NameValueString("damper", BC.damper, padRight: 25)}\n" +
                $"{Formatting.NameValueString("in/out spr", BC.positionSpringZ, padRight: 25)}\n" +
                $"{Formatting.NameValueString("in/out dmp", BC.positionDamperZ, padRight: 25)}\n" +
                $"{Formatting.NameValueString("collider radius", BPH.softVerticesColliderRadius, padRight: 25)}\n" +
                $"{Formatting.NameValueString("back force", BPH.softVerticesBackForce, padRight: 25)}\n" +
                $"{Formatting.NameValueString("back force max", BPH.softVerticesBackForceMaxForce, padRight: 25)}\n" +
                $"{Formatting.NameValueString("back force thres", BPH.softVerticesBackForceThresholdDistance, padRight: 25)}\n" +
                $"{Formatting.NameValueString("fat spring", BPH.softVerticesCombinedSpring, padRight: 25)}\n" +
                $"{Formatting.NameValueString("fat damper", BPH.softVerticesCombinedDamper, padRight: 25)}\n" +
                $"{Formatting.NameValueString("fat mass", BPH.softVerticesMass, padRight: 25)}\n" +
                $"{Formatting.NameValueString("distance limit", BPH.softVerticesNormalLimit, padRight: 25)}\n" +
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
