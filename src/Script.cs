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
        public static bool envIsDevelopment => VERSION.Major == 0;

        public static GenerateDAZMorphsControlUI morphsControlUI { get; private set; }

        public DAZSkinV2 skin { get; set; }

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

        public CalibrationHelper calibration { get; private set; }

        #region InitUI

        private UnityEventsListener _pluginUIEventsListener;
        private Tabs _tabs;

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
                if(enabled && calibration.shouldRun)
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
                colliderVisualizer.enabled = false;

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
                background.color = UIHelpers.backgroundGray;

                SoftPhysicsHandler.ReverseSyncAllowSelfCollision();

                if(_tabs.activeWindow == mainWindow)
                {
                    if(mainWindow?.GetActiveNestedWindow() != null)
                    {
                        if(enabled)
                        {
                            tittyMagic.colliderVisualizer.enabled = true;
                            colliderVisualizer.ShowPreviewsJSON.val = true;
                        }
                        else
                        {
                            ((WindowBase) mainWindow).onReturnToParent();
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

        private JSONStorableFloat _scaleJsf;
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

            /* Setup atom scale changed callback */
            {
                _scaleJsf = containingAtom.GetStorableByID("rescaleObject").GetFloatJSONParam("scale");
                _scaleJsf.setJSONCallbackFunction = _ =>
                {
                    if(!enabled || !isInitialized || calibration.isWaiting ||
                        containingAtom.grabFreezePhysics && containingAtom.mainController.isGrabbing)
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

            skin = containingAtom.GetComponentInChildren<DAZCharacter>().skin;

            /* Setup handlers */
            MainPhysicsHandler.Init(breastControl, chestRb);
            hardColliderHandler = gameObject.AddComponent<HardColliderHandler>();
            hardColliderHandler.Init(geometry, chestRb);
            SoftPhysicsHandler.Init();
            GravityPhysicsHandler.Init();
            GravityOffsetMorphHandler.Init();
            NippleErectionHandler.Init();
            FrictionHandler.Init(containingAtom.GetStorableByID("skin"));

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
                    VertexIndexGroup.LEFT_BREAST_CENTER.Select(i => skin.rawSkinnedWorkingVerts[i]).ToArray()
                );
                _trackRightNipple.getNipplePosition = () => Calc.AveragePosition(
                    VertexIndexGroup.RIGHT_BREAST_CENTER.Select(i => skin.rawSkinnedWorkingVerts[i]).ToArray()
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
                FrictionHandler.LoadSettings();
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

            NavigateToMainWindow();

            /* Subscribe to keybindings */
            SuperController.singleton.BroadcastMessage("OnActionsProviderAvailable", this, SendMessageOptions.DontRequireReceiver);

            /* Finish init */
            Integration.Init();
            calibration = gameObject.AddComponent<CalibrationHelper>();
            calibration.Init();

            if(!_isLoadingFromJson)
            {
                /* Modify atom UI when not loading from JSON */
                {
                    /* Plugin added manually */
                    var atomUIContent = containingAtom.transform.Find("UI/UIPlaceHolderModel/UIModel/Canvas/Panel/Content");
                    if(atomUIContent.gameObject.activeInHierarchy)
                    {
                        StartCoroutine(ModifyBreastPhysicsUI(atomUIContent));
                        StartCoroutine(ModifySkinMaterialsUI(atomUIContent));
                    }
                    /* Plugin added with trigger or programmatically without the person UI being open */
                    else
                    {
                        _atomUIEventsListener = atomUIContent.gameObject.AddComponent<UnityEventsListener>();
                        _atomUIEventsListener.onEnable.AddListener(() =>
                        {
                            StartCoroutine(ModifyBreastPhysicsUI(atomUIContent));
                            StartCoroutine(ModifySkinMaterialsUI(atomUIContent));
                        });
                    }
                }

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

        private UnityEventsListener _atomUIEventsListener;
        private List<GameObject> _customUIGameObjects;
        private List<GameObject> _inactivatedUIGameObjects;
        private bool _modifyBreastPhysicsUIDone;

        private IEnumerator ModifyBreastPhysicsUI(Transform content)
        {
            // Allow modifying UI only once
            if(_modifyBreastPhysicsUIDone)
            {
                yield break;
            }

            _modifyBreastPhysicsUIDone = true;

            Transform mPectoralPhysics = null;
            Transform fBreastPhysics1 = null;
            Transform fBreastPhysics2 = null;
            Transform fBreastPresets = null;

            float waited = 0f;
            while(waited < 1 && (
                mPectoralPhysics == null ||
                fBreastPhysics1 == null ||
                fBreastPhysics2 == null ||
                fBreastPresets == null
            ))
            {
                waited += 0.1f;
                yield return new WaitForSecondsRealtime(0.1f);
                mPectoralPhysics = content.Find("M Pectoral Physics");
                fBreastPhysics1 = content.Find("F Breast Physics 1");
                fBreastPhysics2 = content.Find("F Breast Physics 2");
                fBreastPresets = content.Find("F Breast Presets");
            }

            if(mPectoralPhysics == null || fBreastPhysics1 == null || fBreastPhysics2 == null || fBreastPresets == null)
            {
                Debug.Log("Failed to modify breast physics UI - could not find UI transforms.");
                _modifyBreastPhysicsUIDone = true;
                yield break;
            }

            _customUIGameObjects = new List<GameObject>();

            /* Hide elements in vanilla Breast Physics tabs, add buttons to navigate to plugin UI */
            try
            {
                _inactivatedUIGameObjects = new List<GameObject>();
                Inactivate(mPectoralPhysics);
                Inactivate(fBreastPhysics1);
                Inactivate(fBreastPhysics2);
                Inactivate(fBreastPresets);

                _customUIGameObjects.Add(OpenPluginUIButton(mPectoralPhysics).gameObject);
                _customUIGameObjects.Add(OpenPluginUIButton(fBreastPhysics1).gameObject);
                _customUIGameObjects.Add(OpenPluginUIButton(fBreastPhysics2).gameObject);
                _customUIGameObjects.Add(OpenPluginUIButton(fBreastPresets).gameObject);

                _modifyBreastPhysicsUIDone = true;
            }
            catch(Exception e)
            {
                Utils.LogError($"Error modifying breast physics UI: {e}");
            }
        }

        private bool _modifySkinMaterialsUIDone;
        public UIDynamicToggle enableAdaptiveFrictionToggle { get; private set; }

        private IEnumerator ModifySkinMaterialsUI(Transform content)
        {
            if(_modifySkinMaterialsUIDone)
            {
                yield break;
            }

            _modifySkinMaterialsUIDone = true;

            Transform skinMaterials2 = null;
            float waited = 0f;
            while(waited < 1 && skinMaterials2 == null)
            {
                waited += 0.1f;
                yield return new WaitForSecondsRealtime(0.1f);
                skinMaterials2 = content.Find("Skin Materials 2");
            }

            if(skinMaterials2 == null)
            {
                Debug.Log("Failed to modify Skin Materials UI - could not find UI transform.");
                _modifySkinMaterialsUIDone = true;
                yield break;
            }

            try
            {
                var leftSide = skinMaterials2.Find("LeftSide");
                {
                    var customTransform = UIHelpers.DestroyLayout(this.InstantiateToggle(leftSide));
                    var rectTransform = customTransform.GetComponent<RectTransform>();
                    rectTransform.pivot = new Vector2(0, 0);
                    rectTransform.anchoredPosition = new Vector2(20f, -780f);
                    var sizeDelta = rectTransform.sizeDelta;
                    rectTransform.sizeDelta = new Vector2(sizeDelta.x + 10, sizeDelta.y);
                    _customUIGameObjects.Add(customTransform.gameObject);

                    enableAdaptiveFrictionToggle = customTransform.GetComponent<UIDynamicToggle>();
                    var jsf = FrictionHandler.enableAdaptiveFriction;
                    jsf.toggle = enableAdaptiveFrictionToggle.toggle;
                    toggleToJSONStorableBool.Add(enableAdaptiveFrictionToggle, jsf);
                    enableAdaptiveFrictionToggle.label = "TittyMagic Adaptive Friction";
                    enableAdaptiveFrictionToggle.textColor = jsf.val ? UIHelpers.funkyCyan : Color.white;
                    enableAdaptiveFrictionToggle.backgroundColor = UIHelpers.darkerGray;
                }
                {
                    var customTransform = UIHelpers.DestroyLayout(this.InstantiateSlider(leftSide));
                    var rectTransform = customTransform.GetComponent<RectTransform>();
                    rectTransform.pivot = new Vector2(0, 0);
                    rectTransform.anchoredPosition = new Vector2(20f, -920f);
                    var sizeDelta = rectTransform.sizeDelta;
                    rectTransform.sizeDelta = new Vector2(sizeDelta.x + 10, sizeDelta.y);
                    _customUIGameObjects.Add(customTransform.gameObject);

                    var uiDynamic = customTransform.GetComponent<UIDynamicSlider>();
                    var jsf = FrictionHandler.drySkinFriction;
                    uiDynamic.Configure(jsf.name, jsf.min, jsf.max, jsf.defaultVal, jsf.constrained, valFormat: "F3");
                    jsf.slider = uiDynamic.slider;
                    sliderToJSONStorableFloat.Add(uiDynamic, jsf);
                    uiDynamic.label = "Dry Skin Friction";
                }
                {
                    var customTransform = UIHelpers.DestroyLayout(this.InstantiateSlider(leftSide));
                    var rectTransform = customTransform.GetComponent<RectTransform>();
                    rectTransform.pivot = new Vector2(0, 0);
                    rectTransform.anchoredPosition = new Vector2(20f, -1060f);
                    var sizeDelta = rectTransform.sizeDelta;
                    rectTransform.sizeDelta = new Vector2(sizeDelta.x + 10, sizeDelta.y);
                    _customUIGameObjects.Add(customTransform.gameObject);

                    var uiDynamic = customTransform.GetComponent<UIDynamicSlider>();
                    var jsf = FrictionHandler.softColliderFriction;
                    uiDynamic.Configure(jsf.name, jsf.min, jsf.max, jsf.defaultVal, jsf.constrained, valFormat: "F3");
                    jsf.slider = uiDynamic.slider;
                    sliderToJSONStorableFloat.Add(uiDynamic, jsf);
                    uiDynamic.label = "Collider Friction";
                    uiDynamic.SetActiveStyle(false, true);
                }
                {
                    var fieldTransform = UIHelpers.DestroyLayout(this.InstantiateTextField(leftSide));
                    var rectTransform = fieldTransform.GetComponent<RectTransform>();
                    rectTransform.pivot = new Vector2(0, 0);
                    rectTransform.anchoredPosition = new Vector2(20f, -1300f);
                    rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x + 10, 220f);
                    _customUIGameObjects.Add(fieldTransform.gameObject);

                    var uiDynamic = fieldTransform.GetComponent<UIDynamicTextField>();
                    uiDynamic.text =
                        "Friction decreases with <i>Gloss</i> but increases with <i>Specular Bumpiness</i> " +
                        "when <i>Gloss</i> is high. Dry skin friction represents the value when both " +
                        "<i>Gloss</i> and <i>Specular Bumpiness</i> are zero. Other sliders are ignored.";
                    uiDynamic.backgroundColor = Color.clear;
                    uiDynamic.textColor = Color.white;
                }
            }
            catch(Exception e)
            {
                Utils.LogError($"Error modifying Skin Materials UI: {e}");
            }
        }

        private void Inactivate(Transform t)
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

        private Transform OpenPluginUIButton(Transform parent)
        {
            var button = UIHelpers.DestroyLayout(this.InstantiateButton(parent));
            button.GetComponent<UIDynamicButton>().label = "<b>Open TittyMagic UI</b>";
            button.GetComponent<UIDynamicButton>().button.onClick.AddListener(() => _customBindings.OpenUI());
            return button;
        }

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
            try
            {
            }
            catch(Exception e)
            {
                Utils.LogError($"Update: {e}");
                enabled = false;
            }
        }

        private void UpdateDynamicHandlers(float roll, float pitch)
        {
            hardColliderHandler.UpdateFriction();
            ForcePhysicsHandler.Update();
            GravityPhysicsHandler.Update(roll, pitch);
            ForceMorphHandler.Update(roll, pitch);
            GravityOffsetMorphHandler.Update(roll, pitch);
        }

        private void FixedUpdate()
        {
            try
            {
                bool isFreezeGrabbing = containingAtom.grabFreezePhysics && containingAtom.mainController.isGrabbing;
                if(!isInitialized || _isLoadingFromJson || calibration.isWaiting || isFreezeGrabbing)
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
                hardColliderHandler.UpdateDistanceDiffs();

                var rotation = _chestTransform.rotation;
                float roll = Calc.Roll(rotation);
                float pitch = Calc.Pitch(rotation);

                UpdateDynamicHandlers(roll, pitch);

                if(envIsDevelopment)
                {
                    (mainWindow.GetActiveNestedWindow() as DevWindow)?.UpdateLeftDebugInfo();
                }
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

            if(!enabled)
            {
                Utils.LogMessage("Enable the plugin to recalibrate.");
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

            /* Dynamic adjustments to zero (simulate static upright pose), update physics */
            {
                _trackLeftNipple.ResetAnglesAndDepthDiff();
                _trackRightNipple.ResetAnglesAndDepthDiff();
                hardColliderHandler.ResetDistanceDiffs();
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
            FrictionHandler.CalculateFriction();

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

            hardColliderHandler.UpdateFrictionSizeMultipliers();

            /* Calibrate nipples tracking and colliders */
            {
                _isSimulatingUprightPose = true;
                StartCoroutine(SimulateUprightPose());
                Action calibrateNipplesAndColliders = () =>
                {
                    _trackLeftNipple.Calibrate();
                    _trackRightNipple.Calibrate();
                    hardColliderHandler.CalibrateColliders();
                    hardColliderHandler.SyncAllOffsets();
                };
                yield return calibration.WaitAndRepeat(calibrateNipplesAndColliders, 24, 0.05f);
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

            /* Add listener to person UI content for modifying the UI. When loading
             * from JSON, the person UI isn't open when the plugin is loaded.
             */
            var atomUIContent = containingAtom.transform.Find("UI/UIPlaceHolderModel/UIModel/Canvas/Panel/Content");
            _atomUIEventsListener = atomUIContent.gameObject.AddComponent<UnityEventsListener>();
            _atomUIEventsListener.onEnable.AddListener(() =>
            {
                StartCoroutine(ModifyBreastPhysicsUI(atomUIContent));
                StartCoroutine(ModifySkinMaterialsUI(atomUIContent));
            });

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
                FrictionHandler.RemoveCallbacks();
                _scaleJsf.setJSONCallbackFunction = null;
                mainWindow.GetSliders().ForEach(slider => Destroy(slider.GetPointerUpDownListener()));
                morphingWindow.GetSliders().ForEach(slider => Destroy(slider.GetPointerUpDownListener()));
                gravityWindow.GetSliders().ForEach(slider => Destroy(slider.GetPointerUpDownListener()));
                DestroyImmediate(_pluginUIEventsListener);
                DestroyImmediate(_atomUIEventsListener);
                _customUIGameObjects?.ForEach(Destroy);
                SuperController.singleton.BroadcastMessage("OnActionsProviderDestroyed", this, SendMessageOptions.DontRequireReceiver);
                Integration.RemoveHandlers();
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

                settingsMonitor.SetEnabled(true);
                hardColliderHandler.SetEnabled(true);
                SoftPhysicsHandler.SaveOriginalBoolParamValues();
                SoftPhysicsHandler.EnableMultiplyFriction();
                StartCalibration(true);
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
                MainPhysicsHandler.RestoreOriginalPhysics();
                SoftPhysicsHandler.RestoreOriginalPhysics();
                GravityOffsetMorphHandler.ResetAll();
                ForceMorphHandler.ResetAll();
                NippleErectionHandler.Reset();
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
