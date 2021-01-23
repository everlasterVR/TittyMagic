//#define DEBUGINFO
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace everlaster
{
    class TittyMagic : MVRScript
    {
        private bool enableUpdate;

        private Transform chest;
        private DAZCharacterSelector geometry;

        private List<DAZPhysicsMeshSoftVerticesSet> rightBreastMainGroupSets;
        private Mesh inMemoryMesh;
        private float fatDensity = 0.89f; // g/cm^3
        private float breastMass;
        private float massMax = 2.000f;
        private float softVolume; // cm^3; sphere volume estimation of right breast

        private GravityMorphHandler gravityMorphH;
        private SizeMorphHandler sizeMorphH;
        private ExampleMorphHandler exampleMorphH;
        private NippleErectionMorphHandler nippleMorphH;
        private StaticPhysicsHandler staticPhysicsH;
        private GravityPhysicsHandler gravityPhysicsH;

        private BreastMorphListener breastMorphListener;

        //storables
        private JSONStorableString pluginVersion;
        protected JSONStorableFloat softness;
        private float softnessDefault = 1.5f;
        private float softnessMax = 3.0f;

        protected JSONStorableFloat sagMultiplier;
        private float sagDefault = 1.2f;

        protected JSONStorableFloat nippleErection;
        private float nippleErectionDefault = 0.25f;

        protected JSONStorableString UILog;

#if DEBUGINFO
        protected JSONStorableString baseDebugInfo = new JSONStorableString("Base Debug Info", "");
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
                chest = containingAtom.GetStorableByID("chest").transform;
                geometry = containingAtom.GetStorableByID("geometry") as DAZCharacterSelector;
                rightBreastMainGroupSets = breastPhysicsMesh.softVerticesGroups
                    .Find(it => it.name == "right")
                    .softVerticesSets;
                inMemoryMesh = new Mesh();

                Globals.BREAST_CONTROL = breastControl;
                Globals.BREAST_PHYSICS_MESH = breastPhysicsMesh;
                Globals.MORPH_UI = geometry.morphsControlUI;

                breastMorphListener = new BreastMorphListener(geometry.femaleMorphBank1.morphs);
#if DEBUGINFO
                breastMorphListener.DumpStatus();
#endif

                gravityMorphH = new GravityMorphHandler();
                sizeMorphH = new SizeMorphHandler();
                exampleMorphH = new ExampleMorphHandler();
                nippleMorphH = new NippleErectionMorphHandler();
                gravityPhysicsH = new GravityPhysicsHandler();
                staticPhysicsH = new StaticPhysicsHandler();

                InitPluginUILeft();
                InitPluginUIRight();
                InitSliderListeners();

                SetPhysicsDefaults();
                UpdateMassEstimate();
                staticPhysicsH.FullUpdate(breastMass, softness.val, softnessMax, nippleErection.val);

                enableUpdate = true;
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
            softness = NewFloatSlider("Breast softness", softnessDefault, 0.5f, softnessMax);
            sagMultiplier = NewFloatSlider("Sag multiplier", sagDefault, 0f, 2.0f);
            nippleErection = NewFloatSlider("Erect nipples", nippleErectionDefault, 0f, 1.0f);

            CreateNewSpacer(10f);

            JSONStorableString presetsH2 = NewTextField("Example Settings", 34);
            presetsH2.SetVal("\nExample settings");

            CreateExampleButtons();
        }

        void InitPluginUIRight()
        {
#if DEBUGINFO
            UIDynamicTextField angleInfoField = CreateTextField(baseDebugInfo, true);
            angleInfoField.height = 125;
            angleInfoField.UItext.fontSize = 26;
            UIDynamicTextField physicsInfoField = CreateTextField(physicsDebugInfo, true);
            physicsInfoField.height = 480;
            physicsInfoField.UItext.fontSize = 26;
#else
            JSONStorableString usageInfo = NewTextField("Usage Info Area", 28, 505, true);
            string usage = "\n";
            usage += "Breast softness controls soft physics and affects the amount " +
                "of morph-based sag in different orientations or poses.\n\n";
            usage += "Sag multiplier adjusts the sag produced by Breast softness " +
                "independently of soft physics.\n\n";
            usage += "Set breast morphs to defaults before applying example settings.";
            usageInfo.SetVal(usage);
#endif
#if DEBUGINFO
            UIDynamicTextField morphInfo = CreateTextField(morphDebugInfo, true);
            morphInfo.height = 565;
            morphInfo.UItext.fontSize = 26;
#else
            CreateNewSpacer(10f, true);

            UILog = NewTextField("Log Info Area", 28, 655, true);
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
                sizeMorphH.Update(1.65f);
                softness.val = 2.10f;
                sagMultiplier.val = 1.60f;

                exampleMorphH.ResetMorphs();
                exampleMorphH.Update("bigNaturals");

                Log.AppendTo(UILog, exampleMorphH.GetStatus("bigNaturals"));
            });

            UIDynamicButton smallAndPerky = CreateButton(exampleMorphH.ExampleNames["smallAndPerky"]);
            smallAndPerky.button.onClick.AddListener(() =>
            {
                sizeMorphH.Update(0.30f);
                softness.val = 1.10f;
                sagMultiplier.val = 1.80f;

                exampleMorphH.ResetMorphs();
                exampleMorphH.Update("smallAndPerky");

                Log.AppendTo(UILog, exampleMorphH.GetStatus("smallAndPerky"));
            });

            UIDynamicButton mediumImplants = CreateButton(exampleMorphH.ExampleNames["mediumImplants"]);
            mediumImplants.button.onClick.AddListener(() =>
            {
                sizeMorphH.Update(0.75f);
                softness.val = 0.60f;
                sagMultiplier.val = 0.80f;

                exampleMorphH.ResetMorphs();
                exampleMorphH.Update("mediumImplants");
                
                Log.AppendTo(UILog, exampleMorphH.GetStatus("mediumImplants"));
            });

            UIDynamicButton hugeAndSoft = CreateButton(exampleMorphH.ExampleNames["hugeAndSoft"]);
            hugeAndSoft.button.onClick.AddListener(() =>
            {
                sizeMorphH.Update(3.00f);
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
                sizeMorphH.Update(0.80f);
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
            softness.slider.onValueChanged.AddListener((float val) =>
            {
                staticPhysicsH.FullUpdate(breastMass, val, softnessMax, nippleErection.val);
            });
            sagMultiplier.slider.onValueChanged.AddListener((float val) =>
            {
                staticPhysicsH.FullUpdate(breastMass, softness.val, softnessMax, nippleErection.val);
            });
            nippleErection.slider.onValueChanged.AddListener((float val) =>
            {
                staticPhysicsH.FullUpdate(breastMass, softness.val, softnessMax, nippleErection.val);
                nippleMorphH.Update(val);
            });
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

        public void Update()
        {
            try
            {
                if (enableUpdate)
                {
                    if(breastMorphListener.Changed())
                    {
                        StartCoroutine(RefreshStaticPhysics());
                    }

                    float roll = Calc.Roll(chest.rotation);
                    float pitch = Calc.Pitch(chest.rotation);

                    // roughly estimate the legacy scale value from automatically calculated mass
                    float scaleVal = (breastMass - 0.20f) * 1.60f;

                    gravityMorphH.Update(roll, pitch, scaleVal, softness.val, sagMultiplier.val);
                    //gravityPhysicsH.Update(roll, pitch, scaleVal, softness.val);
#if DEBUGINFO
                    SetBaseDebugInfo(roll, pitch);
                    SetMorphDebugInfo();
                    SetPhysicsDebugInfo();
#endif
                }
            }
            catch(Exception e)
            {
                Log.Error("Exception caught: " + e);
                enableUpdate = false;
            }
        }

        IEnumerator RefreshStaticPhysics()
        {
            while(breastMorphListener.Changed())
            {
                yield return null;
            }

            // Iterate the update a few times because each update changes breast shape and thereby the mass estimate.
            for(int i = 0; i < 10; i++)
            {
                // update only non-soft physics settings to improve of performance
                staticPhysicsH.UpdateMainPhysics(breastMass, softness.val, softnessMax);
                UpdateMassEstimate();
                if(i > 0)
                {
                    yield return new WaitForSeconds(0.10f);
                }
            }

            staticPhysicsH.FullUpdate(breastMass, softness.val, softnessMax, nippleErection.val);
        }

        void UpdateMassEstimate()
        {
            Vector3[] vertices = rightBreastMainGroupSets
                .Select(it => it.currentPosition).ToArray();

            inMemoryMesh.vertices = vertices;
            inMemoryMesh.RecalculateBounds();
            softVolume = (float) Calc.OblateShperoidVolumeCM3(inMemoryMesh.bounds.size);
            float mass = (softVolume * fatDensity) / 1000;
            breastMass = mass > massMax ? massMax : mass;
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
        void SetBaseDebugInfo(float roll, float pitch)
        {
            float x = (float) Calc.RoundToDecimals(inMemoryMesh.bounds.extents.x, 1000f);
            float y = (float) Calc.RoundToDecimals(inMemoryMesh.bounds.extents.y, 1000f);
            float z = (float) Calc.RoundToDecimals(inMemoryMesh.bounds.extents.z, 1000f);
            baseDebugInfo.SetVal(
                $"{Formatting.NameValueString("Roll", roll, 100f, 15)}\n" +
                $"{Formatting.NameValueString("Pitch", pitch, 100f, 15)}\n" +
                $"volume: {softVolume}\n" +
                $"extents: {x}, {y}, {z}"
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
