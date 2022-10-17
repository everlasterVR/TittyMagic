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
        private Dictionary<string, string> _namespace;
        public Dictionary<string, string> Namespace() => _namespace;

        public Dictionary<string, JSONStorableAction> actions { get; private set; }

        // ReSharper disable once ReturnTypeCanBeEnumerable.Global
        public List<object> Actions() => actions.Values.Select(action => (object) action).ToList();

        public void Init()
        {
            _namespace = new Dictionary<string, string>
            {
                { "Namespace", nameof(TittyMagic) },
            };
            var jsonStorableActions = new List<JSONStorableAction>
            {
                tittyMagic.NewJSONStorableAction("OpenUI", OpenUI),
                tittyMagic.NewJSONStorableAction("OpenUI_Control", OpenUIControl),
                tittyMagic.NewJSONStorableAction("OpenUI_ConfigureHardColliders", OpenUIConfigureHardColliders),
                tittyMagic.NewJSONStorableAction("OpenUI_ConfigureColliderFriction", OpenUIConfigureColliderFriction),
                tittyMagic.NewJSONStorableAction("OpenUI_Options", OpenUIOptions),
                tittyMagic.NewJSONStorableAction("OpenUI_PhysicsParams", OpenUIPhysicsParams),
                tittyMagic.NewJSONStorableAction("OpenUI_MorphMultipliers", OpenUIMorphMultipliers),
                tittyMagic.NewJSONStorableAction("OpenUI_GravityMultipliers", OpenUIGravityMultipliers),
                new JSONStorableAction("AutoUpdateMassOn", () => StartCoroutine(DeferSetAutoUpdateMass(true))),
                new JSONStorableAction("AutoUpdateMassOff", () => StartCoroutine(DeferSetAutoUpdateMass(false))),
                new JSONStorableAction("CalculateBreastMass", tittyMagic.calculateBreastMass.actionCallback),
                new JSONStorableAction("RecalibratePhysics", tittyMagic.recalibratePhysics.actionCallback),
            };
            if(envIsDevelopment)
            {
                jsonStorableActions.Add(new JSONStorableAction("OpenUI_Dev", OpenUIDev));
                jsonStorableActions.Add(new JSONStorableAction("OpenUI_DevMorph", OpenUIDevMorph));
                jsonStorableActions.Add(new JSONStorableAction("OpenUI_Experimental", OpenUIExperimental));
            }

            actions = jsonStorableActions.ToDictionary(action => action.name, action => action);
        }

        private void OpenUI() =>
            StartCoroutine(SelectPluginUI());

        private void OpenUIControl() =>
            StartCoroutine(SelectPluginUI(postAction: tittyMagic.NavigateToMainWindow));

        private void OpenUIPhysicsParams() =>
            StartCoroutine(SelectPluginUI(postAction: tittyMagic.NavigateToPhysicsWindow));

        private void OpenUIMorphMultipliers() =>
            StartCoroutine(SelectPluginUI(postAction: tittyMagic.NavigateToMorphingWindow));

        private void OpenUIGravityMultipliers() =>
            StartCoroutine(SelectPluginUI(postAction: tittyMagic.NavigateToGravityWindow));

        private void OpenUIConfigureHardColliders()
        {
            OpenUIControl();

            if(!personIsFemale)
            {
                Utils.LogMessage("Hard colliders are only supported on a female character.");
                return;
            }

            var mainWindow = (MainWindow) tittyMagic.tabs.activeWindow;
            OpenNestedWindow<HardCollidersWindow>(mainWindow, mainWindow.configureHardCollidersAction);
        }

        private void OpenUIConfigureColliderFriction()
        {
            if(!personIsFemale)
            {
                Utils.LogMessage("Collider friction is only supported on a female character.");
                return;
            }

            StartCoroutine(SelectContainingAtomTab(tittyMagic.enabled ? "Skin Materials 2" : "Plugins"));
        }

        private void OpenUIDev()
        {
            OpenUIControl();
            var mainWindow = (MainWindow) tittyMagic.tabs.activeWindow;
            OpenNestedWindow<DevWindow>(mainWindow, mainWindow.openDevWindowAction);
        }

        private void OpenUIDevMorph()
        {
            OpenUIControl();
            var mainWindow = (MainWindow) tittyMagic.tabs.activeWindow;
            OpenNestedWindow<DevMorphWindow>(mainWindow, mainWindow.openDevMorphWindowAction);
        }

        private void OpenUIExperimental()
        {
            OpenUIControl();
            var mainWindow = (MainWindow) tittyMagic.tabs.activeWindow;
            OpenNestedWindow<ExperimentalWindow>(mainWindow, mainWindow.openExperimentalWindowAction);
        }

        private void OpenUIOptions()
        {
            OpenUIControl();
            var mainWindow = (MainWindow) tittyMagic.tabs.activeWindow;
            OpenNestedWindow<OptionsWindow>(mainWindow, mainWindow.openOptionsWindowAction);
        }

        private static void OpenNestedWindow<T>(MainWindow mainWindow, JSONStorableAction openAction)
        {
            var nestedWindow = mainWindow.GetActiveNestedWindow();
            if(nestedWindow is T)
            {
                return;
            }

            if(nestedWindow != null)
            {
                nestedWindow.Clear();
            }

            openAction.actionCallback();
        }

        // adapted from Timeline v4.3.1 (c) acidbubbles
        private static IEnumerator SelectPluginUI(Action postAction = null)
        {
            while(!tittyMagic.initialized)
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

            yield return SelectContainingAtomTab("Plugins");

            float timeout = Time.unscaledTime + 1;
            while(Time.unscaledTime < timeout)
            {
                yield return null;

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

        private static IEnumerator SelectContainingAtomTab(string tabName)
        {
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

                selector.SetActiveTab(tabName);
            }
        }

        private static IEnumerator DeferSetAutoUpdateMass(bool value)
        {
            while(!tittyMagic.initialized)
            {
                yield return null;
            }

            tittyMagic.calibrationHelper.autoUpdateJsb.val = value;
        }
    }
}
