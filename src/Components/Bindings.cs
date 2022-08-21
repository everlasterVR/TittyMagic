using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TittyMagic.UI;
using UnityEngine;
using static TittyMagic.Script;

namespace TittyMagic
{
    internal class Bindings : MonoBehaviour
    {
        public void Init(List<object> bindings)
        {
            bindings.Add(new Dictionary<string, string>
            {
                { "Namespace", nameof(TittyMagic) },
            });
            bindings.AddRange(new List<object>
            {
                new JSONStorableAction("OpenUI", OpenUI),
                new JSONStorableAction("OpenUI_Control", OpenUIControl),
                new JSONStorableAction("OpenUI_ConfigureHardColliders", OpenUIConfigureHardColliders),
                new JSONStorableAction("OpenUI_PhysicsParams", OpenUIPhysicsParams),
                new JSONStorableAction("OpenUI_MorphMultipliers", OpenUIMorphMultipliers),
                new JSONStorableAction("OpenUI_GravityMultipliers", OpenUIGravityMultipliers),
                new JSONStorableAction("AutoUpdateMassOn", () => tittyMagic.autoUpdateJsb.val = true),
                new JSONStorableAction("AutoUpdateMassOff", () => tittyMagic.autoUpdateJsb.val = false),
                new JSONStorableAction("CalculateBreastMass", tittyMagic.calculateBreastMass.actionCallback),
                new JSONStorableAction("RecalibratePhysics", tittyMagic.recalibratePhysics.actionCallback),
            });
        }

        public void OpenUI()
        {
            ShowMainHud();
            StartCoroutine(SelectPluginUI());
        }

        private void OpenUIControl()
        {
            ShowMainHud();
            StartCoroutine(SelectPluginUI(postAction: tittyMagic.NavigateToMainWindow));
        }

        private void OpenUIPhysicsParams()
        {
            ShowMainHud();
            StartCoroutine(SelectPluginUI(postAction: tittyMagic.NavigateToPhysicsWindow));
        }

        private void OpenUIMorphMultipliers()
        {
            ShowMainHud();
            StartCoroutine(SelectPluginUI(postAction: tittyMagic.NavigateToMorphingWindow));
        }

        private void OpenUIGravityMultipliers()
        {
            ShowMainHud();
            StartCoroutine(SelectPluginUI(postAction: tittyMagic.NavigateToGravityWindow));
        }

        private void OpenUIConfigureHardColliders()
        {
            ShowMainHud();
            StartCoroutine(SelectPluginUI(postAction: () =>
            {
                tittyMagic.NavigateToMainWindow();
                var hardCollidersWindow = tittyMagic.mainWindow.GetActiveNestedWindow() as HardCollidersWindow;
                if(hardCollidersWindow == null)
                {
                    tittyMagic.configureHardColliders.actionCallback();
                }
            }));
        }

        private void ShowMainHud()
        {
            SuperController.singleton.SelectController(tittyMagic.containingAtom.freeControllers.First(), false, false);
            SuperController.singleton.ShowMainHUDMonitor();
        }

        // adapted from Timeline v4.3.1 (c) acidbubbles
        private IEnumerator SelectPluginUI(Action postAction = null)
        {
            if(SuperController.singleton.gameMode != SuperController.GameMode.Edit)
            {
                SuperController.singleton.gameMode = SuperController.GameMode.Edit;
            }

            float time = 0f;
            while(time < 1f)
            {
                time += Time.unscaledDeltaTime;
                yield return null;

                var selector = tittyMagic.containingAtom.gameObject.GetComponentInChildren<UITabSelector>();
                if(selector == null)
                {
                    continue;
                }

                selector.SetActiveTab("Plugins");
                if(tittyMagic.UITransform == null)
                {
                    Utils.LogError("No UI", nameof(Bindings));
                }

                if(tittyMagic.enabled)
                {
                    tittyMagic.UITransform.gameObject.SetActive(true);
                    postAction?.Invoke();
                }

                yield break;
            }
        }
    }
}
