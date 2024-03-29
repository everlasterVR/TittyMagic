﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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

        public static DAZCharacterSelector geometry { get; private set; }
        public static DAZSkinV2 skin { get; private set; }

        public float softnessAmount { get; private set; }
        public float quicknessAmount { get; private set; }

        public SettingsMonitor settingsMonitor { get; private set; }
        public Bindings bindings { get; private set; }

        private IWindow _mainWindow;
        private IWindow _physicsWindow;
        private IWindow _morphingWindow;
        private IWindow _gravityWindow;
        public Tabs tabs { get; private set; }

        public JSONStorableAction calibrate { get; private set; }

        public JSONStorableAction calculateBreastMass { get; private set; }

        public JSONStorableFloat softnessJsf { get; private set; }
        public JSONStorableFloat quicknessJsf { get; private set; }
        private JSONStorableFloat _softnessNoCallbackJsf;
        private JSONStorableFloat _quicknessNoCallbackJsf;

        public CalibrationHelper calibrationHelper { get; private set; }

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
                if(enabled && calibrationHelper.shouldRun)
                {
                    var activeParameterWindow = tabs.activeWindow?.GetActiveNestedWindow() as ParameterWindow;
                    if(activeParameterWindow != null)
                    {
                        activeParameterWindow.calibrationAction.actionCallback();
                    }
                    else
                    {
                        calibrate.actionCallback();
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
                background.color = Colors.backgroundGray;

                SoftPhysicsHandler.ReverseSyncAllowSelfCollision();

                if(tabs.activeWindow == _mainWindow)
                {
                    var nestedWindow = _mainWindow?.GetActiveNestedWindow();
                    if(!enabled)
                    {
                        if(nestedWindow != null)
                        {
                            /* Prevent displaying nested window if plugin disabled */
                            _mainWindow.OnReturn();
                        }
                    }
                    else if(nestedWindow is HardCollidersWindow)
                    {
                        if(HardColliderHandler.colliderVisualizer != null)
                        {
                            HardColliderHandler.colliderVisualizer.enabled = true;
                            HardColliderHandler.colliderVisualizer.ShowPreviewsJSON.val = true;
                        }
                    }
                    // else if(nestedWindow is DevWindow)
                    // {
                    //     if(HardColliderHandler.colliderVisualizer != null)
                    //     {
                    //         HardColliderHandler.colliderVisualizer.enabled = true;
                    //         HardColliderHandler.colliderVisualizer.ShowPreviewsJSON.val = true;
                    //     }
                    // }
                }
                else if(tabs.activeWindow == _physicsWindow)
                {
                    ((PhysicsWindow) _physicsWindow).UpdateSoftPhysicsToggleStyle(settingsMonitor.softPhysicsEnabled);
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

        public bool initialized { get; private set; }

        public override void Init()
        {
            try
            {
                /* Used to store version in save JSON and communicate version to other plugin instances */
                var versionJss = this.NewJSONStorableString(Constant.VERSION, "");
                versionJss.val = $"{VERSION}";

                if(containingAtom.type != "Person")
                {
                    Utils.LogError($"Add to a Person atom, not {containingAtom.type}");
                    enabled = false;
                    return;
                }

                if(Utils.PluginIsDuplicate(containingAtom, storeId))
                {
                    Utils.LogError($"Person already has an instance of {nameof(TittyMagic)}.");
                    enabled = false;
                    return;
                }

                tittyMagic = this;
                StartCoroutine(DeferInit());
            }
            catch(Exception e)
            {
                enabled = false;
                Utils.LogError($"Init: {e}");
            }
        }

        private FrequencyRunner _listenersCheckRunner;
        private JSONStorableFloat _scaleJsfOrig;
        public JSONStorableFloat scaleJsf { get; private set; }
        private Transform _chestTransform;
        public Rigidbody pectoralRbLeft { get; private set; }
        public Rigidbody pectoralRbRight { get; private set; }
        public TrackBreast trackLeftBreast { get; private set; }
        public TrackBreast trackRightBreast { get; private set; }
        public UIModManager uiModManager { get; private set; }

        private IEnumerator DeferInit()
        {
            yield return new WaitForEndOfFrame();
            while(SuperController.singleton.isLoading)
            {
                yield return null;
            }

            /* Wait for other plugin permissions to be accepted */
            var confirmPanel = SuperController.singleton.errorLogPanel.parent.Find("UserConfirmCanvas");
            while(confirmPanel != null && confirmPanel.childCount > 0)
            {
                yield return null;
            }

            Utils.SetMorphPath(this.GetPackageId());

            const int timeout = 15;
            yield return WaitForGeometryAndSkinReady(timeout);
            if(!_characterReady)
            {
                Utils.LogError(
                    $"Selected character {geometry.selectedCharacter.name} was not ready after {timeout} seconds of waiting. " +
                    "Aborting plugin initization. Try reloading, and please report an issue."
                );
                yield break;
            }

            if(!_skinReady)
            {
                Utils.LogError(
                    $"Person skin materials not found after {timeout} seconds of waiting. " +
                    "Aborting plugin initization. Try reloading, and please report an issue."
                );
                yield break;
            }

            personIsFemale = !geometry.selectedCharacter.isMale;

            _listenersCheckRunner = new FrequencyRunner(0.333f);

            Utils.morphsControlUI = personIsFemale ? geometry.morphsControlUI : geometry.morphsControlUIOtherGender;
            skin = containingAtom.GetComponentInChildren<DAZCharacter>().skin;
            MainPhysicsHandler.chestRb = Utils.FindRigidbody(containingAtom, "chest");
            _chestTransform = MainPhysicsHandler.chestRb.transform;

            MainPhysicsHandler.breastControl = (AdjustJoints) containingAtom.GetStorableByID(personIsFemale ? "BreastControl" : "PectoralControl");
            pectoralRbLeft = MainPhysicsHandler.breastControl.joint2.GetComponent<Rigidbody>();
            pectoralRbRight = MainPhysicsHandler.breastControl.joint1.GetComponent<Rigidbody>();

            /* Setup atom scale changed callback via a JSONStorableFloat */
            {
                _scaleJsfOrig = containingAtom.GetStorableByID("rescaleObject").GetFloatJSONParam("scale");
                scaleJsf = new JSONStorableFloat("scaleCopy", _scaleJsfOrig.defaultVal, _scaleJsfOrig.min, _scaleJsfOrig.max);
                scaleJsf.val = _scaleJsfOrig.val;
                scaleJsf.setCallbackFunction = _ =>
                {
                    if(calibrationHelper.autoUpdateJsb.val)
                    {
                        if(!enabled || calibrationHelper.calibratingJsb.val || containingAtom.FreezeGrabbing())
                        {
                            return;
                        }

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
            FrictionHandler.Init();

            settingsMonitor = gameObject.AddComponent<SettingsMonitor>();
            settingsMonitor.Init();

            if(personIsFemale)
            {
                trackLeftBreast = new TrackFemaleBreast(Side.LEFT);
                trackRightBreast = new TrackFemaleBreast(Side.RIGHT);
            }
            else
            {
                trackLeftBreast = new TrackFutaBreast(Side.LEFT);
                trackRightBreast = new TrackFutaBreast(Side.RIGHT);
            }

            ForcePhysicsHandler.Init();
            ForceMorphHandler.Init();

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
                _softnessNoCallbackJsf = this.NewJSONStorableFloat("breastSoftnessNoCallback", 70f, 0f, 100f);
                softnessJsf = this.NewJSONStorableFloat("breastSoftness", 70f, 0f, 100f);
                softnessJsf.setCallbackFunction = value =>
                {
                    if(Mathf.Abs(value - softnessAmount) > 0.001f)
                    {
                        StartCalibration(calibratesMass: false, waitsForListeners: true);
                    }

                    _softnessNoCallbackJsf.valNoCallback = value;
                };

                _quicknessNoCallbackJsf = this.NewJSONStorableFloat("breastQuicknessNoCallback", 70f, 0f, 100f);
                quicknessJsf = this.NewJSONStorableFloat("breastQuickness", 70f, 0f, 100f);
                quicknessJsf.setCallbackFunction = value =>
                {
                    if(Mathf.Abs(value - quicknessAmount) > 0.001f)
                    {
                        StartCalibration(calibratesMass: false, waitsForListeners: true);
                    }

                    _quicknessNoCallbackJsf.valNoCallback = value;
                };

                _softnessNoCallbackJsf.setCallbackFunction = value => softnessJsf.valNoCallback = value;
                _quicknessNoCallbackJsf.setCallbackFunction = value => quicknessJsf.valNoCallback = value;

                calibrate = this.NewJSONStorableAction("calibratePhysicsAndMorphs", () => StartCalibration(calibratesMass: false));
                calculateBreastMass = this.NewJSONStorableAction("calculateBreastMass", () => StartCalibration(calibratesMass: true));
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

            Integration.Init();
            calibrationHelper = gameObject.AddComponent<CalibrationHelper>();
            calibrationHelper.Init();

            /* Setup navigation */
            {
                _mainWindow = new MainWindow();
                _physicsWindow = new PhysicsWindow();
                _morphingWindow = new MorphingWindow();
                _gravityWindow = new GravityWindow();

                tabs = new Tabs(leftUIContent, rightUIContent);
                tabs.CreateNavigationButton(_mainWindow, "Control", NavigateToMainWindow);
                tabs.CreateNavigationButton(_physicsWindow, "Physics Params", NavigateToPhysicsWindow);
                tabs.CreateNavigationButton(_morphingWindow, "Morph Multipliers", NavigateToMorphingWindow);
                tabs.CreateNavigationButton(_gravityWindow, "Gravity Multipliers", NavigateToGravityWindow);
            }

            NavigateToMainWindow();
            HardColliderHandler.SetPectoralCollisions(!personIsFemale);
            uiModManager = gameObject.AddComponent<UIModManager>();
            uiModManager.ModifyAtomUI();

            if(!_restoringFromJson)
            {
                HardColliderHandler.SaveOriginalUseColliders();
                SoftPhysicsHandler.SaveOriginalBoolParamValues();
                StartCalibration(calibratesMass: true);
            }

            initialized = true;
        }

        private bool _characterReady;
        private bool _skinReady;

        private IEnumerator WaitForGeometryAndSkinReady(int timeout)
        {
            float timePassed = 0;
            while(timePassed < timeout && !(_characterReady && _skinReady))
            {
                timePassed += Time.unscaledDeltaTime;
                geometry = (DAZCharacterSelector) containingAtom.GetStorableByID("geometry");
                _characterReady = geometry.selectedCharacter.ready;
                _skinReady = geometry.selectedCharacter.name == "femaledummy" || containingAtom.GetStorableByID("skin") != null;
                yield return new WaitForSecondsRealtime(0.5f);
            }
        }

        public IEnumerator RefreshSkin()
        {
            _characterReady = false;
            _skinReady = false;
            skin = null;

            const int timeout = 15;
            yield return WaitForGeometryAndSkinReady(timeout);

            if(!_characterReady)
            {
                Utils.LogError(
                    $"Selected character {geometry.selectedCharacter.name} was not ready after {timeout} seconds of waiting. " +
                    "Try reloading, and please report an issue."
                );
                yield break;
            }

            if(!_skinReady)
            {
                Utils.LogError(
                    $"Person skin materials not found after {timeout} seconds of waiting. " +
                    "Try reloading, and please report an issue."
                );
                yield break;
            }

            skin = containingAtom.GetComponentInChildren<DAZCharacter>().skin;
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

        private void FixedUpdate()
        {
            if(!initialized)
            {
                return;
            }

            try
            {
                if(_restoringFromJson ||
                    calibrationHelper.calibratingJsb.val ||
                    skin == null ||
                    _savingScene ||
                    containingAtom.FreezeGrabbing())
                {
                    return;
                }

                if(_listenersCheckRunner.Run(BreastMorphListener.ChangeWasDetected) && calibrationHelper.autoUpdateJsb.val)
                {
                    StartCalibration(calibratesMass: true, waitsForListeners: true);
                    return;
                }

                scaleJsf.val = _scaleJsfOrig.val;
                trackLeftBreast.Update();
                trackRightBreast.Update();
                HardColliderHandler.UpdateDistanceDiffs();
                GravityEffectCalc.CalculateAngles(pectoralRbLeft, pectoralRbRight);

                HardColliderHandler.UpdateFriction();
                ForcePhysicsHandler.Update();
                GravityPhysicsHandler.Update();
                ForceMorphHandler.Update();
                GravityOffsetMorphHandler.Update();

                if(envIsDevelopment)
                {
                    (_mainWindow.GetActiveNestedWindow() as DevWindow)?.Update();
                }
            }
            catch(Exception e)
            {
                Utils.LogError($"FixedUpdate: {e}");
            }
        }

        #endregion Update

        #region Calibration

        public void StartCalibration(bool calibratesMass, bool waitsForListeners = false)
        {
            if(_restoringFromJson || calibrationHelper.calibratingJsb.val && calibrationHelper.BlockedByInput())
            {
                return;
            }

            if(!enabled)
            {
                Utils.LogMessage("Enable the plugin to calibrate physics and morphs.");
                return;
            }

            StartCoroutine(CalibrationCo(calibratesMass, waitsForListeners));
        }

        private IEnumerator CalibrationCo(bool calibratesMass, bool waitsForListeners)
        {
            /* Ensure everything is ready for calibration to proceed in case of e.g. appearance preset load */
            {
                /* Can occur when loading a look and VaM pauses to load assets */
                while(!geometry.selectedCharacter.ready)
                {
                    yield return null;
                }

                settingsMonitor.CheckSettings();
                settingsMonitor.enabled = false;

                /* Sanity check. Might occur (?) even after hard colliders are enabled by settings monitor */
                float timeout = Time.unscaledTime + 5;
                while(!HardColliderHandler.RigidbodiesFound() && Time.unscaledTime < timeout)
                {
                    yield return null;
                }

                if(!HardColliderHandler.RigidbodiesFound())
                {
                    Utils.LogError("Fatal calibration error: collider rigidbodies not found after 5 seconds of waiting.");
                    yield break;
                }
            }

            if(containingAtom.isPhysicsFrozen)
            {
                Utils.LogMessage("Calibration will start when Person atom physics is unfrozen.");
            }

            while(containingAtom.isPhysicsFrozen)
            {
                yield return null;
            }

            yield return calibrationHelper.Begin();
            if(calibrationHelper.cancelling)
            {
                calibrationHelper.cancelling = false;
                yield break;
            }

            /* Dynamic adjustments to zero (simulate static upright pose), update physics */
            {
                trackLeftBreast.Reset();
                trackRightBreast.Reset();
                HardColliderHandler.ResetDistanceDiffs();

                HardColliderHandler.UpdateFriction();
                ForcePhysicsHandler.Update();
                GravityPhysicsHandler.SimulateUpright();
                ZeroDuplicateMorphs();
                ForceMorphHandler.SimulateUpright();
                GravityOffsetMorphHandler.SimulateUpright();

                MainPhysicsHandler.UpdatePhysics();
                SoftPhysicsHandler.UpdatePhysics();
                NippleErectionHandler.Update();
            }

            if(waitsForListeners)
            {
                yield return calibrationHelper.WaitForListeners();
            }

            SoftPhysicsHandler.SyncSoftPhysics();

            /* Calculate softness and quickness */
            softnessAmount = Curves.SoftnessBaseCurve(softnessJsf.val / 100f);
            quicknessAmount = 2 * quicknessJsf.val / 100 - 1;

            /* Calculate mass when gravity is off and collision is disabled to get a consistent result.
             * Mass is calculated multiple times because each new mass value changes the exact breast
             * shape and therefore the estimated volume.
             */

            if(calibrationHelper.disableBreastCollisionJsb.val)
            {
                calibrationHelper.SetBreastsCollisionEnabled(false);
            }

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
            yield return calibrationHelper.WaitAndRepeat(updateMass, 5, 0.1f);

            /* Set mass and softness based multipliers */
            ForceMorphHandler.SetMultipliers(MainPhysicsHandler.realMassAmount, softnessAmount);
            ForcePhysicsHandler.SetMultipliers();
            GravityOffsetMorphHandler.SetMultipliers(MainPhysicsHandler.normalizedInvertedRealMass);
            HardColliderHandler.UpdateFrictionSizeMultipliers();

            /* Update physics */
            MainPhysicsHandler.UpdatePhysics();
            SoftPhysicsHandler.UpdatePhysics();
            NippleErectionHandler.Update();
            FrictionHandler.CalculateFriction();

            /* Calibrate tracking and colliders */
            {
                _isSimulatingUprightPose = true;
                StartCoroutine(SimulateUprightPose());
                Action calibrateTrackingAndColliders = () =>
                {
                    trackLeftBreast.Calibrate();
                    trackRightBreast.Calibrate();
                    HardColliderHandler.CalibrateColliders();
                    HardColliderHandler.SyncAllOffsets();
                };
                yield return calibrationHelper.WaitAndRepeat(calibrateTrackingAndColliders, 24, 0.05f);
                HardColliderHandler.SyncCollidersMass();
                HardColliderHandler.SyncAllOffsets();
                _isSimulatingUprightPose = false;
            }

            SetBreastsUseGravity(true);
            if(calibrationHelper.disableBreastCollisionJsb.val)
            {
                calibrationHelper.SetBreastsCollisionEnabled(containingAtom.GetStorableByID("AtomControl").GetBoolParamValue("collisionEnabled"));
            }

            yield return calibrationHelper.DeferFinish();
        }

        private void ZeroDuplicateMorphs()
        {
            var versionRegex = new Regex(@"\.\d+$", RegexOptions.Compiled);
            string packageId = this.GetPackageId();
            if(string.IsNullOrEmpty(packageId))
            {
                return;
            }

            string packageName = versionRegex.Split(packageId)[0];
            foreach(var morph in Utils.morphsControlUI.morphBank1.morphs)
            {
                if(
                    morph.isInPackage &&
                    morph.morphValue != 0 &&
                    morph.packageUid.StartsWith(packageName) &&
                    morph.packageUid != packageId
                )
                {
                    morph.morphValue = 0;
                }
            }
        }

        private void SetBreastsUseGravity(bool value)
        {
            pectoralRbLeft.useGravity = value;
            pectoralRbRight.useGravity = value;
        }

        private bool _isSimulatingUprightPose;

        private IEnumerator SimulateUprightPose()
        {
            while(_isSimulatingUprightPose)
            {
                HardColliderHandler.UpdateFriction();
                ForcePhysicsHandler.Update();
                GravityPhysicsHandler.SimulateUpright();
                ForceMorphHandler.SimulateUpright();
                GravityOffsetMorphHandler.SimulateUpright();

                // scale force to be correct for the given fps vs physics rate, for some reason this produces an accurate calibration result
                float rateToPhysicsRateRatio = Time.deltaTime / Time.fixedDeltaTime;
                // simulate force of gravity when upright
                var force = _chestTransform.up * (rateToPhysicsRateRatio * -Physics.gravity.magnitude);
                pectoralRbLeft.AddForce(force, ForceMode.Acceleration);
                pectoralRbRight.AddForce(force, ForceMode.Acceleration);

                yield return null;
            }
        }

        #endregion Calibration

        public void ReinitFrictionHandlerAndUI()
        {
            FrictionHandler.Refresh();
            uiModManager.SetFrictionUIVisibility();
        }

        public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
        {
            var jsonClass = base.GetJSON(includePhysical, includeAppearance, forceStore);
            jsonClass.Remove(calibrationHelper.calibratingJsb.name);
            jsonClass.Remove(uiModManager.skinMaterials2Modified.name);
            jsonClass.Remove(_softnessNoCallbackJsf.name);
            jsonClass.Remove(_quicknessNoCallbackJsf.name);
            needsStore = true;
            return jsonClass;
        }

        private bool _restoringFromJson;

        public override void RestoreFromJSON(
            JSONClass jsonClass,
            bool restorePhysical = true,
            bool restoreAppearance = true,
            JSONArray presetAtoms = null,
            bool setMissingToDefault = true
        )
        {
            /* Disable early to allow correct enabled value to be used during Init */
            if(jsonClass.HasKey("enabled") && !jsonClass["enabled"].AsBool)
            {
                enabled = false;
            }

            _restoringFromJson = true;
            /* Prevent overriding versionJss.val from JSON. Version stored in JSON just for information,
             * but could be intercepted here and used to save a "loadedFromVersion" value.
             */
            if(jsonClass.HasKey(Constant.VERSION))
            {
                jsonClass[Constant.VERSION] = $"{VERSION}";
            }

            if(jsonClass.HasKey("forceMorphingLeftRight"))
            {
                jsonClass["forceMorphingSidewaysIn"] = jsonClass["forceMorphingLeftRight"];
                jsonClass["forceMorphingSidewaysOut"] = jsonClass["forceMorphingLeftRight"];
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
            while(!initialized)
            {
                yield return null;
            }

            base.RestoreFromJSON(jsonClass, restorePhysical, restoreAppearance, presetAtoms, setMissingToDefault);

            _restoringFromJson = false;
            HardColliderHandler.SaveOriginalUseColliders();
            SoftPhysicsHandler.SaveOriginalBoolParamValues();
            StartCalibration(calibratesMass: true);
        }

        private bool _savingScene;

        private void OnBeforeSceneSave()
        {
            _savingScene = true;
            GravityOffsetMorphHandler.ResetAll();
            ForceMorphHandler.ResetAll();
            NippleErectionHandler.Reset();
        }

        private void OnSceneSaved()
        {
            _savingScene = false;
        }

        public void AddToggleToJsb(UIDynamicToggle toggle, JSONStorableBool jsb) =>
            toggleToJSONStorableBool.Add(toggle, jsb);

        public void AddSliderToJsf(UIDynamicSlider slider, JSONStorableFloat jsf) =>
            sliderToJSONStorableFloat.Add(slider, jsf);

        private void OnDestroy()
        {
            try
            {
                Destroy(calibrationHelper);
                Destroy(settingsMonitor);
                Destroy(bindings);

                /* Nullify static reference fields to let GC collect unreachable instances */
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
                Utils.morphsControlUI = null;
                geometry = null;
                skin = null;

                scaleJsf.setJSONCallbackFunction = null;

                _mainWindow.GetSliders().ForEach(slider => Destroy(slider.GetPointerUpDownListener()));
                _morphingWindow.GetSliders().ForEach(slider => Destroy(slider.GetPointerUpDownListener()));
                _gravityWindow.GetSliders().ForEach(slider => Destroy(slider.GetPointerUpDownListener()));

                DestroyImmediate(_pluginUIEventsListener);
                Destroy(uiModManager);

                SuperController.singleton.onSceneSavedHandlers -= OnSceneSaved;
                SuperController.singleton.onBeforeSceneSaveHandlers -= OnBeforeSceneSave;
                SuperController.singleton.BroadcastMessage("OnActionsProviderDestroyed", this, SendMessageOptions.DontRequireReceiver);
            }
            catch(Exception e)
            {
                if(initialized)
                {
                    Utils.LogError($"OnDestroy: {e}");
                }
                else
                {
                    Utils.Log($"OnDestroy: {e}");
                }
            }
        }

        public void OnEnable()
        {
            if(!initialized)
            {
                return;
            }

            try
            {
                settingsMonitor.enabled = true;
                HardColliderHandler.SaveOriginalUseColliders();
                HardColliderHandler.EnableAdvColliders();
                HardColliderHandler.EnableMultiplyFriction();
                SoftPhysicsHandler.SaveOriginalBoolParamValues();
                SoftPhysicsHandler.EnableMultiplyFriction();
                StartCalibration(calibratesMass: true);
                uiModManager.enabled = true;
            }
            catch(Exception e)
            {
                Utils.LogError($"OnEnable: {e}");
            }
        }

        private void OnDisable()
        {
            /* Prevent disable actions if disabled when restoring from JSON */
            if(!initialized)
            {
                return;
            }

            try
            {
                settingsMonitor.enabled = false;
                HardColliderHandler.SetPectoralCollisions(true);
                HardColliderHandler.RestoreOriginalPhysics();
                MainPhysicsHandler.RestoreOriginalPhysics();
                SoftPhysicsHandler.RestoreOriginalPhysics();
                GravityOffsetMorphHandler.ResetAll();
                ForceMorphHandler.ResetAll();
                NippleErectionHandler.Reset();
                uiModManager.enabled = false;
            }
            catch(Exception e)
            {
                Utils.LogError($"OnDisable: {e}");
            }
        }
    }
}
