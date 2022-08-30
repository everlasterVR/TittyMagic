using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TittyMagic.Components;
using TittyMagic.Configs;
using TittyMagic.UI;
using UnityEngine;
using static TittyMagic.Script;

namespace TittyMagic.Handlers
{
    internal class HardColliderHandler : MonoBehaviour
    {
        private DAZCharacterSelector _geometry;
        private Rigidbody _chestRb;

        public JSONStorableStringChooser colliderGroupsJsc { get; private set; }
        public List<ColliderConfigGroup> colliderConfigs { get; private set; }
        public JSONStorableFloat baseForceJsf { get; private set; }
        public JSONStorableBool highlightAllJsb { get; private set; }

        private float _frictionSizeMultiplierLeft;
        private float _frictionSizeMultiplierRight;

        private Dictionary<string, Dictionary<string, Scaler>> _scalingConfigs;

        private const string COLLISION_FORCE = "CollisionForce";
        private const string COLLIDER_RADIUS = "ColliderRadius";
        private const string COLLIDER_CENTER_X = "ColliderCenterX";
        private const string COLLIDER_CENTER_Y = "ColliderCenterY";
        private const string COLLIDER_CENTER_Z = "ColliderCenterZ";

        public void Init(DAZCharacterSelector geometry, Rigidbody chestRb)
        {
            _geometry = geometry;
            _chestRb = chestRb;

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
            highlightAllJsb = tittyMagic.NewJSONStorableBool("highlightAllColliders", false, shouldRegister: false);
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
                                offset: 0.010f,
                                range: new float[] { 0, 1 }
                            )
                        },
                        {
                            COLLIDER_RADIUS, new Scaler(
                                offset: -0.43f,
                                range: new float[] { 0, 40 },
                                massCurve: x => 0.27f * Curves.ColliderRadiusAndPositionSizeCurve(x)
                            )
                        },
                        {
                            COLLIDER_CENTER_X, new Scaler(
                                offset: -0.35f,
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
                                offset: 0.025f,
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
                                offset: 0.015f,
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
                            COLLIDER_CENTER_X, new Scaler(
                                offset: -0.32f,
                                range: new float[] { 0, 40 },
                                massCurve: x => -0.12f * Curves.ColliderRadiusAndPositionSizeCurve(x)
                            )
                        },
                        {
                            COLLIDER_CENTER_Y, new Scaler(
                                offset: 0.19f,
                                range: new float[] { 0, 40 },
                                massCurve: x => 0.30f * Curves.ColliderRadiusAndPositionSizeCurve(x)
                            )
                        },
                        {
                            COLLIDER_CENTER_Z, new Scaler(
                                offset: -0.24f,
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
                                offset: -0.010f,
                                range: new float[] { 0, 1 },
                                softnessCurve: x => -0.020f * Curves.InverseSmoothStep(x, 1.00f, 0.70f, 0.00f)
                            )
                        },
                        {
                            COLLIDER_RADIUS, new Scaler(
                                offset: -0.30f,
                                range: new float[] { 0, 40 },
                                massCurve: x => -0.75f * Curves.ColliderRadiusAndPositionSizeCurve(x)
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
                                offset: -0.24f,
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
                                offset: 0.035f,
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

            var frictionMultipliers = new Dictionary<string, float>
            {
                { "Pectoral1", 0.5f },
                { "Pectoral2", 1.0f },
                { "Pectoral3", 1.0f },
                { "Pectoral4", 1.0f },
                { "Pectoral5", 1.0f },
            };

            var colliderConfigGroup = new ColliderConfigGroup(
                id,
                configLeft,
                configRight,
                scalingConfig[COLLISION_FORCE],
                scalingConfig[COLLIDER_RADIUS],
                scalingConfig[COLLIDER_CENTER_X],
                scalingConfig[COLLIDER_CENTER_Y],
                scalingConfig[COLLIDER_CENTER_Z],
                frictionMultipliers[id]
            )
            {
                forceJsf = tittyMagic.NewJSONStorableFloat(id.ToLower() + COLLISION_FORCE, 0.50f, 0.01f, 1.00f),
                radiusJsf = tittyMagic.NewJSONStorableFloat(id.ToLower() + COLLIDER_RADIUS, 0, -1f, 1f),
                rightJsf = tittyMagic.NewJSONStorableFloat(id.ToLower() + COLLIDER_CENTER_X, 0, -1f, 1f),
                upJsf = tittyMagic.NewJSONStorableFloat(id.ToLower() + COLLIDER_CENTER_Y, 0, -1f, 1f),
                lookJsf = tittyMagic.NewJSONStorableFloat(id.ToLower() + COLLIDER_CENTER_Z, 0, -1f, 1f),
            };

            colliderConfigGroup.forceJsf.setCallbackFunction = _ => SyncHardColliderMass(colliderConfigGroup);
            colliderConfigGroup.radiusJsf.setCallbackFunction = _ => SyncHardColliderRadius(colliderConfigGroup);
            colliderConfigGroup.rightJsf.setCallbackFunction = _ => SyncHardColliderPosition(colliderConfigGroup);
            colliderConfigGroup.upJsf.setCallbackFunction = _ => SyncHardColliderPosition(colliderConfigGroup);
            colliderConfigGroup.lookJsf.setCallbackFunction = _ => SyncHardColliderPosition(colliderConfigGroup);

            colliderConfigGroup.EnableMultiplyFriction();

            return colliderConfigGroup;
        }

        private ColliderConfig NewColliderConfig(string id)
        {
            var collider = _geometry.auxBreastColliders.ToList().Find(c => c.name.Contains(id));
            var autoCollider = FindAutoCollider(collider);
            string visualizerEditableId = tittyMagic.colliderVisualizer.EditablesJSON.choices.Find(option => option.EndsWith(id));
            return new ColliderConfig(autoCollider, visualizerEditableId);
        }

        private static AutoCollider FindAutoCollider(Collider collider)
        {
            var updater = tittyMagic.containingAtom.GetComponentInChildren<AutoColliderBatchUpdater>();
            return updater.autoColliders.First(autoCollider =>
                autoCollider.jointCollider != null && autoCollider.jointCollider.name == collider.name);
        }

        private void SyncHardColliderRadius(ColliderConfigGroup config)
        {
            if(!enabled || !Gender.isFemale)
            {
                return;
            }

            config.UpdateDimensions(MainPhysicsHandler.normalizedMass, tittyMagic.softnessAmount);
            SyncSizeAuto();
        }

        private void SyncHardColliderPosition(ColliderConfigGroup config)
        {
            if(!enabled || !Gender.isFemale)
            {
                return;
            }

            config.UpdatePosition(MainPhysicsHandler.normalizedMass, tittyMagic.softnessAmount);
            SyncSizeAuto();
        }

        private void SyncHardColliderMass(ColliderConfigGroup config)
        {
            if(!enabled || !Gender.isFemale)
            {
                return;
            }

            if(!config.waitingForForceSlider)
            {
                StartCoroutine(DeferSyncMass(config));
            }
        }

        public void SyncAllOffsets()
        {
            if(!enabled || !Gender.isFemale)
            {
                return;
            }

            float mass = MainPhysicsHandler.normalizedRealMass;
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
            float averageRadius = colliderConfigs.Average(config => config.left.autoCollider.colliderRadius);
            colliderConfigs.ForEach(config => config.UpdateMaxFrictionalDistance(
                _frictionSizeMultiplierLeft,
                _frictionSizeMultiplierRight,
                averageRadius
            ));
            tittyMagic.colliderVisualizer.SyncPreviews();
        }

        public void UpdateFrictionSizeMultipliers()
        {
            _frictionSizeMultiplierLeft = FrictionSizeMultiplier(VertexIndexGroup.LEFT_BREAST_WIDTH_MARKERS);
            _frictionSizeMultiplierRight = FrictionSizeMultiplier(VertexIndexGroup.RIGHT_BREAST_WIDTH_MARKERS);
        }

        private static float FrictionSizeMultiplier(int[] indices)
        {
            /* experimentally determined with 3kg breasts, is slightly different for different shapes */
            const float maxWidth = 0.17f;
            float width = (tittyMagic.skin.rawSkinnedVerts[indices[0]] - tittyMagic.skin.rawSkinnedVerts[indices[1]]).magnitude;
            float multiplier = Mathf.InverseLerp(0, maxWidth, width);
            return Curves.InverseSmoothStep(multiplier, 1, 0.55f, 0.42f);
        }

        public void UpdateFriction()
        {
            if(!enabled || !Gender.isFemale)
            {
                return;
            }

            colliderConfigs.ForEach(config => config.UpdateFriction(FrictionHandler.maxHardColliderFriction));
        }

        private IEnumerator DeferSyncMass(ColliderConfigGroup config)
        {
            config.waitingForForceSlider = true;
            yield return WaitForSlider(config.forceJsf.name);
            config.waitingForForceSlider = false;

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
                    MainPhysicsHandler.normalizedMass,
                    tittyMagic.softnessAmount
                );
            }
        }

        private bool _waitingForBaseForceSlider;

        public IEnumerator SyncAll()
        {
            if(!enabled || !Gender.isFemale)
            {
                yield break;
            }

            while(_waitingForBaseForceSlider)
            {
                yield return null;
            }

            _geometry.useAuxBreastColliders = true;
            yield return DeferSyncBaseMass();
            SyncAllOffsets();
        }

        public void SyncHardCollidersBaseMass() => SyncHardCollidersBaseMass(0);

        private void SyncHardCollidersBaseMass(float value)
        {
            if(!enabled || !Gender.isFemale)
            {
                return;
            }

            if(!_waitingForBaseForceSlider)
            {
                StartCoroutine(DeferSyncBaseMass());
            }
        }

        private IEnumerator DeferSyncBaseMass()
        {
            _waitingForBaseForceSlider = true;
            yield return WaitForSlider(baseForceJsf.name);
            _waitingForBaseForceSlider = false;

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
                    MainPhysicsHandler.normalizedMass,
                    tittyMagic.softnessAmount
                ));
            }
        }

        private IEnumerator WaitForSlider(string sliderName)
        {
            var hardColliderWindow = tittyMagic.mainWindow?.GetActiveNestedWindow() as HardCollidersWindow;
            if(hardColliderWindow == null)
            {
                yield break;
            }

            var elements = sliderName == baseForceJsf.name
                ? hardColliderWindow.GetElements()
                : hardColliderWindow.colliderSectionElements;
            if(elements.Any())
            {
                yield return new WaitForSecondsRealtime(0.1f);
                var slider = (UIDynamicSlider) elements[sliderName];
                if(slider != null)
                {
                    while(slider.PointerIsDown())
                    {
                        yield return new WaitForSecondsRealtime(0.1f);
                    }

                    yield return new WaitForSecondsRealtime(0.1f);
                }
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

        public void CalibrateColliders()
        {
            if(!Gender.isFemale)
            {
                return;
            }

            var breastCenterLeft = BreastCenter(VertexIndexGroup.LEFT_BREAST);
            var breastCenterRight = BreastCenter(VertexIndexGroup.RIGHT_BREAST);
            colliderConfigs.ForEach(config => config.Calibrate(breastCenterLeft, breastCenterRight, _chestRb));
        }

        public void UpdateDistanceDiffs()
        {
            if(!Gender.isFemale)
            {
                return;
            }

            var breastCenterLeft = BreastCenter(VertexIndexGroup.LEFT_BREAST);
            var breastCenterRight = BreastCenter(VertexIndexGroup.RIGHT_BREAST);
            colliderConfigs.ForEach(config => config.UpdateDistanceDiffs(breastCenterLeft, breastCenterRight, _chestRb));
        }

        private Vector3 BreastCenter(IEnumerable<int> vertexIndices) =>
            Calc.RelativePosition(_chestRb, Calc.AveragePosition(vertexIndices.Select(index => tittyMagic.skin.rawSkinnedVerts[index]).ToArray()));

        public void ResetDistanceDiffs()
        {
            if(!enabled || !Gender.isFemale)
            {
                return;
            }

            colliderConfigs.ForEach(config => config.ResetDistanceDiffs());
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
            if(tittyMagic == null || !tittyMagic.isInitialized)
            {
                return;
            }

            colliderConfigs.ForEach(config => config.EnableMultiplyFriction());

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
            }
            else if(_geometry.gender != DAZCharacterSelector.Gender.Female)
            {
                message += "Current character is male. Reload the plugin.";
            }
            else
            {
                message += "Unknown reason. Please report a bug.";
            }

            Utils.LogMessage(message);
        }
    }
}
