//#define DEBUGINFO
using System;
using UnityEngine;

namespace everlaster
{
    class TittyMagic : MVRScript
    {
        private bool enableUpdate;
        private bool atomScaleListenerIsSet = false;

        private Transform chest;
        private DAZCharacterSelector geometry;

        private SizeMorphConfig bodyScaleMorph;

        private GravityMorphHandler gravityMorphH;
        private SizeMorphHandler sizeMorphH;
        private ExampleMorphHandler exampleMorphH;
        private NippleErectionMorphHandler nippleMorphH;
        private StaticPhysicsHandler staticPhysicsH;
        private GravityPhysicsHandler gravityPhysicsH;

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
        protected JSONStorableString UILog;

#if DEBUGINFO
        protected JSONStorableString angleDebugInfo = new JSONStorableString("Angle Debug Info", "");
        protected JSONStorableString physicsDebugInfo = new JSONStorableString("Physics Debug Info", "");
        protected JSONStorableString morphDebugInfo = new JSONStorableString("Morph Debug Info", "");
#endif

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

                AdjustJoints breastControl = containingAtom.GetStorableByID("BreastControl") as AdjustJoints;
                DAZPhysicsMesh breastPhysicsMesh = containingAtom.GetStorableByID("BreastPhysicsMesh") as DAZPhysicsMesh;
                atomScale = containingAtom.GetStorableByID("rescaleObject").GetFloatJSONParam("scale");
                geometry = containingAtom.GetStorableByID("geometry") as DAZCharacterSelector;
                chest = containingAtom.GetStorableByID("chest").transform;

                Globals.BREAST_CONTROL = breastControl;
                Globals.BREAST_PHYSICS_MESH = breastPhysicsMesh;
                Globals.MORPH_UI = geometry.morphsControlUI;
                Globals.UPDATE_ENABLED = true;

                gravityMorphH = new GravityMorphHandler();
                sizeMorphH = new SizeMorphHandler();
                exampleMorphH = new ExampleMorphHandler();
                nippleMorphH = new NippleErectionMorphHandler();
                gravityPhysicsH = new GravityPhysicsHandler();
                staticPhysicsH = new StaticPhysicsHandler();

                InitPluginUILeft();
                InitPluginUIRight();
                InitSliderListeners();

                InitBuiltInMorphs();
                ResolveAtomScaleFactor(atomScale.val);

                SetPhysicsDefaults();
                staticPhysicsH.Update(scale.val, scaleMin, softness.val, softnessMax, atomScaleFactor, nippleErection.val);

                enableUpdate = Globals.UPDATE_ENABLED;
            }
            catch(Exception e)
            {
                Log.Error("Exception caught: " + e);
            }
        }

        #region User interface
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

            UILog = NewTextField("Log Info Area", 28, 515, true);
            UILog.SetVal("\n");
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
            UIDynamicButton bigNaturals = CreateButton(exampleMorphH.ExampleNames["bigNaturals"]);
            bigNaturals.button.onClick.AddListener(() =>
            {
                scale.val = 1.65f;
                softness.val = 2.10f;
                sagMultiplier.val = 1.60f;

                exampleMorphH.ResetMorphs();
                exampleMorphH.Update("bigNaturals");
                Log.AppendTo(UILog, exampleMorphH.GetStatus("bigNaturals"));
            });

            UIDynamicButton smallAndPerky = CreateButton(exampleMorphH.ExampleNames["smallAndPerky"]);
            smallAndPerky.button.onClick.AddListener(() =>
            {
                scale.val = 0.30f;
                softness.val = 1.10f;
                sagMultiplier.val = 1.80f;

                exampleMorphH.ResetMorphs();
                exampleMorphH.Update("smallAndPerky");
                Log.AppendTo(UILog, exampleMorphH.GetStatus("smallAndPerky"));
            });

            UIDynamicButton mediumImplants = CreateButton(exampleMorphH.ExampleNames["mediumImplants"]);
            mediumImplants.button.onClick.AddListener(() =>
            {
                scale.val = 0.75f;
                softness.val = 0.60f;
                sagMultiplier.val = 0.80f;

                exampleMorphH.ResetMorphs();
                exampleMorphH.Update("mediumImplants");
                Log.AppendTo(UILog, exampleMorphH.GetStatus("mediumImplants"));
            });

            UIDynamicButton hugeAndSoft = CreateButton(exampleMorphH.ExampleNames["hugeAndSoft"]);
            hugeAndSoft.button.onClick.AddListener(() =>
            {
                scale.val = 3.00f;
                softness.val = 2.80f;
                sagMultiplier.val = 2.00f;

                exampleMorphH.ResetMorphs();
                exampleMorphH.Update("hugeAndSoft");
                Log.AppendTo(UILog, exampleMorphH.GetStatus("hugeAndSoft"));
            });

            CreateNewSpacer(10f);

            UIDynamicButton defaults = CreateButton("Undo example settings");
            defaults.button.onClick.AddListener(() =>
            {
                scale.val = scaleDefault;
                softness.val = softnessDefault;
                sagMultiplier.val = sagDefault;

                exampleMorphH.ResetMorphs();
                Log.AppendTo(UILog, "> Example tweaks zeroed and sliders reset.");
            });
        }

        void CreateNewSpacer(float height, bool rightSide = false)
        {
            UIDynamic spacer = CreateSpacer(rightSide);
            spacer.height = height;
        }
        #endregion

        void InitSliderListeners()
        {
            scale.slider.onValueChanged.AddListener((float val) =>
            {
                staticPhysicsH.Update(val, scaleMin, softness.val, softnessMax, atomScaleFactor, nippleErection.val);
            });
            softness.slider.onValueChanged.AddListener((float val) =>
            {
                staticPhysicsH.Update(scale.val, scaleMin, val, softnessMax, atomScaleFactor, nippleErection.val);
            });
            sagMultiplier.slider.onValueChanged.AddListener((float val) =>
            {
                staticPhysicsH.Update(scale.val, scaleMin, softness.val, softnessMax, atomScaleFactor, nippleErection.val);
            });
            nippleErection.slider.onValueChanged.AddListener((float val) =>
            {
                staticPhysicsH.Update(scale.val, scaleMin, softness.val, softnessMax, atomScaleFactor, nippleErection.val);
            });
        }

        void AtomScaleListener(float val)
        {
            ResolveAtomScaleFactor(atomScale.val);
            staticPhysicsH.Update(scale.val, scaleMin, softness.val, softnessMax, atomScaleFactor, nippleErection.val);
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

        // TODO merge
        void SetPhysicsDefaults()
        {
            // In/Out auto morphs off
            containingAtom.GetStorableByID("BreastInOut").SetBoolParamValue("enabled", false);
            // Hard colliders on
            geometry.useAuxBreastColliders = true;
            staticPhysicsH.SetPhysicsDefaults();
        }

        void ResolveAtomScaleFactor(float value)
        {
            if (value == 1)
            {
                atomScaleFactor = value;
                return;
            }
            
            if (value > 1)
            {
                atomScaleFactor = value / Calc.AtomScaleAdjustment(value);
                return;
            }

            if (value < 1)
            {
                if(value <= 0.5)
                {
                    atomScaleFactor = 0.5f * Calc.AtomScaleAdjustment(0.5f);
                    atomScale.slider.onValueChanged.RemoveListener(AtomScaleListener);
                    Log.Message(
                        "Person Atom Scale values lower than 0.5 are not fully compatible - " +
                        "this plugin will now behave as if it is 0.5. " +
                        "Reload the plugin after returning it to above 0.5."
                    );
                    return;
                }

                atomScaleFactor = value * Calc.AtomScaleAdjustment(value);
            }
        }

        public void Update()
        {
            TryInitGameUIListeners();

            try
            {
                if (enableUpdate)
                {
                    bodyScaleMorph.Morph.morphValue = 0;
                    sizeMorphH.Update((scale.val - scaleMin) / 0.9f);
                    nippleMorphH.Update(nippleErection.val);

                    float roll = Calc.Roll(chest.rotation);
                    float pitch = Calc.Pitch(chest.rotation);
                    float scaleFactor = atomScaleFactor * scale.val;

                    gravityMorphH.Update(roll, pitch, scaleFactor, softness.val, sagMultiplier.val);
                    //gravityPhysicsH.Update(roll, pitch, scaleFactor, softness.val);
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

        void TryInitGameUIListeners()
        {
            if (atomScale.slider != null && !atomScaleListenerIsSet)
            {
                // update physics settings in case Person atom's Scale is changed
                atomScale.slider.onValueChanged.AddListener(AtomScaleListener);
                atomScaleListenerIsSet = true;
            }
        }

        void OnDestroy()
        {
            gravityMorphH.ResetMorphs();
            nippleMorphH.ResetMorphs();
        }

        void OnDisable()
        {
            gravityMorphH.ResetMorphs();
            nippleMorphH.ResetMorphs();
        }

#if DEBUGINFO
        void SetAngleDebugInfo(float roll, float pitch)
        {
            angleDebugInfo.SetVal(
                $"{Formatting.NameValueString("Roll", roll, 100f, 15, true)}\n" +
                $"{Formatting.NameValueString("Pitch", pitch, 100f, 15, true)}"
            );
        }

        void SetPhysicsDebugInfo()
        {
            physicsDebugInfo.SetVal(staticPhysicsH.GetStatus() + gravityPhysicsH.GetStatus());
        }

        void SetMorphDebugInfo()
        {
            morphDebugInfo.SetVal(sizeMorphH.GetStatus() + gravityMorphH.GetStatus());
        }
#endif
    }
}
