#define DEBUG_ON
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

        public float softnessAmount { get; private set; }
        public float quicknessAmount { get; private set; }

        private static float SoftnessAmount(float val) => val / 100f;
        private static float QuicknessAmount(float val) => 2 * val / 100 - 1;

        private TrackNipple _trackLeftNipple;
        private TrackNipple _trackRightNipple;
        public SettingsMonitor settingsMonitor { get; private set; }

        private AtomScaleListener _atomScaleListener;
        private BreastMorphListener _breastMorphListener;
        private BreastVolumeCalculator _breastVolumeCalculator;

        public MainPhysicsHandler mainPhysicsHandler { get; private set; }
        public HardColliderHandler hardColliderHandler { get; private set; }
        public SoftPhysicsHandler softPhysicsHandler { get; private set; }
        public GravityPhysicsHandler gravityPhysicsHandler { get; private set; }
        public GravityOffsetMorphHandler offsetMorphHandler { get; private set; }
        public NippleErectionHandler nippleErectionHandler { get; private set; }
        public ColliderVisualizer colliderVisualizer { get; private set; }

        // ReSharper disable once MemberCanBePrivate.Global
        public ForcePhysicsHandler forcePhysicsHandler { get; private set; }
        public ForceMorphHandler forceMorphHandler { get; private set; }

        private JSONStorableString _pluginVersionStorable;

        private Tabs _tabs;

        // ReSharper disable MemberCanBePrivate.Global
        public IWindow mainWindow { get; private set; }
        public IWindow morphingWindow { get; private set; }
        public IWindow gravityWindow { get; private set; }
        public IWindow physicsWindow { get; private set; }

        // ReSharper restore MemberCanBePrivate.Global
        public JSONStorableAction recalibratePhysics { get; private set; }
        public JSONStorableAction calculateBreastMass { get; private set; }
        public JSONStorableString statusInfo { get; private set; }
        public JSONStorableBool autoUpdateJsb { get; private set; }
        public JSONStorableFloat softnessJsf { get; private set; }
        public JSONStorableFloat quicknessJsf { get; private set; }
        public JSONStorableAction configureHardColliders { get; private set; }

        public bool recalibrationNeeded { get; set; }
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
            nippleErectionHandler = new NippleErectionHandler(this);

            _trackLeftNipple = new TrackNipple(_chestRb, _pectoralRbLeft);
            _trackRightNipple = new TrackNipple(_chestRb, _pectoralRbRight);

            settingsMonitor = gameObject.AddComponent<SettingsMonitor>();
            settingsMonitor.Init();

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

            forcePhysicsHandler = new ForcePhysicsHandler(this, _trackLeftNipple, _trackRightNipple);
            forceMorphHandler = new ForceMorphHandler(this, _trackLeftNipple, _trackRightNipple);

            LoadSettings();

            mainWindow = new MainWindow(this);
            morphingWindow = new MorphingWindow(this);
            gravityWindow = new GravityWindow(this);
            physicsWindow = new PhysicsWindow(this);

            SetupStorables();
            CreateNavigation();
            NavigateToWindow(mainWindow);

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
            colliderVisualizer.GroupsJSON.val = "Left breast";
            colliderVisualizer.GroupsJSON.defaultVal = "Left breast";
            colliderVisualizer.HighlightMirrorJSON.val = true;

            foreach(string option in new[] { "Select...", "Other", "All" })
            {
                colliderVisualizer.GroupsJSON.choices.Remove(option);
            }
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
            mainPhysicsHandler.LoadSettings();
            softPhysicsHandler.LoadSettings();
            nippleErectionHandler.LoadSettings();
            gravityPhysicsHandler.LoadSettings();
            forcePhysicsHandler.LoadSettings();
            forceMorphHandler.LoadSettings();
            offsetMorphHandler.LoadSettings();
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

            mainPhysicsHandler.massJsf.setCallbackFunction = _ => StartCoroutine(DeferBeginRefresh(refreshMass: true));

            softnessJsf.setCallbackFunction = value =>
            {
                if(Mathf.Abs(value - softnessAmount) > 0.001f)
                {
                    StartCoroutine(DeferBeginRefresh(refreshMass: false));
                }
            };

            quicknessJsf.setCallbackFunction = value =>
            {
                if(Mathf.Abs(value - quicknessAmount) > 0.001f)
                {
                    StartCoroutine(DeferBeginRefresh(refreshMass: false));
                }
            };

            configureHardColliders = new JSONStorableAction("configureHardColliders", () => { });
        }

        private void CreateNavigation()
        {
            _tabs = new Tabs(this);
            _tabs.CreateNavigationButton(
                mainWindow,
                "Control",
                () => NavigateToWindow(mainWindow)
            );
            _tabs.CreateNavigationButton(
                physicsWindow,
                "Physics Params",
                () => NavigateToWindow(physicsWindow)
            );
            _tabs.CreateNavigationButton(
                morphingWindow,
                "Morph Multipliers",
                () => NavigateToWindow(morphingWindow)
            );
            _tabs.CreateNavigationButton(
                gravityWindow,
                "Gravity Multipliers",
                () => NavigateToWindow(gravityWindow)
            );
        }

        private void NavigateToWindow(IWindow window)
        {
            _tabs.activeWindow?.Clear();
            _tabs.ActivateTab(window);
            window.Rebuild();
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

            if(_tabs.activeWindow == mainWindow)
            {
                if(mainWindow.GetActiveNestedWindow() != null)
                {
                    colliderVisualizer.ShowPreviewsJSON.val = true;
                }
            }
            else if(_tabs.activeWindow == physicsWindow)
            {
                var parameterWindow = physicsWindow.GetActiveNestedWindow() as ParameterWindow;
                parameterWindow?.SyncAllMultiplierSliderValues();
            }
        }

        private void ActionsOnUIClosed()
        {
            RecalibrateOnNavigation();
            colliderVisualizer.ShowPreviewsJSON.val = false;

            try
            {
                colliderVisualizer.DestroyAllPreviews();
            }
            catch(Exception e)
            {
                LogError($"Failed to destroy collider visualizer previews. {e}");
            }

            try
            {
                mainWindow.GetActiveNestedWindow()?.ClosePopups();
            }
            catch(Exception e)
            {
                LogError($"Failed to close popups in collider configuration window. {e}");
            }
        }

        public void RecalibrateOnNavigation()
        {
            if(recalibrationNeeded)
            {
                StartCoroutine(DeferBeginRefresh(refreshMass: true, waitForListeners: true));
            }
        }

        private void FixedUpdate()
        {
            try
            {
                if(!initDone || _waiting || containingAtom.grabFreezePhysics && containingAtom.mainController.isGrabbing)
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

#if DEBUG_ON
                statusInfo.val = $"pitch: {RoundToDecimals(pitch, 1000f)}" +
                    $"\nangleY {RoundToDecimals(_trackLeftNipple.angleY, 1000f)}" +
                    $"\nangleX {RoundToDecimals(_trackLeftNipple.angleX, 1000f)}";

                ((MainWindow) mainWindow).UpdateCollidersDebugInfo("Pectoral1");
#endif

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
            forcePhysicsHandler.Update(roll, pitch);
            gravityPhysicsHandler.Update(roll, pitch);
        }

        private void UpdateDynamicMorphs(float roll, float pitch)
        {
            forceMorphHandler.Update(roll, pitch);
            offsetMorphHandler.Update(roll, pitch);
        }

        public void StartRefreshCoroutine(bool refreshMass, bool waitForListeners) =>
            StartCoroutine(DeferBeginRefresh(refreshMass, waitForListeners));

        private IEnumerator DeferBeginRefresh(
            bool refreshMass,
            bool waitForListeners = false,
            bool? useNewMass = null
        )
        {
            if(_loadingFromJson)
            {
                yield break;
            }

            _waiting = true;
            recalibrationNeeded = false;

            if(_refreshInProgress)
            {
                if(!_refreshQueued && !((MainWindow) mainWindow).GetSlidersForRefresh().Any(slider => slider.IsClickDown()))
                {
                    _refreshQueued = true;
                }
                else
                {
                    yield break;
                }
            }

            while(_refreshInProgress)
            {
                yield return null;
            }

            _refreshQueued = false;
            _refreshInProgress = true;

            if(useNewMass == null)
            {
                useNewMass = autoUpdateJsb.val;
            }

            PreRefresh(refreshMass, useNewMass.Value);

            if(!waitForListeners)
            {
                // ensure refresh actually begins only once listeners report no change
                yield return new WaitForSeconds(0.33f);
                float waited = 0.33f;

                while(
                    _breastMorphListener.Changed() ||
                    _atomScaleListener.Changed() ||
                    ((MainWindow) mainWindow).GetSlidersForRefresh().Any(slider => slider.IsClickDown())
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

            softnessAmount = SoftnessAmount(softnessJsf.val);
            quicknessAmount = QuicknessAmount(quicknessJsf.val);

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
            }

            mainPhysicsHandler.UpdatePhysics();
            softPhysicsHandler.UpdatePhysics();
            nippleErectionHandler.Update();
        }

        private IEnumerator Refresh(bool refreshMass, bool useNewMass)
        {
            if(settingsMonitor != null)
            {
                settingsMonitor.enabled = false;
            }

            SetBreastsUseGravity(false);

            yield return new WaitForSeconds(0.5f);
            if(refreshMass)
            {
                yield return RefreshMass(useNewMass);
            }

            mainPhysicsHandler.UpdatePhysics();
            softPhysicsHandler.UpdatePhysics();
            nippleErectionHandler.Update();
            SetMorphingExtraMultipliers();

            yield return CalibrateNipplesTracking();

            SetBreastsUseGravity(true);
            if(Gender.isFemale)
            {
                hardColliderHandler.SyncAllOffsets();
            }

            SuperController.singleton.SetFreezeAnimation(_animationWasSetFrozen);

            if(settingsMonitor != null)
            {
                settingsMonitor.enabled = true;
            }

            if(!_refreshQueued)
            {
                _waiting = false;
            }

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
                mainPhysicsHandler.UpdatePhysics();
            }

            if(autoUpdateJsb.val)
            {
                mainPhysicsHandler.massJsf.defaultVal = mainPhysicsHandler.massJsf.val;
            }
        }

        private void SetMorphingExtraMultipliers()
        {
            float mass = mainPhysicsHandler.realMassAmount;

            forceMorphHandler.upDownExtraMultiplier =
                Curves.Exponential1(softnessAmount, 1.73f, 1.68f, 0.88f, m: 0.93f, s: 0.56f) * Curves.MorphingCurve(mass);
            forceMorphHandler.forwardExtraMultiplier = Mathf.Lerp(1.00f, 1.20f, softnessAmount) * Curves.DepthMorphingCurve(mass);
            forceMorphHandler.backExtraMultiplier = Mathf.Lerp(0.80f, 1.00f, softnessAmount) * Curves.DepthMorphingCurve(mass);
            forceMorphHandler.leftRightExtraMultiplier =
                Curves.Exponential1(softnessAmount, 1.73f, 1.68f, 0.88f, m: 0.93f, s: 0.56f) * Curves.MorphingCurve(mass);

            offsetMorphHandler.upDownExtraMultiplier = 1.16f - mass;
        }

        private IEnumerator SimulateUprightPose()
        {
            while(_calibrating)
            {
                // simulate gravityPhysics when upright
                UpdateDynamicPhysics(roll: 0, pitch: 0);
                UpdateDynamicMorphs(roll: 0, pitch: 0);

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
            StartCoroutine(SimulateUprightPose());

            float duration = 0;
            const float interval = 0.05f;
            while(
                duration < 1.5f &&
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
            AddBreastMassToJson(jsonClass);
            jsonClass["originalMainPhysics"] = mainPhysicsHandler.GetOriginalsJSON();
            jsonClass["originalHardColliders"] = hardColliderHandler.GetOriginalsJSON();
            jsonClass["originalSoftPhysics"] = softPhysicsHandler.GetOriginalsJSON();
            needsStore = true;
            return jsonClass;
        }

        private void AddBreastMassToJson(JSONClass jsonClass)
        {
            if(jsonClass.HasKey(autoUpdateJsb.name) && !jsonClass[autoUpdateJsb.name].AsBool)
            {
                jsonClass[mainPhysicsHandler.massJsf.name].AsFloat = mainPhysicsHandler.massJsf.val;
            }
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

            if(jsonClass.HasKey(autoUpdateJsb.name) && !jsonClass[autoUpdateJsb.name].AsBool && jsonClass.HasKey(mainPhysicsHandler.massJsf.name))
            {
                mainPhysicsHandler.massJsf.defaultVal = jsonClass[mainPhysicsHandler.massJsf.name].AsFloat;
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

            StartCoroutine(DeferBeginRefresh(refreshMass: true));
        }

        private void OnRemoveAtom(Atom atom)
        {
            Destroy(settingsMonitor);
            Destroy(colliderVisualizer);
            Destroy(hardColliderHandler);
            DestroyAllSliderClickMonitors();
        }

        private void OnDestroy()
        {
            try
            {
                Destroy(settingsMonitor);
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
                if(settingsMonitor != null)
                {
                    settingsMonitor.enabled = true;
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
                if(settingsMonitor != null)
                {
                    settingsMonitor.enabled = false;
                }

                if(hardColliderHandler != null)
                {
                    hardColliderHandler.enabled = false;
                }

                mainPhysicsHandler?.RestoreOriginalPhysics();
                softPhysicsHandler?.RestoreOriginalPhysics();
                offsetMorphHandler?.ResetAll();
                forceMorphHandler?.ResetAll();
                nippleErectionHandler?.Reset();
            }
            catch(Exception e)
            {
                LogError($"OnDisable: {e}");
            }
        }
    }
}
