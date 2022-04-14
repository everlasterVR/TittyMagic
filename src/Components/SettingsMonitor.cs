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

        private BoolSetting _useAdvancedColliders;

        private bool _prevUseAdvancedColliders;
        private float _prevFixedDeltaTime;

        public void Init(Atom atom)
        {
            enabled = false; // will be enabled during main refresh cycle
            _breastInOut = atom.GetStorableByID("BreastInOut");
            _useAdvancedColliders = new BoolSetting(
                Globals.GEOMETRY.useAdvancedColliders,
                "Advanced Colliders are not enabled in Control & Physics 1 tab. Enable them to allow dynamic breast morphing to work correctly."
            );
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

                    if(_useAdvancedColliders.CheckIfUpdateNeeded(Globals.GEOMETRY.useAdvancedColliders))
                    {
                        StartCoroutine(gameObject.GetComponent<Script>().WaitToBeginRefresh(true));
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
    }
}
