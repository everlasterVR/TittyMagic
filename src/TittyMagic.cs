//#define SHOW_DEBUG
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
        private bool physicsUpdateInProgress = false;

        private Transform chest;
        private DAZCharacterSelector geometry;

        private List<DAZPhysicsMeshSoftVerticesSet> rightBreastMainGroupSets;
        private Mesh inMemoryMesh;
        private float massEstimate;
        private float massMin = 0.100f;
        private float massMax = 2.000f;
        private float softVolume; // cm^3; spheroid volume estimation of right breast
        private float gravityLogAmount;

        private AtomScaleListener atomScaleListener;
        private BreastMorphListener breastMorphListener;

        private GravityMorphHandler gravityMorphH;
        private NippleErectionMorphHandler nippleMorphH;
        private StaticPhysicsHandler staticPhysicsH;
        private GravityPhysicsHandler gravityPhysicsH;

        private JSONStorableString titleUIText;
        private JSONStorableString statusUIText;

        //registered storables
        private JSONStorableString pluginVersion;
        private JSONStorableFloat softness;
        private JSONStorableFloat gravity;
        private JSONStorableBool linkSoftnessAndGravity;
        private JSONStorableFloat nippleErection;

#if SHOW_DEBUG
        protected JSONStorableString baseDebugInfo = new JSONStorableString("Base Debug Info", "");
        protected JSONStorableString physicsDebugInfo = new JSONStorableString("Physics Debug Info", "");
        protected JSONStorableString morphDebugInfo = new JSONStorableString("Morph Debug Info", "");
#endif

        public override void Init()
        {
            try
            {
                pluginVersion = new JSONStorableString("Version", "2.0.0");
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

                gravityMorphH = new GravityMorphHandler();
                nippleMorphH = new NippleErectionMorphHandler();
                gravityPhysicsH = new GravityPhysicsHandler();
                staticPhysicsH = new StaticPhysicsHandler();

                InitPluginUILeft();
                InitPluginUIRight();
                InitSliderListeners();
                UpdateLogarithmicGravityAmount(gravity.val);

                SetPhysicsDefaults();
                StartCoroutine(RefreshStaticPhysics(atomScaleListener.Value));
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
            softness = NewFloatSlider("Breast softness", 1.5f, Const.SOFTNESS_MIN, Const.SOFTNESS_MAX, rightSide);
            gravity = NewFloatSlider("Breast gravity", 1.5f, Const.GRAVITY_MIN, Const.GRAVITY_MAX, rightSide);
            linkSoftnessAndGravity = NewToggle("Link softness and gravity", false);
            linkSoftnessAndGravity.val = true;

            CreateNewSpacer(10f);

            nippleErection = NewFloatSlider("Erect nipples", 0f, 0f, 1.0f, rightSide);

#if SHOW_DEBUG
            UIDynamicTextField angleInfoField = CreateTextField(baseDebugInfo, rightSide);
            angleInfoField.height = 125;
            angleInfoField.UItext.fontSize = 26;
            UIDynamicTextField physicsInfoField = CreateTextField(physicsDebugInfo, rightSide);
            physicsInfoField.height = 450;
            physicsInfoField.UItext.fontSize = 26;
#endif
        }

        void InitPluginUIRight()
        {
            bool rightSide = true;
            statusUIText = NewTextField("statusText", 28, 100, rightSide);
#if SHOW_DEBUG
            UIDynamicTextField morphInfo = CreateTextField(morphDebugInfo, rightSide);
            morphInfo.height = 1085;
            morphInfo.UItext.fontSize = 26;
#else
            JSONStorableString usage1Area = NewTextField("Usage Info Area 1", 28, 255, rightSide);
            string usage1 = "\n";
            usage1 += "Breast softness adjusts soft physics settings from very firm to very soft.\n\n";
            usage1 += "Breast gravity adjusts how much pose morphs shape the breasts in all orientations.";
            usage1Area.SetVal(usage1);
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
            RegisterBool(storable);
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
                if (linkSoftnessAndGravity.val)
                {
                    gravity.val = val;
                    UpdateLogarithmicGravityAmount(val);
                }
                staticPhysicsH.FullUpdate(massEstimate, val, nippleErection.val);
            });
            gravity.slider.onValueChanged.AddListener((float val) =>
            {
                if(linkSoftnessAndGravity.val)
                {
                    softness.val = val;
                }
                UpdateLogarithmicGravityAmount(val);
                staticPhysicsH.FullUpdate(massEstimate, softness.val, nippleErection.val);
            });
            nippleErection.slider.onValueChanged.AddListener((float val) =>
            {
                nippleMorphH.Update(val);
                staticPhysicsH.UpdateNipplePhysics(softness.val, val);
            });
        }

        void UpdateLogarithmicGravityAmount(float val)
        {
            gravityLogAmount = Mathf.Log(10 * val - 3.35f);
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
                    if(!physicsUpdateInProgress && (breastMorphListener.Changed() || atomScaleListener.Changed()))
                    {
                        StartCoroutine(RefreshStaticPhysics(atomScaleListener.Value));
                    }

                    float roll = Calc.Roll(chest.rotation);
                    float pitch = Calc.Pitch(chest.rotation);
                    float scaleVal = Calc.LegacyScale(massEstimate);

                    gravityMorphH.Update(roll, pitch, scaleVal, gravityLogAmount);
                    gravityPhysicsH.Update(roll, pitch, scaleVal, gravity.val);
#if SHOW_DEBUG
                    SetBaseDebugInfo(roll, pitch);
                    morphDebugInfo.SetVal(gravityMorphH.GetStatus());
                    physicsDebugInfo.SetVal(staticPhysicsH.GetStatus() + gravityPhysicsH.GetStatus());            
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
            physicsUpdateInProgress = true;
            while(breastMorphListener.Changed())
            {
                yield return null;
            }

            // Iterate the update a few times because each update changes breast shape and thereby the mass estimate.
            for(int i = 0; i < 6; i++)
            {
                // update only non-soft physics settings to improve performance
                UpdateMassEstimate(atomScale);
                staticPhysicsH.UpdateMainPhysics(massEstimate, softness.val);
                if(i > 0)
                {
                    yield return new WaitForSeconds(0.12f);
                }
            }

            UpdateMassEstimate(atomScale, updateUIStatus: true);
            staticPhysicsH.FullUpdate(massEstimate, softness.val, nippleErection.val);
            physicsUpdateInProgress = false;
        }

        void UpdateMassEstimate(float atomScale, bool updateUIStatus = false)
        {
            softVolume = CupVolumeCalc.EstimateVolume(BoundsSize(), atomScale);
            float mass = Calc.VolumeToMass(softVolume);

            if(mass > massMax)
            {
                massEstimate = massMax;
                if(updateUIStatus)
                {
                    float excess = Calc.RoundToDecimals(mass - massMax, 1000f);
                    statusUIText.SetVal(massExcessStatus(excess));
                }
            }
            else if(mass < massMin)
            {
                massEstimate = massMin;
                if(updateUIStatus)
                {
                    float shortage = Calc.RoundToDecimals(massMin - mass, 1000f);
                    statusUIText.SetVal(massShortageStatus(shortage));
                }
            }
            else
            {
                massEstimate = mass;
                if(updateUIStatus)
                {
                    statusUIText.SetVal("");
                }
            } 
        }

        Vector3 BoundsSize()
        {
            Vector3[] vertices = rightBreastMainGroupSets
                .Select(it => it.currentPosition).ToArray();

            inMemoryMesh.vertices = vertices;
            inMemoryMesh.RecalculateBounds();
            return inMemoryMesh.bounds.size;
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

#if SHOW_DEBUG
        void SetBaseDebugInfo(float roll, float pitch)
        {
            float currentSoftVolume = CupVolumeCalc.EstimateVolume(BoundsSize(), atomScaleListener.Value);
            baseDebugInfo.SetVal(
                $"{Formatting.NameValueString("Roll", roll, 100f, 15)}\n" +
                $"{Formatting.NameValueString("Pitch", pitch, 100f, 15)}\n" +
                $"volume: {softVolume}\n" +
                $"current volume: {currentSoftVolume}"
            );
        }
#endif
    }
}
