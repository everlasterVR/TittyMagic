using System;
using UnityEngine;
using static TittyMagic.Utils;

namespace TittyMagic
{
    internal class SettingsMonitor : MonoBehaviour
    {
        private FrequencyRunner _runner;
        private JSONStorable _breastInOut;
        private JSONStorable _softBodyPhysicsEnabler;
        private DAZPhysicsMesh _breastPhysicsMesh;
        private DAZCharacterSelector _geometry;

        private float _fixedDeltaTime;

        public bool softPhysicsEnabled => _globalSoftPhysicsOn && _atomSoftPhysicsOn && _breastSoftPhysicsOn;

        private bool _globalSoftPhysicsOn;
        private bool _atomSoftPhysicsOn;
        private bool _breastSoftPhysicsOn;

        private Script _script;

        public void Init()
        {
            enabled = false; // will be enabled during main refresh cycle
            _runner = new FrequencyRunner(1);
            _script = gameObject.GetComponent<Script>();
            _breastInOut = _script.containingAtom.GetStorableByID("BreastInOut");
            _softBodyPhysicsEnabler = _script.containingAtom.GetStorableByID("SoftBodyPhysicsEnabler");
            _geometry = (DAZCharacterSelector) _script.containingAtom.GetStorableByID("geometry");

            if(Gender.isFemale)
            {
                _breastPhysicsMesh = (DAZPhysicsMesh) _script.containingAtom.GetStorableByID("BreastPhysicsMesh");
                _breastSoftPhysicsOn = _breastPhysicsMesh.on;
                _atomSoftPhysicsOn = _softBodyPhysicsEnabler.GetBoolParamValue("enabled");
                _globalSoftPhysicsOn = UserPreferences.singleton.softPhysics;
            }

            _fixedDeltaTime = Time.fixedDeltaTime;

            /* prevent breasts being flattened due to breastInOut morphs on scene load with plugin already present */
            _breastInOut.SetBoolParamValue("enabled", true);
            _breastInOut.SetBoolParamValue("enabled", false);
        }

        private void Watch()
        {
            if(_breastInOut.GetBoolParamValue("enabled"))
            {
                _breastInOut.SetBoolParamValue("enabled", false);
                if(Gender.isFemale)
                {
                    LogMessage("Auto Breast In/Out Morphs disabled - directional force morphing works better without them.");
                }
            }

            if(Gender.isFemale && !_geometry.useAdvancedColliders)
            {
                _geometry.useAdvancedColliders = true;
                LogMessage("Advanced Colliders enabled - they are necessary for directional force morphing and hard colliders to work.");
            }

            if(_script.refreshInProgress)
            {
                return;
            }

            bool refreshNeeded = false;
            bool rateDependentRefreshNeeded = false;

            if(Gender.isFemale)
            {
                /* Check if hard colliders have been disabled */
                if(!_geometry.useAuxBreastColliders)
                {
                    refreshNeeded = true;
                }

                /* Check if soft physics was toggled */
                {
                    bool breastSoftPhysicsOn = _breastPhysicsMesh.on;
                    bool atomSoftPhysicsOn = _softBodyPhysicsEnabler.GetBoolParamValue("enabled");
                    bool globalSoftPhysicsOn = UserPreferences.singleton.softPhysics;

                    string location = LocationWhereStillDisabled(breastSoftPhysicsOn, atomSoftPhysicsOn, globalSoftPhysicsOn);
                    if(!string.IsNullOrEmpty(location))
                    {
                        LogMessage($"Soft Physics is still disabled in {location}");
                    }

                    bool value = globalSoftPhysicsOn && atomSoftPhysicsOn && breastSoftPhysicsOn;
                    if(value != softPhysicsEnabled)
                    {
                        _script.softPhysicsHandler.ReverseSyncSoftPhysicsOn();
                        refreshNeeded = true;
                    }

                    _breastSoftPhysicsOn = breastSoftPhysicsOn;
                    _atomSoftPhysicsOn = atomSoftPhysicsOn;
                    _globalSoftPhysicsOn = globalSoftPhysicsOn;
                }
            }

            /* Check if delta time chaned */
            {
                float value = Time.fixedDeltaTime;
                if(Math.Abs(value - _fixedDeltaTime) > 0.001f)
                {
                    rateDependentRefreshNeeded = true;
                }

                _fixedDeltaTime = value;
            }

            if(refreshNeeded)
            {
                _script.StartRefreshCoroutine(refreshMass: false, waitForListeners: false);
            }
            else if(rateDependentRefreshNeeded)
            {
                _script.mainPhysicsHandler.UpdateRateDependentPhysics();
                _script.softPhysicsHandler.UpdateRateDependentPhysics();
                _script.hardColliderHandler.SyncHardCollidersBaseMass();
            }
        }

        private string LocationWhereStillDisabled(bool breastSoftPhysicsOn, bool atomSoftPhysicsOn, bool globalSoftPhysicsOn)
        {
            if(breastSoftPhysicsOn && !_breastSoftPhysicsOn)
            {
                if(!atomSoftPhysicsOn && !globalSoftPhysicsOn)
                {
                    return "Control & Physics 1 and in User Preferences";
                }

                if(!atomSoftPhysicsOn)
                {
                    return "Control & Physics 1";
                }

                if(!globalSoftPhysicsOn)
                {
                    return "User Preferences";
                }
            }
            else if(atomSoftPhysicsOn && !_atomSoftPhysicsOn)
            {
                if(!breastSoftPhysicsOn && !globalSoftPhysicsOn)
                {
                    return "the plugin UI and in User Preferences";
                }

                if(!breastSoftPhysicsOn)
                {
                    return "the plugin UI";
                }

                if(!globalSoftPhysicsOn)
                {
                    return "User Preferences";
                }
            }
            else if(globalSoftPhysicsOn && !_globalSoftPhysicsOn)
            {
                if(!breastSoftPhysicsOn && !atomSoftPhysicsOn)
                {
                    return "the plugin UI and in Control & Physics 1";
                }

                if(!breastSoftPhysicsOn)
                {
                    return "the plugin UI";
                }

                if(!atomSoftPhysicsOn)
                {
                    return "Control & Physics 1";
                }
            }

            return null;
        }

        private void Update()
        {
            try
            {
                _runner.Run(Watch);
            }
            catch(Exception e)
            {
                LogError($"{e}", nameof(SettingsMonitor));
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if(_geometry != null)
            {
                _geometry.useAdvancedColliders = true;
            }
        }
    }
}
