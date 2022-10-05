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
        public const string CALIBRATION_LOCK = "calibrationLock";

        public bool shouldRun;
        public bool isInProgress;
        public bool isQueued;
        public bool isCancelling;
        private bool? _isGlobalFrozen;
        private bool? _isPlaying;
        private MotionAnimationMaster _motionAnimationMaster;

        private JSONStorableBool _calibrationLockJsb;
        public JSONStorableBool autoUpdateJsb { get; private set; }
        public JSONStorableBool freezeMotionSoundJsb { get; private set; }
        public JSONStorableBool pauseSceneAnimationJsb { get; private set; }
        public JSONStorableBool disableBreastCollisionJsb { get; private set; }

        public void Init()
        {
            _motionAnimationMaster = SuperController.singleton.motionAnimationMaster;
            _calibrationLockJsb = tittyMagic.NewJSONStorableBool(CALIBRATION_LOCK, false);

            freezeMotionSoundJsb = tittyMagic.NewJSONStorableBool("freezeMotionSoundWhenCalibrating", true);
            freezeMotionSoundJsb.setCallbackFunction = value =>
            {
                if(value)
                {
                    pauseSceneAnimationJsb.val = false;
                }

                var optionsWindow = tittyMagic.tabs.activeWindow.GetActiveNestedWindow() as OptionsWindow;
                if(optionsWindow != null)
                {
                    optionsWindow.UpdatePauseSceneAnimationToggleStyle();
                }
            };

            pauseSceneAnimationJsb = tittyMagic.NewJSONStorableBool("pauseSceneAnimationWhenCalibrating", false);
            pauseSceneAnimationJsb.setCallbackFunction = value =>
            {
                if(value && freezeMotionSoundJsb.val)
                {
                    pauseSceneAnimationJsb.valNoCallback = false;
                }
            };

            disableBreastCollisionJsb = tittyMagic.NewJSONStorableBool("disableBreastCollisionCalibrating", true);

            autoUpdateJsb = tittyMagic.NewJSONStorableBool("autoUpdateMass", true);
            autoUpdateJsb.setCallbackFunction = value =>
            {
                if(value)
                {
                    tittyMagic.StartCalibration(calibratesMass: true);
                }
            };
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

        public IEnumerator DeferPauseAnimation()
        {
            while(OtherCalibrationInProgress())
            {
                yield return new WaitForSeconds(0.1f);
            }

            _calibrationLockJsb.val = true;

            if(pauseSceneAnimationJsb.val && _isPlaying == null)
            {
                _isPlaying = _motionAnimationMaster.activeWhilePlaying.activeSelf;
                if(_isPlaying.Value)
                {
                    _motionAnimationMaster.StopPlayback();
                }
            }

            if(freezeMotionSoundJsb.val && _isGlobalFrozen == null)
            {
                _isGlobalFrozen = Utils.GlobalAnimationIsFrozen();
                if(!_isGlobalFrozen.Value)
                {
                    SuperController.singleton.SetFreezeAnimation(true);
                }
            }
        }

        private static bool OtherCalibrationInProgress()
        {
            try
            {
                foreach(var instance in Integration.otherInstances)
                {
                    if(instance != null && instance.GetBoolParamNames().Contains(CALIBRATION_LOCK))
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

        private readonly IEnumerable<PhysicsSimulator> _breastSimulators = tittyMagic.containingAtom.physicsSimulators
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

        private readonly IEnumerable<PhysicsSimulatorJSONStorable> _breastSimulatorStorables = tittyMagic.containingAtom.physicsSimulatorsStorable
            .Where(storable => new[]
            {
                "rNippleControl",
                "lNippleControl",
                "BreastPhysicsMesh",
            }.Contains(storable.name));

        private readonly Dictionary<Guid, Dictionary<string, bool>> _saveCollisionEnabled = new Dictionary<Guid, Dictionary<string, bool>>();

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
                if(_isGlobalFrozen.HasValue)
                {
                    if(!_isGlobalFrozen.Value)
                    {
                        SuperController.singleton.SetFreezeAnimation(false);
                    }

                    _isGlobalFrozen = null;
                }

                if(_isPlaying.HasValue)
                {
                    if(_isPlaying.Value)
                    {
                        _motionAnimationMaster.StartPlayback();
                    }

                    _isPlaying = null;
                }

                _calibrationLockJsb.val = false;
            }

            isInProgress = false;
        }
    }
}
