//#define DEBUG_PHYSICS
//#define DEBUG_MORPHS

using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TittyMagic
{
    internal class Script : MVRScript
    {
        private static readonly Version v2_1 = new Version("2.1.0");
        public static readonly Version version = new Version("0.0.0");

        public readonly List<string> modes = new List<string>
        {
            { Mode.ANIM_OPTIMIZED },
            { Mode.BALANCED },
            { Mode.TOUCH_OPTIMIZED }
        };

        private Bindings customBindings;

        private List<Rigidbody> rigidbodies;
        private Rigidbody chestRigidbody;
        private Transform chestTransform;
        private Rigidbody rNippleRigidbody;
        private Transform rNippleTransform;
        private Rigidbody lPectoralRigidbody;
        private Rigidbody rPectoralRigidbody;
        private DAZCharacterSelector geometry;
        private Vector3 neutralRelativePos;
        private Vector3 positionDiff;

        private float massEstimate;
        private float gravityLogAmount;

        private SettingsMonitor settingsMonitor;

        private AtomScaleListener atomScaleListener;
        private BreastMorphListener breastMorphListener;
        private BreastMassCalculator breastMassCalculator;

        private GravityMorphHandler gravityMorphH;
        private RelativePosMorphHandler relativePosMorphH;
        private NippleErectionMorphHandler nippleMorphH;
        private StaticPhysicsHandler staticPhysicsH;
        private GravityPhysicsHandler gravityPhysicsH;

        private JSONStorableString titleUIText;
        private JSONStorableString statusUIText;
        private JSONStorableString positionInfoUIText;

        private Dictionary<string, UIDynamicButton> modeButtonGroup;

        //registered storables

        private JSONStorableStringChooser modeChooser;
        private JSONStorableString pluginVersionStorable;
        private JSONStorableFloat softness;
        private SliderClickMonitor softnessSCM;
        private JSONStorableFloat gravity;
        private SliderClickMonitor gravitySCM;
        private JSONStorableBool linkSoftnessAndGravity;
        private JSONStorableBool enableGravityMorphs;
        private JSONStorableBool enableForceMorphs;
        private JSONStorableFloat nippleErection;

        private float timeSinceListenersChecked;
        private float listenersCheckInterval = 0.1f;
        private int refreshStatus = RefreshStatus.WAITING;
        private bool animationWasFrozen = false;
        private bool restoringFromJson = false;
        private float? legacySoftnessFromJson;
        private float? legacyGravityFromJson;

        private float timeMultiplier;

#if DEBUG_PHYSICS || DEBUG_MORPHS
        protected JSONStorableString baseDebugInfo = new JSONStorableString("Base Debug Info", "");
#endif
#if DEBUG_PHYSICS
        protected JSONStorableString physicsDebugInfo = new JSONStorableString("Physics Debug Info", "");
#elif DEBUG_MORPHS
        protected JSONStorableString morphDebugInfo = new JSONStorableString("Morph Debug Info", "");
#endif

        public override void Init()
        {
            try
            {
                pluginVersionStorable = new JSONStorableString("Version", "");
                pluginVersionStorable.val = $"{version}";
                RegisterString(pluginVersionStorable);

                if(containingAtom.type != "Person")
                {
                    Log.Error($"Add to a Person atom, not {containingAtom.type}");
                    return;
                }

                AdjustJoints breastControl = containingAtom.GetStorableByID("BreastControl") as AdjustJoints;
                DAZPhysicsMesh breastPhysicsMesh = containingAtom.GetStorableByID("BreastPhysicsMesh") as DAZPhysicsMesh;
                rigidbodies = containingAtom.GetComponentsInChildren<Rigidbody>().ToList();
                chestRigidbody = rigidbodies.Find(rb => rb.name == "chest");
                chestTransform = chestRigidbody.transform;
                rNippleRigidbody = rigidbodies.Find(rb => rb.name == "rNipple");
                rNippleTransform = rNippleRigidbody.transform;
                lPectoralRigidbody = rigidbodies.Find(rb => rb.name == "lPectoral");
                rPectoralRigidbody = rigidbodies.Find(rb => rb.name == "rPectoral");
                geometry = containingAtom.GetStorableByID("geometry") as DAZCharacterSelector;

                Globals.BREAST_CONTROL = breastControl;
                Globals.BREAST_PHYSICS_MESH = breastPhysicsMesh;
                Globals.GEOMETRY = containingAtom.GetStorableByID("geometry") as DAZCharacterSelector;

                settingsMonitor = gameObject.AddComponent<SettingsMonitor>();
                settingsMonitor.Init(containingAtom);

                atomScaleListener = new AtomScaleListener(containingAtom.GetStorableByID("rescaleObject").GetFloatJSONParam("scale"));
                breastMorphListener = new BreastMorphListener(geometry.morphBank1.morphs);
                breastMassCalculator = new BreastMassCalculator(chestTransform);

                gravityMorphH = new GravityMorphHandler();
                relativePosMorphH = new RelativePosMorphHandler();
                nippleMorphH = new NippleErectionMorphHandler();
                gravityPhysicsH = new GravityPhysicsHandler();
                staticPhysicsH = new StaticPhysicsHandler(GetPackagePath());

                InitPluginUILeft();
                InitPluginUIRight();
                InitSliderListeners();
                SuperController.singleton.onAtomRemovedHandlers += OnRemoveAtom;

                UpdateLogarithmicGravityAmount(gravity.val);
                if(string.IsNullOrEmpty(modeChooser.val))
                {
                    modeChooser.val = modes.First();
                }
                else
                {
                    StartCoroutine(BeginRefresh());
                }

                StartCoroutine(SubscribeToKeybindings());

                //StartCoroutine(MigrateFromPre2_1());
            }
            catch(Exception e)
            {
                Log.Error($"{e}");
            }
        }

        //https://github.com/vam-community/vam-plugins-interop-specs/blob/main/keybindings.md
        private IEnumerator SubscribeToKeybindings()
        {
            yield return new WaitForEndOfFrame();
            SuperController.singleton.BroadcastMessage("OnActionsProviderAvailable", this, SendMessageOptions.DontRequireReceiver);
        }

        public void OnBindingsListRequested(List<object> bindings)
        {
            customBindings = gameObject.AddComponent<Bindings>();
            customBindings.Init(this);
            bindings.Add(customBindings.Settings);
            bindings.AddRange(customBindings.OnKeyDownActions);
        }

        #region User interface

        private void InitPluginUILeft()
        {
            titleUIText = NewTextField("titleText", 36, 100);
            titleUIText.SetVal($"{nameof(TittyMagic)}\n<size=28>v{version}</size>");

            CreateNewSpacer(10f);
            modeChooser = new JSONStorableStringChooser(
                "Mode",
                modes,
                "",
                "Mode",
                (val) =>
                {
                    UpdateButtonLabels(modeButtonGroup, val);
                    staticPhysicsH.LoadSettings(val);
                    StartCoroutine(BeginRefresh());
                }
            );
            modeButtonGroup = CreateRadioButtonGroup(modeChooser);
            staticPhysicsH.modeChooser = modeChooser;

            CreateNewSpacer(10f);
            softness = NewFloatSlider("Breast softness", 50f, Const.SOFTNESS_MIN, Const.SOFTNESS_MAX, "F0");
            gravity = NewFloatSlider("Breast gravity", 50f, Const.GRAVITY_MIN, Const.GRAVITY_MAX, "F0");
            linkSoftnessAndGravity = NewToggle("Link softness and gravity", true, false);
            positionInfoUIText = NewTextField("positionInfoText", 36, 100);
            enableGravityMorphs = NewToggle("Enable gravity morphs", false, false);
            enableGravityMorphs.toggle.onValueChanged.AddListener((bool val) =>
            {
                if(!val)
                {
                    gravityMorphH.ResetAll();
                }
            });

            enableForceMorphs = NewToggle("Enable force morphs", false, false);

            CreateNewSpacer(10f);
            nippleErection = NewFloatSlider("Erect nipples", 0f, 0f, 1.0f, "F2");
        }

        private void InitPluginUIRight()
        {
            bool rightSide = true;
            statusUIText = NewTextField("statusText", 28, 100, rightSide);

#if DEBUG_PHYSICS || DEBUG_MORPHS
            UIDynamicTextField angleInfoField = CreateTextField(baseDebugInfo, rightSide);
            angleInfoField.height = 125;
            angleInfoField.UItext.fontSize = 26;
#else
            CreateNewSpacer(10f, true);
            JSONStorableString usage2Area = NewTextField("Usage Info Area 2", 28, 135, rightSide);
            string usage2 = UI.Size("\n", 12);
            usage2 += "Physics settings mode selection.";
            usage2Area.SetVal(usage2);

            CreateNewSpacer(10f, true);
            JSONStorableString usage1Area = NewTextField("Usage Info Area 1", 28, 255, rightSide);
            string usage1 = UI.Size("\n", 12);
            usage1 += "Breast softness adjusts soft physics settings from very firm to very soft.\n\n";
            usage1 += "Breast gravity adjusts how much pose morphs shape the breasts in all orientations.";
            usage1Area.SetVal(usage1);
#endif
#if DEBUG_PHYSICS
            UIDynamicTextField physicsInfoField = CreateTextField(physicsDebugInfo, rightSide);
            physicsInfoField.height = 945;
            physicsInfoField.UItext.fontSize = 26;
#elif DEBUG_MORPHS
            UIDynamicTextField morphInfo = CreateTextField(morphDebugInfo, rightSide);
            morphInfo.height = 945;
            morphInfo.UItext.fontSize = 26;
#endif
        }

        private Dictionary<string, UIDynamicButton> CreateRadioButtonGroup(JSONStorableStringChooser jsc, bool rightSide = false)
        {
            Dictionary<string, UIDynamicButton> buttons = new Dictionary<string, UIDynamicButton>();
            jsc.choices.ForEach((choice) =>
            {
                UIDynamicButton btn = CreateButton(UI.RadioButtonLabel(choice, choice == jsc.defaultVal), rightSide);
                btn.buttonText.alignment = TextAnchor.MiddleLeft;
                btn.buttonColor = UI.darkOffGrayViolet;
                btn.height = 60f;
                buttons.Add(choice, btn);
            });

            buttons.Keys.ToList().ForEach(name =>
            {
                buttons[name].button.onClick.AddListener(() =>
                {
                    jsc.val = name;
                });
            });

            return buttons;
        }

        private void UpdateButtonLabels(Dictionary<string, UIDynamicButton> buttons, string selected)
        {
            buttons[selected].label = UI.RadioButtonLabel(selected, true);
            buttons.Where(kvp => kvp.Key != selected).ToList()
                .ForEach(kvp => kvp.Value.label = UI.RadioButtonLabel(kvp.Key, false));
        }

        private JSONStorableFloat NewFloatSlider(
            string paramName,
            float startingValue,
            float minValue,
            float maxValue,
            string valueFormat,
            bool rightSide = false
        )
        {
            JSONStorableFloat storable = new JSONStorableFloat(paramName, startingValue, minValue, maxValue);
            storable.storeType = JSONStorableParam.StoreType.Physical;
            RegisterFloat(storable);
            UIDynamicSlider slider = CreateSlider(storable, rightSide);
            slider.valueFormat = valueFormat;
            return storable;
        }

        private JSONStorableString NewTextField(string paramName, int fontSize, int height = 100, bool rightSide = false)
        {
            JSONStorableString storable = new JSONStorableString(paramName, "");
            UIDynamicTextField textField = CreateTextField(storable, rightSide);
            textField.UItext.fontSize = fontSize;
            textField.height = height;
            return storable;
        }

        private JSONStorableBool NewToggle(string paramName, bool startingValue, bool rightSide = false)
        {
            JSONStorableBool storable = new JSONStorableBool(paramName, startingValue);
            CreateToggle(storable, rightSide);
            RegisterBool(storable);
            return storable;
        }

        private void CreateNewSpacer(float height, bool rightSide = false)
        {
            UIDynamic spacer = CreateSpacer(rightSide);
            spacer.height = height;
        }

        #endregion User interface

        private void InitSliderListeners()
        {
            softnessSCM = softness.slider.gameObject.AddComponent<SliderClickMonitor>();
            gravitySCM = gravity.slider.gameObject.AddComponent<SliderClickMonitor>();

            softness.slider.onValueChanged.AddListener((float val) =>
            {
                if(linkSoftnessAndGravity.val)
                {
                    gravity.val = val;
                    UpdateLogarithmicGravityAmount(val);
                }
                if(refreshStatus != RefreshStatus.WAITING)
                {
                    StartCoroutine(BeginRefresh());
                }
            });
            gravity.slider.onValueChanged.AddListener((float val) =>
            {
                if(linkSoftnessAndGravity.val)
                {
                    softness.val = val;
                }
                UpdateLogarithmicGravityAmount(val);
                if(refreshStatus != RefreshStatus.WAITING)
                {
                    StartCoroutine(BeginRefresh());
                }
            });
            nippleErection.slider.onValueChanged.AddListener((float val) =>
            {
                nippleMorphH.Update(val);
                staticPhysicsH.UpdateNipplePhysics(massEstimate, softness.val, val);
            });
        }

        private void UpdateLogarithmicGravityAmount(float val)
        {
            gravityLogAmount = Mathf.Log(10 * Const.ConvertToLegacyVal(val) - 3.35f);
        }

        private IEnumerator MigrateFromPre2_1()
        {
            yield return new WaitForEndOfFrame();
            if(!restoringFromJson)
            {
                yield break;
            }

            if(legacySoftnessFromJson.HasValue)
            {
                softness.val = Const.ConvertFromLegacyVal(legacySoftnessFromJson.Value);
                Log.Message($"Converted legacy Breast softness {legacySoftnessFromJson.Value} in savefile to new slider value {softness.val}.");
            }

            if(legacyGravityFromJson.HasValue)
            {
                gravity.val = Const.ConvertFromLegacyVal(legacyGravityFromJson.Value);
                Log.Message($"Converted legacy Breast gravity {legacyGravityFromJson.Value} in savefile to new slider value {gravity.val}.");
            }

            restoringFromJson = false;
        }

        private void FixedUpdate()
        {
            try
            {
                DoUpdate();
            }
            catch(Exception e)
            {
                Log.Error($"{e}");
                Log.Error($"Try reloading plugin!");
                enabled = false;
            }
        }

        private float TimeMultiplier()
        {
            if(TimeControl.singleton == null || Mathf.Approximately(Time.timeScale, 0f))
            {
                return 1;
            }

            return 1 / Time.timeScale;
        }

        private void DoUpdate()
        {
            timeSinceListenersChecked += Time.unscaledDeltaTime;

            if(refreshStatus == RefreshStatus.MASS_STARTED)
            {
#if DEBUG_PHYSICS || DEBUG_MORPHS
                positionInfoUIText.SetVal("");
#endif
                return;
            }

            if(refreshStatus > RefreshStatus.MASS_STARTED)
            {
                float gravityMagnitude = Physics.gravity.magnitude / timeMultiplier;
                chestRigidbody.AddForce(chestTransform.up * -gravityMagnitude, ForceMode.Acceleration);
                lPectoralRigidbody.AddForce(chestTransform.up * -gravityMagnitude, ForceMode.Acceleration);
                rPectoralRigidbody.AddForce(chestTransform.up * -gravityMagnitude, ForceMode.Acceleration);
                if(modeChooser.val == Mode.ANIM_OPTIMIZED && refreshStatus == RefreshStatus.MASS_OK)
                {
                    StartCoroutine(RefreshNeutralRelativePosition());
                }
                if(modeChooser.val != Mode.ANIM_OPTIMIZED || refreshStatus == RefreshStatus.NEUTRALPOS_OK)
                {
                    EndRefresh();
                }
                return;
            }

            if(timeSinceListenersChecked >= listenersCheckInterval)
            {
                timeSinceListenersChecked -= listenersCheckInterval;
                if(refreshStatus != RefreshStatus.WAITING && (breastMorphListener.Changed() || atomScaleListener.Changed()))
                {
                    StartCoroutine(BeginRefresh());
                    return;
                }
            }

            UpdateBreastShape();
#if DEBUG_PHYSICS || DEBUG_MORPHS
            positionInfoUIText.SetVal(
                $"<size=28>Neutral pos:\n" +
                $"{Formatting.NameValueString("x", neutralRelativePos.x, 1000)} " +
                $"{Formatting.NameValueString("y", neutralRelativePos.y, 1000)} " +
                $"{Formatting.NameValueString("z", neutralRelativePos.z, 1000)} " +
                $"</size>"
            );
#endif
        }

        private void UpdateBreastShape()
        {
            float roll = Calc.Roll(chestTransform.rotation);
            float pitch = Calc.Pitch(chestTransform.rotation);
            float scaleVal = breastMassCalculator.LegacyScale(massEstimate);

            if(enableGravityMorphs.val)
            {
                gravityMorphH.Update(roll, pitch, scaleVal, gravityLogAmount);
            }

            //TODO properly disable on uncheck enableForceMorphs
            if(modeChooser.val == Mode.ANIM_OPTIMIZED)
            {
                positionDiff = neutralRelativePos - Calc.RelativePosition(chestTransform, rNippleTransform.position);
                if(enableForceMorphs.val)
                {
                    relativePosMorphH.Update(positionDiff, scaleVal, Const.ConvertToLegacyVal(softness.val));
                }
            }

            gravityPhysicsH.Update(roll, pitch, scaleVal, Const.ConvertToLegacyVal(softness.val), Const.ConvertToLegacyVal(gravity.val));
#if DEBUG_PHYSICS || DEBUG_MORPHS
            SetBaseDebugInfo(roll, pitch, positionDiff);
#endif
#if DEBUG_PHYSICS
            physicsDebugInfo.SetVal(staticPhysicsH.GetStatus() + gravityPhysicsH.GetStatus());
#elif DEBUG_MORPHS
            morphDebugInfo.SetVal(gravityMorphH.GetStatus());
#endif
        }

        public IEnumerator BeginRefresh()
        {
            refreshStatus = RefreshStatus.WAITING;
            // ensure refresh actually begins only once listeners report no change
            yield return new WaitForSecondsRealtime(listenersCheckInterval);
            while(breastMorphListener.Changed() || atomScaleListener.Changed() || softnessSCM.isDown || gravitySCM.isDown)
            {
                yield return new WaitForSecondsRealtime(0.1f);
            }
            yield return new WaitForSecondsRealtime(0.33f);

            refreshStatus = RefreshStatus.MASS_STARTED;

            settingsMonitor.enabled = false;
            animationWasFrozen = SuperController.singleton.freezeAnimation;
            SuperController.singleton.SetFreezeAnimation(true);

            // simulate breasts zero G
            chestRigidbody.useGravity = false;
            lPectoralRigidbody.useGravity = false;
            rPectoralRigidbody.useGravity = false;

            // zero pose morphs
            gravityMorphH.ResetAll();

            float duration = 0;
            float interval = 0.1f;
            while(duration < 2f && (
                !Calc.VectorEqualWithin(1000f, rNippleRigidbody.velocity, Vector3.zero) ||
                !Calc.EqualWithin(1000f, massEstimate, DetermineMassEstimate(atomScaleListener.Value))
            ))
            {
                yield return new WaitForSecondsRealtime(interval);
                duration += interval;

                // update mass estimate
                massEstimate = DetermineMassEstimate(atomScaleListener.Value);

                // update main static physics
                staticPhysicsH.UpdateMainPhysics(massEstimate, softness.val);

                // update gravity physics angles
                float roll = 0;
                float pitch = 0;
                float scaleVal = breastMassCalculator.LegacyScale(massEstimate);
                gravityPhysicsH.Update(roll, pitch, scaleVal, Const.ConvertToLegacyVal(softness.val), Const.ConvertToLegacyVal(gravity.val));

                // TODO update gravity morphs ?
            }
            SetMassUIStatus(atomScaleListener.Value);
            staticPhysicsH.FullUpdate(massEstimate, softness.val, nippleErection.val);

            timeMultiplier = TimeMultiplier();
            refreshStatus = RefreshStatus.MASS_OK;
        }

        private IEnumerator RefreshNeutralRelativePosition()
        {
            refreshStatus = RefreshStatus.NEUTRALPOS_STARTED;
            yield return new WaitForSecondsRealtime(1f);

            float duration = 0;
            float interval = 0.1f;
            while(
                duration < 2f && (
                !Calc.VectorEqualWithin(
                    1000000f,
                    neutralRelativePos,
                    Calc.RelativePosition(chestTransform, rNippleTransform.position)
                ))
            )
            {
                yield return new WaitForSecondsRealtime(interval);
                neutralRelativePos = Calc.RelativePosition(chestTransform, rNippleTransform.position);
            }

            refreshStatus = RefreshStatus.NEUTRALPOS_OK;
        }

        private void EndRefresh()
        {
            chestRigidbody.useGravity = true;
            lPectoralRigidbody.useGravity = true;
            rPectoralRigidbody.useGravity = true;
            SuperController.singleton.SetFreezeAnimation(animationWasFrozen);
            settingsMonitor.enabled = true;
            refreshStatus = RefreshStatus.DONE;
        }

        public void RefreshRateDependentPhysics()
        {
            staticPhysicsH.UpdateRateDependentPhysics(massEstimate, softness.val);
        }

        private float DetermineMassEstimate(float atomScale)
        {
            float mass = breastMassCalculator.Calculate(atomScale);

            if(mass > Const.MASS_MAX)
                return Const.MASS_MAX;

            if(mass < Const.MASS_MIN)
                return Const.MASS_MIN;

            return mass;
        }

        private void SetMassUIStatus(float atomScale)
        {
            float mass = breastMassCalculator.Calculate(atomScale);
            if(mass > Const.MASS_MAX)
            {
                float excess = Calc.RoundToDecimals(mass - Const.MASS_MAX, 1000f);
                statusUIText.SetVal(MassExcessStatus(excess));
            }
            else if(mass < Const.MASS_MIN)
            {
                float shortage = Calc.RoundToDecimals(Const.MASS_MIN - mass, 1000f);
                statusUIText.SetVal(MassShortageStatus(shortage));
            }
            else
            {
                statusUIText.SetVal("");
            }
        }

        private string MassExcessStatus(float value)
        {
            Color color = Color.Lerp(
                new Color(0.5f, 0.5f, 0.0f, 1f),
                Color.red,
                value
            );
            return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}><size=28>" +
                $"Estimated mass is <b>{value}</b> over the 2.000 maximum.\n" +
                $"</size></color>";
        }

        private string MassShortageStatus(float value)
        {
            Color color = Color.Lerp(
                new Color(0.5f, 0.5f, 0.0f, 1f),
                Color.red,
                value*10
            );
            return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}><size=28>" +
                $"Estimated mass is <b>{value}</b> below the 0.100 minimum.\n" +
                $"</size></color>";
        }

        public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
        {
            JSONClass json = base.GetJSON(includePhysical, includeAppearance, forceStore);
            if(modeChooser.val != modes.First())
            {
                json["Mode"] = modeChooser.val;
            }
            needsStore = true;
            return json;
        }

        public override void RestoreFromJSON(JSONClass json, bool restorePhysical = true, bool restoreAppearance = true, JSONArray presetAtoms = null, bool setMissingToDefault = true)
        {
            restoringFromJson = true;

            try
            {
                CheckSavedVersion(json, () =>
                {
                    //should never occur
                    if(version.CompareTo(v2_1) < 0)
                    {
                        return;
                    }

                    //needs conversion from legacy values
                    if(json.HasKey("Breast softness"))
                    {
                        float val = json["Breast softness"].AsFloat;
                        if(val <= Const.LEGACY_MAX)
                        {
                            legacySoftnessFromJson = val;
                        }
                    }

                    if(json.HasKey("Breast gravity"))
                    {
                        float val = json["Breast gravity"].AsFloat;
                        if(val <= Const.LEGACY_MAX)
                        {
                            legacyGravityFromJson = val;
                        }
                    }
                });
            }
            catch(Exception)
            {
            }

            if(json.HasKey("Mode"))
            {
                modeChooser.val = json["Mode"];
            }

            base.RestoreFromJSON(json, restorePhysical, restoreAppearance, presetAtoms, setMissingToDefault);
        }

        private void CheckSavedVersion(JSONClass json, Action callback)
        {
            if(json["Version"] != null)
            {
                Version vSave = new Version(json["Version"].Value);
                //no conversion from legacy values needed
                if(vSave.CompareTo(v2_1) >= 0)
                {
                    return;
                }
            }

            callback();
        }

        //MacGruber / Discord 20.10.2020
        //Get path prefix of the package that contains this plugin
        public string GetPackagePath()
        {
            string id = name.Substring(0, name.IndexOf('_'));
            string filename = manager.GetJSON()["plugins"][id].Value;
            int idx = filename.IndexOf(":/");
            return idx >= 0 ? filename.Substring(0, idx+2) : "";
        }

        private void OnRemoveAtom(Atom atom)
        {
            Destroy(softnessSCM);
            Destroy(gravitySCM);
        }

        private void OnDestroy()
        {
            Destroy(settingsMonitor);
            SuperController.singleton.onAtomRemovedHandlers -= OnRemoveAtom;
            SuperController.singleton.BroadcastMessage("OnActionsProviderDestroyed", this, SendMessageOptions.DontRequireReceiver);

            gravityPhysicsH.ResetAll();
            gravityMorphH.ResetAll();
            nippleMorphH.ResetAll();
        }

        private void OnDisable()
        {
            settingsMonitor.enabled = false;
            gravityPhysicsH.ResetAll();
            gravityMorphH.ResetAll();
            nippleMorphH.ResetAll();
        }

#if DEBUG_PHYSICS || DEBUG_MORPHS

        private void SetBaseDebugInfo(float roll, float pitch, Vector3 positionDiff)
        {
            float effX = Mathf.InverseLerp(0, 0.100f, Mathf.Abs(positionDiff.x));
            float effY = Mathf.InverseLerp(0, 0.120f, Mathf.Abs(positionDiff.y));
            float effZ = Mathf.InverseLerp(0, 0.100f, Mathf.Abs(positionDiff.z));
            baseDebugInfo.SetVal(
                //$"{Formatting.NameValueString("Roll", roll, 100f, 15)}\n" +
                //$"{Formatting.NameValueString("Pitch", pitch, 100f, 15)}\n" +
                //$"{breastMassCalculator.GetStatus(atomScaleListener.Value)}"
                //Formatting.NameValueString("diff x", positionDiff.x, 100000) + "\n" +
                //Formatting.NameValueString("diff y", positionDiff.y, 100000) + "\n" +
                //Formatting.NameValueString("diff z", positionDiff.z, 100000) + "\n" +

                Formatting.NameValueString("eff x", effX, 1000) + "\n" +
                Formatting.NameValueString("eff y", effY, 1000) + "\n" +
                Formatting.NameValueString("eff z", effZ, 1000)
            );
        }

#endif
    }
}
