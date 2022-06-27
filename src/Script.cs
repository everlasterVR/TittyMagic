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
        public const string VERSION = "0.0.0";

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
        private SoftPhysicsHandler _softPhysicsHandler;
        private GravityPhysicsHandler _gravityPhysicsHandler;
        private GravityOffsetMorphHandler _gravityOffsetMorphHandler;
        private NippleErectionMorphHandler _nippleErectionMorphHandler;
        private ForcePhysicsHandler _forcePhysicsHandler;
        private ForceMorphHandler _forceMorphHandler;

        private JSONStorableString _pluginVersionStorable;

        private Tabs _tabs;
        private MainWindow _mainWindow;
        private MorphingWindow _morphingWindow;
        private GravityWindow _gravityWindow;
        private AdvancedWindow _advancedWindow;

        public JSONStorableBool autoRefresh;
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

        private bool _isFemale;
        private bool _initDone;
        private bool _loadingFromJson;
        private float _timeSinceListenersChecked;
        private const float LISTENERS_CHECK_INTERVAL = 0.0333f;
        private int _waitStatus = -1;
        private int _refreshStatus = -1;
        private bool _animationWasSetFrozen;
        private bool _removingAtom;

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

            _mainPhysicsHandler = new MainPhysicsHandler(
                _isFemale,
                breastControl,
                _pectoralRbLeft,
                _pectoralRbRight
            );
            if(_isFemale)
            {
                _softPhysicsHandler = new SoftPhysicsHandler(
                    (DAZPhysicsMesh) containingAtom.GetStorableByID("BreastPhysicsMesh"),
                    geometry
                );
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
                _settingsMonitor.Init(containingAtom);
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

            _mainWindow = new MainWindow(this);
            _morphingWindow = new MorphingWindow(this);
            _gravityWindow = new GravityWindow(this);
            _advancedWindow = new AdvancedWindow(this, _mainPhysicsHandler, _softPhysicsHandler);

            SetupStorables();
            CreateNavigation();
            NavigateToMainWindow();
            InitializeValuesAppliedByListeners();

            if(!_loadingFromJson)
            {
                StartCoroutine(WaitToBeginRefresh(refreshMass: true));
            }
            else
            {
                _initDone = true;
            }

            SuperController.singleton.onAtomRemovedHandlers += OnRemoveAtom;
            StartCoroutine(SetPluginVersion());
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

        private IEnumerator SetPluginVersion()
        {
            while(_loadingFromJson)
            {
                yield return null;
            }

            _pluginVersionStorable.val = $"{VERSION}";
        }

        private void SetupStorables()
        {
            autoRefresh = this.NewJsonStorableBool("Auto-update mass", true);
            mass = this.NewJsonStorableFloat("Breast mass", 0.1f, 0.1f, 3f);
            _mainPhysicsHandler.mass = mass;
            softness = this.NewJsonStorableFloat("Breast softness", 70f, 0f, 100f);
            quickness = this.NewJsonStorableFloat("Breast quickness", 70f, 0f, 100f);

            morphingYStorable = this.NewJsonStorableFloat("Morphing Up/down", 1.00f, 0.00f, 3.00f);
            morphingXStorable = this.NewJsonStorableFloat("Morphing Left/right", 1.00f, 0.00f, 3.00f);
            morphingZStorable = this.NewJsonStorableFloat("Morphing Forward/back", 1.00f, 0.00f, 3.00f);

            gravityYStorable = this.NewJsonStorableFloat("Physics Up/down", 1.00f, 0.00f, 2.00f);
            gravityXStorable = this.NewJsonStorableFloat("Physics Left/right", 1.00f, 0.00f, 2.00f);
            gravityZStorable = this.NewJsonStorableFloat("Physics Forward/back", 1.00f, 0.00f, 2.00f);

            offsetMorphing = this.NewJsonStorableFloat("Gravity offset morphing", 1.00f, 0.00f, 2.00f);
            nippleErection = this.NewJsonStorableFloat("Nipple erection", 0.00f, 0.00f, 1.00f);
        }

        private void CreateNavigation()
        {
            _tabs = new Tabs(this, leftUIContent, rightUIContent);
            _tabs.CreateUINavigationButtons();
            _tabs.mainSettingsButton.AddListener(NavigateToMainWindow);
            _tabs.morphingButton.AddListener(NavigateToMorphingWindow);
            _tabs.gravityButton.AddListener(NavigateToGravityWindow);
            _tabs.advancedButton.AddListener(NavigateToAdvancedWindow);
        }

        private void NavigateToMainWindow()
        {
            if(_tabs.activeWindow?.Id() == 1)
            {
                return;
            }

            _tabs.activeWindow?.Clear();
            _tabs.activeWindow = _mainWindow;
            _tabs.activeWindow.Rebuild();
            _tabs.ActivateMainSettingsTab();
            UpdateSlider(_mainWindow.elements[mass.name], autoRefresh.val);

            _mainWindow.elements[autoRefresh.name].AddListener(val =>
            {
                UpdateSlider(_mainWindow.elements[mass.name], val);
            });

            _mainWindow.elements[autoRefresh.name].AddListener(val =>
            {
                if(val)
                {
                    StartCoroutine(WaitToBeginRefresh(refreshMass: true, fromToggleOrButton: true));
                }
            });

            _mainWindow.GetCalculateMassButton().AddListener(() =>
            {
                StartCoroutine(WaitToBeginRefresh(refreshMass: true, fromToggleOrButton: true, useNewMass: true));
            });

            _mainWindow.GetRecalibrateButton().AddListener(() =>
            {
                StartCoroutine(WaitToBeginRefresh(refreshMass: true, fromToggleOrButton: true));
            });

            _mainWindow.elements[mass.name].AddListener((float val) =>
            {
                RefreshFromSliderChanged(refreshMass: true);
            });

            _mainWindow.elements[softness.name].AddListener(val =>
            {
                if(Math.Abs(CalculateSoftnessAmount(val) - _softnessAmount) < 0.001f)
                {
                    return;
                }

                RefreshFromSliderChanged();
            });

            _mainWindow.elements[quickness.name].AddListener(val =>
            {
                if(Math.Abs(CalculateQuicknessAmount(val) - _quicknessAmount) < 0.001f)
                {
                    return;
                }

                RefreshFromSliderChanged();
            });
        }

        private void NavigateToMorphingWindow()
        {
            if(_tabs.activeWindow.Id() == 2)
            {
                return;
            }

            _tabs.activeWindow.Clear();
            _tabs.activeWindow = _morphingWindow;
            _tabs.activeWindow.Rebuild();
            _tabs.ActivateMorphingTab();

            _morphingWindow.elements[morphingXStorable.name].AddListener(val =>
            {
                _forceMorphHandler.xMultiplier.mainMultiplier = Curves.QuadraticRegression(val);
            });

            _morphingWindow.elements[morphingYStorable.name].AddListener(val =>
            {
                _forceMorphHandler.yMultiplier.mainMultiplier = Curves.QuadraticRegression(val);
            });

            _morphingWindow.elements[morphingZStorable.name].AddListener(val =>
            {
                _forceMorphHandler.zMultiplier.mainMultiplier = Curves.QuadraticRegressionLesser(val);
            });

            _morphingWindow.elements[offsetMorphing.name].AddListener((float val) =>
            {
                RefreshFromSliderChanged();
            });

            _morphingWindow.elements[nippleErection.name].AddListener(val =>
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
            if(_tabs.activeWindow.Id() == 3)
            {
                return;
            }

            _tabs.activeWindow.Clear();
            _tabs.activeWindow = _gravityWindow;
            _tabs.activeWindow.Rebuild();
            _tabs.ActivateGravityPhysicsTab();

            _gravityWindow.elements[gravityXStorable.name].AddListener(val =>
            {
                _gravityPhysicsHandler.xMultiplier.mainMultiplier = val;
                RefreshFromSliderChanged();
            });

            _gravityWindow.elements[gravityYStorable.name].AddListener(val =>
            {
                _gravityPhysicsHandler.yMultiplier.mainMultiplier = val;
                _gravityOffsetMorphHandler.yMultiplier.mainMultiplier = val;
                RefreshFromSliderChanged();
            });

            _gravityWindow.elements[gravityZStorable.name].AddListener(val =>
            {
                _gravityPhysicsHandler.zMultiplier.mainMultiplier = val;
                RefreshFromSliderChanged();
            });
        }

        private void NavigateToAdvancedWindow()
        {
            if(_tabs.activeWindow.Id() == 4)
            {
                return;
            }

            _tabs.activeWindow.Clear();
            _tabs.activeWindow = _advancedWindow;
            _tabs.activeWindow.Rebuild();
            _tabs.ActivateAdvancedTab();
        }

        private static void UpdateSlider(UIDynamic element, bool autoRefreshOn)
        {
            var uiDynamicSlider = element as UIDynamicSlider;
            if(uiDynamicSlider == null)
            {
                throw new ArgumentException($"UIDynamic {element.name} was null or not an UIDynamicSlider");
            }

            uiDynamicSlider.slider.interactable = !autoRefreshOn;
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
                        StartCoroutine(WaitToBeginRefresh(refreshMass: true));
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
            StartCoroutine(WaitToBeginRefresh(refreshMass, fromToggleOrButton));
        }

        private IEnumerator WaitToBeginRefresh(bool refreshMass, bool fromToggleOrButton = false, bool? useNewMass = null)
        {
            if(useNewMass == null)
            {
                useNewMass = autoRefresh.val;
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
                    _mainWindow.GetSliders().Any(slider => slider.IsClickDown()) ||
                    _morphingWindow.GetSliders().Any(slider => slider.IsClickDown()) ||
                    _gravityWindow.GetSliders().Any(slider => slider.IsClickDown())
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

            if(autoRefresh.val)
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
            return autoRefresh.val &&
                _waitStatus != RefreshStatus.WAITING &&
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

        public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
        {
            JSONClass json = base.GetJSON(includePhysical, includeAppearance, forceStore);
            json["originalMainPhysics"] = _mainPhysicsHandler.Serialize();
            if(_isFemale)
            {
                json["originalSoftPhysics"] = _softPhysicsHandler.Serialize();
            }
            needsStore = true;
            return json;
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
            StartCoroutine(WaitToBeginRefresh(refreshMass: true));

            if(json.HasKey("originalMainPhysics"))
            {
                _mainPhysicsHandler.RestoreFromJSON(json["originalMainPhysics"].AsObject);
            }
            if(json.HasKey("originalSoftPhysics") && _isFemale)
            {
                _softPhysicsHandler.RestoreFromJSON(json["originalSoftPhysics"].AsObject);
            }

            _loadingFromJson = false;
        }

        private void OnRemoveAtom(Atom atom)
        {
            _removingAtom = true;
            Destroy(_settingsMonitor);
            _mainWindow.GetSliders().ForEach(slider => Destroy(slider.GetSliderClickMonitor()));
            _morphingWindow.GetSliders().ForEach(slider => Destroy(slider.GetSliderClickMonitor()));
            _gravityWindow.GetSliders().ForEach(slider => Destroy(slider.GetSliderClickMonitor()));
        }

        private void OnDestroy()
        {
            try
            {
                Destroy(_settingsMonitor);
                _mainWindow.GetSliders().ForEach(slider => Destroy(slider.GetSliderClickMonitor()));
                _morphingWindow.GetSliders().ForEach(slider => Destroy(slider.GetSliderClickMonitor()));
                _gravityWindow.GetSliders().ForEach(slider => Destroy(slider.GetSliderClickMonitor()));
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
            _mainPhysicsHandler?.SaveOriginalPhysicsAndSetPluginDefaults();
            _softPhysicsHandler?.SaveOriginalPhysicsAndSetPluginDefaults();

            if(_initDone)
            {
                StartCoroutine(WaitToBeginRefresh(true));
            }
        }

        private void OnDisable()
        {
            if(_removingAtom)
            {
                return;
            }

            try
            {
                if(_settingsMonitor != null) _settingsMonitor.enabled = false;

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
