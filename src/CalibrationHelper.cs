// ReSharper disable MemberCanBeMadeStatic.Global
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TittyMagic.Handlers;
using TittyMagic.UI;
using UnityEngine;
using static TittyMagic.Script;

namespace TittyMagic
{
    public class CalibrationHelper : MonoBehaviour
    {
        private static readonly string _uid = tittyMagic.containingAtom.uid;

        public bool shouldRun { get; set; }
        public bool cancelling { get; set; }

        private bool _queued;
        private bool? _globalFrozen;
        private bool? _playing;
        private bool _deferToOtherInstance;
        private MotionAnimationMaster _motionAnimationMaster;

        public JSONStorableBool calibratingJsb { get; private set; }
        public JSONStorableBool autoUpdateJsb { get; private set; }
        public JSONStorableBool freezeMotionSoundJsb { get; private set; }
        public JSONStorableBool pauseSceneAnimationJsb { get; private set; }
        public JSONStorableBool disableBreastCollisionJsb { get; private set; }

        public void Init()
        {
            _motionAnimationMaster = SuperController.singleton.motionAnimationMaster;
            calibratingJsb = tittyMagic.NewJSONStorableBool("isCalibrating", false);

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

        public bool BlockedByInput()
        {
            var mainWindow = tittyMagic.tabs.activeWindow as MainWindow;
            if(mainWindow != null)
            {
                return mainWindow.GetSlidersForRefresh().Any(slider => slider.PointerDown());
            }

            return false;
        }

        public IEnumerator Begin()
        {
            shouldRun = false;
            if(calibratingJsb.val)
            {
                if(!_queued && !BlockedByInput())
                {
                    _queued = true;
                }
                else
                {
                    cancelling = true;
                    yield break;
                }
            }

            while(_queued)
            {
                yield return null;
            }

            calibratingJsb.val = true;

            /* The instance which started calibrating first has control over pausing */
            if(OtherCalibrationInProgress())
            {
                _deferToOtherInstance = true;
            }
            else if(pauseSceneAnimationJsb.val && _playing == null)
            {
                _playing = _motionAnimationMaster.activeWhilePlaying.activeSelf;
                if(_playing.Value)
                {
                    _motionAnimationMaster.StopPlayback();
                }
            }
            else if(freezeMotionSoundJsb.val && _globalFrozen == null)
            {
                _globalFrozen = Utils.GlobalAnimationFrozen();
                if(!_globalFrozen.Value)
                {
                    SuperController.singleton.SetFreezeAnimation(true);
                }
            }
        }

        private bool OtherCalibrationInProgress()
        {
            try
            {
                foreach(var instance in Integration.otherInstances)
                {
                    if(instance != null && instance.GetBoolParamNames().Contains(calibratingJsb.name))
                    {
                        bool response = instance.GetBoolParamValue(calibratingJsb.name);
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

            while(BreastMorphListener.ChangeWasDetected() || BlockedByInput())
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
            if(!personIsFemale)
            {
                HardColliderHandler.SetPectoralCollisions(value);
                return;
            }

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

        public IEnumerator DeferFinish()
        {
            if(!_deferToOtherInstance && !_queued)
            {
                while(OtherCalibrationInProgress())
                {
                    yield return new WaitForSecondsRealtime(0.1f);
                }

                if(_globalFrozen.HasValue)
                {
                    if(!_globalFrozen.Value)
                    {
                        SuperController.singleton.SetFreezeAnimation(false);
                    }

                    _globalFrozen = null;
                }

                if(_playing.HasValue)
                {
                    if(_playing.Value)
                    {
                        _motionAnimationMaster.StartPlayback();
                    }

                    _playing = null;
                }

                tittyMagic.settingsMonitor.enabled = true;
            }

            if(!_queued)
            {
                calibratingJsb.val = false;
            }
            else
            {
                _queued = false;
            }

            _deferToOtherInstance = false;
        }
    }
}
