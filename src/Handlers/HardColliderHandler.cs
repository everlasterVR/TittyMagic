using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using TittyMagic.Configs;
using UnityEngine;
using static TittyMagic.MVRParamName;

namespace TittyMagic
{
    internal class HardColliderHandler : MonoBehaviour
    {
        private Script _script;
        private DAZCharacterSelector _geometry;
        private bool _originalUseAuxBreastColliders;

        public Dictionary<string, ColliderConfig> hardCollidersLeft { get; private set; }
        public Dictionary<string, ColliderConfig> hardCollidersRight { get; private set; }

        public JSONStorableBool useHardColliders { get; private set; }
        public JSONStorableFloat hardCollidersRadiusMultiplier { get; private set; }
        public JSONStorableFloat hardCollidersMassMultiplier { get; private set; }

        public void Init()
        {
            _script = gameObject.GetComponent<Script>();;
            _geometry = (DAZCharacterSelector) _script.containingAtom.GetStorableByID("geometry");

            hardCollidersLeft = new Dictionary<string, ColliderConfig>()
            {
                { ColliderPosition.UPPER, CreateColliderConfig("lPectoral1") },
                { ColliderPosition.CORE, CreateColliderConfig("lPectoral2") },
                { ColliderPosition.INNER, CreateColliderConfig("lPectoral3") },
                { ColliderPosition.LOWER1, CreateColliderConfig("lPectoral4") },
                { ColliderPosition.LOWER2, CreateColliderConfig("lPectoral5") },
            };
            hardCollidersRight = new Dictionary<string, ColliderConfig>()
            {
                { ColliderPosition.UPPER, CreateColliderConfig("rPectoral1") },
                { ColliderPosition.CORE, CreateColliderConfig("rPectoral2") },
                { ColliderPosition.INNER, CreateColliderConfig("rPectoral3") },
                { ColliderPosition.LOWER1, CreateColliderConfig("rPectoral4") },
                { ColliderPosition.LOWER2, CreateColliderConfig("rPectoral5") },
            };

            useHardColliders = _script.NewJsonStorableBool("useHardColliders", true);
            useHardColliders.setCallbackFunction = SyncUseHardColliders;

            hardCollidersRadiusMultiplier = _script.NewJsonStorableFloat("hardColliderRadiusCombined", 1.00f, 0, 2.00f);
            hardCollidersRadiusMultiplier.setCallbackFunction = SyncHardColliderRadiusCombined;

            hardCollidersMassMultiplier = _script.NewJsonStorableFloat("hardColliderMassCombined", 1.00f, 0.10f, 5.00f);
            hardCollidersMassMultiplier.setCallbackFunction = SyncHardColliderMassCombined;

            SaveOriginalPhysicsAndSetPluginDefaults();
            SyncUseHardColliders(useHardColliders.val);
            SyncHardColliderRadiusCombined(hardCollidersRadiusMultiplier.val);
            SyncHardColliderMassCombined(hardCollidersMassMultiplier.val);
        }

        private ColliderConfig CreateColliderConfig(string partName)
        {
            var collider = _geometry.auxBreastColliders.ToList().Find(c => c.name.Contains(partName));
            return new ColliderConfig(collider);
        }

        private void SyncUseHardColliders(bool value)
        {
            if(!enabled) return;

            hardCollidersLeft.Values.ToList().ForEach(config => config.SetEnabled(value, hardCollidersMassMultiplier.val));
            hardCollidersRight.Values.ToList().ForEach(config => config.SetEnabled(value, hardCollidersMassMultiplier.val));

            if(_geometry.useAdvancedColliders) {
                _script.containingAtom.ResetPhysics(false);
            }
        }

        private void SyncHardColliderRadiusCombined(float value)
        {
            if(!enabled) return;

            hardCollidersLeft.Values.ToList().ForEach(config => config.UpdateRadius(value));
            hardCollidersRight.Values.ToList().ForEach(config => config.UpdateRadius(value));
        }

        private void SyncHardColliderMassCombined(float value)
        {
            if(!enabled) return;

            StartCoroutine(DeferSyncHardCollidersMassCombined(value));
        }

        private IEnumerator DeferSyncHardCollidersMassCombined(float value)
        {
            var configs = hardCollidersLeft.Values.Concat(hardCollidersRight.Values).ToList();

            while(configs.Any(config => !config.HasRigidbody()))
            {
                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.1f);

            configs.ForEach(config => config.UpdateRigidbodyMass(value));
        }

        public void SaveOriginalPhysicsAndSetPluginDefaults()
        {
            _originalUseAuxBreastColliders = _geometry.useAuxBreastColliders;

            const float mass = 0.2f;
            const float upperRadius = 0.9f;
            const float coreRadius = 0.9f;
            const float innerRadius = 0.9f;
            const float lower1Radius = 0.4f;
            const float lower2Radius = 0.8f;

            hardCollidersLeft[ColliderPosition.UPPER].SetBaseValues(upperRadius, mass);
            hardCollidersRight[ColliderPosition.UPPER].SetBaseValues(upperRadius, mass);

            hardCollidersLeft[ColliderPosition.CORE].SetBaseValues(coreRadius, mass);
            hardCollidersRight[ColliderPosition.CORE].SetBaseValues(coreRadius, mass);

            hardCollidersLeft[ColliderPosition.INNER].SetBaseValues(innerRadius, mass);
            hardCollidersRight[ColliderPosition.INNER].SetBaseValues(innerRadius, mass);

            hardCollidersLeft[ColliderPosition.LOWER1].SetBaseValues(lower1Radius, mass);
            hardCollidersRight[ColliderPosition.LOWER1].SetBaseValues(lower1Radius,mass);

            hardCollidersLeft[ColliderPosition.LOWER2].SetBaseValues(lower2Radius, mass);
            hardCollidersRight[ColliderPosition.LOWER2].SetBaseValues(lower2Radius,mass);
        }

        public JSONClass Serialize()
        {
            var json = new JSONClass();
            json[USE_AUX_BREAST_COLLIDERS].AsBool = _originalUseAuxBreastColliders;
            json["hardCollidersLeft"] = JSONArrayFromColliderConfigs(hardCollidersLeft);
            json["hardCollidersRight"] = JSONArrayFromColliderConfigs(hardCollidersRight);
            return json;
        }

        private static JSONArray JSONArrayFromColliderConfigs(Dictionary<string, ColliderConfig> dictionary)
        {
            var jsonArray = new JSONArray();
            foreach(var kvp in dictionary)
            {
                var entry = new JSONClass();
                entry["position"] = kvp.Key;
                entry["radius"].AsFloat = kvp.Value.originalRadius;
                jsonArray.Add(entry);
            }
            return jsonArray;
        }

        public void RestoreFromJSON(JSONClass originalPhysicsJSON)
        {
            _originalUseAuxBreastColliders = originalPhysicsJSON[USE_AUX_BREAST_COLLIDERS].AsBool;

            var hardCollidersLeftJson = originalPhysicsJSON["hardCollidersLeft"].AsArray;
            foreach(JSONClass json in hardCollidersLeftJson)
            {
                var config = hardCollidersLeft[json["position"].Value];
                config.originalRadius = json["radius"].AsFloat;
            }

            var hardCollidersRightJson = originalPhysicsJSON["hardCollidersRight"].AsArray;
            foreach(JSONClass json in hardCollidersRightJson)
            {
                var config = hardCollidersRight[json["position"].Value];
                config.originalRadius = json["radius"].AsFloat;
            }
        }

        private void RestoreOriginalColliders()
        {
            hardCollidersLeft.Values.ToList().ForEach(colliderConfig =>
            {
                colliderConfig.ResetRadius();
                colliderConfig.ResetRigidbodyMass();
            });
            hardCollidersRight.Values.ToList().ForEach(colliderConfig =>
            {
                colliderConfig.ResetRadius();
                colliderConfig.ResetRigidbodyMass();
            });
            RestoreUseAuxBreastColliders();
        }

        // ensures that individual colliders are set back on/off that were disabled/enabled by the plugin
        private void RestoreUseAuxBreastColliders()
        {
            _geometry.useAuxBreastColliders = !_originalUseAuxBreastColliders;
            _geometry.useAuxBreastColliders = _originalUseAuxBreastColliders;
        }

        private void OnEnable()
        {
            if(_script == null || !_script.initDone)
                return;

            SaveOriginalPhysicsAndSetPluginDefaults();
            SyncUseHardColliders(useHardColliders.val);
            SyncHardColliderRadiusCombined(hardCollidersRadiusMultiplier.val);
            SyncHardColliderMassCombined(hardCollidersMassMultiplier.val);
        }

        private void OnDisable()
        {
            RestoreOriginalColliders();
        }
    }
}
