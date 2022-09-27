// ReSharper disable MemberCanBeMadeStatic.Global
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TittyMagic.UI;
using UnityEngine;
using static TittyMagic.Script;

namespace TittyMagic
{
    public class CalibrationHelper : MonoBehaviour
    {
        private static readonly string _uid = tittyMagic.containingAtom.uid;
        public bool shouldRun;
        public bool isInProgress;
        public bool isQueued;
        public bool isCancelling;
        private bool? _wasFrozen;
        private Dictionary<Guid, Dictionary<string, bool>> _saveCollisionEnabled;

        private JSONStorableBool _calibrationLockJsb;
        public const string CALIBRATION_LOCK = "calibrationLock";

        public void Init()
        {
            _calibrationLockJsb = tittyMagic.NewJSONStorableBool(CALIBRATION_LOCK, false);
            _saveCollisionEnabled = new Dictionary<Guid, Dictionary<string, bool>>();
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

        private static IEnumerable<PhysicsSimulator> _breastSimulators = tittyMagic.containingAtom.physicsSimulators
            .Where(simulator => new[]
            {
                "AutoColliderFemaleAutoColliderslPectoral1",
                "AutoColliderFemaleAutoColliderslPectoral2",
                "AutoColliderFemaleAutoColliderslPectoral3",
                "AutoColliderFemaleAutoColliderslPectoral4",
                "AutoColliderFemaleAutoColliderslPectoral5",
                "AutoColliderFemaleAutoColliderslNipple1",
                "AutoColliderFemaleAutoColliderslNippleGPU",
                "AutoColliderFemaleAutoCollidersrPectoral1",
                "AutoColliderFemaleAutoCollidersrPectoral2",
                "AutoColliderFemaleAutoCollidersrPectoral3",
                "AutoColliderFemaleAutoCollidersrPectoral4",
                "AutoColliderFemaleAutoCollidersrPectoral5",
                "AutoColliderFemaleAutoCollidersrNipple1",
                "AutoColliderFemaleAutoCollidersrNippleGPU",
            }.Contains(simulator.name));

        private static IEnumerable<PhysicsSimulatorJSONStorable> _breastSimulatorStorables = tittyMagic.containingAtom.physicsSimulatorsStorable
            .Where(storable => new[]
            {
                "rNippleControl",
                "lNippleControl",
                "BreastPhysicsMesh",
            }.Contains(storable.name));

        public void SetBreastsCollisionEnabled(bool value, Guid guid)
        {
            if(!value)
            {
                try
                {
                    _saveCollisionEnabled[guid] = new Dictionary<string, bool>();
                    foreach(var simulator in _breastSimulators)
                    {
                        _saveCollisionEnabled[guid][simulator.name] = simulator.collisionEnabled;
                        simulator.collisionEnabled = false;
                    }

                    foreach(var storable in _breastSimulatorStorables)
                    {
                        _saveCollisionEnabled[guid][storable.name] = storable.collisionEnabled;
                        storable.collisionEnabled = false;
                    }
                }
                catch(Exception e)
                {
                    Utils.LogError($"Error disabling breasts collision: {e}");
                }
            }
            else
            {
                try
                {
                    foreach(var simulator in _breastSimulators)
                    {
                        simulator.collisionEnabled = _saveCollisionEnabled[guid][simulator.name];
                    }

                    foreach(var storable in _breastSimulatorStorables)
                    {
                        storable.collisionEnabled = _saveCollisionEnabled[guid][storable.name];
                    }

                    _saveCollisionEnabled.Remove(guid);
                }
                catch(Exception e)
                {
                    Utils.LogError($"Error enabling breasts collision: {e}");

                }
            }
        }

        public void Finish()
        {
            if(!isQueued)
            {
                tittyMagic.settingsMonitor.enabled = true;
                SuperController.singleton.SetFreezeAnimation(_wasFrozen ?? false);
                _wasFrozen = null;
                _calibrationLockJsb.val = false;
            }

            isInProgress = false;
        }

        public static void Destroy()
        {
            _breastSimulators = null;
            _breastSimulatorStorables = null;
        }
    }
}
