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

        public List<ColliderConfigGroup> configs { get; private set; }

        public JSONStorableBool useHardColliders { get; private set; }
        public JSONStorableFloat radiusOffset { get; private set; }
        public JSONStorableFloat heightOffset { get; private set; }
        public JSONStorableFloat massMultiplier { get; private set; }

        private int _syncMassStatus = -1;

        // TODO storables
        private const float MASS_COMBINED = 1f;
        private const float RADIUS_PECTORAL_1 = 1f;
        private const float RADIUS_PECTORAL_2 = 1f;
        private const float RADIUS_PECTORAL_3 = 1f;
        private const float RADIUS_PECTORAL_4 = 1f;
        private const float RADIUS_PECTORAL_5 = 1f;
        private const float HEIGHT_PECTORAL_1 = 1f;
        private const float HEIGHT_PECTORAL_2 = 1f;
        private const float HEIGHT_PECTORAL_3 = 1f;
        private const float HEIGHT_PECTORAL_4 = 1f;
        private const float HEIGHT_PECTORAL_5 = 1f;

        public void Init()
        {
            _script = gameObject.GetComponent<Script>();;
            _geometry = (DAZCharacterSelector) _script.containingAtom.GetStorableByID("geometry");

            configs = NewColliderConfigs();

            useHardColliders = _script.NewJSONStorableBool("useHardColliders", true);
            useHardColliders.setCallbackFunction = SyncUseHardColliders;

            radiusOffset = _script.NewJSONStorableFloat("hardColliderRadiusCombined", 0.90f, 0, 1.50f);
            radiusOffset.setCallbackFunction = SyncHardColliderRadiusCombined;

            heightOffset = _script.NewJSONStorableFloat("hardColliderHeightCombined", 0.90f, 0, 1.50f);
            heightOffset.setCallbackFunction = SyncHardColliderHeightCombined;

            massMultiplier = _script.NewJSONStorableFloat("hardColliderMassCombined", 0.10f, 0.01f, 1.00f);
            massMultiplier.setCallbackFunction = SyncHardColliderMassCombined;

            _originalUseAuxBreastColliders = _geometry.useAuxBreastColliders;
            SyncUseHardColliders(useHardColliders.val);
        }

        private List<ColliderConfigGroup> NewColliderConfigs()
        {
            return new List<ColliderConfigGroup>
            {
                NewColliderConfigGroup("Pectoral1", RADIUS_PECTORAL_1, HEIGHT_PECTORAL_1),
                NewColliderConfigGroup("Pectoral2", RADIUS_PECTORAL_2, HEIGHT_PECTORAL_2),
                NewColliderConfigGroup("Pectoral3", RADIUS_PECTORAL_3, HEIGHT_PECTORAL_3),
                NewColliderConfigGroup("Pectoral4", RADIUS_PECTORAL_4, HEIGHT_PECTORAL_4),
                NewColliderConfigGroup("Pectoral5", RADIUS_PECTORAL_5, HEIGHT_PECTORAL_5),
            };
        }

        private ColliderConfigGroup NewColliderConfigGroup(string id, float radiusMultiplier, float heightMultiplier)
        {
            var auxBreastColliders = _geometry.auxBreastColliders.ToList();
            var left = auxBreastColliders.Find(c => c.name.Contains("l" + id));
            var right = auxBreastColliders.Find(c => c.name.Contains("r" + id));
            return new ColliderConfigGroup(id, left, right, radiusMultiplier, heightMultiplier, MASS_COMBINED);
        }

        private void SyncUseHardColliders(bool value)
        {
            if(!enabled) return;

            configs.ForEach(config => config.SetEnabled(value));

            if(value)
            {
                SyncHardColliderRadiusCombined(radiusOffset.val);
                SyncHardColliderHeightCombined(heightOffset.val);
                SyncHardColliderMassCombined(massMultiplier.val);
            }

            // TODO necessary?
            if(_geometry.useAdvancedColliders) {
                _script.containingAtom.ResetPhysics(false);
            }
        }

        private void SyncHardColliderRadiusCombined(float value)
        {
            if(!enabled) return;

            configs.ForEach(config => config.UpdateRadius(value));
        }

        private void SyncHardColliderHeightCombined(float value)
        {
            if(!enabled) return;

            configs.ForEach(config => config.UpdateHeight(value));
        }

        private void SyncHardColliderMassCombined(float value)
        {
            if(!enabled) return;

            if(_syncMassStatus != WaitStatus.WAITING)
            {
                StartCoroutine(DeferBeginSyncMassCombined());
            }
        }

        private IEnumerator DeferBeginSyncMassCombined()
        {
            _syncMassStatus = WaitStatus.WAITING;
            yield return new WaitForSecondsRealtime(0.1f);

            var slider = (UIDynamicSlider) _script.mainWindow?.elements[massMultiplier.name];
            if(slider != null)
            {
                while(slider.IsClickDown())
                {
                    yield return new WaitForSecondsRealtime(0.1f);
                }

                yield return new WaitForSecondsRealtime(0.1f);
            }

            _syncMassStatus = WaitStatus.DONE;
            yield return DeferSyncMassCombined(massMultiplier.val);
        }

        private IEnumerator DeferSyncMassCombined(float value)
        {
            // In case hard colliders are not enabled (yet)
            float timeout = Time.unscaledTime + 3f;
            while(configs.Any(config => !config.HasRigidbodies()) && Time.unscaledTime < timeout)
            {
                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.1f);

            if (configs.Any(config => !config.HasRigidbodies()))
            {
                Utils.LogMessage($"Unable to sync hard colliders mass because hard colliders are not enabled. Please enable hard colliders to re-sync.");
            }
            else
            {
                configs.ForEach(config => config.UpdateRigidbodyMass(value));
            }
        }

        public JSONClass Serialize()
        {
            var jsonClass = new JSONClass();
            jsonClass[USE_AUX_BREAST_COLLIDERS].AsBool = _originalUseAuxBreastColliders;
            return jsonClass;
        }

        public void RestoreFromJSON(JSONClass originalJson)
        {
            _originalUseAuxBreastColliders = originalJson[USE_AUX_BREAST_COLLIDERS].AsBool;
        }

        // cycling hard colliders on/off ensures that individual colliders are set back on/off
        // if they were disabled/enabled by the plugin, and their radiuses are reset to defaults
        private IEnumerator RestoreDefaults()
        {
            if(useHardColliders.val)
            {
                yield return DeferRestoreDefaultMass();
                _geometry.useAuxBreastColliders = !_originalUseAuxBreastColliders;
                _geometry.useAuxBreastColliders = _originalUseAuxBreastColliders;
            }
            else
            {
                if(_originalUseAuxBreastColliders)
                {
                    _geometry.useAuxBreastColliders = !_originalUseAuxBreastColliders;
                    _geometry.useAuxBreastColliders = _originalUseAuxBreastColliders;
                    yield return StartCoroutine(DeferRestoreDefaultMass());
                }
                else
                {
                    _geometry.useAuxBreastColliders = !_originalUseAuxBreastColliders;
                    yield return StartCoroutine(DeferRestoreDefaultMass());
                    _geometry.useAuxBreastColliders = _originalUseAuxBreastColliders;
                }
            }
        }

        private IEnumerator DeferRestoreDefaultMass()
        {
            // In case hard colliders are not enabled (yet)
            float timeout = Time.unscaledTime + 3f;
            while(configs.Any(config => !config.HasRigidbodies()) && Time.unscaledTime < timeout)
            {
                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.1f);

            if (configs.Any(config => !config.HasRigidbodies()))
            {
                Utils.LogError($"Failed restoring hard colliders mass to default.");
            }
            else
            {
                configs.ForEach(config => config.RestoreDefaultMass());
            }
        }

        private void OnEnable()
        {
            if(_script == null || !_script.initDone)
                return;

            SyncUseHardColliders(useHardColliders.val);
        }

        private void OnDisable()
        {
            StartCoroutine(RestoreDefaults());
        }
    }
}
