#define DEBUG_ON
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using TittyMagic.Components;
using TittyMagic.Handlers;
using TittyMagic.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TittyMagic
{
    internal sealed class Script : MVRScript
    {
        public static Script tittyMagic { get; private set; }
        public static readonly Version VERSION = new Version("0.0.0");

        public static GenerateDAZMorphsControlUI morphsControlUI { get; private set; }

        public float softnessAmount { get; private set; }
        public float quicknessAmount { get; private set; }

        public SettingsMonitor settingsMonitor { get; private set; }
        public HardColliderHandler hardColliderHandler { get; private set; }
        public ColliderVisualizer colliderVisualizer { get; private set; }

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

        public CalibrationHelper calibration { get; private set; }

        #region InitUI

        private UnityEventsListener _uiEventsListener;
        private Tabs _tabs;

        public override void InitUI()
        {
            base.InitUI();
            if(UITransform == null || _uiEventsListener != null)
            {
                return;
            }

            _uiEventsListener = UITransform.gameObject.AddComponent<UnityEventsListener>();

            _uiEventsListener.onDisable.AddListener(() =>
            {
                if(calibration.shouldRun)
                {
                    var activeParameterWindow = _tabs.activeWindow?.GetActiveNestedWindow() as ParameterWindow;
                    if(activeParameterWindow != null)
                    {
                        activeParameterWindow.recalibrationAction.actionCallback();
                    }
                    else
                    {
                        recalibratePhysics.actionCallback();
                    }
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

            _uiEventsListener.onEnable.AddListener(() =>
            {
                var background = rightUIContent.parent.parent.parent.transform.GetComponent<Image>();
                background.color = new Color(0.85f, 0.85f, 0.85f);

                SoftPhysicsHandler.ReverseSyncAllowSelfCollision();

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
            });
        }

        #endregion Init UI

        #region Init

        public bool isInitialized { get; private set; }

        public override void Init()
        {
            tittyMagic = this;

            try
            {
                /* Used to store version in save JSON and communicate version to other plugin instances */
                var versionJss = this.NewJSONStorableString("version", "");
                versionJss.val = $"{VERSION}";

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

        private Bindings _customBindings;
        private FrequencyRunner _listenersCheckRunner;

        private List<Rigidbody> _rigidbodies;
        private Transform _chestTransform;
        private Rigidbody _pectoralRbLeft;
        private Rigidbody _pectoralRbRight;
        private TrackNipple _trackLeftNipple;
        private TrackNipple _trackRightNipple;

        private bool _isLoadingFromJson;

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
            _rigidbodies = containingAtom.GetComponentsInChildren<Rigidbody>().ToList();
            var chestRb = _rigidbodies.Find(rb => rb.name == "chest");
            _chestTransform = chestRb.transform;

            var breastControl = (AdjustJoints) containingAtom.GetStorableByID(Gender.isFemale ? "BreastControl" : "PectoralControl");
            _pectoralRbLeft = breastControl.joint2.GetComponent<Rigidbody>();
            _pectoralRbRight = breastControl.joint1.GetComponent<Rigidbody>();
            SetPectoralCollisions(false);

            /* Setup atom scale changed callback */
            {
                var scaleJsf = containingAtom.GetStorableByID("rescaleObject").GetFloatJSONParam("scale");
                scaleJsf.setJSONCallbackFunction = _ =>
                {
                    if(!isInitialized || calibration.isWaiting || containingAtom.grabFreezePhysics && containingAtom.mainController.isGrabbing)
                    {
                        return;
                    }

                    if(autoUpdateJsb.val && !calibration.isWaiting)
                    {
                        StartCalibration(calibratesMass: true, waitsForListeners: true);
                    }
                };
            }

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

            var skin = containingAtom.GetComponentInChildren<DAZCharacter>().skin;

            /* Setup handlers */
            MainPhysicsHandler.Init(breastControl, skin, chestRb);
            hardColliderHandler = gameObject.AddComponent<HardColliderHandler>();
            hardColliderHandler.Init(geometry);
            SoftPhysicsHandler.Init();
            GravityPhysicsHandler.Init();
            GravityOffsetMorphHandler.Init();
            NippleErectionHandler.Init();

            settingsMonitor = gameObject.AddComponent<SettingsMonitor>();
            settingsMonitor.Init();

            /* Setup nipples tracking */
            _trackLeftNipple = new TrackNipple(chestRb, _pectoralRbLeft);
            _trackRightNipple = new TrackNipple(chestRb, _pectoralRbRight);

            if(Gender.isFemale)
            {
                yield return DeferSetupTrackFemaleNipples();
            }
            else
            {
                _trackLeftNipple.getNipplePosition = () => Calc.AveragePosition(
                    VertexIndexGroup.LEFT_BREAST_CENTER.Select(i => skin.rawSkinnedWorkingVerts[i]).ToList()
                );
                _trackRightNipple.getNipplePosition = () => Calc.AveragePosition(
                    VertexIndexGroup.RIGHT_BREAST_CENTER.Select(i => skin.rawSkinnedWorkingVerts[i]).ToList()
                );
            }

            ForcePhysicsHandler.Init(_trackLeftNipple, _trackRightNipple);
            ForceMorphHandler.Init(_trackLeftNipple, _trackRightNipple);

            /* Setup breast morph listening */
            BreastMorphListener.ProcessMorphs(geometry.morphBank1);
            if(!Gender.isFemale)
            {
                BreastMorphListener.ProcessMorphs(geometry.morphBank1OtherGender);
            }

            /* Load settings */
            {
                MainPhysicsHandler.LoadSettings();
                SoftPhysicsHandler.LoadSettings();
                NippleErectionHandler.LoadSettings();
                GravityPhysicsHandler.LoadSettings();
                ForcePhysicsHandler.LoadSettings();
                ForceMorphHandler.LoadSettings();
                GravityOffsetMorphHandler.LoadSettings();
            }

            /* Setup storables */
            {
                autoUpdateJsb = this.NewJSONStorableBool("autoUpdateMass", true);
                softnessJsf = this.NewJSONStorableFloat("breastSoftness", 70f, 0f, 100f);
                quicknessJsf = this.NewJSONStorableFloat("breastQuickness", 70f, 0f, 100f);

                recalibratePhysics = this.NewJSONStorableAction(
                    "recalibratePhysics",
                    () => StartCalibration(calibratesMass: false)
                );

                calculateBreastMass = this.NewJSONStorableAction(
                    "calculateBreastMass",
                    () => StartCalibration(calibratesMass: true)
                );

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
                        StartCalibration(calibratesMass: false, waitsForListeners: true);
                    }
                };

                quicknessJsf.setCallbackFunction = value =>
                {
                    if(Mathf.Abs(value - quicknessAmount) > 0.001f)
                    {
                        StartCalibration(calibratesMass: false, waitsForListeners: true);
                    }
                };

                configureHardColliders = new JSONStorableAction("configureHardColliders", () => { });
            }

            /* Setup navigation */
            {
                mainWindow = new MainWindow();
                morphingWindow = new MorphingWindow();
                gravityWindow = new GravityWindow();
                physicsWindow = new PhysicsWindow();

                _tabs = new Tabs(leftUIContent, rightUIContent);
                _tabs.CreateNavigationButton(mainWindow, "Control", NavigateToMainWindow);
                _tabs.CreateNavigationButton(physicsWindow, "Physics Params", NavigateToPhysicsWindow);
                _tabs.CreateNavigationButton(morphingWindow, "Morph Multipliers", NavigateToMorphingWindow);
                _tabs.CreateNavigationButton(gravityWindow, "Gravity Multipliers", NavigateToGravityWindow);
            }

            NavigateToWindow(mainWindow);

            /* Subscribe to keybindings */
            SuperController.singleton.BroadcastMessage("OnActionsProviderAvailable", this, SendMessageOptions.DontRequireReceiver);

            /* Finish init */
            StartCoroutine(ModifyVamUserInterface());
            InitOtherInstancesIntegration();
            calibration = gameObject.AddComponent<CalibrationHelper>();
            calibration.Init();

            if(!_isLoadingFromJson)
            {
                hardColliderHandler.SaveOriginalUseColliders();
                SoftPhysicsHandler.SaveOriginalBoolParamValues();
                calculateBreastMass.actionCallback();
            }
            else
            {
                isInitialized = true;
            }
        }

        private IEnumerator DeferSetupTrackFemaleNipples()
        {
            Rigidbody nippleRbLeft = null;
            Rigidbody nippleRbRight = null;
            float timeout = Time.unscaledTime + 3f;
            while((nippleRbLeft == null || nippleRbRight == null) && Time.unscaledTime < timeout)
            {
                _rigidbodies = containingAtom.GetComponentsInChildren<Rigidbody>().ToList();
                nippleRbLeft = _rigidbodies.Find(rb => rb.name == "lNipple");
                nippleRbRight = _rigidbodies.Find(rb => rb.name == "rNipple");
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
            _customBindings.Init(bindings);
        }

        private List<Transform> _customUITransforms;
        private List<Transform> _inactivatedUITransforms;

        private IEnumerator ModifyVamUserInterface()
        {
            var transforms = new Dictionary<string, Transform>
            {
                { "M Pectoral Physics", null },
                { "F Breast Physics 1", null },
                { "F Breast Physics 2", null },
                { "F Breast Presets", null },
            };

            float waited = 0f;
            while(transforms.Values.Any(t => t == null) && waited < 30)
            {
                waited += 1f;
                yield return new WaitForSecondsRealtime(1f);
                var content = containingAtom.transform.Find("UI/UIPlaceHolderModel/UIModel/Canvas/Panel/Content");
                transforms["M Pectoral Physics"] = content.Find("M Pectoral Physics");
                transforms["F Breast Physics 1"] = content.Find("F Breast Physics 1");
                transforms["F Breast Physics 2"] = content.Find("F Breast Physics 2");
                transforms["F Breast Presets"] = content.Find("F Breast Presets");
            }

            if(transforms.Values.Any(t => t == null))
            {
                Utils.LogError("Failed modifying UI: no person UI content found.");
                yield break;
            }

            /* Hide elements in vanilla Breast Physics tabs, add buttons to nagivate to plugin UI */
            try
            {
                _inactivatedUITransforms = new List<Transform>();
                foreach(var kvp in transforms)
                {
                    foreach(Transform t in kvp.Value)
                    {
                        _inactivatedUITransforms.Add(t);
                    }
                }

                _inactivatedUITransforms.ForEach(t => t.gameObject.SetActive(false));
                _customUITransforms = transforms.Select(kvp => OpenPluginUIButton(kvp.Value)).ToList();
            }
            catch(Exception e)
            {
                Utils.LogError($"Failed modifying UI: {e}");
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

        #region Integration

        private List<JSONStorable> _otherInstances;

        private void InitOtherInstancesIntegration()
        {
            tittyMagic.NewJSONStorableAction("Integrate", RefreshOtherPluginInstances);
            RefreshOtherPluginInstances();

            /* When the plugin is added to an existing atom and this method gets called during initialization,
             * other instances are told to update their knowledge on what other instances are in the network.
             */
            foreach(var instance in _otherInstances)
            {
                if(instance.IsAction("Integrate"))
                {
                    instance.CallAction("Integrate");
                }
            }

            /* When the plugin is added as part of a new atom using e.g. scene or subscene merge load,
             * AddInstance adds the instance to this plugin's list of other instances.
             */
            SuperController.singleton.onAtomAddedHandlers += OnAtomAdded;
            SuperController.singleton.onAtomRemovedHandlers += OnAtomRemoved;
        }

        private void RefreshOtherPluginInstances()
        {
            // Debug.Log($"{containingAtom.uid}: Refreshing other instances...");
            _otherInstances = PruneOtherInstances() ?? new List<JSONStorable>();
            SuperController.singleton.GetAtoms().ForEach(AddInstance);
        }

        private void AddInstance(Atom atom)
        {
            var storable = Utils.FindOtherInstanceStorable(atom);
            if(storable != null && !_otherInstances.Exists(instance => instance.containingAtom.uid == atom.uid))
            {
                // Debug.Log($"{containingAtom.uid}: Adding instance for {atom.uid}");
                _otherInstances.Add(storable);
            }
        }

        private void OnAtomAdded(Atom atom)
        {
            PruneOtherInstances();
            AddInstance(atom);
        }

        private void OnAtomRemoved(Atom atom)
        {
            PruneOtherInstances().RemoveAll(instance => instance.containingAtom.uid == atom.uid);
        }

        public List<JSONStorable> PruneOtherInstances()
        {
            _otherInstances?.RemoveAll(instance =>
                instance == null ||
                instance.containingAtom == null
            );
            return _otherInstances;
        }

        #endregion Integration

        #endregion Init

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

        #region Update

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

        private static void UpdateDynamicHandlers(float roll, float pitch)
        {
            ForcePhysicsHandler.Update();
            GravityPhysicsHandler.Update(roll, pitch);
            ForceMorphHandler.Update(roll, pitch);
            GravityOffsetMorphHandler.Update(roll, pitch);
        }

        private void FixedUpdate()
        {
            try
            {
                if(!isInitialized || calibration.isWaiting || containingAtom.grabFreezePhysics && containingAtom.mainController.isGrabbing)
                {
                    return;
                }

                if(_listenersCheckRunner.Run(BreastMorphListener.ChangeWasDetected) && autoUpdateJsb.val && !calibration.isWaiting)
                {
                    StartCalibration(calibratesMass: true, waitsForListeners: true);
                    return;
                }

                _trackLeftNipple.UpdateAnglesAndDepthDiff();
                _trackRightNipple.UpdateAnglesAndDepthDiff();

                var rotation = _chestTransform.rotation;
                float roll = Calc.Roll(rotation);
                float pitch = Calc.Pitch(rotation);

                UpdateDynamicHandlers(roll, pitch);
            }
            catch(Exception e)
            {
                Utils.LogError($"FixedUpdate: {e}");
                enabled = false;
            }
        }

        #endregion Update

        #region Refresh

        public void StartCalibration(bool calibratesMass, bool waitsForListeners = false)
        {
            if(_isLoadingFromJson)
            {
                return;
            }

            StartCoroutine(CalibrationCo(calibratesMass, waitsForListeners));
        }

        private IEnumerator CalibrationCo(bool calibratesMass, bool waitsForListeners)
        {
            yield return calibration.Begin();
            if(calibration.isCancelling)
            {
                calibration.isCancelling = false;
                yield break;
            }

            yield return calibration.DeferFreezeAnimation();

            /* Dynamic adjustments to zero (simulate upright pose), update physics */
            {
                _trackLeftNipple.ResetAnglesAndDepthDiff();
                _trackRightNipple.ResetAnglesAndDepthDiff();
                UpdateDynamicHandlers(0, 0);

                MainPhysicsHandler.UpdatePhysics();
                SoftPhysicsHandler.UpdatePhysics();
                NippleErectionHandler.Update();
            }

            if(waitsForListeners)
            {
                yield return calibration.WaitForListeners();
            }

            SoftPhysicsHandler.SyncSoftPhysics();

            /* Calculate softness and quickness (in case sliders were adjusted) */
            softnessAmount = Curves.SoftnessBaseCurve(softnessJsf.val / 100f);
            quicknessAmount = 2 * quicknessJsf.val / 100 - 1;

            /* Calculate mass when gravity is off to get a consistent result.
             * Mass is calculated multiple times because each new mass value
             * changes the exact breast shape and therefore the estimated volume.
             */
            SetBreastsUseGravity(false);
            Action updateMass = () =>
            {
                if(calibratesMass)
                {
                    MainPhysicsHandler.UpdateMassValueAndAmounts();
                    MainPhysicsHandler.UpdatePhysics();
                }
            };
            yield return new WaitForSeconds(0.3f);
            yield return calibration.WaitAndRepeat(updateMass, 5, 0.1f);

            /* Update physics */
            MainPhysicsHandler.UpdatePhysics();
            SoftPhysicsHandler.UpdatePhysics();
            NippleErectionHandler.Update();

            /* Set extra multipliers */
            {
                float mass = MainPhysicsHandler.realMassAmount;

                ForceMorphHandler.upDownExtraMultiplier =
                    Curves.Exponential1(softnessAmount, 1.73f, 1.68f, 0.88f, m: 0.93f, s: 0.56f)
                    * Curves.MorphingCurve(mass);
                ForceMorphHandler.forwardExtraMultiplier =
                    Mathf.Lerp(1.00f, 1.20f, softnessAmount)
                    * Curves.DepthMorphingCurve(mass);
                ForceMorphHandler.backExtraMultiplier =
                    Mathf.Lerp(0.80f, 1.00f, softnessAmount)
                    * Curves.DepthMorphingCurve(mass);
                ForceMorphHandler.leftRightExtraMultiplier =
                    Curves.Exponential1(softnessAmount, 1.73f, 1.68f, 0.88f, m: 0.93f, s: 0.56f)
                    * Curves.MorphingCurve(mass);

                GravityOffsetMorphHandler.upDownExtraMultiplier = 1.16f - mass;
            }

            /* Calibrate nipples tracking and colliders */
            {
                _isSimulatingUprightPose = true;
                StartCoroutine(SimulateUprightPose());
                Action calibrateNipples = () =>
                {
                    _trackLeftNipple.Calibrate();
                    _trackRightNipple.Calibrate();
                };
                yield return calibration.WaitAndRepeat(calibrateNipples, 24, 0.05f);
                yield return hardColliderHandler.SyncAll();
                _isSimulatingUprightPose = false;
            }

            SetBreastsUseGravity(true);

            calibration.Finish();
            isInitialized = true;
        }

        private void SetBreastsUseGravity(bool value)
        {
            _pectoralRbLeft.useGravity = value;
            _pectoralRbRight.useGravity = value;
        }

        private bool _isSimulatingUprightPose;

        private IEnumerator SimulateUprightPose()
        {
            while(_isSimulatingUprightPose)
            {
                // simulate upright pose
                UpdateDynamicHandlers(roll: 0, pitch: 0);

                // scale force to be correct for the given fps vs physics rate, for some reason this produces an accurate calibration result
                float rateToPhysicsRateRatio = Time.deltaTime / Time.fixedDeltaTime;
                // simulate force of gravity when upright
                var force = _chestTransform.up * (rateToPhysicsRateRatio * -Physics.gravity.magnitude);
                _pectoralRbLeft.AddForce(force, ForceMode.Acceleration);
                _pectoralRbRight.AddForce(force, ForceMode.Acceleration);

                yield return null;
            }
        }

        /* Disable pectoral collisions while plugin is active, they cause breasts to "jump" when touched */
        private void SetPectoralCollisions(bool value)
        {
            _pectoralRbLeft.detectCollisions = value;
            _pectoralRbRight.detectCollisions = value;
        }

        #endregion Refresh

        public string PluginPath() =>
            $@"{this.GetPackagePath()}Custom\Scripts\everlaster\TittyMagic";

        public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
        {
            var jsonClass = base.GetJSON(includePhysical, includeAppearance, forceStore);
            jsonClass.Remove(CalibrationHelper.CALIBRATION_LOCK);
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
            _isLoadingFromJson = true;
            /* Prevent overriding versionJss.val from JSON. Version stored in JSON just for information,
             * but could be intercepted here and used to save a "loadedFromVersion" value.
             */
            if(jsonClass.HasKey("version"))
            {
                jsonClass["version"] = $"{VERSION}";
            }

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
            while(!isInitialized)
            {
                yield return null;
            }

            base.RestoreFromJSON(jsonClass, restorePhysical, restoreAppearance, presetAtoms, setMissingToDefault);
            _isLoadingFromJson = false;
            hardColliderHandler.SaveOriginalUseColliders();
            SoftPhysicsHandler.SaveOriginalBoolParamValues();
            calculateBreastMass.actionCallback();
        }

        private void OnDestroy()
        {
            try
            {
                Destroy(calibration);
                Destroy(settingsMonitor);
                Destroy(colliderVisualizer);
                Destroy(hardColliderHandler);
                mainWindow.GetSliders().ForEach(slider => Destroy(slider.GetPointerUpDownListener()));
                morphingWindow.GetSliders().ForEach(slider => Destroy(slider.GetPointerUpDownListener()));
                gravityWindow.GetSliders().ForEach(slider => Destroy(slider.GetPointerUpDownListener()));
                DestroyImmediate(_uiEventsListener);
                _customUITransforms?.ForEach(t => Destroy(t.gameObject));
                SuperController.singleton.BroadcastMessage("OnActionsProviderDestroyed", this, SendMessageOptions.DontRequireReceiver);
                SuperController.singleton.onAtomAddedHandlers -= AddInstance;
                SuperController.singleton.onAtomRemovedHandlers -= OnAtomRemoved;
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
                if(!isInitialized)
                {
                    return;
                }

                SetPectoralCollisions(false);
                settingsMonitor.SetEnabled(true);
                hardColliderHandler.SetEnabled(true);
                SoftPhysicsHandler.SaveOriginalBoolParamValues();
                StartCalibration(true);
                _inactivatedUITransforms?.ForEach(t => t.gameObject.SetActive(false));
                _customUITransforms?.ForEach(t => t.gameObject.SetActive(true));
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
                SetPectoralCollisions(true);
                settingsMonitor.SetEnabled(false);
                hardColliderHandler.SetEnabled(false);
                MainPhysicsHandler.RestoreOriginalPhysics();
                SoftPhysicsHandler.RestoreOriginalPhysics();
                GravityOffsetMorphHandler.ResetAll();
                ForceMorphHandler.ResetAll();
                NippleErectionHandler.Reset();
                _customUITransforms?.ForEach(t => t.gameObject.SetActive(false));
                _inactivatedUITransforms?.ForEach(t => t.gameObject.SetActive(true));
            }
            catch(Exception e)
            {
                Utils.LogError($"OnDisable: {e}");
            }
        }
    }
}
