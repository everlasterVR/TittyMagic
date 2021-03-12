using System;
using System.Collections.Generic;
using UnityEngine;

namespace TittyMagic
{
    internal class SettingsMonitor : MonoBehaviour
    {
        private float timeSinceLastCheck;
        private const float checkFrequency = 1f; // check for changes to settings every 1 second

        private JSONStorable breastInOut;
        private JSONStorable softBodyPhysicsEnabler;

        private Dictionary<string, bool> values;
        private Dictionary<string, bool> prevValues;
        private Dictionary<string, string> messages;

        public void Init(Atom containingAtom)
        {
            breastInOut = containingAtom.GetStorableByID("BreastInOut");
            breastInOut.SetBoolParamValue("enabled", false); // In/Out auto morphs off

            softBodyPhysicsEnabler = containingAtom.GetStorableByID("SoftBodyPhysicsEnabler");
            softBodyPhysicsEnabler.SetBoolParamValue("enabled", true); // Atom soft physics on

            values = new Dictionary<string, bool>
            {
                { "prefsSoftPhysics", true },
                { "bodySoftPhysics", true },
                { "breastSoftPhysics", true }
            };
            prevValues = new Dictionary<string, bool>
            {
                { "prefsSoftPhysics", true },
                { "bodySoftPhysics", true },
                { "breastSoftPhysics", true }
            };

            string requires = "Adjusting breast morphs won't cause physics settings to be recalculated until it is enabled. (No need to reload the plugin if you do enable it.)";
            messages = new Dictionary<string, string>
            {
                { "prefsSoftPhysics", $"Detected that Soft Body Physics is not enabled in User Preferences. {requires}" },
                { "bodySoftPhysics", $"Detected that Soft Body Physics is not enabled in Control & Physics 1 tab. {requires}" },
                { "breastSoftPhysics", $"Detected that Soft Physics is not enabled in F Breast Physics 2 tab. {requires}" }
            };

            if(!UserPreferences.singleton.softPhysics)
            {
                UserPreferences.singleton.softPhysics = true;
                Log.Message($"Soft physics has been enabled in VaM preferences.");
            }
            enabled = false;
        }

        private void Update()
        {
            try
            {
                timeSinceLastCheck += Time.deltaTime;
                if(timeSinceLastCheck >= checkFrequency)
                {
                    timeSinceLastCheck -= checkFrequency;

                    values["prefsSoftPhysics"] = UserPreferences.singleton.softPhysics;
                    values["bodySoftPhysics"] = softBodyPhysicsEnabler.GetBoolParamValue("enabled");
                    values["breastSoftPhysics"] = Globals.BREAST_PHYSICS_MESH.on;

                    //In/Out morphs can become enabled by e.g. loading an appearance preset. Force off.
                    if(breastInOut.GetBoolParamValue("enabled"))
                    {
                        breastInOut.SetBoolParamValue("enabled", false);
                        Log.Message("Auto Breast In/Out Morphs disabled - this plugin adjusts breast morphs better without it.");
                    }

                    foreach(KeyValuePair<string, bool> kvp in values)
                    {
                        CheckValue(kvp.Key, kvp.Value);
                    }
                }
            }
            catch(Exception e)
            {
                Log.Error($"{e}", nameof(SettingsMonitor));
                enabled = false;
            }
        }

        private void CheckValue(string key, bool value)
        {
            if(!value && prevValues[key])
            {
                Log.Error(messages[key]);
            }
            else if(value && !prevValues[key])
            {
                StartCoroutine(gameObject.GetComponent<Script>().RefreshStaticPhysics());
            }
            prevValues[key] = value;
        }
    }
}
