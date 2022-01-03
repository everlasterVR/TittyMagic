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

        private Dictionary<string, bool> boolValues;
        private Dictionary<string, bool> prevBoolValues;
        private Dictionary<string, string> messages;

        private float prevFixedDeltaTime;

        public void Init(Atom containingAtom)
        {
            breastInOut = containingAtom.GetStorableByID("BreastInOut");
            breastInOut.SetBoolParamValue("enabled", false); // In/Out auto morphs off

            softBodyPhysicsEnabler = containingAtom.GetStorableByID("SoftBodyPhysicsEnabler");
            softBodyPhysicsEnabler.SetBoolParamValue("enabled", true); // Atom soft physics on

            boolValues = new Dictionary<string, bool>
            {
                { "prefsSoftPhysics", true },
                { "bodySoftPhysics", true },
                { "breastSoftPhysics", true }
            };
            prevBoolValues = new Dictionary<string, bool>
            {
                { "prefsSoftPhysics", true },
                { "bodySoftPhysics", true },
                { "breastSoftPhysics", true }
            };

            //monitor change to physics rate
            prevFixedDeltaTime = Time.fixedDeltaTime;

            string requires = "Enable it to allow physics settings to be recalculated if breast morphs are changed. (No need to reload the plugin if you do enable it.)";
            messages = new Dictionary<string, string>
            {
                { "prefsSoftPhysics", $"Soft Body Physics is not enabled in User Preferences. {requires}" },
                { "bodySoftPhysics", $"Soft Body Physics is not enabled in Control & Physics 1 tab. {requires}" },
                { "breastSoftPhysics", $"Soft Physics is not enabled in F Breast Physics 2 tab. {requires}" }
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

                    boolValues["prefsSoftPhysics"] = UserPreferences.singleton.softPhysics;
                    boolValues["bodySoftPhysics"] = softBodyPhysicsEnabler.GetBoolParamValue("enabled");
                    boolValues["breastSoftPhysics"] = Globals.BREAST_PHYSICS_MESH.on;

                    //In/Out morphs can become enabled by e.g. loading an appearance preset. Force off.
                    if(breastInOut.GetBoolParamValue("enabled"))
                    {
                        breastInOut.SetBoolParamValue("enabled", false);
                        Log.Message("Auto Breast In/Out Morphs disabled - this plugin adjusts breast morphs better without it.");
                    }

                    bool fullUpdateNeeded = false;
                    foreach(KeyValuePair<string, bool> kvp in boolValues)
                    {
                        fullUpdateNeeded = CheckBoolValue(kvp.Key, kvp.Value);
                    }

                    if(fullUpdateNeeded)
                    {
                        StartCoroutine(gameObject.GetComponent<Script>().RefreshPositionAndStaticPhysics());
                    }

                    float fixedDeltaTime = Time.fixedDeltaTime;
                    if(fixedDeltaTime != prevFixedDeltaTime)
                    {
                        gameObject.GetComponent<Script>().RefreshRateDependentPhysics();
                    }
                    prevFixedDeltaTime = fixedDeltaTime;
                }
            }
            catch(Exception e)
            {
                Log.Error($"{e}", nameof(SettingsMonitor));
                enabled = false;
            }
        }

        private bool CheckBoolValue(string key, bool value)
        {
            bool updateNeeded = false;
            if(!value && prevBoolValues[key])
            {
                Log.Error(messages[key]);
            }
            else if(value && !prevBoolValues[key])
            {
                updateNeeded = true;
            }
            prevBoolValues[key] = value;
            return updateNeeded;
        }
    }
}
