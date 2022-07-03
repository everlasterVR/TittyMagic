using System;
using System.Collections;
using UnityEngine;
using static TittyMagic.Utils;

namespace TittyMagic
{
    internal class BoolSetting
    {
        private bool prevValue { get; set; }
        private readonly string _notification;

        public BoolSetting(bool prevValue, string notification = "")
        {
            this.prevValue = prevValue;
            _notification = notification;
        }

        public bool CheckIfUpdateNeeded(bool value)
        {
            bool result = value && !prevValue;
            if(!value && prevValue && !string.IsNullOrEmpty(_notification))
            {
                LogMessage(_notification);
            }

            prevValue = value;
            return result;
        }
    }

    internal class SettingsMonitor : MonoBehaviour
    {
        private FrequencyRunner _runner;
        private JSONStorable _breastInOut;
        private JSONStorable _softBodyPhysicsEnabler;
        private DAZPhysicsMesh _breastPhysicsMesh;
        private DAZCharacterSelector _geometry;

        private BoolSetting _useAdvancedColliders;

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
            if(Gender.isFemale)
            {
                _breastPhysicsMesh = (DAZPhysicsMesh) _script.containingAtom.GetStorableByID("BreastPhysicsMesh");
            }

            _geometry = (DAZCharacterSelector) _script.containingAtom.GetStorableByID("geometry");
            _useAdvancedColliders = new BoolSetting(
                _geometry.useAdvancedColliders,
                "Advanced Colliders are not enabled in Control & Physics 1 tab. Enable them to allow dynamic breast morphing to work correctly!"
            );

            _fixedDeltaTime = Time.fixedDeltaTime;

            _breastSoftPhysicsOn = _breastPhysicsMesh != null && _breastPhysicsMesh.on;
            _atomSoftPhysicsOn = _softBodyPhysicsEnabler.GetBoolParamValue("enabled");
            _globalSoftPhysicsOn = UserPreferences.singleton.softPhysics;

            StartCoroutine(FixInOut());
        }

        // prevents breasts being flattened due to breastInOut morphs on scene load with plugin already present
        private IEnumerator FixInOut()
        {
            yield return new WaitForEndOfFrame();
            _breastInOut.SetBoolParamValue("enabled", true);
            _breastInOut.SetBoolParamValue("enabled", false);
        }

        private bool Watch()
        {
            if(_breastInOut.GetBoolParamValue("enabled"))
            {
                _breastInOut.SetBoolParamValue("enabled", false);
                if(_breastPhysicsMesh != null)
                {
                    LogMessage("Auto Breast In/Out Morphs disabled - TittyMagic adjusts breast morphs better without them.");
                }
            }

            if(Gender.isFemale && _useAdvancedColliders.CheckIfUpdateNeeded(_geometry.useAdvancedColliders))
            {
                _script.StartRefreshCoroutine(refreshMass: true, waitForListeners: false);
            }

            CheckFixedDeltaTimeChanged();
            CheckSoftPhysicsEnabledChanged();

            return true;
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

        private void CheckFixedDeltaTimeChanged()
        {
            float value = Time.fixedDeltaTime;
            if(Math.Abs(value - _fixedDeltaTime) > 0.001f)
            {
                _script.UpdateRateDependentPhysics();
            }

            _fixedDeltaTime = value;
        }

        private void CheckSoftPhysicsEnabledChanged()
        {
            bool breastSoftPhysicsOn = _breastPhysicsMesh != null && _breastPhysicsMesh.on;
            bool atomSoftPhysicsOn = _softBodyPhysicsEnabler.GetBoolParamValue("enabled");
            bool globalSoftPhysicsOn = UserPreferences.singleton.softPhysics;
            CheckIfStillDisabled(breastSoftPhysicsOn, atomSoftPhysicsOn, globalSoftPhysicsOn);

            bool refreshNeeded = false;
            bool value = globalSoftPhysicsOn && atomSoftPhysicsOn && breastSoftPhysicsOn;
            if(value != softPhysicsEnabled)
            {
                refreshNeeded = true;
            }

            _breastSoftPhysicsOn = breastSoftPhysicsOn;
            _atomSoftPhysicsOn = atomSoftPhysicsOn;
            _globalSoftPhysicsOn = globalSoftPhysicsOn;

            if(refreshNeeded)
            {
                _script.LoadSettings();
                _script.StartRefreshCoroutine(false, true);
            }
        }

        private void CheckIfStillDisabled(bool breastSoftPhysicsOn, bool atomSoftPhysicsOn, bool globalSoftPhysicsOn)
        {
            string location = "";

            if(breastSoftPhysicsOn && !_breastSoftPhysicsOn)
            {
                if(!atomSoftPhysicsOn && !globalSoftPhysicsOn)
                {
                    location = "Control & Physics 1 and in User Preferences";
                }
                else if(!atomSoftPhysicsOn)
                {
                    location = "Control & Physics 1";
                }
                else if(!globalSoftPhysicsOn)
                {
                    location = "User Preferences";
                }
            }
            else if(atomSoftPhysicsOn && !_atomSoftPhysicsOn)
            {
                if(!breastSoftPhysicsOn && !globalSoftPhysicsOn)
                {
                    location = "the plugin UI and in User Preferences";
                }
                else if(!breastSoftPhysicsOn)
                {
                    location = "the plugin UI";
                }
                else if(!globalSoftPhysicsOn)
                {
                    location = "User Preferences";
                }
            }
            else if(globalSoftPhysicsOn && !_globalSoftPhysicsOn)
            {
                if(!breastSoftPhysicsOn && !atomSoftPhysicsOn)
                {
                    location = "the plugin UI and in Control & Physics 1";
                }
                else if(!breastSoftPhysicsOn)
                {
                    location = "the plugin UI";
                }
                else if(!atomSoftPhysicsOn)
                {
                    location = "Control & Physics 1";
                }
            }

            if(location != "")
            {
                LogMessage($"Soft Physics is still disabled in {location}");
            }
        }
    }
}
