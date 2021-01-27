//#define DEBUG
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace everlaster
{
    class TittyMagic : MVRScript
    {
        private bool enableUpdate = true;

        private Transform chest;
        private DAZCharacterSelector geometry;

        private List<DAZPhysicsMeshSoftVerticesSet> rightBreastMainGroupSets;
        private Mesh inMemoryMesh;
        private float fatDensity = 0.89f; // g/cm^3
        private float breastMass;
        private float massMin = 0.100f;
        private float massMax = 2.000f;
        private float softVolume; // cm^3; spheroid volume estimation of right breast

        private AtomScaleListener atomScaleListener;
        private BreastMorphListener breastMorphListener;

        private GravityMorphHandler gravityMorphH;
        private NippleErectionMorphHandler nippleMorphH;
        private StaticPhysicsHandler staticPhysicsH;
        private GravityPhysicsHandler gravityPhysicsH;

        private JSONStorableString titleUIText;
        private JSONStorableString statusUIText;
        //private JSONStorableString logUIArea;

        //registered storables
        private JSONStorableString pluginVersion;
        private JSONStorableFloat softness;
        private float softnessMax = 3.0f;

        private JSONStorableFloat sagMultiplier;

        private JSONStorableFloat nippleErection;

#if DEBUG
        private JSONStorableBool useGravityPhysics;
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

                atomScaleListener = new AtomScaleListener(containingAtom.GetStorableByID("rescaleObject").GetFloatJSONParam("scale"));
                breastMorphListener = new BreastMorphListener(geometry.morphBank1.morphs);
#if DEBUG
                breastMorphListener.DumpStatus();
                useGravityPhysics = NewToggle("Use gravity physics", false);
                useGravityPhysics.val = true;
#endif

                gravityMorphH = new GravityMorphHandler();
                nippleMorphH = new NippleErectionMorphHandler();
                gravityPhysicsH = new GravityPhysicsHandler();
                staticPhysicsH = new StaticPhysicsHandler();

                InitPluginUILeft();
                InitPluginUIRight();
                InitSliderListeners();

                SetPhysicsDefaults();
                UpdateMassEstimate(atomScaleListener.Value, updateUIStatus: true);
                staticPhysicsH.FullUpdate(breastMass, softness.val, softnessMax, nippleErection.val);
            }
            catch(Exception e)
            {
                Log.Error("Exception caught: " + e);
            }
        }

        #region User interface
        void InitPluginUILeft()
        {
            bool rightSide = false;
            titleUIText = NewTextField("titleText", 36, 100, rightSide);
            titleUIText.SetVal($"{nameof(TittyMagic)}\n<size=28>v{pluginVersion.val}</size>");

            // doesn't just init UI, also variables...
            softness = NewFloatSlider("Breast softness", 1.5f, 0.5f, softnessMax, rightSide);
            sagMultiplier = NewFloatSlider("Sag multiplier", 1.0f, 0f, 2.0f, rightSide);

            CreateNewSpacer(10f);

            nippleErection = NewFloatSlider("Erect nipples", 0f, 0f, 1.0f, rightSide);

#if DEBUG
            UIDynamicTextField angleInfoField = CreateTextField(baseDebugInfo, rightSide);
            angleInfoField.height = 125;
            angleInfoField.UItext.fontSize = 26;
            UIDynamicTextField physicsInfoField = CreateTextField(physicsDebugInfo, rightSide);
            physicsInfoField.height = 505;
            physicsInfoField.UItext.fontSize = 26;
#endif
        }

        void InitPluginUIRight()
        {
            bool rightSide = true;
            statusUIText = NewTextField("statusText", 28, 100, rightSide);
#if DEBUG
            UIDynamicTextField morphInfo = CreateTextField(morphDebugInfo, rightSide);
            morphInfo.height = 1075;
            morphInfo.UItext.fontSize = 26;
#else
            JSONStorableString usageInfo = NewTextField("Usage Info Area", 28, 415, rightSide);
            string usage = "\n";
            usage += "Breast softness controls soft physics and affects the amount " +
                "of morph-based sag in different orientations or poses.\n\n";
            usage += "Sag multiplier adjusts the sag produced by Breast softness " +
                "independently of soft physics.\n\n";
            usage += "Set breast morphs to defaults before applying example settings.";
            usageInfo.SetVal(usage);
            //CreateNewSpacer(10f, rightSide);

            //logUIArea = NewTextField("Log Info Area", 28, 630, rightSide);
            //logUIArea.SetVal("\n");
#endif
        }

        JSONStorableFloat NewFloatSlider(string paramName, float startingValue, float minValue, float maxValue, bool rightSide)
        {
            JSONStorableFloat storable = new JSONStorableFloat(paramName, startingValue, minValue, maxValue);
            storable.storeType = JSONStorableParam.StoreType.Physical;
            RegisterFloat(storable);
            CreateSlider(storable, rightSide);
            return storable;
        }

        JSONStorableString NewTextField(string paramName, int fontSize, int height = 100, bool rightSide = false)
        {
            JSONStorableString storable = new JSONStorableString(paramName, "");
            UIDynamicTextField textField = CreateTextField(storable, rightSide);
            textField.UItext.fontSize = fontSize;
            textField.height = height;
            return storable;
        }

        JSONStorableBool NewToggle(string paramName, bool rightSide = false)
        {
            JSONStorableBool storable = new JSONStorableBool(paramName, rightSide);
            CreateToggle(storable, false);
            //RegisterBool(storable);
            return storable;
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
                    if(breastMorphListener.Changed() || atomScaleListener.Changed())
                    {
                        StartCoroutine(RefreshStaticPhysics(atomScaleListener.Value));
                    }

                    float roll = Calc.Roll(chest.rotation);
                    float pitch = Calc.Pitch(chest.rotation);
                    float scaleVal = Calc.LegacyScale(breastMass);

                    gravityMorphH.Update(roll, pitch, scaleVal, softness.val, sagMultiplier.val);
#if DEBUG
                    if(useGravityPhysics.val)
                    {
                        gravityPhysicsH.Update(roll, pitch, scaleVal, softness.val);
                    }
                    else
                    {
                        gravityPhysicsH.ResetAll();
                    }

                    SetBaseDebugInfo(roll, pitch);
                    morphDebugInfo.SetVal(gravityMorphH.GetStatus());
                    physicsDebugInfo.SetVal(staticPhysicsH.GetStatus() + gravityPhysicsH.GetStatus());
#else
                    gravityPhysicsH.Update(roll, pitch, scaleVal, softness.val);
#endif
                }
            }
            catch(Exception e)
            {
                Log.Error("Exception caught: " + e);
                enableUpdate = false;
            }
        }

        IEnumerator RefreshStaticPhysics(float atomScale)
        {
            while(breastMorphListener.Changed())
            {
                yield return null;
            }

            // Iterate the update a few times because each update changes breast shape and thereby the mass estimate.
            for(int i = 0; i < 6; i++)
            {
                // update only non-soft physics settings to improve performance
                UpdateMassEstimate(atomScale);
                staticPhysicsH.UpdateMainPhysics(breastMass, softness.val, softnessMax);
                if(i > 0)
                {
                    yield return new WaitForSeconds(0.12f);
                }
            }

            UpdateMassEstimate(atomScale, updateUIStatus: true);
            staticPhysicsH.FullUpdate(breastMass, softness.val, softnessMax, nippleErection.val);
        }

        void UpdateMassEstimate(float atomScale, bool updateUIStatus = false)
        {
            Vector3[] vertices = rightBreastMainGroupSets
                .Select(it => it.currentPosition).ToArray();

            inMemoryMesh.vertices = vertices;
            inMemoryMesh.RecalculateBounds();
            softVolume = CupVolumeCalc.EstimateVolume(inMemoryMesh.bounds.size, atomScale);
            float mass = (softVolume * fatDensity) / 1000;

            if(mass > massMax)
            {
                breastMass = massMax;
                if(updateUIStatus)
                {
                    float excess = Calc.RoundToDecimals(mass - massMax, 1000f);
                    statusUIText.SetVal(massExcessStatus(excess));
                }
            }
            else if(mass < massMin)
            {
                breastMass = massMin;
                if(updateUIStatus)
                {
                    float shortage = Calc.RoundToDecimals(massMin - mass, 1000f);
                    statusUIText.SetVal(massShortageStatus(shortage));
                }
            }
            else
            {
                breastMass = mass;
                if(updateUIStatus)
                {
                    statusUIText.SetVal("");
                }
            } 
        }

        string massExcessStatus(float value)
        {
            Color color = Color.Lerp(
                new Color(0.5f, 0.5f, 0.0f, 1f),
                Color.red,
                value
            );
            return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}><size=28>" +
                $"Estimated mass is <b>{value}</b> over the 2.000 maximum.\n" +
                $"</size></color>";
        }

        string massShortageStatus(float value)
        {
            Color color = Color.Lerp(
                new Color(0.5f, 0.5f, 0.0f, 1f),
                Color.red,
                value*10
            );
            return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}><size=28>" +
                $"Estimated mass is <b>{value}</b> below the 0.100 minimum.\n" +
                $"</size></color>";
        }

        void OnDestroy()
        {
            gravityPhysicsH.ResetAll();
            gravityMorphH.ResetAll();
            nippleMorphH.ResetAll();
        }

        void OnDisable()
        {
            gravityPhysicsH.ResetAll();
            gravityMorphH.ResetAll();
            nippleMorphH.ResetAll();
        }

#if DEBUG
        void SetBaseDebugInfo(float roll, float pitch)
        {
            baseDebugInfo.SetVal(
                $"{Formatting.NameValueString("Roll", roll, 100f, 15)}\n" +
                $"{Formatting.NameValueString("Pitch", pitch, 100f, 15)}\n" +
                $"volume: {softVolume}\n"
            );
        }
#endif
    }
}
