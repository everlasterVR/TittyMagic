using System;
using UnityEngine;
using System.Collections.Generic;

namespace everlaster
{
    class RealBreast : MVRScript
    {
        const string pluginName = "RealBreast";
        const string pluginVersion = "1.0.0";
        private Transform chest;
        private AdjustJoints breastControl;
        private DAZPhysicsMesh breastPhysicsMesh;
        private DAZCharacterSelector geometry;

        private List<MorphConfig> sizeMorphs = new List<MorphConfig>();
        private List<GravityMorphConfig> gravityMorphs = new List<GravityMorphConfig>();

        //storables
        private float sizeMin = 0.3f;
        private float sizeMax = 3.0f;
        protected JSONStorableFloat softness;
        private float softnessMin = 0.5f;
        private float softnessMax = 3.0f;
        protected JSONStorableFloat size;

        // physics storables not directly accessible as attributes of DAZPhysicsMesh
        private JSONStorableFloat mainSpring;
        private JSONStorableFloat mainDamper;
        private JSONStorableFloat outerSpring;
        private JSONStorableFloat outerDamper;
        private JSONStorableFloat areolaSpring;
        private JSONStorableFloat areolaDamper;
        private JSONStorableFloat nippleSpring;
        private JSONStorableFloat nippleDamper;

        //Debug storables
        protected JSONStorableFloat sagMultiplier;
        protected JSONStorableString angleDebugInfo = new JSONStorableString("Angle Debug Info", "");
        protected JSONStorableString physicsDebugInfo = new JSONStorableString("Physics Debug Info", "");
        protected JSONStorableString morphDebugInfo = new JSONStorableString("Morph Debug Info", "");

        public override void Init()
        {
            try
            {
                if(containingAtom.type != "Person")
                {
                    SuperController.LogError($"Plugin is for use with 'Person' atom, not '{containingAtom.type}'");
                    return;
                }

                breastControl = containingAtom.GetStorableByID("BreastControl") as AdjustJoints;
                breastPhysicsMesh = containingAtom.GetStorableByID("BreastPhysicsMesh") as DAZPhysicsMesh;
                geometry = containingAtom.GetStorableByID("geometry") as DAZCharacterSelector;

                mainSpring = breastPhysicsMesh.GetFloatJSONParam("groupASpringMultiplier");
                mainDamper = breastPhysicsMesh.GetFloatJSONParam("groupADamperMultiplier");
                outerSpring = breastPhysicsMesh.GetFloatJSONParam("groupBSpringMultiplier");
                outerDamper = breastPhysicsMesh.GetFloatJSONParam("groupBDamperMultiplier");
                areolaSpring = breastPhysicsMesh.GetFloatJSONParam("groupCSpringMultiplier");
                areolaDamper = breastPhysicsMesh.GetFloatJSONParam("groupCDamperMultiplier");
                nippleSpring = breastPhysicsMesh.GetFloatJSONParam("groupDSpringMultiplier");
                nippleDamper = breastPhysicsMesh.GetFloatJSONParam("groupDDamperMultiplier");

                GlobalVar.MORPH_UI = geometry.morphsControlUI;
                chest = containingAtom.GetStorableByID("chest").transform;
                SetBreastPhysicsDefaults();

                CreateVersionInfoField();
                InitPluginUI();
                InitListeners();

                InitSizeMorphs();
                InitGravityMorphs();
                SetAllGravityMorphsToZero();
            }
            catch(Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

        void CreateVersionInfoField()
        {
            JSONStorableString jsonString = new JSONStorableString("VersionInfo", $"{pluginName} {pluginVersion}");
            UIDynamicTextField textField = CreateTextField(jsonString, false);
            textField.UItext.fontSize = 40;
            textField.height = 100;
        }

        void InitPluginUI()
        {
            size = CreateFloatSlider("Breast size", 1f, sizeMin, sizeMax);
            softness = CreateFloatSlider("Breast softness", 1.5f, softnessMin, softnessMax);;
            sagMultiplier = CreateFloatSlider("Sag multiplier", 1f, 0f, 1.5f);

            //DebugInfo fields
            UIDynamicTextField angleInfo = CreateTextField(angleDebugInfo, false);
            angleInfo.height = 100;
            UIDynamicTextField physicsInfo = CreateTextField(physicsDebugInfo, false);
            physicsInfo.height = 565;
            physicsInfo.UItext.fontSize = 26;
            UIDynamicTextField morphInfo = CreateTextField(morphDebugInfo, true);
            morphInfo.height = 1200;
            morphInfo.UItext.fontSize = 26;
        }

        JSONStorableFloat CreateFloatSlider(string paramName, float startingValue, float minValue, float maxValue)
        {
            JSONStorableFloat storable = new JSONStorableFloat(paramName, startingValue, minValue, maxValue);
            storable.storeType = JSONStorableParam.StoreType.Physical;
            RegisterFloat(storable);
            CreateSlider(storable, false);
            return storable;
        }

        void InitListeners()
        {
            size.slider.onValueChanged.AddListener((float val) =>
            {
                UpdateBreastPhysicsSettings(val, softness.val);
            });
            softness.slider.onValueChanged.AddListener((float val) =>
            {
                UpdateBreastPhysicsSettings(size.val, val);
            });
        }

        // TODO Zero all BuiltIn breast morphs
        void InitSizeMorphs()
        {
            sizeMorphs.AddRange(new List<MorphConfig>
            {
                //              morph                       base
                new MorphConfig("Armpit Curve",             -0.100f),
                new MorphConfig("Breast Height",             0.500f),
                new MorphConfig("Breast Diameter",           0.350f),
                new MorphConfig("Breast Large",              0.350f),
                new MorphConfig("Breasts Implants",          0.200f),
                new MorphConfig("Breasts Natural",          -0.150f),
            });
        }

        // Probably need to be able to weight the softness effects by size
        void InitGravityMorphs()
        {
            // TODO lock left/right and up/down etc. control pose morphs
            // TODO test size/shape morph so it can be zeroed when leaning forward/back -> compensate with neutral size morph
            //      -> shape sliders that only adjust shape, not size
            gravityMorphs.AddRange(new List<GravityMorphConfig>
            {
                //    USAGE: AdjustMorphs function
                //    angle type                            base       softness   size
                new GravityMorphConfig("Areola UpDown", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    { -0.33f,     1.80f,     0.20f } },
                }),
                new GravityMorphConfig("Center Gap Depth (Pose)", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.05f,     1.00f,     1.00f } },
                }),
                new GravityMorphConfig("Center Gap Height (Pose)", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.10f,     1.00f,     1.00f } },
                }),
                new GravityMorphConfig("Center Gap UpDown (Pose)", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.10f,     1.00f,     1.00f } },
                }),
                new GravityMorphConfig("Chest Smoother (Pose)", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.15f,     1.00f,     1.00f } },
                }),
                new GravityMorphConfig("ChestUnderBreast (Pose)", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.05f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("ChestUp (Pose)", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.04f,     1.75f,     0.25f } },
                }),
                new GravityMorphConfig("Breast Diameter (Pose)", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.25f,     1.33f,     0.67f } },
                }),
                new GravityMorphConfig("Breast flat (Pose)", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.10f,     1.25f,     0.75f } },
                }),
                new GravityMorphConfig("Breast Height (Pose)", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.10f,     2.00f,     null } },
                }),
                new GravityMorphConfig("Breast Move Up Left", new Dictionary<string, float?[]> {
                    { Types.UPRIGHT, new float?[]        { -0.15f,     0.67f,     1.33f } },
                    { Types.UPSIDE_DOWN, new float?[]    {  0.15f,     1.40f,     0.60f } },
                }),
                new GravityMorphConfig("Breast Move Up Right", new Dictionary<string, float?[]> {
                    { Types.UPRIGHT, new float?[]        { -0.15f,     0.67f,     1.33f } },
                    { Types.UPSIDE_DOWN, new float?[]    {  0.15f,     1.40f,     0.60f } },
                }),
                new GravityMorphConfig("Breast Pointed (Pose)", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.25f,    -0.25f,     1.00f } },
                }),
                new GravityMorphConfig("Breast Rotate Up Left", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.10f,     1.90f,     0.10f } },
                }),
                new GravityMorphConfig("Breast Rotate Up Right", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.10f,     1.90f,     0.10f } },
                }),
                new GravityMorphConfig("Breast Sag1 (Pose)", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    { -0.03f,     1.67f,     0.33f } },
                }),
                new GravityMorphConfig("Breast Sag2 (Pose)", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    { -0.10f,     1.67f,     0.33f } },
                }),
                new GravityMorphConfig("Breast Top Curve1 (Pose)", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    { -0.02f,     1.60f,     0.40f } },
                }),
                new GravityMorphConfig("Breast Top Curve2 (Pose)", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    { -0.03f,     1.60f,     0.40f } },
                }),
                new GravityMorphConfig("Breast Under Smoother1 (Pose)", new Dictionary<string, float?[]> {
                    { Types.UPRIGHT, new float?[]        { -0.04f,     0.50f,     1.50f } },
                    { Types.UPSIDE_DOWN, new float?[]    {  0.05f,     1.00f,     1.00f } },
                }),
                new GravityMorphConfig("Breast Under Smoother3 (Pose)", new Dictionary<string, float?[]> {
                    { Types.UPRIGHT, new float?[]        { -0.15f,     0.50f,     1.50f } },
                    { Types.UPSIDE_DOWN, new float?[]    {  0.10f,     1.00f,     1.00f } },
                }),
                new GravityMorphConfig("Breasts Flatten", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.10f,     1.25f,     0.75f } },
                }),
                new GravityMorphConfig("Breasts Hang Forward", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.33f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("Breasts Height (Copy)", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.10f,     2.00f,     0.00f } },
                }),
                new GravityMorphConfig("Breasts Implants (Pose)", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    { -0.10f,     1.33f,     0.66f } },
                }),
                new GravityMorphConfig("Breasts Natural (Pose)", new Dictionary<string, float?[]> {
                    { Types.UPRIGHT, new float?[]        {  0.30f,     2.00f,     0.00f } },
                }),
                new GravityMorphConfig("Breasts TogetherApart", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  1.00f,     null,     -0.50f } },
                }),
                new GravityMorphConfig("Breasts Upward Slope (Pose)", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.75f,     1.60f,     0.40f } },
                }),
                new GravityMorphConfig("BreastsShape2 (Pose)", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.50f,     0.33f,     1.67f } },
                }),
                new GravityMorphConfig("Sternum Height (Pose)", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    { -0.30f,     null,     null } },
                }),
            });

            // LEAN_BACK and LEAN_FORWARD morphs
            gravityMorphs.AddRange(new List<GravityMorphConfig>
            {
                //    USAGE: AdjustMorphs function
                //    angle type                            base       softness   size
                new GravityMorphConfig("Breast Diameter (Pose, Copy)", new Dictionary<string, float?[]> {
                    { Types.LEAN_BACK, new float?[]      {  0.08f,     1.50f,     0.50f } },
                    { Types.LEAN_FORWARD, new float?[]   { -0.08f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("Breast Large (Pose)", new Dictionary<string, float?[]> {
                    { Types.LEAN_BACK, new float?[]      { -0.08f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("Breast Side Smoother (Pose)", new Dictionary<string, float?[]> {
                    { Types.LEAN_FORWARD, new float?[]   {  0.20f,     1.80f,     0.20f } },
                    { Types.LEAN_BACK, new float?[]      { -0.33f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("Breast Under Smoother1 (Pose, Copy)", new Dictionary<string, float?[]> {
                    { Types.LEAN_BACK, new float?[]      { -0.04f,     1.00f,     1.00f } },
                }),
                new GravityMorphConfig("Breast Under Smoother3 (Pose, Copy)", new Dictionary<string, float?[]> {
                    { Types.LEAN_BACK, new float?[]      { -0.10f,     1.00f,     1.00f } },
                }),
                new GravityMorphConfig("Breasts Depth", new Dictionary<string, float?[]> {
                    { Types.LEAN_FORWARD, new float?[]   {  0.15f,     1.50f,     0.50f } },
                    { Types.LEAN_BACK, new float?[]      { -0.20f,     2.00f,     0.00f } },
                }),
                new GravityMorphConfig("Breasts Flatten (Copy)", new Dictionary<string, float?[]> {
                    { Types.LEAN_BACK, new float?[]      {  0.40f,     1.67f,     0.33f } },
                }),
                new GravityMorphConfig("Breasts Height", new Dictionary<string, float?[]> {
                    { Types.LEAN_FORWARD, new float?[]   { -0.16f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("Breasts Hang Forward (Copy)", new Dictionary<string, float?[]> {
                    { Types.LEAN_FORWARD, new float?[]   {  0.03f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("Breasts Move S2S Op", new Dictionary<string, float?[]> {
                    { Types.LEAN_BACK, new float?[]      { -0.08f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("Breasts TogetherApart (Copy)", new Dictionary<string, float?[]> {
                    { Types.LEAN_FORWARD, new float?[]   {  0.15f,     1.25f,     0.75f  } },
                }),
                new GravityMorphConfig("Chest Smoother (Pose, Copy)", new Dictionary<string, float?[]> {
                    { Types.LEAN_BACK, new float?[]      {  0.33f,     2.00f,     0.00f } },
                }),
                new GravityMorphConfig("ChestShape", new Dictionary<string, float?[]> {
                    { Types.LEAN_BACK, new float?[]      { -0.20f,     1.00f,     1.00f } },
                }),
                new GravityMorphConfig("ChestSmoothCenter", new Dictionary<string, float?[]> {
                    { Types.LEAN_BACK, new float?[]      {  0.15f,     1.67f,     0.33f } },
                }),
                new GravityMorphConfig("ChestUp", new Dictionary<string, float?[]> {
                    { Types.LEAN_BACK, new float?[]      {  0.20f,     1.00f,     1.00f } },
                }),
                new GravityMorphConfig("Sternum Width", new Dictionary<string, float?[]> { // TODO make pose
                    { Types.LEAN_FORWARD, new float?[]   {  0.33f,     1.75f,     0.25f  } },
                    { Types.LEAN_BACK, new float?[]      {  0.33f,    -0.67f,     1.33f } },
                }),
            });

            // ROLL_LEFT and ROLL_RIGHT morphs
            gravityMorphs.AddRange(new List<GravityMorphConfig>
            {
                //    USAGE: AdjustMorphs function
                //    angle type                            base       softness   size
                new GravityMorphConfig("Areola S2S Left (Copy)", new Dictionary<string, float?[]> {
                    { Types.ROLL_LEFT, new float?[]      { -0.60f,     1.83f,     0.17f } },
                    { Types.ROLL_RIGHT, new float?[]     {  0.60f,     1.83f,     0.17f } },
                }),
                new GravityMorphConfig("Areola S2S Right (Copy)", new Dictionary<string, float?[]> {
                    { Types.ROLL_LEFT, new float?[]      {  0.60f,     1.83f,     0.17f } },
                    { Types.ROLL_RIGHT, new float?[]     { -0.60f,     1.83f,     0.17f } },
                }),
                new GravityMorphConfig("Breasts Shift S2S Left (Copy)", new Dictionary<string, float?[]> {
                    { Types.ROLL_LEFT, new float?[]      {  0.25f,     1.67f,     0.33f } },
                }),
                new GravityMorphConfig("Breasts Shift S2S Right (Copy)", new Dictionary<string, float?[]> {
                    { Types.ROLL_RIGHT, new float?[]     {  0.25f,     1.67f,     0.33f } },
                }),
                new GravityMorphConfig("Breast Move S2S In Left (Copy)", new Dictionary<string, float?[]> {
                    { Types.ROLL_RIGHT, new float?[]     {  0.02f,     2.00f,     0.00f } },
                }),
                new GravityMorphConfig("Breast Move S2S In Right (Copy)", new Dictionary<string, float?[]> {
                    { Types.ROLL_LEFT, new float?[]      {  0.02f,     2.00f,     0.00f } },
                }),
                new GravityMorphConfig("Breast Move S2S Out Left (Copy)", new Dictionary<string, float?[]> {
                    { Types.ROLL_LEFT, new float?[]      {  0.06f,     2.00f,     0.00f } },
                }),
                new GravityMorphConfig("Breast Move S2S Out Right (Copy)", new Dictionary<string, float?[]> {
                    { Types.ROLL_RIGHT, new float?[]     {  0.06f,     2.00f,     0.00f } },
                }),
                new GravityMorphConfig("Breast Rotate X In Left", new Dictionary<string, float?[]> {
                    { Types.ROLL_RIGHT, new float?[]     {  0.10f,     2.00f,     0.00f } },
                }),
                new GravityMorphConfig("Breast Rotate X In Right", new Dictionary<string, float?[]> {
                    { Types.ROLL_LEFT, new float?[]      {  0.10f,     2.00f,     0.00f } },
                }),
                new GravityMorphConfig("Breasts Diameter (Pose)", new Dictionary<string, float?[]> {
                    { Types.ROLL_LEFT, new float?[]      { -0.06f,     2.00f,     0.00f } },
                    { Types.ROLL_RIGHT, new float?[]     { -0.06f,     2.00f,     0.00f } },
                }),
                new GravityMorphConfig("Breast Width Left (Copy)", new Dictionary<string, float?[]> {
                    { Types.ROLL_LEFT, new float?[]      { -0.20f,     1.75f,     0.25f } },
                    { Types.ROLL_RIGHT, new float?[]     {  0.12f,     1.75f,     0.25f } },
                }),
                new GravityMorphConfig("Breast Width Right (Copy)", new Dictionary<string, float?[]> {
                    { Types.ROLL_LEFT, new float?[]      {  0.12f,     1.75f,     0.25f } },
                    { Types.ROLL_RIGHT, new float?[]     { -0.20f,     1.75f,     0.25f } },
                }),
                new GravityMorphConfig("Centre Gap Narrow (Pose)", new Dictionary<string, float?[]> {
                    { Types.ROLL_LEFT, new float?[]      {  0.25f,     1.75f,     0.25f } },
                    { Types.ROLL_RIGHT, new float?[]     {  0.25f,     1.75f,     0.25f } },
                }),
                new GravityMorphConfig("Center Gap Smooth (Pose)", new Dictionary<string, float?[]> {
                    { Types.ROLL_LEFT, new float?[]      {  0.30f,     1.75f,     0.25f } },
                    { Types.ROLL_RIGHT, new float?[]     {  0.30f,     1.75f,     0.25f } },
                }),
            });
        }

        void SetBreastPhysicsDefaults()
        {
            geometry.useAuxBreastColliders = true;

            // Right/left angle target
            breastControl.targetRotationY = 0f;
            breastControl.targetRotationZ = 0f;
            // Soft physics on
            breastPhysicsMesh.on = true;
            breastPhysicsMesh.softVerticesUseAutoColliderRadius = true;
            breastPhysicsMesh.softVerticesBackForceThresholdDistance = 0.002f;
            breastPhysicsMesh.softVerticesColliderAdditionalNormalOffset = 0.002f;
        }

        // TODO update targetRotationX based on up/down
        // TODO update targetRotationY and Z based on forward/back
        // Completely experimental equations... 
        void UpdateBreastPhysicsSettings(float sizeVal, float softnessVal)
        {
            float sizeFactor = sizeVal - sizeMin;
            float softnessFactor = ((softnessVal - softnessMin) / (softnessMax - softnessMin)); // TODO use this more, or use sliders with values 0...1
            float sizeFactor2 = ((sizeVal - sizeMin) / (sizeMax - sizeMin)); // TODO use this more, or use sliders with values 0...1
            //                                                 base      size adjustment         softness adjustment

            breastControl.mass                              =  0.10f  + (0.71f  * sizeFactor);
            breastControl.centerOfGravityPercent            =  0.30f  + (0.05f  * sizeFactor);
            breastControl.spring                            =  50f    + (35f    * sizeFactor2) + (15f + (1f - sizeFactor2) * 35f) * (1f - softnessFactor); // kinda hacky
            breastControl.damper                            =  2.50f  - (0.33f  * sizeFactor);
            breastControl.targetRotationX                   =  8.00f  - (1.67f  * sizeFactor) - (1.67f  * softnessVal);
            breastControl.positionSpringZ                   =  500f   + (167f   * sizeFactor);
            breastControl.positionDamperZ                   =  4f     + (1.33f  * sizeFactor);
            breastPhysicsMesh.softVerticesCombinedSpring    =  160f   + (45f    * sizeFactor);
            breastPhysicsMesh.softVerticesCombinedDamper    =  0.013f * breastPhysicsMesh.softVerticesCombinedSpring;
            breastPhysicsMesh.softVerticesMass              =  0.08f  + (0.08f  * sizeFactor);
            breastPhysicsMesh.softVerticesBackForce         =  10.00f + (4.5f   * sizeFactor) - (2.5f   * softnessVal);
            breastPhysicsMesh.softVerticesBackForceMaxForce =  6.67f  + (3.0f   * sizeFactor) - (1.67f  * softnessVal);
            breastPhysicsMesh.softVerticesNormalLimit       =  0.004f + (0.007f * sizeFactor) + (0.008f * softnessVal);

            mainSpring.val      = (0.67f + (0.10f * softnessVal)) / (1.25f * softnessVal);
            mainDamper.val      = (0.67f + (0.10f * softnessVal)) / (1.25f * softnessVal);
            outerSpring.val     = (0.67f + (0.10f * softnessVal) + (0.10f * sizeFactor)) / (1.25f * softnessVal);
            outerDamper.val     = (0.67f + (0.10f * softnessVal) + (0.05f * sizeFactor)) / (1.25f * softnessVal);
            areolaSpring.val    = (0.75f + (0.10f * softnessVal)) / (1.00f * softnessVal);
            areolaDamper.val    = (0.75f + (0.10f * softnessVal)) / (1.00f * softnessVal);
            nippleSpring.val    = (0.85f + (0.10f * softnessVal)) / (1.00f * softnessVal);
            nippleDamper.val    = (0.85f + (0.10f * softnessVal)) / (1.00f * softnessVal);
        }

        public void Update()
        {
            AdjustMorphsForSize();

            Quaternion q = chest.rotation;
            float roll = Mathf.Rad2Deg * Mathf.Asin(2 * q.x * q.y + 2 * q.z * q.w);
            float pitch = Mathf.Rad2Deg * Mathf.Atan2(2 * q.x * q.w - 2 * q.y * q.z, 1 - 2 * q.x * q.x - 2 * q.z * q.z);

            AdjustMorphsForRoll(roll);

            // Scale pitch effect by roll angle's distance from 90/-90 = person is sideways
            //-> if person is sideways, pitch related morphs have less effect
            AdjustMorphsForPitch(pitch, (90 - Mathf.Abs(roll)) / 90);

            SetAngleDebugInfo(pitch, roll);
            SetPhysicsDebugInfo();
            SetMorphDebugInfo();
        }

        void AdjustMorphsForSize()
        {
            foreach(var it in sizeMorphs)
            {
                // 0.9f is the ratio (sizeMax - sizeMin)/sizeMax
                // this sets morphs to 0 with the sizeMin of 0.3, and to 3 with the sizeMax of 3
                // this makes sense because the breasts still actually have some size when the morphs are at 0
                it.Morph.morphValue = it.BaseMulti * (size.val - sizeMin) / 0.9f;
            }
        }

        void AdjustMorphsForRoll(float roll, float rollFactor = 1f)
        {
            // left
            if(roll >= 0)
            {
                SetGravityMorphsToZero(Types.ROLL_RIGHT);
                AdjustMorphs(Types.ROLL_LEFT, Remap(roll, rollFactor));
            }
            // right
            else
            {
                SetGravityMorphsToZero(Types.ROLL_LEFT);
                AdjustMorphs(Types.ROLL_RIGHT, Remap(Mathf.Abs(roll), rollFactor));
            }
        }

        void AdjustMorphsForPitch(float pitch, float rollFactor)
        {
            // leaning forward
            if(pitch > 0)
            {
                SetGravityMorphsToZero(Types.LEAN_BACK);
                // upright
                if(pitch <= 90)
                {
                    SetGravityMorphsToZero(Types.UPSIDE_DOWN);
                    AdjustMorphs(Types.LEAN_FORWARD, Remap(pitch, rollFactor));
                    AdjustMorphs(Types.UPRIGHT, Remap(90 - pitch, rollFactor));
                }
                // upside down
                else
                {
                    SetGravityMorphsToZero(Types.UPRIGHT);
                    AdjustMorphs(Types.LEAN_FORWARD, Remap(180 - pitch, rollFactor));
                    AdjustMorphs(Types.UPSIDE_DOWN, Remap(pitch - 90, rollFactor));
                }
            }
            // leaning back
            else
            {
                SetGravityMorphsToZero(Types.LEAN_FORWARD);
                // upright
                if(pitch > -90)
                {
                    SetGravityMorphsToZero(Types.UPSIDE_DOWN);
                    AdjustMorphs(Types.LEAN_BACK, Remap(Mathf.Abs(pitch), rollFactor));
                    AdjustMorphs(Types.UPRIGHT, Remap(90 - Mathf.Abs(pitch), rollFactor));
                }
                // upside down
                else
                {
                    SetGravityMorphsToZero(Types.UPRIGHT);
                    AdjustMorphs(Types.LEAN_BACK, Remap(180 - Mathf.Abs(pitch), rollFactor));
                    AdjustMorphs(Types.UPSIDE_DOWN, Remap(Mathf.Abs(pitch) - 90, rollFactor));
                }
            }
        }

        void SetGravityMorphsToZero(string type)
        {
            foreach(var it in gravityMorphs)
            {
                if(it.Multipliers.ContainsKey(type) && it.Multipliers.Count == 1)
                {
                    it.Morph.morphValue = 0;
                }
            }
        }

        void SetAllGravityMorphsToZero()
        {
            foreach(var it in gravityMorphs)
            {
                it.Morph.morphValue = 0;
            }
        }

        void AdjustMorphs(string type, float effect)
        {
            foreach(var it in gravityMorphs)
            {
                if(it.Multipliers.ContainsKey(type))
                {
                    float?[] m = it.Multipliers[type];
                    // m[0] is the base multiplier for the morph in this type (UPRIGHT etc.)
                    // m[1] scales the breast softness slider for this base multiplier
                    //      - if null, slider setting is ignored
                    // m[2] scales the size calibration slider for this base multiplier
                    //      - if null, slider setting is ignored
                    float softnessFactor = m[1].HasValue ? (float) m[1] * softness.val : 1;
                    float sizeFactor = m[2].HasValue ? (float) m[2] * size.val : 1;
                    it.Morph.morphValue = sagMultiplier.val * (float) m[0] * (
                        (softnessFactor * effect / 2) +
                        (sizeFactor * effect / 2)
                    );
                }
            }
        }

        float Remap(float angle, float effect)
        {
            return angle * effect / 90;
        }

        void OnDestroy()
        {
            SetAllGravityMorphsToZero();
        }

        void SetAngleDebugInfo(float pitch, float roll)
        {
            angleDebugInfo.SetVal($"Pitch: {pitch}\r\nRoll: {roll}");
        }

        void SetPhysicsDebugInfo()
        {
            string text = "";
            text += $"mass: {breastControl.mass}\r\n";
            text += $"center of g: {breastControl.centerOfGravityPercent}\r\n";
            text += $"spring: {breastControl.spring}\r\n";
            text += $"damper: {breastControl.damper}\r\n";
            text += $"in/out spr: {breastControl.positionSpringZ}\r\n";
            text += $"in/out dmp: {breastControl.positionDamperZ}\r\n";
            text += $"up/down target: {breastControl.targetRotationX}\r\n";

            text += $"back force: {breastPhysicsMesh.softVerticesBackForce}\r\n";
            text += $"fat spring: {breastPhysicsMesh.softVerticesCombinedSpring}\r\n";
            text += $"fat damper: {breastPhysicsMesh.softVerticesCombinedDamper}\r\n";
            text += $"fat mass: {breastPhysicsMesh.softVerticesMass}\r\n";
            text += $"distance limit: {breastPhysicsMesh.softVerticesNormalLimit}\r\n";
            text += $"main spring: {mainSpring.val}\r\n";
            text += $"main damper: {mainDamper.val}\r\n";
            text += $"outer spring: {outerSpring.val}\r\n";
            text += $"outer damper: {outerDamper.val}\r\n";
            text += $"areola spring: {areolaSpring.val}\r\n";
            text += $"areola damper: {areolaDamper.val}\r\n";
            text += $"nipple spring: {nippleSpring.val}\r\n";
            text += $"nipple damper: {nippleDamper.val}\r\n";
            physicsDebugInfo.SetVal(text);
        }

        void SetMorphDebugInfo()
        {
            string spacer = "======================\r\n";
            string text = "";
            text += $"{spacer}  SIZE MORPHS\r\n{spacer}";
            foreach(var it in sizeMorphs) text += it.ToString();
            text += $"{spacer}  GRAVITY MORPHS\r\n{spacer}";
            foreach(var it in gravityMorphs) text += it.ToString();
            morphDebugInfo.SetVal(text);
        }
    }

    public static class GlobalVar
    {
        public static GenerateDAZMorphsControlUI MORPH_UI { get; set; }
    }

    public static class Types
    {
        public const string LEAN_FORWARD = "leanForward";
        public const string LEAN_BACK = "leanBack";
        public const string UPSIDE_DOWN = "upsideDown";
        public const string ROLL_RIGHT = "rollRight";
        public const string ROLL_LEFT = "rollLeft";
        public const string UPRIGHT = "upright";
    }

    class MorphConfig
    {
        public string Name { get; set; }
        public DAZMorph Morph { get; set; }
        public float BaseMulti { get; set; }

        public MorphConfig(string name, float baseMulti)
        {
            Name = name;
            Morph = GlobalVar.MORPH_UI.GetMorphByDisplayName(name);
            BaseMulti = baseMulti;
        }

        override
        public string ToString()
        {
            float value = (float) Math.Round(this.Morph.morphValue * 1000f) / 1000f;
            return this.Name + ":  " + value + "\r\n";
        }
    }

    class GravityMorphConfig
    {
        public string Name { get; set; }
        public DAZMorph Morph { get; set; }
        public Dictionary<string, float?[]> Multipliers { get; set; }

        public GravityMorphConfig(string name, Dictionary<string, float?[]> multipliers)
        {
            Name = name;
            Morph = GlobalVar.MORPH_UI.GetMorphByDisplayName(name);
            Multipliers = multipliers;
        }

        override
        public string ToString()
        {
            float value = (float) Math.Round(this.Morph.morphValue * 1000f) / 1000f;
            return this.Name + ":  " + value + "\r\n";
        }
    }
}
