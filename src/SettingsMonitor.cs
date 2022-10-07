using System;
using System.Collections;
using TittyMagic.Components;
using TittyMagic.Handlers;
using TittyMagic.UI;
using UnityEngine;
using static TittyMagic.Script;

namespace TittyMagic
{
    public class SettingsMonitor : MonoBehaviour
    {
        private FrequencyRunner _runner;
        private JSONStorable _breastInOut;
        private JSONStorable _softBodyPhysicsEnabler;
        private DAZCharacter _selectedCharacter;

        private float _fixedDeltaTime;

        public bool softPhysicsEnabled => _globalSoftPhysicsOn && _atomSoftPhysicsOn && _breastSoftPhysicsOn;

        private bool _globalSoftPhysicsOn;
        private bool _atomSoftPhysicsOn;
        private bool _breastSoftPhysicsOn;

        private bool _initialized;

        public void Init()
        {
            enabled = false; // will be enabled during main refresh cycle
            _runner = new FrequencyRunner(1);
            _breastInOut = tittyMagic.containingAtom.GetStorableByID("BreastInOut");
            _softBodyPhysicsEnabler = tittyMagic.containingAtom.GetStorableByID("SoftBodyPhysicsEnabler");
            _selectedCharacter = geometry.selectedCharacter;

            _fixedDeltaTime = Time.fixedDeltaTime;

            if(personIsFemale)
            {
                /* Initialize _breastSoftPhysicsOn to same value as initialized to in
                 * SoftPhysicsHandler's own JSONStorable, prevents double calibration on init
                 */
                _breastSoftPhysicsOn = SoftPhysicsHandler.breastSoftPhysicsOnJsb.val;
                _atomSoftPhysicsOn = _softBodyPhysicsEnabler.GetBoolParamValue("enabled");
                _globalSoftPhysicsOn = UserPreferences.singleton.softPhysics;
            }

            /* prevent breasts being flattened due to breastInOut morphs on scene load with plugin already present */
            _breastInOut.SetBoolParamValue("enabled", true);
            _breastInOut.SetBoolParamValue("enabled", false);

            _initialized = true;
        }

        public void CheckSettings()
        {
            /* Enforce in-out morphs off */
            if(_breastInOut.GetBoolParamValue("enabled"))
            {
                _breastInOut.SetBoolParamValue("enabled", false);
                if(personIsFemale)
                {
                    Utils.LogMessage("Auto Breast In/Out Morphs disabled - directional force morphing works better without them.");
                }
            }

            if(personIsFemale)
            {
                /* Enforce advanced colliders on */
                if(!geometry.useAdvancedColliders)
                {
                    geometry.useAdvancedColliders = true;
                    Utils.LogMessage("Advanced Colliders enabled - they are necessary for directional force morphing and hard colliders to work.");
                }

                /* Enforce hard colliders on */
                if(!geometry.useAuxBreastColliders)
                {
                    geometry.useAuxBreastColliders = true;
                    Utils.LogMessage("Breast Hard Colliders re-enabled.");
                }

                /* Disable pectoral joint rb's collisions if enabled by e.g. person atom collisions being toggled off/on */
                if(tittyMagic.pectoralRbLeft.detectCollisions || tittyMagic.pectoralRbRight.detectCollisions)
                {
                    HardColliderHandler.SetPectoralCollisions(false);
                }
            }
            else
            {
                /* Force enable pectoral joint rb's collisions for futa */
                if(!tittyMagic.pectoralRbLeft.detectCollisions || !tittyMagic.pectoralRbRight.detectCollisions)
                {
                    HardColliderHandler.SetPectoralCollisions(true);
                }
            }

            if(_selectedCharacter != geometry.selectedCharacter)
            {
                StartCoroutine(OnCharacterChangedCo());
            }

            if(!tittyMagic.calibrationHelper.calibratingJsb.val)
            {
                CheckIfRecalibrationNeeded();
            }
        }

        private void CheckIfRecalibrationNeeded()
        {
            bool refreshNeeded = false;
            bool rateDependentRefreshNeeded = false;

            if(personIsFemale)
            {
                /* Check if soft physics was toggled */
                {
                    bool breastSoftPhysicsOn = SoftPhysicsHandler.breastPhysicsMesh.on;
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
                        var physicsWindow = tittyMagic.tabs.activeWindow as PhysicsWindow;
                        if(physicsWindow != null)
                        {
                            physicsWindow.UpdateSoftPhysicsToggleStyle(value);
                        }

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

                HardColliderHandler.SyncCollidersMass();
            }
        }

        private IEnumerator OnCharacterChangedCo()
        {
            while(!geometry.selectedCharacter.ready)
            {
                yield return null;
            }

            if(_selectedCharacter.isMale != geometry.selectedCharacter.isMale)
            {
                Utils.LogMessage("Changing gender while the plugin is active is not supported. " +
                    "Disable the plugin, change gender and then reload the plugin.");
                geometry.selectedCharacter = _selectedCharacter;
            }
            else
            {
                _selectedCharacter = geometry.selectedCharacter;
                skin = geometry.containingAtom.GetComponentInChildren<DAZCharacter>().skin;
                tittyMagic.ReinitFrictionHandlerAndUI();
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
            if(!_initialized)
            {
                return;
            }

            try
            {
                _runner.Run(CheckSettings);
            }
            catch(Exception e)
            {
                Utils.LogError($"{nameof(SettingsMonitor)}: {e}");
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if(!_initialized)
            {
                return;
            }

            /* Check settings immediately when plugin enabled instead of waiting for runner to trigger */
            CheckSettings();
        }
    }
}
