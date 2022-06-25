using System;
using System.Collections;
using UnityEngine;
using static TittyMagic.Utils;
using static TittyMagic.Globals;

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
        private float _timeSinceLastCheck;
        private const float CHECK_FREQUENCY = 1f; // check for changes to settings every 1 second

        private JSONStorable _breastInOut;
        private JSONStorable _softBodyPhysicsEnabler;

        private BoolSetting _useAdvancedColliders;

        private float _fixedDeltaTime;
        public bool softPhysicsEnabled;

        private Script _script;

        public void Init(Atom atom)
        {
            enabled = false; // will be enabled during main refresh cycle
            _script = gameObject.GetComponent<Script>();
            _breastInOut = atom.GetStorableByID("BreastInOut");
            _softBodyPhysicsEnabler = atom.GetStorableByID("SoftBodyPhysicsEnabler");

            _useAdvancedColliders = new BoolSetting(
                GEOMETRY.useAdvancedColliders,
                "Advanced Colliders are not enabled in Control & Physics 1 tab. Enable them to allow dynamic breast morphing to work correctly!"
            );

            _fixedDeltaTime = Time.fixedDeltaTime;
            softPhysicsEnabled = CheckSoftPhysicsEnabled();

            StartCoroutine(FixInOut());
        }

        // prevents breasts being flattened due to breastInOut morphs on scene load with plugin already present
        private IEnumerator FixInOut()
        {
            yield return new WaitForEndOfFrame();
            _breastInOut.SetBoolParamValue("enabled", true);
            _breastInOut.SetBoolParamValue("enabled", false);
        }

        private void Update()
        {
            try
            {
                _timeSinceLastCheck += Time.unscaledDeltaTime;
                if(_timeSinceLastCheck >= CHECK_FREQUENCY)
                {
                    _timeSinceLastCheck -= CHECK_FREQUENCY;

                    // In/Out morphs can become enabled by e.g. loading an appearance preset. Force off.
                    if(_breastInOut.GetBoolParamValue("enabled"))
                    {
                        _breastInOut.SetBoolParamValue("enabled", false);
                        LogMessage("Auto Breast In/Out Morphs disabled - TittyMagic adjusts breast morphs better without them.");
                    }

                    if(_useAdvancedColliders.CheckIfUpdateNeeded(GEOMETRY.useAdvancedColliders))
                    {
                        gameObject.GetComponent<Script>().StartRefreshCoroutine();
                    }

                    if(softPhysicsEnabled && GEOMETRY.useAuxBreastColliders)
                    {
                        GEOMETRY.useAuxBreastColliders = false;
                        LogMessage("Breast Hard Colliders disabled. To enable them, disable soft physics first!");
                    }

                    CheckFixedDeltaTimeChanged();
                    CheckSoftPhysicsEnabledChanged();
                }
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
            bool value = CheckSoftPhysicsEnabled();
            if(value != softPhysicsEnabled)
            {
                if(!value && !GEOMETRY.useAuxBreastColliders)
                {
                    LogMessage("Recalibrating due to soft physics being enabled. You might also want to enable Breast Hard Colliders!");
                }
                softPhysicsEnabled = value;
                _script.LoadSettings();
                _script.StartRefreshCoroutine(false, true);
            }

            softPhysicsEnabled = value;
        }

        private bool CheckSoftPhysicsEnabled()
        {
            bool value = UserPreferences.singleton.softPhysics && _softBodyPhysicsEnabler.GetBoolParamValue("enabled") && BREAST_PHYSICS_MESH.on;
            _script.softPhysicsEnabled = value;
            return value;
        }
    }
}
