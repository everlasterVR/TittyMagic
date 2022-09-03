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
                new JSONStorableAction("AutoUpdateMassOn", () => StartCoroutine(DeferSetAutoUpdateMass(true))),
                new JSONStorableAction("AutoUpdateMassOff", () => StartCoroutine(DeferSetAutoUpdateMass(false))),
                new JSONStorableAction("CalculateBreastMass", tittyMagic.calculateBreastMass.actionCallback),
                new JSONStorableAction("RecalibratePhysics", tittyMagic.recalibratePhysics.actionCallback),
            });
            if(envIsDevelopment)
            {
                bindings.AddRange(new List<object>
                {
                    new JSONStorableAction("OpenUI_Dev", OpenUIDev),
                });
            }
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
                var mainWindow = (MainWindow) tittyMagic.tabs.activeWindow;
                var hardCollidersWindow = mainWindow.GetActiveNestedWindow() as HardCollidersWindow;
                if(hardCollidersWindow == null)
                {
                    mainWindow.configureHardColliders.actionCallback();
                }
            }));
        }

        private void OpenUIDev()
        {
            ShowMainHud();
            StartCoroutine(SelectPluginUI(postAction: () =>
            {
                tittyMagic.NavigateToMainWindow();
                var mainWindow = (MainWindow) tittyMagic.tabs.activeWindow;
                var devWindow = mainWindow.GetActiveNestedWindow() as DevWindow;
                if(devWindow == null)
                {
                    mainWindow.openDevWindow.actionCallback();
                }
            }));
        }

        private static void ShowMainHud()
        {
            SuperController.singleton.SelectController(tittyMagic.containingAtom.freeControllers.First(), false, false);
            SuperController.singleton.ShowMainHUDMonitor();
        }

        // adapted from Timeline v4.3.1 (c) acidbubbles
        private static IEnumerator SelectPluginUI(Action postAction = null)
        {
            while(!tittyMagic.isInitialized)
            {
                yield return null;
            }

            if(tittyMagic.UITransform != null && tittyMagic.UITransform.gameObject.activeInHierarchy)
            {
                if(tittyMagic.enabled)
                {
                    postAction?.Invoke();
                }

                yield break;
            }

            if(SuperController.singleton.gameMode != SuperController.GameMode.Edit)
            {
                SuperController.singleton.gameMode = SuperController.GameMode.Edit;
            }

            SuperController.singleton.SelectController(tittyMagic.containingAtom.mainController, false, false);
            SuperController.singleton.ShowMainHUDAuto();

            float timeout = Time.unscaledTime + 1;
            while(Time.unscaledTime < timeout)
            {
                yield return null;

                var selector = tittyMagic.containingAtom.gameObject.GetComponentInChildren<UITabSelector>();
                if(selector == null)
                {
                    continue;
                }

                selector.SetActiveTab("Plugins");
                if(tittyMagic.UITransform == null)
                {
                    continue;
                }

                /* Close any currently open plugin UI before opening this plugin's UI */
                foreach(Transform scriptController in tittyMagic.manager.pluginContainer)
                {
                    var script = scriptController.gameObject.GetComponent<MVRScript>();
                    if(script != null && script != tittyMagic)
                    {
                        script.UITransform.gameObject.SetActive(false);
                    }
                }

                if(tittyMagic.enabled)
                {
                    tittyMagic.UITransform.gameObject.SetActive(true);
                    postAction?.Invoke();
                    yield break;
                }
            }
        }

        private static IEnumerator DeferSetAutoUpdateMass(bool value)
        {
            while(!tittyMagic.isInitialized)
            {
                yield return null;
            }

            tittyMagic.autoUpdateJsb.val = value;
        }
    }
}
