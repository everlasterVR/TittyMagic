using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;
using TittyMagic.UI;
using UnityEngine.UI;
using static TittyMagic.Utils;
using static TittyMagic.Calc;

namespace TittyMagic
{
    internal class Script : MVRScript
    {
        public const string VERSION = "v0.0.0";

        private Bindings _customBindings;
        private FrequencyRunner _listenersCheckRunner;

        private Rigidbody _chestRb;
        private Transform _chestTransform;
        private Rigidbody _pectoralRbLeft;
        private Rigidbody _pectoralRbRight;

        private DAZSkinV2 _skin;

        private float _softnessAmount;
        private float _quicknessAmount;

        private TrackNipple _trackLeftNipple;
        private TrackNipple _trackRightNipple;
        private SettingsMonitor _settingsMonitor;

        private AtomScaleListener _atomScaleListener;
        private BreastMorphListener _breastMorphListener;
        private BreastVolumeCalculator _breastVolumeCalculator;

        public MainPhysicsHandler mainPhysicsHandler { get; private set; }
        public HardColliderHandler hardColliderHandler { get; private set; }
        public SoftPhysicsHandler softPhysicsHandler { get; private set; }
        public GravityPhysicsHandler gravityPhysicsHandler { get; private set; }
        public GravityOffsetMorphHandler offsetMorphHandler { get; private set; }
        public NippleErectionMorphHandler nippleMorphHandler { get; private set; }
        public ColliderVisualizer colliderVisualizer { get; private set; }

        // ReSharper disable once MemberCanBePrivate.Global
        public ForcePhysicsHandler forcePhysicsHandler { get; private set; }
        public ForceMorphHandler forceMorphHandler { get; private set; }

        private JSONStorableString _pluginVersionStorable;

        private Tabs _tabs;

        // ReSharper disable MemberCanBePrivate.Global
        public MainWindow mainWindow { get; private set; }
        public MorphingWindow morphingWindow { get; private set; }
        public GravityWindow gravityWindow { get; private set; }
        public PhysicsWindow physicsWindow { get; private set; }

        // ReSharper restore MemberCanBePrivate.Global
        public JSONStorableAction recalibratePhysics { get; private set; }
        public JSONStorableAction calculateBreastMass { get; private set; }
        public JSONStorableString statusInfo { get; private set; }
        public JSONStorableBool autoUpdateJsb { get; private set; }
        public JSONStorableFloat softnessJsf { get; private set; }
        public JSONStorableFloat quicknessJsf { get; private set; }
        public JSONStorableAction configureHardColliders { get; private set; }

        public bool needsRecalibration { get; set; }
        public bool initDone { get; private set; }

        private bool _loadingFromJson;
        private bool _waiting;
        private bool _refreshInProgress;
        private bool _refreshQueued;
        private bool _calibrating;
        private bool _animationWasSetFrozen;
        private bool _uiOpenPrevFrame;

        public override void Init()
        {
            try
            {
                _pluginVersionStorable = new JSONStorableString("version", "");
                _pluginVersionStorable.storeType = JSONStorableParam.StoreType.Full;
                RegisterString(_pluginVersionStorable);

                if(containingAtom.type != "Person")
                {
                    LogError($"Add to a Person atom, not {containingAtom.type}");
                    return;
                }

                StartCoroutine(DeferInit());
                StartCoroutine(SubscribeToKeybindings());
            }
            catch(Exception e)
            {
                enabled = false;
                LogError($"Init: {e}");
            }
        }

        private IEnumerator DeferInit()
        {
            yield return new WaitForEndOfFrame();
            while(SuperController.singleton.isLoading)
            {
                yield return null;
            }

            morphsPath = GetMorphsPath();

            var geometry = (DAZCharacterSelector) containingAtom.GetStorableByID("geometry");
            Gender.isFemale = geometry.gender == DAZCharacterSelector.Gender.Female;

            _listenersCheckRunner = new FrequencyRunner(0.333f);

            morphsControlUI = Gender.isFemale ? geometry.morphsControlUI : geometry.morphsControlUIOtherGender;
            var rigidbodies = containingAtom.GetComponentsInChildren<Rigidbody>().ToList();
            _chestRb = rigidbodies.Find(rb => rb.name == "chest");
            _chestTransform = _chestRb.transform;

            var breastControl = (AdjustJoints) containingAtom.GetStorableByID(Gender.isFemale ? "BreastControl" : "PectoralControl");
            _pectoralRbLeft = breastControl.joint2.GetComponent<Rigidbody>();
            _pectoralRbRight = breastControl.joint1.GetComponent<Rigidbody>();

            _atomScaleListener = new AtomScaleListener(containingAtom.GetStorableByID("rescaleObject").GetFloatJSONParam("scale"));
            _skin = containingAtom.GetComponentInChildren<DAZCharacter>().skin;
            _breastVolumeCalculator = new BreastVolumeCalculator(_skin, _chestRb);

            SetupColliderVisualizer();

            mainPhysicsHandler = new MainPhysicsHandler(this, breastControl, _pectoralRbLeft, _pectoralRbRight);
            hardColliderHandler = gameObject.AddComponent<HardColliderHandler>();
            hardColliderHandler.Init();

            softPhysicsHandler = new SoftPhysicsHandler(this);
            gravityPhysicsHandler = new GravityPhysicsHandler(this);
            offsetMorphHandler = new GravityOffsetMorphHandler(this);
            nippleMorphHandler = new NippleErectionMorphHandler(this);

            _trackLeftNipple = new TrackNipple(_chestRb, _pectoralRbLeft);
            _trackRightNipple = new TrackNipple(_chestRb, _pectoralRbRight);

            _settingsMonitor = gameObject.AddComponent<SettingsMonitor>();
            _settingsMonitor.Init();

            if(Gender.isFemale)
            {
                yield return DeferSetupTrackFemaleNipples();
            }
            else
            {
                _trackLeftNipple.getNipplePosition = () => AveragePosition(
                    VertexIndexGroup.LEFT_BREAST_CENTER.Select(i => _skin.rawSkinnedWorkingVerts[i]).ToList()
                );
                _trackRightNipple.getNipplePosition = () => AveragePosition(
                    VertexIndexGroup.RIGHT_BREAST_CENTER.Select(i => _skin.rawSkinnedWorkingVerts[i]).ToList()
                );
            }

            if(Gender.isFemale)
            {
                _breastMorphListener = new BreastMorphListener(geometry.morphBank1.morphs);
            }
            else
            {
                _breastMorphListener = new BreastMorphListener(geometry.morphBank1OtherGender.morphs, geometry.morphBank1.morphs);
            }

            forcePhysicsHandler = new ForcePhysicsHandler(this, mainPhysicsHandler, softPhysicsHandler, _trackLeftNipple, _trackRightNipple);
            forceMorphHandler = new ForceMorphHandler(this, _trackLeftNipple, _trackRightNipple);

            LoadSettings();

            mainWindow = new MainWindow(this);
            morphingWindow = new MorphingWindow(this);
            gravityWindow = new GravityWindow(this);
            physicsWindow = new PhysicsWindow(this);

            SetupStorables();
            CreateNavigation();
            NavigateToWindow(mainWindow, PostNavigateToMainWindow);
            InitializeValuesAppliedByListeners();

            if(!_loadingFromJson)
            {
                StartCoroutine(DeferBeginRefresh(refreshMass: true));
            }
            else
            {
                initDone = true;
            }

            SuperController.singleton.onAtomRemovedHandlers += OnRemoveAtom;
            StartCoroutine(DeferSetPluginVersion());
        }

        private void SetupColliderVisualizer()
        {
            colliderVisualizer = gameObject.AddComponent<ColliderVisualizer>();
            var groups = new List<Group>
            {
                new Group("Off", @"$off"), //match nothing
                new Group("Both breasts", @"[lr](Pectoral\d)"),
                new Group("Left breast", @"lPectoral\d"),
            };
            colliderVisualizer.Init(this, groups);
            colliderVisualizer.PreviewOpacityJSON.val = 1;
            colliderVisualizer.PreviewOpacityJSON.defaultVal = 1;
            colliderVisualizer.SelectedPreviewOpacityJSON.val = 1;
            colliderVisualizer.SelectedPreviewOpacityJSON.defaultVal = 1;
            colliderVisualizer.GroupsJSON.val = "Both breasts";
            colliderVisualizer.GroupsJSON.defaultVal = "Both breasts";
            colliderVisualizer.HighlightMirrorJSON.val = true;

            foreach(string option in new[] { "Select...", "Other", "All" })
            {
                colliderVisualizer.GroupsJSON.choices.Remove(option);
            }

            colliderVisualizer.enabled = false;
        }

        private IEnumerator DeferSetupTrackFemaleNipples()
        {
            Rigidbody nippleRbLeft = null;
            Rigidbody nippleRbRight = null;
            float timeout = Time.unscaledTime + 3f;
            while((nippleRbLeft == null || nippleRbRight == null) && Time.unscaledTime < timeout)
            {
                var rigidbodies = containingAtom.GetComponentsInChildren<Rigidbody>().ToList();
                nippleRbLeft = rigidbodies.Find(rb => rb.name == "lNipple");
                nippleRbRight = rigidbodies.Find(rb => rb.name == "rNipple");
                yield return new WaitForSecondsRealtime(0.1f);
            }

            if(nippleRbLeft == null || nippleRbRight == null)
            {
                LogError("Init: failed to find nipple rigidbodies. Try: Remove the plugin, enable advanced colliders, then add the plugin.");
                enabled = false;
                yield break;
            }

            _trackLeftNipple.getNipplePosition = () => nippleRbLeft.position;
            _trackRightNipple.getNipplePosition = () => nippleRbRight.position;
        }

        public void LoadSettings()
        {
            mainPhysicsHandler.LoadSettings(_settingsMonitor.softPhysicsEnabled);
            softPhysicsHandler.LoadSettings();
            gravityPhysicsHandler.LoadSettings();
            forcePhysicsHandler.LoadSettings();
            forceMorphHandler.LoadSettings();
            offsetMorphHandler.LoadSettings();
        }

        private void InitializeValuesAppliedByListeners()
        {
            _softnessAmount = CalculateSoftnessAmount(softnessJsf.val);
            _quicknessAmount = CalculateQuicknessAmount(quicknessJsf.val);
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

        private IEnumerator DeferSetPluginVersion()
        {
            while(_loadingFromJson)
            {
                yield return null;
            }

            _pluginVersionStorable.val = $"{VERSION}";
        }

        private void SetupStorables()
        {
            statusInfo = new JSONStorableString("recalibrating", "");
            autoUpdateJsb = this.NewJSONStorableBool("autoUpdateMass", true);
            softnessJsf = this.NewJSONStorableFloat("breastSoftness", 70f, 0f, 100f);
            quicknessJsf = this.NewJSONStorableFloat("breastQuickness", 70f, 0f, 100f);

            recalibratePhysics = new JSONStorableAction(
                "recalibratePhysics",
                () => StartCoroutine(DeferBeginRefresh(refreshMass: true, waitForListeners: true))
            );

            calculateBreastMass = new JSONStorableAction(
                "calculateBreastMass",
                () => StartCoroutine(
                    DeferBeginRefresh(
                        refreshMass: true,
                        waitForListeners: true,
                        useNewMass: true
                    ))
            );

            autoUpdateJsb.setCallbackFunction = value =>
            {
                if(value)
                {
                    StartCoroutine(DeferBeginRefresh(refreshMass: true, waitForListeners: true));
                }
            };

            mainPhysicsHandler.massJsf.setCallbackFunction = _ => RefreshFromSliderChanged(refreshMass: true);

            softnessJsf.setCallbackFunction = value =>
            {
                if(Math.Abs(CalculateSoftnessAmount(value) - _softnessAmount) > 0.001f)
                {
                    RefreshFromSliderChanged();
                }
            };

            quicknessJsf.setCallbackFunction = value =>
            {
                if(Math.Abs(CalculateQuicknessAmount(value) - _quicknessAmount) > 0.001f)
                {
                    RefreshFromSliderChanged();
                }
            };

            nippleMorphHandler.nippleErectionJsf.setCallbackFunction = value =>
            {
                nippleMorphHandler.Update(value);
                if(_settingsMonitor.softPhysicsEnabled)
                {
                    softPhysicsHandler.UpdateNipplePhysics(
                        mainPhysicsHandler.massAmount,
                        _softnessAmount,
                        value
                    );
                }
            };

            configureHardColliders = new JSONStorableAction("configureHardColliders", () => { });
        }

        private void CreateNavigation()
        {
            _tabs = new Tabs(this);
            _tabs.CreateNavigationButton(
                mainWindow.Id(),
                "Control",
                () => NavigateToWindow(mainWindow, PostNavigateToMainWindow)
            );
            _tabs.CreateNavigationButton(
                physicsWindow.Id(),
                "Physics Params",
                () => NavigateToWindow(physicsWindow)
            );
            _tabs.CreateNavigationButton(
                morphingWindow.Id(),
                "Morph Multipliers",
                () => NavigateToWindow(morphingWindow)
            );
            _tabs.CreateNavigationButton(
                gravityWindow.Id(),
                "Gravity Multipliers",
                () => NavigateToWindow(gravityWindow)
            );
        }

        private void NavigateToWindow(IWindow window, Action postNavigateAction = null)
        {
            _tabs.activeWindow?.Clear();
            _tabs.activeWindow?.ActionsOnWindowClosed();
            _tabs.ActivateTab(window.Id());
            _tabs.activeWindow = window;
            window.Rebuild();

            postNavigateAction?.Invoke();
        }

        public void PostNavigateToMainWindow()
        {
            var elements = _tabs.activeWindow.GetElements();

            elements[autoUpdateJsb.name].AddListener(value =>
                elements[mainPhysicsHandler.massJsf.name].SetActiveStyle(!value, true)
            );
            elements[mainPhysicsHandler.massJsf.name].SetActiveStyle(!autoUpdateJsb.val, true);

            if(Gender.isFemale)
            {
                elements[hardColliderHandler.enabledJsb.name].AddListener(value =>
                    elements[configureHardColliders.name].SetActiveStyle(value, true)
                );

                elements[configureHardColliders.name].SetActiveStyle(hardColliderHandler.enabledJsb.val, true);
            }
        }

        private static float CalculateSoftnessAmount(float val) => Mathf.Pow(val / 100f, 0.67f);

        private static float CalculateQuicknessAmount(float val) => 2 * val / 100 - 1;

        private void RefreshFromSliderChanged(bool refreshMass = false)
        {
            if(!_loadingFromJson)
            {
                StartCoroutine(DeferBeginRefresh(refreshMass));
            }
        }

        private void Update()
        {
            try
            {
                CheckUIOpenedOrClosed();
            }
            catch(Exception e)
            {
                LogError($"Update: {e}");
                enabled = false;
            }
        }

        private void CheckUIOpenedOrClosed()
        {
            bool uiOpen = UITransform.gameObject.activeInHierarchy;
            if(uiOpen && !_uiOpenPrevFrame)
            {
                StartCoroutine(ActionsOnUIOpened());
            }
            else if(!uiOpen && _uiOpenPrevFrame)
            {
                ActionsOnUIClosed();
            }

            _uiOpenPrevFrame = uiOpen;
        }

        private IEnumerator ActionsOnUIOpened()
        {
            yield return new WaitForEndOfFrame();

            var background = rightUIContent.parent.parent.parent.transform.GetComponent<Image>();
            background.color = new Color(0.85f, 0.85f, 0.85f);

            while(_tabs?.activeWindow == null)
            {
                yield return null;
            }

            softPhysicsHandler.ReverseSyncSoftPhysicsOn();
            softPhysicsHandler.ReverseSyncSyncAllowSelfCollision();

            if(_tabs.activeWindow.Id() == mainWindow.Id() && mainWindow.nestedWindowActive)
            {
                colliderVisualizer.ShowPreviewsJSON.val = true;
                colliderVisualizer.enabled = true;
            }
        }

        private void ActionsOnUIClosed()
        {
            RecalibrateOnNavigation();
            colliderVisualizer.ShowPreviewsJSON.val = false;
            colliderVisualizer.enabled = false;

            if(_tabs.activeWindow.Id() == mainWindow.Id() && mainWindow.nestedWindowActive)
            {
                mainWindow.nestedWindow.ClosePopups();
            }
        }

        public void RecalibrateOnNavigation()
        {
            if(needsRecalibration)
            {
                StartCoroutine(DeferBeginRefresh(refreshMass: true, waitForListeners: true));
            }
        }

        private void FixedUpdate()
        {
            try
            {
                if(!initDone || _waiting)
                {
                    return;
                }

                bool morphsOrScaleChanged = _listenersCheckRunner.Run(() =>
                    _breastMorphListener.Changed() || _atomScaleListener.Changed()
                );
                if(morphsOrScaleChanged && autoUpdateJsb.val && !_waiting)
                {
                    StartCoroutine(DeferBeginRefresh(refreshMass: true));
                    return;
                }

                _trackLeftNipple.UpdateAnglesAndDepthDiff();
                _trackRightNipple.UpdateAnglesAndDepthDiff();

                var rotation = _chestTransform.rotation;
                float roll = Roll(rotation);
                float pitch = Pitch(rotation);

                UpdateDynamicPhysics(roll, pitch);
                UpdateDynamicMorphs(roll, pitch);
            }
            catch(Exception e)
            {
                LogError($"FixedUpdate: {e}");
                enabled = false;
            }
        }

        private void UpdateDynamicPhysics(float roll, float pitch)
        {
            forcePhysicsHandler.Update(roll, pitch, mainPhysicsHandler.realMassAmount);
            gravityPhysicsHandler.Update(roll, pitch, mainPhysicsHandler.massAmount, _softnessAmount);
        }

        private void UpdateDynamicMorphs(float roll, float pitch)
        {
            forceMorphHandler.Update(roll, pitch, mainPhysicsHandler.realMassAmount);
            offsetMorphHandler.Update(roll, pitch, mainPhysicsHandler.realMassAmount, _softnessAmount);
        }

        private void UpdateStaticPhysics()
        {
            mainPhysicsHandler.UpdatePhysics(_softnessAmount, _quicknessAmount);
            if(_settingsMonitor.softPhysicsEnabled)
            {
                softPhysicsHandler.UpdatePhysics(
                    mainPhysicsHandler.massAmount,
                    mainPhysicsHandler.realMassAmount,
                    _softnessAmount,
                    _quicknessAmount
                );
                softPhysicsHandler.UpdateNipplePhysics(
                    mainPhysicsHandler.massAmount,
                    _softnessAmount,
                    nippleMorphHandler.nippleErectionJsf.val
                );
            }
        }

        public void UpdateRateDependentPhysics()
        {
            mainPhysicsHandler.UpdateRateDependentPhysics(_softnessAmount, _quicknessAmount);
            if(_settingsMonitor.softPhysicsEnabled)
            {
                softPhysicsHandler.UpdateRateDependentPhysics(
                    mainPhysicsHandler.massAmount,
                    mainPhysicsHandler.realMassAmount,
                    _softnessAmount,
                    _quicknessAmount
                );
            }
        }

        public void StartRefreshCoroutine(bool refreshMass, bool waitForListeners) =>
            StartCoroutine(DeferBeginRefresh(refreshMass, waitForListeners));

        private IEnumerator DeferBeginRefresh(
            bool refreshMass,
            bool waitForListeners = false,
            bool? useNewMass = null
        )
        {
            _waiting = true;
            needsRecalibration = false;
            if(useNewMass == null)
            {
                useNewMass = autoUpdateJsb.val;
            }

            if(!_refreshQueued && !mainWindow.GetSlidersForRefresh().Any(slider => slider.IsClickDown()))
            {
                if(_refreshInProgress)
                {
                    _refreshQueued = true;
                }

                while(_refreshInProgress)
                {
                    yield return null;
                }
            }
            else if(_refreshInProgress)
            {
                yield break;
            }

            _refreshQueued = false;
            _refreshInProgress = true;

            PreRefresh(refreshMass, useNewMass.Value);

            if(!waitForListeners)
            {
                // ensure refresh actually begins only once listeners report no change
                yield return new WaitForSeconds(0.33f);
                float waited = 0.33f;

                while(
                    _breastMorphListener.Changed() ||
                    _atomScaleListener.Changed() ||
                    mainWindow.GetSlidersForRefresh().Any(slider => slider.IsClickDown())
                )
                {
                    yield return new WaitForSeconds(0.1f);
                    waited += 0.1f;
                    if(waited > 2)
                    {
                        statusInfo.val = "Waiting...";
                    }
                }

                if(waited > 2)
                {
                    statusInfo.val = "Recalibrating";
                }

                yield return new WaitForSeconds(0.1f);
            }

            _softnessAmount = CalculateSoftnessAmount(softnessJsf.val);
            _quicknessAmount = CalculateQuicknessAmount(quicknessJsf.val);

            yield return Refresh(refreshMass, useNewMass.Value);
        }

        private void PreRefresh(bool refreshMass, bool useNewMass)
        {
            bool mainToggleFrozen =
                SuperController.singleton.freezeAnimationToggle != null &&
                SuperController.singleton.freezeAnimationToggle.isOn;
            bool altToggleFrozen =
                SuperController.singleton.freezeAnimationToggleAlt != null &&
                SuperController.singleton.freezeAnimationToggleAlt.isOn;

            _animationWasSetFrozen = mainToggleFrozen || altToggleFrozen;
            SuperController.singleton.SetFreezeAnimation(true);

            _trackLeftNipple.ResetAnglesAndDepthDiff();
            _trackRightNipple.ResetAnglesAndDepthDiff();

            if(refreshMass)
            {
                mainPhysicsHandler.UpdateMassValueAndAmounts(useNewMass, _breastVolumeCalculator.Calculate(_atomScaleListener.scale));
                SetMorphingExtraMultipliers();
            }

            _softnessAmount = CalculateSoftnessAmount(softnessJsf.val);
            _quicknessAmount = CalculateQuicknessAmount(quicknessJsf.val);

            UpdateStaticPhysics();
            UpdateDynamicPhysics(roll: 0, pitch: 0);
            UpdateDynamicMorphs(roll: 0, pitch: 0);
        }

        private IEnumerator Refresh(bool refreshMass, bool useNewMass)
        {
            if(_settingsMonitor != null)
            {
                _settingsMonitor.enabled = false;
            }

            SetBreastsUseGravity(false);

            yield return new WaitForSeconds(0.33f);
            UpdateDynamicPhysics(roll: 0, pitch: 0);
            UpdateDynamicMorphs(roll: 0, pitch: 0);

            _softnessAmount = CalculateSoftnessAmount(softnessJsf.val);
            _quicknessAmount = CalculateQuicknessAmount(quicknessJsf.val);

            if(refreshMass)
            {
                yield return RefreshMass(useNewMass);
            }

            UpdateStaticPhysics();

            yield return CalibrateNipplesTracking();

            SetBreastsUseGravity(true);
            hardColliderHandler.SyncAllOffsets();

            SuperController.singleton.SetFreezeAnimation(_animationWasSetFrozen);

            if(_settingsMonitor != null)
            {
                _settingsMonitor.enabled = true;
            }

            _waiting = false;
            _refreshInProgress = false;
            statusInfo.val = "";
            initDone = true;
        }

        private void SetBreastsUseGravity(bool value)
        {
            _pectoralRbLeft.useGravity = value;
            _pectoralRbRight.useGravity = value;
        }

        private IEnumerator RefreshMass(bool useNewMass)
        {
            float duration = 0;
            const float interval = 0.1f;
            while(duration < 0.5f)
            {
                yield return new WaitForSeconds(interval);
                duration += interval;

                mainPhysicsHandler.UpdateMassValueAndAmounts(useNewMass, _breastVolumeCalculator.Calculate(_atomScaleListener.scale));
                mainPhysicsHandler.UpdatePhysics(_softnessAmount, _quicknessAmount);
            }

            if(autoUpdateJsb.val)
            {
                mainPhysicsHandler.massJsf.defaultVal = mainPhysicsHandler.massJsf.val;
            }

            SetMorphingExtraMultipliers();
        }

        private void SetMorphingExtraMultipliers()
        {
            float softnessMultiplier = Mathf.Lerp(0.5f, 1.14f, _softnessAmount);
            float mass = mainPhysicsHandler.realMassAmount;

            forceMorphHandler.upDownExtraMultiplier = softnessMultiplier * (3.15f - 1.40f * mass);
            forceMorphHandler.forwardBackExtraMultiplier = softnessMultiplier * (3.8f - 1.5f * mass);
            forceMorphHandler.leftRightExtraMultiplier = softnessMultiplier * (3.55f - 1.40f * mass);

            offsetMorphHandler.upDownExtraMultiplier = 1.16f - mass;
        }

        private IEnumerator SimulateUprightPhysics()
        {
            while(_calibrating)
            {
                // simulate gravityPhysics when upright
                gravityPhysicsHandler.Update(0, 0, mainPhysicsHandler.massAmount, _softnessAmount);
                forcePhysicsHandler.Update(0, 0, mainPhysicsHandler.massAmount);
                offsetMorphHandler.Update(0, 0, mainPhysicsHandler.massAmount, _softnessAmount);

                // simulate force of gravity when upright
                var force = _chestTransform.up * -Physics.gravity.magnitude;
                _pectoralRbLeft.AddForce(force, ForceMode.Acceleration);
                _pectoralRbRight.AddForce(force, ForceMode.Acceleration);

                yield return null;
            }
        }

        private IEnumerator CalibrateNipplesTracking()
        {
            _calibrating = true;
            StartCoroutine(SimulateUprightPhysics());
            yield return new WaitForSeconds(0.5f);

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

            _calibrating = false;
        }

        public RectTransform GetLeftUIContent() => leftUIContent;
        public RectTransform GetRightUIContent() => rightUIContent;

        private string GetMorphsPath()
        {
            string packageId = this.GetPackageId();
            const string path = "Custom/Atom/Person/Morphs/female/everlaster";

            if(string.IsNullOrEmpty(packageId))
            {
                return $"{path}/{nameof(TittyMagic)}_dev";
            }

            return packageId + $":/{path}/{nameof(TittyMagic)}";
        }

        public string PluginPath() =>
            $@"{this.GetPackagePath()}Custom\Scripts\everlaster\TittyMagic";

        public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
        {
            var jsonClass = base.GetJSON(includePhysical, includeAppearance, forceStore);
            jsonClass["originalMainPhysics"] = mainPhysicsHandler.Serialize();
            jsonClass["originalHardColliders"] = hardColliderHandler.Serialize();
            jsonClass["originalSoftPhysics"] = softPhysicsHandler.Serialize();
            needsStore = true;
            return jsonClass;
        }

        public override void RestoreFromJSON(
            JSONClass jsonClass,
            bool restorePhysical = true,
            bool restoreAppearance = true,
            JSONArray presetAtoms = null,
            bool setMissingToDefault = true
        )
        {
            _loadingFromJson = true;
            StartCoroutine(
                DeferRestoreFromJSON(
                    jsonClass,
                    restorePhysical,
                    restoreAppearance,
                    presetAtoms,
                    setMissingToDefault
                )
            );
        }

        private IEnumerator DeferRestoreFromJSON(
            JSONClass jsonClass,
            bool restorePhysical,
            bool restoreAppearance,
            JSONArray presetAtoms,
            bool setMissingToDefault
        )
        {
            while(!initDone)
            {
                yield return null;
            }

            base.RestoreFromJSON(jsonClass, restorePhysical, restoreAppearance, presetAtoms, setMissingToDefault);
            StartCoroutine(DeferBeginRefresh(refreshMass: true));

            if(jsonClass.HasKey("autoUpdateMass") && jsonClass.HasKey("breastMass") && !jsonClass["autoUpdateMass"].AsBool)
            {
                mainPhysicsHandler.massJsf.defaultVal = jsonClass["breastMass"].AsFloat;
            }

            if(jsonClass.HasKey("originalMainPhysics"))
            {
                mainPhysicsHandler.RestoreFromJSON(jsonClass["originalMainPhysics"].AsObject);
            }

            if(jsonClass.HasKey("originalHardColliders"))
            {
                hardColliderHandler.RestoreFromJSON(jsonClass["originalHardColliders"].AsObject);
            }

            if(jsonClass.HasKey("originalSoftPhysics"))
            {
                softPhysicsHandler.RestoreFromJSON(jsonClass["originalSoftPhysics"].AsObject);
            }

            _loadingFromJson = false;
        }

        private void OnRemoveAtom(Atom atom)
        {
            Destroy(_settingsMonitor);
            Destroy(colliderVisualizer);
            Destroy(hardColliderHandler);
            DestroyAllSliderClickMonitors();
        }

        private void OnDestroy()
        {
            try
            {
                Destroy(_settingsMonitor);
                Destroy(colliderVisualizer);
                Destroy(hardColliderHandler);
                DestroyAllSliderClickMonitors();
                SuperController.singleton.onAtomRemovedHandlers -= OnRemoveAtom;
                SuperController.singleton.BroadcastMessage("OnActionsProviderDestroyed", this, SendMessageOptions.DontRequireReceiver);
            }
            catch(Exception e)
            {
                LogError($"OnDestroy: {e}");
            }
        }

        private void DestroyAllSliderClickMonitors()
        {
            mainWindow.GetSliders().ForEach(slider => Destroy(slider.GetSliderClickMonitor()));
            morphingWindow.GetSliders().ForEach(slider => Destroy(slider.GetSliderClickMonitor()));
            gravityWindow.GetSliders().ForEach(slider => Destroy(slider.GetSliderClickMonitor()));
        }

        public void OnEnable()
        {
            try
            {
                if(_settingsMonitor != null)
                {
                    _settingsMonitor.enabled = true;
                }

                if(hardColliderHandler != null)
                {
                    hardColliderHandler.enabled = true;
                }

                if(initDone)
                {
                    mainPhysicsHandler?.SaveOriginalPhysicsAndSetPluginDefaults();
                    softPhysicsHandler?.SaveOriginalPhysicsAndSetPluginDefaults();
                    StartCoroutine(DeferBeginRefresh(true));
                }
            }
            catch(Exception e)
            {
                LogError($"OnEnable: {e}");
            }
        }

        private void OnDisable()
        {
            try
            {
                if(_settingsMonitor != null)
                {
                    _settingsMonitor.enabled = false;
                }

                if(hardColliderHandler != null)
                {
                    hardColliderHandler.enabled = false;
                }

                mainPhysicsHandler?.RestoreOriginalPhysics();
                softPhysicsHandler?.RestoreOriginalPhysics();
                offsetMorphHandler?.ResetAll();
                forceMorphHandler?.ResetAll();
                nippleMorphHandler?.ResetAll();
            }
            catch(Exception e)
            {
                LogError($"OnDisable: {e}");
            }
        }
    }
}
