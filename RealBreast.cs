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
        private DAZPhysicsMeshUI breastPhysicsMeshUI;

        private List<SizeConfig> sizeMorphs = new List<SizeConfig>();
        private List<GravityConfig> gravityMorphs = new List<GravityConfig>();

        //storables
        private float sizeMin = 0.3f;
        private float sizeMax = 3.0f;
        protected JSONStorableFloat softness;
        private float softnessMin = 0.3f;
        private float softnessMax = 3.0f;
        protected JSONStorableFloat size;

        //DebugInfo storables
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

                JSONStorable geometryStorable = containingAtom.GetStorableByID("geometry");
                DAZCharacterSelector geometry = geometryStorable as DAZCharacterSelector;
                JSONStorable breastControlStorable = containingAtom.GetStorableByID("BreastControl");
                breastControl = breastControlStorable as AdjustJoints;
                JSONStorable breastPhysicsMeshStorable = containingAtom.GetStorableByID("BreastPhysicsMesh");
                breastPhysicsMesh = breastPhysicsMeshStorable as DAZPhysicsMesh;
                breastPhysicsMeshUI = breastPhysicsMesh.UITransform.GetComponentInChildren<DAZPhysicsMeshUI>();

                GlobalVar.MORPH_UI = geometry.morphsControlUI;
                chest = containingAtom.GetStorableByID("chest").transform;
                SetBreastPhysicsDefaults(geometry);

                CreateVersionInfoField();
                InitPluginUI();
                InitListeners();

                InitSizeMorphs();
                InitGravityMorphs();
                SetAllGravityMorphsToZero();

                UpdateBreastPhysicsSettings(size.val, softness.val);
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

            //DebugInfo fields
            UIDynamicTextField angleInfo = CreateTextField(angleDebugInfo, false);
            angleInfo.height = 100;
            UIDynamicTextField physicsInfo = CreateTextField(physicsDebugInfo, false);
            physicsInfo.height = 800;
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
            sizeMorphs.AddRange(new List<SizeConfig>
            {
                //              morph                       base
                new SizeConfig("Armpit Curve",             -0.100f),
                new SizeConfig("Breast Diameter",           0.350f),
                new SizeConfig("Breast Large",              0.350f),
                new SizeConfig("Breasts Implants",          0.200f),
                //              pose morph                  base                      
                new SizeConfig("Breast Rotate Down Right", -0.250f),
                new SizeConfig("Breast Rotate Down Left",  -0.250f),
            });
        }

        void InitGravityMorphs()
        {
            // TODO lock left/right and up/down etc. control pose morphs
            // TODO test size/shape morph so it can be zeroed when leaning forward/back -> compensate with neutral size morph
            //      -> shape sliders that only adjust shape, not size
            // TODO fix softness increasing breast size noticeably when leaning back
            // TODO fix upside down softness deforming smaller breasts
            // TODO fix softness flattening breasts too much when upright (natural -> makes smaller)
            // TODO fix roll using same morphs as some other direction.. need to copy?
            // UPRIGHT and UPSIDE_DOWN adjustments for initial "Zero G" breast morphs
            gravityMorphs.AddRange(new List<GravityConfig>
            {
                //    USAGE: AdjustMorphs function
                //    angle type                            base       softness   size
                new GravityConfig("Breast Move Up Right", new Dictionary<string, float?[]> {
                    { Types.UPRIGHT, new float?[]        { -0.50f,     0.67f,     1.33f } },
                    { Types.UPSIDE_DOWN, new float?[]    {  0.15f,     1.80f,     0.20f } },
                }),
                new GravityConfig("Breast Move Up Left", new Dictionary<string, float?[]> {
                    { Types.UPRIGHT, new float?[]        { -0.50f,     0.67f,     1.33f } },
                    { Types.UPSIDE_DOWN, new float?[]    {  0.15f,     1.80f,     0.20f } },
                }),
                new GravityConfig("Breast Under Smoother1 (Pose)", new Dictionary<string, float?[]> {
                    { Types.UPRIGHT, new float?[]        { -0.07f,     0.67f,     1.33f } },
                    { Types.UPSIDE_DOWN, new float?[]    { -0.05f,     1.80f,     0.20f } },
                }),
                new GravityConfig("Breast Under Smoother3 (Pose)", new Dictionary<string, float?[]> {
                    { Types.UPRIGHT, new float?[]        { -0.35f,     0.67f,     1.33f } },
                    { Types.UPSIDE_DOWN, new float?[]    { -0.10f,     1.80f,     0.20f } },
                }),
                new GravityConfig("Breasts Natural (Pose)", new Dictionary<string, float?[]> {
                    { Types.UPRIGHT, new float?[]        {  0.50f,     2.00f,     0.00f } },
                }),
                new GravityConfig("Breast Diameter (Pose)", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.50f,     0.67f,     1.33f } },
                }),
                new GravityConfig("Breasts Implants (Pose)", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    { -0.30f,     0.67f,     1.33f } },
                }),
                // other UPSIDE_DOWN morphs
                new GravityConfig("Areola UpDown", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    { -1.00f,     1.80f,     0.20f } },
                }),
                new GravityConfig("Center Gap Depth (Pose)", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.10f,     null,      null } },
                }),
                new GravityConfig("Center Gap Height (Pose)", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.20f,     null,      null } },
                }),
                new GravityConfig("Center Gap UpDown (Pose)", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.20f,     null,      null } },
                }),
                new GravityConfig("Chest Smoother (Pose)", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.50f,     null,      null } },
                }),
                new GravityConfig("ChestUnderBreast (Pose)", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.10f,     1.80f,     0.20f } },
                }),
                new GravityConfig("ChestUp (Pose)", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.10f,     1.80f,     0.20f } },
                }),
                new GravityConfig("Breast Height (Pose)", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.10f,     2.00f,     0.00f } },
                }),
                new GravityConfig("Breast Pointed (Pose)", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.50f,     -0.50f,     1.50f } },
                }),
                new GravityConfig("Breast Rotate Up Left", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.10f,     1.80f,     0.20f } },
                }),
                new GravityConfig("Breast Rotate Up Right", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.10f,     1.80f,     0.20f } },
                }),
                new GravityConfig("Breast Sag1 (Pose)", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    { -0.15f,     1.80f,     0.20f } },
                }),
                new GravityConfig("Breast Sag2 (Pose)", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    { -0.50f,     1.80f,     0.20f } },
                }),
                new GravityConfig("Breast Top Curve1 (Pose)", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    { -0.50f,     1.80f,     0.20f } },
                }),
                new GravityConfig("Breast Top Curve2 (Pose)", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    { -0.50f,     1.80f,     0.20f } },
                }),
                new GravityConfig("Breasts Flatten", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.20f,     1.50f,     0.50f } },
                }),
                new GravityConfig("Breast flat (Pose)", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.20f,     1.50f,     0.50f } },
                }),
                new GravityConfig("Breasts Hang Forward", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.40f,     0.00f,     2.00f } },
                }),
                new GravityConfig("Breasts Height (Copy)", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.10f,     2.00f,     0.00f } },
                }),
                new GravityConfig("Breasts TogetherApart", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  1.00f,     null,     -0.50f } },
                }),
                new GravityConfig("Breasts Upward Slope (Pose)", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  1.00f,     1.33f,     0.67f } },
                }),
                new GravityConfig("BreastsShape2 (Pose)", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.75f,     0.20f,     1.80f } },
                }),
                new GravityConfig("Sternum Height (Pose)", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    { -0.30f,     null,     null } },
                }),
            });

            // LEAN_BACK and LEAN_FORWARD morphs
            gravityMorphs.AddRange(new List<GravityConfig>
            {
                new GravityConfig("Breast Diameter (Pose, Copy)", new Dictionary<string, float?[]> {
                    { Types.LEAN_BACK, new float?[]      {  0.20f,     1.67f,     0.33f } },
                    { Types.LEAN_FORWARD, new float?[]   { -0.20f,     1.50f,     0.50f } },
                }),
                new GravityConfig("Breasts Implants (Pose, Copy)", new Dictionary<string, float?[]> {
                    { Types.LEAN_BACK, new float?[]      { -0.25f,     1.67f,     0.33f } },
                }),
                //new MorphConfig("Breast Large (Pose)", new Dictionary<string, float?[]> {
                //    { Types.LEAN_BACK, new float?[]      {  0.25f,     1.67f,     0.33f } },
                //}),
                //new MorphConfig("Areola S2S Op", new Dictionary<string, float?[]> {
                //    { Types.LEAN_BACK, new float?[]      {  1.00f,     0.00f,     2.00f } },
                //}),
                new GravityConfig("Breast Side Smoother (Pose)", new Dictionary<string, float?[]> {
                    { Types.LEAN_FORWARD, new float?[]   {  0.20f,     1.80f,     0.20f } },
                    { Types.LEAN_BACK, new float?[]      { -0.40f,     0.33f,     1.67f } },
                }),
                new GravityConfig("Breasts Depth", new Dictionary<string, float?[]> {
                    { Types.LEAN_FORWARD, new float?[]   {  0.33f,     1.80f,     0.20f } },
                    { Types.LEAN_BACK, new float?[]      { -0.50f,     2.00f,     0.00f } },
                }),
                new GravityConfig("Breasts Flatten (Copy)", new Dictionary<string, float?[]> {
                    { Types.LEAN_BACK, new float?[]      {  0.67f,     1.50f,     0.50f } },
                }),
                new GravityConfig("Breasts Height", new Dictionary<string, float?[]> {
                    { Types.LEAN_FORWARD, new float?[]   { -0.16f,     1.80f,     0.20f } },
                }),
                new GravityConfig("Breasts Hang Forward (Copy)", new Dictionary<string, float?[]> {
                    { Types.LEAN_FORWARD, new float?[]   {  0.10f,     1.80f,     0.20f } },
                }),
                new GravityConfig("Breasts Move S2S Op", new Dictionary<string, float?[]> {
                    { Types.LEAN_BACK, new float?[]      { -0.45f,     0.50f,     1.50f } },
                }),
                new GravityConfig("Breasts TogetherApart (Copy)", new Dictionary<string, float?[]> {
                    { Types.LEAN_FORWARD, new float?[]   {  0.33f,     1.80f,     0.20f  } },
                }),
            });

            // ROLL_LEFT and ROLL_RIGHT morphs
            gravityMorphs.AddRange(new List<GravityConfig>
            {
                new GravityConfig("Areola S2S Left (Copy)", new Dictionary<string, float?[]> {
                    { Types.ROLL_LEFT, new float?[]      { -0.75f,     1.66f,     0.33f } },
                    { Types.ROLL_RIGHT, new float?[]     {  0.75f,     1.66f,     0.33f } },
                }),
                new GravityConfig("Areola S2S Right (Copy)", new Dictionary<string, float?[]> {
                    { Types.ROLL_LEFT, new float?[]      {  0.75f,     1.66f,     0.33f } },
                    { Types.ROLL_RIGHT, new float?[]     { -0.75f,     1.66f,     0.33f } },
                }),
                new GravityConfig("Breasts Shift S2S Left (Copy)", new Dictionary<string, float?[]> {
                    { Types.ROLL_LEFT, new float?[]      {  0.50f,     1.50f,     0.50f } },
                }),
                new GravityConfig("Breasts Shift S2S Right (Copy)", new Dictionary<string, float?[]> {
                    { Types.ROLL_RIGHT, new float?[]     {  0.50f,     1.50f,     0.50f } },
                }),
                new GravityConfig("Breast Move S2S In Left (Copy)", new Dictionary<string, float?[]> {
                    { Types.ROLL_RIGHT, new float?[]     {  0.05f,     1.50f,     0.50f } },
                }),
                new GravityConfig("Breast Move S2S In Right (Copy)", new Dictionary<string, float?[]> {
                    { Types.ROLL_LEFT, new float?[]      {  0.05f,     1.50f,     0.50f } },
                }),
                new GravityConfig("Breast Move S2S Out Left (Copy)", new Dictionary<string, float?[]> {
                    { Types.ROLL_LEFT, new float?[]      {  0.15f,     1.50f,     0.50f } },
                }),
                new GravityConfig("Breast Move S2S Out Right (Copy)", new Dictionary<string, float?[]> {
                    { Types.ROLL_RIGHT, new float?[]     {  0.15f,     1.50f,     0.50f } },
                }),
                new GravityConfig("Breast Rotate X In Left", new Dictionary<string, float?[]> {
                    { Types.ROLL_RIGHT, new float?[]     {  0.30f,     2.00f,     0.00f } },
                }),
                new GravityConfig("Breast Rotate X In Right", new Dictionary<string, float?[]> {
                    { Types.ROLL_LEFT, new float?[]      {  0.30f,     2.00f,     0.00f } },
                }),
                new GravityConfig("Breast Rotate X Out Left", new Dictionary<string, float?[]> {
                    { Types.ROLL_LEFT, new float?[]      {  0.30f,     2.00f,     0.00f } },
                }),
                new GravityConfig("Breast Rotate X Out Right", new Dictionary<string, float?[]> {
                    { Types.ROLL_RIGHT, new float?[]     {  0.30f,     2.00f,     0.00f } },
                }),
                //new MorphConfig("Breasts Diameter (Pose)", new Dictionary<string, float?[]> {
                //    { Types.ROLL_LEFT, new float?[]      {  0.25f,     2.00f,     0.00f } },
                //    { Types.ROLL_RIGHT, new float?[]     {  0.25f,     2.00f,     0.00f } },
                //}),
                new GravityConfig("Breasts Implants Left (Pose)", new Dictionary<string, float?[]> {
                    { Types.ROLL_LEFT, new float?[]      {  0.18f,     1.25f,    -0.75f } },
                    { Types.ROLL_RIGHT, new float?[]     {  0.12f,     1.50f,     0.50f } },
                }),
                new GravityConfig("Breasts Implants Right (Pose)", new Dictionary<string, float?[]> {
                    { Types.ROLL_LEFT, new float?[]      {  0.12f,     1.50f,     0.50f } },
                    { Types.ROLL_RIGHT, new float?[]     {  0.18f,     1.25f,    -0.75f } },
                }),
                new GravityConfig("Breast Width Left (Copy)", new Dictionary<string, float?[]> {
                    { Types.ROLL_LEFT, new float?[]      { -0.25f,     2.00f,     0.00f } },
                    { Types.ROLL_RIGHT, new float?[]     {  0.15f,     2.00f,     0.00f } },
                }),
                new GravityConfig("Breast Width Right (Copy)", new Dictionary<string, float?[]> {
                    { Types.ROLL_LEFT, new float?[]      {  0.15f,     2.00f,     0.00f } },
                    { Types.ROLL_RIGHT, new float?[]     { -0.25f,     2.00f,     0.00f } },
                }),
                new GravityConfig("Centre Gap Narrow (Pose)", new Dictionary<string, float?[]> {
                    { Types.ROLL_LEFT, new float?[]      {  0.25f,     1.80f,     0.20f } },
                    { Types.ROLL_RIGHT, new float?[]     {  0.25f,     1.80f,     0.20f } },
                }),
                new GravityConfig("Center Gap Smooth (Pose)", new Dictionary<string, float?[]> {
                    { Types.ROLL_LEFT, new float?[]      {  0.30f,     1.80f,     0.20f } },
                    { Types.ROLL_RIGHT, new float?[]     {  0.30f,     1.80f,     0.20f } },
                }),
            });
        }

        void SetBreastPhysicsDefaults(DAZCharacterSelector geometry)
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
            // Main spring/damper
            breastPhysicsMeshUI.groupASpringMultplierSlider.value = 1.00f;
            breastPhysicsMeshUI.groupADamperMultplierSlider.value = 1.00f;
            // Areola spring/damper
            breastPhysicsMeshUI.groupCSpringMultplierSlider.value = 1.00f;
            breastPhysicsMeshUI.groupCDamperMultplierSlider.value = 1.00f;
            // Nipple spring/damper
            breastPhysicsMeshUI.groupDSpringMultplierSlider.value = 1.00f;
            breastPhysicsMeshUI.groupDDamperMultplierSlider.value = 1.00f;
        }

        void UpdateBreastPhysicsSettings(float sizeVal, float softnessVal)
        {
            float sizeFactor = sizeVal - sizeMin;
            //                                                min     size adjustment        softness adjustment
            breastControl.mass                              = 0.10f  + (0.71f  * sizeFactor);
            breastControl.centerOfGravityPercent            = 0.50f  - (0.05f  * sizeFactor);
            breastControl.spring                            = 22.0f  + (15.0f  * sizeFactor);
            breastControl.damper                            = 1.20f  + (0.30f  * sizeFactor);
            breastControl.positionSpringZ                   = 175f   + (150f   * sizeFactor);
            breastControl.positionDamperZ                   = 50f    + (10f    * sizeFactor);
            breastControl.targetRotationX                   = 6.00f  - (2.00f  * sizeFactor);
            breastPhysicsMesh.softVerticesCombinedSpring    = 105f   + (55f    * sizeFactor);
            breastPhysicsMesh.softVerticesCombinedDamper    = 1.80f;
            breastPhysicsMesh.softVerticesMass              = 0.06f  + (0.11f  * sizeFactor);
            breastPhysicsMesh.softVerticesBackForce         = 5.0f   + (6.0f   * sizeFactor);
            breastPhysicsMesh.softVerticesBackForceMaxForce = 5.0f   + (2.0f   * sizeFactor);
            breastPhysicsMesh.softVerticesNormalLimit       = 0.015f + (0.025f * sizeFactor);

            // Outer spring/damper
            breastPhysicsMeshUI.groupBSpringMultplierSlider.value = 1.00f + (0.05f * sizeFactor);
            breastPhysicsMeshUI.groupBDamperMultplierSlider.value = 1.00f + (0.10f * sizeFactor);
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
                    it.Morph.morphValue = (float) m[0] * (
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
            text += $"outer spring: {breastPhysicsMeshUI.groupBSpringMultplierSlider.value}\r\n";
            text += $"outer damper: {breastPhysicsMeshUI.groupBDamperMultplierSlider.value}\r\n";
            physicsDebugInfo.SetVal(text);
        }

        void SetMorphDebugInfo()
        {
            string text = "";
            foreach(var it in sizeMorphs)
            {
                text += it.ToString();
            }
            foreach(var it in gravityMorphs)
            {
                text += it.ToString();
            }
            morphDebugInfo.SetVal(text);
        }
    }

    public static class GlobalVar
    {
        public static GenerateDAZMorphsControlUI MORPH_UI { get; set; }
    }

    public static class Types
    {
        public const string SIZE = "size";
        public const string LEAN_FORWARD = "leanForward";
        public const string LEAN_BACK = "leanBack";
        public const string UPSIDE_DOWN = "upsideDown";
        public const string ROLL_RIGHT = "rollRight";
        public const string ROLL_LEFT = "rollLeft";
        public const string UPRIGHT = "upright";
        public const string SAG_ADJUST = "sagAdjust";
    }

    class SizeConfig
    {
        public string Name { get; set; }
        public DAZMorph Morph { get; set; }
        public float BaseMulti { get; set; }

        public SizeConfig(string name, float baseMulti)
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

    class GravityConfig
    {
        public string Name { get; set; }
        public DAZMorph Morph { get; set; }
        public Dictionary<string, float?[]> Multipliers { get; set; }

        public GravityConfig(string name, Dictionary<string, float?[]> multipliers)
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
