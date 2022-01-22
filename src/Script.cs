//#define DEBUG_ON
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

        private float _massEstimate;
        private float _massAmount;
        private float _massScaling;
        private float _softnessAmount;
        private float _gravityAmount;
        private float _upDownMobilityAmount;

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
        private JSONStorableString debugUIText;
        private JSONStorableString modeInfoText;
        private JSONStorableString gravityInfoText;
        private JSONStorableString mobilityInfoText;

        private Dictionary<string, UIDynamicButton> modeButtonGroup;

        //registered storables

        private JSONStorableStringChooser modeChooser;
        private JSONStorableString pluginVersionStorable;
        private JSONStorableFloat _softness;
        private SliderClickMonitor softnessSCM;
        private JSONStorableFloat _gravity;
        private SliderClickMonitor gravitySCM;
        private JSONStorableFloat _upDownMobility;
        private SliderClickMonitor upDownMobilitySCM;
        private JSONStorableBool _linkSoftnessAndGravity;
        private JSONStorableBool _linkGravityAndMobility;
        private UIDynamic _lowerLeftSpacer;
        private JSONStorableFloat _nippleErection;

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

                _softnessAmount = Mathf.Pow(_softness.val/100f, 1/2f);
                _gravityAmount = Mathf.Pow(_gravity.val/100f, 1/2f);

                if(modeChooser.val == Mode.ANIM_OPTIMIZED)
                {
                    _upDownMobilityAmount = 1.5f * Mathf.Pow(_upDownMobility.val/100f, 1/2f);
                }

                StartCoroutine(SelectDefaultMode());
                StartCoroutine(SubscribeToKeybindings());
            }
            catch(Exception e)
            {
                LogError($"Init: {e}");
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
            _softness = UI.NewIntSlider(this, "Breast softness", 75f, 0f, 100f);
            UI.NewSpacer(this, 10f);
            _linkSoftnessAndGravity = UI.NewToggle(this, "Link softness and gravity", true, false);
            _gravity = UI.NewIntSlider(this, "Breast gravity", 75f, 0f, 100f);
            UI.NewSpacer(this, 10f);
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

            UpdateModeInfoText(mode);
            UpdateGravityInfoText(mode);
            BuildPluginUILowerLeft();
            BuildPluginUILowerRight();

            StartCoroutine(TempDisableModeButtons());

            yield return WaitToBeginRefresh();
        }

        private void UpdateModeInfoText(string mode)
        {
            string text = UI.Size("\n", 12);
            if(mode == Mode.ANIM_OPTIMIZED)
            {
                text += "Animation optimized mode morphs breasts in response to up/down forces. Sideways and forward/back shape is based only on the chest angle. Physics settings are similar to balanced mode, but hard colliders are off.";
            }
            else if(mode == Mode.BALANCED)
            {
                text += "In balanced mode, breasts have realistic mass which helps with animation, and hard colliders are on to enable collision to move breasts as well.";
            }
            else if(mode == Mode.TOUCH_OPTIMIZED)
            {
                text += "Touch optimized mode lowers breast mass, increases fat back force and turns off hard colliders. While breast movement due to gravity and other forces isn't very realistic in this mode, collision is based only on soft physics.";
            }
            modeInfoText.SetVal(text);
        }

        private void UpdateGravityInfoText(string mode)
        {
            string infoTextEnd = mode == Mode.ANIM_OPTIMIZED ?
                "when leaning left/right and forward/back" :
                "in all orientations";
            gravityInfoText.SetVal(
                UI.Size("\n", 12) +
                $"Adjusts how much pose morphs shape the breasts {infoTextEnd}."
            );
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
            modeInfoText = UI.NewTextField(this, "Usage Info Area 2", "", 28, 210, rightSide);

            UI.NewSpacer(this, 10f, rightSide);
            JSONStorableString softnessInfoText = UI.NewTextField(this, "Usage Info Area 1", "", 28, 120, rightSide);
            softnessInfoText.SetVal(
                UI.Size("\n", 12) +
                "Adjusts soft physics settings from very firm to very soft."
            );

            UI.NewSpacer(this, 75f, rightSide);
            gravityInfoText = UI.NewTextField(this, "GravityInfoText", "", 28, 120, true);
            UI.NewSpacer(this, 75f, rightSide);

#if DEBUG_ON
            debugUIText = UI.NewTextField(this, "debugText", "", 28, 200, rightSide);
#endif
        }

        #endregion User interface

        private void InitSliderListeners()
        {
            softnessSCM = _softness.slider.gameObject.AddComponent<SliderClickMonitor>();
            gravitySCM = _gravity.slider.gameObject.AddComponent<SliderClickMonitor>();

            _softness.slider.onValueChanged.AddListener((float val) =>
            {
                float newAmount = Mathf.Pow(val/100f, 1/2f);
                if(newAmount == _softnessAmount)
                {
                    return;
                }
                _softnessAmount = newAmount;
                if(_linkSoftnessAndGravity.val)
                {
                    _gravity.val = val;
                    _gravityAmount = Mathf.Pow(val/100f, 1/2f);
                }
                RefreshFromSliderChanged();
            });
            _gravity.slider.onValueChanged.AddListener((float val) =>
            {
                _gravityAmount = Mathf.Pow(val/100f, 1/2f);
                if(_linkSoftnessAndGravity.val)
                {
                    _softness.val = val;
                    float newAmount = Mathf.Pow(val/100f, 1/2f);
                    if(newAmount == _softnessAmount)
                    {
                        return;
                    }
                    _softnessAmount = Mathf.Pow(val/100f, 1/2f);
                    RefreshFromSliderChanged();
                }
            });
        }

        private void GravityListenerForMobilityLink(float val)
        {
            if(_linkGravityAndMobility.val)
            {
                _upDownMobility.val = 2/3f * val;
                _upDownMobilityAmount = 1.5f * Mathf.Pow(_upDownMobility.val/100f, 1/2f);
            }
        }

        private void BuildPluginUILowerLeft()
        {
            if(_linkGravityAndMobility != null)
                RemoveToggle(_linkGravityAndMobility);
            if(_upDownMobility != null)
                RemoveSlider(_upDownMobility);
            if(_lowerLeftSpacer != null)
                RemoveSpacer(_lowerLeftSpacer);
            if(_nippleErection != null)
                RemoveSlider(_nippleErection);

            if(modeChooser.val == Mode.ANIM_OPTIMIZED)
            {
                if(_linkGravityAndMobility == null)
                {
                    _linkGravityAndMobility = UI.NewToggle(this, "Link gravity and mobility", true, false);
                }
                else
                {
                    CreateToggle(_linkGravityAndMobility, false);
                }
                _upDownMobility = UI.NewIntSlider(this, "Up/down mobility", 2/3f * _gravity.val, 0f, 100f);
                _upDownMobilityAmount = 1.5f * Mathf.Pow(_upDownMobility.val/100f, 1/2f);

                upDownMobilitySCM = _upDownMobility.slider.gameObject.AddComponent<SliderClickMonitor>();
                _upDownMobility.slider.onValueChanged.AddListener((float val) =>
                {
                    _upDownMobilityAmount = 1.5f * Mathf.Pow(val/100f, 1/2f);
                    if(_linkGravityAndMobility.val)
                    {
                        _gravity.val = 1.5f * val;
                        float newAmount = Mathf.Pow(_gravity.val/100f, 1/2f);
                        if(newAmount == _gravityAmount)
                        {
                            return;
                        }
                        _gravityAmount = newAmount;
                        if(_linkSoftnessAndGravity.val)
                        {
                            RefreshFromSliderChanged();
                        }
                    }
                });

                _gravity.slider.onValueChanged.AddListener(GravityListenerForMobilityLink);
            }
            else
            {
                try
                {
                    Destroy(upDownMobilitySCM);
                    _gravity.slider.onValueChanged.RemoveListener(GravityListenerForMobilityLink);
                }
                catch(Exception)
                {
                }
            }

            float spacerHeight = modeChooser.val == Mode.ANIM_OPTIMIZED ? 90f : 290f;
            _lowerLeftSpacer = UI.NewSpacer(this, spacerHeight);

            if(_nippleErection == null)
            {
                _nippleErection = UI.NewFloatSlider(this, "Nipple erection", 0f, 0f, 1.0f, "F2");
            }
            else
            {
                UIDynamicSlider slider = CreateSlider(_nippleErection, false);
                slider.valueFormat = "F2";
            }
            _nippleErection.slider.onValueChanged.AddListener((float val) =>
            {
                nippleErectionMorphH.Update(val);
                staticPhysicsH.UpdateNipplePhysics(_softnessAmount, val);
            });
        }

        private void BuildPluginUILowerRight()
        {
            if(mobilityInfoText != null)
                RemoveTextField(mobilityInfoText);

            if(modeChooser.val == Mode.ANIM_OPTIMIZED)
            {
                if(mobilityInfoText == null)
                {
                    mobilityInfoText = UI.NewTextField(this, "MobilityInfoText", "", 28, 120, true);
                }
                else
                {
                    UIDynamicTextField field = CreateTextField(mobilityInfoText, true);
                    field.UItext.fontSize = 28;
                    field.height = 120;
                }
                mobilityInfoText.SetVal(
                    UI.Size("\n", 12) +
                    "Adjusts the amount of up/down morphing due to forces including gravity."
                );
            }
        }

        private void RefreshFromSliderChanged()
        {
            if(modeChooser.val == Mode.ANIM_OPTIMIZED && _waitStatus != RefreshStatus.WAITING)
            {
                StartCoroutine(WaitToBeginRefresh());
            }
            else
            {
                staticPhysicsH.FullUpdate(_softnessAmount, _nippleErection.val);
            }
        }

#if DEBUG_ON
        private void Update()
        {
            debugUIText.SetVal(
                $"softness {_softnessAmount}\n" +
                $"gravity {_gravityAmount}\n" +
                $"upDownMobility {_upDownMobilityAmount}"
            );
        }
#endif

        private void FixedUpdate()
        {
            try
            {
                DoFixedUpdate();
            }
            catch(Exception e)
            {
                LogError($"FixedUpdate: {e}");
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
                gravityPhysicsH.Update(0, 0, _massAmount, _gravityAmount);

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
                    relativePosMorphH.Update(angleY, 0f, _massAmount, _massScaling, _upDownMobilityAmount);
                }
            }

            if(gravityMorphH.IsEnabled())
            {
                gravityMorphH.Update(modeChooser.val, roll, pitch, _massAmount, 0.75f * _gravityAmount);
            }

            if(gravityPhysicsH.IsEnabled())
            {
                gravityPhysicsH.Update(roll, pitch, _massAmount, _gravityAmount);
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
            while(breastMorphListener.Changed() ||
                atomScaleListener.Changed() ||
                softnessSCM.isDown ||
                gravitySCM.isDown ||
                (upDownMobilitySCM != null && upDownMobilitySCM.isDown))
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
                !EqualWithin(1000f, _massEstimate, DetermineMassEstimate(atomScaleListener.Value))
            ))
            {
                yield return new WaitForSeconds(interval);
                duration += interval;

                // update mass estimate
                _massEstimate = DetermineMassEstimate(atomScaleListener.Value);

                // update main static physics
                _massAmount = staticPhysicsH.SetAndReturnMassVal(_massEstimate);
                _massScaling = Mathf.Pow(3/4f * _massAmount, 1/5f);
                staticPhysicsH.UpdateMainPhysics(_softnessAmount);
            }
            SetMassUIStatus(atomScaleListener.Value);
            staticPhysicsH.FullUpdate(_softnessAmount, _nippleErection.val);
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
            staticPhysicsH.UpdateRateDependentPhysics(_softnessAmount);
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
            Destroy(gravitySCM);
            Destroy(upDownMobilitySCM);
        }

        private void OnDestroy()
        {
            try
            {
                Destroy(settingsMonitor);
                Destroy(softnessSCM);
                Destroy(gravitySCM);
                Destroy(upDownMobilitySCM);
                SuperController.singleton.onAtomRemovedHandlers -= OnRemoveAtom;
                SuperController.singleton.BroadcastMessage("OnActionsProviderDestroyed", this, SendMessageOptions.DontRequireReceiver);
            }
            catch(Exception e)
            {
                LogError($"OnDestroy: {e}");
            }
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
            try
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
            catch(Exception e)
            {
                LogError($"OnDisable: {e}");
            }
        }
    }
}
