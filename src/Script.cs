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
        private float _chestRoll;
        private float _chestPitch;

        private SettingsMonitor _settingsMonitor;

        private AtomScaleListener _atomScaleListener;
        private BreastMorphListener _breastMorphListener;
        private BreastVolumeCalculator _breastVolumeCalculator;

        private MainPhysicsHandler _mainPhysicsHandler;
        private HardColliderHandler _hardColliderHandler;
        private SoftPhysicsHandler _softPhysicsHandler;
        private GravityPhysicsHandler _gravityPhysicsHandler;
        private GravityOffsetMorphHandler _gravityOffsetMorphHandler;
        private NippleErectionMorphHandler _nippleErectionMorphHandler;
        private ForcePhysicsHandler _forcePhysicsHandler;
        private ForceMorphHandler _forceMorphHandler;

        private JSONStorableString _pluginVersionStorable;

        private Tabs _tabs;
        public MainWindow mainWindow { get; private set; }
        public MorphingWindow morphingWindow { get; private set; }
        public GravityWindow gravityWindow { get; private set; }
        public PhysicsWindow physicsWindow { get; private set; }

        public JSONStorableBool autoUpdateMass;
        public JSONStorableFloat mass;
        public JSONStorableFloat softness;
        public JSONStorableFloat quickness;
        public JSONStorableFloat morphingYStorable;
        public JSONStorableFloat morphingXStorable;
        public JSONStorableFloat morphingZStorable;
        public JSONStorableFloat gravityYStorable;
        public JSONStorableFloat gravityXStorable;
        public JSONStorableFloat gravityZStorable;
        public JSONStorableFloat offsetMorphing;
        public JSONStorableFloat nippleErection;

        public bool initDone { get; private set; }

        private bool _isFemale;
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
            _isFemale = geometry.gender == DAZCharacterSelector.Gender.Female;
            AdjustJoints breastControl;
            if(_isFemale)
            {
                morphsControlUI = geometry.morphsControlUI;
                breastControl = (AdjustJoints) containingAtom.GetStorableByID("BreastControl");
            }
            else
            {
                morphsControlUI = geometry.morphsControlUIOtherGender;
                breastControl = (AdjustJoints) containingAtom.GetStorableByID("PectoralControl");
            }

            _rigidbodies = containingAtom.GetComponentsInChildren<Rigidbody>().ToList();
            _chestRb = _rigidbodies.Find(rb => rb.name == "chest");
            _chestTransform = _chestRb.transform;
            _pectoralRbLeft = breastControl.joint2.GetComponent<Rigidbody>();
            _pectoralRbRight = breastControl.joint1.GetComponent<Rigidbody>();

            _atomScaleListener = new AtomScaleListener(containingAtom.GetStorableByID("rescaleObject").GetFloatJSONParam("scale"));
            _skin = containingAtom.GetComponentInChildren<DAZCharacter>().skin;
            _breastVolumeCalculator = new BreastVolumeCalculator(_skin, _chestRb);

            _mainPhysicsHandler = new MainPhysicsHandler(breastControl, _pectoralRbLeft, _pectoralRbRight);
            _hardColliderHandler = gameObject.AddComponent<HardColliderHandler>();
            _hardColliderHandler.Init();

            if(_isFemale)
            {
                _softPhysicsHandler = new SoftPhysicsHandler(this);
            }

            _gravityPhysicsHandler = new GravityPhysicsHandler(_mainPhysicsHandler);
            _gravityOffsetMorphHandler = new GravityOffsetMorphHandler(this);
            _nippleErectionMorphHandler = new NippleErectionMorphHandler(this);

            _trackLeftNipple = new TrackNipple(_chestRb, _pectoralRbLeft);
            _trackRightNipple = new TrackNipple(_chestRb, _pectoralRbRight);

            if(_isFemale)
            {
                var nippleRbLeft = _rigidbodies.Find(rb => rb.name == "lNipple");
                var nippleRbRight = _rigidbodies.Find(rb => rb.name == "rNipple");
                _trackLeftNipple.getNipplePosition = () => nippleRbLeft.position;
                _trackRightNipple.getNipplePosition = () => nippleRbRight.position;

                _settingsMonitor = gameObject.AddComponent<SettingsMonitor>();
                _settingsMonitor.Init();
                _breastMorphListener = new BreastMorphListener(geometry.morphBank1.morphs);
            }
            else
            {
                _trackLeftNipple.getNipplePosition = () => AveragePosition(
                    VertexIndexGroup.LEFT_BREAST_CENTER.Select(i => _skin.rawSkinnedWorkingVerts[i]).ToList()
                );
                _trackRightNipple.getNipplePosition = () => AveragePosition(
                    VertexIndexGroup.RIGHT_BREAST_CENTER.Select(i => _skin.rawSkinnedWorkingVerts[i]).ToList()
                );

                _breastMorphListener = new BreastMorphListener(geometry.morphBank1OtherGender.morphs, geometry.morphBank1.morphs);
            }

            _forcePhysicsHandler = new ForcePhysicsHandler(_mainPhysicsHandler, _softPhysicsHandler, _trackLeftNipple, _trackRightNipple);
            _forceMorphHandler = new ForceMorphHandler(this, _trackLeftNipple, _trackRightNipple);

            LoadSettings();

            mainWindow = new MainWindow(this, _hardColliderHandler, _softPhysicsHandler);
            morphingWindow = new MorphingWindow(this);
            gravityWindow = new GravityWindow(this);
            physicsWindow = new PhysicsWindow(this, _mainPhysicsHandler, _softPhysicsHandler);

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
            _mainPhysicsHandler.LoadSettings(_isFemale && _settingsMonitor.softPhysicsEnabled);
            _softPhysicsHandler?.LoadSettings();
            _gravityPhysicsHandler.LoadSettings();
            _forcePhysicsHandler.LoadSettings();
            _forceMorphHandler.LoadSettings();
            _gravityOffsetMorphHandler.LoadSettings();
        }

        private void InitializeValuesAppliedByListeners()
        {
            _softnessAmount = CalculateSoftnessAmount(softness.val);
            _quicknessAmount = CalculateQuicknessAmount(quickness.val);
            _forceMorphHandler.xMultiplier.mainMultiplier = Curves.QuadraticRegression(morphingXStorable.val);
            _forceMorphHandler.yMultiplier.mainMultiplier = Curves.QuadraticRegression(morphingYStorable.val);
            _forceMorphHandler.zMultiplier.mainMultiplier = Curves.QuadraticRegressionLesser(morphingZStorable.val);
            _gravityPhysicsHandler.xMultiplier.mainMultiplier = gravityXStorable.val;
            _gravityPhysicsHandler.yMultiplier.mainMultiplier = gravityYStorable.val;
            _gravityPhysicsHandler.zMultiplier.mainMultiplier = gravityZStorable.val;
            _gravityOffsetMorphHandler.yMultiplier.mainMultiplier = gravityYStorable.val;
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
            autoUpdateMass = this.NewJSONStorableBool("autoUpdateMass", true);
            mass = this.NewJSONStorableFloat("breastMass", 0.1f, 0.1f, 3f);
            _mainPhysicsHandler.mass = mass;
            softness = this.NewJSONStorableFloat("breastSoftness", 70f, 0f, 100f);
            quickness = this.NewJSONStorableFloat("breastQuickness", 70f, 0f, 100f);

            morphingYStorable = this.NewJSONStorableFloat("morphingUpDown", 1.00f, 0.00f, 3.00f);
            morphingXStorable = this.NewJSONStorableFloat("morphingLeftRight", 1.00f, 0.00f, 3.00f);
            morphingZStorable = this.NewJSONStorableFloat("morphingForwardBack", 1.00f, 0.00f, 3.00f);

            gravityYStorable = this.NewJSONStorableFloat("gravityPhysicsUpDown", 1.00f, 0.00f, 2.00f);
            gravityXStorable = this.NewJSONStorableFloat("gravityPhysicsLeftRight", 1.00f, 0.00f, 2.00f);
            gravityZStorable = this.NewJSONStorableFloat("gravityPhysicsForwardBack", 1.00f, 0.00f, 2.00f);

            offsetMorphing = this.NewJSONStorableFloat("gravityOffsetMorphing", 1.00f, 0.00f, 2.00f);
            nippleErection = this.NewJSONStorableFloat("nippleErection", 0.00f, 0.00f, 1.00f);
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

            mainWindow.elements[autoUpdateMass.name].AddListener(val =>
            {
                UpdateSlider(mainWindow.elements[mass.name], !val);
            });
            UpdateSlider(mainWindow.elements[mass.name], !autoUpdateMass.val);

            mainWindow.elements[autoUpdateMass.name].AddListener(val =>
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

            mainWindow.elements[mass.name].AddListener((float val) =>
            {
                RefreshFromSliderChanged(refreshMass: true);
            });

            mainWindow.elements[softness.name].AddListener(val =>
            {
                if(Math.Abs(CalculateSoftnessAmount(val) - _softnessAmount) < 0.001f)
                {
                    return;
                }

                RefreshFromSliderChanged();
            });

            mainWindow.elements[quickness.name].AddListener(val =>
            {
                if(Math.Abs(CalculateQuicknessAmount(val) - _quicknessAmount) < 0.001f)
                {
                    return;
                }

                RefreshFromSliderChanged();
            });

            mainWindow.elements[_hardColliderHandler.useHardColliders.name].AddListener(val =>
            {
                UpdateSlider(mainWindow.elements[_hardColliderHandler.scaleOffset.name], val);
                // UpdateSlider(mainWindow.elements[_hardColliderHandler.radiusMultiplier.name], val);
                // UpdateSlider(mainWindow.elements[_hardColliderHandler.heightMultiplier.name], val);
                UpdateSlider(mainWindow.elements[_hardColliderHandler.forceMultiplier.name], val);
            });
            UpdateSlider(mainWindow.elements[_hardColliderHandler.scaleOffset.name], _hardColliderHandler.useHardColliders.val);
            UpdateSlider(mainWindow.elements[_hardColliderHandler.forceMultiplier.name], _hardColliderHandler.useHardColliders.val);
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

            morphingWindow.elements[morphingXStorable.name].AddListener(val =>
            {
                _forceMorphHandler.xMultiplier.mainMultiplier = Curves.QuadraticRegression(val);
            });

            morphingWindow.elements[morphingYStorable.name].AddListener(val =>
            {
                _forceMorphHandler.yMultiplier.mainMultiplier = Curves.QuadraticRegression(val);
            });

            morphingWindow.elements[morphingZStorable.name].AddListener(val =>
            {
                _forceMorphHandler.zMultiplier.mainMultiplier = Curves.QuadraticRegressionLesser(val);
            });

            morphingWindow.elements[nippleErection.name].AddListener(val =>
            {
                _nippleErectionMorphHandler.Update(val);
                if(_isFemale && _settingsMonitor.softPhysicsEnabled)
                {
                    _softPhysicsHandler.UpdateNipplePhysics(
                        _mainPhysicsHandler.massAmount,
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

            gravityWindow.elements[gravityXStorable.name].AddListener(val =>
            {
                _gravityPhysicsHandler.xMultiplier.mainMultiplier = val;
                RefreshFromSliderChanged();
            });

            gravityWindow.elements[gravityYStorable.name].AddListener(val =>
            {
                _gravityPhysicsHandler.yMultiplier.mainMultiplier = val;
                _gravityOffsetMorphHandler.yMultiplier.mainMultiplier = val;
                RefreshFromSliderChanged();
            });

            gravityWindow.elements[gravityZStorable.name].AddListener(val =>
            {
                _gravityPhysicsHandler.zMultiplier.mainMultiplier = val;
                RefreshFromSliderChanged();
            });

            gravityWindow.elements[offsetMorphing.name].AddListener((float val) =>
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
                _softPhysicsHandler.ReverseSyncSoftPhysicsOn();
                _softPhysicsHandler.ReverseSyncSyncAllowSelfCollision();
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

        private void RunHandlers(bool updateDynamicPhysics = true)
        {
            _forceMorphHandler.Update(
                _chestRoll,
                _chestPitch,
                _mainPhysicsHandler.realMassAmount
            );

            if(updateDynamicPhysics)
            {
                _forcePhysicsHandler.Update(
                    _chestRoll,
                    _chestPitch,
                    _mainPhysicsHandler.realMassAmount
                );

                _gravityPhysicsHandler.Update(
                    _chestRoll,
                    _chestPitch,
                    _mainPhysicsHandler.massAmount,
                    _softnessAmount
                );
            }

            _gravityOffsetMorphHandler.Update(
                _chestRoll,
                _chestPitch,
                _mainPhysicsHandler.realMassAmount,
                _softnessAmount,
                offsetMorphing.val
            );
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
                useNewMass = autoUpdateMass.val;
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

            _softnessAmount = CalculateSoftnessAmount(softness.val);
            _quicknessAmount = CalculateQuicknessAmount(quickness.val);

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

            _chestRoll = 0;
            _chestPitch = 0;

            _trackLeftNipple.ResetAnglesAndDepthDiff();
            _trackRightNipple.ResetAnglesAndDepthDiff();

            if(refreshMass)
            {
                _mainPhysicsHandler.UpdateMassValueAndAmounts(useNewMass, _breastVolumeCalculator.Calculate(_atomScaleListener.scale));
                SetMorphingExtraMultipliers();
            }

            _softnessAmount = CalculateSoftnessAmount(softness.val);
            _quicknessAmount = CalculateQuicknessAmount(quickness.val);

            _mainPhysicsHandler.UpdatePhysics(_softnessAmount, _quicknessAmount);
            if(_isFemale && _settingsMonitor.softPhysicsEnabled)
            {
                _softPhysicsHandler.UpdatePhysics(
                    _mainPhysicsHandler.massAmount,
                    _mainPhysicsHandler.realMassAmount,
                    _softnessAmount,
                    _quicknessAmount
                );
                _softPhysicsHandler.UpdateNipplePhysics(
                    _mainPhysicsHandler.massAmount,
                    _softnessAmount,
                    nippleErection.val
                );
            }

            RunHandlers(false);
            _refreshStatus = RefreshStatus.PRE_REFRESH_OK;
        }

        private IEnumerator Refresh(bool refreshMass, bool useNewMass)
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

            _softnessAmount = CalculateSoftnessAmount(softness.val);
            _quicknessAmount = CalculateQuicknessAmount(quickness.val);

            if(refreshMass)
            {
                yield return RefreshMass(useNewMass);
            }

            _mainPhysicsHandler.UpdatePhysics(_softnessAmount, _quicknessAmount);
            if(_isFemale && _settingsMonitor.softPhysicsEnabled)
            {
                _softPhysicsHandler.UpdatePhysics(
                    _mainPhysicsHandler.massAmount,
                    _mainPhysicsHandler.realMassAmount,
                    _softnessAmount,
                    _quicknessAmount
                );
                _softPhysicsHandler.UpdateNipplePhysics(
                    _mainPhysicsHandler.massAmount,
                    _softnessAmount,
                    nippleErection.val
                );
            }

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

                _mainPhysicsHandler.UpdateMassValueAndAmounts(useNewMass, _breastVolumeCalculator.Calculate(_atomScaleListener.scale));
                _mainPhysicsHandler.UpdatePhysics(_softnessAmount, _quicknessAmount);
            }

            if(autoUpdateMass.val)
            {
                mass.defaultVal = mass.val;
            }

            SetMorphingExtraMultipliers();
        }

        private void SetMorphingExtraMultipliers()
        {
            float softnessMultiplier = Mathf.Lerp(0.5f, 1.14f, _softnessAmount);
            float m = _mainPhysicsHandler.realMassAmount;
            _forceMorphHandler.yMultiplier.extraMultiplier = softnessMultiplier * (3.15f - 1.40f * m);
            _forceMorphHandler.xMultiplier.extraMultiplier = softnessMultiplier * (3.55f - 1.40f * m);
            _forceMorphHandler.zMultiplier.extraMultiplier = softnessMultiplier * (3.8f - 1.5f * m);
            _forceMorphHandler.zMultiplier.oppositeExtraMultiplier = softnessMultiplier * (3.8f - 1.5f * m);
            _gravityOffsetMorphHandler.yMultiplier.extraMultiplier = 1.16f - m;
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
                _gravityPhysicsHandler.Update(0, 0, _mainPhysicsHandler.massAmount, _softnessAmount);
                _forcePhysicsHandler.Update(0, 0, _mainPhysicsHandler.massAmount);
                _gravityOffsetMorphHandler.Update(0, 0, _mainPhysicsHandler.massAmount, _softnessAmount, offsetMorphing.val);

                // simulate force of gravity when upright
                // 0.75f is a hack, for some reason a normal gravity force pushes breasts too much down,
                // causing the neutral position to be off by a little
                var force = _chestTransform.up * (0.75f * -Physics.gravity.magnitude);
                _pectoralRbLeft.AddForce(force, ForceMode.Acceleration);
                _pectoralRbRight.AddForce(force, ForceMode.Acceleration);
                if(_refreshStatus == RefreshStatus.MASS_OK)
                {
                    StartCoroutine(CalibrateNipplesTracking());
                    _hardColliderHandler.ReSyncScaleOffsetCombined();
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
            return autoUpdateMass.val &&
                _waitStatus != WaitStatus.WAITING &&
                (_breastMorphListener.Changed() || _atomScaleListener.Changed());
        }

        public void UpdateRateDependentPhysics()
        {
            _mainPhysicsHandler.UpdateRateDependentPhysics(_softnessAmount, _quicknessAmount);
            if(_isFemale && _settingsMonitor.softPhysicsEnabled)
            {
                _softPhysicsHandler.UpdateRateDependentPhysics(
                    _mainPhysicsHandler.massAmount,
                    _mainPhysicsHandler.realMassAmount,
                    _softnessAmount,
                    _quicknessAmount
                );
            }
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
            jsonClass["originalMainPhysics"] = _mainPhysicsHandler.Serialize();
            jsonClass["originalHardColliders"] = _hardColliderHandler.Serialize();
            if(_isFemale)
            {
                jsonClass["originalSoftPhysics"] = _softPhysicsHandler.Serialize();
            }
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
                _mainPhysicsHandler.RestoreFromJSON(jsonClass["originalMainPhysics"].AsObject);
            }

            if(jsonClass.HasKey("originalHardColliders"))
            {
                _hardColliderHandler.RestoreFromJSON(jsonClass["originalHardColliders"].AsObject);
            }
            if(jsonClass.HasKey("originalSoftPhysics") && _isFemale)
            {
                _softPhysicsHandler.RestoreFromJSON(jsonClass["originalSoftPhysics"].AsObject);
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
                Destroy(_hardColliderHandler);
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
                if(_hardColliderHandler != null) _hardColliderHandler.enabled = true;

                if(initDone)
                {
                    _mainPhysicsHandler?.SaveOriginalPhysicsAndSetPluginDefaults();
                    _softPhysicsHandler?.SaveOriginalPhysicsAndSetPluginDefaults();
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
                if(_hardColliderHandler != null) _hardColliderHandler.enabled = false;

                _mainPhysicsHandler?.RestoreOriginalPhysics();
                _softPhysicsHandler?.RestoreOriginalPhysics();
                _gravityOffsetMorphHandler?.ResetAll();
                _forceMorphHandler?.ResetAll();
                _nippleErectionMorphHandler?.ResetAll();
            }
            catch(Exception e)
            {
                LogError($"OnDisable: {e}");
            }
        }
    }
}
