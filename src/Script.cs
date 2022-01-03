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

        private Bindings customBindings;

        private Transform chest;
        private Transform rNipple;
        private DAZCharacterSelector geometry;
        private Vector3 neutralPos;
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
        private JSONStorableFloat gravity;
        private JSONStorableBool linkSoftnessAndGravity;
        private JSONStorableBool enableGravityMorphs;
        private JSONStorableBool enableForceMorphs;
        private JSONStorableFloat nippleErection;

        private bool staticPhysicsRefreshDone = false;
        private bool neutralBreastPositionRefreshDone = false;
        private bool restoringFromJson = false;
        private float? legacySoftnessFromJson;
        private float? legacyGravityFromJson;

#if DEBUG_PHYSICS || DEBUG_MORPHS
        protected JSONStorableString baseDebugInfo = new JSONStorableString("Base Debug Info", "");
#endif
#if DEBUG_PHYSICS
        protected JSONStorableString physicsDebugInfo = new JSONStorableString("Physics Debug Info", "");
#elif DEBUG_MORPHS
        protected JSONStorableString morphDebugInfo = new JSONStorableString("Morph Debug Info", "");
#endif

        private float timeSinceLastRefresh = 0f;
        private const float refreshFrequency = 0.1f;

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
                chest = containingAtom.GetStorableByID("chest").transform;
                rNipple = containingAtom.GetStorableByID("rNipple").transform;
                geometry = containingAtom.GetStorableByID("geometry") as DAZCharacterSelector;

                Globals.BREAST_CONTROL = breastControl;
                Globals.BREAST_PHYSICS_MESH = breastPhysicsMesh;
                Globals.GEOMETRY = containingAtom.GetStorableByID("geometry") as DAZCharacterSelector;

                settingsMonitor = gameObject.AddComponent<SettingsMonitor>();
                settingsMonitor.Init(containingAtom);

                atomScaleListener = new AtomScaleListener(containingAtom.GetStorableByID("rescaleObject").GetFloatJSONParam("scale"));
                breastMorphListener = new BreastMorphListener(geometry.morphBank1.morphs);
                breastMassCalculator = new BreastMassCalculator();

                gravityMorphH = new GravityMorphHandler();
                relativePosMorphH = new RelativePosMorphHandler();
                nippleMorphH = new NippleErectionMorphHandler();
                gravityPhysicsH = new GravityPhysicsHandler();
                staticPhysicsH = new StaticPhysicsHandler(GetPackagePath());

                InitPluginUILeft();
                InitPluginUIRight();
                InitSliderListeners();
                UpdateLogarithmicGravityAmount(gravity.val);

                modeChooser.setCallbackFunction = (val) =>
                {
                    UpdateButtonLabels(modeButtonGroup, val);
                    staticPhysicsH.LoadSettings(val);
                    staticPhysicsH.FullUpdate(massEstimate, softness.val, nippleErection.val);
                };
                if(string.IsNullOrEmpty(modeChooser.val))
                {
                    modeChooser.val = Const.MODES.Values.First();
                }

                StartCoroutine(SubscribeToKeybindings());
                StartCoroutine(RefreshStaticPhysics(() =>
                {
                    settingsMonitor.enabled = true;
                }));
                StartCoroutine(MigrateFromPre2_1());
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
            modeChooser = new JSONStorableStringChooser("Mode", Const.MODES.Keys.ToList(), Const.MODES.Values.ToList(), "", "Mode");
            modeButtonGroup = CreateRadioButtonGroup(modeChooser);
            staticPhysicsH.modeChooser = modeChooser;

            CreateNewSpacer(10f);
            softness = NewFloatSlider("Breast softness", 50f, Const.SOFTNESS_MIN, Const.SOFTNESS_MAX, "F0");
            gravity = NewFloatSlider("Breast gravity", 50f, Const.GRAVITY_MIN, Const.GRAVITY_MAX, "F0");
            linkSoftnessAndGravity = NewToggle("Link softness and gravity", true, false);
            positionInfoUIText = NewTextField("positionInfoText", 36, 100);
            enableGravityMorphs = NewToggle("Enable gravity morphs", true, false);
            enableForceMorphs = NewToggle("Enable force morphs", true, false);

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
                UIDynamicButton btn = CreateButton(UI.RadioButtonLabel(Const.MODES[choice], choice == jsc.defaultVal), rightSide);
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
            buttons[selected].label = UI.RadioButtonLabel(Const.MODES[selected], true);
            buttons.Where(kvp => kvp.Key != selected).ToList()
                .ForEach(kvp => kvp.Value.label = UI.RadioButtonLabel(Const.MODES[kvp.Key], false));
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
            softness.slider.onValueChanged.AddListener((float val) =>
            {
                if(linkSoftnessAndGravity.val)
                {
                    gravity.val = val;
                    UpdateLogarithmicGravityAmount(val);
                }
                staticPhysicsH.FullUpdate(massEstimate, val, nippleErection.val);
            });
            gravity.slider.onValueChanged.AddListener((float val) =>
            {
                if(linkSoftnessAndGravity.val)
                {
                    softness.val = val;
                }
                UpdateLogarithmicGravityAmount(val);
                staticPhysicsH.FullUpdate(massEstimate, softness.val, nippleErection.val);
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
                DoFixedUpdate();
            }
            catch(Exception e)
            {
                Log.Error($"{e}");
                Log.Error($"Try reloading plugin!");
                enabled = false;
            }
        }

        private void DoFixedUpdate()
        {
            if(!neutralBreastPositionRefreshDone)
            {
                StartCoroutine(RefreshNeutralBreastPosition());
            }
            else if(!staticPhysicsRefreshDone && (breastMorphListener.Changed() || atomScaleListener.Changed()))
            {
                StartCoroutine(RefreshStaticPhysics());
            }
            else
            {
                UpdateBreastShape();
#if DEBUG_PHYSICS || DEBUG_MORPHS
                positionInfoUIText.SetVal(
                    $"<size=28>Neutral pos:\n" +
                    $"{Formatting.NameValueString("x", neutralPos.x, 1000)} " +
                    $"{Formatting.NameValueString("y", neutralPos.y, 1000)} " +
                    $"{Formatting.NameValueString("z", neutralPos.z, 1000)} " +
                    $"</size>"
                );
#endif
            }
        }

        private void UpdateBreastShape()
        {
            float roll = AngleCalc.Roll(chest.rotation);
            float pitch = AngleCalc.Pitch(chest.rotation);
            float scaleVal = breastMassCalculator.LegacyScale(massEstimate);

            if(enableGravityMorphs.val)
            {
                gravityMorphH.Update(roll, pitch, scaleVal, gravityLogAmount);
            }

            // TODO properly disable on uncheck
            positionDiff = Vector3.zero;
            if(enableForceMorphs.val)
            {
                positionDiff = neutralPos - Calc.RelativePosition(chest, rNipple.position);
            }
            relativePosMorphH.Update(positionDiff, scaleVal, Const.ConvertToLegacyVal(softness.val));

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

        public IEnumerator RefreshNeutralBreastPosition()
        {
            neutralBreastPositionRefreshDone = false;
            containingAtom.SetFreezePhysics(true);

            Vector3 diff = neutralPos - Calc.RelativePosition(chest, rNipple.position);

            while(diff != Vector3.zero)
            {
                yield return new WaitForSeconds(0.1f);
                neutralPos = Calc.RelativePosition(chest, rNipple.position);
            }

            containingAtom.SetFreezePhysics(false);
            neutralBreastPositionRefreshDone = true;
        }

        public IEnumerator RefreshStaticPhysics(Action callback = null)
        {
            staticPhysicsRefreshDone = false;
            float atomScale = atomScaleListener.Value;
            while(breastMorphListener.Changed())
            {
                yield return null;
            }

            // Iterate the update a few times because each update changes breast shape and thereby the mass estimate.
            for(int i = 0; i < 10; i++)
            {
                // update only non-soft physics settings to improve performance
                UpdateMassEstimate(atomScale);
                staticPhysicsH.UpdateMainPhysics(massEstimate, softness.val);
                if(i > 0)
                {
                    yield return new WaitForSeconds(0.10f);
                }
            }

            UpdateMassEstimate(atomScale, updateUIStatus: true);
            staticPhysicsH.FullUpdate(massEstimate, softness.val, nippleErection.val);

            staticPhysicsRefreshDone = true;
            callback?.Invoke();
        }

        public void RefreshRateDependentPhysics()
        {
            staticPhysicsH.UpdateRateDependentPhysics(massEstimate, softness.val);
        }

        private void UpdateMassEstimate(float atomScale, bool updateUIStatus = false)
        {
            float mass = breastMassCalculator.Calculate(atomScale);

            if(mass > Const.MASS_MAX)
            {
                massEstimate = Const.MASS_MAX;
                if(updateUIStatus)
                {
                    float excess = Calc.RoundToDecimals(mass - Const.MASS_MAX, 1000f);
                    statusUIText.SetVal(MassExcessStatus(excess));
                }
            }
            else if(mass < Const.MASS_MIN)
            {
                massEstimate = Const.MASS_MIN;
                if(updateUIStatus)
                {
                    float shortage = Calc.RoundToDecimals(Const.MASS_MIN - mass, 1000f);
                    statusUIText.SetVal(MassShortageStatus(shortage));
                }
            }
            else
            {
                massEstimate = mass;
                if(updateUIStatus)
                {
                    statusUIText.SetVal("");
                }
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
            if(modeChooser.val != Const.MODES.Values.First())
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

        private void OnDestroy()
        {
            Destroy(settingsMonitor);
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
            float effX = Mathf.InverseLerp(0, 0.080f, Mathf.Abs(positionDiff.x));
            float effY = Mathf.InverseLerp(0, 0.080f, Mathf.Abs(positionDiff.y));
            float effZ = Mathf.InverseLerp(0, 0.080f, Mathf.Abs(positionDiff.z));
            baseDebugInfo.SetVal(
                //$"{Formatting.NameValueString("Roll", roll, 100f, 15)}\n" +
                //$"{Formatting.NameValueString("Pitch", pitch, 100f, 15)}\n" +
                //$"{breastMassCalculator.GetStatus(atomScaleListener.Value)}" +
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
