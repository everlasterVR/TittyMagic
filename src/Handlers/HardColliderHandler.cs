using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TittyMagic.Configs;
using TittyMagic.UI;
using UnityEngine;
using static TittyMagic.Script;

namespace TittyMagic.Handlers
{
    internal class HardColliderHandler : MonoBehaviour
    {
        private DAZCharacterSelector _geometry;

        public JSONStorableStringChooser colliderGroupsJsc { get; private set; }
        public List<ColliderConfigGroup> colliderConfigs { get; private set; }
        public JSONStorableFloat baseForceJsf { get; private set; }
        public JSONStorableBool highlightAllJsb { get; private set; }

        private Dictionary<string, Dictionary<string, Scaler>> _scalingConfigs;

        private const string COLLISION_FORCE = "CollisionForce";
        private const string COLLIDER_RADIUS = "ColliderRadius";
        private const string COLLIDER_LENGTH = "ColliderLength";
        private const string COLLIDER_CENTER_X = "ColliderCenterX";
        private const string COLLIDER_CENTER_Y = "ColliderCenterY";
        private const string COLLIDER_CENTER_Z = "ColliderCenterZ";

        private bool _combinedSyncInProgress;

        public void Init()
        {
            _geometry = (DAZCharacterSelector) tittyMagic.containingAtom.GetStorableByID("geometry");

            if(!Gender.isFemale)
            {
                return;
            }

            CreateScalingConfigs();
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
                tittyMagic.colliderVisualizer.EditablesJSON.val = value;
                SyncSizeAuto();
            };
            colliderGroupsJsc.val = options[0];

            tittyMagic.colliderVisualizer.GroupsJSON.setJSONCallbackFunction = jsc =>
            {
                tittyMagic.colliderVisualizer.SelectedPreviewOpacityJSON.val = jsc.val == "Off" ? 0 : 1;
                if(jsc.val != "Off")
                {
                    tittyMagic.colliderVisualizer.EditablesJSON.val = colliderGroupsJsc.val;
                }

                SyncSizeAuto();
            };
            tittyMagic.colliderVisualizer.XRayPreviewsJSON.setJSONCallbackFunction = _ => SyncSizeAuto();

            baseForceJsf = tittyMagic.NewJSONStorableFloat("baseCollisionForce", 0.75f, 0.01f, 1.50f);
            baseForceJsf.setCallbackFunction = SyncHardCollidersBaseMass;
            highlightAllJsb = tittyMagic.NewJSONStorableBool("highlightAllColliders", false, register: false);
            highlightAllJsb.setCallbackFunction = value => tittyMagic.colliderVisualizer.PreviewOpacityJSON.val = value ? 1.00f : 0.67f;
        }

        private void CreateScalingConfigs()
        {
            _scalingConfigs = new Dictionary<string, Dictionary<string, Scaler>>
            {
                {
                    "Pectoral1", new Dictionary<string, Scaler>
                    {
                        {
                            COLLISION_FORCE, new Scaler(
                                offset: 0.030f,
                                range: new float[] { 0, 1 }
                            )
                        },
                        {
                            COLLIDER_RADIUS, new Scaler(
                                offset: -0.34f,
                                range: new float[] { 0, 40 },
                                massCurve: x => 0.27f * Curves.ColliderRadiusAndPositionSizeCurve(x)
                            )
                        },
                        {
                            COLLIDER_LENGTH, new Scaler(
                                offset: 0.00f,
                                range: new float[] { 0, 40 }
                            )
                        },
                        {
                            COLLIDER_CENTER_X, new Scaler(
                                offset: -0.10f,
                                range: new float[] { 0, 40 },
                                massCurve: x => -0.32f * Curves.ColliderRadiusAndPositionSizeCurve(x)
                            )
                        },
                        {
                            COLLIDER_CENTER_Y, new Scaler(
                                offset: 0.30f,
                                range: new float[] { 0, 40 },
                                massCurve: x => 0.38f * Curves.ColliderRadiusAndPositionSizeCurve(x)
                            )
                        },
                        {
                            COLLIDER_CENTER_Z, new Scaler(
                                offset: -0.25f,
                                range: new float[] { 0, 40 }
                            )
                        },
                    }
                },
                {
                    "Pectoral2", new Dictionary<string, Scaler>
                    {
                        {
                            COLLISION_FORCE, new Scaler(
                                offset: 0.040f,
                                range: new float[] { 0, 1 },
                                softnessCurve: x => -0.020f * Curves.InverseSmoothStep(x, 1.00f, 0.70f, 0.00f)
                            )
                        },
                        {
                            COLLIDER_RADIUS, new Scaler(
                                offset: -0.65f,
                                range: new float[] { 0, 40 },
                                massCurve: x => -0.18f * Curves.ColliderRadiusAndPositionSizeCurve(x)
                            )
                        },
                        {
                            COLLIDER_LENGTH, new Scaler(
                                offset: 0.00f,
                                range: new float[] { 0, 40 }
                            )
                        },
                        {
                            COLLIDER_CENTER_X, new Scaler(
                                offset: 0.88f,
                                range: new float[] { 0, 40 },
                                massCurve: x => 0.48f * Curves.ColliderRadiusAndPositionSizeCurve(x)
                            )
                        },
                        {
                            COLLIDER_CENTER_Y, new Scaler(
                                offset: -0.15f,
                                range: new float[] { 0, 40 },
                                massCurve: x => -0.11f * Curves.ColliderRadiusAndPositionSizeCurve(x)
                            )
                        },
                        {
                            COLLIDER_CENTER_Z, new Scaler(
                                offset: -0.80f,
                                range: new float[] { 0, 40 },
                                massCurve: x => 0.03f * Curves.ColliderRadiusAndPositionSizeCurve(x)
                            )
                        },
                    }
                },
                {
                    "Pectoral3", new Dictionary<string, Scaler>
                    {
                        {
                            COLLISION_FORCE, new Scaler(
                                offset: 0.000f,
                                range: new float[] { 0, 1 },
                                softnessCurve: x => -0.020f * Curves.InverseSmoothStep(x, 1.00f, 0.70f, 0.00f)
                            )
                        },
                        {
                            COLLIDER_RADIUS, new Scaler(
                                offset: -0.20f,
                                range: new float[] { 0, 40 },
                                massCurve: x => -1.07f * Curves.ColliderRadiusAndPositionSizeCurve(x)
                            )
                        },
                        {
                            COLLIDER_LENGTH, new Scaler(
                                offset: 0.00f,
                                range: new float[] { 0, 40 }
                            )
                        },
                        {
                            COLLIDER_CENTER_X, new Scaler(
                                offset: -0.39f,
                                range: new float[] { 0, 40 },
                                massCurve: x => -0.12f * Curves.ColliderRadiusAndPositionSizeCurve(x)
                            )
                        },
                        {
                            COLLIDER_CENTER_Y, new Scaler(
                                offset: 0.12f,
                                range: new float[] { 0, 40 },
                                massCurve: x => 0.30f * Curves.ColliderRadiusAndPositionSizeCurve(x)
                            )
                        },
                        {
                            COLLIDER_CENTER_Z, new Scaler(
                                offset: -0.30f,
                                range: new float[] { 0, 40 },
                                massCurve: x => -0.42f * Curves.ColliderRadiusAndPositionSizeCurve(x)
                            )
                        },
                    }
                },
                {
                    "Pectoral4", new Dictionary<string, Scaler>
                    {
                        {
                            COLLISION_FORCE, new Scaler(
                                offset: 0.020f,
                                range: new float[] { 0, 1 },
                                softnessCurve: x => -0.020f * Curves.InverseSmoothStep(x, 1.00f, 0.70f, 0.00f)
                            )
                        },
                        {
                            COLLIDER_RADIUS, new Scaler(
                                offset: -0.26f,
                                range: new float[] { 0, 40 },
                                massCurve: x => -0.75f * Curves.ColliderRadiusAndPositionSizeCurve(x)
                            )
                        },
                        {
                            COLLIDER_LENGTH, new Scaler(
                                offset: 0.00f,
                                range: new float[] { 0, 40 }
                            )
                        },
                        {
                            COLLIDER_CENTER_X, new Scaler(
                                offset: -0.17f,
                                range: new float[] { 0, 40 },
                                massCurve: x => 0.42f * Curves.ColliderRadiusAndPositionSizeCurve(x)
                            )
                        },
                        {
                            COLLIDER_CENTER_Y, new Scaler(
                                offset: -0.05f,
                                range: new float[] { 0, 40 },
                                massCurve: x => 0.55f * Curves.ColliderRadiusAndPositionSizeCurve(x)
                            )
                        },
                        {
                            COLLIDER_CENTER_Z, new Scaler(
                                offset: -0.21f,
                                range: new float[] { 0, 40 },
                                massCurve: x => 0.12f * Curves.ColliderRadiusAndPositionSizeCurve(x)
                            )
                        },
                    }
                },
                {
                    "Pectoral5", new Dictionary<string, Scaler>
                    {
                        {
                            COLLISION_FORCE, new Scaler(
                                offset: 0.040f,
                                range: new float[] { 0, 1 },
                                softnessCurve: x => -0.055f * Curves.InverseSmoothStep(x, 1.00f, 0.70f, 0.00f)
                            )
                        },
                        {
                            COLLIDER_RADIUS, new Scaler(
                                offset: -0.50f,
                                range: new float[] { 0, 40 },
                                massCurve: x => -1.35f * Curves.ColliderRadiusAndPositionSizeCurve(x)
                            )
                        },
                        {
                            COLLIDER_LENGTH, new Scaler(
                                offset: 0.00f,
                                range: new float[] { 0, 40 }
                            )
                        },
                        {
                            COLLIDER_CENTER_X, new Scaler(
                                offset: -0.24f,
                                range: new float[] { 0, 40 },
                                massCurve: x => -0.85f * Curves.ColliderRadiusAndPositionSizeCurve(x)
                            )
                        },
                        {
                            COLLIDER_CENTER_Y, new Scaler(
                                offset: 0.75f,
                                range: new float[] { 0, 40 },
                                massCurve: x => 0.47f * Curves.ColliderRadiusAndPositionSizeCurve(x)
                            )
                        },
                        {
                            COLLIDER_CENTER_Z, new Scaler(
                                offset: -0.16f,
                                range: new float[] { 0, 40 },
                                massCurve: x => 0.13f * Curves.ColliderRadiusAndPositionSizeCurve(x)
                            )
                        },
                    }
                },
            };
        }

        private ColliderConfigGroup NewColliderConfigGroup(string id)
        {
            var configLeft = NewColliderConfig("l" + id);
            var configRight = NewColliderConfig("r" + id);
            var scalingConfig = _scalingConfigs[id];
            var colliderConfigGroup = new ColliderConfigGroup(
                id,
                configLeft,
                configRight,
                scalingConfig[COLLISION_FORCE],
                scalingConfig[COLLIDER_RADIUS],
                scalingConfig[COLLIDER_LENGTH],
                scalingConfig[COLLIDER_CENTER_X],
                scalingConfig[COLLIDER_CENTER_Y],
                scalingConfig[COLLIDER_CENTER_Z]
            )
            {
                forceJsf = tittyMagic.NewJSONStorableFloat(id.ToLower() + COLLISION_FORCE, 0.50f, 0.01f, 1.00f),
                radiusJsf = tittyMagic.NewJSONStorableFloat(id.ToLower() + COLLIDER_RADIUS, 0, -1f, 1f),
                lengthJsf = tittyMagic.NewJSONStorableFloat(id.ToLower() + COLLIDER_LENGTH, 0, 0f, 5f),
                rightJsf = tittyMagic.NewJSONStorableFloat(id.ToLower() + COLLIDER_CENTER_X, 0, -1f, 1f),
                upJsf = tittyMagic.NewJSONStorableFloat(id.ToLower() + COLLIDER_CENTER_Y, 0, -1f, 1f),
                lookJsf = tittyMagic.NewJSONStorableFloat(id.ToLower() + COLLIDER_CENTER_Z, 0, -1f, 1f),
            };

            colliderConfigGroup.forceJsf.setCallbackFunction = _ => SyncHardColliderMass(colliderConfigGroup);
            colliderConfigGroup.radiusJsf.setCallbackFunction = _ => SyncHardColliderDimensions(colliderConfigGroup);
            colliderConfigGroup.lengthJsf.setCallbackFunction = _ => SyncHardColliderDimensions(colliderConfigGroup);
            colliderConfigGroup.rightJsf.setCallbackFunction = _ => SyncHardColliderPosition(colliderConfigGroup);
            colliderConfigGroup.upJsf.setCallbackFunction = _ => SyncHardColliderPosition(colliderConfigGroup);
            colliderConfigGroup.lookJsf.setCallbackFunction = _ => SyncHardColliderPosition(colliderConfigGroup);

            return colliderConfigGroup;
        }

        private ColliderConfig NewColliderConfig(string id)
        {
            var collider = _geometry.auxBreastColliders.ToList().Find(c => c.name.Contains(id));
            var autoCollider = FindAutoCollider(collider);
            string visualizerEditableId = tittyMagic.colliderVisualizer.EditablesJSON.choices.Find(option => option.EndsWith(id));
            return new ColliderConfig(autoCollider, visualizerEditableId);
        }

        private AutoCollider FindAutoCollider(Collider collider)
        {
            var updater = tittyMagic.containingAtom.GetComponentInChildren<AutoColliderBatchUpdater>();
            return updater.autoColliders.First(autoCollider =>
                autoCollider.jointCollider != null && autoCollider.jointCollider.name == collider.name);
        }

        private void SyncHardColliderDimensions(ColliderConfigGroup config)
        {
            if(!enabled || !Gender.isFemale)
            {
                return;
            }

            config.UpdateDimensions(tittyMagic.mainPhysicsHandler.normalizedMass, tittyMagic.softnessAmount);
            SyncSizeAuto();
        }

        private void SyncHardColliderPosition(ColliderConfigGroup config)
        {
            if(!enabled || !Gender.isFemale)
            {
                return;
            }

            config.UpdatePosition(tittyMagic.mainPhysicsHandler.normalizedMass, tittyMagic.softnessAmount);
            SyncSizeAuto();
        }

        private void SyncHardColliderMass(ColliderConfigGroup config)
        {
            if(!enabled || !Gender.isFemale)
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
            if(!enabled || !Gender.isFemale)
            {
                return;
            }

            float mass = tittyMagic.mainPhysicsHandler.normalizedRealMass;
            float softness = tittyMagic.softnessAmount;
            colliderConfigs.ForEach(config =>
            {
                config.UpdateDimensions(mass, softness);
                config.UpdatePosition(mass, softness);
            });
            SyncSizeAuto();
        }

        private void SyncSizeAuto()
        {
            colliderConfigs.ForEach(config => config.AutoColliderSizeSet());
            tittyMagic.colliderVisualizer.SyncPreviews();
        }

        private IEnumerator DeferBeginSyncMass(ColliderConfigGroup config)
        {
            config.syncInProgress = true;

            var elements = ((HardCollidersWindow) tittyMagic.mainWindow.GetActiveNestedWindow())?.colliderSectionElements;
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
                    tittyMagic.mainPhysicsHandler.normalizedMass,
                    tittyMagic.softnessAmount
                );
            }
        }

        public IEnumerator SyncAll()
        {
            if(!enabled || !Gender.isFemale)
            {
                yield break;
            }

            while(_combinedSyncInProgress)
            {
                yield return null;
            }

            _geometry.useAuxBreastColliders = true;
            yield return DeferBeginSyncBaseMass();
            SyncAllOffsets();
        }

        public void SyncHardCollidersBaseMass() => SyncHardCollidersBaseMass(0);

        private void SyncHardCollidersBaseMass(float value)
        {
            if(!enabled || !Gender.isFemale)
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

            var elements = tittyMagic.mainWindow?.GetActiveNestedWindow()?.GetElements();
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
                    tittyMagic.mainPhysicsHandler.normalizedMass,
                    tittyMagic.softnessAmount
                ));
            }
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

            /* Changes to collider properties must be done while advanced colliders are enabled */
            _geometry.useAdvancedColliders = _originalUseAdvancedColliders;
        }

        private bool _originalUseAdvancedColliders;
        private bool _originalUseAuxBreastColliders;

        public void SaveOriginalUseColliders()
        {
            _originalUseAdvancedColliders = _geometry.useAdvancedColliders;
            _originalUseAuxBreastColliders = _geometry.useAuxBreastColliders;
        }

        private void OnEnable()
        {
            if(tittyMagic == null || !tittyMagic.initDone)
            {
                return;
            }

            SaveOriginalUseColliders();
            _geometry.useAdvancedColliders = true;
            _geometry.useAuxBreastColliders = _originalUseAuxBreastColliders;
        }

        private void OnDisable()
        {
            if(Gender.isFemale)
            {
                /* Restore defaults */
                colliderConfigs.ForEach(config => config.RestoreDefaults());
                _geometry.useAuxBreastColliders = true;
                StartCoroutine(DeferRestoreDefaultMass());
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
                message += "Unknown reason. Please report a bug.";
                Utils.LogMessage(message);
            }
        }
    }
}
