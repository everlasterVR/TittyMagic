using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static TittyMagic.Utils;

namespace TittyMagic
{
    internal class Bindings : MonoBehaviour
    {
        private Script script;

        public Dictionary<string, string> Settings { get; set; }

        public List<object> OnKeyDownActions { get; set; }
        public JSONStorableAction OpenUIAction { get; set; }

        public void Init(Script script)
        {
            this.script = script;
            Settings = new Dictionary<string, string>
            {
                { "Namespace", nameof(TittyMagic) }
            };
            OnKeyDownActions = new List<object>()
            {
                OpenUI(),
            };
        }

        private object OpenUI()
        {
            OpenUIAction = new JSONStorableAction(nameof(OpenUI), () => ShowUI(() => StartCoroutine(SelectPluginUICo())));
            return OpenUIAction;
        }

        private void ShowUI(Action callback = null)
        {
            SuperController.singleton.SelectController(script.containingAtom.freeControllers.First(), false, false, true);
            SuperController.singleton.ShowMainHUDMonitor();
            callback?.Invoke();
        }

        //adapted from Timeline v4.3.1 (c) acidbubbles
        private IEnumerator SelectPluginUICo()
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

                var selector = script.containingAtom.gameObject.GetComponentInChildren<UITabSelector>();
                if(selector == null)
                {
                    continue;
                }

                selector.SetActiveTab("Plugins");
                if(script.UITransform == null)
                {
                    LogError($"No UI", nameof(Bindings));
                }

                if(script.enabled)
                {
                    script.UITransform.gameObject.SetActive(true);
                }
                yield break;
            }
        }
    }
}
