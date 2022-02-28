using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TittyMagic.Utils;

namespace TittyMagic
{
    internal class SettingsMonitor : MonoBehaviour
    {
        private float _timeSinceLastCheck;
        private const float CHECK_FREQUENCY = 1f; // check for changes to settings every 1 second

        private Atom _atom;

        private JSONStorable _breastInOut;
        private JSONStorable _softBodyPhysicsEnabler;

        private Dictionary<string, bool> _boolValues;
        private Dictionary<string, bool> _prevBoolValues;
        private Dictionary<string, string> _messages;

        private float _prevFixedDeltaTime;

        public void Init(Atom containingAtom)
        {
            enabled = false; // will be enabled during main refresh cycle
            _atom = containingAtom;
            _breastInOut = _atom.GetStorableByID("BreastInOut");
            _breastInOut.SetBoolParamValue("enabled", false); // In/Out auto morphs off

            _softBodyPhysicsEnabler = _atom.GetStorableByID("SoftBodyPhysicsEnabler");
            _softBodyPhysicsEnabler.SetBoolParamValue("enabled", true); // Atom soft physics on

            _boolValues = new Dictionary<string, bool>
            {
                { "prefsSoftPhysics", true },
                { "bodySoftPhysics", true },
                { "breastSoftPhysics", true },
                { "advancedColliders", true },
            };
            _prevBoolValues = new Dictionary<string, bool>
            {
                { "prefsSoftPhysics", true },
                { "bodySoftPhysics", true },
                { "breastSoftPhysics", true },
                { "advancedColliders", true },
            };

            //monitor change to physics rate
            _prevFixedDeltaTime = Time.fixedDeltaTime;

            const string softPhysicsRequired = "Enable it to allow physics settings to be recalculated if breast morphs are changed.";
            _messages = new Dictionary<string, string>
            {
                {
                    "prefsSoftPhysics",
                    $"Soft Body Physics is not enabled in User Preferences. {softPhysicsRequired}"
                },
                {
                    "bodySoftPhysics",
                    $"Soft Body Physics is not enabled in Control & Physics 1 tab. {softPhysicsRequired}"
                },
                {
                    "breastSoftPhysics",
                    $"Soft Physics is not enabled in F Breast Physics 2 tab. {softPhysicsRequired}"
                },
                {
                    "advancedColliders",
                    "Advanced Colliders are not enabled in Control & Physics 1 tab. Enable them to allow Animation optimized mode to work correctly."
                },
            };

            if(!UserPreferences.singleton.softPhysics)
            {
                UserPreferences.singleton.softPhysics = true;
                LogMessage("Soft physics has been enabled in VaM preferences.");
            }

            StartCoroutine(FixInOut());
        }

        //prevents breasts being flattened due to breastInOut morphs on scene load with plugin already present
        private IEnumerator FixInOut()
        {
            yield return new WaitForEndOfFrame();

            //disable to prevent Update() from messaging about breastInOut being enabled
            bool wasEnabled = enabled;
            if(wasEnabled)
                enabled = false;

            _breastInOut.SetBoolParamValue("enabled", true);
            _breastInOut.SetBoolParamValue("enabled", false);
            enabled = wasEnabled;
        }

        private void Update()
        {
            try
            {
                _timeSinceLastCheck += Time.unscaledDeltaTime;
                if(_timeSinceLastCheck >= CHECK_FREQUENCY)
                {
                    _timeSinceLastCheck -= CHECK_FREQUENCY;

                    _boolValues["prefsSoftPhysics"] = UserPreferences.singleton.softPhysics;
                    _boolValues["bodySoftPhysics"] = _softBodyPhysicsEnabler.GetBoolParamValue("enabled");
                    _boolValues["breastSoftPhysics"] = Globals.BREAST_PHYSICS_MESH.on;
                    _boolValues["advancedColliders"] = Globals.GEOMETRY.useAdvancedColliders;

                    //In/Out morphs can become enabled by e.g. loading an appearance preset. Force off.
                    if(_breastInOut.GetBoolParamValue("enabled"))
                    {
                        _breastInOut.SetBoolParamValue("enabled", false);
                        LogMessage("Auto Breast In/Out Morphs disabled - TittyMagic adjusts breast morphs better without them.");
                    }

                    bool fullUpdateNeeded = false;
                    foreach(var kvp in _boolValues)
                    {
                        fullUpdateNeeded = fullUpdateNeeded || CheckBoolValue(kvp.Key, kvp.Value);
                    }

                    if(fullUpdateNeeded)
                    {
                        StartCoroutine(gameObject.GetComponent<Script>().WaitToBeginRefresh());
                    }

                    float fixedDeltaTime = Time.fixedDeltaTime;
                    if(Math.Abs(fixedDeltaTime - _prevFixedDeltaTime) > 0.001f)
                    {
                        gameObject.GetComponent<Script>().RefreshRateDependentPhysics();
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

        private bool CheckBoolValue(string key, bool value)
        {
            bool updateNeeded = false;
            if(!value && _prevBoolValues[key])
            {
                LogMessage(_messages[key]);
            }
            else if(value && !_prevBoolValues[key])
            {
                updateNeeded = true;
            }

            _prevBoolValues[key] = value;
            return updateNeeded;
        }
    }
}
