using System.Collections.Generic;
using System.Linq;
using TittyMagic.Components;
using TittyMagic.Models;
using UnityEngine;
using static TittyMagic.Script;

namespace TittyMagic.Handlers
{
    public static class HardColliderHandler
    {
        public static JSONStorableStringChooser colliderGroupsJsc { get; private set; }
        public static List<HardColliderGroup> colliderConfigs { get; private set; }
        public static JSONStorableFloat baseForceJsf { get; private set; }
        public static JSONStorableBool highlightAllJsb { get; private set; }

        private static float _frictionSizeMultiplierLeft;
        private static float _frictionSizeMultiplierRight;

        private static Dictionary<string, Dictionary<string, Scaler>> _scalingConfigs;

        private const string COLLISION_FORCE = "CollisionForce";
        private const string COLLIDER_RADIUS = "ColliderRadius";
        private const string COLLIDER_CENTER_X = "ColliderCenterX";
        private const string COLLIDER_CENTER_Y = "ColliderCenterY";
        private const string COLLIDER_CENTER_Z = "ColliderCenterZ";

        public static void Init()
        {
            if(!personIsFemale)
            {
                return;
            }

            CreateScalingConfigs();
            var autoColliders = tittyMagic.containingAtom.GetComponentInChildren<AutoColliderBatchUpdater>().autoColliders;
            colliderConfigs = new List<HardColliderGroup>
            {
                NewHardColliderGroup("Pectoral1", autoColliders),
                NewHardColliderGroup("Pectoral2", autoColliders),
                NewHardColliderGroup("Pectoral3", autoColliders),
                NewHardColliderGroup("Pectoral4", autoColliders),
                NewHardColliderGroup("Pectoral5", autoColliders),
            };
            EnableMultiplyFriction();

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

        private static void CreateScalingConfigs()
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

        private static HardColliderGroup NewHardColliderGroup(string id, AutoCollider[] autoColliders)
        {
            var configLeft = NewHardCollider("l" + id, autoColliders);
            var configRight = NewHardCollider("r" + id, autoColliders);
            var scalingConfig = _scalingConfigs[id];

            var frictionMultipliers = new Dictionary<string, float>
            {
                { "Pectoral1", 0.5f },
                { "Pectoral2", 1.0f },
                { "Pectoral3", 1.0f },
                { "Pectoral4", 1.0f },
                { "Pectoral5", 1.0f },
            };

            string visualizerEditableId = tittyMagic.colliderVisualizer.EditablesJSON.choices.Find(option => option.EndsWith("l" + id));
            var colliderConfigGroup = new HardColliderGroup(
                id,
                visualizerEditableId,
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

            return colliderConfigGroup;
        }

        private static HardCollider NewHardCollider(string id, IEnumerable<AutoCollider> autoColliders)
        {
            var collider = geometry.auxBreastColliders.ToList().Find(c => c.name.Contains(id));
            var autoCollider = autoColliders.First(ac => ac.jointCollider != null && ac.jointCollider.name == collider.name);
            return new HardCollider(autoCollider);
        }

        private static void SyncRadius(HardColliderGroup config)
        {
            if(!personIsFemale)
            {
                return;
            }

            config.UpdateDimensions(MainPhysicsHandler.normalizedMass, tittyMagic.softnessAmount);
            SyncSizeAuto();
        }

        private static void SyncPosition(HardColliderGroup config)
        {
            if(!personIsFemale)
            {
                return;
            }

            config.UpdatePosition(MainPhysicsHandler.normalizedMass, tittyMagic.softnessAmount);
            SyncSizeAuto();
        }

        public static void SyncAllOffsets()
        {
            if(!personIsFemale)
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

        private static void SyncSizeAuto()
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

        public static void UpdateFrictionSizeMultipliers()
        {
            _frictionSizeMultiplierLeft = FrictionSizeMultiplier(VertexIndexGroup.leftBreastWidthMarkers);
            _frictionSizeMultiplierRight = FrictionSizeMultiplier(VertexIndexGroup.rightBreastWidthMarkers);
        }

        private static float FrictionSizeMultiplier(int[] indices)
        {
            /* experimentally determined with 3kg breasts, is slightly different for different shapes */
            const float maxWidth = 0.17f;
            float width = (skin.rawSkinnedVerts[indices[0]] - skin.rawSkinnedVerts[indices[1]]).magnitude;
            float multiplier = Mathf.InverseLerp(0, maxWidth, width);
            return Curves.InverseSmoothStep(multiplier, 1, 0.55f, 0.42f);
        }

        public static void UpdateFriction()
        {
            if(!personIsFemale)
            {
                return;
            }

            colliderConfigs.ForEach(config => config.UpdateFriction(FrictionHandler.maxHardColliderFriction));
        }

        private static void SyncColliderMass(HardColliderGroup config)
        {
            if(!personIsFemale)
            {
                return;
            }

            config.UpdateRigidbodyMass(
                1 / Utils.PhysicsRateMultiplier() * baseForceJsf.val * config.forceJsf.val,
                MainPhysicsHandler.normalizedMass,
                tittyMagic.softnessAmount
            );
        }

        public static void SyncCollidersMass() => colliderConfigs?.ForEach(SyncColliderMass);

        public static void CalibrateColliders()
        {
            if(!personIsFemale)
            {
                return;
            }

            var breastCenterLeft = BreastCenter(VertexIndexGroup.leftBreast);
            var breastCenterRight = BreastCenter(VertexIndexGroup.rightBreast);
            colliderConfigs.ForEach(config => config.Calibrate(breastCenterLeft, breastCenterRight, MainPhysicsHandler.chestRb));
        }

        public static void UpdateDistanceDiffs()
        {
            if(!personIsFemale)
            {
                return;
            }

            var breastCenterLeft = BreastCenter(VertexIndexGroup.leftBreast);
            var breastCenterRight = BreastCenter(VertexIndexGroup.rightBreast);
            colliderConfigs.ForEach(config => config.UpdateDistanceDiffs(breastCenterLeft, breastCenterRight, MainPhysicsHandler.chestRb));
        }

        private static Vector3 BreastCenter(IEnumerable<int> vertexIndices) =>
            Calc.RelativePosition(
                MainPhysicsHandler.chestRb,
                Calc.AveragePosition(vertexIndices.Select(index => skin.rawSkinnedVerts[index]).ToArray())
            );

        public static void ResetDistanceDiffs()
        {
            if(!personIsFemale)
            {
                return;
            }

            colliderConfigs.ForEach(config => config.ResetDistanceDiffs());
        }

        private static bool _originalUseAdvancedColliders;
        private static bool _originalUseAuxBreastColliders;

        public static void SaveOriginalUseColliders()
        {
            if(!personIsFemale)
            {
                return;
            }

            _originalUseAdvancedColliders = geometry.useAdvancedColliders;
            _originalUseAuxBreastColliders = geometry.useAuxBreastColliders;
        }

        public static void EnableAdvColliders()
        {
            if(!personIsFemale)
            {
                return;
            }

            geometry.useAdvancedColliders = true;
            geometry.useAuxBreastColliders = true;
        }

        public static void EnableMultiplyFriction()
        {
            if(!personIsFemale)
            {
                return;
            }

            colliderConfigs.ForEach(config => config.EnableMultiplyFriction());
        }

        public static void RestoreOriginalPhysics()
        {
            if(!personIsFemale)
            {
                return;
            }

            /* Required for restoring default collider mass. Enabled in case disabled
             * programmatically, and not yet restored back on by SettingsMonitor.
             */
            geometry.useAdvancedColliders = true;
            geometry.useAuxBreastColliders = true;
            /* Restore defaults */
            colliderConfigs.ForEach(config => config.RestoreDefaults());
            geometry.useAdvancedColliders = _originalUseAdvancedColliders;
            geometry.useAuxBreastColliders = _originalUseAuxBreastColliders;
        }

        public static void Destroy()
        {
            colliderGroupsJsc = null;
            colliderConfigs = null;
            baseForceJsf = null;
            highlightAllJsb = null;
            _scalingConfigs = null;
        }
    }
}
