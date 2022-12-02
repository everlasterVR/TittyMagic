using System;
using System.Collections;
using TittyMagic.Components;
using UnityEngine;
using static TittyMagic.Script;

namespace TittyMagic
{
    public class UIModManager : MonoBehaviour
    {
        private Transform _atomUIContent;

        public JSONStorableBool skinMaterials2Modified { get; private set; }

        private UIMod _mPectoralPhysicsMod;
        private UIMod _fBreastPhysics1Mod;
        private UIMod _fBreastPhysics2Mod;
        private UIMod _fBreastPresetsMod;
        private UIMod _skinMaterials2FrictionMod;
        private UIMod _skinMaterials2MoveBtnGroup;
        private UIMod _skinMaterials2AddBtnMod;

        private static bool _initialized;

        public void ModifyAtomUI()
        {
            _atomUIContent = tittyMagic.containingAtom.transform.Find("UI/UIPlaceHolderModel/UIModel/Canvas/Panel/Content");

            tittyMagic.NewJSONStorableAction(Constant.SHOW_FRICTION_UI, ShowFrictionUI);
            tittyMagic.NewJSONStorableAction(Constant.HIDE_FRICTION_UI, HideFrictionUI);
            skinMaterials2Modified = tittyMagic.NewJSONStorableBool(Constant.SKIN_MATERIALS_2_FRICTION + "Modified", false);

            _mPectoralPhysicsMod = NewUIMod(Constant.M_PECTORAL_PHYSICS, "M Pectoral Physics", ReplaceWithPluginUIButton);
            _fBreastPhysics1Mod = NewUIMod(Constant.F_BREAST_PHYSICS_1, "F Breast Physics 1", ReplaceWithPluginUIButton);
            _fBreastPhysics2Mod = NewUIMod(Constant.F_BREAST_PHYSICS_2, "F Breast Physics 2", ReplaceWithPluginUIButton);
            _fBreastPresetsMod = NewUIMod(Constant.F_BREAST_PRESETS, "F Breast Presets", ReplaceWithPluginUIButton);
            _skinMaterials2FrictionMod = NewUIMod(Constant.SKIN_MATERIALS_2_FRICTION, "Skin Materials 2", CreateFrictionUI);
            _skinMaterials2MoveBtnGroup = NewUIMod(Constant.SKIN_MATERIALS_2_MOVE_BTN_GROUP, "Skin Materials 2", MoveSkinMaterialsUIButtonGroup);
            _skinMaterials2AddBtnMod = NewUIMod(Constant.SKIN_MATERIALS_2_ADD_BTN, "Skin Materials 2", AddButtonToSkinMaterialsUI);

            if(tittyMagic.enabled)
            {
                _mPectoralPhysicsMod.Apply();
                _fBreastPhysics1Mod.Apply();
                _fBreastPhysics2Mod.Apply();
                _fBreastPresetsMod.Apply();
                _skinMaterials2FrictionMod.Apply();
                _skinMaterials2MoveBtnGroup.Apply();
                _skinMaterials2AddBtnMod.Apply();
            }

            _initialized = true;
        }

        private UIMod NewUIMod(string uiModId, string targetName, Func<UIMod, IEnumerator> changesFunc)
        {
            var uiMod = tittyMagic.gameObject.AddComponent<UIMod>();
            uiMod.Init(uiModId, _atomUIContent, targetName, changesFunc);
            return uiMod;
        }

        private static IEnumerator ReplaceWithPluginUIButton(UIMod uiMod)
        {
            while(tittyMagic.bindings == null)
            {
                yield return null;
            }

            try
            {
                uiMod.InactivateChildren();
                uiMod.AddCustomObject(OpenPluginUIButton(uiMod.target).gameObject);
            }
            catch(Exception e)
            {
                Utils.LogError($"Error modifying {uiMod.id} UI: {e}");
            }
        }

        private UIDynamicToggle enableAdaptiveFrictionToggle { get; set; }
        public UIDynamicSlider drySkinFrictionSlider { get; private set; }

        private IEnumerator CreateFrictionUI(UIMod uiMod)
        {
            if(!FrictionHandler.enabled)
            {
                yield break;
            }

            try
            {
                var leftSide = uiMod.target.Find("LeftSide");
                var rightSide = uiMod.target.Find("RightSide");

                /* Collider friction title */
                {
                    var fieldTransform = Utils.DestroyLayout(tittyMagic.InstantiateTextField(leftSide));
                    var rectTransform = fieldTransform.GetComponent<RectTransform>();
                    rectTransform.pivot = new Vector2(0, 0);
                    rectTransform.anchoredPosition = new Vector2(20f, -930);
                    rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x + 10, 100);
                    uiMod.AddCustomObject(fieldTransform.gameObject);

                    var uiDynamic = fieldTransform.GetComponent<UIDynamicTextField>();
                    uiDynamic.UItext.alignment = TextAnchor.LowerCenter;
                    uiDynamic.text = $"{nameof(TittyMagic)} Collider Friction".Size(32).Bold();
                    uiDynamic.backgroundColor = Color.clear;
                    uiDynamic.textColor = Color.white;
                }

                /* Collider friction info text area */
                {
                    var fieldTransform = Utils.DestroyLayout(tittyMagic.InstantiateTextField(leftSide));
                    var rectTransform = fieldTransform.GetComponent<RectTransform>();
                    rectTransform.pivot = new Vector2(0, 0);
                    rectTransform.anchoredPosition = new Vector2(20, -1290);
                    rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x + 10, 400);
                    uiMod.AddCustomObject(fieldTransform.gameObject);

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

                /* Adaptive friction toggle */
                {
                    var customTransform = CustomToggle(rightSide, new Vector2(0, -880));
                    uiMod.AddCustomObject(customTransform.gameObject);

                    enableAdaptiveFrictionToggle = customTransform.GetComponent<UIDynamicToggle>();
                    var storable = FrictionHandler.adaptiveFrictionJsb;
                    storable.toggle = enableAdaptiveFrictionToggle.toggle;
                    tittyMagic.AddToggleToJsb(enableAdaptiveFrictionToggle, storable);
                    enableAdaptiveFrictionToggle.label = "Use Adaptive Friction";
                }

                /* Dry skin friction slider */
                {
                    var customTransform = CustomSlider(rightSide, new Vector2(0, -1020));
                    uiMod.AddCustomObject(customTransform.gameObject);

                    drySkinFrictionSlider = customTransform.GetComponent<UIDynamicSlider>();
                    var storable = FrictionHandler.drySkinFrictionJsf;
                    drySkinFrictionSlider.Configure(storable.name,
                        storable.min,
                        storable.max,
                        storable.defaultVal,
                        storable.constrained,
                        valFormat: "F3"
                    );
                    storable.slider = drySkinFrictionSlider.slider;
                    tittyMagic.AddSliderToJsf(drySkinFrictionSlider, storable);
                    drySkinFrictionSlider.label = "Dry Skin Friction";
                    drySkinFrictionSlider.SetActiveStyle(FrictionHandler.adaptiveFrictionJsb.val, true);
                }

                /* Friction offset slider */
                {
                    var customTransform = CustomSlider(rightSide, new Vector2(0, -1160));
                    uiMod.AddCustomObject(customTransform.gameObject);

                    var uiDynamic = customTransform.GetComponent<UIDynamicSlider>();
                    var storable = FrictionHandler.frictionOffsetJsf;
                    uiDynamic.Configure(storable.name, storable.min, storable.max, storable.defaultVal, storable.constrained, valFormat: "F3");
                    storable.slider = uiDynamic.slider;
                    tittyMagic.AddSliderToJsf(uiDynamic, storable);
                    uiDynamic.label = "Friction Offset";
                }

                /* Soft collider friction value slider */
                {
                    var customTransform = CustomSlider(rightSide, new Vector2(0, -1300));
                    uiMod.AddCustomObject(customTransform.gameObject);

                    var uiDynamic = customTransform.GetComponent<UIDynamicSlider>();
                    var storable = FrictionHandler.softColliderFrictionJsf;
                    uiDynamic.Configure(storable.name, storable.min, storable.max, storable.defaultVal, storable.constrained, valFormat: "F3");
                    storable.slider = uiDynamic.slider;
                    tittyMagic.AddSliderToJsf(uiDynamic, storable);
                    uiDynamic.label = "Friction Value";
                    uiDynamic.SetActiveStyle(false, true);
                }

                if(
                    Integration.bootyMagic != null &&
                    Integration.bootyMagic.GetBoolParamValue(skinMaterials2Modified.name)
                )
                {
                    Integration.bootyMagic.CallActionNullSafe(Constant.HIDE_FRICTION_UI);
                }

                skinMaterials2Modified.val = true;
            }
            catch(Exception e)
            {
                Utils.LogError($"Error modifying {uiMod.id} UI: {e}");
            }
        }

        private static IEnumerator MoveSkinMaterialsUIButtonGroup(UIMod uiMod)
        {
            if(!FrictionHandler.enabled)
            {
                yield break;
            }

            try
            {
                var rightSide = uiMod.target.Find("RightSide");
                uiMod.MoveRect(rightSide.Find("SavePanel"), new Vector2(-440, 70), new Vector2(0, 600));
                uiMod.MoveRect(rightSide.Find("RestorePanel"), new Vector2(-230, 70), new Vector2(0, 600));
                uiMod.MoveRect(rightSide.Find("Reset"), new Vector2(-70, 70), new Vector2(0, 600));
            }
            catch(Exception e)
            {
                Utils.LogError($"Error modifying {uiMod.id} UI: {e}");
            }
        }

        private IEnumerator AddButtonToSkinMaterialsUI(UIMod uiMod)
        {
            if(!FrictionHandler.enabled)
            {
                yield break;
            }

            try
            {
                var leftSide = uiMod.target.Find("LeftSide");

                /* Button */
                {
                    var customTransform = CustomButton(leftSide, new Vector2(20f, -810));
                    uiMod.AddCustomObject(customTransform.gameObject);

                    var uiDynamicButton = customTransform.GetComponent<UIDynamicButton>();
                    uiDynamicButton.button.onClick.AddListener(() =>
                    {
                        Integration.bootyMagic.CallActionNullSafe(Constant.HIDE_FRICTION_UI);
                        ShowFrictionUI();
                    });
                    uiDynamicButton.label = "Switch To TittyMagic";
                }

                uiMod.Disable();
            }
            catch(Exception e)
            {
                Utils.LogError($"Error modifying {uiMod.id}  UI: {e}");
            }
        }

        private static Transform OpenPluginUIButton(Transform parent)
        {
            var button = Utils.DestroyLayout(tittyMagic.InstantiateButton(parent));
            var uiDynamic = button.GetComponent<UIDynamicButton>();
            uiDynamic.label = $"<b>Open {nameof(TittyMagic)} UI</b>";
            uiDynamic.button.onClick.AddListener(() => tittyMagic.bindings.actions["OpenUI"].actionCallback());
            return button;
        }

        private static Transform CustomToggle(Transform parent, Vector2 anchoredPosition)
        {
            var customTransform = Utils.DestroyLayout(tittyMagic.InstantiateToggle(parent));
            var rectTransform = customTransform.GetComponent<RectTransform>();
            rectTransform.pivot = new Vector2(0, 0);
            rectTransform.anchoredPosition = anchoredPosition;
            var sizeDelta = rectTransform.sizeDelta;
            rectTransform.sizeDelta = new Vector2(sizeDelta.x + 10, sizeDelta.y);
            return customTransform;
        }

        private static Transform CustomSlider(Transform parent, Vector2 anchoredPosition)
        {
            var customTransform = Utils.DestroyLayout(tittyMagic.InstantiateSlider(parent));
            var rectTransform = customTransform.GetComponent<RectTransform>();
            rectTransform.pivot = new Vector2(0, 0);
            rectTransform.anchoredPosition = anchoredPosition;
            var sizeDelta = rectTransform.sizeDelta;
            rectTransform.sizeDelta = new Vector2(sizeDelta.x + 10, sizeDelta.y);
            return customTransform;
        }

        private static Transform CustomButton(Transform parent, Vector2 anchoredPosition)
        {
            var customTransform = Utils.DestroyLayout(tittyMagic.InstantiateButton(parent));
            var rectTransform = customTransform.GetComponent<RectTransform>();
            rectTransform.pivot = new Vector2(0, 0);
            rectTransform.anchoredPosition = anchoredPosition;
            var sizeDelta = rectTransform.sizeDelta;
            rectTransform.sizeDelta = new Vector2(sizeDelta.x + 10, sizeDelta.y);
            return customTransform;
        }

        private void ShowFrictionUI()
        {
            if(!enabled)
            {
                return;
            }

            _skinMaterials2FrictionMod.Enable();
            _skinMaterials2AddBtnMod.Disable();
            skinMaterials2Modified.val = true;
        }

        private void HideFrictionUI()
        {
            if(!enabled)
            {
                return;
            }

            _skinMaterials2FrictionMod.Disable();
            _skinMaterials2AddBtnMod.Enable();
            skinMaterials2Modified.val = false;
        }

        public void SetFrictionUIVisibility()
        {
            if(FrictionHandler.enabled)
            {
                ShowFrictionUI();
            }
            else
            {
                HideFrictionUI();
            }
        }

        private void OnEnable()
        {
            if(!_initialized)
            {
                return;
            }

            _mPectoralPhysicsMod.Enable();
            _fBreastPhysics1Mod.Enable();
            _fBreastPhysics2Mod.Enable();
            _fBreastPresetsMod.Enable();

            _skinMaterials2FrictionMod.Enable();
            _skinMaterials2MoveBtnGroup.Enable();
            _skinMaterials2AddBtnMod.Disable();
            skinMaterials2Modified.val = true;
            Integration.bootyMagic.CallActionNullSafe(Constant.HIDE_FRICTION_UI);
        }

        private void OnDisable()
        {
            _mPectoralPhysicsMod.Disable();
            _fBreastPhysics1Mod.Disable();
            _fBreastPhysics2Mod.Disable();
            _fBreastPresetsMod.Disable();

            _skinMaterials2FrictionMod.Disable();
            _skinMaterials2AddBtnMod.Disable();
            skinMaterials2Modified.val = false;
            Integration.bootyMagic.CallActionNullSafe(Constant.SHOW_FRICTION_UI);

            if(Integration.bootyMagic == null || !Integration.bootyMagic.enabled)
            {
                _skinMaterials2MoveBtnGroup.Disable();
            }
        }

        private void OnDestroy()
        {
            Destroy(_mPectoralPhysicsMod);
            Destroy(_fBreastPhysics1Mod);
            Destroy(_fBreastPhysics2Mod);
            Destroy(_fBreastPresetsMod);
            Destroy(_skinMaterials2FrictionMod);
            Destroy(_skinMaterials2MoveBtnGroup);
            Destroy(_skinMaterials2AddBtnMod);
        }
    }
}
