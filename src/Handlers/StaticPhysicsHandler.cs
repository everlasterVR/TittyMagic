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
            // Soft physics on
            BPH.on = true;
            // Auto collider radius off
            BPH.softVerticesUseAutoColliderRadius = false;
            // Collider depth
            BPH.softVerticesColliderAdditionalNormalOffset = 0.001f;
        }

        public void UpdateMainPhysics(
            float breastMass,
            float softnessVal,
            float softnessMax
        )
        {
            float scaleVal = Calc.LegacyScale(breastMass);
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
            float scaleVal = Calc.LegacyScale(breastMass);
            float hardnessVal = softnessMax - softnessVal; // 0.00 .. 2.50 for softness 3.00 .. 0.50

            //                                    base      size adjustment         softness adjustment
            BC.mass                             = breastMass;
            BC.spring                           = 25f    + (4f      * scaleVal) + (12f      * hardnessVal);
            BC.damper                           = 0.8f   + (0.2f    * scaleVal) + (0.2f     * hardnessVal);
            BC.positionSpringZ                  = 250f   + (200f    * scaleVal);
            BC.positionDamperZ                  = 5f     + (3.0f    * scaleVal) + (3.0f     * softnessVal);
            BPH.softVerticesColliderRadius      = 0.022f + (0.005f  * scaleVal);
            BPH.softVerticesCombinedSpring      = 80f    + (80f     * scaleVal) + (40f      * softnessVal);
            BPH.softVerticesCombinedDamper      = 0.75f  + (0.75f   * scaleVal) + (0.25f    * softnessVal);
            BPH.softVerticesMass                = 0.04f  + (0.100f  * scaleVal) + (0.030f   * softnessVal);
            BPH.softVerticesBackForce           = 1.0f   + (2.8f    * scaleVal) + (2.0f     * hardnessVal);
            BPH.softVerticesBackForceMaxForce   = 1.0f   + (1.4f    * scaleVal) + (1.0f     * hardnessVal);
            BPH.softVerticesNormalLimit         = 0.010f + (0.015f  * scaleVal) + (0.003f   * softnessVal);

            groupASpringMultiplier.val  = (0.75f + (0.10f * softnessVal) - (0.07f * scaleVal)) / softnessVal;
            groupADamperMultiplier.val  = groupASpringMultiplier.val;
            groupBSpringMultiplier.val  = (1.50f + (0.20f * softnessVal) - (0.07f * scaleVal)) / softnessVal;
            groupBDamperMultiplier.val  = groupBSpringMultiplier.val;
            groupCSpringMultiplier.val  = (1.10f + (0.25f * softnessVal)) / softnessVal;
            groupCDamperMultiplier.val  = 2.00f * groupCSpringMultiplier.val;
            groupDSpringMultiplier.val  = nippleErectionVal + groupCSpringMultiplier.val;
            groupDDamperMultiplier.val  = 1.50f * nippleErectionVal + groupCSpringMultiplier.val;

            BPH.softVerticesBackForceThresholdDistance = Calc.RoundToDecimals(
                0.0015f + (0.0015f * scaleVal) - (0.0005f * softnessVal),
                1000f
            );
        }

        public string GetStatus()
        {
            return (
                $"{Formatting.NameValueString("mass", BC.mass, padRight: 25)}\n" +
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
