//#define DEBUGINFO
using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace everlaster
{
    class TittyMagic : MVRScript
    {
        private bool enableUpdate;
        private Transform chest;
        private AdjustJoints breastControl;
        private DAZPhysicsMesh breastPhysicsMesh;
        private DAZCharacterSelector geometry;

        private SizeMorphConfig bodyScaleMorph;
        private List<SizeMorphConfig> sizeMorphs = new List<SizeMorphConfig>();
        private List<NippleErectionMorphConfig> nippleErectionMorphs = new List<NippleErectionMorphConfig>();
        private List<ExampleMorphConfig> example1Morphs = new List<ExampleMorphConfig>();
        private List<ExampleMorphConfig> example2Morphs = new List<ExampleMorphConfig>();
        private List<ExampleMorphConfig> example3Morphs = new List<ExampleMorphConfig>();
        private List<ExampleMorphConfig> example4Morphs = new List<ExampleMorphConfig>();
        private List<GravityMorphConfig> gravityMorphs = new List<GravityMorphConfig>();

        private List<GravityPhysicsConfig> gravityPhysics = new List<GravityPhysicsConfig>();

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

        // TODO cleanup initialization somehow
        public override void Init()
        {
            try
            {
                pluginVersion = new JSONStorableString("Version", "1.3.0");
                RegisterString(pluginVersion);

                if(containingAtom.type != "Person")
                {
                    Log.Error($"Plugin is for use with 'Person' atom, not '{containingAtom.type}'");
                    return;
                }

                Globals.UPDATE_ENABLED = true;

                InitVariables();
                InitPluginUILeft();
                InitPluginUIRight();
                InitSliderListeners();

                InitMorphConfigs();
                
                gravityMorphs.ForEach(it => it.Reset());
                
                ResolveAtomScaleFactor(atomScale.val);

                InitPhysicsConfigs();
                SetPhysicsDefaults();
                UpdatePhysicsSettings(scale.val, softness.val);

                enableUpdate = Globals.UPDATE_ENABLED;
            }
            catch(Exception e)
            {
                Log.Error("Exception caught: " + e);
            }
        }

        void InitVariables()
        {
            atomScale = containingAtom.GetStorableByID("rescaleObject").GetFloatJSONParam("scale");
            breastControl = containingAtom.GetStorableByID("BreastControl") as AdjustJoints;
            breastPhysicsMesh = containingAtom.GetStorableByID("BreastPhysicsMesh") as DAZPhysicsMesh;
            geometry = containingAtom.GetStorableByID("geometry") as DAZCharacterSelector;
            chest = containingAtom.GetStorableByID("chest").transform;

            mainSpring = breastPhysicsMesh.GetFloatJSONParam("groupASpringMultiplier");
            mainDamper = breastPhysicsMesh.GetFloatJSONParam("groupADamperMultiplier");
            outerSpring = breastPhysicsMesh.GetFloatJSONParam("groupBSpringMultiplier");
            outerDamper = breastPhysicsMesh.GetFloatJSONParam("groupBDamperMultiplier");
            areolaSpring = breastPhysicsMesh.GetFloatJSONParam("groupCSpringMultiplier");
            areolaDamper = breastPhysicsMesh.GetFloatJSONParam("groupCDamperMultiplier");
            nippleSpring = breastPhysicsMesh.GetFloatJSONParam("groupDSpringMultiplier");
            nippleDamper = breastPhysicsMesh.GetFloatJSONParam("groupDDamperMultiplier");

            Globals.MORPH_UI = geometry.morphsControlUI;
            Globals.BREAST_CONTROL = containingAtom.GetStorableByID("BreastControl") as AdjustJoints;
        }

        // TODO UI class?
        void InitPluginUILeft()
        {
            JSONStorableString versionH1 = NewTextField("Version Info", 40);
            versionH1.SetVal($"{nameof(TittyMagic)} v{pluginVersion.val}");

            // doesn't just init UI, also variables...
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

                UndoMorphTweaks();
                example1Morphs.ForEach(it => it.UpdateVal());
                AppendToUILog(FormatExampleMorphsMessage("Pornstar big naturals", example1Morphs));
            });

            UIDynamicButton example2 = CreateButton("Small and perky");
            example2.button.onClick.AddListener(() =>
            {
                scale.val = 0.30f;
                softness.val = 1.10f;
                sagMultiplier.val = 1.80f;

                UndoMorphTweaks();
                example2Morphs.ForEach(it => it.UpdateVal());
                AppendToUILog(FormatExampleMorphsMessage("Small and perky", example2Morphs));
            });

            UIDynamicButton example3 = CreateButton("Medium implants");
            example3.button.onClick.AddListener(() =>
            {
                scale.val = 0.75f;
                softness.val = 0.60f;
                sagMultiplier.val = 0.80f;

                UndoMorphTweaks();
                example3Morphs.ForEach(it => it.UpdateVal());
                AppendToUILog(FormatExampleMorphsMessage("Medium implants", example3Morphs));
            });

            UIDynamicButton example4 = CreateButton("Huge and soft");
            example4.button.onClick.AddListener(() =>
            {
                scale.val = 3.00f;
                softness.val = 2.80f;
                sagMultiplier.val = 2.00f;

                UndoMorphTweaks();
                example4Morphs.ForEach(it => it.UpdateVal());
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

        string FormatExampleMorphsMessage(string example, List<ExampleMorphConfig> morphs)
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
                UpdatePhysicsSettings(val, softness.val);
            });
            softness.slider.onValueChanged.AddListener((float val) =>
            {
                UpdatePhysicsSettings(scale.val, val);
            });
            sagMultiplier.slider.onValueChanged.AddListener((float val) =>
            {
                UpdatePhysicsSettings(scale.val, softness.val);
            });
            nippleErection.slider.onValueChanged.AddListener((float val) =>
            {
                UpdatePhysicsSettings(scale.val, softness.val);
            });
        }

        void AtomScaleListener(float val)
        {
            ResolveAtomScaleFactor(atomScale.val);
            UpdatePhysicsSettings(scale.val, softness.val);
        }

        #region Morph configs
        void InitMorphConfigs()
        {
            InitBuiltInMorphs();
            InitSizeMorphs();
            InitNippleErectionMorphs();
            InitExampleMorphs();
            InitGravityMorphs();
        }

        void InitBuiltInMorphs()
        {
            bodyScaleMorph = new SizeMorphConfig("Body Scale", 0.00f);
            if (bodyScaleMorph.Morph.morphValue != 0)
            {
                Log.Message(
                    $"Morph '{bodyScaleMorph.Name}' is locked to 0.000! (It was {bodyScaleMorph.Morph.morphValue}.) " +
                    $"It is recommended to use the Scale slider in Control & Physics 1 to adjust atom scale if needed."
                );
            }
        }

        void InitSizeMorphs()
        {
            sizeMorphs.AddRange(new List<SizeMorphConfig>
            {
                //               morph                       base        start
                new SizeMorphConfig("TM_Baseline",               1.000f),
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

                new SizeMorphConfig("TM_Baseline_Smaller",      -0.333f,     1.000f),
                //new MorphConfig("Breast Small",             -0.140f,     0.420f),

                new SizeMorphConfig("TM_Baseline_Fixer",         0.000f,     1.000f),
                //new MorphConfig("Breast Top Curve1",         0.033f,    -0.033f),
                //new MorphConfig("Breast Top Curve2",         0.250f,    -0.750f),
                //new MorphConfig("Breasts Implants",          0.150f,     0.075f),
                //new MorphConfig("Breasts Size",              0.050f,    -0.050f),
            });
        }

        void InitNippleErectionMorphs()
        {
            nippleErectionMorphs.AddRange(new List<NippleErectionMorphConfig>
            {
                new NippleErectionMorphConfig("TM_Natural Nipples",       -0.100f,    0.025f), // Spacedog.Import_Reloaded_Lite.2
                new NippleErectionMorphConfig("TM_Nipple",                 0.500f,   -0.125f), // Spacedog.Import_Reloaded_Lite.2
                new NippleErectionMorphConfig("TM_Nipple Length",         -0.200f,    0.050f),
                new NippleErectionMorphConfig("TM_Nipples Apply",          0.500f,   -0.125f),
                new NippleErectionMorphConfig("TM_Nipples Bulbous",        0.600f,   -0.150f), // kemenate.Morphs.10
                new NippleErectionMorphConfig("TM_Nipples Large",          0.300f,   -0.075f),
                new NippleErectionMorphConfig("TM_Nipples Sag",           -0.200f,    0.050f), // kemenate.Morphs.10
                new NippleErectionMorphConfig("TM_Nipples Tilt",           0.200f,   -0.050f), // kemenate.Morphs.10
            });
        }

        void InitExampleMorphs()
        {
            example1Morphs.AddRange(new List<ExampleMorphConfig>
            {
                //               morph                       value
                new ExampleMorphConfig("Breast Height Upper",       0.175f),
                new ExampleMorphConfig("Breast Pointed",            0.100f),
                new ExampleMorphConfig("Breast Round",             -0.500f),
                new ExampleMorphConfig("Breast Top Curve2",        -0.175f),
                new ExampleMorphConfig("Breast Zero",               0.100f),
                new ExampleMorphConfig("Breasts Natural Left",     -0.150f),
                new ExampleMorphConfig("Breasts Natural Right",     0.075f),
                new ExampleMorphConfig("Breasts Size",              0.050f),
                new ExampleMorphConfig("BreastsShape2",            -0.175f),
                new ExampleMorphConfig("Nipple Diameter",          -0.400f),
                new ExampleMorphConfig("Nipple Size",              -0.400f),
                new ExampleMorphConfig("Nipple Length",            -0.200f),
            });
            example2Morphs.AddRange(new List<ExampleMorphConfig>
            {
                new ExampleMorphConfig("Areolae Perk",              0.400f),
                new ExampleMorphConfig("Areola Size",              -0.300f),
                new ExampleMorphConfig("Breast diameter",          -0.050f),
                new ExampleMorphConfig("Breast Sag1",               0.100f),
                new ExampleMorphConfig("Breast Sag2",               0.150f),
                new ExampleMorphConfig("Breast Pointed",            0.300f),
                new ExampleMorphConfig("Breast Round",             -0.300f),
                new ExampleMorphConfig("Breasts Cleavage",          0.150f),
                new ExampleMorphConfig("Breasts Natural Left",      0.150f),
                new ExampleMorphConfig("Breasts Natural Right",     0.190f),
                new ExampleMorphConfig("Breasts Implants Left",    -0.025f),
                new ExampleMorphConfig("Breasts Implants Right",   -0.025f),
                new ExampleMorphConfig("Breasts Small",             0.150f),
                new ExampleMorphConfig("Breasts Under Curve",       0.150f),
                new ExampleMorphConfig("Nipple Diameter",          -0.600f),
                new ExampleMorphConfig("Nipple Length",            -0.300f),
                new ExampleMorphConfig("Nipple Size",              -0.100f),
                new ExampleMorphConfig("Sternum Width",             0.100f),
            });
            example3Morphs.AddRange(new List<ExampleMorphConfig>
            {
                new ExampleMorphConfig("Breast diameter",           0.500f),
                new ExampleMorphConfig("Breast Round",             -0.250f),
                new ExampleMorphConfig("Breast Under Smoother3",   -0.350f),
                new ExampleMorphConfig("Breasts Cleavage",          0.500f),
                new ExampleMorphConfig("Breasts Implants Left",     0.200f),
                new ExampleMorphConfig("Breasts Implants Right",    0.180f),
                new ExampleMorphConfig("Breasts Natural Left",     -0.150f),
                new ExampleMorphConfig("Breasts Natural Right",    -0.125f),
                new ExampleMorphConfig("Breasts Perk Side",         0.300f),
                new ExampleMorphConfig("Breasts Size",             -0.350f),
                new ExampleMorphConfig("Chest Smoother",           -0.500f),
                new ExampleMorphConfig("Nipple Size",              -0.500f),
                new ExampleMorphConfig("Nipple Length",            -0.350f),
                new ExampleMorphConfig("Nipples Size",              0.150f),
                new ExampleMorphConfig("Sternum Width",             0.750f),
            });
            example4Morphs.AddRange(new List<ExampleMorphConfig>
            {
                new ExampleMorphConfig("Areola Size",               0.500f),
                new ExampleMorphConfig("Areola Size X",            -0.250f),
                new ExampleMorphConfig("Areola Size Y",             0.650f),
                new ExampleMorphConfig("Areola Puffy Edge",         0.500f),
                new ExampleMorphConfig("Areolae Diameter",          0.250f),
                new ExampleMorphConfig("Areolae Perk",              0.500f),
                new ExampleMorphConfig("Breasts Cleavage",         -0.100f),
                new ExampleMorphConfig("Breasts Gone",             -0.100f),
                new ExampleMorphConfig("Breasts Perk Side",         0.450f),
                new ExampleMorphConfig("Breasts Size",             -0.100f),
                new ExampleMorphConfig("BreastsShape3",            -0.300f),
                new ExampleMorphConfig("Nipple Diameter",          -0.333f),
                new ExampleMorphConfig("Nipples Large",            -0.100f),
                new ExampleMorphConfig("ChestUnderBreast",          0.250f),
                new ExampleMorphConfig("Sternum Width",             0.250f),
            });
        }

        // TODO refactor to not use Dictionary -> same morph can be listed multiple times for different angle types
        // Possible to merge to one morph per angle type?
        void InitGravityMorphs()
        {
            gravityMorphs.AddRange(new List<GravityMorphConfig>
            {
                //    USAGE: AdjustMorphs function
                //    angle type                            base       softness   scale
                //    Main sag morphs
                new GravityMorphConfig("TM_Breast Move Up", new Dictionary<string, float?[]> {
                    { AngleTypes.UPRIGHT, new float?[]        { -0.07f,     1.67f,     0.33f } },
                    { AngleTypes.UPSIDE_DOWN, new float?[]    {  0.07f,     1.67f,     0.33f } },
                }),
                new GravityMorphConfig("TM_Breast Sag1", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    { -0.03f,     1.25f,     0.75f } },
                }),
                new GravityMorphConfig("TM_Breast Sag2", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    { -0.05f,     1.25f,     0.75f } },
                }),
                new GravityMorphConfig("TM_Breasts Hang Forward", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    {  0.05f,     1.50f,     0.80f } },
                }),
                new GravityMorphConfig("TM_Breasts Natural", new Dictionary<string, float?[]> {
                    { AngleTypes.UPRIGHT, new float?[]        {  0.08f,     2.00f,     0.00f } },
                    { AngleTypes.UPSIDE_DOWN, new float?[]    { -0.04f,     2.00f,     0.00f } },
                }),
                new GravityMorphConfig("TM_Breasts TogetherApart", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    {  0.10f,     1.50f,     0.80f } },
                }),

                //    Tweak morphs
                new GravityMorphConfig("TM_Areola UpDown", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    { -0.15f,     1.33f,     0.67f } },
                }),
                new GravityMorphConfig("TM_Center Gap Depth", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    {  0.05f,     0.50f,     1.50f } },
                }),
                new GravityMorphConfig("TM_Center Gap Height", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    {  0.10f,     0.50f,     1.50f } },
                }),
                new GravityMorphConfig("TM_Center Gap UpDown", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    {  0.10f,     0.50f,     1.50f } },
                }),
                new GravityMorphConfig("TM_Chest Smoother", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    {  0.10f,     0.75f,     1.25f } },
                }),
                new GravityMorphConfig("TM_ChestUnderBreast", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    {  0.15f,     1.75f,     0.25f } },
                }),
                new GravityMorphConfig("TM_ChestUp", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    {  0.05f,     1.75f,     0.25f } },
                }),
                new GravityMorphConfig("TM_ChestUpperNarrow", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    {  0.10f,     1.75f,     0.25f } },
                }),
                new GravityMorphConfig("TM_Breast Diameter", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    {  0.05f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast flat", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    {  0.08f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Height", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    {  0.10f,     2.00f,     0.00f } },
                }),
                new GravityMorphConfig("TM_Breast Pointed", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    {  0.33f,     0.00f,     1.00f } },
                }),
                new GravityMorphConfig("TM_Breast Rotate Up", new Dictionary<string, float?[]> {
                    { AngleTypes.UPRIGHT, new float?[]        {  0.15f,     0.80f,     1.20f } },
                    { AngleTypes.UPSIDE_DOWN, new float?[]    {  0.25f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Top Curve1", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    { -0.04f,     2.00f,    -0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Top Curve2", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    { -0.06f,     2.00f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Under Smoother1", new Dictionary<string, float?[]> {
                    { AngleTypes.UPRIGHT, new float?[]        { -0.04f,     0.50f,     1.50f } },
                    { AngleTypes.UPSIDE_DOWN, new float?[]    {  0.45f,     0.60f,     1.40f } },
                }),
                new GravityMorphConfig("TM_Breast Under Smoother3", new Dictionary<string, float?[]> {
                    { AngleTypes.UPRIGHT, new float?[]        { -0.08f,     1.00f,     1.00f } },
                    { AngleTypes.UPSIDE_DOWN, new float?[]    {  0.20f,     1.00f,    -1.00f } },
                }),
                new GravityMorphConfig("TM_Breasts Flatten", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    {  0.10f,     1.40f,     0.60f } },
                }),
                new GravityMorphConfig("TM_Breasts Height", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    {  0.10f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breasts Implants", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    { -0.05f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breasts Upward Slope", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    {  0.15f,     1.20f,     0.80f } },
                }),
                new GravityMorphConfig("TM_BreastsShape2", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    {  0.50f,     0.67f,     1.33f } },
                }),
                new GravityMorphConfig("TM_Sternum Height", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    { -0.30f,     null,     null } },
                }),
            });

            gravityMorphs.AddRange(new List<GravityMorphConfig>
            {
                //    USAGE: AdjustMorphs function
                //    angle type                            base       softness   scale
                new GravityMorphConfig("TM_Breast Depth Left", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_FORWARD, new float?[]   {  0.22f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Depth Right", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_FORWARD, new float?[]   {  0.22f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Depth Squash Left", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_BACK, new float?[]      { -0.20f,     2.00f,     0.00f } },
                }),
                new GravityMorphConfig("TM_Breast Depth Squash Right", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_BACK, new float?[]      { -0.20f,     2.00f,     0.00f } },
                }),
                new GravityMorphConfig("TM_Breast Diameter Left", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_FORWARD, new float?[]   {  0.22f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Diameter Right", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_FORWARD, new float?[]   {  0.22f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Diameter (Copy)", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_BACK, new float?[]      {  0.08f,     1.50f,     0.50f } },
                    { AngleTypes.LEAN_FORWARD, new float?[]   { -0.04f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Large", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_BACK, new float?[]      { -0.08f,     1.50f,     0.50f } },
                    { AngleTypes.LEAN_FORWARD, new float?[]   { -0.08f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Side Smoother", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_FORWARD, new float?[]   {  0.20f,     1.80f,     0.20f } },
                    { AngleTypes.LEAN_BACK, new float?[]      { -0.33f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Under Smoother1 (Copy)", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_BACK, new float?[]      { -0.04f,     1.00f,     1.00f } },
                }),
                new GravityMorphConfig("TM_Breast Under Smoother3 (Copy)", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_BACK, new float?[]      { -0.10f,     1.00f,     1.00f } },
                }),
                new GravityMorphConfig("TM_Breast Move S2S Out Left", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_BACK, new float?[]      {  0.08f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Move S2S Out Right", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_BACK, new float?[]      {  0.08f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breasts Flatten (Copy)", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_BACK, new float?[]      {  0.25f,     1.67f,     0.33f } },
                }),
                new GravityMorphConfig("TM_Breasts Height (Copy)", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_FORWARD, new float?[]   { -0.18f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breasts Hang Forward (Copy)", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_FORWARD, new float?[]   {  0.05f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breasts TogetherApart (Copy)", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_FORWARD, new float?[]   {  0.20f,     1.75f,     0.25f  } },
                }),
                new GravityMorphConfig("TM_Chest Smoother (Copy)", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_BACK, new float?[]      {  0.33f,     2.00f,     0.00f } },
                }),
                new GravityMorphConfig("TM_ChestShape", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_BACK, new float?[]      { -0.20f,     1.00f,     1.00f } },
                }),
                new GravityMorphConfig("TM_ChestSmoothCenter", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_BACK, new float?[]      {  0.15f,     1.67f,     0.33f } },
                }),
                new GravityMorphConfig("TM_ChestUp (Copy)", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_BACK, new float?[]      {  0.20f,     1.00f,     1.00f } },
                }),
                new GravityMorphConfig("TM_Sternum Width", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_FORWARD, new float?[]   {  0.25f,     1.25f,     0.75f  } },
                    { AngleTypes.LEAN_BACK, new float?[]      {  0.33f,    -0.67f,     1.33f } },
                }),
            });

            gravityMorphs.AddRange(new List<GravityMorphConfig>
            {
                //    USAGE: AdjustMorphs function
                //    angle type                            base       softness   scale
                new GravityMorphConfig("TM_Areola S2S Left", new Dictionary<string, float?[]> {
                    { AngleTypes.ROLL_LEFT, new float?[]      { -0.40f,     1.50f,     0.50f } },
                    { AngleTypes.ROLL_RIGHT, new float?[]     {  0.40f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Areola S2S Right", new Dictionary<string, float?[]> {
                    { AngleTypes.ROLL_LEFT, new float?[]      {  0.40f,     1.50f,     0.50f } },
                    { AngleTypes.ROLL_RIGHT, new float?[]     { -0.40f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Move S2S In Left", new Dictionary<string, float?[]> {
                    { AngleTypes.ROLL_RIGHT, new float?[]     {  0.28f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Move S2S In Right", new Dictionary<string, float?[]> {
                    { AngleTypes.ROLL_LEFT, new float?[]      {  0.28f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Move S2S Out Left (Copy)", new Dictionary<string, float?[]> {
                    { AngleTypes.ROLL_LEFT, new float?[]      {  0.40f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Move S2S Out Right (Copy)", new Dictionary<string, float?[]> {
                    { AngleTypes.ROLL_RIGHT, new float?[]     {  0.40f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Rotate X In Left", new Dictionary<string, float?[]> {
                    { AngleTypes.ROLL_RIGHT, new float?[]     {  0.10f,     2.00f,     0.00f } },
                }),
                new GravityMorphConfig("TM_Breast Rotate X In Right", new Dictionary<string, float?[]> {
                    { AngleTypes.ROLL_LEFT, new float?[]      {  0.10f,     2.00f,     0.00f } },
                }),
                new GravityMorphConfig("TM_Breast Width Left", new Dictionary<string, float?[]> {
                    { AngleTypes.ROLL_LEFT, new float?[]      { -0.03f,     1.60f,     0.40f } },
                    { AngleTypes.ROLL_RIGHT, new float?[]     {  0.07f,     1.60f,     0.40f } },
                }),
                new GravityMorphConfig("TM_Breast Width Right", new Dictionary<string, float?[]> {
                    { AngleTypes.ROLL_LEFT, new float?[]      {  0.07f,     1.60f,     0.40f } },
                    { AngleTypes.ROLL_RIGHT, new float?[]     { -0.03f,     1.60f,     0.40f } },
                }),
                new GravityMorphConfig("TM_Breasts Diameter", new Dictionary<string, float?[]> {
                    { AngleTypes.ROLL_LEFT, new float?[]      { -0.05f,     1.60f,     0.40f } },
                    { AngleTypes.ROLL_RIGHT, new float?[]     { -0.05f,     1.60f,     0.40f } },
                }),
                new GravityMorphConfig("TM_Centre Gap Narrow", new Dictionary<string, float?[]> {
                    { AngleTypes.ROLL_LEFT, new float?[]      {  0.10f,     1.75f,     0.25f } },
                    { AngleTypes.ROLL_RIGHT, new float?[]     {  0.10f,     1.75f,     0.25f } },
                }),
                new GravityMorphConfig("TM_Center Gap Smooth", new Dictionary<string, float?[]> {
                    { AngleTypes.ROLL_LEFT, new float?[]      {  0.20f,     1.75f,     0.25f } },
                    { AngleTypes.ROLL_RIGHT, new float?[]     {  0.20f,     1.75f,     0.25f } },
                }),
            });
        }
        #endregion

        #region Physics configs
        void SetPhysicsDefaults()
        {
            // In/Out auto morphs off
            containingAtom.GetStorableByID("BreastInOut").SetBoolParamValue("enabled", false);
            // Hard colliders on
            geometry.useAuxBreastColliders = true;
            // Right/left angle target moves both breasts in the same direction
            breastControl.invertJoint2RotationY = false;
            // Soft physics on
            breastPhysicsMesh.on = true;
            // Auto collider radius off
            breastPhysicsMesh.softVerticesUseAutoColliderRadius = false;
            // Collider depth
            breastPhysicsMesh.softVerticesColliderAdditionalNormalOffset = 0.001f;
        }

        //breastControl storables:
        //=======================
        //mass
        //centerOfGravityPercent
        //spring
        //damper
        //positionSpringX
        //positionSpringY
        //positionSpringZ
        //positionDamperX
        //positionDamperY
        //positionDamperZ
        //targetRotationX
        //targetRotationY
        //targetRotationZ

        //breastPhysicsMesh storables:
        //===========================
        //softVerticesCombinedSpring
        //softVerticesCombinedDamper
        //softVerticesMass
        //softVerticesBackForce
        //softVerticesBackForceThresholdDistance
        //softVerticesBackForceMaxForce
        //softVerticesColliderRadius
        //softVerticesColliderAdditionalNormalOffset
        //softVerticesDistanceLimit
        //groupASpringMultiplier
        //groupADamperMultiplier
        //groupBSpringMultiplier
        //groupBDamperMultiplier
        //groupCSpringMultiplier
        //groupCDamperMultiplier
        //groupDSpringMultiplier
        //groupDDamperMultiplier

        void InitPhysicsConfigs()
        {
            gravityPhysics.AddRange(new List<GravityPhysicsConfig>()
            {
                //                       name                       angle type      min     max     scale   softness
                new GravityPhysicsConfig("centerOfGravityPercent",  AngleTypes.PITCH,    0.40f,  0.574f, 1f,     null),
                new GravityPhysicsConfig("targetRotationX",         AngleTypes.PITCH,    0f,     8f,     2f,     2f),
                new GravityPhysicsConfig("targetRotationY",         AngleTypes.ROLL,     0f,     8f,     2f,     2f),
            });
            gravityPhysics.ForEach(it => it.InitStorable());
        
            // TODO init basePhysics configs
            // basePhysics.ForEach(it => it.InitStorable());
        }
        #endregion

        void ResolveAtomScaleFactor(float value)
        {
            if (value == 1)
            {
                atomScaleFactor = value;
                return;
            }
            
            if (value > 1)
            {
                atomScaleFactor = value / CalcUtils.AtomScaleAdjustment(value);
                return;
            }

            if (value < 1)
            {
                if(value <= 0.5)
                {
                    atomScaleFactor = 0.5f * CalcUtils.AtomScaleAdjustment(0.5f);
                    atomScale.slider.onValueChanged.RemoveListener(AtomScaleListener);
                    Log.Message(
                        "Person Atom Scale values lower than 0.5 are not fully compatible - " +
                        "this plugin will now behave as if it is 0.5. " +
                        "Reload the plugin after returning it to above 0.5."
                    );
                    return;
                }

                atomScaleFactor = value * CalcUtils.AtomScaleAdjustment(value);
            }
        }

        // TODO refactor to use its own Config class and JSON storables (basePhysics)
        // Update function given individually to each parameter
        void UpdatePhysicsSettings(float scaleVal, float softnessVal)
        {
            float hardnessVal = softnessMax - softnessVal; // 0.00 .. 2.50 for softness 3.00 .. 0.50
            float scaleFactor = atomScaleFactor * (scaleVal - scaleMin);
            //                                                 base      size adjustment         softness adjustment
            breastControl.mass                              =  0.20f  + (0.621f * scaleFactor);
            breastControl.spring                            =  34f    + (10f    * scaleFactor) + (10f    * hardnessVal);
            breastControl.damper                            =  0.75f  + (0.66f  * scaleFactor);
            breastControl.positionSpringZ                   =  250f   + (200f   * scaleFactor);
            breastControl.positionDamperZ                   =  5f     + (3.0f   * scaleFactor) + (3.0f   * softnessVal);
            breastPhysicsMesh.softVerticesColliderRadius    =  0.022f + (0.005f * scaleFactor);
            breastPhysicsMesh.softVerticesCombinedSpring    =  80f    + (60f    * scaleFactor) + (45f    * softnessVal);
            breastPhysicsMesh.softVerticesCombinedDamper    =  1.00f  + (1.20f  * scaleFactor) + (0.30f  * softnessVal);
            breastPhysicsMesh.softVerticesMass              =  0.08f  + (0.10f  * scaleFactor);
            breastPhysicsMesh.softVerticesBackForce         =  2.0f   + (5.0f   * scaleFactor) + (4.0f   * hardnessVal);
            breastPhysicsMesh.softVerticesBackForceMaxForce =  2.0f   + (1.5f   * scaleFactor) + (1.0f   * hardnessVal);
            breastPhysicsMesh.softVerticesNormalLimit       =  0.010f + (0.010f * scaleFactor) + (0.003f * softnessVal);

            mainSpring.val      = (1.00f + (0.15f * softnessVal) - (0.10f * scaleFactor)) / softnessVal;
            mainDamper.val      = mainSpring.val;
            outerSpring.val     = (1.80f + (0.20f * softnessVal) - (0.10f * scaleFactor)) / softnessVal;
            outerDamper.val     = outerSpring.val;
            areolaSpring.val    = (1.10f + (0.25f * softnessVal)) / softnessVal;
            areolaDamper.val    = areolaSpring.val;
            nippleSpring.val    = nippleErection.val + areolaSpring.val;
            nippleDamper.val    = nippleErection.val + areolaSpring.val;

            breastPhysicsMesh.softVerticesBackForceThresholdDistance = (float) CalcUtils.RoundToDecimals(
                0.0015f + (0.002f * scaleFactor) - (0.001f * softnessVal),
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
                    bodyScaleMorph.Morph.morphValue = 0;
                    sizeMorphs.ForEach(it => it.UpdateVal((scale.val - scaleMin) / 0.9f));
                    nippleErectionMorphs.ForEach(it => it.UpdateVal(nippleErection.val));

                    float roll = CalcUtils.Roll(chest.rotation);
                    float pitch = CalcUtils.Pitch(chest.rotation);

                    AdjustMorphsForRoll(roll);
                    // Scale pitch effect by roll angle's distance from 90/-90 = person is sideways
                    //-> if person is sideways, pitch related morphs have less effect
                    AdjustMorphsForPitch(pitch, (90 - Mathf.Abs(roll)) / 90);

                    //AdjustPhysicsForRoll(roll);
                    //AdjustPhysicsForPitch(pitch, (90 - Mathf.Abs(roll)) / 90);
#if DEBUGINFO
                    SetAngleDebugInfo(roll, pitch);
                    SetMorphDebugInfo();
                    SetPhysicsDebugInfo();
#endif
                }
            }
            catch(Exception e)
            {
                Log.Error("Exception caught: " + e);
                Globals.UPDATE_ENABLED = false;
                enableUpdate = Globals.UPDATE_ENABLED;
            }
        }

        float ScaleFactor()
        {
            return atomScaleFactor * scale.val;
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

        // TODO Move all angle-based update logic to another class?
        void AdjustMorphsForRoll(float roll, float rollFactor = 1f)
        {
            // left
            if(roll >= 0)
            {
                SetGravityMorphsToZero(AngleTypes.ROLL_RIGHT);
                DoAdjustMorphsForRoll(AngleTypes.ROLL_LEFT, CalcUtils.Remap(roll, rollFactor));
            }
            // right
            else
            {
                SetGravityMorphsToZero(AngleTypes.ROLL_LEFT);
                DoAdjustMorphsForRoll(AngleTypes.ROLL_RIGHT, CalcUtils.Remap(Mathf.Abs(roll), rollFactor));
            }
        }

        void AdjustMorphsForPitch(float pitch, float rollFactor)
        {
            // leaning forward
            if(pitch > 0)
            {
                SetGravityMorphsToZero(AngleTypes.LEAN_BACK);
                // upright
                if(pitch <= 90)
                {
                    SetGravityMorphsToZero(AngleTypes.UPSIDE_DOWN);
                    DoAdjustMorphs(AngleTypes.LEAN_FORWARD, CalcUtils.Remap(pitch, rollFactor));
                    DoAdjustMorphs(AngleTypes.UPRIGHT, CalcUtils.Remap(90 - pitch, rollFactor));
                }
                // upside down
                else
                {
                    SetGravityMorphsToZero(AngleTypes.UPRIGHT);
                    DoAdjustMorphs(AngleTypes.LEAN_FORWARD, CalcUtils.Remap(180 - pitch, rollFactor));
                    DoAdjustMorphs(AngleTypes.UPSIDE_DOWN, CalcUtils.Remap(pitch - 90, rollFactor));
                }
            }
            // leaning back
            else
            {
                SetGravityMorphsToZero(AngleTypes.LEAN_FORWARD);
                // upright
                if(pitch > -90)
                {
                    SetGravityMorphsToZero(AngleTypes.UPSIDE_DOWN);
                    DoAdjustMorphs(AngleTypes.LEAN_BACK, CalcUtils.Remap(Mathf.Abs(pitch), rollFactor));
                    DoAdjustMorphs(AngleTypes.UPRIGHT, CalcUtils.Remap(90 - Mathf.Abs(pitch), rollFactor));
                }
                // upside down
                else
                {
                    SetGravityMorphsToZero(AngleTypes.UPRIGHT);
                    DoAdjustMorphs(AngleTypes.LEAN_BACK, CalcUtils.Remap(180 - Mathf.Abs(pitch), rollFactor));
                    DoAdjustMorphs(AngleTypes.UPSIDE_DOWN, CalcUtils.Remap(Mathf.Abs(pitch) - 90, rollFactor));
                }
            }
        }

        void AdjustPhysicsForRoll(float roll, float rollFactor = 1)
        {
            float effect = CalcUtils.Remap(roll, rollFactor);
            gravityPhysics
                .Where(it => it.AngleType == AngleTypes.ROLL)
                .ToList().ForEach(it => it.UpdateVal(effect, ScaleFactor(), softness.val));
        }

        void AdjustPhysicsForPitch(float pitch, float rollFactor)
        {
            float effect = CalcUtils.Remap(pitch, rollFactor);
            gravityPhysics
                .Where(it => it.AngleType == AngleTypes.PITCH)
                .ToList().ForEach(it => it.UpdateVal(effect, ScaleFactor(), softness.val));
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
        void UndoMorphTweaks()
        {
            example1Morphs.ForEach(it => it.Reset());
            example2Morphs.ForEach(it => it.Reset());
            example3Morphs.ForEach(it => it.Reset());
            example4Morphs.ForEach(it => it.Reset());
        }

        void DoAdjustMorphs(string type, float effect)
        {
            gravityMorphs
                .Where(it => it.Multipliers.ContainsKey(type))
                .ToList().ForEach(it => it.UpdatePitchVal(type, effect, ScaleFactor(), softness.val, sagMultiplier.val));
        }

        void DoAdjustMorphsForRoll(string type, float effect)
        {
            gravityMorphs
                .Where(it => it.Multipliers.ContainsKey(type))
                .ToList().ForEach(it => it.UpdateRollVal(type, effect, ScaleFactor(), softness.val, sagMultiplier.val));
        }

        void OnDestroy()
        {
            gravityMorphs.ForEach(it => it.Reset());
            nippleErectionMorphs.ForEach(it => it.Reset());
        }

        void OnDisable()
        {
            gravityMorphs.ForEach(it => it.Reset());
            nippleErectionMorphs.ForEach(it => it.Reset());
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
            //physicsDebugInfo.SetVal(
            //    $"{FormatNameValueString("mass", breastControl.mass, padRight: 25)}\n" +
            //    $"{FormatNameValueString("center of g", breastControl.centerOfGravityPercent, padRight: 25)}\n" +
            //    $"{FormatNameValueString("spring", breastControl.spring, padRight: 25)}\n" +
            //    $"{FormatNameValueString("damper", breastControl.damper, padRight: 25)}\n" +
            //    $"{FormatNameValueString("in/out spr", breastControl.positionSpringZ, padRight: 25)}\n" +
            //    $"{FormatNameValueString("in/out dmp", breastControl.positionDamperZ, padRight: 25)}\n" +
            //    $"{FormatNameValueString("collider radius", breastPhysicsMesh.softVerticesColliderRadius, padRight: 25)}\n" +
            //    $"{FormatNameValueString("back force", breastPhysicsMesh.softVerticesBackForce, padRight: 25)}\n" +
            //    $"{FormatNameValueString("back force max", breastPhysicsMesh.softVerticesBackForceMaxForce, padRight: 25)}\n" +
            //    $"{FormatNameValueString("back force thres", breastPhysicsMesh.softVerticesBackForceThresholdDistance, padRight: 25)}\n" +
            //    $"{FormatNameValueString("fat spring", breastPhysicsMesh.softVerticesCombinedSpring, padRight: 25)}\n" +
            //    $"{FormatNameValueString("fat damper", breastPhysicsMesh.softVerticesCombinedDamper, padRight: 25)}\n" +
            //    $"{FormatNameValueString("fat mass", breastPhysicsMesh.softVerticesMass, padRight: 25)}\n" +
            //    $"{FormatNameValueString("distance limit", breastPhysicsMesh.softVerticesNormalLimit, padRight: 25)}\n" +
            //    $"{FormatNameValueString("main spring", mainSpring.val, padRight: 25)}\n" +
            //    $"{FormatNameValueString("main damper", mainDamper.val, padRight: 25)}\n" +
            //    $"{FormatNameValueString("outer spring", outerSpring.val, padRight: 25)}\n" +
            //    $"{FormatNameValueString("outer damper", outerDamper.val, padRight: 25)}\n" +
            //    $"{FormatNameValueString("areola spring", areolaSpring.val, padRight: 25)}\n" +
            //    $"{FormatNameValueString("areola damper", areolaDamper.val, padRight: 25)}\n" +
            //    $"{FormatNameValueString("nipple spring", nippleSpring.val, padRight: 25)}\n" +
            //    $"{FormatNameValueString("nipple damper", nippleDamper.val, padRight: 25)}\n"
            //);
            string text = "";
            text += "BASE PHYSICS\n";
            gravityPhysics.ForEach((it) =>
            {
                text = text + FormatNameValueString(it.Name, it.Setting.val, padRight: 25) + "\n";
            });

            text += "GRAVITY PHYSICS\n";
            gravityPhysics.ForEach((it) =>
            {
                text = text + FormatNameValueString(it.Name, it.Setting.val, padRight: 25) + "\n";
            });

            physicsDebugInfo.SetVal(text);
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
            double rounded = CalcUtils.RoundToDecimals(value, roundFactor);
            string printName = StripPrefix(name, "TM_").PadRight(padRight, ' ');
            string printValue = normalize ? NormalizeNumberFormat(rounded) : $"{rounded}";
            return string.Format("{0} {1}", printName, printValue);
        }

        public static string StripPrefix(string text, string prefix)
        {
            return text.StartsWith(prefix) ? text.Substring(prefix.Length) : text;
        }

        string NormalizeNumberFormat(double value)
        {
            string formatted = string.Format("{0:000.00}", value);
            return value >= 0 ? $" {formatted}" : formatted;
        }
        #endregion
    }

    class SizeMorphConfig : MorphConfig
    {
        public SizeMorphConfig(string name, float baseMulti, float startValue = 0.00f) : base(name, baseMulti, startValue) { }

        public void UpdateVal(float scale)
        {
            Morph.morphValue = StartValue + BaseMulti * scale;
        }
    }

    class ExampleMorphConfig : MorphConfig
    {
        public ExampleMorphConfig(string name, float baseMulti, float startValue = 0.00f) : base(name, baseMulti, startValue) { }

        public void UpdateVal()
        {
            Morph.morphValue = BaseMulti;
        }
    }

    class NippleErectionMorphConfig : MorphConfig
    {
        public NippleErectionMorphConfig(string name, float baseMulti, float startValue = 0.00f) : base(name, baseMulti, startValue) { }

        public void UpdateVal(float nippleErection)
        {
            Morph.morphValue = StartValue + BaseMulti * nippleErection;
        }
    }

    // TODO refactor to not use Dictionary
    class GravityMorphConfig
    {
        public string Name { get; set; }
        public DAZMorph Morph { get; set; }
        public Dictionary<string, float?[]> Multipliers { get; set; }

        public GravityMorphConfig(string name, Dictionary<string, float?[]> multipliers)
        {
            Name = name;
            Morph = Globals.MORPH_UI.GetMorphByDisplayName(name);
            Multipliers = multipliers;
            if (Morph == null)
            {
                SuperController.LogError($"everlaster.TittyMagic.{nameof(GravityMorphConfig)}: Morph with name {name} not found!");
            }
        }

        public void UpdatePitchVal(string type, float effect, float scale, float softness, float sag)
        {
            float?[] m = Multipliers[type];

            // m[0] is the base multiplier for the morph in this type (UPRIGHT etc.)
            // m[1] scales the breast softness slider for this base multiplier
            //      - if null, slider setting is ignored
            // m[2] scales the size calibration slider for this base multiplier
            //      - if null, slider setting is ignored
            float softnessFactor = m[1].HasValue ? (float) m[1] * softness : 1;
            float scaleFactor = m[2].HasValue ? scale * (float) m[2] : 1;
            float morphValue = sag * (float) m[0] * (
                (softnessFactor * effect / 2) +
                (scaleFactor * effect / 2)
            );

            if(morphValue > 0)
            {
                Morph.morphValue = morphValue >= 1.33f ? 1.33f : morphValue;
            }
            else
            {
                Morph.morphValue = morphValue < -1.33f ? -1.33f : morphValue;
            }
        }

        public void UpdateRollVal(string type, float effect, float scale, float softness, float sag)
        {
            float?[] m = Multipliers[type];

            float softnessFactor = m[1].HasValue ? (float) m[1] * softness : 1;
            float scaleFactor = m[2].HasValue ? scale * (float) m[2] : 1;
            float sagMultiplierVal = sag >= 1 ?
                1 + (sag - 1) / 2 :
                sag;
            float morphValue = sagMultiplierVal * (float) m[0] * (
                (softnessFactor * effect / 2) +
                (scaleFactor * effect / 2)
            );

            if(morphValue > 0)
            {
                Morph.morphValue = morphValue >= 1.33f ? 1.33f : morphValue;
            }
            else
            {
                Morph.morphValue = morphValue < -1.33f ? -1.33f : morphValue;
            }
        }

        public void Reset()
        {
            Morph.morphValue = 0;
        }
    }

    class GravityPhysicsConfig : PhysicsConfig
    {
        public GravityPhysicsConfig(string name, string angleType, float min, float max, float? scaleMultiplier, float? softnessMultiplier) : base(name, angleType, min, max, scaleMultiplier, softnessMultiplier) { }

        public void InitStorable()
        {
            Setting = Globals.BREAST_CONTROL.GetFloatJSONParam(Name);
            if(Setting == null)
            {
                SuperController.LogError($"everlaster.TittyMagic.{nameof(GravityPhysicsConfig)}: BreastControl float param with name {Name} not found!");
            }
        }

        public void UpdateVal(float effect, float scale, float softness)
        {
            float scaleFactor = ScaleMultiplier.HasValue ? (float) ScaleMultiplier * scale : 1;
            float softnessFactor = SoftnessMultiplier.HasValue ? (float) SoftnessMultiplier * softness : 1;
            float value = (scaleFactor * effect / 2) + (softnessFactor * effect / 2);

            Setting.SetVal(Mathf.SmoothStep(Min, Max, value));
        }
    }
}
