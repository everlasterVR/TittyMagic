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
        private bool _originalUseAdvancedColliders;
        private bool _originalUseAuxBreastColliders;

        public JSONStorableStringChooser colliderGroupsJsc { get; private set; }

        public JSONStorableBool enabledJsb { get; private set; }

        public List<ColliderConfigGroup> colliderConfigs { get; private set; }

        public JSONStorableFloat baseForceJsf { get; private set; }

        private bool _combinedSyncInProgress;

        public const string ALL_OPTION = "All";

        // TODO storables
        private const float RADIUS_PECTORAL_1 = 0.89f;
        private const float RADIUS_PECTORAL_2 = 0.79f;
        private const float RADIUS_PECTORAL_3 = 0.93f;
        private const float RADIUS_PECTORAL_4 = 0.79f;
        private const float RADIUS_PECTORAL_5 = 0.74f;
        private const float LENGTH_PECTORAL_1 = 1.15f;
        private const float LENGTH_PECTORAL_2 = 1.25f;
        private const float LENGTH_PECTORAL_3 = 0.68f;
        private const float LENGTH_PECTORAL_4 = 0.78f;
        private const float LENGTH_PECTORAL_5 = 0.79f;
        private const float MASS_COMBINED = 1f;
        private const float MASS_PECTORAL_1 = 2.5f;

        public void Init()
        {
            _script = gameObject.GetComponent<Script>();
            _geometry = (DAZCharacterSelector) _script.containingAtom.GetStorableByID("geometry");
            _originalUseAdvancedColliders = _geometry.useAdvancedColliders;
            _geometry.useAdvancedColliders = true;
            _originalUseAuxBreastColliders = _geometry.useAuxBreastColliders;

            enabledJsb = _script.NewJSONStorableBool("useHardColliders", true, register: Gender.isFemale);
            enabledJsb.setCallbackFunction = SyncUseHardColliders;

            if(!Gender.isFemale)
            {
                enabledJsb.val = false;
                return;
            }

            colliderConfigs = new List<ColliderConfigGroup>
            {
                NewColliderConfigGroup("Pectoral1"),
                NewColliderConfigGroup("Pectoral2"),
                NewColliderConfigGroup("Pectoral3"),
                NewColliderConfigGroup("Pectoral4"),
                NewColliderConfigGroup("Pectoral5"),
            };

            var options = colliderConfigs.Select(c => c.visualizerEditableId).ToList();
            options.Insert(0, ALL_OPTION);
            var displayOptions = colliderConfigs.Select(c => c.id).ToList();
            displayOptions.Insert(0, ALL_OPTION);
            colliderGroupsJsc = new JSONStorableStringChooser(
                "colliderGroup",
                options,
                displayOptions,
                ALL_OPTION,
                "Collider"
            );
            colliderGroupsJsc.setCallbackFunction = value =>
            {
                _script.colliderVisualizer.PreviewOpacityJSON.val = value == ALL_OPTION ? 1 : 0.67f;
                _script.colliderVisualizer.EditablesJSON.val = value == ALL_OPTION ? "" : value;
            };

            baseForceJsf = _script.NewJSONStorableFloat("combinedColliderForce", 0.50f, 0.01f, 1.00f);
            baseForceJsf.setCallbackFunction = SyncHardColliderBaseMass;

            SyncUseHardColliders(enabledJsb.val);
        }

        private ColliderConfigGroup NewColliderConfigGroup(string id)
        {
            var configLeft = NewColliderConfig("l" + id);
            var configRight = NewColliderConfig("r" + id);
            var colliderConfigGroup = new ColliderConfigGroup(
                id,
                configLeft,
                configRight
            );

            var forceJsf = _script.NewJSONStorableFloat($"{id.ToLower()}ColliderForce", 0.50f, 0.01f, 1.00f);
            forceJsf.setCallbackFunction = _ => SyncHardColliderMass(colliderConfigGroup);
            colliderConfigGroup.forceJsf = forceJsf;

            var radiusJsf = _script.NewJSONStorableFloat($"{id.ToLower()}ColliderRadius", 0, -0.02f, 0.02f);
            radiusJsf.setCallbackFunction = _ => SyncHardColliderRadius(colliderConfigGroup);
            colliderConfigGroup.radiusJsf = radiusJsf;

            var lengthJsf = _script.NewJSONStorableFloat($"{id.ToLower()}ColliderLength", 0, -0.02f, 0.02f);
            lengthJsf.setCallbackFunction = _ => SyncHardColliderLength(colliderConfigGroup);
            colliderConfigGroup.lengthJsf = lengthJsf;

            var centerXJsf = _script.NewJSONStorableFloat($"{id.ToLower()}ColliderCenterX", 0, -0.02f, 0.02f);
            var centerYJsf = _script.NewJSONStorableFloat($"{id.ToLower()}ColliderCenterY", 0, -0.02f, 0.02f);
            var centerZJsf = _script.NewJSONStorableFloat($"{id.ToLower()}ColliderCenterZ", 0, -0.02f, 0.02f);

            centerXJsf.setCallbackFunction = _ => SyncHardColliderRightOffset(colliderConfigGroup);
            centerYJsf.setCallbackFunction = _ => SyncHardColliderUpOffset(colliderConfigGroup);
            centerZJsf.setCallbackFunction = _ => SyncHardColliderLookOffset(colliderConfigGroup);

            colliderConfigGroup.rightJsf = centerXJsf;
            colliderConfigGroup.upJsf = centerYJsf;
            colliderConfigGroup.lookJsf = centerZJsf;

            return colliderConfigGroup;
        }

        private ColliderConfig NewColliderConfig(string id)
        {
            var collider = _geometry.auxBreastColliders.ToList().Find(c => c.name.Contains(id));
            var autoCollider = FindAutoCollider(collider);
            string visualizerEditableId = _script.colliderVisualizer.EditablesJSON.choices.Find(option => option.EndsWith(id));
            return new ColliderConfig(autoCollider, visualizerEditableId);
        }

        private AutoCollider FindAutoCollider(Collider collider)
        {
            var updater = _script.containingAtom.GetComponentInChildren<AutoColliderBatchUpdater>();
            return updater.autoColliders.First(autoCollider =>
                autoCollider.jointCollider != null && autoCollider.jointCollider.name == collider.name);
        }

        private void SyncUseHardColliders(bool value)
        {
            if(!enabled)
            {
                return;
            }

            colliderConfigs.ForEach(config => config.SetEnabled(value));

            if(value)
            {
                SyncHardColliderBaseMass(0);
            }
        }

        public void ResetBaseValues()
        {
            foreach(var config in colliderConfigs)
            {
                config.RestoreDefaultMass();
                config.UpdateRadius();
                config.UpdateLength();
                config.UpdateLookOffset();
                config.UpdateUpOffset();
                config.UpdateRightOffset();
                //UpdateRigidbodyMass not needed, as base value should not change
                _script.colliderVisualizer.SyncPreviews();
            }
        }

        private void SyncHardColliderRadius(ColliderConfigGroup config)
        {
            if(!enabled)
            {
                return;
            }

            config.UpdateRadius();
            _script.colliderVisualizer.SyncPreviews();
        }

        private void SyncHardColliderLength(ColliderConfigGroup config)
        {
            if(!enabled)
            {
                return;
            }

            config.UpdateLength();
            _script.colliderVisualizer.SyncPreviews();
        }

        private void SyncHardColliderLookOffset(ColliderConfigGroup config)
        {
            if(!enabled)
            {
                return;
            }

            config.UpdateLookOffset();
            _script.colliderVisualizer.SyncPreviews();
        }

        private void SyncHardColliderUpOffset(ColliderConfigGroup config)
        {
            if(!enabled)
            {
                return;
            }

            config.UpdateUpOffset();
            _script.colliderVisualizer.SyncPreviews();
        }

        private void SyncHardColliderRightOffset(ColliderConfigGroup config)
        {
            if(!enabled)
            {
                return;
            }

            config.UpdateRightOffset();
            _script.colliderVisualizer.SyncPreviews();
        }

        private void SyncHardColliderMass(ColliderConfigGroup config)
        {
            if(!enabled)
            {
                return;
            }

            if(!config.syncInProgress)
            {
                StartCoroutine(DeferBeginSyncMass(config));
            }
        }

        private IEnumerator DeferBeginSyncMass(ColliderConfigGroup config)
        {
            config.syncInProgress = true;

            var elements = _script.mainWindow?.nestedWindow?.GetColliderSectionElements();
            if(elements != null)
            {
                yield return new WaitForSecondsRealtime(0.1f);
                var slider = (UIDynamicSlider) elements[config.forceJsf.name];
                if(slider != null)
                {
                    while(slider.IsClickDown())
                    {
                        yield return new WaitForSecondsRealtime(0.1f);
                    }

                    yield return new WaitForSecondsRealtime(0.1f);
                }
            }

            config.syncInProgress = false;
            yield return DeferSyncMass(config);
        }

        private IEnumerator DeferSyncMass(ColliderConfigGroup config)
        {
            // In case hard colliders are not enabled (yet)
            float timeout = Time.unscaledTime + 3f;
            while(!config.HasRigidbodies() && Time.unscaledTime < timeout)
            {
                yield return new WaitForSecondsRealtime(0.3f);
            }

            yield return new WaitForSecondsRealtime(0.1f);

            if(!config.HasRigidbodies())
            {
                LogSyncMassFailure();
            }
            else
            {
                config.UpdateRigidbodyMass(baseForceJsf.val * config.forceJsf.val);
            }
        }

        private void SyncHardColliderBaseMass(float value)
        {
            if(!enabled)
            {
                return;
            }

            if(!_combinedSyncInProgress)
            {
                StartCoroutine(DeferBeginSyncBaseMass());
            }
        }

        private IEnumerator DeferBeginSyncBaseMass()
        {
            _combinedSyncInProgress = true;

            var elements = _script.mainWindow?.nestedWindow?.GetElements();
            if(elements != null)
            {
                yield return new WaitForSecondsRealtime(0.1f);
                var slider = (UIDynamicSlider) elements[baseForceJsf.name];
                if(slider != null)
                {
                    while(slider.IsClickDown())
                    {
                        yield return new WaitForSecondsRealtime(0.1f);
                    }

                    yield return new WaitForSecondsRealtime(0.1f);
                }
            }

            _combinedSyncInProgress = false;
            yield return DeferSyncMassCombined(baseForceJsf.val);
        }

        private IEnumerator DeferSyncMassCombined(float value)
        {
            // In case hard colliders are not enabled (yet)
            float timeout = Time.unscaledTime + 3f;
            while(colliderConfigs.Any(config => !config.HasRigidbodies()) && Time.unscaledTime < timeout)
            {
                yield return new WaitForSecondsRealtime(0.3f);
            }

            yield return new WaitForSecondsRealtime(0.1f);

            if(colliderConfigs.Any(config => !config.HasRigidbodies()))
            {
                LogSyncMassFailure();
            }
            else
            {
                colliderConfigs.ForEach(config => config.UpdateRigidbodyMass(value * config.forceJsf.val));
            }
        }

        public JSONClass Serialize()
        {
            var jsonClass = new JSONClass();
            if(Gender.isFemale)
            {
                jsonClass[USE_ADVANCED_COLLIDERS].AsBool = _originalUseAdvancedColliders;
                jsonClass[USE_AUX_BREAST_COLLIDERS].AsBool = _originalUseAuxBreastColliders;
            }

            return jsonClass;
        }

        public void RestoreFromJSON(JSONClass originalJson)
        {
            if(originalJson.HasKey(USE_ADVANCED_COLLIDERS))
            {
                _originalUseAdvancedColliders = originalJson[USE_ADVANCED_COLLIDERS].AsBool;
            }

            if(originalJson.HasKey(USE_AUX_BREAST_COLLIDERS))
            {
                _originalUseAuxBreastColliders = originalJson[USE_AUX_BREAST_COLLIDERS].AsBool;
            }
        }

        private void RestoreDefaults()
        {
            colliderConfigs.ForEach(config =>
            {
                if(!enabledJsb.val)
                {
                    config.SetEnabled(true);
                }

                config.RestoreDefaults();
            });

            StartCoroutine(DeferRestoreDefaultMass());
            _geometry.useAuxBreastColliders = _originalUseAuxBreastColliders;
        }

        private IEnumerator DeferRestoreDefaultMass()
        {
            // e.g. in case hard colliders are not enabled (yet)
            yield return new WaitForSecondsRealtime(0.1f);
            float timeout = Time.unscaledTime + 3f;

            while(colliderConfigs.Any(config => !config.HasRigidbodies()) && Time.unscaledTime < timeout)
            {
                yield return new WaitForSecondsRealtime(0.3f);
            }

            yield return new WaitForSecondsRealtime(0.1f);

            if(colliderConfigs.Any(config => !config.HasRigidbodies()))
            {
                Utils.LogError("Failed restoring hard colliders mass to default.");
            }
            else
            {
                colliderConfigs.ForEach(config => config.RestoreDefaultMass());
            }

            _geometry.useAdvancedColliders = _originalUseAdvancedColliders;
        }

        private void OnEnable()
        {
            if(_script == null || !_script.initDone)
            {
                return;
            }

            SyncUseHardColliders(enabledJsb.val);
        }

        private void OnDisable()
        {
            if(Gender.isFemale)
            {
                RestoreDefaults();
            }
        }

        private void LogSyncMassFailure()
        {
            string message = "Unable to apply collision force: ";
            if(!_geometry.useAdvancedColliders)
            {
                message +=
                    "Advanced Colliders are not enabled in Control & Physics 1 tab. " +
                    "Enabling them and toggling hard colliders on will auto-apply the current collision force.";
                Utils.LogMessage(message);
            }
            else
            {
                message += "Unknown reason.";
                Utils.LogMessage(message);
            }
        }
    }
}
