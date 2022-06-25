using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using TittyMagic.Extensions;
using UnityEngine;
using TittyMagic.UI;
using static TittyMagic.Utils;
using static TittyMagic.Calc;
using static TittyMagic.Globals;
using Navigation = TittyMagic.UI.Navigation;

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

        private PhysicsHandler _physicsHandler;
        private GravityPhysicsHandler _gravityPhysicsHandler;
        private ForceMorphHandler _forceMorphHandler;
        private GravityOffsetMorphHandler _gravityOffsetMorphHandler;
        private NippleErectionMorphHandler _nippleErectionMorphHandler;

        private JSONStorableString _pluginVersionStorable;

        private Navigation _navigation;
        private MainWindow _mainWindow;
        private MorphingWindow _morphingWindow;
        private GravityWindow _gravityWindow;
        private AdvancedWindow _advancedWindow;

        public JSONStorableString titleText;
        public JSONStorableBool autoRefresh;
        public JSONStorableFloat mass;
        public JSONStorableFloat softness;
        public JSONStorableFloat quickness;
        public JSONStorableString morphingTitleText;
        public JSONStorableFloat morphingYStorable;
        public JSONStorableFloat morphingXStorable;
        public JSONStorableFloat morphingZStorable;
        public JSONStorableString morphingInfoText;
        public JSONStorableString gravityTitleText;
        public JSONStorableFloat gravityYStorable;
        public JSONStorableFloat gravityXStorable;
        public JSONStorableFloat gravityZStorable;
        public JSONStorableString gravityInfoText;
        public JSONStorableString additionalSettingsTitleText;
        public JSONStorableString additionalSettingsInfoText;
        public JSONStorableFloat offsetMorphing;
        public JSONStorableFloat nippleErection;

        private bool _isFemale;
        public bool softPhysicsEnabled;
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

            _physicsHandler = new PhysicsHandler(_isFemale);
            _gravityPhysicsHandler = new GravityPhysicsHandler(_physicsHandler);
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
                _breastMorphListener = new BreastMorphListener(GEOMETRY.morphBank1.morphs);
            }
            else
            {
                _trackLeftNipple.getNipplePosition = () => AveragePosition(
                    VertexIndexGroups.LEFT_BREAST_CENTER.Select(i => _skin.rawSkinnedWorkingVerts[i]).ToList()
                );
                _trackRightNipple.getNipplePosition = () => AveragePosition(
                    VertexIndexGroups.RIGHT_BREAST_CENTER.Select(i => _skin.rawSkinnedWorkingVerts[i]).ToList()
                );

                _breastMorphListener = new BreastMorphListener(GEOMETRY.morphBank1OtherGender.morphs, GEOMETRY.morphBank1.morphs);
            }

            _forceMorphHandler = new ForceMorphHandler(this, _trackLeftNipple, _trackRightNipple);

            _mainWindow = new MainWindow(this);
            _morphingWindow = new MorphingWindow(this);
            _gravityWindow = new GravityWindow(this);
            _advancedWindow = new AdvancedWindow(this);

            SetupStorables();
            CreateNavigation();
            NavigateToMainWindow();
            LoadSettings();
            InitializeValuesAppliedByListeners();

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

        private void InitializeValuesAppliedByListeners()
        {
            UIHelpers.ApplySliderStyle(_mainWindow.massSlider);
            _mainWindow.massSlider.slider.interactable = !autoRefresh.val;
            _softnessAmount = CalculateSoftnessAmount(softness.val);
            _quicknessAmount = CalculateQuicknessAmount(quickness.val);
            _forceMorphHandler.yMultiplier.mainMultiplier = Curves.QuadraticRegression(morphingYStorable.val);
            _forceMorphHandler.xMultiplier.mainMultiplier = Curves.QuadraticRegression(morphingXStorable.val);
            _forceMorphHandler.zMultiplier.mainMultiplier = Curves.QuadraticRegressionLesser(morphingZStorable.val);
            _gravityPhysicsHandler.yMultiplier.mainMultiplier = gravityYStorable.val;
            _gravityPhysicsHandler.xMultiplier.mainMultiplier = gravityXStorable.val;
            _gravityPhysicsHandler.zMultiplier.mainMultiplier = gravityZStorable.val;
            _gravityOffsetMorphHandler.yMultiplier.mainMultiplier = gravityYStorable.val;
        }

        public void LoadSettings()
        {
            _forceMorphHandler.LoadSettings();
            _gravityPhysicsHandler.LoadSettings();
            _gravityOffsetMorphHandler.LoadSettings();
            _physicsHandler.LoadSettings(_isFemale && softPhysicsEnabled);
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
            titleText = new JSONStorableString("titleText", "");
            autoRefresh = this.NewJsonStorableBool("Auto-update mass", true);
            mass = this.NewJsonStorableFloat("Breast mass", Const.MASS_MIN, Const.MASS_MIN, Const.MASS_MAX);
            softness = this.NewJsonStorableFloat("Breast softness", 70f, 0f, 100f);
            quickness = this.NewJsonStorableFloat("Breast quickness", 70f, 0f, 100f);

            morphingTitleText = new JSONStorableString("morphingMultipliers", "");
            morphingYStorable = this.NewJsonStorableFloat("Morphing Up/down", 1.00f, 0.00f, 3.00f);
            morphingXStorable = this.NewJsonStorableFloat("Morphing Left/right", 1.00f, 0.00f, 3.00f);
            morphingZStorable = this.NewJsonStorableFloat("Morphing Forward/back", 1.00f, 0.00f, 3.00f);
            morphingInfoText = new JSONStorableString("morphingInfoText", "");

            gravityTitleText = new JSONStorableString("gravityPhysicsMultipliers", "");
            gravityYStorable = this.NewJsonStorableFloat("Physics Up/down", 1.00f, 0.00f, 2.00f);
            gravityXStorable = this.NewJsonStorableFloat("Physics Left/right", 1.00f, 0.00f, 2.00f);
            gravityZStorable = this.NewJsonStorableFloat("Physics Forward/back", 1.00f, 0.00f, 2.00f);
            gravityInfoText = new JSONStorableString("gravityInfoText", "");

            additionalSettingsTitleText = new JSONStorableString("additionalSettings", "");
            additionalSettingsInfoText = new JSONStorableString("additionalSettingsInfoText", "");
            offsetMorphing = this.NewJsonStorableFloat("Gravity offset morphing", 1.00f, 0.00f, 2.00f);
            nippleErection = this.NewJsonStorableFloat("Nipple erection", 0.00f, 0.00f, 1.00f);

            titleText.val = $"<size=18>\n</size><b>{nameof(TittyMagic)}</b><size=36>    v{VERSION}</size>";
            morphingTitleText.val = "<size=28>\n\n</size><b>Dynamic morphing multipliers</b>";
            morphingInfoText.val = UIHelpers.SizeTag("\n", 12) +
                "Adjust the amount of breast morphing due to forces including gravity.\n" +
                "\n" +
                "Breasts morph up/down, left/right and forward/back.";
            additionalSettingsTitleText.val = "<size=28>\n\n</size><b>Additional settings</b>";
            additionalSettingsInfoText.val = UIHelpers.SizeTag("\n", 12) +
                "Rotate breasts up when upright to compensate for negative Up/Down Angle Target.";
            gravityTitleText.val = "<size=28>\n\n</size><b>Gravity physics multipliers</b>";
            gravityInfoText.val = UIHelpers.SizeTag("\n", 12) +
                "Adjust the effect of chest angle on breast main physics settings.\n" +
                "\n" +
                "Higher values mean breasts drop more heavily up/down and left/right, " +
                "are more swingy when leaning forward, and less springy when leaning back.";
        }

        private void CreateNavigation()
        {
            _navigation = new Navigation(leftUIContent, rightUIContent);
            _navigation.instantiateButton = () => Instantiate(manager.configurableButtonPrefab).GetComponent<UIDynamicButton>();

            _navigation.CreateUINavigationButtons();
            _navigation.mainSettingsButton.button.onClick.AddListener(NavigateToMainWindow);
            _navigation.morphingButton.button.onClick.AddListener(NavigateToMorphingWindow);
            _navigation.gravityButton.button.onClick.AddListener(NavigateToGravityWindow);
            _navigation.physicsButton.button.onClick.AddListener(NavigateToAdvancedWindow);
        }

        private void NavigateToMainWindow()
        {
            if(_navigation.activeWindow?.Id() == 1)
            {
                return;
            }

            _navigation.activeWindow?.Clear();
            _navigation.activeWindow = _mainWindow;
            _navigation.activeWindow.Rebuild();

            _mainWindow.autoRefreshToggle.toggle.onValueChanged.AddListener(val =>
            {
                _mainWindow.massSlider.slider.interactable = !val;
                UIHelpers.ApplySliderStyle(_mainWindow.massSlider);
            });

            _mainWindow.autoRefreshToggle.toggle.onValueChanged.AddListener(val =>
            {
                if(val && DeviatesAtLeast(mass.val, CalculateMass(), 10))
                {
                    StartCoroutine(WaitToBeginRefresh(refreshMass: true, fromToggleOrButton: true));
                }
            });

            _mainWindow.refreshButton.button.onClick.AddListener(() =>
            {
                StartCoroutine(WaitToBeginRefresh(refreshMass: true, fromToggleOrButton: true, useNewMass: true));
            });

            _mainWindow.massSlider.slider.onValueChanged.AddListener(val =>
            {
                RefreshFromSliderChanged(refreshMass: true);
            });

            _mainWindow.softnessSlider.slider.onValueChanged.AddListener(val =>
            {
                if(Math.Abs(CalculateSoftnessAmount(val) - _softnessAmount) < 0.001f)
                {
                    return;
                }

                RefreshFromSliderChanged();
            });

            _mainWindow.quicknessSlider.slider.onValueChanged.AddListener(val =>
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
            if(_navigation.activeWindow?.Id() == 2)
            {
                return;
            }

            _navigation.activeWindow?.Clear();
            _navigation.activeWindow = _morphingWindow;
            _navigation.activeWindow.Rebuild();

            _morphingWindow.morphingYSlider.slider.onValueChanged.AddListener(val =>
            {
                _forceMorphHandler.yMultiplier.mainMultiplier = Curves.QuadraticRegression(val);
            });

            _morphingWindow.morphingXSlider.slider.onValueChanged.AddListener(val =>
            {
                _forceMorphHandler.xMultiplier.mainMultiplier = Curves.QuadraticRegression(val);
            });

            _morphingWindow.morphingZSlider.slider.onValueChanged.AddListener(val =>
            {
                _forceMorphHandler.zMultiplier.mainMultiplier = Curves.QuadraticRegressionLesser(val);
            });

            _morphingWindow.offsetMorphingSlider.slider.onValueChanged.AddListener(val =>
            {
                RefreshFromSliderChanged();
            });

            _morphingWindow.nippleErectionSlider.slider.onValueChanged.AddListener(val =>
            {
                _nippleErectionMorphHandler.Update(val);
                if(_isFemale)
                {
                    _physicsHandler.UpdateNipplePhysics(_softnessAmount, val);
                }
            });
        }

        private void NavigateToGravityWindow()
        {
            if(_navigation.activeWindow?.Id() == 3)
            {
                return;
            }

            _navigation.activeWindow?.Clear();
            _navigation.activeWindow = _gravityWindow;
            _navigation.activeWindow.Rebuild();

            _gravityWindow.gravityYSlider.slider.onValueChanged.AddListener(val =>
            {
                _gravityPhysicsHandler.yMultiplier.mainMultiplier = val;
                _gravityOffsetMorphHandler.yMultiplier.mainMultiplier = val;
                RefreshFromSliderChanged();
            });

            _gravityWindow.gravityXSlider.slider.onValueChanged.AddListener(val =>
            {
                _gravityPhysicsHandler.xMultiplier.mainMultiplier = val;
                RefreshFromSliderChanged();
            });

            _gravityWindow.gravityZSlider.slider.onValueChanged.AddListener(val =>
            {
                _gravityPhysicsHandler.zMultiplier.mainMultiplier = val;
                RefreshFromSliderChanged();
            });
        }

        private void NavigateToAdvancedWindow()
        {
            if(_navigation.activeWindow?.Id() == 4)
            {
                return;
            }

            _navigation.activeWindow?.Clear();
            _navigation.activeWindow = _advancedWindow;
            _navigation.activeWindow.Rebuild();
        }

        public void OnAutoRefreshToggled(bool val)
        {
            if(val && DeviatesAtLeast(mass.val, CalculateMass(), 10))
            {
                StartCoroutine(WaitToBeginRefresh(refreshMass: true, fromToggleOrButton: true));
            }
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
            _forceMorphHandler.Update(
                _chestRoll,
                _chestPitch,
                _realMassAmount
            );

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
                    _mainWindow.massSlider.IsClickDown() ||
                    _mainWindow.softnessSlider.IsClickDown() ||
                    _mainWindow.quicknessSlider.IsClickDown() ||
                    _morphingWindow.offsetMorphingSlider.IsClickDown() ||
                    _gravityWindow.gravityXSlider.IsClickDown() ||
                    _gravityWindow.gravityYSlider.IsClickDown() ||
                    _gravityWindow.gravityZSlider.IsClickDown()
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
                UpdateMassValueAndAmounts(useNewMass);
                SetMorphingExtraMultipliers();
            }

            _softnessAmount = CalculateSoftnessAmount(softness.val);
            _quicknessAmount = CalculateQuicknessAmount(quickness.val);

            _physicsHandler.UpdateMainPhysics(_softnessAmount, _quicknessAmount);
            if(_isFemale && softPhysicsEnabled)
            {
                _physicsHandler.UpdateSoftPhysics(_softnessAmount, _quicknessAmount);
                _physicsHandler.UpdateNipplePhysics(_softnessAmount, nippleErection.val);
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

            _physicsHandler.UpdateMainPhysics(_softnessAmount, _quicknessAmount);
            if(_isFemale && softPhysicsEnabled)
            {
                _physicsHandler.UpdateSoftPhysics(_softnessAmount, _quicknessAmount);
                _physicsHandler.UpdateNipplePhysics(_softnessAmount, nippleErection.val);
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

                UpdateMassValueAndAmounts(useNewMass);
                _physicsHandler.UpdateMainPhysics(_softnessAmount, _quicknessAmount);
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
            _forceMorphHandler.yMultiplier.extraMultiplier = softnessMultiplier * (3.15f - 1.40f * _realMassAmount);
            _forceMorphHandler.xMultiplier.extraMultiplier = softnessMultiplier * (3.55f - 1.40f * _realMassAmount);
            _forceMorphHandler.zMultiplier.extraMultiplier = softnessMultiplier * (3.8f - 1.5f * _realMassAmount);
            _forceMorphHandler.zMultiplier.oppositeExtraMultiplier = softnessMultiplier * (3.8f - 1.5f * _realMassAmount);
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
                // simulate gravityPhysics when upright
                _gravityPhysicsHandler.Update(0, 0, _massAmount, _softnessAmount);
                _gravityOffsetMorphHandler.Update(0, 0, _massAmount, _softnessAmount, offsetMorphing.val);

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
                (_breastMorphListener.Changed() || _atomScaleListener.Changed()) &&
                DeviatesAtLeast(mass.val, CalculateMass(), 10);
        }

        public void UpdateRateDependentPhysics()
        {
            _physicsHandler.UpdateRateDependentPhysics(_softnessAmount, _quicknessAmount);
        }

        private void UpdateMassValueAndAmounts(bool useNewMass)
        {
            float calculatedMass = CalculateMass();
            _realMassAmount = Mathf.InverseLerp(0, Const.MASS_MAX, calculatedMass);
            if(useNewMass)
            {
                _massAmount = _realMassAmount;
                mass.val = calculatedMass;
            }
            else
            {
                _massAmount = Mathf.InverseLerp(0, Const.MASS_MAX, mass.val);
            }

            BREAST_CONTROL.mass = mass.val;
            _physicsHandler.realMassAmount = _realMassAmount;
            _physicsHandler.massAmount = _massAmount;
        }

        private float CalculateMass()
        {
            return Mathf.Clamp(
                Mathf.Pow(0.78f * _breastVolumeCalculator.Calculate(_atomScaleListener.scale), 1.5f),
                mass.min,
                mass.max
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
            Destroy(_mainWindow.massSlider.GetSliderClickMonitor());
            Destroy(_mainWindow.softnessSlider.GetSliderClickMonitor());
            Destroy(_mainWindow.quicknessSlider.GetSliderClickMonitor());
            Destroy(_gravityWindow.gravityXSlider.GetSliderClickMonitor());
            Destroy(_gravityWindow.gravityYSlider.GetSliderClickMonitor());
            Destroy(_gravityWindow.gravityZSlider.GetSliderClickMonitor());
            Destroy(_morphingWindow.offsetMorphingSlider.GetSliderClickMonitor());
        }

        private void OnDestroy()
        {
            try
            {
                Destroy(_settingsMonitor);
                Destroy(_mainWindow.massSlider.GetSliderClickMonitor());
                Destroy(_mainWindow.softnessSlider.GetSliderClickMonitor());
                Destroy(_mainWindow.quicknessSlider.GetSliderClickMonitor());
                Destroy(_gravityWindow.gravityXSlider.GetSliderClickMonitor());
                Destroy(_gravityWindow.gravityYSlider.GetSliderClickMonitor());
                Destroy(_gravityWindow.gravityZSlider.GetSliderClickMonitor());
                Destroy(_morphingWindow.offsetMorphingSlider.GetSliderClickMonitor());
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

                _physicsHandler?.Reset();
                _gravityOffsetMorphHandler?.ResetAll();
                _forceMorphHandler?.ResetAll();
                _nippleErectionMorphHandler?.ResetAll();
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
