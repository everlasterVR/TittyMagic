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
        private DAZCharacter _selectedCharacter;

        private float _fixedDeltaTime;

        public bool softPhysicsEnabled => _globalSoftPhysicsOn && _atomSoftPhysicsOn && _breastSoftPhysicsOn;

        private bool _globalSoftPhysicsOn;
        private bool _atomSoftPhysicsOn;
        private bool _breastSoftPhysicsOn;

        private Rigidbody _pectoralRbLeft;
        private Rigidbody _pectoralRbRight;

        private bool _isInitialized;

        public void Init()
        {
            enabled = false; // will be enabled during main refresh cycle
            _runner = new FrequencyRunner(1);
            _breastInOut = tittyMagic.containingAtom.GetStorableByID("BreastInOut");
            _softBodyPhysicsEnabler = tittyMagic.containingAtom.GetStorableByID("SoftBodyPhysicsEnabler");
            _geometry = (DAZCharacterSelector) tittyMagic.containingAtom.GetStorableByID("geometry");
            _selectedCharacter = _geometry.selectedCharacter;

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

                var breastControl = (AdjustJoints) tittyMagic.containingAtom.GetStorableByID("BreastControl");
                _pectoralRbLeft = breastControl.joint2.GetComponent<Rigidbody>();
                _pectoralRbRight = breastControl.joint1.GetComponent<Rigidbody>();
            }

            /* prevent breasts being flattened due to breastInOut morphs on scene load with plugin already present */
            _breastInOut.SetBoolParamValue("enabled", true);
            _breastInOut.SetBoolParamValue("enabled", false);

            _isInitialized = true;
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

            if(Gender.isFemale)
            {
                /* Enforce advanced colliders on */
                if(!_geometry.useAdvancedColliders)
                {
                    _geometry.useAdvancedColliders = true;
                    Utils.LogMessage("Advanced Colliders enabled - they are necessary for directional force morphing and hard colliders to work.");
                }

                /* Enforce hard colliders on */
                if(!_geometry.useAuxBreastColliders)
                {
                    _geometry.useAuxBreastColliders = true;
                    Utils.LogMessage("Breast Hard Colliders re-enabled.");
                }

                /* Disable pectoral joint rb's collisions if enabled by e.g. person atom collisions being toggled off/on */
                if(_pectoralRbLeft.detectCollisions || _pectoralRbRight.detectCollisions)
                {
                    SetPectoralCollisions(false);
                }
            }

            if(!tittyMagic.calibration.isInProgress)
            {
                CheckIfRecalibrationNeeded();

                if(_selectedCharacter != _geometry.selectedCharacter)
                {
                    StartCoroutine(OnCharacterChangedCo());
                }
            }
        }

        private void CheckIfRecalibrationNeeded()
        {
            bool refreshNeeded = false;
            bool rateDependentRefreshNeeded = false;

            if(Gender.isFemale)
            {
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
                if(softPhysicsEnabled)
                {
                    SoftPhysicsHandler.UpdateRateDependentPhysics();
                }

                tittyMagic.hardColliderHandler.SyncCollidersMass();
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
            FrictionHandler.Refresh(tittyMagic.containingAtom.GetStorableByID("skin"));
        }

        private void SetPectoralCollisions(bool value)
        {
            _pectoralRbLeft.detectCollisions = value;
            _pectoralRbRight.detectCollisions = value;
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
            if(!_isInitialized)
            {
                return;
            }

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
            if(!_isInitialized)
            {
                return;
            }

            if(Gender.isFemale)
            {
                SetPectoralCollisions(false);
            }

            if(_geometry != null)
            {
                _geometry.useAdvancedColliders = true;
            }
        }

        private void OnDisable()
        {
            if(!_isInitialized)
            {
                return;
            }

            if(Gender.isFemale)
            {
                SetPectoralCollisions(true);
            }
        }
    }
}
