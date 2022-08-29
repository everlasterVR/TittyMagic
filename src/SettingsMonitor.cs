using System;
using System.Collections;
using TittyMagic.Components;
using TittyMagic.Handlers;
using UnityEngine;
using static TittyMagic.Script;

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
        private DAZCharacter _selectedCharacter;

        public void Init()
        {
            enabled = false; // will be enabled during main refresh cycle
            _runner = new FrequencyRunner(1);
            _breastInOut = tittyMagic.containingAtom.GetStorableByID("BreastInOut");
            _softBodyPhysicsEnabler = tittyMagic.containingAtom.GetStorableByID("SoftBodyPhysicsEnabler");
            _geometry = (DAZCharacterSelector) tittyMagic.containingAtom.GetStorableByID("geometry");

            _fixedDeltaTime = Time.fixedDeltaTime;

            if(Gender.isFemale)
            {
                _breastPhysicsMesh = (DAZPhysicsMesh) tittyMagic.containingAtom.GetStorableByID("BreastPhysicsMesh");
                /* Initialize _breastSoftPhysicsOn to same value as initialized to in
                 * SoftPhysicsHandler's own JSONStorable, prevents double calibration on init
                 */
                _breastSoftPhysicsOn = SoftPhysicsHandler.softPhysicsOnJsb.val;
                _atomSoftPhysicsOn = _softBodyPhysicsEnabler.GetBoolParamValue("enabled");
                _globalSoftPhysicsOn = UserPreferences.singleton.softPhysics;
            }

            _selectedCharacter = _geometry.selectedCharacter;

            /* prevent breasts being flattened due to breastInOut morphs on scene load with plugin already present */
            _breastInOut.SetBoolParamValue("enabled", true);
            _breastInOut.SetBoolParamValue("enabled", false);
        }

        private void Watch()
        {
            /* Enforce in-out morphs off */
            if(_breastInOut.GetBoolParamValue("enabled"))
            {
                _breastInOut.SetBoolParamValue("enabled", false);
                if(Gender.isFemale)
                {
                    Utils.LogMessage("Auto Breast In/Out Morphs disabled - directional force morphing works better without them.");
                }
            }

            /* Enforce advanced colliders on */
            if(Gender.isFemale && !_geometry.useAdvancedColliders)
            {
                _geometry.useAdvancedColliders = true;
                Utils.LogMessage("Advanced Colliders enabled - they are necessary for directional force morphing and hard colliders to work.");
            }

            if(tittyMagic.calibration.isInProgress)
            {
                return;
            }

            CheckIfRecalibrationNeeded();

            if(_selectedCharacter != _geometry.selectedCharacter)
            {
                Utils.LogMessage($"changed!");
                StartCoroutine(OnCharacterChangedCo());
            }
        }

        private void CheckIfRecalibrationNeeded()
        {
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
                        Utils.LogMessage($"Soft Physics is still disabled in {location}");
                    }

                    bool value = globalSoftPhysicsOn && atomSoftPhysicsOn && breastSoftPhysicsOn;
                    if(value != softPhysicsEnabled)
                    {
                        SoftPhysicsHandler.ReverseSyncSoftPhysicsOn();
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
                tittyMagic.StartCalibration(calibratesMass: false, waitsForListeners: false);
            }
            else if(rateDependentRefreshNeeded)
            {
                MainPhysicsHandler.UpdateRateDependentPhysics();
                SoftPhysicsHandler.UpdateRateDependentPhysics();
                tittyMagic.hardColliderHandler.SyncHardCollidersBaseMass();
            }
        }

        private IEnumerator OnCharacterChangedCo()
        {
            _selectedCharacter = _geometry.selectedCharacter;
            while(!_selectedCharacter.ready)
            {
                yield return null;
            }

            tittyMagic.skin = _geometry.containingAtom.GetComponentInChildren<DAZCharacter>().skin;
            FrictionCalc.Refresh(tittyMagic.containingAtom.GetStorableByID("skin"));
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
                Utils.LogError($"{e}", nameof(SettingsMonitor));
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
