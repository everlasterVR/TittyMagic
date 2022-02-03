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
        private Script _script;

        public Dictionary<string, string> Settings { get; set; }

        public List<object> OnKeyDownActions { get; set; }
        public JSONStorableAction OpenUIAction { get; set; }

        public void Init(Script script)
        {
            _script = script;
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
            SuperController.singleton.SelectController(_script.containingAtom.freeControllers.First(), false, false, true);
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

                var selector = _script.containingAtom.gameObject.GetComponentInChildren<UITabSelector>();
                if(selector == null)
                {
                    continue;
                }

                selector.SetActiveTab("Plugins");
                if(_script.UITransform == null)
                {
                    LogError($"No UI", nameof(Bindings));
                }

                if(_script.enabled)
                {
                    _script.UITransform.gameObject.SetActive(true);
                }
                yield break;
            }
        }
    }
}
