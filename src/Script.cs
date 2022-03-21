using System;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;
using UnityEngine.UI;
using static TittyMagic.Utils;
using static TittyMagic.Calc;
using static TittyMagic.Globals;

namespace TittyMagic
{
    internal class Script : MVRScript
    {
        public static readonly Version VERSION = new Version("0.0.0");

        private Bindings _customBindings;

        private List<Rigidbody> _rigidbodies;
        private Rigidbody _chestRb;
        private Rigidbody _pectoralRbLeft;
        private Rigidbody _pectoralRbRight;

        private float _massEstimate;
        private float _massAmount;
        private float _softnessAmount;

        private TrackNipple _trackLeftNipple;
        private TrackNipple _trackRightNipple;
        private float _chestRoll;
        private float _chestPitch;

        private SettingsMonitor _settingsMonitor;

        private AtomScaleListener _atomScaleListener;
        private BreastMorphListener _breastMorphListener;
        private BreastMassCalculator _breastMassCalculator;

        private StaticPhysicsHandler _staticPhysicsH;
        private GravityPhysicsHandler _gravityPhysicsHandler;
        private GravityMorphHandler _gravityMorphHandler;
        private ForceMorphHandler _forceMorphHandler;
        private NippleErectionMorphHandler _nippleErectionMorphH;

        private JSONStorableString _titleUIText;
        private JSONStorableString _statusUIText;
        private InputField _statusUIInputField;
        private JSONStorableString _modeInfoText;

        private Dictionary<string, UIDynamicButton> _modeButtonGroup;

        private JSONStorableStringChooser _modeChooser;
        private JSONStorableString _pluginVersionStorable;
        private JSONStorableBool _autoRecalibrateOnSizeChange;
        private UIDynamicButton _recalibrateButton;
        private JSONStorableFloat _softness;
        private SliderClickMonitor _softnessSCM;
        private SliderClickMonitor _xPhysicsSCM;
        private SliderClickMonitor _yPhysicsSCM;
        private SliderClickMonitor _zPhysicsSCM;
        private JSONStorableFloat _nippleErection;

        private bool _isFemale;
        private bool _initDone;
        private bool _loadingFromJson;
        private float _timeSinceListenersChecked;
        private const float LISTENERS_CHECK_INTERVAL = 0.0333f;
        private int _waitStatus = -1;
        private int _refreshStatus = -1;
        private bool _animationWasSetFrozen;

        public override void Init()
        {
            try
            {
                _pluginVersionStorable = new JSONStorableString("Version", "");
                _pluginVersionStorable.storeType = JSONStorableParam.StoreType.Full;
                RegisterString(_pluginVersionStorable);

                if(containingAtom.type != "Person")
                {
                    LogError($"Add to a Person atom, not {containingAtom.type}");
                    return;
                }

                StartCoroutine(DelayedInit());
                StartCoroutine(SubscribeToKeybindings());
            }
            catch(Exception e)
            {
                enabled = false;
                LogError($"Init: {e}");
            }
        }

        private IEnumerator DelayedInit()
        {
            yield return new WaitForEndOfFrame();
            while(SuperController.singleton.isLoading)
            {
                yield return null;
            }

            SAVES_DIR = SuperController.singleton.savesDir + @"everlaster\TittyMagicSettings\";
            MORPHS_PATH = MorphsPath();
            PLUGIN_PATH = GetPackagePath(this) + @"Custom\Scripts\everlaster\TittyMagic\";
            GEOMETRY = (DAZCharacterSelector) containingAtom.GetStorableByID("geometry");

            _isFemale = GEOMETRY.gender == DAZCharacterSelector.Gender.Female;
            if(_isFemale)
            {
                MORPHS_CONTROL_UI = GEOMETRY.morphsControlUI;
                BREAST_CONTROL = (AdjustJoints) containingAtom.GetStorableByID("BreastControl");
                BREAST_PHYSICS_MESH = (DAZPhysicsMesh) containingAtom.GetStorableByID("BreastPhysicsMesh");
            }
            else
            {
                MORPHS_CONTROL_UI = GEOMETRY.morphsControlUIOtherGender;
                BREAST_CONTROL = (AdjustJoints) containingAtom.GetStorableByID("PectoralControl");
            }

            _rigidbodies = containingAtom.GetComponentsInChildren<Rigidbody>().ToList();
            _chestRb = _rigidbodies.Find(rb => rb.name == "chest");
            _pectoralRbLeft = _rigidbodies.Find(rb => rb.name == "lPectoral");
            _pectoralRbRight = _rigidbodies.Find(rb => rb.name == "rPectoral");

            _atomScaleListener = new AtomScaleListener(containingAtom.GetStorableByID("rescaleObject").GetFloatJSONParam("scale"));
            var dazCharacter = containingAtom.GetComponentInChildren<DAZCharacter>();
            _breastMassCalculator = new BreastMassCalculator(dazCharacter.skin);

            _staticPhysicsH = new StaticPhysicsHandler(_isFemale);
            _gravityPhysicsHandler = new GravityPhysicsHandler(this);
            _gravityMorphHandler = new GravityMorphHandler(this);
            _nippleErectionMorphH = new NippleErectionMorphHandler(this);

            if(_isFemale)
            {
                InitFemale();
            }
            else
            {
                InitMale();
            }

            SuperController.singleton.onAtomRemovedHandlers += OnRemoveAtom;
            _initDone = true;
            StartCoroutine(SetPluginVersion());
        }

        private void InitFemale()
        {
            try
            {
                _breastMorphListener = new BreastMorphListener(GEOMETRY.morphBank1.morphs, GEOMETRY.morphBank1OtherGender.morphs);

                var nippleRbLeft = _rigidbodies.Find(rb => rb.name == "lNipple");
                var nippleRbRight = _rigidbodies.Find(rb => rb.name == "rNipple");
                _trackLeftNipple = new TrackNipple(_chestRb, _pectoralRbLeft, nippleRbLeft);
                _trackRightNipple = new TrackNipple(_chestRb, _pectoralRbRight, nippleRbRight);

                _settingsMonitor = gameObject.AddComponent<SettingsMonitor>();
                _settingsMonitor.Init(containingAtom);

                _forceMorphHandler = new ForceMorphHandler(this, _trackLeftNipple, _trackRightNipple);

                InitPluginUI();
                SoftnessSliderListener();
                GravityPhysicsSliderListeners();

                if(!_loadingFromJson)
                {
                    _modeChooser.val = Mode.ANIM_OPTIMIZED; // selection causes BeginRefresh;
                }
            }
            catch(Exception e)
            {
                LogError($"InitFemale: {e}");
                enabled = false;
            }
        }

        private void InitMale()
        {
            try
            {
                _breastMorphListener = new BreastMorphListener(GEOMETRY.morphBank1OtherGender.morphs, GEOMETRY.morphBank1.morphs);

                InitPluginUI();
                GravityPhysicsSliderListeners();

                _gravityMorphHandler.LoadSettings(Mode.BALANCED);
                _gravityPhysicsHandler.LoadSettings(Mode.FUTA);
                _staticPhysicsH.LoadSettings(this, Mode.FUTA);
                StartCoroutine(WaitToBeginRefresh());
            }
            catch(Exception e)
            {
                LogError($"InitMale: {e}");
                enabled = false;
            }
        }

        // https://github.com/vam-community/vam-plugins-interop-specs/blob/main/keybindings.md
        private IEnumerator SubscribeToKeybindings()
        {
            yield return new WaitForEndOfFrame();
            SuperController.singleton.BroadcastMessage(
                "OnActionsProviderAvailable",
                this,
                SendMessageOptions.DontRequireReceiver
            );
        }

        public void OnBindingsListRequested(List<object> bindings)
        {
            _customBindings = gameObject.AddComponent<Bindings>();
            _customBindings.Init(this);
            bindings.Add(_customBindings.settings);
            bindings.AddRange(_customBindings.onKeyDownActions);
        }

        private IEnumerator SetPluginVersion()
        {
            while(_loadingFromJson)
            {
                yield return null;
            }

            _pluginVersionStorable.val = $"{VERSION}";
        }

        #region User interface

        private void InitPluginUI()
        {
            _titleUIText = this.NewTextField("titleText", "", 36, 100);
            _titleUIText.SetVal($"{nameof(TittyMagic)}\n<size=28>v{VERSION}</size>");

            _statusUIText = this.NewTextField("statusText", "", 28, 50, true);
            _statusUIInputField = UI.NewInputField(_statusUIText.dynamicText);
            _statusUIInputField.interactable = false;
            _autoRecalibrateOnSizeChange = this.NewToggle("Auto-recalibrate if size changed", true, true);
            _autoRecalibrateOnSizeChange.storeType = JSONStorableParam.StoreType.Full;

            _recalibrateButton = CreateButton("Recalibrate physics", true);
            _recalibrateButton.button.interactable = !_autoRecalibrateOnSizeChange.val;

            _autoRecalibrateOnSizeChange.toggle.onValueChanged.AddListener(
                val => _recalibrateButton.button.interactable = !val
            );

            this.NewSpacer(20f, true);
            _modeInfoText = this.NewTextField("Usage Info Area 2", "", 28, 210, true);

            if(_isFemale)
            {
                InitPluginUIFemale();
            }
            else
            {
                InitPluginUIMale();
            }
        }

        private void InitPluginUIFemale()
        {
            _recalibrateButton.button.onClick.AddListener(
                () => StartCoroutine(WaitToBeginRefresh(true))
            );

            CreateModeChooser();
            CreateSoftnessSlider();
            CreateMorphingMultipliers();
            CreateGravityPhysicsMultipliers();
            CreateAdditionalSettings();
        }

        private void InitPluginUIMale()
        {
            _recalibrateButton.button.onClick.AddListener(
                () => StartCoroutine(WaitToBeginRefresh(true))
            );

            CreateMorphingMultipliers();
            CreateGravityPhysicsMultipliers();
            CreateAdditionalSettings();

            _modeInfoText.val = "Futa mode";
        }

        private void CreateModeChooser()
        {
            var title = this.NewTextField("modeSelection", "", 32, 100);
            title.SetVal("<size=28>\n\n</size><b>Mode selection</b>");
            title.dynamicText.backgroundColor = Color.clear;

            _modeChooser = new JSONStorableStringChooser(
                "Mode",
                new List<string>
                {
                    Mode.ANIM_OPTIMIZED,
                    Mode.BALANCED,
                    Mode.TOUCH_OPTIMIZED,
                },
                "",
                "Mode",
                mode =>
                {
                    UI.UpdateButtonLabels(_modeButtonGroup, mode);
                    StartCoroutine(WaitToBeginRefresh(true, () => OnModeChosen(mode)));
                }
            );
            _modeChooser.storeType = JSONStorableParam.StoreType.Full;
            RegisterStringChooser(_modeChooser);
            _modeButtonGroup = this.CreateRadioButtonGroup(_modeChooser);
        }

        private void CreateSoftnessSlider()
        {
            _softness = this.NewIntSlider("Breast softness", 75f, 0f, 100f);
            var softnessInfoText = this.NewTextField("Usage Info Area 1", "", 28, 120, true);
            softnessInfoText.SetVal(
                UI.Size("\n", 12) +
                "Adjusts soft physics settings from very firm to very soft."
            );
        }

        private void CreateMorphingMultipliers()
        {
            var title = this.NewTextField("morphingMultipliers", "", 32, 100);
            title.SetVal("<size=28>\n\n</size><b>Dynamic morphing multipliers</b>");
            title.dynamicText.backgroundColor = Color.clear;

            var yStorable = new JSONStorableFloat("Morphing Up/down", 1.00f, 0.00f, 2.00f);
            var xStorable = new JSONStorableFloat("Morphing Left/right", 1.00f, 0.00f, 2.00f);
            var zStorable = new JSONStorableFloat("Morphing Forward/back", 1.00f, 0.00f, 2.00f);

            this.NewFloatSlider(yStorable, "F2");
            this.NewFloatSlider(xStorable, "F2");
            this.NewFloatSlider(zStorable, "F2");

            var yMultiplier = new Multiplier(yStorable.slider, true);
            var xMultiplier = new Multiplier(xStorable.slider, true);
            var zMultiplier = new Multiplier(zStorable.slider);

            _forceMorphHandler.yMultiplier = yMultiplier;
            _forceMorphHandler.xMultiplier = xMultiplier;
            _forceMorphHandler.zMultiplier = zMultiplier;

            _gravityMorphHandler.yMultiplier = yMultiplier;
            _gravityMorphHandler.xMultiplier = xMultiplier;
            _gravityMorphHandler.zMultiplier = zMultiplier;

            this.NewSpacer(100f, true);
            var gravityInfoText = this.NewTextField("GravityInfoText", "", 28, 390, true);
            gravityInfoText.val = UI.Size("\n", 12) +
                "Adjusts the amount of breast morphing due to forces (or chest angle in Balanced mode). " +
                "Values much higher than 1.00 generally don't work well, but it might depend on breast shape.";
        }

        private void CreateGravityPhysicsMultipliers()
        {
            var title = this.NewTextField("gravityPhysicsMultipliers", "", 32, 100);
            title.SetVal("<size=28>\n\n</size><b>Gravity physics multipliers</b>");
            title.dynamicText.backgroundColor = Color.clear;

            var yStorable = new JSONStorableFloat("Physics Up/down", 1.00f, 0.00f, 2.00f);
            var xStorable = new JSONStorableFloat("Physics Left/right", 1.00f, 0.00f, 2.00f);
            var zStorable = new JSONStorableFloat("Physics Forward/back", 1.00f, 0.00f, 2.00f);

            this.NewFloatSlider(yStorable, "F2");
            this.NewFloatSlider(xStorable, "F2");
            this.NewFloatSlider(zStorable, "F2");

            var yMultiplier = new Multiplier(yStorable.slider);
            var xMultiplier = new Multiplier(xStorable.slider);
            var zMultiplier = new Multiplier(zStorable.slider);

            _gravityPhysicsHandler.yMultiplier = yMultiplier;
            _gravityPhysicsHandler.xMultiplier = xMultiplier;
            _gravityPhysicsHandler.zMultiplier = zMultiplier;

            this.NewSpacer(100f, true);
            var morphingInfoText = this.NewTextField("MorphingInfoText", "", 28, 390, true);
            morphingInfoText.val = UI.Size("\n", 12) +
                "Adjusts breast physics target angles and center of gravity based on chest angle. " +
                "Higher value means breasts drop more heavily up/down and left/right, and are more swingy when leaning forward/back.";
        }

        private void CreateAdditionalSettings()
        {
            var title = this.NewTextField("additionalSettings", "", 32, 100);
            title.SetVal("<size=28>\n\n</size><b>Additional settings</b>");
            title.dynamicText.backgroundColor = Color.clear;

            _nippleErection = this.NewFloatSlider("Nipple erection", 0.00f, 0.00f, 1.00f, "F2");
            _nippleErection.slider.onValueChanged.AddListener(
                val =>
                {
                    _nippleErectionMorphH.Update(val);
                    if(_isFemale)
                    {
                        _staticPhysicsH.UpdateNipplePhysics(_softnessAmount, val);
                    }
                }
            );
        }

        private void OnModeChosen(string mode)
        {
            _gravityPhysicsHandler.LoadSettings(mode);
            _staticPhysicsH.LoadSettings(this, mode);
            if(mode == Mode.ANIM_OPTIMIZED || mode == Mode.TOUCH_OPTIMIZED)
            {
                _forceMorphHandler.LoadSettings(mode);
            }
            else
            {
                _gravityMorphHandler.LoadSettings(mode);
            }

            UpdateModeInfoText(mode);
            StartCoroutine(TempDisableModeButtons());
        }

        private void UpdateModeInfoText(string mode)
        {
            string text = UI.Size("\n", 12);
            if(mode == Mode.ANIM_OPTIMIZED)
                text += "Animation optimized mode morphs breasts in response to forces. Breast mobility is increased and more dynamic. Physics settings are similar to Balanced mode.";
            else if(mode == Mode.BALANCED)
                text += "In Balanced mode, breasts have realistic mass. There should be a sense of weight both in animations and when touched.";
            else if(mode == Mode.TOUCH_OPTIMIZED)
                text += "WIP";

            _modeInfoText.SetVal(text);
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

        private void SoftnessSliderListener()
        {
            _softnessSCM = _softness.slider.gameObject.AddComponent<SliderClickMonitor>();

            _softness.slider.onValueChanged.AddListener(
                val =>
                {
                    float newAmount = Mathf.Pow(val / 100f, 0.67f);
                    if(Math.Abs(newAmount - _softnessAmount) < 0.001f)
                    {
                        return;
                    }

                    _softnessAmount = newAmount;
                    RefreshFromSliderChanged();
                }
            );

            _softnessAmount = Mathf.Pow(_softness.val / 100f, 0.67f);
        }

        private void GravityPhysicsSliderListeners()
        {
            var xSlider = _gravityPhysicsHandler.xMultiplier.slider;
            var ySlider = _gravityPhysicsHandler.yMultiplier.slider;
            var zSlider = _gravityPhysicsHandler.zMultiplier.slider;

            _xPhysicsSCM = xSlider.gameObject.AddComponent<SliderClickMonitor>();
            _yPhysicsSCM = ySlider.gameObject.AddComponent<SliderClickMonitor>();
            _zPhysicsSCM = zSlider.gameObject.AddComponent<SliderClickMonitor>();

            xSlider.onValueChanged.AddListener(
                val => { RefreshFromSliderChanged(); }
            );
            ySlider.onValueChanged.AddListener(
                val => { RefreshFromSliderChanged(); }
            );
            zSlider.onValueChanged.AddListener(
                val => { RefreshFromSliderChanged(); }
            );
        }

        #endregion User interface

        private void RefreshFromSliderChanged()
        {
            if(_loadingFromJson) return;

            if((_modeChooser.val == Mode.ANIM_OPTIMIZED || _modeChooser.val == Mode.TOUCH_OPTIMIZED) && _waitStatus != RefreshStatus.WAITING)
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
            if(!_initDone || _waitStatus != RefreshStatus.DONE)
            {
                return;
            }

            try
            {
                if(_isFemale && (_modeChooser.val == Mode.ANIM_OPTIMIZED || _modeChooser.val == Mode.TOUCH_OPTIMIZED))
                {
                    _trackLeftNipple.UpdateAnglesAndDepthDiff();
                    _trackRightNipple.UpdateAnglesAndDepthDiff();
                }

                _chestRoll = Roll(_chestRb.rotation);
                _chestPitch = Pitch(_chestRb.rotation);
            }
            catch(Exception e)
            {
                LogError($"Update: {e}");
                enabled = false;
            }
        }

        private void FixedUpdate()
        {
            if(!_initDone)
            {
                return;
            }

            try
            {
                if(_refreshStatus == RefreshStatus.MASS_STARTED) return;

                if(_refreshStatus > RefreshStatus.MASS_STARTED)
                {
                    if(_isFemale)
                    {
                        EndRefreshFemale();
                    }
                    else
                    {
                        EndRefresh();
                    }
                }

                if(_waitStatus != RefreshStatus.DONE) return;

                _timeSinceListenersChecked += Time.deltaTime;
                if(_timeSinceListenersChecked >= LISTENERS_CHECK_INTERVAL)
                {
                    _timeSinceListenersChecked -= LISTENERS_CHECK_INTERVAL;
                    if(CheckListeners())
                    {
                        StartCoroutine(WaitToBeginRefresh());
                        return;
                    }
                }

                if(_isFemale && (_modeChooser.val == Mode.ANIM_OPTIMIZED || _modeChooser.val == Mode.TOUCH_OPTIMIZED))
                {
                    _trackLeftNipple.UpdateAnglesAndDepthDiff();
                    _trackRightNipple.UpdateAnglesAndDepthDiff();
                }

                RunHandlers();
            }
            catch(Exception e)
            {
                LogError($"FixedUpdate: {e}");
                enabled = false;
            }
        }

        private void RunHandlers()
        {
            if(_isFemale && (_modeChooser.val == Mode.ANIM_OPTIMIZED || _modeChooser.val == Mode.TOUCH_OPTIMIZED))
            {
                if(_forceMorphHandler.IsEnabled())
                {
                    _forceMorphHandler.Update(
                        _chestRoll,
                        _chestPitch,
                        _massAmount
                    );
                }
            }
            else if(_gravityMorphHandler.IsEnabled())
            {
                _gravityMorphHandler.Update(
                    _chestRoll,
                    _chestPitch,
                    _massAmount,
                    0.75f * _softnessAmount
                );
            }

            if(_gravityPhysicsHandler.IsEnabled())
            {
                _gravityPhysicsHandler.Update(
                    _chestRoll,
                    _chestPitch,
                    _massAmount,
                    _softnessAmount
                );
            }
        }

        public IEnumerator WaitToBeginRefresh(bool triggeredManually = false, Action onModeChosen = null)
        {
            _waitStatus = RefreshStatus.WAITING;

            while(_refreshStatus != RefreshStatus.DONE && _refreshStatus != -1)
            {
                yield return null;
            }

            onModeChosen?.Invoke();

            PreRefresh();
            yield return BeginRefresh(triggeredManually);
        }

        private void PreRefresh()
        {
            bool mainToggleFrozen =
                SuperController.singleton.freezeAnimationToggle != null &&
                SuperController.singleton.freezeAnimationToggle.isOn;
            bool altToggleFrozen =
                SuperController.singleton.freezeAnimationToggleAlt != null &&
                SuperController.singleton.freezeAnimationToggleAlt.isOn;
            _animationWasSetFrozen = mainToggleFrozen || altToggleFrozen;
            SuperController.singleton.SetFreezeAnimation(true);

            if(_isFemale && (_modeChooser.val == Mode.ANIM_OPTIMIZED || _modeChooser.val == Mode.TOUCH_OPTIMIZED))
            {
                _trackLeftNipple.ResetAnglesAndDepthDiff();
                _trackRightNipple.ResetAnglesAndDepthDiff();
            }

            _chestRoll = 0;
            _chestPitch = 0;
            _massEstimate = EstimateMass();
            _massAmount = _staticPhysicsH.SetAndReturnMassVal(_massEstimate);
            if(_isFemale)
            {
                _staticPhysicsH.FullUpdate(_softnessAmount, _nippleErection.val);
            }
            else
            {
                _staticPhysicsH.UpdatePectoralPhysics();
            }

            RunHandlers();
        }

        private IEnumerator BeginRefresh(bool triggeredManually = false)
        {
            if(!triggeredManually)
            {
                // ensure refresh actually begins only once listeners report no change
                yield return new WaitForSeconds(LISTENERS_CHECK_INTERVAL);

                while(
                    _breastMorphListener.Changed() ||
                    _atomScaleListener.Changed() ||
                    (_softnessSCM != null && _softnessSCM.isDown) ||
                    (_xPhysicsSCM != null && _xPhysicsSCM.isDown) ||
                    (_yPhysicsSCM != null && _yPhysicsSCM.isDown) ||
                    (_zPhysicsSCM != null && _zPhysicsSCM.isDown)
                )
                {
                    yield return new WaitForSeconds(0.1f);
                }

                yield return new WaitForSeconds(0.33f);
            }

            _refreshStatus = RefreshStatus.MASS_STARTED;
            if(_isFemale)
            {
                yield return RefreshFemale();
            }
            else
            {
                yield return RefreshMale();
            }

            _refreshStatus = RefreshStatus.MASS_OK;
        }

        private IEnumerator RefreshFemale()
        {
            _settingsMonitor.enabled = false;

            // simulate breasts zero G
            _pectoralRbLeft.useGravity = false;
            _pectoralRbRight.useGravity = false;

            yield return new WaitForSeconds(0.33f);

            RunHandlers();

            float duration = 0;
            const float interval = 0.1f;
            while(duration < 0.5f)
            {
                yield return new WaitForSeconds(interval);
                duration += interval;

                _massEstimate = EstimateMass();
                _massAmount = _staticPhysicsH.SetAndReturnMassVal(_massEstimate);
                _staticPhysicsH.UpdateMainPhysics(_softnessAmount);
            }

            SetMorphingMultipliers();
            SetMassUIStatus(_atomScaleListener.scale);
            _staticPhysicsH.FullUpdate(_softnessAmount, _nippleErection.val);
            _gravityPhysicsHandler.SetBaseValues();
        }

        private void SetMorphingMultipliers()
        {
            // updates GravityMorphHandler's multipliers as well since it is referencing the same Multiplier objects.
            // 1.05f because these were calibrated before use of QuadraticRegression in morphing sliders where default 1 slider value resulted in 0.95 actual value
            _forceMorphHandler.yMultiplier.extraMultiplier1 = 1.05f * (2.5f - Mathf.Pow(1.67f * _massAmount, 0.53f));
            _forceMorphHandler.xMultiplier.extraMultiplier1 = 1.05f * (2.67f - Mathf.Pow(_massAmount, 1.75f));
            _forceMorphHandler.zMultiplier.extraMultiplier1 = (1 / Mathf.Pow(1 / 2f * _massAmount, 1 / 3f)) - 0.51f;
            _forceMorphHandler.zMultiplier.oppositeExtraMultiplier1 = 17 / (12 * Mathf.Pow(0.9f * (_massAmount + 0.02f), 1 / 4f));

            if(_modeChooser.val == Mode.TOUCH_OPTIMIZED)
            {
                _forceMorphHandler.yMultiplier.extraMultiplier2 = Mathf.Lerp(1.17f, 1.20f, _massAmount);
                _forceMorphHandler.xMultiplier.extraMultiplier2 = Mathf.Lerp(1.17f, 1.23f, _massAmount);
                _forceMorphHandler.zMultiplier.extraMultiplier2 = Mathf.Lerp(1.25f, 1.40f, _massAmount);
                _forceMorphHandler.zMultiplier.oppositeExtraMultiplier2 = Mathf.Lerp(1.25f, 1.40f, _massAmount);
            }
            else
            {
                _forceMorphHandler.yMultiplier.extraMultiplier2 = null;
                _forceMorphHandler.xMultiplier.extraMultiplier2 = null;
                _forceMorphHandler.zMultiplier.extraMultiplier2 = null;
                _forceMorphHandler.zMultiplier.oppositeExtraMultiplier2 = null;
            }
        }

        private IEnumerator RefreshMale()
        {
            yield return new WaitForSeconds(0.33f);

            RunHandlers();

            float duration = 0;
            const float interval = 0.1f;
            while(
                duration < 1f &&
                !EqualWithin(1000f, _massEstimate, EstimateMass())
            )
            {
                yield return new WaitForSeconds(interval);
                duration += interval;

                _massEstimate = EstimateMass();
                _massAmount = _staticPhysicsH.SetAndReturnMassVal(_massEstimate);
                _staticPhysicsH.UpdatePectoralPhysics();
            }

            SetMassUIStatus(_atomScaleListener.scale);
            _gravityPhysicsHandler.SetBaseValues();
        }

        private IEnumerator CalibrateNipplesTracking()
        {
            _refreshStatus = RefreshStatus.NEUTRALPOS_STARTED;

            if(!_trackLeftNipple.HasTransform() || !_trackRightNipple.HasTransform())
            {
                var rigidbodiesList = containingAtom.GetComponentsInChildren<Rigidbody>().ToList();
                _trackLeftNipple.nippleRb = rigidbodiesList.Find(rb => rb.name == "lNipple");
                _trackRightNipple.nippleRb = rigidbodiesList.Find(rb => rb.name == "rNipple");
                if(!_trackLeftNipple.HasTransform() || !_trackRightNipple.HasTransform())
                {
                    _refreshStatus = RefreshStatus.NEUTRALPOS_OK;

                    yield break;
                }
            }

            yield return new WaitForSeconds(0.67f);

            float duration = 0;
            const float interval = 0.1f;
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

        private void EndRefreshFemale()
        {
            try
            {
                // simulate gravityPhysics when upright
                _gravityPhysicsHandler.Update(0, 0, _massAmount, _softnessAmount);

                // simulate force of gravity when upright
                // 0.75f is a hack, for some reason a normal gravity force pushes breasts too much down,
                // causing the neutral position to be off by a little
                var force = _chestRb.transform.up * (0.75f * -Physics.gravity.magnitude);
                _pectoralRbLeft.AddForce(force, ForceMode.Acceleration);
                _pectoralRbRight.AddForce(force, ForceMode.Acceleration);
                if(_modeChooser.val == Mode.ANIM_OPTIMIZED || _modeChooser.val == Mode.TOUCH_OPTIMIZED)
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
            }
            catch(Exception e)
            {
                LogError($"EndRefreshFemale: {e}");
                enabled = false;
            }
        }

        private void EndRefresh()
        {
            _pectoralRbLeft.useGravity = true;
            _pectoralRbRight.useGravity = true;
            SuperController.singleton.SetFreezeAnimation(_animationWasSetFrozen);
            if(_settingsMonitor != null)
            {
                _settingsMonitor.enabled = true;
            }

            _waitStatus = RefreshStatus.DONE;
            _refreshStatus = RefreshStatus.DONE;
        }

        private bool CheckListeners()
        {
            return _autoRecalibrateOnSizeChange.val &&
                _waitStatus != RefreshStatus.WAITING &&
                (_breastMorphListener.Changed() || _atomScaleListener.Changed()) &&
                DeviatesAtLeast(_massEstimate, EstimateMass(), 10);
        }

        public void UpdateRateDependentPhysics()
        {
            _staticPhysicsH.UpdateRateDependentPhysics(_softnessAmount);
        }

        private float EstimateMass()
        {
            return Mathf.Clamp(
                _breastMassCalculator.Calculate(_atomScaleListener.scale),
                Const.MASS_MIN,
                Const.MASS_MAX
            );
        }

        private void SetMassUIStatus(float atomScale)
        {
            float mass = _breastMassCalculator.Calculate(atomScale);
            string text = $"Mass is {RoundToDecimals(mass, 1000f)}kg";
            if(mass > Const.MASS_MAX)
            {
                float value = RoundToDecimals(mass - Const.MASS_MAX, 1000f);
                text = $"Mass is {value}kg over the 2kg max";
            }
            else if(mass < Const.MASS_MIN)
            {
                float value = RoundToDecimals(Const.MASS_MIN - mass, 1000f);
                text = $"Mass is {value}kg below the 0.1kg min";
            }

            _statusUIText.SetVal(text);
            _statusUIInputField.text = text;
        }

        public override void RestoreFromJSON(
            JSONClass json,
            bool restorePhysical = true,
            bool restoreAppearance = true,
            JSONArray presetAtoms = null,
            bool setMissingToDefault = true
        )
        {
            _loadingFromJson = true;
            StartCoroutine(DelayedRestore(json, restorePhysical, restoreAppearance, presetAtoms, setMissingToDefault));
        }

        private IEnumerator DelayedRestore(JSONClass json, bool restorePhysical, bool restoreAppearance, JSONArray presetAtoms, bool setMissingToDefault)
        {
            while(!_initDone)
            {
                yield return null;
            }

            SetJsonMode(json);
            // for some reason, base restore doesn't always trigger mode selection
            _modeChooser.val = json["Mode"];
            base.RestoreFromJSON(json, restorePhysical, restoreAppearance, presetAtoms, setMissingToDefault);

            _loadingFromJson = false;
        }

        private void SetJsonMode(JSONClass json)
        {
            if(!json.HasKey("Mode"))
            {
                json["Mode"] = Mode.ANIM_OPTIMIZED;
            }
            else if(json["Mode"] == "TouchOptimized")
            {
                // compatibility with 2.1 saves
                json["Mode"] = Mode.TOUCH_OPTIMIZED;
            }
            else if(!_modeChooser.choices.Contains(json["Mode"]))
            {
                json["Mode"] = Mode.ANIM_OPTIMIZED;
            }
        }

        private string MorphsPath()
        {
            string packageId = GetPackageId(this);
            const string path = "Custom/Atom/Person/Morphs/female/everlaster";

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
            Destroy(_xPhysicsSCM);
            Destroy(_yPhysicsSCM);
            Destroy(_zPhysicsSCM);
        }

        private void OnDestroy()
        {
            try
            {
                Destroy(_settingsMonitor);
                Destroy(_softnessSCM);
                Destroy(_xPhysicsSCM);
                Destroy(_yPhysicsSCM);
                Destroy(_zPhysicsSCM);
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
            if(_settingsMonitor != null) _settingsMonitor.enabled = true;
            BREAST_CONTROL.invertJoint2RotationY = false;

            if(_initDone && CheckListeners())
            {
                StartCoroutine(WaitToBeginRefresh());
            }
        }

        private void OnDisable()
        {
            try
            {
                if(_settingsMonitor != null) _settingsMonitor.enabled = false;

                _gravityPhysicsHandler?.ResetAll();
                BREAST_CONTROL.invertJoint2RotationY = true;
                _gravityMorphHandler?.ResetAll();
                _forceMorphHandler?.ResetAll();
                _nippleErectionMorphH?.ResetAll();
            }
            catch(Exception e)
            {
                LogError($"OnDisable: {e}");
            }
        }
    }
}
