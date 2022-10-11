using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TittyMagic.Handlers.Configs;
using TittyMagic.Models;
using TittyMagic.UI;
using UnityEngine;
using static TittyMagic.ParamName;
using static TittyMagic.Script;
using static TittyMagic.Side;
using static TittyMagic.SoftColliderGroup;
using Object = UnityEngine.Object;

namespace TittyMagic.Handlers
{
    public static class SoftPhysicsHandler
    {
        public static ColliderVisualizer colliderVisualizer { get; private set; }
        public static JSONStorableBool showSoftVerticesColliderPreviewsJsb { get; private set; }

        public static DAZPhysicsMesh breastPhysicsMesh { get; private set; }
        private static List<string> _breastPhysicsMeshFloatParamNames;

        // Left/Right -> Group name -> Group
        private static Dictionary<string, Dictionary<string, DAZPhysicsMeshSoftVerticesGroup>> _softVerticesGroups;
        private static Dictionary<string, Dictionary<string, int[]>> _vertexIndices;

        // Group name -> Group
        public static Dictionary<string, PhysicsParameterGroup> parameterGroups { get; private set; }
        public static JSONStorableBool breastSoftPhysicsOnJsb { get; private set; }
        public static JSONStorableBool allowSelfCollisionJsb { get; private set; }

        private static bool _initialized;

        public static void Init()
        {
            if(personIsFemale)
            {
                InitColliderVisualizer();
                showSoftVerticesColliderPreviewsJsb = tittyMagic.NewJSONStorableBool("showSoftVerticesColliders", false, shouldRegister: false);
                showSoftVerticesColliderPreviewsJsb.setCallbackFunction = value => colliderVisualizer.GroupsJSON.val = value ? "Both breasts" : "Off";

                breastPhysicsMesh = (DAZPhysicsMesh) tittyMagic.containingAtom.GetStorableByID("BreastPhysicsMesh");
                _breastPhysicsMeshFloatParamNames = breastPhysicsMesh.GetFloatParamNames();

                var groups = breastPhysicsMesh.softVerticesGroups;
                _softVerticesGroups = new Dictionary<string, Dictionary<string, DAZPhysicsMeshSoftVerticesGroup>>
                {
                    {
                        LEFT, new Dictionary<string, DAZPhysicsMeshSoftVerticesGroup>
                        {
                            { MAIN, groups.Find(group => group.name == "left") },
                            { OUTER, groups.Find(group => group.name == "leftouter") },
                            { AREOLA, groups.Find(group => group.name == "leftareola") },
                            { NIPPLE, groups.Find(group => group.name == "leftnipple") },
                        }
                    },
                    {
                        RIGHT, new Dictionary<string, DAZPhysicsMeshSoftVerticesGroup>
                        {
                            { MAIN, groups.Find(group => group.name == "right") },
                            { OUTER, groups.Find(group => group.name == "rightouter") },
                            { AREOLA, groups.Find(group => group.name == "rightareola") },
                            { NIPPLE, groups.Find(group => group.name == "rightnipple") },
                        }
                    },
                };
                _vertexIndices = new Dictionary<string, Dictionary<string, int[]>>
                {
                    {
                        LEFT, new Dictionary<string, int[]>
                        {
                            {
                                MAIN,
                                new[]
                                {
                                    7230, 7231, 7232, 7233, 7234, 7235, 7236, 7237, 7238, 7239, 7243, 7244, 7245,
                                    7246, 7247, 8925, 8926, 8927, 8928, 8929, 8930, 8931, 8932, 8933, 8934, 8935,
                                    8936, 8937, 8938, 8939, 8940, 8941, 8942, 8943, 8944, 8945, 8946, 8947, 8948,
                                }
                            },
                            { OUTER, new[] { 17, 137, 2403, 2407, 2410, 2418, 2596, 7248, 8848, 8849 } },
                            { AREOLA, new[] { 7945, 7946, 7947, 7950, 7958, 7966 } },
                        }
                    },
                    {
                        RIGHT, new Dictionary<string, int[]>
                        {
                            {
                                MAIN,
                                new[]
                                {
                                    17936, 17937, 17938, 17939, 17940, 17941, 17942, 17943, 17944, 17945, 17949, 17950, 17951,
                                    17952, 17953, 19591, 19592, 19593, 19594, 19595, 19596, 19597, 19598, 19599, 19600, 19601,
                                    19602, 19603, 19604, 19605, 19606, 19607, 19608, 19609, 19610, 19611, 19612, 19613, 19614,
                                }
                            },
                            { OUTER, new[] { 10945, 11065, 13233, 13237, 13240, 13248, 13412, 17954, 19519, 19520 } },
                            { AREOLA, new[] { 18623, 18624, 18625, 18628, 18636, 18644 } },
                        }
                    },
                };

                EnableMultiplyFriction();
            }

            breastSoftPhysicsOnJsb = tittyMagic.NewJSONStorableBool(
                SOFT_PHYSICS_ON,
                personIsFemale && breastPhysicsMesh.on,
                shouldRegister: personIsFemale
            );
            breastSoftPhysicsOnJsb.setCallbackFunction = val =>
            {
                SyncSoftPhysicsOn(val);
                var physicsWindow = tittyMagic.tabs.activeWindow as PhysicsWindow;
                if(physicsWindow != null)
                {
                    physicsWindow.UpdateSoftPhysicsToggleStyle(tittyMagic.settingsMonitor.softPhysicsEnabled);
                }
            };

            allowSelfCollisionJsb = tittyMagic.NewJSONStorableBool(ALLOW_SELF_COLLISION, personIsFemale, shouldRegister: personIsFemale);
            allowSelfCollisionJsb.setCallbackFunction = SyncAllowSelfCollision;

            if(breastPhysicsMesh == null)
            {
                breastSoftPhysicsOnJsb.valNoCallback = false;
                allowSelfCollisionJsb.valNoCallback = false;
            }

            _initialized = true;
        }

        private static void InitColliderVisualizer()
        {
            colliderVisualizer = tittyMagic.gameObject.AddComponent<ColliderVisualizer>();
            var groups = new List<Group>
            {
                new Group("Off", @"$off"), //match nothing
                new Group("Both breasts", @"PhysicsMeshJoint(left|right)(areola|nipple|outer)?\d+$"),
                // new Group("Left breast", @"PhysicsMeshJointleft(areola|nipple|outer)?\d+$"),
                new Group($"Both breasts ({MAIN})", @"PhysicsMeshJoint(left|right)\d+$"),
                // new Group($"Left breast ({MAIN})", @"PhysicsMeshJointleft\d+$"),
                new Group($"Both breasts ({OUTER})", @"PhysicsMeshJoint(left|right)outer\d+$"),
                // new Group($"Left breast ({OUTER})", @"PhysicsMeshJointleftouter\d+$"),
                new Group($"Both breasts ({AREOLA})", @"PhysicsMeshJoint(left|right)areola\d+$"),
                // new Group($"Left breast ({AREOLA})", @"PhysicsMeshJointleftareola\d+$"),
                new Group($"Both breasts ({NIPPLE})", @"PhysicsMeshJoint(left|right)nipple\d+$"),
                // new Group($"Left breast ({NIPPLE})", @"PhysicsMeshJointleftnipple\d+$"),
            };
            colliderVisualizer.Init(tittyMagic, groups);
            colliderVisualizer.PreviewOpacityJSON.val = 0.67f;
            colliderVisualizer.PreviewOpacityJSON.defaultVal = 0.67f;
            colliderVisualizer.SelectedPreviewOpacityJSON.val = 1;
            colliderVisualizer.SelectedPreviewOpacityJSON.defaultVal = 1;
            colliderVisualizer.GroupsJSON.val = "Off";
            colliderVisualizer.GroupsJSON.defaultVal = "Off";
            colliderVisualizer.HighlightMirrorJSON.val = true;

            foreach(string option in new[] { "Select...", "Other", "All" })
            {
                colliderVisualizer.GroupsJSON.choices.Remove(option);
            }
        }

        public static void HidePreviewsOnPointerDown()
        {
            if(!showSoftVerticesColliderPreviewsJsb.val)
            {
                colliderVisualizer.GroupsJSON.val = "Off";
            }
        }

        public static void ShowPreviewsOnPointerDown(string colliderGroup = null)
        {
            if(!showSoftVerticesColliderPreviewsJsb.val)
            {
                colliderVisualizer.GroupsJSON.val = $"Both breasts{(colliderGroup != null ? $" ({colliderGroup})" : "")}";
            }
        }

        public static void LoadSettings()
        {
            if(!personIsFemale)
            {
                return;
            }

            SetupPhysicsParameterGroups();

            var texts = CreateInfoTexts();
            foreach(var param in parameterGroups)
            {
                param.Value.infoText = texts[param.Key];
            }
        }

        public static Rigidbody[] GetTrackingRigidbodies(string side, string group)
        {
            var rigidbodies = new List<Rigidbody>();
            foreach(var set in _softVerticesGroups[side][group].softVerticesSets)
            {
                if(_vertexIndices[side][group].Contains(set.targetVertex))
                {
                    rigidbodies.Add(set.jointRB);
                }
            }

            return rigidbodies.ToArray();
        }

        private static IEnumerable<Rigidbody> GetNippleTrackingRigidbody(string side) =>
            _softVerticesGroups[side][NIPPLE].softVerticesSets.Select(set => set.jointRB).ToArray();

        public static Rigidbody[] GetBreastTipTrackingRigidbodies(string side) =>
            GetTrackingRigidbodies(side, AREOLA).Concat(GetNippleTrackingRigidbody(side)).ToArray();

        public static Rigidbody[] GetBreastCenterTrackingRigidbodies(string side) =>
            GetTrackingRigidbodies(side, MAIN).Concat(GetTrackingRigidbodies(side, AREOLA)).ToArray();

        #region *** Parameter setup ***

        private static PhysicsParameter NewPhysicsParameter(string paramName, string side, float startingValue, float minValue, float maxValue)
        {
            string jsfName = $"{paramName}{(side == LEFT ? "" : side)}";
            var valueJsf = new JSONStorableFloat($"{jsfName}Value", startingValue, minValue, maxValue);
            return new PhysicsParameter(
                valueJsf,
                baseValueJsf: new JSONStorableFloat($"{jsfName}BaseValue", valueJsf.val, valueJsf.min, valueJsf.max),
                offsetJsf: tittyMagic.NewJSONStorableFloat($"{jsfName}Offset", 0, -valueJsf.max, valueJsf.max, shouldRegister: side == LEFT)
            );
        }

        private static SoftGroupPhysicsParameter NewSoftGroupPhysicsParameter(
            string paramName,
            string side,
            string group,
            float min = 0,
            float max = 2
        )
        {
            string jsfName = $"{paramName}{(side == LEFT ? "" : side)}{group}";
            var valueJsf = new JSONStorableFloat($"{jsfName}Value", 1, min, max);
            return new SoftGroupPhysicsParameter(
                valueJsf,
                baseValueJsf: new JSONStorableFloat($"{jsfName}BaseValue", valueJsf.val, valueJsf.min, valueJsf.max),
                offsetJsf: tittyMagic.NewJSONStorableFloat($"{jsfName}Offset", 0, -valueJsf.max, valueJsf.max, shouldRegister: side == LEFT)
            );
        }

        private static PhysicsParameter NewSpringParameter(string side)
        {
            var parameter = NewPhysicsParameter(SOFT_VERTICES_SPRING, side, 0, 0, 500);
            parameter.config = new StaticPhysicsConfig(
                180f,
                softnessCurve: x => -0.62f * Curves.Exponential1(x, 1.90f, 1.74f, 1.17f)
            );
            parameter.valueFormat = "F0";

            Func<float, float> groupSoftnessCurve = x => Curves.Exponential1(x, 1.90f, 1.74f, 1.17f);
            var groupConfigs = new Dictionary<string, StaticPhysicsConfig>
            {
                {
                    MAIN, new StaticPhysicsConfig(
                        3.80f,
                        softnessCurve: x => (1 / 3.80f - 1) * groupSoftnessCurve(x)
                    )
                },
                {
                    OUTER, new StaticPhysicsConfig(
                        4.60f,
                        softnessCurve: x => (1 / 4.60f - 1) * groupSoftnessCurve(x)
                    )
                },
                {
                    AREOLA, new StaticPhysicsConfig(
                        4.80f,
                        softnessCurve: x => (1 / 4.80f - 1) * groupSoftnessCurve(x)
                    )
                },
                {
                    NIPPLE, new StaticPhysicsConfig(
                        4.80f,
                        softnessCurve: x => (1 / 2.40f - 1) * groupSoftnessCurve(x)
                    )
                },
            };

            foreach(string group in allGroups)
            {
                var groupParam = NewSoftGroupPhysicsParameter(SOFT_VERTICES_SPRING, side, group, min: 0, max: 5);
                groupParam.config = groupConfigs[group];
                groupParam.sync = value => SyncGroupSpring(parameter.valueJsf.val * value, _softVerticesGroups[side][group]);
                parameter.groupMultiplierParams[group] = groupParam;
            }

            return parameter;
        }

        private static PhysicsParameter NewDamperParameter(string side)
        {
            var parameter = NewPhysicsParameter(SOFT_VERTICES_DAMPER, side, 0, 0, 5.00f);
            parameter.config = new StaticPhysicsConfig(
                0.85f,
                massCurve: x => 0.40f * Curves.Exponential2(x / 1.5f, c: 0.04f, s: 0.04f),
                softnessCurve: x => -0.50f * Curves.Exponential1(x, 1.90f, 1.74f, 1.17f)
            );
            parameter.quicknessOffsetConfig = new StaticPhysicsConfig(
                -0.15f,
                massCurve: x => -0.40f * Curves.Exponential2(x / 1.5f, c: 0.04f, s: 0.04f),
                softnessCurve: x => 0.50f * Curves.Exponential1(x, 1.90f, 1.74f, 1.17f)
            );
            parameter.slownessOffsetConfig = new StaticPhysicsConfig(
                0.15f,
                massCurve: x => 0.40f * Curves.Exponential2(x / 1.5f, c: 0.04f, s: 0.04f),
                softnessCurve: x => -0.50f * Curves.Exponential1(x, 1.90f, 1.74f, 1.17f)
            );
            parameter.valueFormat = "F2";

            var groupConfigs = new Dictionary<string, StaticPhysicsConfig>
            {
                { MAIN, new StaticPhysicsConfig(1.00f) },
                {
                    OUTER, new StaticPhysicsConfig(
                        1.00f,
                        softnessCurve: x => 0.20f * Curves.DeemphasizeMiddle(x)
                    )
                },
                {
                    AREOLA, new StaticPhysicsConfig(
                        1.25f,
                        softnessCurve: x => 1.00f * Curves.DeemphasizeMiddle(x)
                    )
                },
                {
                    NIPPLE, new StaticPhysicsConfig(
                        1.40f,
                        softnessCurve: x => 1.00f * Curves.DeemphasizeMiddle(x)
                    )
                },
            };

            foreach(string group in allGroups)
            {
                var groupParam = NewSoftGroupPhysicsParameter(SOFT_VERTICES_DAMPER, side, group, min: 0, max: 5);
                groupParam.config = groupConfigs[group];
                groupParam.sync = value => SyncGroupDamper(parameter.valueJsf.val * value, _softVerticesGroups[side][group]);
                parameter.groupMultiplierParams[group] = groupParam;
            }

            return parameter;
        }

        private static PhysicsParameter NewSoftVerticesMassParameter(string side)
        {
            var parameter = NewPhysicsParameter(SOFT_VERTICES_MASS, side, 0, 0.001f, 0.300f);
            parameter.config = new StaticPhysicsConfig(
                0.040f,
                // https://www.desmos.com/calculator/inmadsqhj2
                softnessCurve: x => 1.00f * Curves.Exponential1(x, 2.30f, 1.74f, 1.17f),
                // https://www.desmos.com/calculator/gsyidpluyg
                massCurve: x => 2.25f * Curves.Exponential1(2 / 3f * x, 1.91f, 1.7f, 0.82f)
            );
            parameter.quicknessOffsetConfig = new StaticPhysicsConfig(
                -0.022f,
                softnessCurve: x => 0.50f * x
            );
            parameter.slownessOffsetConfig = new StaticPhysicsConfig(
                0.066f,
                softnessCurve: x => 0.50f * x
            );
            parameter.valueFormat = "F3";

            var groupConfigs = new Dictionary<string, StaticPhysicsConfig>
            {
                {
                    MAIN, new StaticPhysicsConfig(
                        1.00f,
                        softnessCurve: x => 0.13f * Curves.DeemphasizeMiddle(x)
                    )
                },
                {
                    OUTER, new StaticPhysicsConfig(
                        1.00f,
                        softnessCurve: x => -0.20f * Curves.DeemphasizeMiddle(x)
                    )
                },
                {
                    AREOLA, new StaticPhysicsConfig(1.00f)
                },
                {
                    NIPPLE, new StaticPhysicsConfig(
                        1.00f,
                        softnessCurve: x => -0.13f * Curves.DeemphasizeMiddle(x)
                    )
                },
            };

            foreach(string group in allGroups)
            {
                var groupParam = NewSoftGroupPhysicsParameter(SOFT_VERTICES_MASS, side, group);
                groupParam.config = groupConfigs[group];
                groupParam.sync = value => SyncGroupMass(parameter.valueJsf.val * value, _softVerticesGroups[side][group]);
                parameter.groupMultiplierParams[group] = groupParam;
            }

            return parameter;
        }

        private static PhysicsParameter NewColliderRadiusParameter(string side)
        {
            var parameter = NewPhysicsParameter(SOFT_VERTICES_COLLIDER_RADIUS, side, 0, 0, 0.060f);
            parameter.config = new StaticPhysicsConfig(
                0.016f,
                // https://www.desmos.com/calculator/rotof03irg
                massCurve: x => 1.78f * Curves.Exponential1(2 / 3f * x, 1.42f, 4.25f, 1.17f),
                softnessCurve: x => 0.18f * x
            );
            parameter.valueFormat = "F4";

            var groupConfigs = new Dictionary<string, StaticPhysicsConfig>
            {
                { MAIN, new StaticPhysicsConfig(1.00f) },
                { OUTER, new StaticPhysicsConfig(1.00f) },
                { AREOLA, new StaticPhysicsConfig(1.15f) },
                { NIPPLE, new StaticPhysicsConfig(0.33f) },
            };

            foreach(string group in allGroups)
            {
                var groupParam = NewSoftGroupPhysicsParameter(SOFT_VERTICES_COLLIDER_RADIUS, side, group);
                groupParam.config = groupConfigs[group];
                groupParam.sync = value => SyncGroupColliderRadius(parameter.valueJsf.val * value, _softVerticesGroups[side][group]);
                parameter.groupMultiplierParams[group] = groupParam;
            }

            return parameter;
        }

        private static PhysicsParameter NewColliderAdditionalNormalOffsetParameter(string side)
        {
            var parameter = NewPhysicsParameter(SOFT_VERTICES_COLLIDER_ADDITIONAL_NORMAL_OFFSET, side, 0, -0.010f, 0.010f);
            parameter.config = new StaticPhysicsConfig(0.001f);
            parameter.valueFormat = "F4";

            var groupConfigs = new Dictionary<string, StaticPhysicsConfig>
            {
                { MAIN, new StaticPhysicsConfig(1.00f) },
                { OUTER, new StaticPhysicsConfig(1.00f) },
                { AREOLA, new StaticPhysicsConfig(1.00f) },
                { NIPPLE, new StaticPhysicsConfig(1.00f) },
            };

            foreach(string group in allGroups)
            {
                var groupParam = NewSoftGroupPhysicsParameter(SOFT_VERTICES_COLLIDER_ADDITIONAL_NORMAL_OFFSET, side, group);
                groupParam.config = groupConfigs[group];
                groupParam.sync = value => SyncGroupAdditionalNormalOffset(parameter.valueJsf.val * value, _softVerticesGroups[side][group]);
                parameter.groupMultiplierParams[group] = groupParam;
            }

            return parameter;
        }

        private static PhysicsParameter NewDistanceLimitParameter(string side)
        {
            var parameter = NewPhysicsParameter(SOFT_VERTICES_DISTANCE_LIMIT, side, 0, 0, 0.100f);
            parameter.config = new StaticPhysicsConfig(
                0.019f,
                massCurve: x => 2.4f * x,
                softnessCurve: x => 0.4f * x
            );
            parameter.quicknessOffsetConfig = new StaticPhysicsConfig(
                0.003f,
                softnessCurve: _ => 4.0f
            );
            parameter.slownessOffsetConfig = new StaticPhysicsConfig(
                -0.001f,
                softnessCurve: _ => 4.0f
            );
            parameter.valueFormat = "F3";

            var groupConfigs = new Dictionary<string, StaticPhysicsConfig>
            {
                { MAIN, new StaticPhysicsConfig(1.00f) },
                { OUTER, new StaticPhysicsConfig(1.00f) },
                { AREOLA, new StaticPhysicsConfig(1.10f) },
                { NIPPLE, new StaticPhysicsConfig(1.20f) },
            };

            foreach(string group in allGroups)
            {
                var groupParam = NewSoftGroupPhysicsParameter(SOFT_VERTICES_DISTANCE_LIMIT, side, group);
                groupParam.config = groupConfigs[group];
                groupParam.sync = value => SyncGroupDistanceLimit(parameter.valueJsf.val * value, _softVerticesGroups[side][group]);
                parameter.groupMultiplierParams[group] = groupParam;
            }

            return parameter;
        }

        private static PhysicsParameter NewBackForceParameter(string side)
        {
            var parameter = NewPhysicsParameter(SOFT_VERTICES_BACK_FORCE, side, 0, 0, 50.00f);
            parameter.config = new StaticPhysicsConfig(
                0.40f,
                massCurve: x => 30.00f * Curves.InverseSmoothStep(2 / 3f * x, 1.00f, 0.50f, 0.88f)
            );
            parameter.quicknessOffsetConfig = new StaticPhysicsConfig(
                -0.40f,
                massCurve: x => 15.00f * Curves.InverseSmoothStep(2 / 3f * x, 1.00f, 0.50f, 0.88f)
            );
            parameter.slownessOffsetConfig = new StaticPhysicsConfig(
                5.00f,
                massCurve: x => 0.70f * Curves.InverseSmoothStep(2 / 3f * x, 1.00f, 0.25f, 0.70f)
            );
            parameter.valueFormat = "F2";

            var groupConfigs = new Dictionary<string, StaticPhysicsConfig>
            {
                { MAIN, new StaticPhysicsConfig(1.00f) },
                { OUTER, new StaticPhysicsConfig(1.00f) },
                {
                    AREOLA, new StaticPhysicsConfig(
                        0.25f,
                        softnessCurve: x => (1 / 0.25f - 1) * x
                    )
                },
                {
                    NIPPLE, new StaticPhysicsConfig(
                        0.08f,
                        softnessCurve: x => (1 / 0.08f - 1) * x
                    )
                },
            };

            foreach(string group in allGroups)
            {
                var groupParam = NewSoftGroupPhysicsParameter(SOFT_VERTICES_BACK_FORCE, side, group);
                groupParam.config = groupConfigs[group];
                groupParam.sync = value => SyncGroupBackForce(parameter.valueJsf.val * value, _softVerticesGroups[side][group]);
                parameter.groupMultiplierParams[group] = groupParam;
            }

            return parameter;
        }

        private static PhysicsParameter NewBackForceMaxForceParameter(string side)
        {
            var parameter = NewPhysicsParameter(SOFT_VERTICES_BACK_FORCE_MAX_FORCE, side, 0, 0, 50.00f);
            parameter.config = new StaticPhysicsConfig(50.00f);
            parameter.valueFormat = "F2";

            var groupConfigs = new Dictionary<string, StaticPhysicsConfig>
            {
                { MAIN, new StaticPhysicsConfig(1.00f) },
                { OUTER, new StaticPhysicsConfig(1.00f) },
                { AREOLA, new StaticPhysicsConfig(1.00f) },
                { NIPPLE, new StaticPhysicsConfig(1.00f) },
            };

            foreach(string group in allGroups)
            {
                var groupParam = NewSoftGroupPhysicsParameter(SOFT_VERTICES_BACK_FORCE_MAX_FORCE, side, group);
                groupParam.config = groupConfigs[group];
                groupParam.sync = value => SyncGroupBackForceMaxForce(parameter.valueJsf.val * value, _softVerticesGroups[side][group]);
                parameter.groupMultiplierParams[group] = groupParam;
            }

            return parameter;
        }

        private static PhysicsParameter NewBackForceThresholdDistanceParameter(string side)
        {
            var parameter = NewPhysicsParameter(SOFT_VERTICES_BACK_FORCE_THRESHOLD_DISTANCE, side, 0, 0, 0.030f);
            parameter.config = new StaticPhysicsConfig(0.001f);
            parameter.valueFormat = "F3";

            var groupConfigs = new Dictionary<string, StaticPhysicsConfig>
            {
                { MAIN, new StaticPhysicsConfig(1.00f) },
                { OUTER, new StaticPhysicsConfig(1.00f) },
                { AREOLA, new StaticPhysicsConfig(1.00f) },
                { NIPPLE, new StaticPhysicsConfig(1.00f) },
            };

            foreach(string group in allGroups)
            {
                var groupParam = NewSoftGroupPhysicsParameter(SOFT_VERTICES_BACK_FORCE_THRESHOLD_DISTANCE, side, group);
                groupParam.config = groupConfigs[group];
                groupParam.sync = value => SyncGroupBackForceThresholdDistance(parameter.valueJsf.val * value, _softVerticesGroups[side][group]);
                parameter.groupMultiplierParams[group] = groupParam;
            }

            return parameter;
        }

        private static void SetupPhysicsParameterGroups()
        {
            var softVerticesSpring = new PhysicsParameterGroup(
                NewSpringParameter(LEFT),
                NewSpringParameter(RIGHT),
                "Fat Spring"
            );

            var softVerticesDamper = new PhysicsParameterGroup(
                NewDamperParameter(LEFT),
                NewDamperParameter(RIGHT),
                "Fat Damper"
            )
            {
                dependsOnPhysicsRate = true,
            };

            var softVerticesMass = new PhysicsParameterGroup(
                NewSoftVerticesMassParameter(LEFT),
                NewSoftVerticesMassParameter(RIGHT),
                "Fat Mass"
            );

            var softVerticesColliderRadius = new PhysicsParameterGroup(
                NewColliderRadiusParameter(LEFT),
                NewColliderRadiusParameter(RIGHT),
                "Fat Collider Radius"
            )
            {
                usesRealMass = true,
                allowsSoftColliderVisualization = true,
            };

            var softVerticesColliderAdditionalNormalOffset = new PhysicsParameterGroup(
                NewColliderAdditionalNormalOffsetParameter(LEFT),
                NewColliderAdditionalNormalOffsetParameter(RIGHT),
                "Fat Collider Depth"
            )
            {
                allowsSoftColliderVisualization = true,
            };

            var softVerticesDistanceLimit = new PhysicsParameterGroup(
                NewDistanceLimitParameter(LEFT),
                NewDistanceLimitParameter(RIGHT),
                "Fat Distance Limit"
            )
            {
                usesRealMass = true,
            };

            var softVerticesBackForce = new PhysicsParameterGroup(
                NewBackForceParameter(LEFT),
                NewBackForceParameter(RIGHT),
                "Fat Back Force"
            );

            var softVerticesBackForceMaxForce = new PhysicsParameterGroup(
                NewBackForceMaxForceParameter(LEFT),
                NewBackForceMaxForceParameter(RIGHT),
                "Fat Bk Force Max Force"
            );

            var softVerticesBackForceThresholdDistance = new PhysicsParameterGroup(
                NewBackForceThresholdDistanceParameter(LEFT),
                NewBackForceThresholdDistanceParameter(RIGHT),
                "Fat Bk Force Threshold"
            );

            softVerticesSpring.SetOffsetCallbackFunctions();
            softVerticesDamper.SetOffsetCallbackFunctions();
            softVerticesMass.SetOffsetCallbackFunctions();
            softVerticesBackForce.SetOffsetCallbackFunctions();
            softVerticesBackForceMaxForce.SetOffsetCallbackFunctions();
            softVerticesBackForceThresholdDistance.SetOffsetCallbackFunctions();
            softVerticesColliderRadius.SetOffsetCallbackFunctions();
            softVerticesColliderAdditionalNormalOffset.SetOffsetCallbackFunctions();
            softVerticesDistanceLimit.SetOffsetCallbackFunctions();

            parameterGroups = new Dictionary<string, PhysicsParameterGroup>
            {
                { SOFT_VERTICES_SPRING, softVerticesSpring },
                { SOFT_VERTICES_DAMPER, softVerticesDamper },
                { SOFT_VERTICES_MASS, softVerticesMass },
                { SOFT_VERTICES_BACK_FORCE, softVerticesBackForce },
                { SOFT_VERTICES_BACK_FORCE_MAX_FORCE, softVerticesBackForceMaxForce },
                { SOFT_VERTICES_BACK_FORCE_THRESHOLD_DISTANCE, softVerticesBackForceThresholdDistance },
                { SOFT_VERTICES_COLLIDER_RADIUS, softVerticesColliderRadius },
                { SOFT_VERTICES_COLLIDER_ADDITIONAL_NORMAL_OFFSET, softVerticesColliderAdditionalNormalOffset },
                { SOFT_VERTICES_DISTANCE_LIMIT, softVerticesDistanceLimit },
            };
        }

        #endregion *** Parameter setup ***

        #region *** Sync functions ***

        // Reimplements DAZPhysicsMesh.cs methods SyncGroup[A|B|C|D]SpringMultiplier and SyncSoftVerticesCombinedSpring
        // Circumvents use of softVerticesCombinedSpring value as multiplier on the group specific value, using custom multiplier instead
        private static void SyncGroupSpring(float value, DAZPhysicsMeshSoftVerticesGroup group)
        {
            if(!tittyMagic.enabled)
            {
                return;
            }

            group.jointSpringNormal = value;
            group.jointSpringTangent = value;
            group.jointSpringTangent2 = value;
            if(group.tieLinkJointSpringAndDamperToNormalSpringAndDamper)
            {
                group.linkSpring = value;
            }
        }

        // Reimplements DAZPhysicsMesh.cs methods SyncGroup[A|B|C|D]DamperMultiplier and SyncSoftVerticesCombinedDamper
        // Circumvents use of softVerticesCombinedDamper value as multiplier on the group specific value, using custom multiplier instead
        private static void SyncGroupDamper(float value, DAZPhysicsMeshSoftVerticesGroup group)
        {
            if(!tittyMagic.enabled)
            {
                return;
            }

            group.jointDamperNormal = value;
            group.jointDamperTangent = value;
            group.jointDamperTangent2 = value;
            if(group.tieLinkJointSpringAndDamperToNormalSpringAndDamper)
            {
                group.linkDamper = value;
            }
        }

        private static void SyncGroupMass(float value, DAZPhysicsMeshSoftVerticesGroup group)
        {
            if(!tittyMagic.enabled)
            {
                return;
            }

            group.jointMass = value;
        }

        private static void SyncGroupColliderRadius(float value, DAZPhysicsMeshSoftVerticesGroup group)
        {
            if(!tittyMagic.enabled)
            {
                return;
            }

            /* Nipple group would normally be excluded because it has useParentColliderSettings=false.
             * Updating it regardless ensures that the visualized collider has correct radius
             */
            // if(group.useParentColliderSettings)
            {
                group.colliderRadiusNoSync = value;
                group.colliderNormalOffsetNoSync = value;
            }

            if(group.useSecondCollider)
            {
                group.secondColliderRadiusNoSync = value;
                group.secondColliderNormalOffsetNoSync = value;
            }

            if(group.colliderSyncDirty)
            {
                group.SyncColliders();
                colliderVisualizer.SyncPreviews();
            }
        }

        private static void SyncGroupBackForce(float value, DAZPhysicsMeshSoftVerticesGroup group)
        {
            if(!tittyMagic.enabled)
            {
                return;
            }

            group.jointBackForce = value;
        }

        private static void SyncGroupAdditionalNormalOffset(float value, DAZPhysicsMeshSoftVerticesGroup group)
        {
            if(!tittyMagic.enabled)
            {
                return;
            }

            group.colliderAdditionalNormalOffset = value;
            colliderVisualizer.SyncPreviews();
        }

        private static void SyncGroupDistanceLimit(float value, DAZPhysicsMeshSoftVerticesGroup group)
        {
            if(!tittyMagic.enabled)
            {
                return;
            }

            group.normalDistanceLimit = value;
        }

        private static void SyncGroupBackForceMaxForce(float value, DAZPhysicsMeshSoftVerticesGroup group)
        {
            if(!tittyMagic.enabled)
            {
                return;
            }

            group.jointBackForceMaxForce = value;
        }

        private static void SyncGroupBackForceThresholdDistance(float value, DAZPhysicsMeshSoftVerticesGroup group)
        {
            if(!tittyMagic.enabled)
            {
                return;
            }

            group.jointBackForceThresholdDistance = value;
        }

        /* Update value if external value changed */
        public static void ReverseSyncSoftPhysicsOn()
        {
            if(breastPhysicsMesh != null)
            {
                breastSoftPhysicsOnJsb.valNoCallback = breastPhysicsMesh.on;
            }
        }

        /* Update value if external value changed */
        public static void ReverseSyncAllowSelfCollision()
        {
            if(breastPhysicsMesh != null)
            {
                allowSelfCollisionJsb.valNoCallback = breastPhysicsMesh.allowSelfCollision;
            }
        }

        private static void SyncSoftPhysicsOn(bool value)
        {
            if(!tittyMagic.enabled)
            {
                Utils.LogMessage("Enable the plugin to update Soft Physics Enabled from the plugin.");
                return;
            }

            if(breastPhysicsMesh != null)
            {
                breastPhysicsMesh.on = value;
            }
        }

        private static void SyncAllowSelfCollision(bool value)
        {
            if(!tittyMagic.enabled)
            {
                Utils.LogMessage("Enable the plugin to update Breast Soft Physics Self Collide via the plugin.");
                return;
            }

            if(breastPhysicsMesh != null)
            {
                breastPhysicsMesh.allowSelfCollision = value;
            }
        }

        public static void SyncFriction(float friction)
        {
            float areolaFriction = friction + 0.33f * (1 - friction);
            float nippleFriction = friction + 0.5f * (1 - friction);

            foreach(string side in new[] { LEFT, RIGHT })
            {
                SyncFriction(friction, _softVerticesGroups[side][MAIN]);
                SyncFriction(friction, _softVerticesGroups[side][OUTER]);
                SyncFriction(areolaFriction, _softVerticesGroups[side][AREOLA]);
                SyncFriction(nippleFriction, _softVerticesGroups[side][NIPPLE]);
            }
        }

        private static void SyncFriction(float value, DAZPhysicsMeshSoftVerticesGroup group)
        {
            foreach(var set in group.softVerticesSets)
            {
                var material = set.jointCollider.material;
                material.dynamicFriction = value;
                material.staticFriction = value;
            }
        }

        #endregion *** Sync functions ***

        public static void SyncSoftPhysics()
        {
            if(!personIsFemale)
            {
                return;
            }

            SyncSoftPhysicsOn(breastSoftPhysicsOnJsb.val);
            SyncAllowSelfCollision(allowSelfCollisionJsb.val);
        }

        public static void UpdatePhysics()
        {
            if(!tittyMagic.settingsMonitor.softPhysicsEnabled)
            {
                return;
            }

            float softness = tittyMagic.softnessAmount;
            float quickness = tittyMagic.quicknessAmount;
            parameterGroups.Values
                .ToList()
                .ForEach(paramGroup =>
                {
                    float mass = paramGroup.usesRealMass ? MainPhysicsHandler.realMassAmount : MainPhysicsHandler.massAmount;
                    paramGroup.UpdateValue(mass, softness, quickness);
                });
        }

        public static void UpdateRateDependentPhysics()
        {
            float softness = tittyMagic.softnessAmount;
            float quickness = tittyMagic.quicknessAmount;
            parameterGroups.Values
                .Where(paramGroup => paramGroup.dependsOnPhysicsRate)
                .ToList()
                .ForEach(paramGroup =>
                {
                    float mass = paramGroup.usesRealMass ? MainPhysicsHandler.realMassAmount : MainPhysicsHandler.massAmount;
                    paramGroup.UpdateValue(mass, softness, quickness);
                });
        }

        private static bool _originalSoftPhysicsOn;
        private static bool _originalAllowSelfCollision;

        public static void SaveOriginalBoolParamValues()
        {
            if(breastPhysicsMesh == null)
            {
                return;
            }

            _originalSoftPhysicsOn = breastPhysicsMesh.on;
            _originalAllowSelfCollision = breastPhysicsMesh.allowSelfCollision;
        }

        public static void EnableMultiplyFriction()
        {
            if(breastPhysicsMesh == null)
            {
                return;
            }

            foreach(var dictionary in _softVerticesGroups.Values.ToList())
            {
                foreach(var group in dictionary)
                {
                    foreach(var set in group.Value.softVerticesSets)
                    {
                        var material = set.jointCollider.material;
                        material.frictionCombine = PhysicMaterialCombine.Multiply;
                    }
                }
            }
        }

        public static void RestoreOriginalPhysics()
        {
            if(!_initialized || breastPhysicsMesh == null)
            {
                return;
            }

            breastPhysicsMesh.on = _originalSoftPhysicsOn;
            breastPhysicsMesh.allowSelfCollision = _originalAllowSelfCollision;

            foreach(string name in _breastPhysicsMeshFloatParamNames)
            {
                /* Set a value that is different from the original, then restore the original
                 * in order to trigger VaM's internal sync
                 */
                var paramJsf = breastPhysicsMesh.GetFloatJSONParam(name);
                float original = paramJsf.val;
                paramJsf.valNoCallback = Math.Abs(paramJsf.val - paramJsf.min) > 0.01f
                    ? paramJsf.min
                    : paramJsf.max;
                paramJsf.val = original;
            }

            foreach(var dictionary in _softVerticesGroups.Values.ToList())
            {
                foreach(var group in dictionary)
                {
                    foreach(var set in group.Value.softVerticesSets)
                    {
                        var material = set.jointCollider.material;
                        material.dynamicFriction = 0.6f;
                        material.staticFriction = 0.6f;
                        material.frictionCombine = PhysicMaterialCombine.Average;
                    }
                }
            }
        }

        private static Dictionary<string, string> CreateInfoTexts()
        {
            string springText;
            {
                var sb = new StringBuilder();
                sb.Append("Magnitude of the spring that holds each soft joint in its target position.");
                sb.Append("\n\n");
                sb.Append("Low fat spring makes breast fat soft and slow. High fat spring makes it rigid and");
                sb.Append(" quick to return to its normal shape.");
                springText = sb.ToString();
            }

            string damperText;
            {
                var sb = new StringBuilder();
                sb.Append("Magnitude of the damper that reduces oscillation of each soft joint around");
                sb.Append(" its target position.");
                sb.Append("\n\n");
                sb.Append("Low fat damper makes breast fat jiggle more easily.");
                damperText = sb.ToString();
            }

            string massText;
            {
                var sb = new StringBuilder();
                sb.Append("Mass of each soft joint.");
                sb.Append("\n\n");
                sb.Append("Higher mass makes the breast tissue more dense. The value is an absolute value,");
                sb.Append(" so increasing breast size while keeping fat mass the same reduces density.");
                massText = sb.ToString();
            }

            string backForceText;
            {
                var sb = new StringBuilder();
                sb.Append("Force applied to the pectoral joint based on movement of each soft joint.");
                sb.Append("\n\n");
                sb.Append("Low back force (not 0) helps move the breast with collision, and adds a dampening effect.");
                sb.Append(" High force can create a feedback loop that spirals out of control.");
                backForceText = sb.ToString();
            }

            string backForceThresholdDistanceText;
            {
                var sb = new StringBuilder();
                sb.Append("Minimum distance each soft joint needs to move for back force to be applied.");
                sb.Append("\n\n");
                sb.Append("Ensures that small movements of soft joints don't cause the whole breast");
                sb.Append(" to move. Along with Fat Bk Force Threshold, this can be used to prevent");
                sb.Append(" an out of control feedback loop.");
                backForceThresholdDistanceText = sb.ToString();
            }

            string backForceMaxForceText;
            {
                var sb = new StringBuilder();
                sb.Append("Upper limit on the magnitude of back force.");
                sb.Append("\n\n");
                sb.Append("Along with Fat Bk Force Threshold, this can be used to prevent an out of control feedback loop.");
                backForceMaxForceText = sb.ToString();
            }

            string colliderRadiusText;
            {
                var sb = new StringBuilder();
                sb.Append("Radius of each soft collider.");
                sb.Append("\n\n");
                sb.Append("Since the number of soft colliders is fixed, the radius scales with breast size.");
                colliderRadiusText = sb.ToString();
            }

            string colliderAdditionalNormalOffsetText;
            {
                var sb = new StringBuilder();
                sb.Append("Offset of soft collider positions relative to skin surface.");
                sb.Append("\n\n");
                sb.Append("Negative values pull colliders out from the breast, positive values push them into the breast.");
                colliderAdditionalNormalOffsetText = sb.ToString();
            }

            string distanceLimitText;
            {
                var sb = new StringBuilder();
                sb.Append("The maximum distance each soft joint can move away from its target position.");
                distanceLimitText = sb.ToString();
            }

            return new Dictionary<string, string>
            {
                { SOFT_VERTICES_SPRING, springText },
                { SOFT_VERTICES_DAMPER, damperText },
                { SOFT_VERTICES_MASS, massText },
                { SOFT_VERTICES_BACK_FORCE, backForceText },
                { SOFT_VERTICES_BACK_FORCE_THRESHOLD_DISTANCE, backForceThresholdDistanceText },
                { SOFT_VERTICES_BACK_FORCE_MAX_FORCE, backForceMaxForceText },
                { SOFT_VERTICES_COLLIDER_RADIUS, colliderRadiusText },
                { SOFT_VERTICES_COLLIDER_ADDITIONAL_NORMAL_OFFSET, colliderAdditionalNormalOffsetText },
                { SOFT_VERTICES_DISTANCE_LIMIT, distanceLimitText },
            };
        }

        public static void Destroy()
        {
            Object.Destroy(colliderVisualizer);
            colliderVisualizer = null;
            showSoftVerticesColliderPreviewsJsb = null;
            breastPhysicsMesh = null;
            _breastPhysicsMeshFloatParamNames = null;
            _softVerticesGroups = null;
            _vertexIndices = null;
            parameterGroups = null;
            breastSoftPhysicsOnJsb = null;
            allowSelfCollisionJsb = null;
        }
    }
}
