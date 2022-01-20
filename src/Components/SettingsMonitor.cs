using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static TittyMagic.Utils;

namespace TittyMagic
{
    internal class SettingsMonitor : MonoBehaviour
    {
        private float timeSinceLastCheck;
        private const float checkFrequency = 1f; // check for changes to settings every 1 second

        private Atom atom;
        private FreeControllerV3 control;

        private JSONStorable breastInOut;
        private JSONStorable softBodyPhysicsEnabler;

        private Dictionary<string, bool> boolValues;
        private Dictionary<string, bool> prevBoolValues;
        private Dictionary<string, string> messages;

        private float prevFixedDeltaTime;

        public void Init(Atom containingAtom)
        {
            enabled = false; // will be enabled during main refresh cycle
            atom = containingAtom;
            control = atom.freeControllers.First();
            breastInOut = atom.GetStorableByID("BreastInOut");
            breastInOut.SetBoolParamValue("enabled", false); // In/Out auto morphs off

            softBodyPhysicsEnabler = atom.GetStorableByID("SoftBodyPhysicsEnabler");
            softBodyPhysicsEnabler.SetBoolParamValue("enabled", true); // Atom soft physics on

            // TODO only necessary in Animation optimized mode
            control.SetBoolParamValue("freezeAtomPhysicsWhenGrabbed", false);

            boolValues = new Dictionary<string, bool>
            {
                { "prefsSoftPhysics", true },
                { "bodySoftPhysics", true },
                { "breastSoftPhysics", true },
            };
            prevBoolValues = new Dictionary<string, bool>
            {
                { "prefsSoftPhysics", true },
                { "bodySoftPhysics", true },
                { "breastSoftPhysics", true },
            };

            //monitor change to physics rate
            prevFixedDeltaTime = Time.fixedDeltaTime;

            string softPhysicsRequired = "Enable it to allow physics settings to be recalculated if breast morphs are changed. (No need to reload the plugin if you do enable it.)";
            messages = new Dictionary<string, string>
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
            };

            if(!UserPreferences.singleton.softPhysics)
            {
                UserPreferences.singleton.softPhysics = true;
                LogMessage($"Soft physics has been enabled in VaM preferences.");
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

            breastInOut.SetBoolParamValue("enabled", true);
            breastInOut.SetBoolParamValue("enabled", false);
            enabled = wasEnabled;
        }

        private void Update()
        {
            try
            {
                timeSinceLastCheck += Time.unscaledDeltaTime;
                if(timeSinceLastCheck >= checkFrequency)
                {
                    timeSinceLastCheck -= checkFrequency;

                    boolValues["prefsSoftPhysics"] = UserPreferences.singleton.softPhysics;
                    boolValues["bodySoftPhysics"] = softBodyPhysicsEnabler.GetBoolParamValue("enabled");
                    boolValues["breastSoftPhysics"] = Globals.BREAST_PHYSICS_MESH.on;

                    if(control.GetBoolParamValue("freezeAtomPhysicsWhenGrabbed"))
                    {
                        control.SetBoolParamValue("freezeAtomPhysicsWhenGrabbed", false);
                        LogMessage("Prevented enabling Freeze Physics While Grabbing - it does not work in Animation Optimized mode.");
                    }

                    //In/Out morphs can become enabled by e.g. loading an appearance preset. Force off.
                    if(breastInOut.GetBoolParamValue("enabled"))
                    {
                        breastInOut.SetBoolParamValue("enabled", false);
                        LogMessage("Auto Breast In/Out Morphs disabled - this plugin adjusts breast morphs better without it.");
                    }

                    bool fullUpdateNeeded = false;
                    foreach(KeyValuePair<string, bool> kvp in boolValues)
                    {
                        fullUpdateNeeded = CheckBoolValue(kvp.Key, kvp.Value);
                    }

                    if(fullUpdateNeeded)
                    {
                        StartCoroutine(gameObject.GetComponent<Script>().BeginRefresh());
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
                LogError($"{e}", nameof(SettingsMonitor));
                enabled = false;
            }
        }

        private bool CheckBoolValue(string key, bool value)
        {
            bool updateNeeded = false;
            if(!value && prevBoolValues[key])
            {
                LogMessage(messages[key]);
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
