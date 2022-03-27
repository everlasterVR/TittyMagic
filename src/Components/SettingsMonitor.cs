using System;
using System.Collections;
using UnityEngine;
using static TittyMagic.Utils;

namespace TittyMagic
{
    internal class SettingsMonitor : MonoBehaviour
    {
        private float _timeSinceLastCheck;
        private const float CHECK_FREQUENCY = 1f; // check for changes to settings every 1 second

        private JSONStorable _breastInOut;

        private bool _prevUseAdvancedColliders;
        private float _prevFixedDeltaTime;

        public void Init(Atom atom)
        {
            enabled = false; // will be enabled during main refresh cycle
            _breastInOut = atom.GetStorableByID("BreastInOut");
            _prevUseAdvancedColliders = Globals.GEOMETRY.useAdvancedColliders;
            _prevFixedDeltaTime = Time.fixedDeltaTime;
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

                    if(CheckAdvancedColliders())
                    {
                        StartCoroutine(gameObject.GetComponent<Script>().WaitToBeginRefresh(true, false));
                    }

                    float fixedDeltaTime = Time.fixedDeltaTime;
                    if(Math.Abs(fixedDeltaTime - _prevFixedDeltaTime) > 0.001f)
                    {
                        gameObject.GetComponent<Script>().UpdateRateDependentPhysics();
                    }

                    _prevFixedDeltaTime = fixedDeltaTime;
                }
            }
            catch(Exception e)
            {
                LogError($"{e}", nameof(SettingsMonitor));
                enabled = false;
            }
        }

        private bool CheckAdvancedColliders()
        {
            bool updateNeeded = false;
            bool value = Globals.GEOMETRY.useAdvancedColliders;
            if(!value && _prevUseAdvancedColliders)
            {
                LogMessage("Advanced Colliders are not enabled in Control & Physics 1 tab. Enable them to allow Animation optimized mode to work correctly.");
            }
            else if(value && !_prevUseAdvancedColliders)
            {
                updateNeeded = true;
            }

            _prevUseAdvancedColliders = value;
            return updateNeeded;
        }
    }
}
