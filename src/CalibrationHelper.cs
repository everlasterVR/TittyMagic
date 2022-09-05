// ReSharper disable MemberCanBeMadeStatic.Global
using System;
using System.Collections;
using System.Linq;
using TittyMagic.UI;
using UnityEngine;
using static TittyMagic.Script;

namespace TittyMagic
{
    public class CalibrationHelper : MonoBehaviour
    {
        private static readonly string _uid = tittyMagic.containingAtom.uid;
        public bool isWaiting;
        public bool shouldRun;
        public bool isInProgress;
        public bool isQueued;
        public bool isCancelling;
        private bool? _wasFrozen;

        private JSONStorableBool _calibrationLockJsb;
        public const string CALIBRATION_LOCK = "calibrationLock";

        public void Init()
        {
            _calibrationLockJsb = tittyMagic.NewJSONStorableBool(CALIBRATION_LOCK, false);
        }

        public bool IsBlockedByInput()
        {
            var mainWindow = tittyMagic.tabs.activeWindow as MainWindow;
            if(mainWindow != null)
            {
                return mainWindow.GetSlidersForRefresh().Any(slider => slider.PointerIsDown());
            }

            return false;
        }

        public IEnumerator Begin()
        {
            isWaiting = true;
            shouldRun = false;

            if(isInProgress)
            {
                if(!isQueued && !IsBlockedByInput())
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

        public IEnumerator DeferFreezeAnimation()
        {
            while(OtherCalibrationInProgress())
            {
                yield return new WaitForSeconds(0.1f);
            }

            _calibrationLockJsb.val = true;
            if(_wasFrozen == null)
            {
                _wasFrozen = _wasFrozen ?? Utils.AnimationIsFrozen();
                SuperController.singleton.SetFreezeAnimation(true);
            }
        }

        private static bool OtherCalibrationInProgress()
        {
            try
            {
                foreach(var instance in Integration.otherInstances)
                {
                    if(instance != null)
                    {
                        bool response = instance.GetBoolParamValue(CALIBRATION_LOCK);
                        if(response)
                        {
                            /* Another instance is currently calibrating. */
                            return true;
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Utils.LogError($"{_uid}: Error checking if other plugins are calibrating {e}");
            }

            /* No other instance is currently calibrating. */
            return false;
        }

        public IEnumerator WaitForListeners()
        {
            yield return new WaitForSeconds(0.33f);

            while(BreastMorphListener.ChangeWasDetected() || IsBlockedByInput())
            {
                yield return new WaitForSeconds(0.1f);
            }

            yield return new WaitForSeconds(0.1f);
        }

        public IEnumerator WaitAndRepeat(Action callback, int times, float intervalWait)
        {
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
                SuperController.singleton.SetFreezeAnimation(_wasFrozen ?? false);
                _wasFrozen = null;
                _calibrationLockJsb.val = false;
            }

            if(envIsDevelopment)
            {
                Utils.LogMessage("Calibration done");
            }

            isInProgress = false;
        }
    }
}
