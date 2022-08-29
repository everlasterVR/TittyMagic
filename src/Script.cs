#define DEBUG_ON
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;
using TittyMagic.UI;
using UnityEngine.UI;
using static TittyMagic.Calc;

namespace TittyMagic
{
    internal class Script : MVRScript
    {
        public const string VERSION = "v0.0.0";

        public static GenerateDAZMorphsControlUI morphsControlUI { get; private set; }
        private Bindings _customBindings;
        private FrequencyRunner _listenersCheckRunner;

        public DAZSkinV2 skin { get; set; }
        private Transform _chestTransform;
        private Rigidbody _pectoralRbLeft;
        private Rigidbody _pectoralRbRight;

        public float softnessAmount { get; private set; }
        public float quicknessAmount { get; private set; }

        private static float SoftnessAmount(float val) => Curves.SoftnessBaseCurve(val / 100f);
        private static float QuicknessAmount(float val) => 2 * val / 100 - 1;

        private TrackNipple _trackLeftNipple;
        private TrackNipple _trackRightNipple;
        public SettingsMonitor settingsMonitor { get; private set; }

        public AtomScaleListener atomScaleListener { get; private set; }
        private BreastMorphListener _breastMorphListener;

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
        public JSONStorableBool autoUpdateJsb { get; private set; }
        public JSONStorableFloat softnessJsf { get; private set; }
        public JSONStorableFloat quicknessJsf { get; private set; }
        public JSONStorableAction configureHardColliders { get; private set; }

        public bool initDone { get; private set; }
        public bool refreshInProgress { get; private set; }

        private bool _loadingFromJson;
        private bool _waiting;
        private bool _refreshQueued;
        private bool _animationWasSetFrozen;

        private UnityEventsListener _pluginUIEventsListener;

        public override void InitUI()
        {
            base.InitUI();
            if(UITransform == null || _pluginUIEventsListener != null)
            {
                return;
            }

            _pluginUIEventsListener = UITransform.gameObject.AddComponent<UnityEventsListener>();

            _pluginUIEventsListener.onDisable.AddListener(() =>
            {
                if(enabled)
                {
                    var activeParameterWindow = _tabs.activeWindow?.GetActiveNestedWindow() as ParameterWindow;
                    var recalibrationAction = activeParameterWindow != null ? activeParameterWindow.recalibrationAction : recalibratePhysics;
                    RecalibrateOnNavigation(recalibrationAction);
                }

                colliderVisualizer.ShowPreviewsJSON.val = false;

                try
                {
                    colliderVisualizer.DestroyAllPreviews();
                }
                catch(Exception e)
                {
                    Utils.LogError($"Failed to destroy collider visualizer previews. {e}");
                }

                try
                {
                    mainWindow.GetActiveNestedWindow()?.ClosePopups();
                }
                catch(Exception e)
                {
                    Utils.LogError($"Failed to close popups in collider configuration window. {e}");
                }
            });

            _pluginUIEventsListener.onEnable.AddListener(() =>
            {
                var background = rightUIContent.parent.parent.parent.transform.GetComponent<Image>();
                background.color = new Color(0.85f, 0.85f, 0.85f);

                softPhysicsHandler.ReverseSyncAllowSelfCollision();

                if(_tabs.activeWindow == mainWindow)
                {
                    var hardCollidersWindow = mainWindow?.GetActiveNestedWindow() as HardCollidersWindow;
                    if(hardCollidersWindow != null)
                    {
                        if(enabled)
                        {
                            colliderVisualizer.ShowPreviewsJSON.val = true;
                        }
                        else
                        {
                            hardCollidersWindow.returnToParent();
                        }
                    }
                }
                else if(_tabs.activeWindow == physicsWindow)
                {
                    var parameterWindow = physicsWindow.GetActiveNestedWindow() as ParameterWindow;
                    parameterWindow?.SyncAllMultiplierSliderValues();
                }
            });
        }

        public override void Init()
        {
            try
            {
                _pluginVersionStorable = new JSONStorableString("version", "");
                _pluginVersionStorable.storeType = JSONStorableParam.StoreType.Full;
                RegisterString(_pluginVersionStorable);

                if(containingAtom.type != "Person")
                {
                    Utils.LogError($"Add to a Person atom, not {containingAtom.type}");
                    return;
                }

                StartCoroutine(DeferInit());
            }
            catch(Exception e)
            {
                enabled = false;
                Utils.LogError($"Init: {e}");
            }
        }

        private IEnumerator DeferInit()
        {
            yield return new WaitForEndOfFrame();
            while(SuperController.singleton.isLoading)
            {
                yield return null;
            }

            /* Morphs path from main dir or var package */
            {
                string packageId = this.GetPackageId();
                const string path = "Custom/Atom/Person/Morphs/female/everlaster";

                if(string.IsNullOrEmpty(packageId))
                {
                    Utils.morphsPath = $"{path}/{nameof(TittyMagic)}_dev";
                }
                else
                {
                    Utils.morphsPath = $"{packageId}:/{path}/{nameof(TittyMagic)}";
                }
            }

            var geometry = (DAZCharacterSelector) containingAtom.GetStorableByID("geometry");
            Gender.isFemale = geometry.gender == DAZCharacterSelector.Gender.Female;

            _listenersCheckRunner = new FrequencyRunner(0.333f);

            morphsControlUI = Gender.isFemale ? geometry.morphsControlUI : geometry.morphsControlUIOtherGender;
            var rigidbodies = containingAtom.GetComponentsInChildren<Rigidbody>().ToList();
            var chestRb = rigidbodies.Find(rb => rb.name == "chest");
            _chestTransform = chestRb.transform;

            var breastControl = (AdjustJoints) containingAtom.GetStorableByID(Gender.isFemale ? "BreastControl" : "PectoralControl");
            _pectoralRbLeft = breastControl.joint2.GetComponent<Rigidbody>();
            _pectoralRbRight = breastControl.joint1.GetComponent<Rigidbody>();

            atomScaleListener = new AtomScaleListener(containingAtom.GetStorableByID("rescaleObject").GetFloatJSONParam("scale"));
            skin = containingAtom.GetComponentInChildren<DAZCharacter>().skin;

            /* Setup collider visualizer */
            {
                colliderVisualizer = gameObject.AddComponent<ColliderVisualizer>();
                var groups = new List<Group>
                {
                    new Group("Off", @"$off"), //match nothing
                    new Group("Both breasts", @"[lr](Pectoral\d)"),
                    new Group("Left breast", @"lPectoral\d"),
                };
                colliderVisualizer.Init(this, groups);
                colliderVisualizer.PreviewOpacityJSON.val = 0.67f;
                colliderVisualizer.PreviewOpacityJSON.defaultVal = 0.67f;
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

            mainPhysicsHandler = new MainPhysicsHandler(this, breastControl, new BreastVolumeCalculator(this, chestRb));
            hardColliderHandler = gameObject.AddComponent<HardColliderHandler>();
            hardColliderHandler.Init();
            softPhysicsHandler = new SoftPhysicsHandler(this);
            gravityPhysicsHandler = new GravityPhysicsHandler(this);
            offsetMorphHandler = new GravityOffsetMorphHandler(this);
            nippleErectionHandler = new NippleErectionHandler(this);

            settingsMonitor = gameObject.AddComponent<SettingsMonitor>();
            settingsMonitor.Init();

            _trackLeftNipple = new TrackNipple(chestRb, _pectoralRbLeft);
            _trackRightNipple = new TrackNipple(chestRb, _pectoralRbRight);

            if(Gender.isFemale)
            {
                yield return DeferSetupTrackFemaleNipples();
            }
            else
            {
                _trackLeftNipple.getNipplePosition = () => AveragePosition(
                    VertexIndexGroup.LEFT_BREAST_CENTER.Select(i => skin.rawSkinnedWorkingVerts[i]).ToList()
                );
                _trackRightNipple.getNipplePosition = () => AveragePosition(
                    VertexIndexGroup.RIGHT_BREAST_CENTER.Select(i => skin.rawSkinnedWorkingVerts[i]).ToList()
                );
            }

            forcePhysicsHandler = new ForcePhysicsHandler(this, _trackLeftNipple, _trackRightNipple);
            forceMorphHandler = new ForceMorphHandler(this, _trackLeftNipple, _trackRightNipple);

            if(Gender.isFemale)
            {
                _breastMorphListener = new BreastMorphListener(geometry.morphBank1.morphs);
            }
            else
            {
                _breastMorphListener = new BreastMorphListener(geometry.morphBank1OtherGender.morphs, geometry.morphBank1.morphs);
            }

            /* Load settings */
            {
                mainPhysicsHandler.LoadSettings();
                softPhysicsHandler.LoadSettings();
                nippleErectionHandler.LoadSettings();
                gravityPhysicsHandler.LoadSettings();
                forcePhysicsHandler.LoadSettings();
                forceMorphHandler.LoadSettings();
                offsetMorphHandler.LoadSettings();
            }

            /* Setup storables */
            {
                autoUpdateJsb = this.NewJSONStorableBool("autoUpdateMass", true);
                softnessJsf = this.NewJSONStorableFloat("breastSoftness", 70f, 0f, 100f);
                quicknessJsf = this.NewJSONStorableFloat("breastQuickness", 70f, 0f, 100f);

                recalibratePhysics = new JSONStorableAction(
                    "recalibratePhysics",
                    () => StartRefreshCoroutine(refreshMass: false)
                );
                RegisterAction(recalibratePhysics);

                calculateBreastMass = new JSONStorableAction(
                    "calculateBreastMass",
                    () => StartRefreshCoroutine(refreshMass: true)
                );
                RegisterAction(calculateBreastMass);

                autoUpdateJsb.setCallbackFunction = value =>
                {
                    if(value)
                    {
                        calculateBreastMass.actionCallback();
                    }
                };

                softnessJsf.setCallbackFunction = value =>
                {
                    if(Mathf.Abs(value - softnessAmount) > 0.001f)
                    {
                        StartRefreshCoroutine(refreshMass: false, waitForListeners: true);
                    }
                };

                quicknessJsf.setCallbackFunction = value =>
                {
                    if(Mathf.Abs(value - quicknessAmount) > 0.001f)
                    {
                        StartRefreshCoroutine(refreshMass: false, waitForListeners: true);
                    }
                };

                configureHardColliders = new JSONStorableAction("configureHardColliders", () => { });
            }

            /* Setup navigation */
            {
                mainWindow = new MainWindow(this);
                morphingWindow = new MorphingWindow(this);
                gravityWindow = new GravityWindow(this);
                physicsWindow = new PhysicsWindow(this);

                _tabs = new Tabs(this, leftUIContent, rightUIContent);
                _tabs.CreateNavigationButton(mainWindow, "Control", NavigateToMainWindow);
                _tabs.CreateNavigationButton(physicsWindow, "Physics Params", NavigateToPhysicsWindow);
                _tabs.CreateNavigationButton(morphingWindow, "Morph Multipliers", NavigateToMorphingWindow);
                _tabs.CreateNavigationButton(gravityWindow, "Gravity Multipliers", NavigateToGravityWindow);
            }

            NavigateToWindow(mainWindow);

            /* Subscribe to keybindings */
            SuperController.singleton.BroadcastMessage("OnActionsProviderAvailable", this, SendMessageOptions.DontRequireReceiver);

            StartCoroutine(DeferSetPluginVersion());

            if(!_loadingFromJson)
            {
                /* Modify atom UI when not loading from JSON */
                {
                    /* Plugin added manually */
                    var atomUIContent = containingAtom.transform.Find("UI/UIPlaceHolderModel/UIModel/Canvas/Panel/Content");
                    if(atomUIContent.gameObject.activeInHierarchy)
                    {
                        StartCoroutine(ModifyAtomUI(atomUIContent));
                    }
                    /* Plugin added with trigger or programmatically without the person UI being open */
                    else
                    {
                        _atomUIEventsListener = atomUIContent.gameObject.AddComponent<UnityEventsListener>();
                        _atomUIEventsListener.onEnable.AddListener(() => StartCoroutine(ModifyAtomUI(atomUIContent)));
                    }
                }

                hardColliderHandler.SaveOriginalUseColliders();
                softPhysicsHandler.SaveOriginalBoolParamValues();
                calculateBreastMass.actionCallback();
            }
            else
            {
                initDone = true;
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
                Utils.LogError("Init: failed to find nipple rigidbodies. Try: Remove the plugin, enable advanced colliders, then add the plugin.");
                enabled = false;
                yield break;
            }

            _trackLeftNipple.getNipplePosition = () => nippleRbLeft.position;
            _trackRightNipple.getNipplePosition = () => nippleRbRight.position;
        }

        // https://github.com/vam-community/vam-plugins-interop-specs/blob/main/keybindings.md
        public void OnBindingsListRequested(List<object> bindings)
        {
            _customBindings = gameObject.AddComponent<Bindings>();
            _customBindings.Init(this, bindings);
        }

        private IEnumerator DeferSetPluginVersion()
        {
            while(_loadingFromJson)
            {
                yield return null;
            }

            _pluginVersionStorable.val = $"{VERSION}";
        }

        public void NavigateToMainWindow() => NavigateToWindow(mainWindow);
        public void NavigateToPhysicsWindow() => NavigateToWindow(physicsWindow);
        public void NavigateToMorphingWindow() => NavigateToWindow(morphingWindow);
        public void NavigateToGravityWindow() => NavigateToWindow(gravityWindow);

        private void NavigateToWindow(IWindow window)
        {
            _tabs.activeWindow?.Clear();
            _tabs.ActivateTab(window);
            window.Rebuild();
        }

        private UnityEventsListener _atomUIEventsListener;
        private List<GameObject> _customUIGameObjects;
        private List<GameObject> _inactivatedUIGameObjects;

        private bool _modifyAtomUIHasBeenCalled;

        private IEnumerator ModifyAtomUI(Transform content)
        {
            // Allow modifying UI only once
            if(_modifyAtomUIHasBeenCalled)
            {
                yield break;
            }

            _modifyAtomUIHasBeenCalled = true;

            var transforms = new Dictionary<string, Transform>
            {
                { "M Pectoral Physics", null },
                { "F Breast Physics 1", null },
                { "F Breast Physics 2", null },
                { "F Breast Presets", null },
            };

            float waited = 0f;
            while(transforms.Values.Any(t => t == null) && waited < 1)
            {
                waited += 0.1f;
                yield return new WaitForSecondsRealtime(0.1f);
                transforms["M Pectoral Physics"] = content.Find("M Pectoral Physics");
                transforms["F Breast Physics 1"] = content.Find("F Breast Physics 1");
                transforms["F Breast Physics 2"] = content.Find("F Breast Physics 2");
                transforms["F Breast Presets"] = content.Find("F Breast Presets");
            }

            if(transforms.Values.Any(t => t == null))
            {
                Debug.Log("Failed to modify person UI - could not find UI transforms.");
                _modifyAtomUIHasBeenCalled = true;
                yield break;
            }

            /* Hide elements in vanilla Breast Physics tabs, add buttons to navigate to plugin UI */
            try
            {
                _inactivatedUIGameObjects = new List<GameObject>();
                foreach(var t in transforms.Values.ToList())
                {
                    foreach(Transform child in t)
                    {
                        if(child.gameObject.activeSelf)
                        {
                            _inactivatedUIGameObjects.Add(child.gameObject);
                            child.gameObject.SetActive(false);
                        }
                    }
                }

                _customUIGameObjects = transforms.Values.ToList()
                    .Select(t => OpenPluginUIButton(t).gameObject)
                    .ToList();
                _modifyAtomUIHasBeenCalled = true;
            }
            catch(Exception e)
            {
                Utils.LogError($"Error modifying person UI: {e}");
            }
        }

        private Transform OpenPluginUIButton(Transform parent)
        {
            var button = this.InstantiateButtonTransform();
            button.SetParent(parent, false);
            Destroy(button.GetComponent<LayoutElement>());
            button.GetComponent<UIDynamicButton>().label = "<b>Open TittyMagic UI</b>";
            button.GetComponent<UIDynamicButton>().button.onClick.AddListener(() => _customBindings.OpenUI());
            return button;
        }

        public bool recalibrationNeeded { get; set; }

        public void RecalibrateOnNavigation(JSONStorableAction recalibrationAction)
        {
            if(recalibrationNeeded)
            {
                recalibrationAction.actionCallback();
            }
        }

        private void Update()
        {
#if DEBUG_ON
            try
            {
                var window = mainWindow?.GetActiveNestedWindow() as HardCollidersWindow;
                if(window != null)
                {
                    window.UpdateCollidersDebugInfo();
                }
            }
            catch(Exception e)
            {
                Utils.LogError($"Update: {e}");
                enabled = false;
            }
#endif
        }

        private void UpdateDynamicPhysics(float roll, float pitch)
        {
            forcePhysicsHandler.Update();
            gravityPhysicsHandler.Update(roll, pitch);
        }

        private void UpdateDynamicMorphs(float roll, float pitch)
        {
            forceMorphHandler.Update(roll, pitch);
            offsetMorphHandler.Update(roll, pitch);
        }

        private void FixedUpdate()
        {
            try
            {
                bool isFreezeGrabbing = containingAtom.grabFreezePhysics && containingAtom.mainController.isGrabbing;
                if(!initDone || _loadingFromJson || _waiting || isFreezeGrabbing)
                {
                    return;
                }

                bool morphsOrScaleChanged = _listenersCheckRunner.Run(() =>
                    _breastMorphListener.Changed() || atomScaleListener.Changed()
                );
                if(morphsOrScaleChanged && autoUpdateJsb.val && !_waiting)
                {
                    StartRefreshCoroutine(refreshMass: true, waitForListeners: true);
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
                Utils.LogError($"FixedUpdate: {e}");
                enabled = false;
            }
        }

        public void StartRefreshCoroutine(bool refreshMass, bool waitForListeners = false) =>
            StartCoroutine(RefreshCo(refreshMass, waitForListeners));

        private IEnumerator RefreshCo(bool refreshMass, bool waitForListeners)
        {
            if(!enabled)
            {
                Utils.LogMessage("Enable the plugin to recalibrate.");
                yield break;
            }

            if(_loadingFromJson)
            {
                yield break;
            }

            /* Setup refresh */
            _waiting = true;
            recalibrationNeeded = false;

            if(refreshInProgress)
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

            while(refreshInProgress)
            {
                yield return null;
            }

            _refreshQueued = false;
            refreshInProgress = true;

            /* Freeze animation and zero adjustments */
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
                UpdateDynamicPhysics(0, 0);
                UpdateDynamicMorphs(0, 0);

                mainPhysicsHandler.UpdatePhysics();
                softPhysicsHandler.UpdatePhysics();
                nippleErectionHandler.Update();
            }

            if(waitForListeners)
            {
                var sliders = ((MainWindow) mainWindow).GetSlidersForRefresh();
                yield return new WaitForSeconds(0.33f);

                while(
                    _breastMorphListener.Changed() ||
                    atomScaleListener.Changed() ||
                    sliders.Any(slider => slider.IsClickDown())
                )
                {
                    yield return new WaitForSeconds(0.1f);
                }

                yield return new WaitForSeconds(0.1f);
            }

            softPhysicsHandler.SyncSoftPhysics();
            softnessAmount = SoftnessAmount(softnessJsf.val);
            quicknessAmount = QuicknessAmount(quicknessJsf.val);

            settingsMonitor.SetEnabled(false);
            SetBreastsUseGravity(false);

            /* Apply a consistent delay, maybe refresh mass*/
            {
                yield return new WaitForSeconds(0.30f);
                float duration = 0;
                const float interval = 0.1f;
                while(duration < 0.5f)
                {
                    yield return new WaitForSeconds(interval);
                    duration += interval;

                    if(refreshMass)
                    {
                        mainPhysicsHandler.UpdateMassValueAndAmounts();
                        mainPhysicsHandler.UpdatePhysics();
                    }
                }
            }

            mainPhysicsHandler.UpdatePhysics();
            softPhysicsHandler.UpdatePhysics();
            nippleErectionHandler.Update();

            /* Set extra multipliers */
            {
                float mass = mainPhysicsHandler.realMassAmount;

                forceMorphHandler.upDownExtraMultiplier =
                    Curves.Exponential1(softnessAmount, 1.73f, 1.68f, 0.88f, m: 0.93f, s: 0.56f)
                    * Curves.MorphingCurve(mass);
                forceMorphHandler.forwardExtraMultiplier =
                    Mathf.Lerp(1.00f, 1.20f, softnessAmount)
                    * Curves.DepthMorphingCurve(mass);
                forceMorphHandler.backExtraMultiplier =
                    Mathf.Lerp(0.80f, 1.00f, softnessAmount)
                    * Curves.DepthMorphingCurve(mass);
                forceMorphHandler.leftRightExtraMultiplier =
                    Curves.Exponential1(softnessAmount, 1.73f, 1.68f, 0.88f, m: 0.93f, s: 0.56f)
                    * Curves.MorphingCurve(mass);

                offsetMorphHandler.upDownExtraMultiplier = 1.16f - mass;
            }

            /* Calibrate nipples tracking and colliders */
            {
                _calibrating = true;
                StartCoroutine(SimulateUprightPose());

                float duration = 0;
                const float interval = 0.05f;
                while(duration < 1.20f)
                {
                    yield return new WaitForSeconds(interval);
                    duration += interval;
                    _trackLeftNipple.Calibrate();
                    _trackRightNipple.Calibrate();
                }

                yield return hardColliderHandler.SyncAll();

                _calibrating = false;
            }

            SetBreastsUseGravity(true);
            SuperController.singleton.SetFreezeAnimation(_animationWasSetFrozen);
            settingsMonitor.SetEnabled(true);

            _waiting = _refreshQueued;
            refreshInProgress = false;
            initDone = true;
        }

        private void SetBreastsUseGravity(bool value)
        {
            _pectoralRbLeft.useGravity = value;
            _pectoralRbRight.useGravity = value;
        }

        private bool _calibrating;

        private IEnumerator SimulateUprightPose()
        {
            while(_calibrating)
            {
                // simulate gravityPhysics when upright
                UpdateDynamicPhysics(roll: 0, pitch: 0);
                UpdateDynamicMorphs(roll: 0, pitch: 0);

                // scale force to be correct for the given fps vs physics rate, for some reason this produces an accurate calibration result
                float rateToPhysicsRateRatio = Time.deltaTime / Time.fixedDeltaTime;
                // simulate force of gravity when upright
                var force = _chestTransform.up * (rateToPhysicsRateRatio * -Physics.gravity.magnitude);
                _pectoralRbLeft.AddForce(force, ForceMode.Acceleration);
                _pectoralRbRight.AddForce(force, ForceMode.Acceleration);

                yield return null;
            }
        }

        public string PluginPath() =>
            $@"{this.GetPackagePath()}Custom\Scripts\everlaster\TittyMagic";

        public override void RestoreFromJSON(
            JSONClass jsonClass,
            bool restorePhysical = true,
            bool restoreAppearance = true,
            JSONArray presetAtoms = null,
            bool setMissingToDefault = true
        )
        {
            _loadingFromJson = true;
            StartCoroutine(DeferRestoreFromJSON(
                jsonClass,
                restorePhysical,
                restoreAppearance,
                presetAtoms,
                setMissingToDefault
            ));
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

            /* Add listener to person UI content for modifying the UI. When loading
             * from JSON, the person UI isn't open when the plugin is loaded.
             */
            var atomUIContent = containingAtom.transform.Find("UI/UIPlaceHolderModel/UIModel/Canvas/Panel/Content");
            _atomUIEventsListener = atomUIContent.gameObject.AddComponent<UnityEventsListener>();
            _atomUIEventsListener.onEnable.AddListener(() => StartCoroutine(ModifyAtomUI(atomUIContent)));

            base.RestoreFromJSON(jsonClass, restorePhysical, restoreAppearance, presetAtoms, setMissingToDefault);
            _loadingFromJson = false;
            hardColliderHandler.SaveOriginalUseColliders();
            softPhysicsHandler.SaveOriginalBoolParamValues();
            calculateBreastMass.actionCallback();
        }

        private void OnDestroy()
        {
            try
            {
                Destroy(settingsMonitor);
                Destroy(colliderVisualizer);
                Destroy(hardColliderHandler);
                mainWindow.GetSliders().ForEach(slider => Destroy(slider.GetPointerUpDownListener()));
                morphingWindow.GetSliders().ForEach(slider => Destroy(slider.GetPointerUpDownListener()));
                gravityWindow.GetSliders().ForEach(slider => Destroy(slider.GetPointerUpDownListener()));
                DestroyImmediate(_pluginUIEventsListener);
                DestroyImmediate(_atomUIEventsListener);
                _customUIGameObjects?.ForEach(Destroy);
                SuperController.singleton.BroadcastMessage("OnActionsProviderDestroyed", this, SendMessageOptions.DontRequireReceiver);
            }
            catch(Exception e)
            {
                Utils.LogError($"OnDestroy: {e}");
            }
        }

        public void OnEnable()
        {
            try
            {
                if(!initDone)
                {
                    return;
                }

                settingsMonitor.SetEnabled(true);
                hardColliderHandler.SetEnabled(true);
                softPhysicsHandler?.SaveOriginalBoolParamValues();
                StartRefreshCoroutine(true);
                _inactivatedUIGameObjects?.ForEach(go => go.SetActive(false));
                _customUIGameObjects?.ForEach(go => go.SetActive(true));
            }
            catch(Exception e)
            {
                Utils.LogError($"OnEnable: {e}");
            }
        }

        private void OnDisable()
        {
            try
            {
                settingsMonitor.SetEnabled(false);
                hardColliderHandler.SetEnabled(false);
                mainPhysicsHandler?.RestoreOriginalPhysics();
                softPhysicsHandler?.RestoreOriginalPhysics();
                offsetMorphHandler?.ResetAll();
                forceMorphHandler?.ResetAll();
                nippleErectionHandler?.Reset();
                _inactivatedUIGameObjects?.ForEach(go => go.SetActive(true));
                _customUIGameObjects?.ForEach(go => go.SetActive(false));
            }
            catch(Exception e)
            {
                Utils.LogError($"OnDisable: {e}");
            }
        }
    }
}
