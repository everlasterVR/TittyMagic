//#define DEBUGINFO
using System;
using UnityEngine;
using System.Collections.Generic;

namespace everlaster
{
    class TittyMagic : MVRScript
    {
        const string pluginName = "TittyMagic";
        const string pluginVersion = "1.1.0";

        private bool enableUpdate;
        private bool enableDebug = false;
        private Transform chest;
        private AdjustJoints breastControl;
        private DAZPhysicsMesh breastPhysicsMesh;
        private DAZCharacterSelector geometry;

        private List<MorphConfig> sizeMorphs = new List<MorphConfig>();
        private List<MorphConfig> example1Morphs = new List<MorphConfig>();
        private List<MorphConfig> example2Morphs = new List<MorphConfig>();
        private List<MorphConfig> example3Morphs = new List<MorphConfig>();
        private List<MorphConfig> example4Morphs = new List<MorphConfig>();
        private List<GravityMorphConfig> gravityMorphs = new List<GravityMorphConfig>();

        //storables
        private float scaleMin = 0.3f;
        private float scaleDefault = 0.8f;
        private float scaleMax = 3.0f;
        protected JSONStorableFloat softness;
        private float softnessMin = 0.5f;
        private float softnessDefault = 1.5f;
        private float softnessMax = 3.0f;
        protected JSONStorableFloat scale;
        private float sagDefault = 1.2f;
        protected JSONStorableFloat sagMultiplier;
        protected JSONStorableString logInfo;
        
        // physics storables not directly accessible as attributes of DAZPhysicsMesh
        private JSONStorableFloat mainSpring;
        private JSONStorableFloat mainDamper;
        private JSONStorableFloat outerSpring;
        private JSONStorableFloat outerDamper;
        private JSONStorableFloat areolaSpring;
        private JSONStorableFloat areolaDamper;
        private JSONStorableFloat nippleSpring;
        private JSONStorableFloat nippleDamper;

#if DEBUGINFO
        protected JSONStorableString angleDebugInfo = new JSONStorableString("Angle Debug Info", "");
        protected JSONStorableString physicsDebugInfo = new JSONStorableString("Physics Debug Info", "");
        protected JSONStorableString morphDebugInfo = new JSONStorableString("Morph Debug Info", "");
#endif

        public override void Init()
        {
            try
            {
                if(containingAtom.type != "Person")
                {
                    SuperController.LogError($"Plugin is for use with 'Person' atom, not '{containingAtom.type}'");
                    return;
                }

                GlobalVar.UPDATE_ENABLED = true;

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

                InitPluginUILeft();
                InitPluginUIRight();
                InitListeners();

                InitSizeMorphs();
                InitExampleMorphs();
                InitGravityMorphs();
                SetAllGravityMorphsToZero();
                UpdateBreastPhysicsSettings(scale.val, softness.val);

                enableUpdate = GlobalVar.UPDATE_ENABLED;
            }
            catch(Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

        void InitPluginUILeft()
        {
            JSONStorableString versionH1 = NewTextField("Version Info", 40);
            versionH1.SetVal($"{pluginName} {pluginVersion}");

            scale = NewFloatSlider("Breast scale", scaleDefault, scaleMin, scaleMax);
            softness = NewFloatSlider("Breast softness", softnessDefault, softnessMin, softnessMax);

            CreateNewSpacer(10f);

            sagMultiplier = NewFloatSlider("Sag multiplier", sagDefault, 0f, 2.0f);

#if DEBUGINFO
            UIDynamicTextField angleInfo = CreateTextField(angleDebugInfo, false);
            angleInfo.height = 100;
            angleInfo.UItext.fontSize = 26;
            UIDynamicTextField physicsInfo = CreateTextField(physicsDebugInfo, false);
            physicsInfo.height = 540;
            physicsInfo.UItext.fontSize = 26;
#else
            CreateNewSpacer(10f);

            JSONStorableString presetsH2 = NewTextField("Example Settings", 34);
            presetsH2.SetVal("\nExample settings");

            CreateExampleButtons();
#endif

        }

        void InitPluginUIRight()
        {
#if DEBUGINFO
            UIDynamicTextField morphInfo = CreateTextField(morphDebugInfo, true);
            morphInfo.height = 1200;
            morphInfo.UItext.fontSize = 26;
#else
            JSONStorableString usageInfo = NewTextField("Usage Info Area", 28, 530, true);
            string usage = "\n";
            usage += "Breast scale applies size morphs and anchors them to " +
                "size related physics settings. For best results, breast morphs " +
                "should be tweaked manually only after setting the scale amount." +
                "(See the examples below.)\n\n";
            usage += "Breast softness controls soft physics and affects the amount " +
                "of morph-based sag in different orientations or poses.\n\n";
            usage += "Sag multiplier adjusts the sag produced by Breast softness " +
                "independently of soft physics.";
            usageInfo.SetVal(usage);

            CreateNewSpacer(10f, true);

            JSONStorableString presetsInfo = NewTextField("Example Settings", 28, 100, true);
            presetsInfo.SetVal("\nSet breast morphs to defaults before applying example settings.");

            logInfo = NewTextField("Log Info Area", 28, 515, true);
            logInfo.SetVal("\n");
#endif
        }

        JSONStorableFloat NewFloatSlider(string paramName, float startingValue, float minValue, float maxValue)
        {
            JSONStorableFloat storable = new JSONStorableFloat(paramName, startingValue, minValue, maxValue);
            storable.storeType = JSONStorableParam.StoreType.Physical;
            RegisterFloat(storable);
            CreateSlider(storable, false);
            return storable;
        }

        JSONStorableString NewTextField(string paramName, int fontSize, int height = 100,  bool rightSide = false)
        {
            JSONStorableString storable = new JSONStorableString(paramName, "");
            UIDynamicTextField textField = CreateTextField(storable, rightSide);
            textField.UItext.fontSize = fontSize;
            textField.height = height;
            return storable;
        }

        void CreateExampleButtons()
        {
            UIDynamicButton example1 = CreateButton("Pornstar big naturals");
            example1.button.onClick.AddListener(() =>
            {
                scale.val = 1.65f;
                softness.val = 2.10f;
                sagMultiplier.val = 1.60f;

                ApplyMorphTweaks(example1Morphs);
                string text = "> Pornstar big naturals morph tweaks:\n";
                foreach(var it in example1Morphs) text = text + it.ToString() + "\n";
                logInfo.SetVal("\n" + text + "\n" + logInfo.val);
            });

            UIDynamicButton example2 = CreateButton("Small and perky");
            example2.button.onClick.AddListener(() =>
            {
                scale.val = 0.30f;
                softness.val = 1.10f;
                sagMultiplier.val = 1.80f;

                ApplyMorphTweaks(example2Morphs);
                string text = "> Small and perky morph tweaks:\n";
                foreach(var it in example2Morphs) text = text + it.ToString() + "\n";
                logInfo.SetVal("\n" + text + "\n" + logInfo.val);
            });

            UIDynamicButton example3 = CreateButton("Medium implants");
            example3.button.onClick.AddListener(() =>
            {
                scale.val = 0.75f;
                softness.val = 0.60f;
                sagMultiplier.val = 0.80f;

                ApplyMorphTweaks(example3Morphs);
                string text = "> Medium implants morph tweaks:\n";
                foreach(var it in example3Morphs) text = text + it.ToString() + "\n";
                logInfo.SetVal("\n" + text + "\n" + logInfo.val);
            });

            UIDynamicButton example4 = CreateButton("Huge and soft");
            example4.button.onClick.AddListener(() =>
            {
                scale.val = 3.00f;
                softness.val = 2.80f;
                sagMultiplier.val = 2.00f;

                ApplyMorphTweaks(example4Morphs);
                string text = "> Huge and soft morph tweaks:\n";
                foreach(var it in example4Morphs) text = text + it.ToString() + "\n";
                logInfo.SetVal("\n" + text + "\n" + logInfo.val);
            });

            CreateNewSpacer(10f);

            UIDynamicButton defaults = CreateButton("Undo example settings");
            defaults.button.onClick.AddListener(() =>
            {
                scale.val = scaleDefault;
                softness.val = softnessDefault;
                sagMultiplier.val = sagDefault;
                
                UndoMorphTweaks();
                string text = "> Example tweaks zeroed and sliders reset.";
                logInfo.SetVal("\n" + text + "\n" + logInfo.val);
            });
        }

        void CreateNewSpacer(float height, bool rightSide = false)
        {
            UIDynamic spacer = CreateSpacer(rightSide);
            spacer.height = height;
        }

        //JSONStorableBool NewToggle(string paramName)
        //{
        //    JSONStorableBool storable = new JSONStorableBool(paramName, true);
        //    CreateToggle(storable, false);
        //    RegisterBool(storable);
        //    return storable;
        //}

        void InitListeners()
        {
            scale.slider.onValueChanged.AddListener((float val) =>
            {
                UpdateBreastPhysicsSettings(val, softness.val);
            });
            softness.slider.onValueChanged.AddListener((float val) =>
            {
                UpdateBreastPhysicsSettings(scale.val, val);
            });
            // reset physics settings in case changed with button
            sagMultiplier.slider.onValueChanged.AddListener((float val) =>
            {
                UpdateBreastPhysicsSettings(scale.val, softness.val);
            });
        }

        void InitSizeMorphs()
        {
            sizeMorphs.AddRange(new List<MorphConfig>
            {
                //               morph                       base        start
                new MorphConfig("TM_Baseline",               1.000f),
                //new MorphConfig("Armpit Curve",             -0.100f),
                //new MorphConfig("Breast Diameter",           0.250f),
                //new MorphConfig("Breast Centered",           0.150f),
                //new MorphConfig("Breast Height",             0.250f),
                //new MorphConfig("Breast Large",              0.350f),
                //new MorphConfig("Breasts Natural",          -0.050f),
                //new MorphConfig("BreastsCrease",            -0.250f),
                //new MorphConfig("BreastsShape1",             0.150f),
                //new MorphConfig("BreastsShape2",            -0.050f),
                //new MorphConfig("ChestSeparateBreasts",     -0.025f),

                new MorphConfig("TM_Baseline_Fixer",         0.000f,     1.000f),
                //new MorphConfig("Breast Top Curve1",         0.033f,    -0.033f),
                //new MorphConfig("Breast Top Curve2",         0.250f,    -0.750f),
                //new MorphConfig("Breasts Implants",          0.150f,     0.075f),
                //new MorphConfig("Breasts Size",              0.050f,    -0.050f),
            });
        }

        void InitExampleMorphs()
        {
            example1Morphs.AddRange(new List<MorphConfig>
            {
                //               morph                       value
                new MorphConfig("Breast Height Upper",       0.175f),
                new MorphConfig("Breast Pointed",            0.100f),
                new MorphConfig("Breast Round",             -0.500f),
                new MorphConfig("Breast Top Curve2",        -0.175f),
                new MorphConfig("Breast Zero",               0.100f),
                new MorphConfig("Breasts Natural Left",     -0.150f),
                new MorphConfig("Breasts Natural Right",     0.075f),
                new MorphConfig("Breasts Size",              0.050f),
                new MorphConfig("BreastsShape2",            -0.175f),
                new MorphConfig("Nipple Diameter",          -0.500f),
                new MorphConfig("Nipple Size",              -0.500f),
            });
            example2Morphs.AddRange(new List<MorphConfig>
            {
                new MorphConfig("Areolae Perk",              0.600f),
                new MorphConfig("Areola Size",              -0.400f),
                new MorphConfig("Breast diameter",          -0.050f),
                new MorphConfig("Breast Sag1",               0.100f),
                new MorphConfig("Breast Sag2",               0.150f),
                new MorphConfig("Breast Pointed",            0.300f),
                new MorphConfig("Breast Round",             -0.300f),
                new MorphConfig("Breasts Cleavage",          0.150f),
                new MorphConfig("Breasts Natural Left",      0.150f),
                new MorphConfig("Breasts Natural Right",     0.190f),
                new MorphConfig("Breasts Implants Left",    -0.025f),
                new MorphConfig("Breasts Implants Right",   -0.025f),
                new MorphConfig("Breasts Small",             0.150f),
                new MorphConfig("Breasts Under Curve",       0.150f),
                new MorphConfig("Nipple Diameter",          -0.600f),
                new MorphConfig("Nipple Length",            -0.300f),
                new MorphConfig("Nipple Size",              -0.100f),
                new MorphConfig("Sternum Width",             0.100f),
            });
            example3Morphs.AddRange(new List<MorphConfig>
            {
                new MorphConfig("Breast diameter",           0.500f),
                new MorphConfig("Breast Round",             -0.250f),
                new MorphConfig("Breast Under Smoother3",   -0.350f),
                new MorphConfig("Breasts Cleavage",          0.500f),
                new MorphConfig("Breasts Implants Left",     0.200f),
                new MorphConfig("Breasts Implants Right",    0.180f),
                new MorphConfig("Breasts Natural Left",     -0.150f),
                new MorphConfig("Breasts Natural Right",    -0.125f),
                new MorphConfig("Breasts Perk Side",         0.300f),
                new MorphConfig("Breasts Size",             -0.350f),
                new MorphConfig("Chest Smoother",           -0.500f),
                new MorphConfig("Nipple Size",              -0.500f),
                new MorphConfig("Nipple Length",            -0.350f),
                new MorphConfig("Nipples Size",              0.150f),
                new MorphConfig("Sternum Width",             0.750f),
            });
            example4Morphs.AddRange(new List<MorphConfig>
            {
                new MorphConfig("Areola Size",               0.500f),
                new MorphConfig("Areola Size X",            -0.250f),
                new MorphConfig("Areola Size Y",             0.650f),
                new MorphConfig("Areola Puffy Edge",         0.500f),
                new MorphConfig("Areolae Diameter",          0.250f),
                new MorphConfig("Areolae Perk",              0.500f),
                new MorphConfig("Breasts Cleavage",         -0.100f),
                new MorphConfig("Breasts Gone",             -0.100f),
                new MorphConfig("Breasts Perk Side",         0.450f),
                new MorphConfig("Breasts Size",             -0.100f),
                new MorphConfig("BreastsShape3",            -0.300f),
                new MorphConfig("Nipple Diameter",          -0.500f),
                new MorphConfig("Nipples Large",            -0.650f),
                new MorphConfig("ChestUnderBreast",          0.250f),
                new MorphConfig("Sternum Width",             0.250f),
            });
        }

        void InitGravityMorphs()
        {
            gravityMorphs.AddRange(new List<GravityMorphConfig>
            {
                //    USAGE: AdjustMorphs function
                //    angle type                            base       softness   scale
                //    Main sag morphs
                new GravityMorphConfig("TM_Breast Move Up", new Dictionary<string, float?[]> {
                    { Types.UPRIGHT, new float?[]        { -0.07f,     1.67f,     0.33f } },
                    { Types.UPSIDE_DOWN, new float?[]    {  0.07f,     1.67f,     0.33f } },
                }),
                new GravityMorphConfig("TM_Breast Sag1", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    { -0.03f,     1.25f,     0.75f } },
                }),
                new GravityMorphConfig("TM_Breast Sag2", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    { -0.05f,     1.25f,     0.75f } },
                }),
                new GravityMorphConfig("TM_Breasts Hang Forward", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.05f,     1.50f,     0.80f } },
                }),
                new GravityMorphConfig("TM_Breasts Natural", new Dictionary<string, float?[]> {
                    { Types.UPRIGHT, new float?[]        {  0.08f,     2.00f,     0.00f } },
                    { Types.UPSIDE_DOWN, new float?[]    { -0.04f,     2.00f,     0.00f } },
                }),
                new GravityMorphConfig("TM_Breasts TogetherApart", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.10f,     1.50f,     0.80f } },
                }),

                //    Tweak morphs
                new GravityMorphConfig("TM_Areola UpDown", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    { -0.15f,     1.33f,     0.67f } },
                }),
                new GravityMorphConfig("TM_Center Gap Depth", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.05f,     0.50f,     1.50f } },
                }),
                new GravityMorphConfig("TM_Center Gap Height", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.10f,     0.50f,     1.50f } },
                }),
                new GravityMorphConfig("TM_Center Gap UpDown", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.10f,     0.50f,     1.50f } },
                }),
                new GravityMorphConfig("TM_Chest Smoother", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.10f,     0.75f,     1.25f } },
                }),
                new GravityMorphConfig("TM_ChestUnderBreast", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.15f,     1.75f,     0.25f } },
                }),
                new GravityMorphConfig("TM_ChestUp", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.05f,     1.75f,     0.25f } },
                }),
                new GravityMorphConfig("TM_ChestUpperNarrow", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.10f,     1.75f,     0.25f } },
                }),
                new GravityMorphConfig("TM_Breast Diameter", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.05f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast flat", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.08f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Height", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.10f,     2.00f,     0.00f } },
                }),
                new GravityMorphConfig("TM_Breast Pointed", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.33f,     0.00f,     1.00f } },
                }),
                new GravityMorphConfig("TM_Breast Rotate Up", new Dictionary<string, float?[]> {
                    { Types.UPRIGHT, new float?[]        {  0.15f,     0.80f,     1.20f } },
                    { Types.UPSIDE_DOWN, new float?[]    {  0.25f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Top Curve1", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    { -0.04f,     2.00f,    -0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Top Curve2", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    { -0.06f,     2.00f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Under Smoother1", new Dictionary<string, float?[]> {
                    { Types.UPRIGHT, new float?[]        { -0.04f,     0.50f,     1.50f } },
                    { Types.UPSIDE_DOWN, new float?[]    {  0.45f,     0.60f,     1.40f } },
                }),
                new GravityMorphConfig("TM_Breast Under Smoother3", new Dictionary<string, float?[]> {
                    { Types.UPRIGHT, new float?[]        { -0.08f,     1.00f,     1.00f } },
                    { Types.UPSIDE_DOWN, new float?[]    {  0.20f,     1.00f,    -1.00f } },
                }),
                new GravityMorphConfig("TM_Breasts Flatten", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.10f,     1.40f,     0.60f } },
                }),
                new GravityMorphConfig("TM_Breasts Height", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.10f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breasts Implants", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    { -0.05f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breasts Upward Slope", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.15f,     1.20f,     0.80f } },
                }),
                new GravityMorphConfig("TM_BreastsShape2", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    {  0.50f,     0.67f,     1.33f } },
                }),
                new GravityMorphConfig("TM_Sternum Height", new Dictionary<string, float?[]> {
                    { Types.UPSIDE_DOWN, new float?[]    { -0.30f,     null,     null } },
                }),
            });

            gravityMorphs.AddRange(new List<GravityMorphConfig>
            {
                //    USAGE: AdjustMorphs function
                //    angle type                            base       softness   scale
                new GravityMorphConfig("TM_Breast Depth Left", new Dictionary<string, float?[]> {
                    { Types.LEAN_FORWARD, new float?[]   {  0.22f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Depth Right", new Dictionary<string, float?[]> {
                    { Types.LEAN_FORWARD, new float?[]   {  0.22f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Depth Squash Left", new Dictionary<string, float?[]> {
                    { Types.LEAN_BACK, new float?[]      { -0.20f,     2.00f,     0.00f } },
                }),
                new GravityMorphConfig("TM_Breast Depth Squash Right", new Dictionary<string, float?[]> {
                    { Types.LEAN_BACK, new float?[]      { -0.20f,     2.00f,     0.00f } },
                }),
                new GravityMorphConfig("TM_Breast Diameter Left", new Dictionary<string, float?[]> {
                    { Types.LEAN_FORWARD, new float?[]   {  0.22f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Diameter Right", new Dictionary<string, float?[]> {
                    { Types.LEAN_FORWARD, new float?[]   {  0.22f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Diameter (Copy)", new Dictionary<string, float?[]> {
                    { Types.LEAN_BACK, new float?[]      {  0.08f,     1.50f,     0.50f } },
                    { Types.LEAN_FORWARD, new float?[]   { -0.04f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Large", new Dictionary<string, float?[]> {
                    { Types.LEAN_BACK, new float?[]      { -0.08f,     1.50f,     0.50f } },
                    { Types.LEAN_FORWARD, new float?[]   { -0.08f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Side Smoother", new Dictionary<string, float?[]> {
                    { Types.LEAN_FORWARD, new float?[]   {  0.20f,     1.80f,     0.20f } },
                    { Types.LEAN_BACK, new float?[]      { -0.33f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Under Smoother1 (Copy)", new Dictionary<string, float?[]> {
                    { Types.LEAN_BACK, new float?[]      { -0.04f,     1.00f,     1.00f } },
                }),
                new GravityMorphConfig("TM_Breast Under Smoother3 (Copy)", new Dictionary<string, float?[]> {
                    { Types.LEAN_BACK, new float?[]      { -0.10f,     1.00f,     1.00f } },
                }),
                new GravityMorphConfig("TM_Breast Move S2S Out Left", new Dictionary<string, float?[]> {
                    { Types.LEAN_BACK, new float?[]      {  0.08f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Move S2S Out Right", new Dictionary<string, float?[]> {
                    { Types.LEAN_BACK, new float?[]      {  0.08f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breasts Flatten (Copy)", new Dictionary<string, float?[]> {
                    { Types.LEAN_BACK, new float?[]      {  0.25f,     1.67f,     0.33f } },
                }),
                new GravityMorphConfig("TM_Breasts Height (Copy)", new Dictionary<string, float?[]> {
                    { Types.LEAN_FORWARD, new float?[]   { -0.18f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breasts Hang Forward (Copy)", new Dictionary<string, float?[]> {
                    { Types.LEAN_FORWARD, new float?[]   {  0.05f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breasts TogetherApart (Copy)", new Dictionary<string, float?[]> {
                    { Types.LEAN_FORWARD, new float?[]   {  0.20f,     1.75f,     0.25f  } },
                }),
                new GravityMorphConfig("TM_Chest Smoother (Copy)", new Dictionary<string, float?[]> {
                    { Types.LEAN_BACK, new float?[]      {  0.33f,     2.00f,     0.00f } },
                }),
                new GravityMorphConfig("TM_ChestShape", new Dictionary<string, float?[]> {
                    { Types.LEAN_BACK, new float?[]      { -0.20f,     1.00f,     1.00f } },
                }),
                new GravityMorphConfig("TM_ChestSmoothCenter", new Dictionary<string, float?[]> {
                    { Types.LEAN_BACK, new float?[]      {  0.15f,     1.67f,     0.33f } },
                }),
                new GravityMorphConfig("TM_ChestUp (Copy)", new Dictionary<string, float?[]> {
                    { Types.LEAN_BACK, new float?[]      {  0.20f,     1.00f,     1.00f } },
                }),
                new GravityMorphConfig("TM_Sternum Width", new Dictionary<string, float?[]> {
                    { Types.LEAN_FORWARD, new float?[]   {  0.25f,     1.25f,     0.75f  } },
                    { Types.LEAN_BACK, new float?[]      {  0.33f,    -0.67f,     1.33f } },
                }),
            });

            gravityMorphs.AddRange(new List<GravityMorphConfig>
            {
                //    USAGE: AdjustMorphs function
                //    angle type                            base       softness   scale
                new GravityMorphConfig("TM_Areola S2S Left", new Dictionary<string, float?[]> {
                    { Types.ROLL_LEFT, new float?[]      { -0.40f,     1.50f,     0.50f } },
                    { Types.ROLL_RIGHT, new float?[]     {  0.40f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Areola S2S Right", new Dictionary<string, float?[]> {
                    { Types.ROLL_LEFT, new float?[]      {  0.40f,     1.50f,     0.50f } },
                    { Types.ROLL_RIGHT, new float?[]     { -0.40f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Move S2S In Left", new Dictionary<string, float?[]> {
                    { Types.ROLL_RIGHT, new float?[]     {  0.28f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Move S2S In Right", new Dictionary<string, float?[]> {
                    { Types.ROLL_LEFT, new float?[]      {  0.28f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Move S2S Out Left (Copy)", new Dictionary<string, float?[]> {
                    { Types.ROLL_LEFT, new float?[]      {  0.40f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Move S2S Out Right (Copy)", new Dictionary<string, float?[]> {
                    { Types.ROLL_RIGHT, new float?[]     {  0.40f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Rotate X In Left", new Dictionary<string, float?[]> {
                    { Types.ROLL_RIGHT, new float?[]     {  0.10f,     2.00f,     0.00f } },
                }),
                new GravityMorphConfig("TM_Breast Rotate X In Right", new Dictionary<string, float?[]> {
                    { Types.ROLL_LEFT, new float?[]      {  0.10f,     2.00f,     0.00f } },
                }),
                new GravityMorphConfig("TM_Breast Width Left", new Dictionary<string, float?[]> {
                    { Types.ROLL_LEFT, new float?[]      { -0.03f,     1.60f,     0.40f } },
                    { Types.ROLL_RIGHT, new float?[]     {  0.07f,     1.60f,     0.40f } },
                }),
                new GravityMorphConfig("TM_Breast Width Right", new Dictionary<string, float?[]> {
                    { Types.ROLL_LEFT, new float?[]      {  0.07f,     1.60f,     0.40f } },
                    { Types.ROLL_RIGHT, new float?[]     { -0.03f,     1.60f,     0.40f } },
                }),
                new GravityMorphConfig("TM_Breasts Diameter", new Dictionary<string, float?[]> {
                    { Types.ROLL_LEFT, new float?[]      { -0.05f,     1.60f,     0.40f } },
                    { Types.ROLL_RIGHT, new float?[]     { -0.05f,     1.60f,     0.40f } },
                }),
                new GravityMorphConfig("TM_Centre Gap Narrow", new Dictionary<string, float?[]> {
                    { Types.ROLL_LEFT, new float?[]      {  0.10f,     1.75f,     0.25f } },
                    { Types.ROLL_RIGHT, new float?[]     {  0.10f,     1.75f,     0.25f } },
                }),
                new GravityMorphConfig("TM_Center Gap Smooth", new Dictionary<string, float?[]> {
                    { Types.ROLL_LEFT, new float?[]      {  0.20f,     1.75f,     0.25f } },
                    { Types.ROLL_RIGHT, new float?[]     {  0.20f,     1.75f,     0.25f } },
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
        
        void UpdateBreastPhysicsSettings(float sizeVal, float softnessVal)
        {
            float scaleFactor = sizeVal - scaleMin;
            float softnessFactor = ((softnessVal - softnessMin) / (softnessMax - softnessMin)); // TODO use this more, or use sliders with values 0...1
            float scaleFactor2 = ((sizeVal - scaleMin) / (scaleMax - scaleMin));

            //                                                 base      size adjustment         softness adjustment
            breastControl.mass                              =  0.10f  + (0.71f  * scaleFactor);
            breastControl.centerOfGravityPercent            =  0.45f  + (0.04f  * scaleFactor) + (0.04f  * softnessVal);
            breastControl.spring                            =  60f    + (28f    * scaleFactor2) + (12f + (1f - scaleFactor2) * 28f) * (1f - softnessFactor); // kinda hacky
            breastControl.damper                            =  2.10f  - (0.25f  * scaleFactor);
            breastControl.targetRotationX                   =  8.00f  - (1.67f  * scaleFactor) - (1.67f  * softnessVal);
            breastControl.positionSpringZ                   =  250f   + (200f   * scaleFactor);
            breastControl.positionDamperZ                   =  5f     + (3.0f   * scaleFactor) + (3.0f   * softnessVal);
            breastPhysicsMesh.softVerticesCombinedSpring    =  120f   + (80f    * scaleFactor);
            breastPhysicsMesh.softVerticesCombinedDamper    =  0.010f * breastPhysicsMesh.softVerticesCombinedSpring;
            breastPhysicsMesh.softVerticesMass              =  0.09f  + (0.10f  * scaleFactor);
            breastPhysicsMesh.softVerticesBackForce         =  10.0f  + (6.0f   * scaleFactor) - (2f     * softnessVal);
            breastPhysicsMesh.softVerticesBackForceMaxForce =  7.5f   + (4.5f   * scaleFactor) - (1.5f   * softnessVal);
            breastPhysicsMesh.softVerticesNormalLimit       =  0.008f + (0.006f * scaleFactor) + (0.006f * softnessVal);

            mainSpring.val      = (0.67f + (0.10f * softnessVal)) / (1.00f * softnessVal);
            mainDamper.val      = (0.67f + (0.10f * softnessVal)) / (1.00f * softnessVal);
            outerSpring.val     = (0.67f + (0.10f * softnessVal)) / (1.00f * softnessVal);
            outerDamper.val     = (0.67f + (0.10f * softnessVal)) / (1.00f * softnessVal);
            areolaSpring.val    = (0.75f + (0.10f * softnessVal)) / (1.00f * softnessVal);
            areolaDamper.val    = (0.75f + (0.10f * softnessVal)) / (1.00f * softnessVal);
            nippleSpring.val    = (0.85f + (0.10f * softnessVal)) / (1.00f * softnessVal);
            nippleDamper.val    = (0.85f + (0.10f * softnessVal)) / (1.00f * softnessVal);
        }

        public void Update()
        {
            try
            {
                if (enableUpdate)
                {
                    AdjustMorphsForSize();

                    Quaternion q = chest.rotation;
                    float roll = Mathf.Rad2Deg * Mathf.Asin(2 * q.x * q.y + 2 * q.z * q.w);
                    float pitch = Mathf.Rad2Deg * Mathf.Atan2(2 * q.x * q.w - 2 * q.y * q.z, 1 - 2 * q.x * q.x - 2 * q.z * q.z);

                    AdjustMorphsForRoll(roll);

                    // Scale pitch effect by roll angle's distance from 90/-90 = person is sideways
                    //-> if person is sideways, pitch related morphs have less effect
                    AdjustMorphsForPitch(pitch, (90 - Mathf.Abs(roll)) / 90);

#if DEBUGINFO
                    SetAngleDebugInfo(pitch, roll);
                    SetPhysicsDebugInfo();
                    SetMorphDebugInfo();
#endif
                }
            }
            catch(Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
                GlobalVar.UPDATE_ENABLED = false;
                enableUpdate = GlobalVar.UPDATE_ENABLED;
            }
        }

        void AdjustMorphsForSize()
        {
            foreach(var it in sizeMorphs)
            {
                // 0.9f is the ratio (sizeMax - sizeMin)/sizeMax
                // this sets morphs to 0 with the sizeMin of 0.3, and to 3 with the sizeMax of 3
                // this makes sense because the breasts still actually have some size when the morphs are at 0
                it.Morph.morphValue = it.StartValue + it.BaseMulti * (scale.val - scaleMin) / 0.9f;
            }
        }

        void AdjustMorphsForRoll(float roll, float rollFactor = 1f)
        {
            // left
            if(roll >= 0)
            {
                SetGravityMorphsToZero(Types.ROLL_RIGHT);
                DoAdjustMorphsForRoll(Types.ROLL_LEFT, Remap(roll, rollFactor));
            }
            // right
            else
            {
                SetGravityMorphsToZero(Types.ROLL_LEFT);
                DoAdjustMorphsForRoll(Types.ROLL_RIGHT, Remap(Mathf.Abs(roll), rollFactor));
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
                    DoAdjustMorphs(Types.LEAN_FORWARD, Remap(pitch, rollFactor));
                    DoAdjustMorphs(Types.UPRIGHT, Remap(90 - pitch, rollFactor));
                }
                // upside down
                else
                {
                    SetGravityMorphsToZero(Types.UPRIGHT);
                    DoAdjustMorphs(Types.LEAN_FORWARD, Remap(180 - pitch, rollFactor));
                    DoAdjustMorphs(Types.UPSIDE_DOWN, Remap(pitch - 90, rollFactor));
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
                    DoAdjustMorphs(Types.LEAN_BACK, Remap(Mathf.Abs(pitch), rollFactor));
                    DoAdjustMorphs(Types.UPRIGHT, Remap(90 - Mathf.Abs(pitch), rollFactor));
                }
                // upside down
                else
                {
                    SetGravityMorphsToZero(Types.UPRIGHT);
                    DoAdjustMorphs(Types.LEAN_BACK, Remap(180 - Mathf.Abs(pitch), rollFactor));
                    DoAdjustMorphs(Types.UPSIDE_DOWN, Remap(Mathf.Abs(pitch) - 90, rollFactor));
                }
            }
        }

        float Remap(float angle, float effect)
        {
            return angle * effect / 90;
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

        void SetMorphGroupToZero(List<MorphConfig> morphs)
        {
            foreach(var it in morphs)
            {
                it.Morph.morphValue = 0;
            }
        }

        void ApplyMorphTweaks(List<MorphConfig> morphs)
        {
            UndoMorphTweaks();
            foreach(var it in morphs)
            {
                it.Morph.morphValue = it.BaseMulti;
            }
        }

        void UndoMorphTweaks()
        {
            SetMorphGroupToZero(example1Morphs);
            SetMorphGroupToZero(example2Morphs);
            SetMorphGroupToZero(example3Morphs);
            SetMorphGroupToZero(example4Morphs);
        }

        void DoAdjustMorphs(string type, float effect)
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
                    float scaleFactor = m[2].HasValue ? (float) m[2] * scale.val : 1;
                    
                    float morphValue = sagMultiplier.val * (float) m[0] * (
                        (softnessFactor * effect / 2) +
                        (scaleFactor * effect / 2)
                    );
                    if (morphValue > 0)
                    {
                        it.Morph.morphValue = morphValue >= 1.33f ? 1.33f : morphValue;
                    }
                    else
                    {
                        it.Morph.morphValue = morphValue < -1.33f ? -1.33f : morphValue;
                    }
                }
            }
        }

        void DoAdjustMorphsForRoll(string type, float effect)
        {
            foreach(var it in gravityMorphs)
            {
                if(it.Multipliers.ContainsKey(type))
                {
                    float?[] m = it.Multipliers[type];

                    float softnessFactor = m[1].HasValue ? (float) m[1] * softness.val : 1;
                    float scaleFactor = m[2].HasValue ? (float) m[2] * scale.val : 1;
                    float sagMultiplierVal = sagMultiplier.val >= 1 ?
                        1 + (sagMultiplier.val - 1) / 2 :
                        sagMultiplier.val;
                    float morphValue = sagMultiplierVal * (float) m[0] * (
                        (softnessFactor * effect / 2) +
                        (scaleFactor * effect / 2)
                    );
                    if(morphValue > 0)
                    {
                        it.Morph.morphValue = morphValue >= 1.33f ? 1.33f : morphValue;
                    }
                    else
                    {
                        it.Morph.morphValue = morphValue < -1.33f ? -1.33f : morphValue;
                    }
                }
            }
        }

        void OnDestroy()
        {
            SetAllGravityMorphsToZero();
        }

#if DEBUGINFO
        void SetAngleDebugInfo(float pitch, float roll)
        {
            angleDebugInfo.SetVal($"Pitch: {pitch}\nRoll: {roll}");
        }

        void SetPhysicsDebugInfo()
        {
            string text = "";
            text += $"mass: {breastControl.mass}\n";
            text += $"center of g: {breastControl.centerOfGravityPercent}\n";
            text += $"spring: {breastControl.spring}\n";
            text += $"damper: {breastControl.damper}\n";
            text += $"in/out spr: {breastControl.positionSpringZ}\n";
            text += $"in/out dmp: {breastControl.positionDamperZ}\n";
            text += $"up/down target: {breastControl.targetRotationX}\n";

            text += $"back force: {breastPhysicsMesh.softVerticesBackForce}\n";
            text += $"fat spring: {breastPhysicsMesh.softVerticesCombinedSpring}\n";
            text += $"fat damper: {breastPhysicsMesh.softVerticesCombinedDamper}\n";
            text += $"fat mass: {breastPhysicsMesh.softVerticesMass}\n";
            text += $"distance limit: {breastPhysicsMesh.softVerticesNormalLimit}\n";
            text += $"main spring: {mainSpring.val}\n";
            text += $"main damper: {mainDamper.val}\n";
            text += $"outer spring: {outerSpring.val}\n";
            text += $"outer damper: {outerDamper.val}\n";
            text += $"areola spring: {areolaSpring.val}\n";
            text += $"areola damper: {areolaDamper.val}\n";
            text += $"nipple spring: {nippleSpring.val}\n";
            text += $"nipple damper: {nippleDamper.val}\n";
            physicsDebugInfo.SetVal(text);
        }

        void SetMorphDebugInfo()
        {
            string text = "";
            text += "SIZE MORPHS\n";
            foreach(var it in sizeMorphs) text = text + it.ToString() + "\n";
            text += "\nGRAVITY MORPHS\n";
            foreach(var it in gravityMorphs) text = text + it.ToString() + "\n";
            morphDebugInfo.SetVal(text);
        }
#endif
    }

    public static class GlobalVar
    {
        public static bool UPDATE_ENABLED { get; set; }
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
        public float StartValue { get; set; }

        public MorphConfig(string name, float baseMulti, float startValue = 0.00f)
        {
            Name = name;
            Morph = GlobalVar.MORPH_UI.GetMorphByDisplayName(name);
            BaseMulti = baseMulti;
            StartValue = startValue;
            if(Morph == null)
            {
                SuperController.LogError($"Morph with name {name} not found!");
            }
        }

        override
        public string ToString()
        {
            float value = (float) Math.Round(this.Morph.morphValue * 1000f) / 1000f;
            return this.Name + ":  " + value;
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
            if (Morph == null)
            {
                SuperController.LogError($"Morph with name {name} not found!");
            }
        }

        override
        public string ToString()
        {
            float value = (float) Math.Round(this.Morph.morphValue * 1000f) / 1000f;
            return this.Name + ":  " + value;
        }
    }
}
