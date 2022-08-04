using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using TittyMagic.Configs;
using TittyMagic.UI;
using UnityEngine;
using static TittyMagic.ParamName;

namespace TittyMagic
{
    internal class HardColliderHandler : MonoBehaviour
    {
        private Script _script;
        private DAZCharacterSelector _geometry;
        private bool _originalUseAdvancedColliders;
        private bool _originalUseAuxBreastColliders;

        public JSONStorableStringChooser colliderGroupsJsc { get; private set; }
        public List<ColliderConfigGroup> colliderConfigs { get; private set; }
        public JSONStorableFloat baseForceJsf { get; private set; }
        public JSONStorableBool highlightAllJsb { get; private set; }

        private Dictionary<string, Dictionary<string, Scaler>> _sliderScalers;
        private Dictionary<string, Dictionary<string, StaticPhysicsConfig>> _multiplierConfigs;

        private const string COLLIDER_FORCE = "ColliderForce";
        private const string COLLIDER_RADIUS = "ColliderRadius";
        private const string COLLIDER_LENGTH = "ColliderLength";
        private const string COLLIDER_CENTER_X = "ColliderCenterX";
        private const string COLLIDER_CENTER_Y = "ColliderCenterY";
        private const string COLLIDER_CENTER_Z = "ColliderCenterZ";

        private bool _combinedSyncInProgress;

        public void Init()
        {
            _script = gameObject.GetComponent<Script>();
            _geometry = (DAZCharacterSelector) _script.containingAtom.GetStorableByID("geometry");
            _originalUseAdvancedColliders = _geometry.useAdvancedColliders;
            _geometry.useAdvancedColliders = true;
            _originalUseAuxBreastColliders = _geometry.useAuxBreastColliders;

            if(!Gender.isFemale)
            {
                return;
            }

            CreateSliderScalers();
            CreateMultiplierConfigs();
            colliderConfigs = new List<ColliderConfigGroup>
            {
                NewColliderConfigGroup("Pectoral1"),
                NewColliderConfigGroup("Pectoral2"),
                NewColliderConfigGroup("Pectoral3"),
                NewColliderConfigGroup("Pectoral4"),
                NewColliderConfigGroup("Pectoral5"),
            };

            var options = colliderConfigs.Select(c => c.visualizerEditableId).ToList();
            var displayOptions = colliderConfigs.Select(c => c.id).ToList();
            colliderGroupsJsc = new JSONStorableStringChooser(
                "colliderGroup",
                options,
                displayOptions,
                "",
                "Collider"
            );
            colliderGroupsJsc.setCallbackFunction = value =>
            {
                _script.colliderVisualizer.EditablesJSON.val = value;
                SyncSizeAuto();
            };
            colliderGroupsJsc.val = options[0];

            _script.colliderVisualizer.GroupsJSON.setJSONCallbackFunction = jsc =>
            {
                _script.colliderVisualizer.SelectedPreviewOpacityJSON.val = jsc.val == "Off" ? 0 : 1;
                if(jsc.val != "Off")
                {
                    _script.colliderVisualizer.EditablesJSON.val = colliderGroupsJsc.val;
                }

                SyncSizeAuto();
            };
            _script.colliderVisualizer.XRayPreviewsJSON.setJSONCallbackFunction = _ => SyncSizeAuto();

            baseForceJsf = _script.NewJSONStorableFloat("combinedColliderForce", 0.50f, 0.01f, 1.00f);
            baseForceJsf.setCallbackFunction = SyncHardCollidersBaseMass;
            highlightAllJsb = _script.NewJSONStorableBool("highlightAll", false, register: false);
            highlightAllJsb.setCallbackFunction = value => _script.colliderVisualizer.PreviewOpacityJSON.val = value ? 1.00f : 0.67f;
        }

        private void CreateSliderScalers()
        {
            _sliderScalers = new Dictionary<string, Dictionary<string, Scaler>>
            {
                {
                    "Pectoral1", new Dictionary<string, Scaler>
                    {
                        { COLLIDER_FORCE, new Scaler(-0.008f) },
                        { COLLIDER_RADIUS, new Scaler(-0.34f, 0, 40) },
                        { COLLIDER_LENGTH, new Scaler(0.38f, 0, 40) },
                        { COLLIDER_CENTER_X, new Scaler(-0.43f, 0, 40) },
                        { COLLIDER_CENTER_Y, new Scaler(0.23f, 0, 40) },
                        { COLLIDER_CENTER_Z, new Scaler(-0.40f, 0, 40) },
                    }
                },
                {
                    "Pectoral2", new Dictionary<string, Scaler>
                    {
                        { COLLIDER_FORCE, new Scaler(-0.016f) },
                        { COLLIDER_RADIUS, new Scaler(0.03f, 0, 40) },
                        { COLLIDER_LENGTH, new Scaler(0.20f, 0, 40) },
                        { COLLIDER_CENTER_X, new Scaler(0.37f, 0, 40) },
                        { COLLIDER_CENTER_Y, new Scaler(0.25f, 0, 40) },
                        { COLLIDER_CENTER_Z, new Scaler(-0.82f, 0, 40) },
                    }
                },
                {
                    "Pectoral3", new Dictionary<string, Scaler>
                    {
                        { COLLIDER_FORCE, new Scaler(-0.008f) },
                        { COLLIDER_RADIUS, new Scaler(-0.22f, 0, 40) },
                        { COLLIDER_LENGTH, new Scaler(-0.26f, 0, 40) },
                        { COLLIDER_CENTER_X, new Scaler(0.13f, 0, 40) },
                        { COLLIDER_CENTER_Y, new Scaler(0.13f, 0, 40) },
                        { COLLIDER_CENTER_Z, new Scaler(-0.40f, 0, 40) },
                    }
                },
                {
                    "Pectoral4", new Dictionary<string, Scaler>
                    {
                        { COLLIDER_FORCE, new Scaler(-0.008f) },
                        { COLLIDER_RADIUS, new Scaler(-0.26f, 0, 40) },
                        { COLLIDER_LENGTH, new Scaler(-0.63f, 0, 40) },
                        { COLLIDER_CENTER_X, new Scaler(-0.39f, 0, 40) },
                        { COLLIDER_CENTER_Y, new Scaler(0.05f, 0, 40) },
                        { COLLIDER_CENTER_Z, new Scaler(-0.53f, 0, 40) },
                    }
                },
                {
                    "Pectoral5", new Dictionary<string, Scaler>
                    {
                        { COLLIDER_FORCE, new Scaler(-0.0016f) },
                        { COLLIDER_RADIUS, new Scaler(-0.85f, 0, 40) },
                        { COLLIDER_LENGTH, new Scaler(-0.30f, 0, 40) },
                        { COLLIDER_CENTER_X, new Scaler(-0.60f, 0, 40) },
                        { COLLIDER_CENTER_Y, new Scaler(0.70f, 0, 40) },
                        { COLLIDER_CENTER_Z, new Scaler(-0.45f, 0, 40) },
                    }
                },
            };
        }

        private void CreateMultiplierConfigs()
        {
            _multiplierConfigs = new Dictionary<string, Dictionary<string, StaticPhysicsConfig>>
            {
                {
                    "Pectoral1", new Dictionary<string, StaticPhysicsConfig>
                    {
                        { COLLIDER_FORCE, new StaticPhysicsConfig(1.00f) },
                        { COLLIDER_RADIUS, new StaticPhysicsConfig(1.00f) },
                        { COLLIDER_LENGTH, new StaticPhysicsConfig(1.00f) },
                    }
                },
                {
                    "Pectoral2", new Dictionary<string, StaticPhysicsConfig>
                    {
                        { COLLIDER_FORCE, new StaticPhysicsConfig(1.00f) },
                        { COLLIDER_RADIUS, new StaticPhysicsConfig(1.00f) },
                        { COLLIDER_LENGTH, new StaticPhysicsConfig(1.00f) },
                    }
                },
                {
                    "Pectoral3", new Dictionary<string, StaticPhysicsConfig>
                    {
                        { COLLIDER_FORCE, new StaticPhysicsConfig(1.00f) },
                        { COLLIDER_RADIUS, new StaticPhysicsConfig(1.00f) },
                        { COLLIDER_LENGTH, new StaticPhysicsConfig(1.00f) },
                    }
                },
                {
                    "Pectoral4", new Dictionary<string, StaticPhysicsConfig>
                    {
                        { COLLIDER_FORCE, new StaticPhysicsConfig(1.00f) },
                        { COLLIDER_RADIUS, new StaticPhysicsConfig(1.00f) },
                        { COLLIDER_LENGTH, new StaticPhysicsConfig(1.00f) },
                    }
                },
                {
                    "Pectoral5", new Dictionary<string, StaticPhysicsConfig>
                    {
                        { COLLIDER_FORCE, new StaticPhysicsConfig(1.00f) },
                        { COLLIDER_RADIUS, new StaticPhysicsConfig(1.00f) },
                        { COLLIDER_LENGTH, new StaticPhysicsConfig(1.00f) },
                    }
                },
            };
        }

        private ColliderConfigGroup NewColliderConfigGroup(string id)
        {
            var configLeft = NewColliderConfig("l" + id);
            var configRight = NewColliderConfig("r" + id);
            var sliderScalers = _sliderScalers[id];
            var multiplierConfigs = _multiplierConfigs[id];
            var colliderConfigGroup = new ColliderConfigGroup(
                id,
                configLeft,
                configRight,
                multiplierConfigs[COLLIDER_FORCE],
                multiplierConfigs[COLLIDER_RADIUS],
                multiplierConfigs[COLLIDER_LENGTH],
                sliderScalers[COLLIDER_FORCE],
                sliderScalers[COLLIDER_RADIUS],
                sliderScalers[COLLIDER_LENGTH],
                sliderScalers[COLLIDER_CENTER_X],
                sliderScalers[COLLIDER_CENTER_Y],
                sliderScalers[COLLIDER_CENTER_Z]
            )
            {
                forceJsf = _script.NewJSONStorableFloat(id.ToLower() + COLLIDER_FORCE, 0.50f, 0.01f, 1.00f),
                radiusJsf = _script.NewJSONStorableFloat(id.ToLower() + COLLIDER_RADIUS, 0, -1f, 1f),
                lengthJsf = _script.NewJSONStorableFloat(id.ToLower() + COLLIDER_LENGTH, 0, -1f, 1f),
                rightJsf = _script.NewJSONStorableFloat(id.ToLower() + COLLIDER_CENTER_X, 0, -1f, 1f),
                upJsf = _script.NewJSONStorableFloat(id.ToLower() + COLLIDER_CENTER_Y, 0, -1f, 1f),
                lookJsf = _script.NewJSONStorableFloat(id.ToLower() + COLLIDER_CENTER_Z, 0, -1f, 1f),
            };

            colliderConfigGroup.forceJsf.setCallbackFunction = _ => SyncHardColliderMass(colliderConfigGroup);
            colliderConfigGroup.radiusJsf.setCallbackFunction = _ => SyncHardColliderRadius(colliderConfigGroup);
            colliderConfigGroup.lengthJsf.setCallbackFunction = _ => SyncHardColliderLength(colliderConfigGroup);
            colliderConfigGroup.rightJsf.setCallbackFunction = _ => SyncHardColliderRightOffset(colliderConfigGroup);
            colliderConfigGroup.upJsf.setCallbackFunction = _ => SyncHardColliderUpOffset(colliderConfigGroup);
            colliderConfigGroup.lookJsf.setCallbackFunction = _ => SyncHardColliderLookOffset(colliderConfigGroup);

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

        private void SyncHardColliderRadius(ColliderConfigGroup config)
        {
            if(!enabled)
            {
                return;
            }

            config.UpdateRadius(_script.mainPhysicsHandler.normalizedMass, _script.softnessAmount);
            SyncSizeAuto();
        }

        private void SyncHardColliderLength(ColliderConfigGroup config)
        {
            if(!enabled)
            {
                return;
            }

            config.UpdateLength(_script.mainPhysicsHandler.normalizedMass, _script.softnessAmount);
            SyncSizeAuto();
        }

        private void SyncHardColliderLookOffset(ColliderConfigGroup config)
        {
            if(!enabled)
            {
                return;
            }

            config.UpdateLookOffset();
            SyncSizeAuto();
        }

        private void SyncHardColliderUpOffset(ColliderConfigGroup config)
        {
            if(!enabled)
            {
                return;
            }

            config.UpdateUpOffset();
            SyncSizeAuto();
        }

        private void SyncHardColliderRightOffset(ColliderConfigGroup config)
        {
            if(!enabled)
            {
                return;
            }

            config.UpdateRightOffset();
            SyncSizeAuto();
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

        public void SyncAllOffsets()
        {
            colliderConfigs.ForEach(config =>
            {
                config.UpdateRadius(_script.mainPhysicsHandler.normalizedMass, _script.softnessAmount);
                config.UpdateLength(_script.mainPhysicsHandler.normalizedMass, _script.softnessAmount);
                config.UpdateLookOffset();
                config.UpdateUpOffset();
                config.UpdateRightOffset();
            });
            SyncSizeAuto();
        }

        public void SyncSizeAuto()
        {
            colliderConfigs.ForEach(config => config.AutoColliderSizeSet());
            _script.colliderVisualizer.SyncPreviews();
        }

        private IEnumerator DeferBeginSyncMass(ColliderConfigGroup config)
        {
            config.syncInProgress = true;

            var elements = ((HardCollidersWindow) _script.mainWindow.GetActiveNestedWindow())?.colliderSectionElements;
            if(elements != null && elements.Any())
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
                yield return new WaitForSecondsRealtime(0.2f);
            }

            yield return new WaitForSecondsRealtime(0.1f);

            if(!config.HasRigidbodies())
            {
                LogSyncMassFailure();
            }
            else
            {
                config.UpdateRigidbodyMass(
                    1 / Utils.PhysicsRateMultiplier() * baseForceJsf.val * config.forceJsf.val,
                    _script.mainPhysicsHandler.normalizedMass,
                    _script.softnessAmount
                );
            }
        }

        public IEnumerator SyncAll()
        {
            if(!enabled)
            {
                yield break;
            }

            while(_combinedSyncInProgress)
            {
                yield return null;
            }

            yield return DeferBeginSyncBaseMass();
            SyncAllOffsets();
        }

        public void SyncHardCollidersBaseMass() => SyncHardCollidersBaseMass(0);

        private void SyncHardCollidersBaseMass(float value)
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

            var elements = _script.mainWindow?.GetActiveNestedWindow()?.GetElements();
            if(elements != null && elements.Any())
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
            yield return DeferSyncMassCombined();
        }

        private IEnumerator DeferSyncMassCombined()
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
                colliderConfigs.ForEach(config => config.UpdateRigidbodyMass(
                    1 / Utils.PhysicsRateMultiplier() * baseForceJsf.val * config.forceJsf.val,
                    _script.mainPhysicsHandler.normalizedMass,
                    _script.softnessAmount
                ));
            }
        }

        public JSONClass GetOriginalsJSON()
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
            colliderConfigs.ForEach(config => config.RestoreDefaults());
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

            SyncHardCollidersBaseMass(baseForceJsf.val);
            SyncAllOffsets();
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
