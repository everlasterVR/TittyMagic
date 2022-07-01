using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;
using TittyMagic.UI;
using static TittyMagic.Utils;
using static TittyMagic.Calc;

namespace TittyMagic
{
    internal class Script : MVRScript
    {
        public const string VERSION = "v0.0.0";

        private Bindings _customBindings;

        private List<Rigidbody> _rigidbodies;
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
        public ForcePhysicsHandler forcePhysicsHandler { get; private set; }
        public ForceMorphHandler forceMorphHandler { get; private set; }

        private JSONStorableString _pluginVersionStorable;

        private Tabs _tabs;
        public MainWindow mainWindow { get; private set; }
        public MorphingWindow morphingWindow { get; private set; }
        public GravityWindow gravityWindow { get; private set; }
        public PhysicsWindow physicsWindow { get; private set; }

        public JSONStorableBool autoUpdateJsb;
        public JSONStorableFloat softnessJsf;
        public JSONStorableFloat quicknessJsf;

        public bool initDone { get; private set; }

        private bool _loadingFromJson;
        private float _timeSinceListenersChecked;
        private const float LISTENERS_CHECK_INTERVAL = 0.0333f;
        private int _waitStatus = -1;
        private int _refreshStatus = -1;
        private bool _animationWasSetFrozen;
        private bool _uiOpenPrevFrame;

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

            morphsPath = this.GetMorphsPath();

            var geometry = (DAZCharacterSelector) containingAtom.GetStorableByID("geometry");
            Gender.isFemale = geometry.gender == DAZCharacterSelector.Gender.Female;

            morphsControlUI = Gender.isFemale ? geometry.morphsControlUI : geometry.morphsControlUIOtherGender;
            _rigidbodies = containingAtom.GetComponentsInChildren<Rigidbody>().ToList();
            _chestRb = _rigidbodies.Find(rb => rb.name == "chest");
            _chestTransform = _chestRb.transform;

            AdjustJoints breastControl = (AdjustJoints) containingAtom.GetStorableByID(Gender.isFemale ? "BreastControl" : "PectoralControl");
            _pectoralRbLeft = breastControl.joint2.GetComponent<Rigidbody>();
            _pectoralRbRight = breastControl.joint1.GetComponent<Rigidbody>();

            _atomScaleListener = new AtomScaleListener(containingAtom.GetStorableByID("rescaleObject").GetFloatJSONParam("scale"));
            _skin = containingAtom.GetComponentInChildren<DAZCharacter>().skin;
            _breastVolumeCalculator = new BreastVolumeCalculator(_skin, _chestRb);

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
                var nippleRbLeft = _rigidbodies.Find(rb => rb.name == "lNipple");
                var nippleRbRight = _rigidbodies.Find(rb => rb.name == "rNipple");
                _trackLeftNipple.getNipplePosition = () => nippleRbLeft.position;
                _trackRightNipple.getNipplePosition = () => nippleRbRight.position;
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
                _breastMorphListener = new BreastMorphListener(geometry.morphBank1.morphs);
            else
                _breastMorphListener = new BreastMorphListener(geometry.morphBank1OtherGender.morphs, geometry.morphBank1.morphs);

            forcePhysicsHandler = new ForcePhysicsHandler(mainPhysicsHandler, softPhysicsHandler, _trackLeftNipple, _trackRightNipple);
            forceMorphHandler = new ForceMorphHandler(this, _trackLeftNipple, _trackRightNipple);

            LoadSettings();

            mainWindow = new MainWindow(this);
            morphingWindow = new MorphingWindow(this);
            gravityWindow = new GravityWindow(this);
            physicsWindow = new PhysicsWindow(this);

            SetupStorables();
            CreateNavigation();
            NavigateToMainWindow();
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
            autoUpdateJsb = this.NewJSONStorableBool("autoUpdateMass", true);
            softnessJsf = this.NewJSONStorableFloat("breastSoftness", 70f, 0f, 100f);
            quicknessJsf = this.NewJSONStorableFloat("breastQuickness", 70f, 0f, 100f);

        }

        private void CreateNavigation()
        {
            _tabs = new Tabs(this);
            _tabs.CreateUINavigationButtons();
            _tabs.tab1Button.AddListener(NavigateToMainWindow);
            _tabs.tab2Button.AddListener(NavigateToPhysicsWindow);
            _tabs.tab3Button.AddListener(NavigateToMorphingWindow);
            _tabs.tab4Button.AddListener(NavigateToGravityWindow);
        }

        private void NavigateToMainWindow()
        {
            if(_tabs.activeWindow?.Id() == 1)
            {
                return;
            }

            ResetActiveTabListener();
            _tabs.activeWindow?.Clear();
            _tabs.activeWindow = mainWindow;
            _tabs.activeWindow.Rebuild();
            _tabs.ActivateTab1();

            mainWindow.elements[autoUpdateJsb.name].AddListener(val =>
            {
                UpdateSlider(mainWindow.elements[mainPhysicsHandler.massJsf.name], !val);
            });
            UpdateSlider(mainWindow.elements[mainPhysicsHandler.massJsf.name], !autoUpdateJsb.val);

            mainWindow.elements[autoUpdateJsb.name].AddListener(val =>
            {
                if(val)
                {
                    StartCoroutine(DeferBeginRefresh(refreshMass: true, fromToggleOrButton: true));
                }
            });

            mainWindow.GetCalculateMassButton().AddListener(() =>
            {
                StartCoroutine(DeferBeginRefresh(refreshMass: true, fromToggleOrButton: true, useNewMass: true));
            });

            mainWindow.GetRecalibrateButton().AddListener(() =>
            {
                StartCoroutine(DeferBeginRefresh(refreshMass: true, fromToggleOrButton: true));
            });

            mainWindow.elements[mainPhysicsHandler.massJsf.name].AddListener((float val) =>
            {
                RefreshFromSliderChanged(refreshMass: true);
            });

            mainWindow.elements[softnessJsf.name].AddListener(val =>
            {
                if(Math.Abs(CalculateSoftnessAmount(val) - _softnessAmount) < 0.001f)
                {
                    return;
                }

                RefreshFromSliderChanged();
            });

            mainWindow.elements[quicknessJsf.name].AddListener(val =>
            {
                if(Math.Abs(CalculateQuicknessAmount(val) - _quicknessAmount) < 0.001f)
                {
                    return;
                }

                RefreshFromSliderChanged();
            });

            mainWindow.elements[hardColliderHandler.enabledJsb.name].AddListener(val =>
            {
                UpdateSlider(mainWindow.elements[hardColliderHandler.scaleJsf.name], val);
                // UpdateSlider(mainWindow.elements[_hardColliderHandler.radiusMultiplier.name], val);
                // UpdateSlider(mainWindow.elements[_hardColliderHandler.heightMultiplier.name], val);
                UpdateSlider(mainWindow.elements[hardColliderHandler.forceJsf.name], val);
            });
            UpdateSlider(mainWindow.elements[hardColliderHandler.scaleJsf.name], hardColliderHandler.enabledJsb.val);
            UpdateSlider(mainWindow.elements[hardColliderHandler.forceJsf.name], hardColliderHandler.enabledJsb.val);
        }

        private void NavigateToPhysicsWindow()
        {
            if(_tabs.activeWindow?.Id() == 2)
            {
                return;
            }

            ResetActiveTabListener();
            _tabs.activeWindow?.Clear();
            _tabs.activeWindow = physicsWindow;
            _tabs.activeWindow.Rebuild();
            _tabs.ActivateTab2();
        }

        private void NavigateToMorphingWindow()
        {
            if(_tabs.activeWindow?.Id() == 3)
            {
                return;
            }

            ResetActiveTabListener();
            _tabs.activeWindow?.Clear();
            _tabs.activeWindow = morphingWindow;
            _tabs.activeWindow.Rebuild();
            _tabs.ActivateTab3();

            morphingWindow.elements[forceMorphHandler.xMultiplierJsf.name].AddListener(val =>
            {
                forceMorphHandler.xMultiplier.mainMultiplier = Curves.QuadraticRegression(val);
            });

            morphingWindow.elements[forceMorphHandler.yMultiplierJsf.name].AddListener(val =>
            {
                forceMorphHandler.yMultiplier.mainMultiplier = Curves.QuadraticRegression(val);
            });

            morphingWindow.elements[forceMorphHandler.zMultiplierJsf.name].AddListener(val =>
            {
                forceMorphHandler.zMultiplier.mainMultiplier = Curves.QuadraticRegressionLesser(val);
            });

            morphingWindow.elements[nippleMorphHandler.nippleErectionJsf.name].AddListener(val =>
            {
                nippleMorphHandler.Update(val);
                if(_settingsMonitor.softPhysicsEnabled)
                {
                    softPhysicsHandler.UpdateNipplePhysics(
                        mainPhysicsHandler.massAmount,
                        _softnessAmount,
                        val
                    );
                }
            });
        }

        private void NavigateToGravityWindow()
        {
            if(_tabs.activeWindow?.Id() == 4)
            {
                return;
            }

            ResetActiveTabListener();
            _tabs.activeWindow?.Clear();
            _tabs.activeWindow = gravityWindow;
            _tabs.activeWindow.Rebuild();
            _tabs.ActivateTab4();

            gravityWindow.elements[gravityPhysicsHandler.xMultiplierJsf.name].AddListener(val =>
            {
                gravityPhysicsHandler.xMultiplier.mainMultiplier = val;
                RefreshFromSliderChanged();
            });

            gravityWindow.elements[gravityPhysicsHandler.yMultiplierJsf.name].AddListener(val =>
            {
                gravityPhysicsHandler.yMultiplier.mainMultiplier = val;
                offsetMorphHandler.yMultiplier.mainMultiplier = val;
                RefreshFromSliderChanged();
            });

            gravityWindow.elements[gravityPhysicsHandler.zMultiplierJsf.name].AddListener(val =>
            {
                gravityPhysicsHandler.zMultiplier.mainMultiplier = val;
                RefreshFromSliderChanged();
            });

            gravityWindow.elements[offsetMorphHandler.offsetMorphingJsf.name].AddListener((float val) =>
            {
                RefreshFromSliderChanged();
            });
        }

        private void ResetActiveTabListener()
        {
            if(_tabs.activeWindow?.Id() == 1)
            {
                _tabs.tab1Button.RemoveAllListeners();
                _tabs.tab1Button.AddListener(NavigateToMainWindow);
            }
            else if(_tabs.activeWindow?.Id() == 2)
            {
                _tabs.tab2Button.RemoveAllListeners();
                _tabs.tab2Button.AddListener(NavigateToPhysicsWindow);
            }
            else if(_tabs.activeWindow?.Id() == 3)
            {
                _tabs.tab3Button.RemoveAllListeners();
                _tabs.tab3Button.AddListener(NavigateToMorphingWindow);
            }
            else if(_tabs.activeWindow?.Id() == 4)
            {
                _tabs.tab4Button.RemoveAllListeners();
                _tabs.tab4Button.AddListener(NavigateToGravityWindow);
            }
        }

        public void EnableCurrentTabRenavigation()
        {
            if(_tabs.activeWindow?.Id() == 1)
            {
                _tabs.tab1Button.SetInteractable();
                _tabs.tab1Button.RemoveListener(NavigateToMainWindow);
                _tabs.tab1Button.AddListener(() =>
                {
                    _tabs.activeWindow.Clear();
                    _tabs.activeWindow.Rebuild();
                    _tabs.ActivateTab1();
                });
            }
            else if(_tabs.activeWindow?.Id() == 2)
            {
                _tabs.tab2Button.SetInteractable();
                _tabs.tab2Button.RemoveListener(NavigateToPhysicsWindow);
                _tabs.tab2Button.AddListener(() =>
                {
                    _tabs.activeWindow.Clear();
                    _tabs.activeWindow.Rebuild();
                    _tabs.ActivateTab2();
                });
            }
            else if(_tabs.activeWindow?.Id() == 3)
            {
            }
            else if(_tabs.activeWindow?.Id() == 4)
            {
            }
        }

        private static void UpdateSlider(UIDynamic element, bool interactable)
        {
            var uiDynamicSlider = element as UIDynamicSlider;
            if(uiDynamicSlider == null)
            {
                throw new ArgumentException($"UIDynamic {element.name} was null or not an UIDynamicSlider");
            }

            uiDynamicSlider.slider.interactable = interactable;
            uiDynamicSlider.labelText.color = uiDynamicSlider.slider.interactable
                ? Color.black
                : UIHelpers.darkerGray;
        }

        private static float CalculateSoftnessAmount(float val)
        {
            return Mathf.Pow(val / 100f, 0.67f);
        }

        private static float CalculateQuicknessAmount(float val)
        {
            return 2 * val / 100 - 1;
        }

        private void RefreshFromSliderChanged(bool refreshMass = false)
        {
            if(!_loadingFromJson && _waitStatus != WaitStatus.WAITING)
            {
                StartCoroutine(DeferBeginRefresh(refreshMass));
            }
        }

        private void Update()
        {
            try
            {
                CheckOutsideParametersChanged();
            }
            catch(Exception e)
            {
                LogError($"Update: {e}");
                enabled = false;
            }
        }

        private void CheckOutsideParametersChanged()
        {
            bool uiOpen = UITransform.gameObject.activeInHierarchy;
            if(uiOpen && !_uiOpenPrevFrame)
            {
                softPhysicsHandler.ReverseSyncSoftPhysicsOn();
                softPhysicsHandler.ReverseSyncSyncAllowSelfCollision();
            }
            _uiOpenPrevFrame = uiOpen;
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

                if(!initDone || _waitStatus != WaitStatus.DONE)
                {
                    return;
                }

                _timeSinceListenersChecked += Time.deltaTime;
                if(_timeSinceListenersChecked >= LISTENERS_CHECK_INTERVAL)
                {
                    _timeSinceListenersChecked -= LISTENERS_CHECK_INTERVAL;
                    if(CheckListeners())
                    {
                        StartCoroutine(DeferBeginRefresh(refreshMass: true));
                        return;
                    }
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
            forcePhysicsHandler.Update(
                roll,
                pitch,
                mainPhysicsHandler.realMassAmount
            );

            gravityPhysicsHandler.Update(
                roll,
                pitch,
                mainPhysicsHandler.massAmount,
                _softnessAmount
            );
        }

        private void UpdateDynamicMorphs(float roll, float pitch)
        {
            forceMorphHandler.Update(
                roll,
                pitch,
                mainPhysicsHandler.realMassAmount
            );

            offsetMorphHandler.Update(
                roll,
                pitch,
                mainPhysicsHandler.realMassAmount,
                _softnessAmount
            );
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

        public void StartRefreshCoroutine()
        {
            StartRefreshCoroutine(true, false);
        }

        public void StartRefreshCoroutine(bool refreshMass, bool fromToggleOrButton)
        {
            StartCoroutine(DeferBeginRefresh(refreshMass, fromToggleOrButton));
        }

        private IEnumerator DeferBeginRefresh(bool refreshMass, bool fromToggleOrButton = false, bool? useNewMass = null)
        {
            if(useNewMass == null)
            {
                useNewMass = autoUpdateJsb.val;
            }

            _waitStatus = WaitStatus.WAITING;

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
                    mainWindow.GetSlidersForRefresh().Any(slider => slider.IsClickDown()) ||
                    morphingWindow.GetSliders().Any(slider => slider.IsClickDown()) ||
                    gravityWindow.GetSliders().Any(slider => slider.IsClickDown())
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

            _softnessAmount = CalculateSoftnessAmount(softnessJsf.val);
            _quicknessAmount = CalculateQuicknessAmount(quicknessJsf.val);

            yield return Refresh(refreshMass, useNewMass.Value);
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
            UpdateDynamicMorphs(roll: 0, pitch: 0);

            _refreshStatus = RefreshStatus.PRE_REFRESH_OK;
        }

        private IEnumerator Refresh(bool refreshMass, bool useNewMass)
        {
            _refreshStatus = RefreshStatus.MASS_STARTED;

            if(_settingsMonitor != null) _settingsMonitor.enabled = false;

            // simulate breasts zero G
            _pectoralRbLeft.useGravity = false;
            _pectoralRbRight.useGravity = false;

            yield return new WaitForSeconds(0.33f);
            UpdateDynamicMorphs(roll: 0, pitch: 0);

            _softnessAmount = CalculateSoftnessAmount(softnessJsf.val);
            _quicknessAmount = CalculateQuicknessAmount(quicknessJsf.val);

            if(refreshMass)
            {
                yield return RefreshMass(useNewMass);
            }

            UpdateStaticPhysics();

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
            forceMorphHandler.yMultiplier.extraMultiplier = softnessMultiplier * (3.15f - 1.40f * mass);
            forceMorphHandler.xMultiplier.extraMultiplier = softnessMultiplier * (3.55f - 1.40f * mass);
            forceMorphHandler.zMultiplier.extraMultiplier = softnessMultiplier * (3.8f - 1.5f * mass);
            forceMorphHandler.zMultiplier.oppositeExtraMultiplier = softnessMultiplier * (3.8f - 1.5f * mass);
            offsetMorphHandler.yMultiplier.extraMultiplier = 1.16f - mass;
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
                // simulate gravityPhysics when upright
                gravityPhysicsHandler.Update(0, 0, mainPhysicsHandler.massAmount, _softnessAmount);
                forcePhysicsHandler.Update(0, 0, mainPhysicsHandler.massAmount);
                offsetMorphHandler.Update(0, 0, mainPhysicsHandler.massAmount, _softnessAmount);

                // simulate force of gravity when upright
                // 0.75f is a hack, for some reason a normal gravity force pushes breasts too much down,
                // causing the neutral position to be off by a little
                var force = _chestTransform.up * (0.75f * -Physics.gravity.magnitude);
                _pectoralRbLeft.AddForce(force, ForceMode.Acceleration);
                _pectoralRbRight.AddForce(force, ForceMode.Acceleration);
                if(_refreshStatus == RefreshStatus.MASS_OK)
                {
                    StartCoroutine(CalibrateNipplesTracking());
                    hardColliderHandler.ReSyncScaleOffsetCombined();
                }
                else if(_refreshStatus == RefreshStatus.NEUTRALPOS_OK)
                {
                    _pectoralRbLeft.useGravity = true;
                    _pectoralRbRight.useGravity = true;
                    SuperController.singleton.SetFreezeAnimation(_animationWasSetFrozen);
                    if(_settingsMonitor != null) _settingsMonitor.enabled = true;
                    _waitStatus = WaitStatus.DONE;
                    _refreshStatus = RefreshStatus.DONE;
                    initDone = true;
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
            return autoUpdateJsb.val &&
                _waitStatus != WaitStatus.WAITING &&
                (_breastMorphListener.Changed() || _atomScaleListener.Changed());
        }

        public RectTransform GetLeftUIContent()
        {
            return leftUIContent;
        }

        public RectTransform GetRightUIContent()
        {
            return rightUIContent;
        }

        public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
        {
            JSONClass jsonClass = base.GetJSON(includePhysical, includeAppearance, forceStore);
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
            StartCoroutine(DeferRestoreFromJSON(jsonClass, restorePhysical, restoreAppearance, presetAtoms, setMissingToDefault));
        }

        private IEnumerator DeferRestoreFromJSON(JSONClass jsonClass, bool restorePhysical, bool restoreAppearance, JSONArray presetAtoms, bool setMissingToDefault)
        {
            while(!initDone)
            {
                yield return null;
            }

            base.RestoreFromJSON(jsonClass, restorePhysical, restoreAppearance, presetAtoms, setMissingToDefault);
            StartCoroutine(DeferBeginRefresh(refreshMass: true));

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
            mainWindow.GetSliders().ForEach(slider => Destroy(slider.GetSliderClickMonitor()));
            morphingWindow.GetSliders().ForEach(slider => Destroy(slider.GetSliderClickMonitor()));
            gravityWindow.GetSliders().ForEach(slider => Destroy(slider.GetSliderClickMonitor()));
        }

        private void OnDestroy()
        {
            try
            {
                Destroy(_settingsMonitor);
                Destroy(hardColliderHandler);
                mainWindow.GetSliders().ForEach(slider => Destroy(slider.GetSliderClickMonitor()));
                morphingWindow.GetSliders().ForEach(slider => Destroy(slider.GetSliderClickMonitor()));
                gravityWindow.GetSliders().ForEach(slider => Destroy(slider.GetSliderClickMonitor()));
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
            try
            {
                if(_settingsMonitor != null) _settingsMonitor.enabled = true;
                if(hardColliderHandler != null) hardColliderHandler.enabled = true;

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
                if(_settingsMonitor != null) _settingsMonitor.enabled = false;
                if(hardColliderHandler != null) hardColliderHandler.enabled = false;

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
