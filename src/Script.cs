//#define USE_CONFIGURATORS

using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TittyMagic.Utils;
using static TittyMagic.Calc;
using static TittyMagic.Globals;

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

        private float massEstimate;
        private float massAmount;
        private float massScaling;
        private float softnessAmount;
        private float mobilityAmount;

        private SettingsMonitor settingsMonitor;

        private AtomScaleListener atomScaleListener;
        private BreastMorphListener breastMorphListener;
        private BreastMassCalculator breastMassCalculator;

        private StaticPhysicsHandler staticPhysicsH;
        private GravityPhysicsHandler gravityPhysicsH;
        private GravityMorphHandler gravityMorphH;
        private RelativePosMorphHandler relativePosMorphH;
        private NippleErectionMorphHandler nippleErectionMorphH;

        private JSONStorableString titleUIText;
        private JSONStorableString statusUIText;

        private Dictionary<string, UIDynamicButton> modeButtonGroup;

        //registered storables

        private JSONStorableStringChooser modeChooser;
        private JSONStorableString pluginVersionStorable;
        private JSONStorableFloat softness;
        private SliderClickMonitor softnessSCM;
        private JSONStorableFloat mobility;
        private SliderClickMonitor mobilitySCM;
        private JSONStorableBool linkSoftnessAndMobility;
        private JSONStorableFloat nippleErection;

        private bool modeSetFromJson;
        private float timeSinceListenersChecked;
        private float listenersCheckInterval = 0.1f;
        private int _waitStatus = -1;
        private int _refreshStatus = -1;
        private bool animationWasSetFrozen = false;

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

                SAVES_DIR = SuperController.singleton.savesDir + @"everlaster\TittyMagicSettings\";
#if USE_CONFIGURATORS
                MORPHMULTIPLIERS_DIRNAME = "morphmultipliers_dev";
#else
                MORPHMULTIPLIERS_DIRNAME = "morphmultipliers";
#endif
                PLUGIN_PATH = GetPackagePath(this) + @"Custom\Scripts\everlaster\TittyMagic\";
                BREAST_CONTROL = breastControl;
                BREAST_PHYSICS_MESH = breastPhysicsMesh;
                GEOMETRY = containingAtom.GetStorableByID("geometry") as DAZCharacterSelector;

                settingsMonitor = gameObject.AddComponent<SettingsMonitor>();
                settingsMonitor.Init(containingAtom);

                atomScaleListener = new AtomScaleListener(containingAtom.GetStorableByID("rescaleObject").GetFloatJSONParam("scale"));
                breastMorphListener = new BreastMorphListener(geometry.morphBank1.morphs);
                breastMassCalculator = new BreastMassCalculator(chestTransform);

                staticPhysicsH = new StaticPhysicsHandler();
#if USE_CONFIGURATORS
                gravityPhysicsH = new GravityPhysicsHandler(FindPluginOnAtom(containingAtom, "GravityPhysicsConfigurator"));
                gravityMorphH = new GravityMorphHandler(FindPluginOnAtom(containingAtom, "GravityMorphConfigurator"));
                relativePosMorphH = new RelativePosMorphHandler(FindPluginOnAtom(containingAtom, "RelativePosMorphConfigurator"));
#else
                gravityPhysicsH = new GravityPhysicsHandler(this);
                gravityMorphH = new GravityMorphHandler(this);
                relativePosMorphH = new RelativePosMorphHandler(this);
#endif
                nippleErectionMorphH = new NippleErectionMorphHandler(this);

                InitPluginUILeft();
                InitPluginUIRight();
                InitSliderListeners();
                SuperController.singleton.onAtomRemovedHandlers += OnRemoveAtom;

                softnessAmount = Mathf.Pow(softness.val/softness.max, 1/2f);
                mobilityAmount = 0.75f * Mathf.Pow(mobility.val/mobility.max, 1/2f);

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

            modeChooser.val = Mode.BALANCED; // selection causes BeginRefresh
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
            softness = UI.NewFloatSlider(this, "Breast softness", 75f, Const.SOFTNESS_MIN, Const.SOFTNESS_MAX, "F0");
            mobility = UI.NewFloatSlider(this, "Breast mobility", 75f, Const.GRAVITY_MIN, Const.GRAVITY_MAX, "F0");
            linkSoftnessAndMobility = UI.NewToggle(this, "Link softness and mobility", true, false);

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
                    StartCoroutine(OnModeChosen(mode));
                }
            );
        }

        private IEnumerator OnModeChosen(string mode)
        {
            gravityPhysicsH.LoadSettings(mode);
            staticPhysicsH.LoadSettings(this, mode);
            gravityMorphH.LoadSettings(mode);
            if(mode == Mode.ANIM_OPTIMIZED)
            {
                //RelativePosMorphHandler doesn't actually support any other mode currently
                relativePosMorphH.LoadSettings(mode);
            }

            StartCoroutine(TempDisableModeButtons());

            yield return WaitToBeginRefresh();
        }

        private IEnumerator TempDisableModeButtons()
        {
            while(_waitStatus != RefreshStatus.WAITING)
            {
                yield return null;
            }

            foreach(var buttonKvp in modeButtonGroup)
            {
                buttonKvp.Value.button.interactable = false;
            }

            while(_waitStatus != RefreshStatus.DONE)
            {
                yield return null;
            }

            foreach(var buttonKvp in modeButtonGroup)
            {
                buttonKvp.Value.button.interactable = true;
            }
        }

        private void InitPluginUIRight()
        {
            bool rightSide = true;
            statusUIText = UI.NewTextField(this, "statusText", "", 28, 100, rightSide);

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
        }

        #endregion User interface

        private void InitSliderListeners()
        {
            softnessSCM = softness.slider.gameObject.AddComponent<SliderClickMonitor>();
            mobilitySCM = mobility.slider.gameObject.AddComponent<SliderClickMonitor>();

            softness.slider.onValueChanged.AddListener((float val) =>
            {
                softnessAmount = Mathf.Pow(val/softness.max, 1/2f);
                if(linkSoftnessAndMobility.val)
                {
                    mobility.val = val;
                    mobilityAmount = 0.75f * Mathf.Pow(val/mobility.max, 1/2f);
                }
                RefreshFromSliderChanged();
            });
            mobility.slider.onValueChanged.AddListener((float val) =>
            {
                mobilityAmount = 0.75f * Mathf.Pow(val/mobility.max, 1/2f);
                if(linkSoftnessAndMobility.val)
                {
                    softness.val = val;
                    softnessAmount = Mathf.Pow(val/softness.max, 1/2f);
                }
                // prevent double call to Refresh when linked
                else
                {
                    RefreshFromSliderChanged();
                }
            });
            nippleErection.slider.onValueChanged.AddListener((float val) =>
            {
                nippleErectionMorphH.Update(val);
                staticPhysicsH.UpdateNipplePhysics(softnessAmount, val);
            });
        }

        private void RefreshFromSliderChanged()
        {
            if(modeChooser.val == Mode.ANIM_OPTIMIZED && _waitStatus != RefreshStatus.WAITING)
            {
                StartCoroutine(WaitToBeginRefresh());
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
            timeSinceListenersChecked += Time.deltaTime;

            if(_refreshStatus == RefreshStatus.MASS_STARTED)
            {
                return;
            }

            if(_refreshStatus > RefreshStatus.MASS_STARTED)
            {
                // simulate gravityPhysics when upright
                Quaternion zero = new Quaternion(0, 0, 0, -1);
                gravityPhysicsH.Update(0, 0, massEstimate, softnessAmount);

                // simulate force of gravity when upright
                // 0.75f is a hack, for some reason a normal gravity force pushes breasts too much down,
                // causing the neutral position to be off by a little
                Vector3 force = chestRigidbody.transform.up * 0.75f * -Physics.gravity.magnitude;
                lPectoralRigidbody.AddForce(force, ForceMode.Acceleration);
                rPectoralRigidbody.AddForce(force, ForceMode.Acceleration);
                if(modeChooser.val == Mode.ANIM_OPTIMIZED)
                {
                    if(_refreshStatus == RefreshStatus.MASS_OK)
                    {
                        StartCoroutine(RefreshNeutralRelativePosition());
                    }
                    else if(_refreshStatus == RefreshStatus.NEUTRALPOS_OK)
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
                if(_waitStatus != RefreshStatus.WAITING && (breastMorphListener.Changed() || atomScaleListener.Changed()))
                {
                    StartCoroutine(WaitToBeginRefresh());
                    return;
                }
            }

            if(_waitStatus != RefreshStatus.DONE)
            {
                return;
            }

            float roll = Roll(chestTransform.rotation);
            float pitch = Pitch(chestTransform.rotation);

            if(modeChooser.val == Mode.ANIM_OPTIMIZED)
            {
                Vector3 relativePos = RelativePosition(chestTransform, rNippleTransform.position);
                float angleY = Vector2.SignedAngle(
                    new Vector2(neutralRelativePos.z, neutralRelativePos.y),
                    new Vector2(relativePos.z, relativePos.y)
                );
                //float positionDiffZ = (neutralRelativePos - relativePos).z;
                if(relativePosMorphH.IsEnabled())
                {
                    relativePosMorphH.Update(angleY, 0f, massAmount, massScaling, 1.2f * mobilityAmount);
                }
            }

            if(gravityMorphH.IsEnabled())
            {
                gravityMorphH.Update(modeChooser.val, roll, pitch, massAmount, mobilityAmount);
            }

            if(gravityPhysicsH.IsEnabled())
            {
                gravityPhysicsH.Update(roll, pitch, massAmount, mobilityAmount);
            }
        }

        private IEnumerator WaitToBeginRefresh()
        {
            _waitStatus = RefreshStatus.WAITING;
            while(_refreshStatus != RefreshStatus.DONE && _refreshStatus != -1)
            {
                yield return null;
            }

            yield return BeginRefresh();
        }

        public IEnumerator BeginRefresh()
        {
            animationWasSetFrozen = modeSetFromJson ? false : SuperController.singleton.freezeAnimation;

            SuperController.singleton.SetFreezeAnimation(true);

            // ensure refresh actually begins only once listeners report no change
            yield return new WaitForSeconds(listenersCheckInterval);
            while(breastMorphListener.Changed() || atomScaleListener.Changed() || softnessSCM.isDown || mobilitySCM.isDown)
            {
                yield return new WaitForSeconds(0.1f);
            }
            yield return new WaitForSeconds(0.33f);

            _refreshStatus = RefreshStatus.MASS_STARTED;

            settingsMonitor.enabled = false;

            // simulate breasts zero G
            lPectoralRigidbody.useGravity = false;
            rPectoralRigidbody.useGravity = false;

            // zero pose morphs
            gravityMorphH.ResetAll();
            if(modeChooser.val == Mode.ANIM_OPTIMIZED)
            {
                relativePosMorphH.ResetAll();
            }
            gravityPhysicsH.ZeroAll();

            yield return new WaitForSeconds(0.33f);

            float duration = 0;
            float interval = 0.1f;
            while(duration < 1f && (
                !VectorEqualWithin(1000f, rNippleRigidbody.velocity, Vector3.zero) ||
                !EqualWithin(1000f, massEstimate, DetermineMassEstimate(atomScaleListener.Value))
            ))
            {
                yield return new WaitForSeconds(interval);
                duration += interval;

                // update mass estimate
                massEstimate = DetermineMassEstimate(atomScaleListener.Value);

                // update main static physics
                massAmount = staticPhysicsH.SetAndReturnMassVal(massEstimate);
                massScaling = Mathf.Pow(3/4f * massAmount, 1/5f);
                staticPhysicsH.UpdateMainPhysics(softnessAmount);
            }
            SetMassUIStatus(atomScaleListener.Value);
            staticPhysicsH.FullUpdate(softnessAmount, nippleErection.val);
            gravityPhysicsH.SetBaseValues();

            _refreshStatus = RefreshStatus.MASS_OK;
        }

        private IEnumerator RefreshNeutralRelativePosition()
        {
            _refreshStatus = RefreshStatus.NEUTRALPOS_STARTED;

            yield return new WaitForSeconds(0.67f);

            float duration = 0;
            float interval = 0.1f;
            while(
                duration < 1f && (
                !VectorEqualWithin(
                    1000000f,
                    neutralRelativePos,
                    RelativePosition(chestTransform, rNippleTransform.position)
                ))
            )
            {
                yield return new WaitForSeconds(interval);
                duration += interval;
                neutralRelativePos = RelativePosition(chestTransform, rNippleTransform.position);
            }

            _refreshStatus = RefreshStatus.NEUTRALPOS_OK;
        }

        private void EndRefresh()
        {
            lPectoralRigidbody.useGravity = true;
            rPectoralRigidbody.useGravity = true;
            SuperController.singleton.SetFreezeAnimation(animationWasSetFrozen);
            settingsMonitor.enabled = true;
            modeSetFromJson = false;
            _waitStatus = RefreshStatus.DONE;
            _refreshStatus = RefreshStatus.DONE;
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
            Destroy(settingsMonitor);
            Destroy(softnessSCM);
            Destroy(mobilitySCM);
        }

        private void OnDestroy()
        {
            try
            {
                Destroy(settingsMonitor);
                Destroy(softnessSCM);
                Destroy(mobilitySCM);
            }
            catch(Exception)
            {
            }
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
    }
}
