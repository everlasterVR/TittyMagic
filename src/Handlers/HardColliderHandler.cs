using System;
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

        public JSONStorableBool enabledJsb { get; private set; }
        public JSONStorableFloat scaleJsf { get; private set; }
        public JSONStorableFloat radiusJsf { get; private set; }
        public JSONStorableFloat heightJsf { get; private set; }
        public JSONStorableFloat forceJsf { get; private set; }

        private int _syncMassStatus = -1;

        // TODO storables
        private const float RADIUS_PECTORAL_1 = 0.89f;
        private const float RADIUS_PECTORAL_2 = 0.79f;
        private const float RADIUS_PECTORAL_3 = 0.93f;
        private const float RADIUS_PECTORAL_4 = 0.79f;
        private const float RADIUS_PECTORAL_5 = 0.74f;
        private const float HEIGHT_PECTORAL_1 = 1.15f;
        private const float HEIGHT_PECTORAL_2 = 1.25f;
        private const float HEIGHT_PECTORAL_3 = 0.68f;
        private const float HEIGHT_PECTORAL_4 = 0.78f;
        private const float HEIGHT_PECTORAL_5 = 0.79f;
        private const float MASS_COMBINED = 1f;
        private const float MASS_PECTORAL_1 = 2.5f;

        public void Init()
        {
            _script = gameObject.GetComponent<Script>();;
            _geometry = (DAZCharacterSelector) _script.containingAtom.GetStorableByID("geometry");

            configs = NewColliderConfigs();

            enabledJsb = _script.NewJSONStorableBool("useHardColliders", false);
            enabledJsb.setCallbackFunction = SyncUseHardColliders;

            scaleJsf = _script.NewJSONStorableFloat("hardCollidersScaleCombined", 0, -0.05f, 0.05f);
            scaleJsf.setCallbackFunction = SyncScaleOffsetCombined;

            // TODO no slider
            radiusJsf = _script.NewJSONStorableFloat("hardColliderRadiusCombined", 1f, 0, 1.5f);
            radiusJsf.setCallbackFunction = SyncHardColliderRadiusCombined;

            // TODO no slider
            heightJsf = _script.NewJSONStorableFloat("hardColliderHeightCombined", 1f, 0, 1.50f);
            heightJsf.setCallbackFunction = SyncHardColliderHeightCombined;

            forceJsf = _script.NewJSONStorableFloat("hardColliderForceCombined", 0.25f, 0.01f, 1.00f);
            forceJsf.setCallbackFunction = SyncHardColliderMassCombined;

            _originalUseAuxBreastColliders = _geometry.useAuxBreastColliders;
            SyncUseHardColliders(enabledJsb.val);
        }

        private List<ColliderConfigGroup> NewColliderConfigs()
        {
            return new List<ColliderConfigGroup>
            {
                NewColliderConfigGroup("Pectoral1", RADIUS_PECTORAL_1, HEIGHT_PECTORAL_1, MASS_PECTORAL_1),
                NewColliderConfigGroup("Pectoral2", RADIUS_PECTORAL_2, HEIGHT_PECTORAL_2),
                NewColliderConfigGroup("Pectoral3", RADIUS_PECTORAL_3, HEIGHT_PECTORAL_3),
                NewColliderConfigGroup("Pectoral4", RADIUS_PECTORAL_4, HEIGHT_PECTORAL_4),
                NewColliderConfigGroup("Pectoral5", RADIUS_PECTORAL_5, HEIGHT_PECTORAL_5),
            };
        }

        private ColliderConfigGroup NewColliderConfigGroup(string id, float radiusMultiplier, float heightMultiplier, float? massMultiplier = null)
        {
            var auxBreastColliders = _geometry.auxBreastColliders.ToList();
            var left = auxBreastColliders.Find(c => c.name.Contains("l" + id));
            var right = auxBreastColliders.Find(c => c.name.Contains("r" + id));
            return new ColliderConfigGroup(id, left, right, radiusMultiplier, heightMultiplier, massMultiplier ?? MASS_COMBINED);
        }

        private void SyncUseHardColliders(bool value)
        {
            if(!enabled) return;

            configs.ForEach(config => config.SetEnabled(value));

            if(value)
            {
                SyncScaleOffsetCombined(scaleJsf.val);
                // SyncHardColliderRadiusCombined(radiusMultiplier.val);
                // SyncHardColliderHeightCombined(heightMultiplier.val);
                SyncHardColliderMassCombined(forceJsf.val);
            }
        }

        public void ReSyncScaleOffsetCombined()
        {
            if(enabledJsb.val)
            {
                SyncScaleOffsetCombined(scaleJsf.val);
            }
        }

        private void SyncScaleOffsetCombined(float value)
        {
            if(!enabled) return;

            configs.ForEach(config => config.UpdateScaleOffset(value, radiusJsf.val, heightJsf.val));
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

            var slider = (UIDynamicSlider) _script.mainWindow?.elements[forceJsf.name];
            if(slider != null)
            {
                while(slider.IsClickDown())
                {
                    yield return new WaitForSecondsRealtime(0.1f);
                }

                yield return new WaitForSecondsRealtime(0.1f);
            }

            _syncMassStatus = WaitStatus.DONE;
            yield return DeferSyncMassCombined(forceJsf.val);
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
                Utils.LogMessage("Unable to apply force multiplier: hard colliders are not enabled. Enable hard colliders in order to re-apply.");
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

        private IEnumerator RestoreDefaults()
        {
            if(!enabledJsb.val)
            {
                configs.ForEach(config => config.SetEnabled(true));
            }
            yield return DeferRestoreDefaultMass();
            _geometry.useAuxBreastColliders = _originalUseAuxBreastColliders;
            ResetAutoColliderScale();
        }

        private void ResetAutoColliderScale()
        {
            try
            {
                var updater = _script.containingAtom.GetComponentInChildren<AutoColliderBatchUpdater>();
                updater.autoColliders
                    .Where(autoCollider =>
                    {
                        var jointCollider = autoCollider.jointCollider;
                        if(jointCollider == null) return false;
                        return jointCollider.name.Contains("lPectoral") || jointCollider.name.Contains("rPectoral");
                    })
                    .ToList()
                    .ForEach(autoCollider => autoCollider.AutoColliderSizeSet());
            }
            catch(Exception e)
            {
                Utils.LogError($"Failed to reset hard collider scale. Error: {e}");
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
                Utils.LogError("Failed restoring hard colliders mass to default.");
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

            SyncUseHardColliders(enabledJsb.val);
        }

        private void OnDisable()
        {
            StartCoroutine(RestoreDefaults());
        }
    }
}
