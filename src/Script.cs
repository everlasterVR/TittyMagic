//#define DEBUG_ON
//#define USE_CONFIGURATORS

using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static TittyMagic.Utils;
using static TittyMagic.Calc;
using static TittyMagic.Globals;

namespace TittyMagic
{
    internal class Script : MVRScript
    {
        public static readonly Version version = new Version("0.0.0");

        private Bindings _customBindings;

        private Rigidbody _chestRb;
        private Rigidbody _pectoralRbLeft;
        private Rigidbody _pectoralRbRight;

        private DAZCharacterSelector _geometry;

        private float _massEstimate;
        private float _massAmount;
        private float _verticalAngleMassMultiplier;
        private float _rollAngleMassMultiplier;
        private float _backDepthDiffMassMultiplier;
        private float _forwardDepthDiffMassMultiplier;
        private float _softnessAmount;
        private float _gravityAmount;

        private TrackNipple _trackLeftNipple;
        private TrackNipple _trackRightNipple;
        private float _chestRoll;
        private float _chestPitch;

        private SettingsMonitor _settingsMonitor;

        private AtomScaleListener _atomScaleListener;
        private BreastMorphListener _breastMorphListener;
        private BreastMassCalculator _breastMassCalculator;

        private StaticPhysicsHandler _staticPhysicsH;
        private GravityPhysicsHandler _gravityPhysicsH;
        private GravityMorphHandler _gravityMorphH;
        private RelativePosMorphHandler _relativePosMorphH;
        private NippleErectionMorphHandler _nippleErectionMorphH;

        private JSONStorableString _titleUIText;
        private JSONStorableString _statusUIText;
        private InputField _statusUIInputField;
#if DEBUG_ON
        private JSONStorableString _debugUIText;
#endif
        private JSONStorableString _modeInfoText;
        private JSONStorableString _gravityInfoText;

        private Dictionary<string, UIDynamicButton> _modeButtonGroup;

        private JSONStorableStringChooser _modeChooser;
        private JSONStorableString _pluginVersionStorable;
        private JSONStorableBool _autoRecalibrateOnSizeChange;
        private JSONStorableFloat _softness;
        private SliderClickMonitor _softnessSCM;
        private JSONStorableFloat _gravity;
        private UIDynamicSlider _gravitySlider;
        private SliderClickMonitor _gravitySCM;
        private JSONStorableBool _linkSoftnessAndGravity;
        private UIDynamicToggle _linkSoftnessAndGravityToggle;
        private JSONStorableFloat _nippleErection;

        private bool _loadingFromJson;
        private float _timeSinceListenersChecked;
        private float _listenersCheckInterval = 0.1f;
        private int _waitStatus = -1;
        private int _refreshStatus = -1;
        private bool _animationWasSetFrozen = false;

        public override void Init()
        {
            try
            {
                _pluginVersionStorable = new JSONStorableString("Version", "");
                _pluginVersionStorable.val = $"{version}";
                _pluginVersionStorable.storeType = JSONStorableParam.StoreType.Full;
                RegisterString(_pluginVersionStorable);

                if(containingAtom.type != "Person")
                {
                    LogError($"Add to a Person atom, not {containingAtom.type}");
                    return;
                }

                AdjustJoints breastControl = containingAtom.GetStorableByID("BreastControl") as AdjustJoints;
                DAZPhysicsMesh breastPhysicsMesh = containingAtom.GetStorableByID("BreastPhysicsMesh") as DAZPhysicsMesh;
                var rigidbodies = containingAtom.GetComponentsInChildren<Rigidbody>().ToList();
                _chestRb = rigidbodies.Find(rb => rb.name == "chest");
                _pectoralRbLeft = rigidbodies.Find(rb => rb.name == "lPectoral");
                _pectoralRbRight = rigidbodies.Find(rb => rb.name == "rPectoral");
                var nippleRbLeft = rigidbodies.Find(rb => rb.name == "lNipple");
                var nippleRbRight = rigidbodies.Find(rb => rb.name == "rNipple");

                _trackLeftNipple = new TrackNipple(_chestRb, _pectoralRbLeft, nippleRbLeft);
                _trackRightNipple = new TrackNipple(_chestRb, _pectoralRbRight, nippleRbRight);

                SAVES_DIR = SuperController.singleton.savesDir + @"everlaster\TittyMagicSettings\";
                MORPHS_PATH = MorphsPath();
                PLUGIN_PATH = GetPackagePath(this) + @"Custom\Scripts\everlaster\TittyMagic\";
                BREAST_CONTROL = breastControl;
                BREAST_PHYSICS_MESH = breastPhysicsMesh;
                GEOMETRY = containingAtom.GetStorableByID("geometry") as DAZCharacterSelector;

                _settingsMonitor = gameObject.AddComponent<SettingsMonitor>();
                _settingsMonitor.Init(containingAtom);

                _atomScaleListener = new AtomScaleListener(containingAtom.GetStorableByID("rescaleObject").GetFloatJSONParam("scale"));
                _breastMorphListener = new BreastMorphListener(GEOMETRY.morphBank1.morphs);
                _breastMassCalculator = new BreastMassCalculator(_chestRb);

                _staticPhysicsH = new StaticPhysicsHandler();
#if USE_CONFIGURATORS
                _gravityPhysicsH = new GravityPhysicsHandler(FindPluginOnAtom(containingAtom, "GravityPhysicsConfigurator"));
                _gravityMorphH = new GravityMorphHandler(FindPluginOnAtom(containingAtom, "GravityMorphConfigurator"));
                _relativePosMorphH = new RelativePosMorphHandler(FindPluginOnAtom(containingAtom, "RelativePosMorphConfigurator"));
#else
                _gravityPhysicsH = new GravityPhysicsHandler(this);
                _gravityMorphH = new GravityMorphHandler(this);
                _relativePosMorphH = new RelativePosMorphHandler(this);
#endif
                _nippleErectionMorphH = new NippleErectionMorphHandler(this);

                InitPluginUILeft();
                InitPluginUIRight();
                InitSliderListeners();
                SuperController.singleton.onAtomRemovedHandlers += OnRemoveAtom;

                _softnessAmount = Mathf.Pow(_softness.val/100f, 1/2f);
                _gravityAmount = Mathf.Pow(_gravity.val/100f, 1/2f);

                StartCoroutine(SelectDefaultMode());
                StartCoroutine(SubscribeToKeybindings());
            }
            catch(Exception e)
            {
                enabled = false;
                LogError($"Init: {e}");
            }
        }

        private IEnumerator SelectDefaultMode()
        {
            yield return new WaitForEndOfFrame();
            if(_loadingFromJson)
            {
                yield break;
            }
            _modeChooser.val = Mode.ANIM_OPTIMIZED; // selection causes BeginRefresh
        }

        //https://github.com/vam-community/vam-plugins-interop-specs/blob/main/keybindings.md
        private IEnumerator SubscribeToKeybindings()
        {
            yield return new WaitForEndOfFrame();
            SuperController.singleton.BroadcastMessage("OnActionsProviderAvailable", this, SendMessageOptions.DontRequireReceiver);
        }

        public void OnBindingsListRequested(List<object> bindings)
        {
            _customBindings = gameObject.AddComponent<Bindings>();
            _customBindings.Init(this);
            bindings.Add(_customBindings.Settings);
            bindings.AddRange(_customBindings.OnKeyDownActions);
        }

        #region User interface

        private void InitPluginUILeft()
        {
            _titleUIText = this.NewTextField("titleText", "", 36, 100);
            _titleUIText.SetVal($"{nameof(TittyMagic)}\n<size=28>v{version}</size>");

            var modeSelection = this.NewTextField("modeSelection", "", 32, 100);
            modeSelection.SetVal("<size=28>\n\n</size><b>Mode selection</b>");
            modeSelection.dynamicText.backgroundColor = Color.clear;

            CreateModeChooser();
            _modeButtonGroup = this.CreateRadioButtonGroup(_modeChooser);
            _staticPhysicsH.modeChooser = _modeChooser;

            this.NewSpacer(10f);
            _softness = this.NewIntSlider("Breast softness", 75f, 0f, 100f);
            this.NewSpacer(10f);
            _linkSoftnessAndGravity = new JSONStorableBool("Link softness and gravity", true);
            _linkSoftnessAndGravityToggle = this.NewToggle(_linkSoftnessAndGravity);
            _gravity = new JSONStorableFloat("Breast gravity", 75f, 0f, 100f);
            _gravitySlider = this.NewIntSlider(_gravity);

            this.NewSpacer(210f);
            _nippleErection = this.NewFloatSlider("Nipple erection", 0f, 0f, 1.0f, "F2");
            _nippleErection.slider.onValueChanged.AddListener((float val) =>
            {
                _nippleErectionMorphH.Update(val);
                _staticPhysicsH.UpdateNipplePhysics(_softnessAmount, val);
            });
        }

        private void CreateModeChooser()
        {
            _modeChooser = new JSONStorableStringChooser(
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
                    UI.UpdateButtonLabels(_modeButtonGroup, mode);
                    StartCoroutine(OnModeChosen(mode));
                }
            );
            _modeChooser.storeType = JSONStorableParam.StoreType.Full;
            RegisterStringChooser(_modeChooser);
        }

        private IEnumerator OnModeChosen(string mode)
        {
            _gravityPhysicsH.LoadSettings(mode);
            _staticPhysicsH.LoadSettings(this, mode);
            if(mode == Mode.ANIM_OPTIMIZED)
            {
                //RelativePosMorphHandler doesn't actually support any other mode currently
                _relativePosMorphH.LoadSettings(mode);
            }
            else
            {
                _gravityMorphH.LoadSettings(mode);
            }

            UpdateToggleAndSliderTexts(mode);
            UpdateModeInfoText(mode);
            UpdateGravityInfoText(mode);

            StartCoroutine(TempDisableModeButtons());

            yield return WaitToBeginRefresh();
        }

        private void UpdateToggleAndSliderTexts(string mode)
        {
            if(mode == Mode.ANIM_OPTIMIZED)
            {
                _linkSoftnessAndGravityToggle.label = "Link softness and mobility";
                _gravitySlider.label = "Breast mobility";
            }
            else
            {
                _linkSoftnessAndGravityToggle.label = "Link softness and gravity";
                _gravitySlider.label = "Breast gravity";
            }
        }

        private void UpdateModeInfoText(string mode)
        {
            string text = UI.Size("\n", 12);
            if(mode == Mode.ANIM_OPTIMIZED)
            {
                text += "Animation optimized mode morphs breasts in response to forces. Breast mobility is increased and more dynamic. Physics settings are similar to Balanced mode.";
            }
            else if(mode == Mode.BALANCED)
            {
                text += "In Balanced mode, breasts have realistic mass. There should be a sense of weight both in animations and when touched.";
            }
            else if(mode == Mode.TOUCH_OPTIMIZED)
            {
                text += "Touch optimized mode lowers breast mass and increases fat back force. Animation is less realistic, but collision is more accurate with hard colliders turned off.";
            }
            _modeInfoText.SetVal(text);
        }

        private void UpdateGravityInfoText(string mode)
        {
            string text;
            if(mode == Mode.ANIM_OPTIMIZED)
            {
                text = "Adjusts the amount of morphing due to forces including gravity.";
            }
            else
            {
                text = "Adjusts the amount of morphing in different poses/orientations.";
            }
            _gravityInfoText.val = UI.Size("\n", 12) + text;
        }

        private IEnumerator TempDisableModeButtons()
        {
            while(_waitStatus != RefreshStatus.WAITING)
            {
                yield return null;
            }

            foreach(var buttonKvp in _modeButtonGroup)
            {
                buttonKvp.Value.button.interactable = false;
            }

            while(_waitStatus != RefreshStatus.DONE)
            {
                yield return null;
            }

            foreach(var buttonKvp in _modeButtonGroup)
            {
                buttonKvp.Value.button.interactable = true;
            }
        }

        private void InitPluginUIRight()
        {
            bool rightSide = true;
            _statusUIText = this.NewTextField("statusText", "", 28, 50, rightSide);
            _statusUIInputField = UI.NewInputField(_statusUIText.dynamicText);
            _statusUIInputField.interactable = false;
            _autoRecalibrateOnSizeChange = this.NewToggle("Auto-recalibrate if size changed", true, rightSide);
            _autoRecalibrateOnSizeChange.storeType = JSONStorableParam.StoreType.Full;

            var recalibrateButton = CreateButton("Recalibrate physics", rightSide);
            recalibrateButton.button.interactable = !_autoRecalibrateOnSizeChange.val;

            _autoRecalibrateOnSizeChange.toggle.onValueChanged.AddListener(val => recalibrateButton.button.interactable = !val);
            recalibrateButton.button.onClick.AddListener(() =>
                StartCoroutine(WaitToBeginRefresh(triggeredManually: true))
            );

            this.NewSpacer(20f, rightSide);
            _modeInfoText = this.NewTextField("Usage Info Area 2", "", 28, 210, rightSide);

            this.NewSpacer(10f, rightSide);
            JSONStorableString softnessInfoText = this.NewTextField("Usage Info Area 1", "", 28, 120, rightSide);
            softnessInfoText.SetVal(
                UI.Size("\n", 12) +
                "Adjusts soft physics settings from very firm to very soft."
            );

            this.NewSpacer(75f, rightSide);
            _gravityInfoText = this.NewTextField("GravityInfoText", "", 28, 120, true);
            this.NewSpacer(75f, rightSide);

#if DEBUG_ON
            _debugUIText = this.NewTextField("debugText", "", 28, 200, rightSide);
#endif
        }

        private void InitSliderListeners()
        {
            _softnessSCM = _softness.slider.gameObject.AddComponent<SliderClickMonitor>();
            _gravitySCM = _gravity.slider.gameObject.AddComponent<SliderClickMonitor>();

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

        #endregion User interface

        private void RefreshFromSliderChanged()
        {
            if(_loadingFromJson)
            {
                return;
            }
            if(_modeChooser.val == Mode.ANIM_OPTIMIZED && _waitStatus != RefreshStatus.WAITING)
            {
                StartCoroutine(WaitToBeginRefresh());
            }
            else
            {
                _staticPhysicsH.FullUpdate(_softnessAmount, _nippleErection.val);
            }
        }

        private void Update()
        {
            if(_waitStatus != RefreshStatus.DONE)
            {
                return;
            }

            if(_modeChooser.val == Mode.ANIM_OPTIMIZED)
            {
                _trackLeftNipple.UpdateAnglesAndDepthDiff();
                _trackRightNipple.UpdateAnglesAndDepthDiff();
            }

            _chestRoll = Roll(_chestRb.rotation);
            _chestPitch = Pitch(_chestRb.rotation);

#if DEBUG_ON
            _debugUIText.SetVal(
                $"softness {_softnessAmount}\n" +
                $"gravity {_gravityAmount}"
            );
#endif
        }

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
            if(_refreshStatus == RefreshStatus.MASS_STARTED)
            {
                return;
            }

            if(_refreshStatus > RefreshStatus.MASS_STARTED)
            {
                // simulate gravityPhysics when upright
                Quaternion zero = new Quaternion(0, 0, 0, -1);
                _gravityPhysicsH.Update(0, 0, _massAmount, _gravityAmount);

                // simulate force of gravity when upright
                // 0.75f is a hack, for some reason a normal gravity force pushes breasts too much down,
                // causing the neutral position to be off by a little
                Vector3 force = _chestRb.transform.up * 0.75f * -Physics.gravity.magnitude;
                _pectoralRbLeft.AddForce(force, ForceMode.Acceleration);
                _pectoralRbRight.AddForce(force, ForceMode.Acceleration);
                if(_modeChooser.val == Mode.ANIM_OPTIMIZED)
                {
                    if(_refreshStatus == RefreshStatus.MASS_OK)
                    {
                        StartCoroutine(CalibrateNipplesTracking());
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

            _timeSinceListenersChecked += Time.deltaTime;
            if(_timeSinceListenersChecked >= _listenersCheckInterval)
            {
                _timeSinceListenersChecked -= _listenersCheckInterval;
                if(CheckListeners())
                {
                    StartCoroutine(WaitToBeginRefresh());
                    return;
                }
            }

            if(_waitStatus != RefreshStatus.DONE)
            {
                return;
            }

            if(_modeChooser.val == Mode.ANIM_OPTIMIZED)
            {
                if(_relativePosMorphH.IsEnabled())
                {
                    _relativePosMorphH.Update(
                        _trackLeftNipple.AngleY,
                        _trackRightNipple.AngleY,
                        _trackLeftNipple.DepthDiff,
                        _trackRightNipple.DepthDiff,
                        _trackLeftNipple.AngleX,
                        _trackRightNipple.AngleX,
                        _verticalAngleMassMultiplier,
                        _rollAngleMassMultiplier,
                        _backDepthDiffMassMultiplier,
                        _forwardDepthDiffMassMultiplier,
                        _massAmount,
                        _gravityAmount
                    );
                }
            }
            else
            {
                if(_gravityMorphH.IsEnabled())
                {
                    _gravityMorphH.Update(_chestRoll, _chestPitch, _massAmount, 0.75f * _gravityAmount);
                }
            }

            if(_gravityPhysicsH.IsEnabled())
            {
                _gravityPhysicsH.Update(_chestRoll, _chestPitch, _massAmount, _gravityAmount);
            }
        }

        public IEnumerator WaitToBeginRefresh(bool triggeredManually = false)
        {
            _waitStatus = RefreshStatus.WAITING;
            while(_refreshStatus != RefreshStatus.DONE && _refreshStatus != -1)
            {
                yield return null;
            }

            yield return BeginRefresh(triggeredManually);
        }

        public IEnumerator BeginRefresh(bool triggeredManually = false)
        {
            _animationWasSetFrozen =
                SuperController.singleton.freezeAnimationToggle?.isOn == true ||
                SuperController.singleton.freezeAnimationToggleAlt?.isOn == true;
            SuperController.singleton.SetFreezeAnimation(true);

            if(!triggeredManually)
            {
                // ensure refresh actually begins only once listeners report no change
                yield return new WaitForSeconds(_listenersCheckInterval);
                while(_breastMorphListener.Changed() ||
                    _atomScaleListener.Changed() ||
                    _softnessSCM.isDown ||
                    _gravitySCM.isDown)
                {
                    yield return new WaitForSeconds(0.1f);
                }

                yield return new WaitForSeconds(0.33f);
            }

            _refreshStatus = RefreshStatus.MASS_STARTED;

            _settingsMonitor.enabled = false;

            // simulate breasts zero G
            _pectoralRbLeft.useGravity = false;
            _pectoralRbRight.useGravity = false;

            // zero pose morphs
            if(_modeChooser.val == Mode.ANIM_OPTIMIZED)
            {
                _relativePosMorphH.ResetAll();
            }
            else
            {
                _gravityMorphH.ResetAll();
            }
            _gravityPhysicsH.ZeroAll();

            yield return new WaitForSeconds(0.33f);

            float duration = 0;
            float interval = 0.1f;
            while(
                duration < 1f &&
                !EqualWithin(1000f, _massEstimate, DetermineMassEstimate(_atomScaleListener.Value))
            )
            {
                yield return new WaitForSeconds(interval);
                duration += interval;

                // update mass estimate
                _massEstimate = DetermineMassEstimate(_atomScaleListener.Value);

                // update main static physics
                _massAmount = _staticPhysicsH.SetAndReturnMassVal(_massEstimate);
                _verticalAngleMassMultiplier = -Mathf.Pow(1.67f * _massAmount, 0.53f) + 2.5f;
                _rollAngleMassMultiplier = -Mathf.Pow(_massAmount, 1.75f) + 2.67f;
                _backDepthDiffMassMultiplier = (1 / Mathf.Pow(1/2f * _massAmount, 1/3f)) - 0.51f;
                _forwardDepthDiffMassMultiplier = (1 / Mathf.Pow(1/2f * (_massAmount - 0.03f), 1/4f)) + 1.1f;
                _staticPhysicsH.UpdateMainPhysics(_softnessAmount);
            }
            SetMassUIStatus(_atomScaleListener.Value);
            _staticPhysicsH.FullUpdate(_softnessAmount, _nippleErection.val);
            _gravityPhysicsH.SetBaseValues();

            _refreshStatus = RefreshStatus.MASS_OK;
        }

        private IEnumerator CalibrateNipplesTracking()
        {
            _refreshStatus = RefreshStatus.NEUTRALPOS_STARTED;

            if(_trackLeftNipple.NippleRb?.transform == null || _trackRightNipple.NippleRb?.transform == null)
            {
                var rigidbodies = containingAtom.GetComponentsInChildren<Rigidbody>().ToList();
                _trackLeftNipple.NippleRb = rigidbodies.Find(rb => rb.name == "lNipple");
                _trackRightNipple.NippleRb = rigidbodies.Find(rb => rb.name == "rNipple");
                if(_trackLeftNipple.NippleRb?.transform == null || _trackRightNipple.NippleRb?.transform == null)
                {
                    _refreshStatus = RefreshStatus.NEUTRALPOS_OK;
                    yield break;
                }
            }

            yield return new WaitForSeconds(0.67f);

            float duration = 0;
            float interval = 0.1f;
            while(
                duration < 2f &&
                !_trackLeftNipple.CalibrationDone() &&
                !_trackRightNipple.CalibrationDone()
            )
            {
                yield return new WaitForSeconds(interval);
                duration += interval;
                _trackLeftNipple.Calibrate();
                _trackRightNipple.Calibrate();
            }

            _refreshStatus = RefreshStatus.NEUTRALPOS_OK;
        }

        private void EndRefresh()
        {
            _pectoralRbLeft.useGravity = true;
            _pectoralRbRight.useGravity = true;
            SuperController.singleton.SetFreezeAnimation(_animationWasSetFrozen);
            _settingsMonitor.enabled = true;
            _loadingFromJson = false;
            _waitStatus = RefreshStatus.DONE;
            _refreshStatus = RefreshStatus.DONE;
        }

        private bool CheckListeners()
        {
            return _autoRecalibrateOnSizeChange.val &&
                _waitStatus != RefreshStatus.WAITING &&
                (_breastMorphListener.Changed() || _atomScaleListener.Changed()) &&
                DeviatesAtLeast(_massEstimate, DetermineMassEstimate(_atomScaleListener.Value), percent: 10);
        }

        public void RefreshRateDependentPhysics()
        {
            _staticPhysicsH.UpdateRateDependentPhysics(_softnessAmount);
        }

        private float DetermineMassEstimate(float atomScale)
        {
            float mass = _breastMassCalculator.Calculate(atomScale);
            if(mass > Const.MASS_MAX)
                return Const.MASS_MAX;

            if(mass < Const.MASS_MIN)
                return Const.MASS_MIN;

            return mass;
        }

        private void SetMassUIStatus(float atomScale)
        {
            float mass = _breastMassCalculator.Calculate(atomScale);
            string text = $"Mass is {RoundToDecimals(mass, 1000f)}kg";
            if(mass > Const.MASS_MAX)
            {
                float excess = RoundToDecimals(mass - Const.MASS_MAX, 1000f);
                text = MassExcessStatus(excess);
            }
            else if(mass < Const.MASS_MIN)
            {
                float shortage = RoundToDecimals(Const.MASS_MIN - mass, 1000f);
                text = MassShortageStatus(shortage);
            }
            _statusUIText.SetVal(text);
            _statusUIInputField.text = text;
        }

        private string MassExcessStatus(float value)
        {
            return $"Mass is {value}kg over the 2kg max";
        }

        private string MassShortageStatus(float value)
        {
            return $"Mass is {value}kg below the 0.1kg min";
        }

        public override void RestoreFromJSON(JSONClass json, bool restorePhysical = true, bool restoreAppearance = true, JSONArray presetAtoms = null, bool setMissingToDefault = true)
        {
            if(json.HasKey("Mode"))
            {
                _loadingFromJson = true;
                var mode = json["Mode"];
                if(mode == "TouchOptimized") // compatibility with 2.1 saves
                    mode = "touch optimized";
                _modeChooser.val = mode;
            }
            base.RestoreFromJSON(json, restorePhysical, restoreAppearance, presetAtoms, setMissingToDefault);
        }

        private string MorphsPath()
        {
            var packageId = GetPackageId(this);
            var path = "Custom/Atom/Person/Morphs/female/everlaster";
            if(string.IsNullOrEmpty(packageId))
            {
                return $"{path}/TM3_Dev/";
            }

            return packageId + $":/{path}/TittyMagic/";
        }

        private void OnRemoveAtom(Atom atom)
        {
            Destroy(_settingsMonitor);
            Destroy(_softnessSCM);
            Destroy(_gravitySCM);
        }

        private void OnDestroy()
        {
            try
            {
                Destroy(_settingsMonitor);
                Destroy(_softnessSCM);
                Destroy(_gravitySCM);
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
            if(_settingsMonitor != null)
                _settingsMonitor.enabled = true;
            if(_gravityPhysicsH != null)
                _gravityPhysicsH.SetInvertJoint2RotationY(false);
            if(CheckListeners())
            {
                StartCoroutine(WaitToBeginRefresh());
            }
        }

        private void OnDisable()
        {
            try
            {
                if(_settingsMonitor != null)
                    _settingsMonitor.enabled = false;
                if(_gravityPhysicsH != null)
                {
                    _gravityPhysicsH.ResetAll();
                    _gravityPhysicsH.SetInvertJoint2RotationY(true);
                }
                if(_gravityMorphH != null)
                    _gravityMorphH.ResetAll();
                if(_modeChooser?.val == Mode.ANIM_OPTIMIZED && _relativePosMorphH != null)
                    _relativePosMorphH.ResetAll();
                if(_nippleErectionMorphH != null)
                    _nippleErectionMorphH.ResetAll();
            }
            catch(Exception e)
            {
                LogError($"OnDisable: {e}");
            }
        }
    }
}
