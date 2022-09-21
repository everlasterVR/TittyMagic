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
        public const string VERSION = "0.0.0";
        public static bool envIsDevelopment => VERSION.StartsWith("0.");
        public static bool personIsFemale { get; private set; }

        public static GenerateDAZMorphsControlUI morphsControlUI { get; private set; }
        public static DAZCharacterSelector geometry { get; private set; }
        public static DAZSkinV2 skin { get; set; }

        public float softnessAmount { get; private set; }
        public float quicknessAmount { get; private set; }

        public SettingsMonitor settingsMonitor { get; private set; }
        public Bindings bindings { get; private set; }

        private IWindow _mainWindow;
        private IWindow _morphingWindow;
        private IWindow _gravityWindow;
        private IWindow _physicsWindow;
        public Tabs tabs { get; private set; }

        public JSONStorableAction recalibratePhysics { get; private set; }
        public JSONStorableAction calculateBreastMass { get; private set; }
        public JSONStorableBool autoUpdateJsb { get; private set; }
        public JSONStorableFloat softnessJsf { get; private set; }
        public JSONStorableFloat quicknessJsf { get; private set; }

        public CalibrationHelper calibration { get; private set; }

        #region InitUI

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
                if(enabled && calibration.shouldRun)
                {
                    var activeParameterWindow = tabs.activeWindow?.GetActiveNestedWindow() as ParameterWindow;
                    if(activeParameterWindow != null)
                    {
                        activeParameterWindow.recalibrationAction.actionCallback();
                    }
                    else
                    {
                        recalibratePhysics.actionCallback();
                    }
                }

                if(HardColliderHandler.colliderVisualizer != null)
                {
                    HardColliderHandler.colliderVisualizer.ShowPreviewsJSON.val = false;
                    HardColliderHandler.colliderVisualizer.enabled = false;
                }

                if(SoftPhysicsHandler.colliderVisualizer != null)
                {
                    SoftPhysicsHandler.colliderVisualizer.ShowPreviewsJSON.val = false;
                    SoftPhysicsHandler.colliderVisualizer.enabled = false;
                }

                _mainWindow.GetActiveNestedWindow()?.ClosePopups();
            });

            _pluginUIEventsListener.onEnable.AddListener(() =>
            {
                var background = rightUIContent.parent.parent.parent.transform.GetComponent<Image>();
                background.color = UIHelpers.backgroundGray;

                SoftPhysicsHandler.ReverseSyncAllowSelfCollision();

                if(tabs.activeWindow == _mainWindow)
                {
                    if(_mainWindow?.GetActiveNestedWindow() != null)
                    {
                        if(enabled && HardColliderHandler.colliderVisualizer != null)
                        {
                            HardColliderHandler.colliderVisualizer.enabled = true;
                            HardColliderHandler.colliderVisualizer.ShowPreviewsJSON.val = true;
                        }
                        else
                        {
                            /* Prevent displaying hard collider window if plugin disabled */
                            ((WindowBase) _mainWindow).onReturnToParent();
                        }
                    }
                }
                else if(tabs.activeWindow == _physicsWindow)
                {
                    var parameterWindow = _physicsWindow.GetActiveNestedWindow() as ParameterWindow;
                    if(parameterWindow != null)
                    {
                        if(enabled && SoftPhysicsHandler.colliderVisualizer != null)
                        {
                            SoftPhysicsHandler.colliderVisualizer.enabled = true;
                            SoftPhysicsHandler.colliderVisualizer.ShowPreviewsJSON.val = true;
                        }

                        parameterWindow.SyncAllMultiplierSliderValues();
                    }
                }
            });
        }

        #endregion InitUI

        #region Init

        // ReSharper disable once MemberCanBePrivate.Global
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

        private FrequencyRunner _listenersCheckRunner;

        private JSONStorableFloat _scaleJsf;
        private List<Rigidbody> _rigidbodies;
        private Transform _chestTransform;
        private Rigidbody _pectoralRbLeft;
        private Rigidbody _pectoralRbRight;
        private TrackNipple _trackLeftNipple;
        private TrackNipple _trackRightNipple;

        private IEnumerator DeferInit()
        {
            yield return new WaitForEndOfFrame();
            while(SuperController.singleton.isLoading)
            {
                yield return null;
            }

            /* Wait for plugin permissions to be accepted */
            var confirmPanel = SuperController.singleton.errorLogPanel.parent.Find("UserConfirmCanvas");
            while(confirmPanel != null && confirmPanel.childCount > 0)
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

            /* Wait for geometry and skin to be ready */
            {
                float timeout = Time.unscaledTime + 10;
                bool ready = false;
                while(Time.unscaledTime < timeout && !ready)
                {
                    geometry = (DAZCharacterSelector) containingAtom.GetStorableByID("geometry");
                    ready = geometry.selectedCharacter.ready && containingAtom.GetStorableByID("skin") != null;
                    yield return new WaitForSecondsRealtime(0.1f);
                }

                if(!geometry.selectedCharacter.ready)
                {
                    Utils.LogError(
                        $"Selected character {geometry.selectedCharacter.name} was not ready after 10 seconds of waiting. " +
                        "Aborting plugin initization. Try reloading, and please report an issue."
                    );
                    yield break;
                }

                if(containingAtom.GetStorableByID("skin") == null)
                {
                    Utils.LogError(
                        "Person skin materials not found after 2 seconds of waiting. " +
                        "Aborting plugin initization. Try reloading, and please report an issue."
                    );
                    yield break;
                }
            }

            personIsFemale = !geometry.selectedCharacter.isMale;

            _listenersCheckRunner = new FrequencyRunner(0.333f);

            morphsControlUI = personIsFemale ? geometry.morphsControlUI : geometry.morphsControlUIOtherGender;
            skin = containingAtom.GetComponentInChildren<DAZCharacter>().skin;
            _rigidbodies = containingAtom.GetComponentsInChildren<Rigidbody>().ToList();
            MainPhysicsHandler.chestRb = _rigidbodies.Find(rb => rb.name == "chest");
            _chestTransform = MainPhysicsHandler.chestRb.transform;

            MainPhysicsHandler.breastControl = (AdjustJoints) containingAtom.GetStorableByID(personIsFemale ? "BreastControl" : "PectoralControl");
            _pectoralRbLeft = MainPhysicsHandler.breastControl.joint2.GetComponent<Rigidbody>();
            _pectoralRbRight = MainPhysicsHandler.breastControl.joint1.GetComponent<Rigidbody>();

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

            /* Advanced colliders must be enabled for collider visualizer, force morphing and hard collider handler */
            HardColliderHandler.EnableAdvColliders();

            /* Setup handlers */
            MainPhysicsHandler.Init();
            HardColliderHandler.Init();
            SoftPhysicsHandler.Init();
            GravityPhysicsHandler.Init();
            GravityOffsetMorphHandler.Init();
            NippleErectionHandler.Init();
            FrictionHandler.Init(containingAtom.GetStorableByID("skin"));

            settingsMonitor = gameObject.AddComponent<SettingsMonitor>();
            settingsMonitor.Init();

            /* Setup nipples tracking */
            _trackLeftNipple = new TrackNipple(_pectoralRbLeft);
            _trackRightNipple = new TrackNipple(_pectoralRbRight);

            if(personIsFemale)
            {
                yield return DeferSetupTrackFemaleNipples();
            }
            else
            {
                _trackLeftNipple.getNipplePosition = () => Calc.AveragePosition(
                    VertexIndexGroup.leftBreastCenter.Select(i => skin.rawSkinnedWorkingVerts[i]).ToArray()
                );
                _trackRightNipple.getNipplePosition = () => Calc.AveragePosition(
                    VertexIndexGroup.rightBreastCenter.Select(i => skin.rawSkinnedWorkingVerts[i]).ToArray()
                );
            }

            ForcePhysicsHandler.Init(_trackLeftNipple, _trackRightNipple);
            ForceMorphHandler.Init(_trackLeftNipple, _trackRightNipple);

            /* Setup breast morph listening */
            BreastMorphListener.ProcessMorphs(geometry.morphBank1);
            if(!personIsFemale)
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

            /* Create custom bindings and subscribe to Keybindings.
             * Custom bindings actions are used in-plugin as well, and might already be setup in OnBindingsListRequested.
             */
            if(bindings == null)
            {
                bindings = gameObject.AddComponent<Bindings>();
                bindings.Init();
            }

            SuperController.singleton.BroadcastMessage("OnActionsProviderAvailable", this, SendMessageOptions.DontRequireReceiver);
            SuperController.singleton.onBeforeSceneSaveHandlers += OnBeforeSceneSave;
            SuperController.singleton.onSceneSavedHandlers += OnSceneSaved;

            /* Setup navigation */
            {
                _mainWindow = new MainWindow();
                _morphingWindow = new MorphingWindow();
                _gravityWindow = new GravityWindow();
                _physicsWindow = new PhysicsWindow();

                tabs = new Tabs(leftUIContent, rightUIContent);
                tabs.CreateNavigationButton(_mainWindow, "Control", NavigateToMainWindow);
                tabs.CreateNavigationButton(_physicsWindow, "Physics Params", NavigateToPhysicsWindow);
                tabs.CreateNavigationButton(_morphingWindow, "Morph Multipliers", NavigateToMorphingWindow);
                tabs.CreateNavigationButton(_gravityWindow, "Gravity Multipliers", NavigateToGravityWindow);
            }

            NavigateToMainWindow();

            /* Finish init */
            Integration.Init();
            calibration = gameObject.AddComponent<CalibrationHelper>();
            calibration.Init();
            settingsMonitor.SetPectoralCollisions(!personIsFemale);

            ModifyAtomUI();

            if(!_isRestoringFromJson)
            {
                HardColliderHandler.SaveOriginalUseColliders();
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
        public void OnBindingsListRequested(List<object> bindingsList)
        {
            /* Might already be setup in Init. */
            if(bindings == null)
            {
                bindings = gameObject.AddComponent<Bindings>();
                bindings.Init();
            }

            bindingsList.Add(bindings.Namespace());
            bindingsList.AddRange(bindings.Actions());
        }

        private UnityEventsListener _atomUIEventsListener;

        private void ModifyAtomUI()
        {
            /* Plugin added manually.*/
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

        private readonly List<GameObject> _customUIGameObjects = new List<GameObject>();
        private readonly List<GameObject> _inactivatedUIGameObjects = new List<GameObject>();
        private bool _modifyBreastPhysicsUIDone;

        private IEnumerator ModifyBreastPhysicsUI(Transform content)
        {
            // Allow modifying UI only once
            if(_modifyBreastPhysicsUIDone)
            {
                yield break;
            }

            while(bindings == null)
            {
                yield return null;
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

            /* Hide elements in vanilla Breast Physics tabs, add buttons to navigate to plugin UI */
            try
            {
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

        private Transform OpenPluginUIButton(Transform parent)
        {
            var button = UIHelpers.DestroyLayout(this.InstantiateButton(parent));
            button.GetComponent<UIDynamicButton>().label = $"<b>Open {nameof(TittyMagic)} UI</b>";
            button.GetComponent<UIDynamicButton>().button.onClick.AddListener(() => bindings.actions["OpenUI"].actionCallback());
            return button;
        }

        private bool _modifySkinMaterialsUIDone;
        private readonly List<RectTransformChange> _movedRects = new List<RectTransformChange>();
        private UIDynamicToggle enableAdaptiveFrictionToggle { get; set; }
        public UIDynamicSlider drySkinFrictionSlider { get; private set; }

        private IEnumerator ModifySkinMaterialsUI(Transform content)
        {
            if(_modifySkinMaterialsUIDone)
            {
                yield break;
            }

            _modifySkinMaterialsUIDone = true;

            if(!personIsFemale)
            {
                yield break;
            }

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
                /* Left side */
                var leftSide = skinMaterials2.Find("LeftSide");
                {
                    var fieldTransform = UIHelpers.DestroyLayout(this.InstantiateTextField(leftSide));
                    var rectTransform = fieldTransform.GetComponent<RectTransform>();
                    rectTransform.pivot = new Vector2(0, 0);
                    rectTransform.anchoredPosition = new Vector2(20f, -930);
                    rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x + 10, 100);
                    _customUIGameObjects.Add(fieldTransform.gameObject);

                    var uiDynamic = fieldTransform.GetComponent<UIDynamicTextField>();
                    uiDynamic.UItext.alignment = TextAnchor.LowerCenter;
                    uiDynamic.text = $"{nameof(TittyMagic)} Collider Friction".Size(32).Bold();
                    uiDynamic.backgroundColor = Color.clear;
                    uiDynamic.textColor = Color.white;
                }
                {
                    var fieldTransform = UIHelpers.DestroyLayout(this.InstantiateTextField(leftSide));
                    var rectTransform = fieldTransform.GetComponent<RectTransform>();
                    rectTransform.pivot = new Vector2(0, 0);
                    rectTransform.anchoredPosition = new Vector2(20, -1290);
                    rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x + 10, 400);
                    _customUIGameObjects.Add(fieldTransform.gameObject);

                    var uiDynamic = fieldTransform.GetComponent<UIDynamicTextField>();
                    uiDynamic.text =
                        "Combined friction for hard colliders and soft colliders." +
                        "\n\n" +
                        "Adaptive friction reduces friction when <i>Gloss</i> is increased. The higher " +
                        "the gloss, the more <i>Specular Bumpiness</i> adds friction. Other skin material " +
                        "sliders are ignored." +
                        "\n\n" +
                        "Dry skin friction represents the value when both <i>Gloss</i> and <i>Specular Bumpiness</i> " +
                        "are zero.";
                    uiDynamic.backgroundColor = Color.clear;
                    uiDynamic.textColor = Color.white;
                }

                /* Right side */
                var rightSide = skinMaterials2.Find("RightSide");
                foreach(Transform t in rightSide)
                {
                    if(t.name == "SavePanel" || t.name == "RestorePanel" || t.name == "Reset")
                    {
                        var change = new RectTransformChange(t.GetComponent<RectTransform>(), new Vector2(0, 600f));
                        _movedRects.Add(change);
                    }
                }

                {
                    var customTransform = UIHelpers.DestroyLayout(this.InstantiateToggle(rightSide));
                    var rectTransform = customTransform.GetComponent<RectTransform>();
                    rectTransform.pivot = new Vector2(0, 0);
                    rectTransform.anchoredPosition = new Vector2(0, -880);
                    var sizeDelta = rectTransform.sizeDelta;
                    rectTransform.sizeDelta = new Vector2(sizeDelta.x + 10, sizeDelta.y);
                    _customUIGameObjects.Add(customTransform.gameObject);

                    enableAdaptiveFrictionToggle = customTransform.GetComponent<UIDynamicToggle>();
                    var jsf = FrictionHandler.adaptiveFrictionJsb;
                    jsf.toggle = enableAdaptiveFrictionToggle.toggle;
                    toggleToJSONStorableBool.Add(enableAdaptiveFrictionToggle, jsf);
                    enableAdaptiveFrictionToggle.label = "Use Adaptive Friction";
                }
                {
                    var customTransform = UIHelpers.DestroyLayout(this.InstantiateSlider(rightSide));
                    var rectTransform = customTransform.GetComponent<RectTransform>();
                    rectTransform.pivot = new Vector2(0, 0);
                    rectTransform.anchoredPosition = new Vector2(0, -1020);
                    var sizeDelta = rectTransform.sizeDelta;
                    rectTransform.sizeDelta = new Vector2(sizeDelta.x + 10, sizeDelta.y);
                    _customUIGameObjects.Add(customTransform.gameObject);

                    drySkinFrictionSlider = customTransform.GetComponent<UIDynamicSlider>();
                    var jsf = FrictionHandler.drySkinFrictionJsf;
                    drySkinFrictionSlider.Configure(jsf.name, jsf.min, jsf.max, jsf.defaultVal, jsf.constrained, valFormat: "F3");
                    jsf.slider = drySkinFrictionSlider.slider;
                    sliderToJSONStorableFloat.Add(drySkinFrictionSlider, jsf);
                    drySkinFrictionSlider.label = "Dry Skin Friction";
                    drySkinFrictionSlider.SetActiveStyle(FrictionHandler.adaptiveFrictionJsb.val, true);
                }
                {
                    var customTransform = UIHelpers.DestroyLayout(this.InstantiateSlider(rightSide));
                    var rectTransform = customTransform.GetComponent<RectTransform>();
                    rectTransform.pivot = new Vector2(0, 0);
                    rectTransform.anchoredPosition = new Vector2(0, -1160);
                    var sizeDelta = rectTransform.sizeDelta;
                    rectTransform.sizeDelta = new Vector2(sizeDelta.x + 10, sizeDelta.y);
                    _customUIGameObjects.Add(customTransform.gameObject);

                    var uiDynamic = customTransform.GetComponent<UIDynamicSlider>();
                    var jsf = FrictionHandler.frictionOffsetJsf;
                    uiDynamic.Configure(jsf.name, jsf.min, jsf.max, jsf.defaultVal, jsf.constrained, valFormat: "F3");
                    jsf.slider = uiDynamic.slider;
                    sliderToJSONStorableFloat.Add(uiDynamic, jsf);
                    uiDynamic.label = "Friction Offset";
                }
                {
                    var customTransform = UIHelpers.DestroyLayout(this.InstantiateSlider(rightSide));
                    var rectTransform = customTransform.GetComponent<RectTransform>();
                    rectTransform.pivot = new Vector2(0, 0);
                    rectTransform.anchoredPosition = new Vector2(0, -1300);
                    var sizeDelta = rectTransform.sizeDelta;
                    rectTransform.sizeDelta = new Vector2(sizeDelta.x + 10, sizeDelta.y);
                    _customUIGameObjects.Add(customTransform.gameObject);

                    var uiDynamic = customTransform.GetComponent<UIDynamicSlider>();
                    var jsf = FrictionHandler.softColliderFrictionJsf;
                    uiDynamic.Configure(jsf.name, jsf.min, jsf.max, jsf.defaultVal, jsf.constrained, valFormat: "F3");
                    jsf.slider = uiDynamic.slider;
                    sliderToJSONStorableFloat.Add(uiDynamic, jsf);
                    uiDynamic.label = "Friction Value";
                    uiDynamic.SetActiveStyle(false, true);
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

        #endregion Init

        public void NavigateToMainWindow() => NavigateToWindow(_mainWindow);
        public void NavigateToPhysicsWindow() => NavigateToWindow(_physicsWindow);
        public void NavigateToMorphingWindow() => NavigateToWindow(_morphingWindow);
        public void NavigateToGravityWindow() => NavigateToWindow(_gravityWindow);

        private void NavigateToWindow(IWindow window)
        {
            tabs.activeWindow?.Clear();
            tabs.ActivateTab(window);
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

        private static void UpdateDynamicHandlers(float roll, float pitch)
        {
            HardColliderHandler.UpdateFriction();
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
                if(!isInitialized || _isRestoringFromJson || calibration.isWaiting || isFreezeGrabbing || _isSavingScene)
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
                HardColliderHandler.UpdateDistanceDiffs();

                var rotation = _chestTransform.rotation;
                float roll = Calc.Roll(rotation);
                float pitch = Calc.Pitch(rotation);

                UpdateDynamicHandlers(roll, pitch);

                if(envIsDevelopment)
                {
                    (_mainWindow.GetActiveNestedWindow() as DevWindow)?.UpdateLeftDebugInfo();
                }
            }
            catch(Exception e)
            {
                Utils.LogError($"FixedUpdate: {e}");
                enabled = false;
            }
        }

        #endregion Update

        #region Calibration

        public void StartCalibration(bool calibratesMass, bool waitsForListeners = false)
        {
            if(_isRestoringFromJson)
            {
                return;
            }

            if(calibration.isInProgress && calibration.IsBlockedByInput())
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

            /* This can occur when loading a look and VaM pauses to load assets */
            while(!geometry.selectedCharacter.ready)
            {
                yield return null;
            }

            yield return calibration.DeferFreezeAnimation();

            /* Dynamic adjustments to zero (simulate static upright pose), update physics */
            {
                _trackLeftNipple.ResetAnglesAndDepthDiff();
                _trackRightNipple.ResetAnglesAndDepthDiff();
                HardColliderHandler.ResetDistanceDiffs();
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

            /* Calculate mass when gravity is off and collision is disabled to get a consistent result.
             * Mass is calculated multiple times because each new mass value changes the exact breast
             * shape and therefore the estimated volume.
             */
            var guid = Guid.NewGuid();
            calibration.SetBreastsCollisionEnabled(false, guid);
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

            HardColliderHandler.UpdateFrictionSizeMultipliers();

            /* Calibrate nipples tracking and colliders */
            {
                _isSimulatingUprightPose = true;
                StartCoroutine(SimulateUprightPose());
                Action calibrateNipplesAndColliders = () =>
                {
                    _trackLeftNipple.Calibrate();
                    _trackRightNipple.Calibrate();
                    HardColliderHandler.CalibrateColliders();
                    HardColliderHandler.SyncAllOffsets();
                };
                yield return calibration.WaitAndRepeat(calibrateNipplesAndColliders, 24, 0.05f);
                HardColliderHandler.SyncCollidersMass();
                HardColliderHandler.SyncAllOffsets();
                _isSimulatingUprightPose = false;
            }

            SetBreastsUseGravity(true);
            calibration.SetBreastsCollisionEnabled(true, guid);
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

        #endregion Calibration

        public string PluginPath() =>
            $@"{this.GetPackagePath()}Custom\Scripts\everlaster\TittyMagic";

        public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
        {
            var jsonClass = base.GetJSON(includePhysical, includeAppearance, forceStore);
            jsonClass.Remove(CalibrationHelper.CALIBRATION_LOCK);
            needsStore = true;
            return jsonClass;
        }

        private bool _isRestoringFromJson;

        public override void RestoreFromJSON(
            JSONClass jsonClass,
            bool restorePhysical = true,
            bool restoreAppearance = true,
            JSONArray presetAtoms = null,
            bool setMissingToDefault = true
        )
        {
            _isRestoringFromJson = true;
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
            _isRestoringFromJson = false;
            HardColliderHandler.SaveOriginalUseColliders();
            SoftPhysicsHandler.SaveOriginalBoolParamValues();
            calculateBreastMass.actionCallback();
        }

        private bool _isSavingScene;

        private void OnBeforeSceneSave()
        {
            _isSavingScene = true;
            GravityOffsetMorphHandler.ResetAll();
            ForceMorphHandler.ResetAll();
            NippleErectionHandler.Reset();
        }

        private void OnSceneSaved()
        {
            _isSavingScene = false;
        }

        private void OnDestroy()
        {
            try
            {
                Destroy(calibration);
                Destroy(settingsMonitor);
                Destroy(bindings);

                /* Nullify static reference fields to let GC collect unreachable instances */
                CalibrationHelper.Destroy();
                ForceMorphHandler.Destroy();
                ForcePhysicsHandler.Destroy();
                FrictionHandler.Destroy();
                GravityOffsetMorphHandler.Destroy();
                GravityPhysicsHandler.Destroy();
                HardColliderHandler.Destroy();
                MainPhysicsHandler.Destroy();
                NippleErectionHandler.Destroy();
                SoftPhysicsHandler.Destroy();
                BreastMorphListener.Destroy();
                VertexIndexGroup.Destroy();
                Integration.Destroy();
                tittyMagic = null;
                morphsControlUI = null;
                geometry = null;
                skin = null;

                _scaleJsf.setJSONCallbackFunction = null;

                _mainWindow.GetSliders().ForEach(slider => Destroy(slider.GetPointerUpDownListener()));
                _morphingWindow.GetSliders().ForEach(slider => Destroy(slider.GetPointerUpDownListener()));
                _gravityWindow.GetSliders().ForEach(slider => Destroy(slider.GetPointerUpDownListener()));

                DestroyImmediate(_pluginUIEventsListener);
                DestroyImmediate(_atomUIEventsListener);

                _customUIGameObjects?.ForEach(Destroy);

                SuperController.singleton.onSceneSavedHandlers -= OnSceneSaved;
                SuperController.singleton.onBeforeSceneSaveHandlers -= OnBeforeSceneSave;
                SuperController.singleton.BroadcastMessage("OnActionsProviderDestroyed", this, SendMessageOptions.DontRequireReceiver);
            }
            catch(Exception e)
            {
                if(isInitialized)
                {
                    Utils.LogError($"OnDestroy: {e}");
                }
                else if(envIsDevelopment)
                {
                    Debug.Log($"OnDestroy: {e}");
                }
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
                HardColliderHandler.SaveOriginalUseColliders();
                HardColliderHandler.EnableAdvColliders();
                HardColliderHandler.EnableMultiplyFriction();
                SoftPhysicsHandler.SaveOriginalBoolParamValues();
                SoftPhysicsHandler.EnableMultiplyFriction();
                StartCalibration(true);
                _inactivatedUIGameObjects?.ForEach(go => go.SetActive(false));
                _customUIGameObjects?.ForEach(go => go.SetActive(true));
                _movedRects?.ForEach(change => change.Apply());
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
                settingsMonitor.SetPectoralCollisions(true);
                HardColliderHandler.RestoreOriginalPhysics();
                MainPhysicsHandler.RestoreOriginalPhysics();
                SoftPhysicsHandler.RestoreOriginalPhysics();
                GravityOffsetMorphHandler.ResetAll();
                ForceMorphHandler.ResetAll();
                NippleErectionHandler.Reset();
                _inactivatedUIGameObjects?.ForEach(go => go.SetActive(true));
                _customUIGameObjects?.ForEach(go => go.SetActive(false));
                _movedRects?.ForEach(change => change.RestoreOriginal());
            }
            catch(Exception e)
            {
                if(isInitialized)
                {
                    Utils.LogError($"OnDisable: {e}");
                }
                else if(envIsDevelopment)
                {
                    Debug.Log($"OnDisable: {e}");
                }
            }
        }
    }
}
