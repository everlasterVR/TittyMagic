//#define DEBUGINFO
using System;
using UnityEngine;
using System.Collections.Generic;

namespace everlaster
{
    class TittyMagic : MVRScript
    {
        private bool enableUpdate;
        private Transform chest;
        private AdjustJoints breastControl;
        private DAZPhysicsMesh breastPhysicsMesh;
        private DAZCharacterSelector geometry;

        private MorphConfig bodyScaleMorph;
        private List<MorphConfig> sizeMorphs = new List<MorphConfig>();
        private List<MorphConfig> nippleErectionMorphs = new List<MorphConfig>();
        private List<MorphConfig> example1Morphs = new List<MorphConfig>();
        private List<MorphConfig> example2Morphs = new List<MorphConfig>();
        private List<MorphConfig> example3Morphs = new List<MorphConfig>();
        private List<MorphConfig> example4Morphs = new List<MorphConfig>();
        private List<GravityMorphConfig> gravityMorphs = new List<GravityMorphConfig>();

        bool atomScaleListenerIsSet = false;

        //storables
        private JSONStorableFloat atomScale;
        private float atomScaleFactor;
        private JSONStorableString pluginVersion;
        private float scaleMin = 0.1f;
        private float scaleDefault = 0.8f;
        private float scaleMax = 3.0f;
        protected JSONStorableFloat softness;
        private float softnessMin = 0.5f;
        private float softnessDefault = 1.5f;
        private float softnessMax = 3.0f;
        protected JSONStorableFloat scale;
        private float sagDefault = 1.2f;
        protected JSONStorableFloat sagMultiplier;
        private float nippleErectionDefault = 0.25f;
        protected JSONStorableFloat nippleErection;
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
                pluginVersion = new JSONStorableString("Version", "1.2.0");
                RegisterString(pluginVersion);

                if(containingAtom.type != "Person")
                {
                    LogError($"Plugin is for use with 'Person' atom, not '{containingAtom.type}'");
                    return;
                }

                GlobalVar.UPDATE_ENABLED = true;

                atomScale = containingAtom.GetStorableByID("rescaleObject").GetFloatJSONParam("scale");
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
                InitSliderListeners();

                InitBuiltInMorphs();
                InitSizeMorphs();
                InitNippleErectionMorphs();
                InitExampleMorphs();
                InitGravityMorphs();
                SetAllGravityMorphsToZero();
                UpdateAtomScaleFactor(atomScale.val);
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
            versionH1.SetVal($"{nameof(TittyMagic)} v{pluginVersion.val}");

            scale = NewFloatSlider("Breast scale", scaleDefault, scaleMin, scaleMax);
            softness = NewFloatSlider("Breast softness", softnessDefault, softnessMin, softnessMax);
            sagMultiplier = NewFloatSlider("Sag multiplier", sagDefault, 0f, 2.0f);
            nippleErection = NewFloatSlider("Erect nipples", nippleErectionDefault, 0f, 1.0f);

#if DEBUGINFO
            UIDynamicTextField angleInfoField = CreateTextField(angleDebugInfo, false);
            angleInfoField.height = 100;
            angleInfoField.UItext.fontSize = 26;
            UIDynamicTextField physicsInfoField = CreateTextField(physicsDebugInfo, false);
            physicsInfoField.height = 430;
            physicsInfoField.UItext.fontSize = 26;
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
            JSONStorableString usageInfo = NewTextField("Usage Info Area", 28, 640, true);
            string usage = "\n";
            usage += "Breast scale applies size morphs and anchors them to " +
                "size related physics settings. For best results, breast morphs " +
                "should be tweaked manually only after setting the scale amount. " +
                "(See the examples below.)\n\n";
            usage += "Breast softness controls soft physics and affects the amount " +
                "of morph-based sag in different orientations or poses.\n\n";
            usage += "Sag multiplier adjusts the sag produced by Breast softness " +
                "independently of soft physics.\n\n";
            usage += "Set breast morphs to defaults before applying example settings.";
            usageInfo.SetVal(usage);

            CreateNewSpacer(10f, true);

            //JSONStorableString presetsInfo = NewTextField("Example Settings", 28, 100, true);
            //presetsInfo.SetVal("\nSet breast morphs to defaults before applying example settings.");

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

        //JSONStorableBool NewToggle(string paramName)
        //{
        //    JSONStorableBool storable = new JSONStorableBool(paramName, true);
        //    CreateToggle(storable, false);
        //    RegisterBool(storable);
        //    return storable;
        //}

        void CreateExampleButtons()
        {
            UIDynamicButton example1 = CreateButton("Pornstar big naturals");
            example1.button.onClick.AddListener(() =>
            {
                scale.val = 1.65f;
                softness.val = 2.10f;
                sagMultiplier.val = 1.60f;

                ApplyMorphTweaks(example1Morphs);
                AppendToUILog(FormatExampleMorphsMessage("Pornstar big naturals", example1Morphs));
            });

            UIDynamicButton example2 = CreateButton("Small and perky");
            example2.button.onClick.AddListener(() =>
            {
                scale.val = 0.30f;
                softness.val = 1.10f;
                sagMultiplier.val = 1.80f;

                ApplyMorphTweaks(example2Morphs);
                AppendToUILog(FormatExampleMorphsMessage("Small and perky", example2Morphs));
            });

            UIDynamicButton example3 = CreateButton("Medium implants");
            example3.button.onClick.AddListener(() =>
            {
                scale.val = 0.75f;
                softness.val = 0.60f;
                sagMultiplier.val = 0.80f;

                ApplyMorphTweaks(example3Morphs);
                AppendToUILog(FormatExampleMorphsMessage("Medium implants", example3Morphs));
            });

            UIDynamicButton example4 = CreateButton("Huge and soft");
            example4.button.onClick.AddListener(() =>
            {
                scale.val = 3.00f;
                softness.val = 2.80f;
                sagMultiplier.val = 2.00f;

                ApplyMorphTweaks(example4Morphs);
                AppendToUILog(FormatExampleMorphsMessage("Huge and soft", example4Morphs));
            });

            CreateNewSpacer(10f);

            UIDynamicButton defaults = CreateButton("Undo example settings");
            defaults.button.onClick.AddListener(() =>
            {
                scale.val = scaleDefault;
                softness.val = softnessDefault;
                sagMultiplier.val = sagDefault;
                
                UndoMorphTweaks();
                AppendToUILog("> Example tweaks zeroed and sliders reset.");
            });
        }

        string FormatExampleMorphsMessage(string example, List<MorphConfig> morphs)
        {
            string text = $"> {example} morph tweaks:\n";
            foreach(var it in morphs)
            {
                text = text + FormatNameValueString(it.Name, it.Morph.morphValue) + "\n";
            }
            return text;
        }

        void AppendToUILog(string text)
        {
            logInfo.SetVal("\n" + text + "\n" + logInfo.val);
        }

        void CreateNewSpacer(float height, bool rightSide = false)
        {
            UIDynamic spacer = CreateSpacer(rightSide);
            spacer.height = height;
        }

        void InitSliderListeners()
        {
            scale.slider.onValueChanged.AddListener((float val) =>
            {
                UpdateBreastPhysicsSettings(val, softness.val);
            });
            softness.slider.onValueChanged.AddListener((float val) =>
            {
                UpdateBreastPhysicsSettings(scale.val, val);
            });
            sagMultiplier.slider.onValueChanged.AddListener((float val) =>
            {
                UpdateBreastPhysicsSettings(scale.val, softness.val);
            });
            nippleErection.slider.onValueChanged.AddListener((float val) =>
            {
                UpdateBreastPhysicsSettings(scale.val, softness.val);
            });
        }

        void AtomScaleListener(float val)
        {
            UpdateAtomScaleFactor(atomScale.val);
            UpdateBreastPhysicsSettings(scale.val, softness.val);
        }

        void InitBuiltInMorphs()
        {
            bodyScaleMorph = new MorphConfig("Body Scale", 0.00f);
            if (bodyScaleMorph.Morph.morphValue != 0)
            {
                LogMessage(
                    $"Morph '{bodyScaleMorph.Name}' is locked to 0.000! (It was {bodyScaleMorph.Morph.morphValue}.) " +
                    $"It is recommended to use the Scale slider in Control & Physics 1 to adjust atom scale if needed."
                );
            }
        }

        #region Morph settings
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

                new MorphConfig("TM_Baseline_Smaller",      -0.333f,     1.000f),
                //new MorphConfig("Breast Small",             -0.140f,     0.420f),

                new MorphConfig("TM_Baseline_Fixer",         0.000f,     1.000f),
                //new MorphConfig("Breast Top Curve1",         0.033f,    -0.033f),
                //new MorphConfig("Breast Top Curve2",         0.250f,    -0.750f),
                //new MorphConfig("Breasts Implants",          0.150f,     0.075f),
                //new MorphConfig("Breasts Size",              0.050f,    -0.050f),
            });
        }

        void InitNippleErectionMorphs()
        {
            nippleErectionMorphs.AddRange(new List<MorphConfig>
            {
                new MorphConfig("TM_Natural Nipples",       -0.100f,    0.025f), // Spacedog.Import_Reloaded_Lite.2
                new MorphConfig("TM_Nipple",                 0.500f,   -0.125f), // Spacedog.Import_Reloaded_Lite.2
                new MorphConfig("TM_Nipple Length",         -0.200f,    0.050f),
                new MorphConfig("TM_Nipples Apply",          0.500f,   -0.125f),
                new MorphConfig("TM_Nipples Bulbous",        0.600f,   -0.150f), // kemenate.Morphs.10
                new MorphConfig("TM_Nipples Large",          0.300f,   -0.075f),
                new MorphConfig("TM_Nipples Sag",           -0.200f,    0.050f), // kemenate.Morphs.10
                new MorphConfig("TM_Nipples Tilt",           0.200f,   -0.050f), // kemenate.Morphs.10
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
                new MorphConfig("Nipple Diameter",          -0.400f),
                new MorphConfig("Nipple Size",              -0.400f),
                new MorphConfig("Nipple Length",            -0.200f),
            });
            example2Morphs.AddRange(new List<MorphConfig>
            {
                new MorphConfig("Areolae Perk",              0.400f),
                new MorphConfig("Areola Size",              -0.300f),
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
                new MorphConfig("Nipple Diameter",          -0.333f),
                new MorphConfig("Nipples Large",            -0.100f),
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
        #endregion

        void SetBreastPhysicsDefaults()
        {
            containingAtom.GetStorableByID("BreastInOut").SetBoolParamValue("enabled", false);
            geometry.useAuxBreastColliders = true;

            // Right/left angle target
            breastControl.targetRotationY = 0f;
            breastControl.targetRotationZ = 0f;
            // Soft physics on
            breastPhysicsMesh.on = true;
            breastPhysicsMesh.softVerticesUseAutoColliderRadius = false;
            breastPhysicsMesh.softVerticesColliderAdditionalNormalOffset = 0.001f;
        }

        void UpdateAtomScaleFactor(float value)
        {
            if (value == 1)
            {
                atomScaleFactor = value;
                return;
            }
            
            if (value > 1)
            {
                atomScaleFactor = value / AtomScaleAdjustment(value);
                return;
            }

            if (value < 1)
            {
                if(value <= 0.5)
                {
                    atomScaleFactor = 0.5f * AtomScaleAdjustment(0.5f);
                    atomScale.slider.onValueChanged.RemoveListener(AtomScaleListener);
                    LogMessage(
                        "Person Atom Scale values lower than 0.5 are not fully compatible - " +
                        "this plugin will now behave as if it is 0.5. " +
                        "Reload the plugin after returning it to above 0.5."
                    );
                    return;
                }

                atomScaleFactor = value * AtomScaleAdjustment(value);
            }
        }

        // Experimentally determined that this somewhat accurately scales the Breast scale 
        // slider's effective value to the apparent breast size when body is scaled down/up.
        // Multiply by this when scaling down, divide when scaling up.
        float AtomScaleAdjustment(float value)
        {
            return 1 - (float) Math.Abs(Math.Log10(Math.Pow(value, 3)));
        }

        //float NormalizedScaleFactor(float scaleVal)
        //{
        //    return (scaleVal - scaleMin) / (scaleMax - scaleMin);
        //}

        //float NormalizedSoftnessFactor(float softnessVal)
        //{
        //    return (softnessVal - softnessMin) / (softnessMax - softnessMin);
        //}

        void UpdateBreastPhysicsSettings(float scaleVal, float softnessVal)
        {
            float scaleFactor = atomScaleFactor * (scaleVal - scaleMin);
            //                                                 base      size adjustment         softness adjustment
            breastControl.mass                              =  0.20f  + (0.621f * scaleFactor);
            breastControl.centerOfGravityPercent            =  0.40f  + (0.06f  * scaleFactor);
            breastControl.spring                            =  45f    + (20f    * scaleFactor);
            breastControl.damper                            =  1.20f  - (0.10f  * scaleFactor) + (0.10f  * softnessVal);
            breastControl.targetRotationX                   =  8.00f  - (1.67f  * scaleFactor) - (1.67f  * softnessVal);
            breastControl.positionSpringZ                   =  250f   + (200f   * scaleFactor);
            breastControl.positionDamperZ                   =  5f     + (3.0f   * scaleFactor) + (3.0f   * softnessVal);
            breastPhysicsMesh.softVerticesColliderRadius    =  0.020f + (0.004f * scaleFactor);
            breastPhysicsMesh.softVerticesCombinedSpring    =  120f   + (80f    * scaleFactor);
            breastPhysicsMesh.softVerticesCombinedDamper    =  2.0f   + (1.2f   * scaleFactor) + (1.2f   * softnessVal);
            breastPhysicsMesh.softVerticesMass              =  0.10f  + (0.14f  * scaleFactor);
            breastPhysicsMesh.softVerticesBackForce         =  12.0f  + (8.0f  * scaleFactor) - (6.0f   * softnessVal);
            breastPhysicsMesh.softVerticesBackForceMaxForce =  4.0f   + (1.0f   * scaleFactor);
            breastPhysicsMesh.softVerticesNormalLimit       =  0.010f + (0.010f * scaleFactor) + (0.003f * softnessVal);

            mainSpring.val      = (0.90f + (0.20f * softnessVal) - (0.10f * scaleFactor)) / softnessVal;
            mainDamper.val      = mainSpring.val;
            outerSpring.val     = mainSpring.val;
            outerDamper.val     = mainSpring.val;
            areolaSpring.val    = (1.40f + (0.33f * softnessVal)) / softnessVal;
            areolaDamper.val    = areolaSpring.val;
            nippleSpring.val    = nippleErection.val + areolaSpring.val;
            nippleDamper.val    = nippleErection.val + areolaSpring.val;

            breastPhysicsMesh.softVerticesBackForceThresholdDistance = (float) RoundToDecimals(
                0.000f + (0.002f * scaleFactor) + (0.001f * softnessVal),
                1000f
            );
        }

        public void Update()
        {
            TryInitGameUIListeners();

            try
            {
                if (enableUpdate)
                {
                    LockBodyScale();
                    AdjustMorphsForSize();
                    AdjustNipples();

                    float roll = Roll(chest.rotation);
                    float pitch = Pitch(chest.rotation);

                    AdjustMorphsForRoll(roll);
                    // Scale pitch effect by roll angle's distance from 90/-90 = person is sideways
                    //-> if person is sideways, pitch related morphs have less effect
                    AdjustMorphsForPitch(pitch, (90 - Mathf.Abs(roll)) / 90);

#if DEBUGINFO
                    SetAngleDebugInfo(roll, pitch);
                    SetPhysicsDebugInfo();
                    SetMorphDebugInfo();
#endif
                }
            }
            catch(Exception e)
            {
                LogError("Exception caught: " + e);
                GlobalVar.UPDATE_ENABLED = false;
                enableUpdate = GlobalVar.UPDATE_ENABLED;
            }
        }

        void TryInitGameUIListeners()
        {
            if (atomScale.slider != null && !atomScaleListenerIsSet)
            {
                // update physics settings in case Person atom's Scale is changed
                atomScale.slider.onValueChanged.AddListener(AtomScaleListener);
                atomScaleListenerIsSet = true;
            }
        }

        float Roll(Quaternion q)
        {
            return Mathf.Rad2Deg * Mathf.Asin(2 * q.x * q.y + 2 * q.z * q.w);
        }

        float Pitch(Quaternion q)
        {
            return Mathf.Rad2Deg* Mathf.Atan2(2 * q.x * q.w - 2 * q.y * q.z, 1 - 2 * q.x * q.x - 2 * q.z * q.z);
        }

        void LockBodyScale()
        {
            bodyScaleMorph.Morph.morphValue = 0;
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

        void AdjustNipples()
        {
            foreach(var it in nippleErectionMorphs)
            {
                it.Morph.morphValue = it.StartValue + it.BaseMulti * nippleErection.val;
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
                    float scaleFactor = m[2].HasValue ? atomScaleFactor * (float) m[2] * scale.val : 1;
                    
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
                    float scaleFactor = m[2].HasValue ? atomScaleFactor * (float) m[2] * scale.val : 1;
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
            SetMorphGroupToZero(nippleErectionMorphs);
        }

        void OnDisable()
        {
            SetAllGravityMorphsToZero();
            SetMorphGroupToZero(nippleErectionMorphs);
        }

#if DEBUGINFO
        void SetAngleDebugInfo(float roll, float pitch)
        {
            angleDebugInfo.SetVal(
                $"{FormatNameValueString("Roll", roll, 100f, 15, true)}\n" +
                $"{FormatNameValueString("Pitch", pitch, 100f, 15, true)}"
            );
        }

        void SetPhysicsDebugInfo()
        {
            physicsDebugInfo.SetVal(
                $"{FormatNameValueString("mass", breastControl.mass, padRight: 25)}\n" +
                $"{FormatNameValueString("center of g", breastControl.centerOfGravityPercent, padRight: 25)}\n" +
                $"{FormatNameValueString("spring", breastControl.spring, padRight: 25)}\n" +
                $"{FormatNameValueString("damper", breastControl.damper, padRight: 25)}\n" +
                $"{FormatNameValueString("in/out spr", breastControl.positionSpringZ, padRight: 25)}\n" +
                $"{FormatNameValueString("in/out dmp", breastControl.positionDamperZ, padRight: 25)}\n" +
                $"{FormatNameValueString("up/down target", breastControl.targetRotationX, padRight: 25)}\n" +

                $"{FormatNameValueString("collider radius", breastPhysicsMesh.softVerticesColliderRadius, padRight: 25)}\n" +
                $"{FormatNameValueString("back force", breastPhysicsMesh.softVerticesBackForce, padRight: 25)}\n" +
                $"{FormatNameValueString("back force max", breastPhysicsMesh.softVerticesBackForceMaxForce, padRight: 25)}\n" +
                $"{FormatNameValueString("back force thres", breastPhysicsMesh.softVerticesBackForceThresholdDistance, padRight: 25)}\n" +
                $"{FormatNameValueString("fat spring", breastPhysicsMesh.softVerticesCombinedSpring, padRight: 25)}\n" +
                $"{FormatNameValueString("fat damper", breastPhysicsMesh.softVerticesCombinedDamper, padRight: 25)}\n" +
                $"{FormatNameValueString("fat mass", breastPhysicsMesh.softVerticesMass, padRight: 25)}\n" +
                $"{FormatNameValueString("distance limit", breastPhysicsMesh.softVerticesNormalLimit, padRight: 25)}\n" +
                $"{FormatNameValueString("main spring", mainSpring.val, padRight: 25)}\n" +
                $"{FormatNameValueString("main damper", mainDamper.val, padRight: 25)}\n" +
                $"{FormatNameValueString("outer spring", outerSpring.val, padRight: 25)}\n" +
                $"{FormatNameValueString("outer damper", outerDamper.val, padRight: 25)}\n" +
                $"{FormatNameValueString("areola spring", areolaSpring.val, padRight: 25)}\n" +
                $"{FormatNameValueString("areola damper", areolaDamper.val, padRight: 25)}\n" +
                $"{FormatNameValueString("nipple spring", nippleSpring.val, padRight: 25)}\n" +
                $"{FormatNameValueString("nipple damper", nippleDamper.val, padRight: 25)}\n"
            );
        }

        void SetMorphDebugInfo()
        {
            string text = "";
            text += "SIZE MORPHS\n";
            foreach(var it in sizeMorphs)
            {
                text = text + FormatNameValueString(it.Name, it.Morph.morphValue, 1000f, 30) + "\n";
            }

            text += "\nGRAVITY MORPHS\n";
            foreach(var it in gravityMorphs)
            {
                text = text + FormatNameValueString(it.Name, it.Morph.morphValue, 1000f, 30) + "\n";
            }
            morphDebugInfo.SetVal(text);
        }
#endif

        #region Formatting utils
        string FormatNameValueString(string name, float value, float roundFactor = 1000f, int padRight = 0, bool normalize = false)
        {
            double rounded = RoundToDecimals(value, 1000f);
            string printName = StripPrefix(name, "TM_").PadRight(padRight, ' ');
            string printValue = normalize ? NormalizeNumberFormat(rounded) : $"{rounded}";
            return string.Format("{0} {1}", printName, printValue);
        }

        public static string StripPrefix(string text, string prefix)
        {
            return text.StartsWith(prefix) ? text.Substring(prefix.Length) : text;
        }

        double RoundToDecimals(float value, float roundFactor)
        {
            return Math.Round(value * roundFactor) / roundFactor;
        }

        string NormalizeNumberFormat(double value)
        {
            string formatted = string.Format("{0:000.00}", value);
            return value >= 0 ? $" {formatted}" : formatted;
        }
        #endregion

        void LogError(string message)
        {
            SuperController.LogError($"{nameof(everlaster)}.{nameof(TittyMagic)}: {message}");
        }

        void LogMessage(string message)
        {
            SuperController.LogMessage($"{nameof(everlaster)}.{nameof(TittyMagic)}: {message}");
        }
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
                SuperController.LogError($"everlaster.TittyMagic: Morph with name {name} not found!");
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
                SuperController.LogError($"everlaster.TittyMagic: Morph with name {name} not found!");
            }
        }
    }
}
