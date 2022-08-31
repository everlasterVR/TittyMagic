using System.Collections.Generic;
using System.Linq;
using TittyMagic.Components;
using TittyMagic.Configs;
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

            /* Enable hard colliders on init */
            _geometry.useAuxBreastColliders = true;

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
            baseForceJsf.setCallbackFunction = _ => SyncCollidersMass();
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
                                offset: -0.72f,
                                range: new float[] { 0, 40 },
                                massCurve: x => -0.08f * Curves.ColliderRadiusAndPositionSizeCurve2(x)
                            )
                        },
                        {
                            COLLIDER_CENTER_X, new Scaler(
                                offset: 0.88f,
                                range: new float[] { 0, 40 },
                                massCurve: x => 0.48f * Curves.ColliderRadiusAndPositionSizeCurve2(x)
                            )
                        },
                        {
                            COLLIDER_CENTER_Y, new Scaler(
                                offset: -0.15f,
                                range: new float[] { 0, 40 },
                                massCurve: x => -0.11f * Curves.ColliderRadiusAndPositionSizeCurve2(x)
                            )
                        },
                        {
                            COLLIDER_CENTER_Z, new Scaler(
                                offset: -0.80f,
                                range: new float[] { 0, 40 },
                                massCurve: x => 0.03f * Curves.ColliderRadiusAndPositionSizeCurve2(x)
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
                                offset: -0.30f,
                                range: new float[] { 0, 40 },
                                massCurve: x => -0.94f * Curves.ColliderRadiusAndPositionSizeCurve2(x)
                            )
                        },
                        {
                            COLLIDER_CENTER_X, new Scaler(
                                offset: -0.32f,
                                range: new float[] { 0, 40 },
                                massCurve: x => -0.12f * Curves.ColliderRadiusAndPositionSizeCurve2(x)
                            )
                        },
                        {
                            COLLIDER_CENTER_Y, new Scaler(
                                offset: 0.15f,
                                range: new float[] { 0, 40 }
                            )
                        },
                        {
                            COLLIDER_CENTER_Z, new Scaler(
                                offset: -0.24f,
                                range: new float[] { 0, 40 },
                                massCurve: x => -0.42f * Curves.ColliderRadiusAndPositionSizeCurve2(x)
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
                                offset: -0.40f,
                                range: new float[] { 0, 40 },
                                massCurve: x => -0.62f * Curves.ColliderRadiusAndPositionSizeCurve2(x)
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
                                massCurve: x => 0.35f * Curves.ColliderRadiusAndPositionSizeCurve(x)
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
                                offset: -0.58f,
                                range: new float[] { 0, 40 },
                                massCurve: x => -1.24f * Curves.ColliderRadiusAndPositionSizeCurve2(x)
                            )
                        },
                        {
                            COLLIDER_CENTER_X, new Scaler(
                                offset: -0.17f,
                                range: new float[] { 0, 40 },
                                massCurve: x => -0.84f * Curves.ColliderRadiusAndPositionSizeCurve2(x)
                            )
                        },
                        {
                            COLLIDER_CENTER_Y, new Scaler(
                                offset: 0.90f,
                                range: new float[] { 0, 40 },
                                massCurve: x => 0.65f * Curves.ColliderRadiusAndPositionSizeCurve2(x)
                            )
                        },
                        {
                            COLLIDER_CENTER_Z, new Scaler(
                                offset: -0.26f,
                                range: new float[] { 0, 40 },
                                massCurve: x => 0.18f * Curves.ColliderRadiusAndPositionSizeCurve2(x)
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

            colliderConfigGroup.forceJsf.setCallbackFunction = _ => SyncColliderMass(colliderConfigGroup);
            colliderConfigGroup.radiusJsf.setCallbackFunction = _ => SyncRadius(colliderConfigGroup);
            colliderConfigGroup.rightJsf.setCallbackFunction = _ => SyncPosition(colliderConfigGroup);
            colliderConfigGroup.upJsf.setCallbackFunction = _ => SyncPosition(colliderConfigGroup);
            colliderConfigGroup.lookJsf.setCallbackFunction = _ => SyncPosition(colliderConfigGroup);

            colliderConfigGroup.EnableMultiplyFriction();

            return colliderConfigGroup;
        }

        private ColliderConfig NewColliderConfig(string id)
        {
            var collider = _geometry.auxBreastColliders.ToList().Find(c => c.name.Contains(id));
            /* Find auto collider */
            var autoColliders = tittyMagic.containingAtom.GetComponentInChildren<AutoColliderBatchUpdater>().autoColliders;
            var autoCollider = autoColliders.First(ac => ac.jointCollider != null && ac.jointCollider.name == collider.name);

            string visualizerEditableId = tittyMagic.colliderVisualizer.EditablesJSON.choices.Find(option => option.EndsWith(id));
            return new ColliderConfig(autoCollider, visualizerEditableId);
        }

        private void SyncRadius(ColliderConfigGroup config)
        {
            if(!enabled || !Gender.isFemale)
            {
                return;
            }

            config.UpdateDimensions(MainPhysicsHandler.normalizedMass, tittyMagic.softnessAmount);
            SyncSizeAuto();
        }

        private void SyncPosition(ColliderConfigGroup config)
        {
            if(!enabled || !Gender.isFemale)
            {
                return;
            }

            config.UpdatePosition(MainPhysicsHandler.normalizedMass, tittyMagic.softnessAmount);
            SyncSizeAuto();
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

        private void SyncColliderMass(ColliderConfigGroup config)
        {
            if(!enabled || !Gender.isFemale)
            {
                return;
            }

            config.UpdateRigidbodyMass(
                1 / Utils.PhysicsRateMultiplier() * baseForceJsf.val * config.forceJsf.val,
                MainPhysicsHandler.normalizedMass,
                tittyMagic.softnessAmount
            );
        }

        public void SyncCollidersMass() => colliderConfigs?.ForEach(SyncColliderMass);

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
            _geometry.useAuxBreastColliders = true;
        }

        private void OnDisable()
        {
            if(Gender.isFemale)
            {
                /* Required for restoring default collider mass. Enabled in case disabled
                 * programmatically, and not yet restored back on by SettingsMonitor.
                 */
                _geometry.useAuxBreastColliders = true;
                /* Restore defaults */
                colliderConfigs.ForEach(config => config.RestoreDefaults());
                _geometry.useAdvancedColliders = _originalUseAdvancedColliders;
                _geometry.useAuxBreastColliders = _originalUseAuxBreastColliders;
            }
        }
    }
}
