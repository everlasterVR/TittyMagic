//#define SHOW_DEBUG
//#define USE_CONFIGURATORS

using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TittyMagic.Utils;
using static TittyMagic.Calc;

namespace TittyMagic
{
    internal class Script : MVRScript
    {
        public static readonly Version version = new Version("0.0.0");

        private Bindings customBindings;

        private List<Rigidbody> rigidbodies;
        private Rigidbody chestRigidbody;
        private Transform chestTransform;
        private Rigidbody rNippleRigidbody;
        private Transform rNippleTransform;
        private Rigidbody lPectoralRigidbody;
        private Rigidbody rPectoralRigidbody;
        private DAZCharacterSelector geometry;
        private Vector3 neutralRelativePos;
        private Vector3 positionDiff;

        private float massEstimate;
        private float massAmount;
        private float softnessAmount;
        private float gravityAmount;

        private SettingsMonitor settingsMonitor;

        private AtomScaleListener atomScaleListener;
        private BreastMorphListener breastMorphListener;
        private BreastMassCalculator breastMassCalculator;

        private GravityMorphHandler gravityMorphH;
        private RelativePosMorphHandler relativePosMorphH;
        private NippleErectionMorphHandler nippleErectionMorphH;
        private StaticPhysicsHandler staticPhysicsH;
        private GravityPhysicsHandler gravityPhysicsH;

        private JSONStorableString titleUIText;
        private JSONStorableString statusUIText;

        private Dictionary<string, UIDynamicButton> modeButtonGroup;

        //registered storables

        private JSONStorableStringChooser modeChooser;
        private JSONStorableString pluginVersionStorable;
        private JSONStorableFloat softness;
        private SliderClickMonitor softnessSCM;
        private JSONStorableFloat gravity;
        private SliderClickMonitor gravitySCM;
        private JSONStorableBool linkSoftnessAndGravity;
        private JSONStorableFloat nippleErection;

        private bool modeSetFromJson;
        private float timeSinceListenersChecked;
        private float listenersCheckInterval = 0.1f;
        private int refreshStatus = RefreshStatus.WAITING;
        private bool animationWasSetFrozen = false;

        private float timeMultiplier;

        private float roll;
        private float pitch;

#if SHOW_DEBUG
        private JSONStorableString baseDebugInfo;
        private JSONStorableString physicsDebugInfo;
#endif

        public override void Init()
        {
            try
            {
                pluginVersionStorable = new JSONStorableString("Version", "");
                pluginVersionStorable.val = $"{version}";
                RegisterString(pluginVersionStorable);

                if(containingAtom.type != "Person")
                {
                    LogError($"Add to a Person atom, not {containingAtom.type}");
                    return;
                }

                AdjustJoints breastControl = containingAtom.GetStorableByID("BreastControl") as AdjustJoints;
                DAZPhysicsMesh breastPhysicsMesh = containingAtom.GetStorableByID("BreastPhysicsMesh") as DAZPhysicsMesh;
                rigidbodies = containingAtom.GetComponentsInChildren<Rigidbody>().ToList();
                chestRigidbody = rigidbodies.Find(rb => rb.name == "chest");
                chestTransform = chestRigidbody.transform;
                rNippleRigidbody = rigidbodies.Find(rb => rb.name == "rNipple");
                rNippleTransform = rNippleRigidbody.transform;
                lPectoralRigidbody = rigidbodies.Find(rb => rb.name == "lPectoral");
                rPectoralRigidbody = rigidbodies.Find(rb => rb.name == "rPectoral");
                geometry = containingAtom.GetStorableByID("geometry") as DAZCharacterSelector;

                Globals.SAVES_DIR = SuperController.singleton.savesDir + @"everlaster\TittyMagicSettings\";
                Globals.PLUGIN_PATH = GetPackagePath(this) + @"Custom\Scripts\everlaster\TittyMagic\";
                Globals.BREAST_CONTROL = breastControl;
                Globals.BREAST_PHYSICS_MESH = breastPhysicsMesh;
                Globals.GEOMETRY = containingAtom.GetStorableByID("geometry") as DAZCharacterSelector;

                settingsMonitor = gameObject.AddComponent<SettingsMonitor>();
                settingsMonitor.Init(containingAtom);

                atomScaleListener = new AtomScaleListener(containingAtom.GetStorableByID("rescaleObject").GetFloatJSONParam("scale"));
                breastMorphListener = new BreastMorphListener(geometry.morphBank1.morphs);
                breastMassCalculator = new BreastMassCalculator(chestTransform);

#if USE_CONFIGURATORS
                gravityMorphH = new GravityMorphHandler(FindPluginOnAtom(containingAtom, "GravityMorphConfigurator"));
                relativePosMorphH = new RelativePosMorphHandler(FindPluginOnAtom(containingAtom, "RelativePosMorphConfigurator"));
#else
                // preloads settings for default mode before default mode actually selected
                gravityMorphH = new GravityMorphHandler(this);
                relativePosMorphH = new RelativePosMorphHandler(this);
#endif
                nippleErectionMorphH = new NippleErectionMorphHandler(this);
                gravityPhysicsH = new GravityPhysicsHandler();
                staticPhysicsH = new StaticPhysicsHandler();

                InitPluginUILeft();
                InitPluginUIRight();
                InitSliderListeners();
                SuperController.singleton.onAtomRemovedHandlers += OnRemoveAtom;

                softnessAmount = Mathf.Pow(softness.val/softness.max, 1/2f);
                gravityAmount = Mathf.Pow(gravity.val/gravity.max, 1/2f);

                StartCoroutine(SelectDefaultMode());
                StartCoroutine(SubscribeToKeybindings());
            }
            catch(Exception e)
            {
                LogError($"{e}");
            }
        }

        private IEnumerator SelectDefaultMode()
        {
            yield return new WaitForEndOfFrame();
            if(modeSetFromJson)
            {
                yield break;
            }

            modeChooser.val = Mode.ANIM_OPTIMIZED; // selection causes BeginRefresh
        }

        //https://github.com/vam-community/vam-plugins-interop-specs/blob/main/keybindings.md
        private IEnumerator SubscribeToKeybindings()
        {
            yield return new WaitForEndOfFrame();
            SuperController.singleton.BroadcastMessage("OnActionsProviderAvailable", this, SendMessageOptions.DontRequireReceiver);
        }

        public void OnBindingsListRequested(List<object> bindings)
        {
            customBindings = gameObject.AddComponent<Bindings>();
            customBindings.Init(this);
            bindings.Add(customBindings.Settings);
            bindings.AddRange(customBindings.OnKeyDownActions);
        }

        #region User interface

        private void InitPluginUILeft()
        {
            titleUIText = UI.NewTextField(this, "titleText", "", 36, 100);
            titleUIText.SetVal($"{nameof(TittyMagic)}\n<size=28>v{version}</size>");

            UI.NewSpacer(this, 10f);
            modeChooser = CreateModeChooser();
            modeButtonGroup = UI.CreateRadioButtonGroup(this, modeChooser);
            staticPhysicsH.modeChooser = modeChooser;

            UI.NewSpacer(this, 10f);
            softness = UI.NewFloatSlider(this, "Breast softness", 100f, Const.SOFTNESS_MIN, Const.SOFTNESS_MAX, "F0");
            gravity = UI.NewFloatSlider(this, "Breast gravity", 50f, Const.GRAVITY_MIN, Const.GRAVITY_MAX, "F0");
            linkSoftnessAndGravity = UI.NewToggle(this, "Link softness and gravity", false, false);

            UI.NewSpacer(this, 10f);
            nippleErection = UI.NewFloatSlider(this, "Erect nipples", 0f, 0f, 1.0f, "F2");
        }

        private JSONStorableStringChooser CreateModeChooser()
        {
            return new JSONStorableStringChooser(
                "Mode",
                new List<string>
                {
                    { Mode.ANIM_OPTIMIZED },
                    { Mode.BALANCED },
                    { Mode.TOUCH_OPTIMIZED }
                },
                "",
                "Mode",
                (mode) =>
                {
                    UI.UpdateButtonLabels(modeButtonGroup, mode);
                    staticPhysicsH.LoadSettings(this, mode);
                    gravityMorphH.LoadSettings(mode);
                    if(mode == Mode.ANIM_OPTIMIZED)
                    {
                        //RelativePosMorphHandler doesn't actually support any other mode currently
                        relativePosMorphH.LoadSettings(mode);
                    }
                    StartCoroutine(BeginRefresh());
                }
            );
        }

        private void InitPluginUIRight()
        {
            bool rightSide = true;
            statusUIText = UI.NewTextField(this, "statusText", "", 28, 100, rightSide);

#if SHOW_DEBUG
            baseDebugInfo = UI.NewTextField(this, "Base Debug Info", 26, 125, rightSide);
            physicsDebugInfo = UI.NewTextField(this, "Physics Debug Info", 26, 945, rightSide);
#else
            UI.NewSpacer(this, 10f, rightSide);
            JSONStorableString usage2Area = UI.NewTextField(this, "Usage Info Area 2", "", 28, 135, rightSide);
            string usage2 = UI.Size("\n", 12) + "Physics settings mode selection.";
            usage2Area.SetVal(usage2);

            UI.NewSpacer(this, 10f, rightSide);
            JSONStorableString usage1Area = UI.NewTextField(this, "Usage Info Area 1", "", 28, 255, rightSide);
            string usage1 = UI.Size("\n", 12) +
                "Breast softness adjusts soft physics settings from very firm to very soft.\n\n" +
                "Breast gravity adjusts how much pose morphs shape the breasts in all orientations.";
            usage1Area.SetVal(usage1);
#endif
        }

        #endregion User interface

        private void InitSliderListeners()
        {
            softnessSCM = softness.slider.gameObject.AddComponent<SliderClickMonitor>();
            gravitySCM = gravity.slider.gameObject.AddComponent<SliderClickMonitor>();

            softness.slider.onValueChanged.AddListener((float val) =>
            {
                softnessAmount = Mathf.Pow(val/softness.max, 1/2f);
                if(linkSoftnessAndGravity.val)
                {
                    gravity.val = val;
                    gravityAmount = Mathf.Pow(val/gravity.max, 1/2f);
                }
                RefreshFromSliderChanged();
            });
            gravity.slider.onValueChanged.AddListener((float val) =>
            {
                gravityAmount = Mathf.Pow(val/gravity.max, 1/2f);
                if(linkSoftnessAndGravity.val)
                {
                    softness.val = val;
                    softnessAmount = Mathf.Pow(val/softness.max, 1/2f);
                }
                RefreshFromSliderChanged();
            });
            nippleErection.slider.onValueChanged.AddListener((float val) =>
            {
                nippleErectionMorphH.Update(val);
                staticPhysicsH.UpdateNipplePhysics(softnessAmount, val);
            });
        }

        private void RefreshFromSliderChanged()
        {
            if(modeChooser.val == Mode.ANIM_OPTIMIZED && refreshStatus != RefreshStatus.WAITING)
            {
                StartCoroutine(BeginRefresh());
            }
            else
            {
                staticPhysicsH.FullUpdate(softnessAmount, nippleErection.val);
            }
        }

        private void FixedUpdate()
        {
            try
            {
                DoFixedUpdate();
            }
            catch(Exception e)
            {
                LogError($"{e}");
                LogError($"Try reloading plugin!");
                enabled = false;
            }
        }

        private void DoFixedUpdate()
        {
            timeSinceListenersChecked += Time.unscaledDeltaTime;

            if(refreshStatus == RefreshStatus.MASS_STARTED)
            {
                return;
            }

            if(refreshStatus > RefreshStatus.MASS_STARTED)
            {
                float gravityMagnitude = Physics.gravity.magnitude / timeMultiplier;
                chestRigidbody.AddForce(chestTransform.up * -gravityMagnitude, ForceMode.Acceleration);
                lPectoralRigidbody.AddForce(chestTransform.up * -gravityMagnitude, ForceMode.Acceleration);
                rPectoralRigidbody.AddForce(chestTransform.up * -gravityMagnitude, ForceMode.Acceleration);
                if(modeChooser.val == Mode.ANIM_OPTIMIZED)
                {
                    if(refreshStatus == RefreshStatus.MASS_OK)
                    {
                        StartCoroutine(RefreshNeutralRelativePosition());
                    }
                    else if(refreshStatus == RefreshStatus.NEUTRALPOS_OK)
                    {
                        EndRefresh();
                    }
                }
                else
                {
                    EndRefresh();
                }
                return;
            }

            if(timeSinceListenersChecked >= listenersCheckInterval)
            {
                timeSinceListenersChecked -= listenersCheckInterval;
                if(refreshStatus != RefreshStatus.WAITING && (breastMorphListener.Changed() || atomScaleListener.Changed()))
                {
                    StartCoroutine(BeginRefresh());
                    return;
                }
            }

            if(refreshStatus != RefreshStatus.DONE)
            {
                return;
            }

            roll = Roll(chestTransform.rotation);
            pitch = Pitch(chestTransform.rotation);

            if(gravityMorphH.IsEnabled())
            {
                gravityMorphH.Update(roll, pitch, massAmount, gravityAmount);
            }

            if(modeChooser.val == Mode.ANIM_OPTIMIZED)
            {
                positionDiff = neutralRelativePos - RelativePosition(chestTransform, rNippleTransform.position);
                if(relativePosMorphH.IsEnabled())
                {
                    relativePosMorphH.Update(positionDiff, massAmount, softnessAmount);
                }

#if USE_CONFIGURATORS
                string positionDiffText =
                    $"{NameValueString("x", positionDiff.x, 10000f)} \n" +
                    $"{NameValueString("y", positionDiff.y, 10000f)} \n" +
                    $"{NameValueString("z", positionDiff.z, 10000f)} ";
                gravityMorphH.UpdateDebugInfo(positionDiffText);
                if(modeChooser.val == Mode.ANIM_OPTIMIZED)
                {
                    relativePosMorphH.UpdateDebugInfo(positionDiffText);
                }
#endif
            }

            //TODO re-enable
            //gravityPhysicsH.Update(roll, pitch, massAmount, softnessAmount, gravityAmount);

#if SHOW_DEBUG
            SetBaseDebugInfo();
#endif
#if SHOW_DEBUG
            physicsDebugInfo.SetVal(staticPhysicsH.GetStatus() + gravityPhysicsH.GetStatus());
#endif
        }

        private float TimeMultiplier()
        {
            if(TimeControl.singleton == null || Mathf.Approximately(Time.timeScale, 0f))
            {
                return 1;
            }

            return 1 / Time.timeScale;
        }

        public IEnumerator BeginRefresh()
        {
            refreshStatus = RefreshStatus.WAITING;
            animationWasSetFrozen = modeSetFromJson ? false : SuperController.singleton.freezeAnimation;
            SuperController.singleton.SetFreezeAnimation(true);

            // ensure refresh actually begins only once listeners report no change
            yield return new WaitForSecondsRealtime(listenersCheckInterval);
            while(breastMorphListener.Changed() || atomScaleListener.Changed() || softnessSCM.isDown || gravitySCM.isDown)
            {
                yield return new WaitForSecondsRealtime(0.1f);
            }
            yield return new WaitForSecondsRealtime(1.0f);

            refreshStatus = RefreshStatus.MASS_STARTED;

            settingsMonitor.enabled = false;

            // simulate breasts zero G
            chestRigidbody.useGravity = false;
            lPectoralRigidbody.useGravity = false;
            rPectoralRigidbody.useGravity = false;

            // zero pose morphs
            gravityMorphH.ResetAll();
            if(modeChooser.val == Mode.ANIM_OPTIMIZED)
            {
                relativePosMorphH.ResetAll();
            }

            float duration = 0;
            float interval = 0.1f;
            while(duration < 2f && (
                !VectorEqualWithin(1000f, rNippleRigidbody.velocity, Vector3.zero) ||
                !EqualWithin(1000f, massEstimate, DetermineMassEstimate(atomScaleListener.Value))
            ))
            {
                yield return new WaitForSecondsRealtime(interval);
                duration += interval;

                // update mass estimate
                massEstimate = DetermineMassEstimate(atomScaleListener.Value);

                // update main static physics
                massAmount = staticPhysicsH.SetAndReturnMassVal(massEstimate);
                staticPhysicsH.UpdateMainPhysics(softnessAmount);

                // update gravity physics angles
                roll = 0;
                pitch = 0;
                gravityPhysicsH.Update(roll, pitch, massEstimate, softnessAmount, gravityAmount);

                // TODO update gravity morphs ?
            }
            SetMassUIStatus(atomScaleListener.Value);
            staticPhysicsH.FullUpdate(softnessAmount, nippleErection.val);

            timeMultiplier = TimeMultiplier();
            refreshStatus = RefreshStatus.MASS_OK;
        }

        private IEnumerator RefreshNeutralRelativePosition()
        {
            refreshStatus = RefreshStatus.NEUTRALPOS_STARTED;
            yield return new WaitForSecondsRealtime(1f);

            float duration = 0;
            float interval = 0.1f;
            while(
                duration < 2f && (
                !VectorEqualWithin(
                    1000000f,
                    neutralRelativePos,
                    RelativePosition(chestTransform, rNippleTransform.position)
                ))
            )
            {
                yield return new WaitForSecondsRealtime(interval);
                neutralRelativePos = RelativePosition(chestTransform, rNippleTransform.position);
            }

            refreshStatus = RefreshStatus.NEUTRALPOS_OK;
        }

        private void EndRefresh()
        {
            chestRigidbody.useGravity = true;
            lPectoralRigidbody.useGravity = true;
            rPectoralRigidbody.useGravity = true;
            SuperController.singleton.SetFreezeAnimation(animationWasSetFrozen);
            settingsMonitor.enabled = true;
            modeSetFromJson = false;
            refreshStatus = RefreshStatus.DONE;
        }

        public void RefreshRateDependentPhysics()
        {
            staticPhysicsH.UpdateRateDependentPhysics(softnessAmount);
        }

        private float DetermineMassEstimate(float atomScale)
        {
            float mass = breastMassCalculator.Calculate(atomScale);
            if(mass > Const.MASS_MAX)
                return Const.MASS_MAX;

            if(mass < Const.MASS_MIN)
                return Const.MASS_MIN;

            return mass;
        }

        private void SetMassUIStatus(float atomScale)
        {
            float mass = breastMassCalculator.Calculate(atomScale);
            if(mass > Const.MASS_MAX)
            {
                float excess = RoundToDecimals(mass - Const.MASS_MAX, 1000f);
                statusUIText.SetVal(MassExcessStatus(excess));
            }
            else if(mass < Const.MASS_MIN)
            {
                float shortage = RoundToDecimals(Const.MASS_MIN - mass, 1000f);
                statusUIText.SetVal(MassShortageStatus(shortage));
            }
            else
            {
                statusUIText.SetVal("");
            }
        }

        private string MassExcessStatus(float value)
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

        private string MassShortageStatus(float value)
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

        public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
        {
            JSONClass json = base.GetJSON(includePhysical, includeAppearance, forceStore);
            json["Mode"] = modeChooser.val;
            needsStore = true;
            return json;
        }

        public override void RestoreFromJSON(JSONClass json, bool restorePhysical = true, bool restoreAppearance = true, JSONArray presetAtoms = null, bool setMissingToDefault = true)
        {
            if(json.HasKey("Mode"))
            {
                modeSetFromJson = true;
                modeChooser.val = json["Mode"];
            }

            base.RestoreFromJSON(json, restorePhysical, restoreAppearance, presetAtoms, setMissingToDefault);
        }

        private void OnRemoveAtom(Atom atom)
        {
            Destroy(softnessSCM);
            Destroy(gravitySCM);
        }

        private void OnDestroy()
        {
            Destroy(settingsMonitor);
            SuperController.singleton.onAtomRemovedHandlers -= OnRemoveAtom;
            SuperController.singleton.BroadcastMessage("OnActionsProviderDestroyed", this, SendMessageOptions.DontRequireReceiver);

            gravityPhysicsH.ResetAll();
            gravityMorphH.ResetAll();
            if(modeChooser.val == Mode.ANIM_OPTIMIZED)
            {
                relativePosMorphH.ResetAll();
            }
            nippleErectionMorphH.ResetAll();
        }

        public void OnEnable()
        {
            if(settingsMonitor != null)
            {
                settingsMonitor.enabled = true;
            }
        }

        private void OnDisable()
        {
            settingsMonitor.enabled = false;
            gravityPhysicsH.ResetAll();
            gravityMorphH.ResetAll();
            if(modeChooser.val == Mode.ANIM_OPTIMIZED)
            {
                relativePosMorphH.ResetAll();
            }
            nippleErectionMorphH.ResetAll();
        }

#if SHOW_DEBUG

        private void SetBaseDebugInfo()
        {
            baseDebugInfo.SetVal(
                $"{NameValueString("Roll", roll, 100f, 15)}\n" +
                $"{NameValueString("Pitch", pitch, 100f, 15)}\n" +
                $"{breastMassCalculator.GetStatus(atomScaleListener.Value)}"
            );
        }

#endif
    }
}
