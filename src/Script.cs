﻿using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;
using static TittyMagic.Utils;
using static TittyMagic.Calc;
using static TittyMagic.Globals;

namespace TittyMagic
{
    internal class Script : MVRScript
    {
        public const string VERSION = "0.0.0";

        private Bindings _customBindings;

        private List<Rigidbody> _rigidbodies;
        private Rigidbody _chestRb;
        private Transform _chestTransform;
        private Rigidbody _pectoralRbLeft;
        private Rigidbody _pectoralRbRight;

        private DAZSkinV2 _skin;

        private BoolSetting _pectoralRbLeftDetectCollisions;
        private BoolSetting _pectoralRbRightDetectCollisions;

        private float _realMassAmount;
        private float _massAmount;
        private float _softnessAmount;
        private float _quicknessAmount;

        private TrackNipple _trackLeftNipple;
        private TrackNipple _trackRightNipple;
        private float _chestRoll;
        private float _chestPitch;

        private SettingsMonitor _settingsMonitor;

        private AtomScaleListener _atomScaleListener;
        private BreastMorphListener _breastMorphListener;
        private BreastVolumeCalculator _breastVolumeCalculator;

        private StaticPhysicsHandler _staticPhysicsH;
        private GravityPhysicsHandler _gravityPhysicsHandler;
        private ForceMorphHandler _forceMorphHandler;
        private GravityOffsetMorphHandler _gravityOffsetMorphHandler;
        private NippleErectionMorphHandler _nippleErectionMorphH;

        private JSONStorableString _titleUIText;

        private JSONStorableString _pluginVersionStorable;
        private JSONStorableBool _autoRefresh;
        private UIDynamicButton _refreshButton;
        private JSONStorableFloat _mass;
        private UIDynamicSlider _massSlider;
        private JSONStorableFloat _softness;
        private JSONStorableFloat _quickness;
        private SliderClickMonitor _massSCM;
        private SliderClickMonitor _softnessSCM;
        private SliderClickMonitor _quicknessSCM;
        private SliderClickMonitor _xPhysicsSCM;
        private SliderClickMonitor _yPhysicsSCM;
        private SliderClickMonitor _zPhysicsSCM;
        private JSONStorableFloat _gravityOffsetMorphing;
        private SliderClickMonitor _offsetMorphingSCM;
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
            _chestTransform = _chestRb.transform;
            _pectoralRbLeft = _rigidbodies.Find(rb => rb.name == "lPectoral");
            _pectoralRbRight = _rigidbodies.Find(rb => rb.name == "rPectoral");

            _pectoralRbLeftDetectCollisions = new BoolSetting(_pectoralRbLeft.detectCollisions);
            _pectoralRbRightDetectCollisions = new BoolSetting(_pectoralRbRight.detectCollisions);
            _pectoralRbLeft.detectCollisions = false;
            _pectoralRbRight.detectCollisions = false;

            _atomScaleListener = new AtomScaleListener(containingAtom.GetStorableByID("rescaleObject").GetFloatJSONParam("scale"));
            _skin = containingAtom.GetComponentInChildren<DAZCharacter>().skin;
            _breastVolumeCalculator = new BreastVolumeCalculator(_skin, _chestRb);

            BREAST_CONTROL.invertJoint2RotationY = false;
            _staticPhysicsH = new StaticPhysicsHandler(_isFemale);
            _gravityPhysicsHandler = new GravityPhysicsHandler(this);
            _gravityOffsetMorphHandler = new GravityOffsetMorphHandler(this);
            _nippleErectionMorphH = new NippleErectionMorphHandler(this);

            if(_isFemale)
            {
                _settingsMonitor = gameObject.AddComponent<SettingsMonitor>();
                _settingsMonitor.Init(containingAtom);
                _breastMorphListener = new BreastMorphListener(GEOMETRY.morphBank1.morphs);
            }
            else
            {
                _breastMorphListener = new BreastMorphListener(GEOMETRY.morphBank1OtherGender.morphs, GEOMETRY.morphBank1.morphs);
            }

            _trackLeftNipple = new TrackNipple(_chestRb, _pectoralRbLeft);
            _trackRightNipple = new TrackNipple(_chestRb, _pectoralRbRight);

            if(_isFemale)
            {
                var nippleRbLeft = _rigidbodies.Find(rb => rb.name == "lNipple");
                var nippleRbRight = _rigidbodies.Find(rb => rb.name == "rNipple");
                _trackLeftNipple.getNipplePosition = () => nippleRbLeft.position;
                _trackRightNipple.getNipplePosition = () => nippleRbRight.position;
            }
            else
            {
                _trackLeftNipple.getNipplePosition = () => AveragePosition(
                    VertexIndexGroups.LEFT_BREAST_CENTER.Select(i => _skin.rawSkinnedWorkingVerts[i]).ToList()
                );
                _trackRightNipple.getNipplePosition = () => AveragePosition(
                    VertexIndexGroups.RIGHT_BREAST_CENTER.Select(i => _skin.rawSkinnedWorkingVerts[i]).ToList()
                );
            }

            _forceMorphHandler = new ForceMorphHandler(this, _trackLeftNipple, _trackRightNipple);

            InitPluginUI();

            _forceMorphHandler.LoadSettings();
            _gravityPhysicsHandler.LoadSettings();
            _gravityOffsetMorphHandler.LoadSettings();
            _staticPhysicsH.LoadSettings(_isFemale);

            if(!_loadingFromJson)
            {
                StartCoroutine(WaitToBeginRefresh(true));
            }
            else
            {
                _initDone = true;
            }

            SuperController.singleton.onAtomRemovedHandlers += OnRemoveAtom;
            StartCoroutine(SetPluginVersion());
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
            _titleUIText = this.NewTextField("titleText", "", 46, 100);
            _titleUIText.SetVal($"<b>{nameof(TittyMagic)}</b><size=36>    v{VERSION}</size>");
            _titleUIText.dynamicText.backgroundColor = Color.clear;
            _titleUIText.dynamicText.textColor = UI.funkyCyan;

            this.NewSpacer(35, true);

            _autoRefresh = this.NewToggle("Auto-update mass", true, true);
            _autoRefresh.toggle.onValueChanged.AddListener(
                val =>
                {
                    if(val && DeviatesAtLeast(_mass.val, CalculateMass(), 10))
                    {
                        StartCoroutine(WaitToBeginRefresh(true, true));
                    }
                }
            );

            _refreshButton = CreateButton("Calculate breast mass", true);
            _refreshButton.height = 60;
            _refreshButton.button.onClick.AddListener(
                () => StartCoroutine(WaitToBeginRefresh(true, true, true))
            );

            CreateMassSlider();

            _autoRefresh.toggle.onValueChanged.AddListener(
                val =>
                {
                    _mass.slider.interactable = !val;
                    UI.ApplySliderStyle(_massSlider);
                }
            );
            _mass.slider.interactable = !_autoRefresh.val;
            UI.ApplySliderStyle(_massSlider);

            CreateSoftnessSlider();
            this.NewSpacer(45, true);
            CreateQuicknessSlider();
            CreateMorphingMultipliers();
            CreateGravityPhysicsMultipliers();
            CreateAdditionalSettings();
        }

        private void CreateMassSlider()
        {
            _mass = new JSONStorableFloat("Breast mass", Const.MASS_MIN, Const.MASS_MIN, Const.MASS_MAX);
            _massSlider = this.NewFloatSlider(_mass, "F3");
            _massSCM = _mass.slider.gameObject.AddComponent<SliderClickMonitor>();
            _mass.slider.onValueChanged.AddListener(val => RefreshFromSliderChanged(true));
        }

        private void CreateSoftnessSlider()
        {
            _softness = this.NewIntSlider("Breast softness", 70f, 0f, 100f);
            _softnessSCM = _softness.slider.gameObject.AddComponent<SliderClickMonitor>();

            _softness.slider.onValueChanged.AddListener(
                val =>
                {
                    if(Math.Abs(CalculateSoftnessAmount(val) - _softnessAmount) < 0.001f)
                    {
                        return;
                    }

                    RefreshFromSliderChanged();
                }
            );

            _softnessAmount = CalculateSoftnessAmount(_softness.val);
        }

        private static float CalculateSoftnessAmount(float val)
        {
            return Mathf.Pow(val / 100f, 0.67f);
        }

        private void CreateQuicknessSlider()
        {
            _quickness = this.NewIntSlider("Breast quickness", 70f, 0f, 100f, true);
            _quicknessSCM = _quickness.slider.gameObject.AddComponent<SliderClickMonitor>();

            _quickness.slider.onValueChanged.AddListener(
                val =>
                {
                    if(Math.Abs(CalculateQuicknessAmount(val) - _quicknessAmount) < 0.001f)
                    {
                        return;
                    }

                    RefreshFromSliderChanged();
                }
            );

            _quicknessAmount = CalculateQuicknessAmount(_quickness.val);
        }

        private static float CalculateQuicknessAmount(float val)
        {
            return (2 * val / 100f) - 1;
        }

        private void CreateMorphingMultipliers()
        {
            var title = this.NewTextField("morphingMultipliers", "", 32, 100);
            title.SetVal("<size=28>\n\n</size><b>Dynamic morphing multipliers</b>");
            title.dynamicText.backgroundColor = Color.clear;

            // values above above 2.4 would actually lower the multiplier due to quadratic regression when nonlinear=true
            var yStorable = this.NewFloatSlider("Morphing Up/down", 1.00f, 0.00f, 3.00f, "F2");
            var xStorable = this.NewFloatSlider("Morphing Left/right", 1.00f, 0.00f, 3.00f, "F2");
            var zStorable = this.NewFloatSlider("Morphing Forward/back", 1.00f, 0.00f, 3.00f, "F2");

            _forceMorphHandler.yMultiplier = new Multiplier(yStorable.slider, Curves.QuadraticRegression);
            _forceMorphHandler.xMultiplier = new Multiplier(xStorable.slider, Curves.QuadraticRegression);
            _forceMorphHandler.zMultiplier = new Multiplier(zStorable.slider, Curves.QuadraticRegressionLesser);

            this.NewSpacer(100, true);
            var gravityInfoText = this.NewTextField("DynamicMorphingInfoText", "", 28, 390, true);
            gravityInfoText.val = UI.Size("\n", 12) +
                "Adjust the amount of breast morphing due to forces including gravity.\n" +
                "\n" +
                "Breasts morph up/down, left/right and forward/back.";
        }

        private void CreateGravityPhysicsMultipliers()
        {
            var title = this.NewTextField("gravityPhysicsMultipliers", "", 32, 100);
            title.SetVal("<size=28>\n\n</size><b>Gravity physics multipliers</b>");
            title.dynamicText.backgroundColor = Color.clear;

            var yStorable = CreateGravityPhysicsMultiplier("Physics Up/down");
            var xStorable = CreateGravityPhysicsMultiplier("Physics Left/right");
            var zStorable = CreateGravityPhysicsMultiplier("Physics Forward/back");

            _gravityPhysicsHandler.yMultiplier = new Multiplier(yStorable.slider);
            _gravityPhysicsHandler.xMultiplier = new Multiplier(xStorable.slider);
            _gravityPhysicsHandler.zMultiplier = new Multiplier(zStorable.slider);

            _gravityOffsetMorphHandler.yMultiplier = new Multiplier(yStorable.slider);

            _xPhysicsSCM = _gravityPhysicsHandler.yMultiplier.slider.gameObject.AddComponent<SliderClickMonitor>();
            _yPhysicsSCM = _gravityPhysicsHandler.xMultiplier.slider.gameObject.AddComponent<SliderClickMonitor>();
            _zPhysicsSCM = _gravityPhysicsHandler.zMultiplier.slider.gameObject.AddComponent<SliderClickMonitor>();

            this.NewSpacer(100, true);
            var infoText = this.NewTextField("GravityPhysicsInfoText", "", 28, 390, true);
            infoText.val = UI.Size("\n", 12) +
                "Adjust the effect of chest angle on breast main physics settings.\n" +
                "\n" +
                "Higher values mean breasts drop more heavily up/down and left/right, " +
                "are more swingy when leaning forward, and less springy when leaning back.";
        }

        private JSONStorableFloat CreateGravityPhysicsMultiplier(string storableName)
        {
            var storable = this.NewFloatSlider(storableName, 1.00f, 0.00f, 2.00f, "F2");
            storable.slider.onValueChanged.AddListener(val => { RefreshFromSliderChanged(); });
            return storable;
        }

        private void CreateAdditionalSettings()
        {
            var title = this.NewTextField("additionalSettings", "", 32, 100);
            title.SetVal("<size=28>\n\n</size><b>Additional settings</b>");
            title.dynamicText.backgroundColor = Color.clear;

            _gravityOffsetMorphing = this.NewFloatSlider("Gravity offset morphing", 1.00f, 0.00f, 2.00f, "F2");
            _offsetMorphingSCM = _gravityOffsetMorphing.slider.gameObject.AddComponent<SliderClickMonitor>();

            _gravityOffsetMorphing.slider.onValueChanged.AddListener(
                val => { RefreshFromSliderChanged(); }
            );

            this.NewSpacer(100, true);
            var infoText = this.NewTextField("GravityOffsetMorphingInfoText", "", 28, 115, true);
            infoText.val = UI.Size("\n", 12) +
                "Rotate breasts up when upright to compensate for negative Up/Down Angle Target.";

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

        #endregion User interface

        private void RefreshFromSliderChanged(bool refreshMass = false)
        {
            if(!_loadingFromJson && _waitStatus != RefreshStatus.WAITING)
            {
                StartCoroutine(WaitToBeginRefresh(refreshMass));
            }
        }

        private void FixedUpdate()
        {
            try
            {
                if(_refreshStatus == RefreshStatus.MASS_STARTED)
                {
                    return;
                }

                if(_refreshStatus > RefreshStatus.MASS_STARTED)
                {
                    EndRefresh();
                }

                if(!_initDone || _waitStatus != RefreshStatus.DONE)
                {
                    return;
                }

                _timeSinceListenersChecked += Time.deltaTime;
                if(_timeSinceListenersChecked >= LISTENERS_CHECK_INTERVAL)
                {
                    _timeSinceListenersChecked -= LISTENERS_CHECK_INTERVAL;
                    if(CheckListeners())
                    {
                        StartCoroutine(WaitToBeginRefresh(true));
                        return;
                    }
                }

                var rotation = _chestTransform.rotation;
                _chestRoll = Roll(rotation);
                _chestPitch = Pitch(rotation);

                _trackLeftNipple.UpdateAnglesAndDepthDiff();
                _trackRightNipple.UpdateAnglesAndDepthDiff();

                RunHandlers();
            }
            catch(Exception e)
            {
                LogError($"FixedUpdate: {e}");
                enabled = false;
            }
        }

        private void RunHandlers(bool updateGravityPhysics = true)
        {
            if(_forceMorphHandler.IsEnabled())
            {
                _forceMorphHandler.Update(
                    _chestRoll,
                    _chestPitch,
                    _realMassAmount
                );
            }

            if(_gravityPhysicsHandler.IsEnabled())
            {
                if(updateGravityPhysics)
                {
                    _gravityPhysicsHandler.Update(
                        _chestRoll,
                        _chestPitch,
                        _massAmount,
                        _softnessAmount
                    );
                }

                _gravityOffsetMorphHandler.Update(
                    _chestRoll,
                    _chestPitch,
                    _realMassAmount,
                    _softnessAmount,
                    _gravityOffsetMorphing.val
                );
            }
        }

        public IEnumerator WaitToBeginRefresh(bool refreshMass, bool fromToggleOrButton = false, bool? useNewMass = null)
        {
            if(useNewMass == null)
            {
                useNewMass = _autoRefresh.val;
            }

            _waitStatus = RefreshStatus.WAITING;

            while(_refreshStatus != RefreshStatus.DONE && _refreshStatus != -1)
            {
                yield return null;
            }

            if(_refreshStatus != RefreshStatus.PRE_REFRESH_STARTED && _refreshStatus != RefreshStatus.PRE_REFRESH_OK)
            {
                PreRefresh(refreshMass, useNewMass.Value);
            }

            if(!fromToggleOrButton)
            {
                // ensure refresh actually begins only once listeners report no change
                yield return new WaitForSeconds(LISTENERS_CHECK_INTERVAL);

                while(
                    _breastMorphListener.Changed() ||
                    _atomScaleListener.Changed() ||
                    (_massSCM != null && _massSCM.isDown) ||
                    (_softnessSCM != null && _softnessSCM.isDown) ||
                    (_quicknessSCM != null && _quicknessSCM.isDown) ||
                    (_xPhysicsSCM != null && _xPhysicsSCM.isDown) ||
                    (_yPhysicsSCM != null && _yPhysicsSCM.isDown) ||
                    (_zPhysicsSCM != null && _zPhysicsSCM.isDown) ||
                    (_offsetMorphingSCM != null && _offsetMorphingSCM.isDown)
                )
                {

                    yield return new WaitForSeconds(0.1f);
                }

                yield return new WaitForSeconds(0.33f);
            }

            while(_refreshStatus != RefreshStatus.PRE_REFRESH_OK)
            {
                yield return null;
            }

            _softnessAmount = CalculateSoftnessAmount(_softness.val);
            _quicknessAmount = CalculateQuicknessAmount(_quickness.val);

            yield return RefreshMass(refreshMass, useNewMass.Value);
        }

        private void PreRefresh(bool refreshMass, bool useNewMass)
        {
            _refreshStatus = RefreshStatus.PRE_REFRESH_STARTED;
            bool mainToggleFrozen =
                SuperController.singleton.freezeAnimationToggle != null &&
                SuperController.singleton.freezeAnimationToggle.isOn;
            bool altToggleFrozen =
                SuperController.singleton.freezeAnimationToggleAlt != null &&
                SuperController.singleton.freezeAnimationToggleAlt.isOn;
            _animationWasSetFrozen = mainToggleFrozen || altToggleFrozen;
            SuperController.singleton.SetFreezeAnimation(true);

            _chestRoll = 0;
            _chestPitch = 0;

            _trackLeftNipple.ResetAnglesAndDepthDiff();
            _trackRightNipple.ResetAnglesAndDepthDiff();

            if(refreshMass)
            {
                UpdateMassValueAndAmounts(useNewMass);
                SetMorphingExtraMultipliers();
            }

            _softnessAmount = CalculateSoftnessAmount(_softness.val);
            _quicknessAmount = CalculateQuicknessAmount(_quickness.val);

            _staticPhysicsH.UpdateMainPhysics(_softnessAmount, _quicknessAmount);
            if(_isFemale)
            {
                _staticPhysicsH.UpdateSoftPhysics(_softnessAmount, _quicknessAmount);
                _staticPhysicsH.UpdateNipplePhysics(_softnessAmount, _nippleErection.val);
            }

            RunHandlers(false);
            _refreshStatus = RefreshStatus.PRE_REFRESH_OK;
        }

        private IEnumerator RefreshMass(bool refreshMass, bool useNewMass)
        {
            _refreshStatus = RefreshStatus.MASS_STARTED;
            if(_isFemale)
            {
                _settingsMonitor.enabled = false;
            }

            // simulate breasts zero G
            _pectoralRbLeft.useGravity = false;
            _pectoralRbRight.useGravity = false;

            yield return new WaitForSeconds(0.33f);
            RunHandlers(false);

            _softnessAmount = CalculateSoftnessAmount(_softness.val);
            _quicknessAmount = CalculateQuicknessAmount(_quickness.val);

            if(refreshMass)
            {
                yield return RefreshMass(useNewMass);
            }

            _staticPhysicsH.UpdateMainPhysics(_softnessAmount, _quicknessAmount);
            if(_isFemale)
            {
                _staticPhysicsH.UpdateSoftPhysics(_softnessAmount, _quicknessAmount);
                _staticPhysicsH.UpdateNipplePhysics(_softnessAmount, _nippleErection.val);
            }

            _gravityPhysicsHandler.SetBaseValues();
            _refreshStatus = RefreshStatus.MASS_OK;
        }

        private IEnumerator RefreshMass(bool useNewMass)
        {
            float duration = 0;
            const float interval = 0.1f;
            while(duration < 0.5f)
            {
                yield return new WaitForSeconds(interval);
                duration += interval;

                UpdateMassValueAndAmounts(useNewMass);
                _staticPhysicsH.UpdateMainPhysics(_softnessAmount, _quicknessAmount);
            }

            if(_autoRefresh.val)
            {
                _mass.defaultVal = _mass.val;
            }

            SetMorphingExtraMultipliers();
        }

        private void SetMorphingExtraMultipliers()
        {
            float softnessMultiplier = Mathf.Lerp(0.5f, 1.14f, _softnessAmount);
            _forceMorphHandler.yMultiplier.extraMultiplier = softnessMultiplier * (3.15f - (1.40f * _realMassAmount));
            _forceMorphHandler.xMultiplier.extraMultiplier = softnessMultiplier * (3.55f - (1.40f * _realMassAmount));
            _forceMorphHandler.zMultiplier.extraMultiplier = softnessMultiplier * (3.8f - (1.5f * _realMassAmount));
            _forceMorphHandler.zMultiplier.oppositeExtraMultiplier = softnessMultiplier * (3.8f - (1.5f * _realMassAmount));
            _gravityOffsetMorphHandler.yMultiplier.extraMultiplier = 1.16f - _realMassAmount;
        }

        private IEnumerator CalibrateNipplesTracking()
        {
            _refreshStatus = RefreshStatus.NEUTRALPOS_STARTED;

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

        private void EndRefresh()
        {
            try
            {
                if(_gravityPhysicsHandler.IsEnabled())
                {
                    // simulate gravityPhysics when upright
                    _gravityPhysicsHandler.Update(0, 0, _massAmount, _softnessAmount);
                    _gravityOffsetMorphHandler.Update(0, 0, _massAmount, _softnessAmount, _gravityOffsetMorphing.val);
                }

                // simulate force of gravity when upright
                // 0.75f is a hack, for some reason a normal gravity force pushes breasts too much down,
                // causing the neutral position to be off by a little
                var force = _chestTransform.up * (0.75f * -Physics.gravity.magnitude);
                _pectoralRbLeft.AddForce(force, ForceMode.Acceleration);
                _pectoralRbRight.AddForce(force, ForceMode.Acceleration);
                if(_refreshStatus == RefreshStatus.MASS_OK)
                {
                    StartCoroutine(CalibrateNipplesTracking());
                }
                else if(_refreshStatus == RefreshStatus.NEUTRALPOS_OK)
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
                    _initDone = true;
                }
            }
            catch(Exception e)
            {
                LogError($"EndRefresh: {e}");
                enabled = false;
            }
        }

        private bool CheckListeners()
        {
            return _autoRefresh.val &&
                _waitStatus != RefreshStatus.WAITING &&
                (_breastMorphListener.Changed() || _atomScaleListener.Changed()) &&
                DeviatesAtLeast(_mass.val, CalculateMass(), 10);
        }

        public void UpdateRateDependentPhysics()
        {
            _staticPhysicsH.UpdateRateDependentPhysics(_softnessAmount, _quicknessAmount);
        }

        private void UpdateMassValueAndAmounts(bool useNewMass)
        {
            float mass = CalculateMass();
            _realMassAmount = Mathf.InverseLerp(0, Const.MASS_MAX, mass);
            if(useNewMass)
            {
                _massAmount = _realMassAmount;
                _mass.val = mass;
            }
            else
            {
                _massAmount = Mathf.InverseLerp(0, Const.MASS_MAX, _mass.val);
            }

            BREAST_CONTROL.mass = _mass.val;
            _staticPhysicsH.realMassAmount = _realMassAmount;
            _staticPhysicsH.massAmount = _massAmount;
        }

        private float CalculateMass()
        {
            return Mathf.Clamp(
                Mathf.Pow(0.78f * _breastVolumeCalculator.Calculate(_atomScaleListener.scale), 1.5f),
                _mass.min,
                _mass.max
            );
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

            base.RestoreFromJSON(json, restorePhysical, restoreAppearance, presetAtoms, setMissingToDefault);
            StartCoroutine(WaitToBeginRefresh(true));
            _loadingFromJson = false;
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
            Destroy(_massSCM);
            Destroy(_softnessSCM);
            Destroy(_quicknessSCM);
            Destroy(_xPhysicsSCM);
            Destroy(_yPhysicsSCM);
            Destroy(_zPhysicsSCM);
            Destroy(_offsetMorphingSCM);
        }

        private void OnDestroy()
        {
            try
            {
                Destroy(_settingsMonitor);
                Destroy(_massSCM);
                Destroy(_softnessSCM);
                Destroy(_quicknessSCM);
                Destroy(_xPhysicsSCM);
                Destroy(_yPhysicsSCM);
                Destroy(_zPhysicsSCM);
                Destroy(_offsetMorphingSCM);
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
            if(_isFemale)
            {
                BREAST_CONTROL.invertJoint2RotationY = false;
            }

            if(_initDone)
            {
                StartCoroutine(WaitToBeginRefresh(false));
            }
        }

        private void OnDisable()
        {
            try
            {
                if(_settingsMonitor != null) _settingsMonitor.enabled = false;

                _gravityPhysicsHandler?.ResetAll();
                _gravityOffsetMorphHandler?.ResetAll();
                if(_isFemale)
                {
                    BREAST_CONTROL.invertJoint2RotationY = true;
                }

                _forceMorphHandler?.ResetAll();
                _nippleErectionMorphH?.ResetAll();
                _pectoralRbLeft.detectCollisions = _pectoralRbLeftDetectCollisions.prevValue;
                _pectoralRbRight.detectCollisions = _pectoralRbRightDetectCollisions.prevValue;
            }
            catch(Exception e)
            {
                LogError($"OnDisable: {e}");
            }
        }
    }
}
