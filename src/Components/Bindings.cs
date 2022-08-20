using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TittyMagic.UI;
using UnityEngine;

namespace TittyMagic
{
    internal class Bindings : MonoBehaviour
    {
        private Script _script;

        public void Init(Script script, List<object> bindings)
        {
            _script = script;
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
                new JSONStorableAction("AutoUpdateMassOn", () => _script.autoUpdateJsb.val = true),
                new JSONStorableAction("AutoUpdateMassOff", () => _script.autoUpdateJsb.val = false),
                new JSONStorableAction("CalculateBreastMass", _script.calculateBreastMass.actionCallback),
                new JSONStorableAction("RecalibratePhysics", _script.recalibratePhysics.actionCallback),
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
            StartCoroutine(SelectPluginUI(postAction: _script.NavigateToMainWindow));
        }

        private void OpenUIPhysicsParams()
        {
            ShowMainHud();
            StartCoroutine(SelectPluginUI(postAction: _script.NavigateToPhysicsWindow));
        }

        private void OpenUIMorphMultipliers()
        {
            ShowMainHud();
            StartCoroutine(SelectPluginUI(postAction: _script.NavigateToMorphingWindow));
        }

        private void OpenUIGravityMultipliers()
        {
            ShowMainHud();
            StartCoroutine(SelectPluginUI(postAction: _script.NavigateToGravityWindow));
        }

        private void OpenUIConfigureHardColliders()
        {
            ShowMainHud();
            StartCoroutine(SelectPluginUI(postAction: () =>
            {
                _script.NavigateToMainWindow();
                var hardCollidersWindow = _script.mainWindow.GetActiveNestedWindow() as HardCollidersWindow;
                if(hardCollidersWindow == null)
                {
                    _script.configureHardColliders.actionCallback();
                }
            }));
        }

        private void ShowMainHud()
        {
            SuperController.singleton.SelectController(_script.containingAtom.freeControllers.First(), false, false);
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

                var selector = _script.containingAtom.gameObject.GetComponentInChildren<UITabSelector>();
                if(selector == null)
                {
                    continue;
                }

                selector.SetActiveTab("Plugins");
                if(_script.UITransform == null)
                {
                    Utils.LogError("No UI", nameof(Bindings));
                }

                if(_script.enabled)
                {
                    _script.UITransform.gameObject.SetActive(true);
                    postAction?.Invoke();
                }

                yield break;
            }
        }
    }
}
