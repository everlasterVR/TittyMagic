// ReSharper disable MemberCanBeMadeStatic.Global
using System;
using System.Collections;
using System.Linq;
using TittyMagic.UI;
using UnityEngine;
using static TittyMagic.Script;

namespace TittyMagic
{
    internal class CalibrationHelper : MonoBehaviour
    {
        public bool isWaiting;
        public bool shouldRun;
        public bool isInProgress;
        public bool isQueued;
        public bool isCancelling;
        private bool? _wasFrozenBefore;

        private static bool isBlocked => ((MainWindow) tittyMagic.mainWindow)
            .GetSlidersForRefresh()
            .Any(slider => slider.PointerIsDown());

        public IEnumerator Begin()
        {
            isWaiting = true;
            shouldRun = false;

            if(isInProgress)
            {
                if(!isQueued && !isBlocked)
                {
                    isQueued = true;
                }
                else
                {
                    isCancelling = true;
                    yield break;
                }
            }

            tittyMagic.settingsMonitor.SetEnabled(false);

            while(isInProgress)
            {
                yield return null;
            }

            isQueued = false;
            isInProgress = true;
        }

        public void FreezeAnimation()
        {
            bool mainToggleFrozen =
                SuperController.singleton.freezeAnimationToggle != null &&
                SuperController.singleton.freezeAnimationToggle.isOn;
            bool altToggleFrozen =
                SuperController.singleton.freezeAnimationToggleAlt != null &&
                SuperController.singleton.freezeAnimationToggleAlt.isOn;

            if(_wasFrozenBefore == null)
            {
                _wasFrozenBefore = _wasFrozenBefore ?? mainToggleFrozen || altToggleFrozen;
                SuperController.singleton.SetFreezeAnimation(true);
            }
        }

        public IEnumerator WaitForListeners()
        {
            yield return new WaitForSeconds(0.33f);

            while(BreastMorphListener.ChangeWasDetected() || isBlocked)
            {
                yield return new WaitForSeconds(0.1f);
            }

            yield return new WaitForSeconds(0.1f);
        }

        /* Applies the same consistent delay regardless of whether callback invoke is actually updated */
        public IEnumerator WaitAndRepeat(Action callback, int times, float intervalWait = 0.1f, float initialWait = 0f)
        {
            yield return new WaitForSeconds(initialWait);
            int count = 0;
            while(count < times)
            {
                count++;
                yield return new WaitForSeconds(intervalWait);
                callback.Invoke();
            }
        }

        public void Finish()
        {
            if(!isQueued)
            {
                tittyMagic.settingsMonitor.SetEnabled(true);
                isWaiting = false;
                SuperController.singleton.SetFreezeAnimation(_wasFrozenBefore ?? false);
                _wasFrozenBefore = null;
            }

            isInProgress = false;
        }
    }
}
