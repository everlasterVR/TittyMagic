//#define DEBUG_ON
//#define USE_CONFIGURATORS

using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static TittyMagic.Utils;
using static TittyMagic.Calc;
using static TittyMagic.Globals;

namespace TittyMagic
{
    internal class Script : MVRScript
    {
        public static readonly Version version = new Version("0.0.0");

        private Bindings _customBindings;

        private List<Rigidbody> _rigidbodies;
        private Rigidbody _chestRigidbody;
        private Transform _chestTransform;
        private Rigidbody _rNippleRigidbody;
        private Transform _rNippleTransform;
        private Rigidbody _lPectoralRigidbody;
        private Rigidbody _rPectoralRigidbody;
        private DAZCharacterSelector _geometry;
        private Vector3 _neutralRelativePos;

        private float _massEstimate;
        private float _massAmount;
        private float _massScaling;
        private float _softnessAmount;
        private float _gravityAmount;
        private float _upDownMobilityAmount;
        private float _angleY;

        private SettingsMonitor _settingsMonitor;

        private AtomScaleListener _atomScaleListener;
        private BreastMorphListener _breastMorphListener;
        private BreastMassCalculator _breastMassCalculator;

        private StaticPhysicsHandler _staticPhysicsH;
        private GravityPhysicsHandler _gravityPhysicsH;
        private GravityMorphHandler _gravityMorphH;
        private RelativePosMorphHandler _relativePosMorphH;
        private NippleErectionMorphHandler _nippleErectionMorphH;

        private JSONStorableString _titleUIText;
        private JSONStorableString _statusUIText;
        private InputField _statusUIInputField;
        private JSONStorableString _debugUIText;

        private JSONStorableString _modeInfoText;
        private JSONStorableString _gravityInfoText;
        private JSONStorableString _mobilityInfoText;

        private Dictionary<string, UIDynamicButton> _modeButtonGroup;

        private JSONStorableStringChooser _modeChooser;
        private JSONStorableString _pluginVersionStorable;
        private JSONStorableBool _autoRecalibrateOnSizeChange;
        private JSONStorableFloat _softness;
        private SliderClickMonitor _softnessSCM;
        private JSONStorableFloat _gravity;
        private SliderClickMonitor _gravitySCM;
        private JSONStorableFloat _upDownMobility;
        private SliderClickMonitor _upDownMobilitySCM;
        private JSONStorableBool _linkSoftnessAndGravity;
        private JSONStorableBool _linkGravityAndMobility;
        private UIDynamic _lowerLeftSpacer;
        private JSONStorableFloat _nippleErection;

        private bool _loadingFromJson;
        private float _timeSinceListenersChecked;
        private float _listenersCheckInterval = 0.1f;
        private int _waitStatus = -1;
        private int _refreshStatus = -1;
        private bool _animationWasSetFrozen = false;

        public override void Init()
        {
            try
            {
                _pluginVersionStorable = new JSONStorableString("Version", "");
                _pluginVersionStorable.val = $"{version}";
                _pluginVersionStorable.storeType = JSONStorableParam.StoreType.Full;
                RegisterString(_pluginVersionStorable);

                if(containingAtom.type != "Person")
                {
                    LogError($"Add to a Person atom, not {containingAtom.type}");
                    return;
                }

                AdjustJoints breastControl = containingAtom.GetStorableByID("BreastControl") as AdjustJoints;
                DAZPhysicsMesh breastPhysicsMesh = containingAtom.GetStorableByID("BreastPhysicsMesh") as DAZPhysicsMesh;
                _rigidbodies = containingAtom.GetComponentsInChildren<Rigidbody>().ToList();
                _chestRigidbody = _rigidbodies.Find(rb => rb.name == "chest");
                _chestTransform = _chestRigidbody.transform;
                _rNippleRigidbody = _rigidbodies.Find(rb => rb.name == "rNipple");
                _rNippleTransform = _rNippleRigidbody.transform;
                _lPectoralRigidbody = _rigidbodies.Find(rb => rb.name == "lPectoral");
                _rPectoralRigidbody = _rigidbodies.Find(rb => rb.name == "rPectoral");
                _geometry = containingAtom.GetStorableByID("geometry") as DAZCharacterSelector;

                SAVES_DIR = SuperController.singleton.savesDir + @"everlaster\TittyMagicSettings\";
#if USE_CONFIGURATORS
                MORPHMULTIPLIERS_DIRNAME = "morphmultipliers_dev";
#else
                MORPHMULTIPLIERS_DIRNAME = "morphmultipliers";
#endif
                PLUGIN_PATH = GetPackagePath(this) + @"Custom\Scripts\everlaster\TittyMagic\";
                BREAST_CONTROL = breastControl;
                BREAST_PHYSICS_MESH = breastPhysicsMesh;
                GEOMETRY = containingAtom.GetStorableByID("geometry") as DAZCharacterSelector;

                _settingsMonitor = gameObject.AddComponent<SettingsMonitor>();
                _settingsMonitor.Init(containingAtom);

                _atomScaleListener = new AtomScaleListener(containingAtom.GetStorableByID("rescaleObject").GetFloatJSONParam("scale"));
                _breastMorphListener = new BreastMorphListener(_geometry.morphBank1.morphs);
                _breastMassCalculator = new BreastMassCalculator(_chestTransform);

                _staticPhysicsH = new StaticPhysicsHandler();
#if USE_CONFIGURATORS
                _gravityPhysicsH = new GravityPhysicsHandler(FindPluginOnAtom(containingAtom, "GravityPhysicsConfigurator"));
                _gravityMorphH = new GravityMorphHandler(FindPluginOnAtom(containingAtom, "GravityMorphConfigurator"));
                _relativePosMorphH = new RelativePosMorphHandler(FindPluginOnAtom(containingAtom, "RelativePosMorphConfigurator"));
#else
                _gravityPhysicsH = new GravityPhysicsHandler(this);
                _gravityMorphH = new GravityMorphHandler(this);
                _relativePosMorphH = new RelativePosMorphHandler(this);
#endif
                _nippleErectionMorphH = new NippleErectionMorphHandler(this);

                InitPluginUILeft();
                InitPluginUIRight();
                InitSliderListeners();
                SuperController.singleton.onAtomRemovedHandlers += OnRemoveAtom;

                _softnessAmount = Mathf.Pow(_softness.val/100f, 1/2f);
                _gravityAmount = Mathf.Pow(_gravity.val/100f, 1/2f);

                StartCoroutine(SelectDefaultMode());
                StartCoroutine(SubscribeToKeybindings());
            }
            catch(Exception e)
            {
                LogError($"Init: {e}");
            }
        }

        private IEnumerator SelectDefaultMode()
        {
            yield return new WaitForEndOfFrame();
            if(_loadingFromJson)
            {
                yield break;
            }
            _modeChooser.val = Mode.BALANCED; // selection causes BeginRefresh
        }

        //https://github.com/vam-community/vam-plugins-interop-specs/blob/main/keybindings.md
        private IEnumerator SubscribeToKeybindings()
        {
            yield return new WaitForEndOfFrame();
            SuperController.singleton.BroadcastMessage("OnActionsProviderAvailable", this, SendMessageOptions.DontRequireReceiver);
        }

        public void OnBindingsListRequested(List<object> bindings)
        {
            _customBindings = gameObject.AddComponent<Bindings>();
            _customBindings.Init(this);
            bindings.Add(_customBindings.Settings);
            bindings.AddRange(_customBindings.OnKeyDownActions);
        }

        #region User interface

        private void InitPluginUILeft()
        {
            _titleUIText = this.NewTextField("titleText", "", 36, 100);
            _titleUIText.SetVal($"{nameof(TittyMagic)}\n<size=28>v{version}</size>");

            var modeSelection = this.NewTextField("modeSelection", "", 32, 100);
            modeSelection.SetVal("<size=28>\n\n</size><b>Mode selection</b>");
            modeSelection.dynamicText.backgroundColor = Color.clear;

            CreateModeChooser();
            _modeButtonGroup = this.CreateRadioButtonGroup(_modeChooser);
            _staticPhysicsH.modeChooser = _modeChooser;

            this.NewSpacer(10f);
            _softness = this.NewIntSlider("Breast softness", 75f, 0f, 100f);
            this.NewSpacer(10f);
            _linkSoftnessAndGravity = this.NewToggle("Link softness and gravity", true, false);
            _gravity = this.NewIntSlider("Breast gravity", 75f, 0f, 100f);
            this.NewSpacer(10f);
        }

        private void CreateModeChooser()
        {
            _modeChooser = new JSONStorableStringChooser(
                "Mode",
                new List<string>
                {
                    { Mode.ANIM_OPTIMIZED },
                    { Mode.BALANCED },
                    { Mode.TOUCH_OPTIMIZED }
                },
                "",
                "Mode",
                (mode) =>
                {
                    UI.UpdateButtonLabels(_modeButtonGroup, mode);
                    StartCoroutine(OnModeChosen(mode));
                }
            );
            _modeChooser.storeType = JSONStorableParam.StoreType.Full;
            RegisterStringChooser(_modeChooser);
        }

        private IEnumerator OnModeChosen(string mode)
        {
            _gravityPhysicsH.LoadSettings(mode);
            _staticPhysicsH.LoadSettings(this, mode);
            _gravityMorphH.LoadSettings(mode);
            if(mode == Mode.ANIM_OPTIMIZED)
            {
                //RelativePosMorphHandler doesn't actually support any other mode currently
                _relativePosMorphH.LoadSettings(mode);
            }

            UpdateModeInfoText(mode);
            UpdateGravityInfoText(mode);
            BuildPluginUILowerLeft();
            BuildPluginUILowerRight();

            StartCoroutine(TempDisableModeButtons());

            yield return WaitToBeginRefresh();
        }

        private void UpdateModeInfoText(string mode)
        {
            string text = UI.Size("\n", 12);
            if(mode == Mode.ANIM_OPTIMIZED)
            {
                text += "Animation optimized mode morphs breasts in response to up/down forces. Sideways and forward/back shape is based only on the chest angle. Physics settings are similar to balanced mode, but hard colliders are off.";
            }
            else if(mode == Mode.BALANCED)
            {
                text += "In balanced mode, breasts have realistic mass which helps with animation, and hard colliders are on to enable collision to move breasts as well.";
            }
            else if(mode == Mode.TOUCH_OPTIMIZED)
            {
                text += "Touch optimized mode lowers breast mass, increases fat back force and turns off hard colliders. While breast movement due to gravity and other forces isn't very realistic in this mode, collision is based only on soft physics.";
            }
            _modeInfoText.SetVal(text);
        }

        private void UpdateGravityInfoText(string mode)
        {
            string infoTextEnd = mode == Mode.ANIM_OPTIMIZED ?
                "when leaning left/right and forward/back" :
                "in all orientations";
            _gravityInfoText.SetVal(
                UI.Size("\n", 12) +
                $"Adjusts how much pose morphs shape the breasts {infoTextEnd}."
            );
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

        private void InitPluginUIRight()
        {
            bool rightSide = true;
            _statusUIText = this.NewTextField("statusText", "", 28, 50, rightSide);
            _statusUIInputField = UI.NewInputField(_statusUIText.dynamicText);
            _statusUIInputField.interactable = false;
            _autoRecalibrateOnSizeChange = this.NewToggle("Auto-recalibrate if size changed", true, rightSide);
            _autoRecalibrateOnSizeChange.storeType = JSONStorableParam.StoreType.Full;

            var recalibrateButton = CreateButton("Recalibrate physics", rightSide);
            recalibrateButton.button.interactable = !_autoRecalibrateOnSizeChange.val;

            _autoRecalibrateOnSizeChange.toggle.onValueChanged.AddListener(val => recalibrateButton.button.interactable = !val);
            recalibrateButton.button.onClick.AddListener(() =>
                StartCoroutine(WaitToBeginRefresh(triggeredManually: true))
            );

            this.NewSpacer(20f, rightSide);
            _modeInfoText = this.NewTextField("Usage Info Area 2", "", 28, 210, rightSide);

            this.NewSpacer(10f, rightSide);
            JSONStorableString softnessInfoText = this.NewTextField("Usage Info Area 1", "", 28, 120, rightSide);
            softnessInfoText.SetVal(
                UI.Size("\n", 12) +
                "Adjusts soft physics settings from very firm to very soft."
            );

            this.NewSpacer(75f, rightSide);
            _gravityInfoText = this.NewTextField("GravityInfoText", "", 28, 120, true);
            this.NewSpacer(75f, rightSide);

#if DEBUG_ON
            _debugUIText = this.NewTextField("debugText", "", 28, 200, rightSide);
#endif
        }

        #endregion User interface

        private void InitSliderListeners()
        {
            _softnessSCM = _softness.slider.gameObject.AddComponent<SliderClickMonitor>();
            _gravitySCM = _gravity.slider.gameObject.AddComponent<SliderClickMonitor>();

            _softness.slider.onValueChanged.AddListener((float val) =>
            {
                float newAmount = Mathf.Pow(val/100f, 1/2f);
                if(newAmount == _softnessAmount)
                {
                    return;
                }
                _softnessAmount = newAmount;
                if(_linkSoftnessAndGravity.val)
                {
                    _gravity.val = val;
                    _gravityAmount = Mathf.Pow(val/100f, 1/2f);
                }
                RefreshFromSliderChanged();
            });
            _gravity.slider.onValueChanged.AddListener((float val) =>
            {
                _gravityAmount = Mathf.Pow(val/100f, 1/2f);
                if(_linkSoftnessAndGravity.val)
                {
                    _softness.val = val;
                    float newAmount = Mathf.Pow(val/100f, 1/2f);
                    if(newAmount == _softnessAmount)
                    {
                        return;
                    }
                    _softnessAmount = Mathf.Pow(val/100f, 1/2f);
                    RefreshFromSliderChanged();
                }
            });
        }

        private void GravityListenerForMobilityLink(float val)
        {
            if(_linkGravityAndMobility.val)
            {
                _upDownMobility.val = 2/3f * val;
                _upDownMobilityAmount = 1.5f * Mathf.Pow(_upDownMobility.val/100f, 1/2f);
            }
        }

        private void BuildPluginUILowerLeft()
        {
            if(_linkGravityAndMobility != null)
                RemoveToggle(_linkGravityAndMobility);
            if(_upDownMobility != null)
                RemoveSlider(_upDownMobility);
            if(_lowerLeftSpacer != null)
                RemoveSpacer(_lowerLeftSpacer);
            if(_nippleErection != null)
                RemoveSlider(_nippleErection);

            if(_modeChooser.val == Mode.ANIM_OPTIMIZED)
            {
                if(_linkGravityAndMobility == null)
                {
                    _linkGravityAndMobility = this.NewToggle("Link gravity and mobility", true, false);
                    _linkGravityAndMobility.storeType = JSONStorableParam.StoreType.Full;
                }
                else
                {
                    CreateToggle(_linkGravityAndMobility, false);
                }
                _upDownMobility = this.NewIntSlider("Up/down mobility", 2/3f * _gravity.val, 0f, 100f);
                _upDownMobilityAmount = 1.5f * Mathf.Pow(_upDownMobility.val/100f, 1/2f);

                _upDownMobilitySCM = _upDownMobility.slider.gameObject.AddComponent<SliderClickMonitor>();
                _upDownMobility.slider.onValueChanged.AddListener((float val) =>
                {
                    _upDownMobilityAmount = 1.5f * Mathf.Pow(val/100f, 1/2f);
                    if(_linkGravityAndMobility.val)
                    {
                        _gravity.val = 1.5f * val;
                        float newAmount = Mathf.Pow(_gravity.val/100f, 1/2f);
                        if(newAmount == _gravityAmount)
                        {
                            return;
                        }
                        _gravityAmount = newAmount;
                        if(_linkSoftnessAndGravity.val)
                        {
                            RefreshFromSliderChanged();
                        }
                    }
                });

                _gravity.slider.onValueChanged.AddListener(GravityListenerForMobilityLink);
            }
            else
            {
                try
                {
                    Destroy(_upDownMobilitySCM);
                    _gravity.slider.onValueChanged.RemoveListener(GravityListenerForMobilityLink);
                }
                catch(Exception)
                {
                }
            }

            float spacerHeight = _modeChooser.val == Mode.ANIM_OPTIMIZED ? 0f : 200f;
            _lowerLeftSpacer = this.NewSpacer(spacerHeight);

            if(_nippleErection == null)
            {
                _nippleErection = this.NewFloatSlider("Nipple erection", 0f, 0f, 1.0f, "F2");
                _nippleErection.storeType = JSONStorableParam.StoreType.Full;
            }
            else
            {
                UIDynamicSlider slider = CreateSlider(_nippleErection, false);
                slider.valueFormat = "F2";
            }
            _nippleErection.slider.onValueChanged.AddListener((float val) =>
            {
                _nippleErectionMorphH.Update(val);
                _staticPhysicsH.UpdateNipplePhysics(_softnessAmount, val);
            });
        }

        private void BuildPluginUILowerRight()
        {
            if(_mobilityInfoText != null)
                RemoveTextField(_mobilityInfoText);

            if(_modeChooser.val == Mode.ANIM_OPTIMIZED)
            {
                if(_mobilityInfoText == null)
                {
                    _mobilityInfoText = this.NewTextField("MobilityInfoText", "", 28, 120, true);
                }
                else
                {
                    UIDynamicTextField field = CreateTextField(_mobilityInfoText, true);
                    field.UItext.fontSize = 28;
                    field.height = 120;
                }
                _mobilityInfoText.SetVal(
                    UI.Size("\n", 12) +
                    "Adjusts the amount of up/down morphing due to forces including gravity."
                );
            }
        }

        private void RefreshFromSliderChanged()
        {
            if(_loadingFromJson)
            {
                return;
            }
            if(_modeChooser.val == Mode.ANIM_OPTIMIZED && _waitStatus != RefreshStatus.WAITING)
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
            Vector3 relativePos = RelativePosition(_chestTransform, _rNippleTransform.position);
            _angleY = Vector2.SignedAngle(
                new Vector2(_neutralRelativePos.z, _neutralRelativePos.y),
                new Vector2(relativePos.z, relativePos.y)
            );

#if DEBUG_ON
            _debugUIText.SetVal(
                $"softness {_softnessAmount}\n" +
                $"gravity {_gravityAmount}\n" +
                $"upDownMobility {_upDownMobilityAmount}"
            );
#endif
        }

        private void FixedUpdate()
        {
            try
            {
                DoFixedUpdate();
            }
            catch(Exception e)
            {
                LogError($"FixedUpdate: {e}");
                enabled = false;
            }
        }

        private void DoFixedUpdate()
        {
            if(_refreshStatus == RefreshStatus.MASS_STARTED)
            {
                return;
            }

            if(_refreshStatus > RefreshStatus.MASS_STARTED)
            {
                // simulate gravityPhysics when upright
                Quaternion zero = new Quaternion(0, 0, 0, -1);
                _gravityPhysicsH.Update(0, 0, _massAmount, _gravityAmount);

                // simulate force of gravity when upright
                // 0.75f is a hack, for some reason a normal gravity force pushes breasts too much down,
                // causing the neutral position to be off by a little
                Vector3 force = _chestRigidbody.transform.up * 0.75f * -Physics.gravity.magnitude;
                _lPectoralRigidbody.AddForce(force, ForceMode.Acceleration);
                _rPectoralRigidbody.AddForce(force, ForceMode.Acceleration);
                if(_modeChooser.val == Mode.ANIM_OPTIMIZED)
                {
                    if(_refreshStatus == RefreshStatus.MASS_OK)
                    {
                        StartCoroutine(RefreshNeutralRelativePosition());
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
                return;
            }

            _timeSinceListenersChecked += Time.deltaTime;
            if(_timeSinceListenersChecked >= _listenersCheckInterval)
            {
                _timeSinceListenersChecked -= _listenersCheckInterval;
                if(
                    _autoRecalibrateOnSizeChange.val &&
                    _waitStatus != RefreshStatus.WAITING &&
                    (_breastMorphListener.Changed() || _atomScaleListener.Changed()) &&
                    DeviatesAtLeast(_massEstimate, DetermineMassEstimate(_atomScaleListener.Value), percent: 10)
                )
                {
                    StartCoroutine(WaitToBeginRefresh());
                    return;
                }
            }

            if(_waitStatus != RefreshStatus.DONE)
            {
                return;
            }

            float roll = Roll(_chestTransform.rotation);
            float pitch = Pitch(_chestTransform.rotation);

            if(_modeChooser.val == Mode.ANIM_OPTIMIZED)
            {
                //float positionDiffZ = (neutralRelativePos - relativePos).z;
                if(_relativePosMorphH.IsEnabled())
                {
                    _relativePosMorphH.Update(_angleY, 0f, _massAmount, _massScaling, _upDownMobilityAmount);
                }
            }

            if(_gravityMorphH.IsEnabled())
            {
                _gravityMorphH.Update(_modeChooser.val, roll, pitch, _massAmount, 0.75f * _gravityAmount);
            }

            if(_gravityPhysicsH.IsEnabled())
            {
                _gravityPhysicsH.Update(roll, pitch, _massAmount, _gravityAmount);
            }
        }

        private IEnumerator WaitToBeginRefresh(bool triggeredManually = false)
        {
            _waitStatus = RefreshStatus.WAITING;
            while(_refreshStatus != RefreshStatus.DONE && _refreshStatus != -1)
            {
                yield return null;
            }

            yield return BeginRefresh(triggeredManually);
        }

        public IEnumerator BeginRefresh(bool triggeredManually = false)
        {
            _animationWasSetFrozen = _loadingFromJson ? false : SuperController.singleton.freezeAnimation;

            SuperController.singleton.SetFreezeAnimation(true);

            if(!triggeredManually)
            {
                // ensure refresh actually begins only once listeners report no change
                yield return new WaitForSeconds(_listenersCheckInterval);
                while(_breastMorphListener.Changed() ||
                    _atomScaleListener.Changed() ||
                    _softnessSCM.isDown ||
                    _gravitySCM.isDown ||
                    (_upDownMobilitySCM != null && _upDownMobilitySCM.isDown))
                {
                    yield return new WaitForSeconds(0.1f);
                }
                yield return new WaitForSeconds(0.33f);
            }

            _refreshStatus = RefreshStatus.MASS_STARTED;

            _settingsMonitor.enabled = false;

            // simulate breasts zero G
            _lPectoralRigidbody.useGravity = false;
            _rPectoralRigidbody.useGravity = false;

            // zero pose morphs
            _gravityMorphH.ResetAll();
            if(_modeChooser.val == Mode.ANIM_OPTIMIZED)
            {
                _relativePosMorphH.ResetAll();
            }
            _gravityPhysicsH.ZeroAll();

            yield return new WaitForSeconds(0.33f);

            float duration = 0;
            float interval = 0.1f;
            while(duration < 1f && (
                !VectorEqualWithin(1000f, _rNippleRigidbody.velocity, Vector3.zero) ||
                !EqualWithin(1000f, _massEstimate, DetermineMassEstimate(_atomScaleListener.Value))
            ))
            {
                yield return new WaitForSeconds(interval);
                duration += interval;

                // update mass estimate
                _massEstimate = DetermineMassEstimate(_atomScaleListener.Value);

                // update main static physics
                _massAmount = _staticPhysicsH.SetAndReturnMassVal(_massEstimate);
                _massScaling = Mathf.Pow(3/4f * _massAmount, 1/5f);
                _staticPhysicsH.UpdateMainPhysics(_softnessAmount);
            }
            SetMassUIStatus(_atomScaleListener.Value);
            _staticPhysicsH.FullUpdate(_softnessAmount, _nippleErection.val);
            _gravityPhysicsH.SetBaseValues();

            _refreshStatus = RefreshStatus.MASS_OK;
        }

        private IEnumerator RefreshNeutralRelativePosition()
        {
            _refreshStatus = RefreshStatus.NEUTRALPOS_STARTED;

            yield return new WaitForSeconds(0.67f);

            float duration = 0;
            float interval = 0.1f;
            while(
                duration < 1f && (
                !VectorEqualWithin(
                    1000000f,
                    _neutralRelativePos,
                    RelativePosition(_chestTransform, _rNippleTransform.position)
                ))
            )
            {
                yield return new WaitForSeconds(interval);
                duration += interval;
                _neutralRelativePos = RelativePosition(_chestTransform, _rNippleTransform.position);
            }

            _refreshStatus = RefreshStatus.NEUTRALPOS_OK;
        }

        private void EndRefresh()
        {
            _lPectoralRigidbody.useGravity = true;
            _rPectoralRigidbody.useGravity = true;
            SuperController.singleton.SetFreezeAnimation(_animationWasSetFrozen);
            _settingsMonitor.enabled = true;
            _loadingFromJson = false;
            _waitStatus = RefreshStatus.DONE;
            _refreshStatus = RefreshStatus.DONE;
        }

        public void RefreshRateDependentPhysics()
        {
            _staticPhysicsH.UpdateRateDependentPhysics(_softnessAmount);
        }

        private float DetermineMassEstimate(float atomScale)
        {
            float mass = _breastMassCalculator.Calculate(atomScale);
            if(mass > Const.MASS_MAX)
                return Const.MASS_MAX;

            if(mass < Const.MASS_MIN)
                return Const.MASS_MIN;

            return mass;
        }

        private void SetMassUIStatus(float atomScale)
        {
            float mass = _breastMassCalculator.Calculate(atomScale);
            string text = $"Mass is {RoundToDecimals(mass, 1000f)}kg";
            if(mass > Const.MASS_MAX)
            {
                float excess = RoundToDecimals(mass - Const.MASS_MAX, 1000f);
                text = MassExcessStatus(excess);
            }
            else if(mass < Const.MASS_MIN)
            {
                float shortage = RoundToDecimals(Const.MASS_MIN - mass, 1000f);
                text = MassShortageStatus(shortage);
            }
            _statusUIText.SetVal(text);
            _statusUIInputField.text = text;
        }

        private string MassExcessStatus(float value)
        {
            return $"Mass is {value}kg over the 2kg max";
        }

        private string MassShortageStatus(float value)
        {
            return $"Mass is {value}kg below the 0.1kg min";
        }

        public override void RestoreFromJSON(JSONClass json, bool restorePhysical = true, bool restoreAppearance = true, JSONArray presetAtoms = null, bool setMissingToDefault = true)
        {
            if(json.HasKey("Mode"))
            {
                _loadingFromJson = true;
                _modeChooser.val = json["Mode"];
            }
            base.RestoreFromJSON(json, restorePhysical, restoreAppearance, presetAtoms, setMissingToDefault);
        }

        private void OnRemoveAtom(Atom atom)
        {
            Destroy(_settingsMonitor);
            Destroy(_softnessSCM);
            Destroy(_gravitySCM);
            Destroy(_upDownMobilitySCM);
        }

        private void OnDestroy()
        {
            try
            {
                Destroy(_settingsMonitor);
                Destroy(_softnessSCM);
                Destroy(_gravitySCM);
                Destroy(_upDownMobilitySCM);
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
            if(_settingsMonitor != null)
                _settingsMonitor.enabled = true;
            if(_gravityPhysicsH != null)
                _gravityPhysicsH.SetInvertJoint2RotationY(false);
        }

        private void OnDisable()
        {
            try
            {
                if(_settingsMonitor != null)
                    _settingsMonitor.enabled = false;
                if(_gravityPhysicsH != null)
                {
                    _gravityPhysicsH.ResetAll();
                    _gravityPhysicsH.SetInvertJoint2RotationY(true);
                }
                if(_gravityMorphH != null)
                    _gravityMorphH.ResetAll();
                if(_modeChooser.val == Mode.ANIM_OPTIMIZED && _relativePosMorphH != null)
                    _relativePosMorphH.ResetAll();
                if(_nippleErectionMorphH != null)
                    _nippleErectionMorphH.ResetAll();
            }
            catch(Exception e)
            {
                LogError($"OnDisable: {e}");
            }
        }
    }
}
