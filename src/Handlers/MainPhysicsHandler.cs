using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TittyMagic.Handlers.Configs;
using TittyMagic.Models;
using UnityEngine;
using static TittyMagic.ParamName;
using static TittyMagic.Script;
using static TittyMagic.Side;

namespace TittyMagic.Handlers
{
    public static class MainPhysicsHandler
    {
        public static Rigidbody chestRb { get; set; }
        public static AdjustJoints breastControl { get; set; }

        private static Dictionary<string, ConfigurableJoint> _joints;
        private static Dictionary<string, Rigidbody> _pectoralRbs;
        private static Dictionary<string, DAZBone> _dazBones;

        private static List<string> _breastControlFloatParamNames;

        // AdjustJoints.joint1.slerpDrive.maximumForce value logged on plugin Init
        private const float DEFAULT_SLERP_MAX_FORCE = 500;

        public static Dictionary<string, PhysicsParameterGroup> parameterGroups { get; private set; }

        public static float realMassAmount { get; private set; }
        public static float massAmount { get; private set; }

        // hack. 1.5f because 3f is the max mass and massValue is actual mass / 2
        public static float InvertMass(float x) => 1.5f - x;
        public static float normalizedMass => Mathf.InverseLerp(0, 1.45f, massAmount);
        public static float normalizedRealMass => Mathf.InverseLerp(0, 1.50f, realMassAmount);
        public static float normalizedInvertedMass => Mathf.InverseLerp(0, 1.45f, InvertMass(massAmount));

        public static MassParameterGroup massParameterGroup { get; private set; }

        private static bool _isInitialized;

        public static void Init()
        {
            _joints = new Dictionary<string, ConfigurableJoint>
            {
                { LEFT, breastControl.joint2 },
                { RIGHT, breastControl.joint1 },
            };
            _pectoralRbs = new Dictionary<string, Rigidbody>
            {
                { LEFT, _joints[LEFT].GetComponent<Rigidbody>() },
                { RIGHT, _joints[RIGHT].GetComponent<Rigidbody>() },
            };
            _dazBones = new Dictionary<string, DAZBone>
            {
                { LEFT, _joints[LEFT].GetComponent<DAZBone>() },
                { RIGHT, _joints[RIGHT].GetComponent<DAZBone>() },
            };

            _breastControlFloatParamNames = new List<string>
            {
                MASS,
                CENTER_OF_GRAVITY_PERCENT,
                SPRING,
                DAMPER,
                POSITION_SPRING_Z,
                POSITION_DAMPER_Z,
                TARGET_ROTATION_X,
                TARGET_ROTATION_Y,
                TARGET_ROTATION_Z,
            };

            _isInitialized = true;
        }

        public static void UpdateMassValueAndAmounts()
        {
            float volume = (CalculateVolume(VertexIndexGroup.leftBreast) + CalculateVolume(VertexIndexGroup.rightBreast)) / 2;
            massParameterGroup.UpdateValue(volume / 1000);

            /* Division by 2 is a hacky way to make the value compatible with legacy configurations for morphs and physics settings */
            realMassAmount = massParameterGroup.left.baseValueJsf.val / 2;
            massAmount = massParameterGroup.left.valueJsf.val / 2;
        }

        private static float CalculateVolume(IEnumerable<int> vertexIndices)
        {
            var positions = vertexIndices.Select(i => Calc.RelativePosition(chestRb, skin.rawSkinnedVerts[i])).ToList();
            var bounds = new Bounds();

            /* Calculate bounds size */
            {
                var min = Vector3.one * float.MaxValue;
                var max = Vector3.one * float.MinValue;
                foreach(var vertex in positions)
                {
                    min = Vector3.Min(min, vertex);
                    max = Vector3.Max(max, vertex);
                }

                bounds.min = min;
                bounds.max = max;
            }

            /* Calculate volume */
            {
                float toCm3 = Mathf.Pow(10, 6);
                float scale = tittyMagic.scaleJsf.val;

                /* This somewhat accurately scales breast volume to the apparent breast size when atom scale is adjusted. */
                float atomScaleAdjustment = 1 - Mathf.Abs(Mathf.Log10(Mathf.Pow(scale, 3)));
                float atomScaleFactor = scale >= 1
                    ? scale * atomScaleAdjustment
                    : scale / atomScaleAdjustment;

                float z = bounds.size.z * atomScaleFactor;
                float volume = toCm3 * (4 * Mathf.PI * bounds.size.x / 2 * bounds.size.y / 2 * z / 2) / 3;

                /* Times 0.75f compensates for change in estimated volume compared to pre v3.2 bounds calculation */
                return volume * 0.75f;
            }
        }

        public static void LoadSettings()
        {
            SetupPhysicsParameterGroups();

            var texts = CreateInfoTexts();
            massParameterGroup.infoText = texts[MASS];
            foreach(var param in parameterGroups)
            {
                param.Value.infoText = texts[param.Key];
            }
        }

        #region *** Parameter setup ***

        private static MassParameter NewMassParameter(string side)
        {
            string jsfName = $"{MASS}{(side == LEFT ? "" : side)}";
            var valueJsf = new JSONStorableFloat($"{jsfName}Value", 0.100f, 0.100f, 3.000f);
            var parameter = new MassParameter(
                valueJsf,
                baseValueJsf: new JSONStorableFloat($"{jsfName}BaseValue", valueJsf.val, valueJsf.min, valueJsf.max),
                offsetJsf: tittyMagic.NewJSONStorableFloat($"{jsfName}Offset", 0, -valueJsf.max, valueJsf.max, shouldRegister: side == LEFT)
            );
            parameter.valueFormat = "F3";
            parameter.sync = value => SyncMass(_pectoralRbs[side], value);
            return parameter;
        }

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

        private static PhysicsParameter NewCenterOfGravityParameter(string side)
        {
            var parameter = NewPhysicsParameter(CENTER_OF_GRAVITY_PERCENT, side, 0, 0, 1.00f);
            parameter.config = new StaticPhysicsConfig(
                0.60f,
                massCurve: x => 0.25f * x,
                softnessCurve: x => 0.08f * x
            );
            parameter.valueFormat = "F2";
            parameter.sync = value => SyncCenterOfGravity(_pectoralRbs[side], value);
            return parameter;
        }

        private static PhysicsParameter NewSpringParameter(string side)
        {
            var parameter = NewPhysicsParameter(SPRING, side, 10, 10, 100);
            parameter.config = new StaticPhysicsConfig(
                60f,
                massCurve: x => 0.14f * x,
                // https://www.desmos.com/calculator/nxyosar9o6
                softnessCurve: x => -0.40f * Curves.InverseSmoothStep(x, 1.00f, 0.24f, 0.61f)
            );
            parameter.quicknessOffsetConfig = new StaticPhysicsConfig(24f);
            parameter.slownessOffsetConfig = new StaticPhysicsConfig(-12f);
            parameter.valueFormat = "F0";
            parameter.sync = value => SyncJointSpring(_joints[side], _pectoralRbs[side], value);
            return parameter;
        }

        private static PhysicsParameter NewDamperParameter(string side)
        {
            var parameter = NewPhysicsParameter(DAMPER, side, 0, 0, 10.00f);
            parameter.config = new StaticPhysicsConfig(
                1.10f,
                // https://www.desmos.com/calculator/y3akvzgr1s
                massCurve: x => 1.35f * Curves.InverseSmoothStep(2 / 3f * x, 1.00f, 0.30f, 0.60f),
                // https://www.desmos.com/calculator/nxyosar9o6
                softnessCurve: x => -0.80f * Curves.InverseSmoothStep(x, 1.00f, 0.24f, 0.61f)
            );
            parameter.valueFormat = "F2";
            parameter.sync = value => SyncJointDamper(_joints[side], _pectoralRbs[side], value);
            return parameter;
        }

        private static PhysicsParameter NewPositionSpringZParameter(string side)
        {
            var parameter = NewPhysicsParameter(POSITION_SPRING_Z, side, 0, 0, 1000);
            parameter.config = new StaticPhysicsConfig(
                690f,
                massCurve: x => -0.25f * InvertMass(x),
                softnessCurve: x => -0.26f * Curves.SpringZSoftnessCurve(x)
            );
            parameter.valueFormat = "F0";
            parameter.sync = value => SyncJointPositionZDriveSpring(_joints[side], _pectoralRbs[side], value);
            return parameter;
        }

        private static PhysicsParameter NewPositionDamperZParameter(string side)
        {
            var parameter = NewPhysicsParameter(POSITION_DAMPER_Z, side, 0, 0, 100);
            parameter.config = new StaticPhysicsConfig(8f);
            parameter.valueFormat = "F0";
            parameter.sync = value => SyncJointPositionZDriveDamper(_joints[side], _pectoralRbs[side], value);
            return parameter;
        }

        private static PhysicsParameter NewTargetRotationYParameter(string side)
        {
            var parameter = NewPhysicsParameter(TARGET_ROTATION_Y, side, 0, -20.00f, 20.00f);
            parameter.config = new StaticPhysicsConfig(0.00f);
            parameter.valueFormat = "F2";
            parameter.sync = value => SyncTargetRotationY(side, value);
            return parameter;
        }

        private static PhysicsParameter NewTargetRotationXParameter(string side)
        {
            var parameter = NewPhysicsParameter(TARGET_ROTATION_X, side, 0, -20.00f, 20.00f);
            parameter.config = new StaticPhysicsConfig(0.00f);
            parameter.valueFormat = "F2";
            parameter.sync = value => SyncTargetRotationX(side, value);
            return parameter;
        }

        private static PhysicsParameter NewTargetRotationZParameter(string side)
        {
            var parameter = NewPhysicsParameter(TARGET_ROTATION_Z, side, 0, -30.00f, 30.00f);
            parameter.config = new StaticPhysicsConfig(0.00f);
            parameter.valueFormat = "F2";
            parameter.sync = value => SyncTargetRotationZ(side, value);
            return parameter;
        }

        private static void SetupPhysicsParameterGroups()
        {
            massParameterGroup = new MassParameterGroup(
                NewMassParameter(LEFT),
                NewMassParameter(RIGHT),
                "Breast Mass"
            )
            {
                requiresRecalibration = true,
            };

            var centerOfGravityPercent = new PhysicsParameterGroup(
                NewCenterOfGravityParameter(LEFT),
                NewCenterOfGravityParameter(RIGHT),
                "Center Of Gravity"
            )
            {
                requiresRecalibration = true,
            };

            var spring = new PhysicsParameterGroup(
                NewSpringParameter(LEFT),
                NewSpringParameter(RIGHT),
                "Spring"
            )
            {
                requiresRecalibration = true,
            };

            var damper = new PhysicsParameterGroup(
                NewDamperParameter(LEFT),
                NewDamperParameter(RIGHT),
                "Damper"
            )
            {
                dependsOnPhysicsRate = true,
            };

            var positionSpringZ = new PhysicsParameterGroup(
                NewPositionSpringZParameter(LEFT),
                NewPositionSpringZParameter(RIGHT),
                "In/Out Spring"
            )
            {
                requiresRecalibration = true,
            };

            var positionDamperZ = new PhysicsParameterGroup(
                NewPositionDamperZParameter(LEFT),
                NewPositionDamperZParameter(RIGHT),
                "In/Out Damper"
            )
            {
                dependsOnPhysicsRate = true,
            };

            var targetRotationY = new PhysicsParameterGroup(
                NewTargetRotationYParameter(LEFT),
                NewTargetRotationYParameter(RIGHT),
                "Right/Left Angle Target"
            )
            {
                requiresRecalibration = true,
                rightIsInverted = true,
            };

            var targetRotationX = new PhysicsParameterGroup(
                NewTargetRotationXParameter(LEFT),
                NewTargetRotationXParameter(RIGHT),
                "Up/Down Angle Target"
            )
            {
                requiresRecalibration = true,
            };

            var targetRotationZ = new PhysicsParameterGroup(
                NewTargetRotationZParameter(LEFT),
                NewTargetRotationZParameter(RIGHT),
                "Twist Angle Target"
            )
            {
                requiresRecalibration = true,
                rightIsInverted = true,
            };

            massParameterGroup.SetOffsetCallbackFunctions();
            centerOfGravityPercent.SetOffsetCallbackFunctions();
            spring.SetOffsetCallbackFunctions();
            damper.SetOffsetCallbackFunctions();
            positionSpringZ.SetOffsetCallbackFunctions();
            positionDamperZ.SetOffsetCallbackFunctions();
            targetRotationY.SetOffsetCallbackFunctions();
            targetRotationX.SetOffsetCallbackFunctions();
            targetRotationZ.SetOffsetCallbackFunctions();

            parameterGroups = new Dictionary<string, PhysicsParameterGroup>
            {
                { CENTER_OF_GRAVITY_PERCENT, centerOfGravityPercent },
                { SPRING, spring },
                { DAMPER, damper },
                { POSITION_SPRING_Z, positionSpringZ },
                { POSITION_DAMPER_Z, positionDamperZ },
                { TARGET_ROTATION_X, targetRotationX },
                { TARGET_ROTATION_Y, targetRotationY },
                { TARGET_ROTATION_Z, targetRotationZ },
            };
        }

        #endregion *** Parameter setup ***

        #region *** Sync functions ***

        // Reimplements AdjustJoints.cs method SyncMass
        private static void SyncMass(Rigidbody rb, float value)
        {
            if(!tittyMagic.enabled)
            {
                return;
            }

            if(Math.Abs(rb.mass - value) <= 0.001f)
            {
                return;
            }

            rb.mass = value;
            rb.WakeUp();
        }

        // Reimplements AdjustJoints.cs method SyncCenterOfGravity
        private static void SyncCenterOfGravity(Rigidbody rb, float value)
        {
            if(!tittyMagic.enabled)
            {
                return;
            }

            var newCenterOfMass = Vector3.Lerp(breastControl.lowCenterOfGravity, breastControl.highCenterOfGravity, value);
            if(Calc.VectorIsEqualWithin(1 / 100f, rb.centerOfMass, newCenterOfMass))
            {
                return;
            }

            rb.centerOfMass = newCenterOfMass;
            rb.WakeUp();
        }

        private static void SyncJointSpring(ConfigurableJoint joint, Rigidbody rb, float spring)
        {
            if(!tittyMagic.enabled)
            {
                return;
            }

            // see AdjustJoints.cs method ScaleChanged
            float scalePow = Mathf.Pow(1.7f, breastControl.scale - 1f);

            float scaledSpring = spring * scalePow;
            if(Mathf.Abs(joint.slerpDrive.positionSpring - scaledSpring) <= 1)
            {
                return;
            }

            var slerpDrive = joint.slerpDrive;
            slerpDrive.positionSpring = scaledSpring;
            slerpDrive.maximumForce = DEFAULT_SLERP_MAX_FORCE * scalePow;
            joint.slerpDrive = slerpDrive;

            var angularXDrive = joint.angularXDrive;
            angularXDrive.positionSpring = scaledSpring;
            joint.angularXDrive = angularXDrive;

            var angularYZDrive = joint.angularYZDrive;
            angularYZDrive.positionSpring = scaledSpring;
            joint.angularYZDrive = angularYZDrive;

            var angularXLimitSpring = joint.angularXLimitSpring;
            angularXLimitSpring.spring = scaledSpring * breastControl.limitSpringMultiplier;
            joint.angularXLimitSpring = angularXLimitSpring;

            var angularYZLimitSpring = joint.angularYZLimitSpring;
            angularYZLimitSpring.spring = scaledSpring * breastControl.limitSpringMultiplier;
            joint.angularYZLimitSpring = angularYZLimitSpring;

            rb.WakeUp();
        }

        private static void SyncJointDamper(ConfigurableJoint joint, Rigidbody rb, float damper)
        {
            if(!tittyMagic.enabled)
            {
                return;
            }

            // see AdjustJoints.cs method ScaleChanged
            float scalePow = Mathf.Pow(1.7f, breastControl.scale - 1f);

            float scaledDamper = damper * scalePow;
            if(Mathf.Abs(joint.slerpDrive.positionDamper - scaledDamper) <= 0.01f)
            {
                return;
            }

            var slerpDrive = joint.slerpDrive;
            slerpDrive.positionDamper = scaledDamper;
            slerpDrive.maximumForce = DEFAULT_SLERP_MAX_FORCE * scalePow;
            joint.slerpDrive = slerpDrive;

            var angularXDrive = joint.angularXDrive;
            angularXDrive.positionDamper = scaledDamper;
            joint.angularXDrive = angularXDrive;

            var angularYZDrive = joint.angularYZDrive;
            angularYZDrive.positionDamper = scaledDamper;
            joint.angularYZDrive = angularYZDrive;

            var angularXLimitSpring = joint.angularXLimitSpring;
            angularXLimitSpring.damper = scaledDamper * breastControl.limitDamperMultiplier;
            joint.angularXLimitSpring = angularXLimitSpring;

            var angularYZLimitSpring = joint.angularYZLimitSpring;
            angularYZLimitSpring.damper = scaledDamper * breastControl.limitDamperMultiplier;
            joint.angularYZLimitSpring = angularYZLimitSpring;

            rb.WakeUp();
        }

        // Reimplements AdjustJoints.cs method SyncJointPositionZDrive
        private static void SyncJointPositionZDriveSpring(ConfigurableJoint joint, Rigidbody rb, float spring)
        {
            if(!tittyMagic.enabled)
            {
                return;
            }

            var zDrive = joint.zDrive;
            if(Mathf.Abs(zDrive.positionSpring - spring) <= 1)
            {
                return;
            }

            zDrive.positionSpring = spring;
            joint.zDrive = zDrive;
            rb.WakeUp();
        }

        // Reimplements AdjustJoints.cs method SyncJointPositionZDrive
        private static void SyncJointPositionZDriveDamper(ConfigurableJoint joint, Rigidbody rb, float damper)
        {
            if(!tittyMagic.enabled)
            {
                return;
            }

            var zDrive = joint.zDrive;
            if(Mathf.Abs(zDrive.positionDamper - damper) <= 1)
            {
                return;
            }

            zDrive.positionDamper = damper;
            joint.zDrive = zDrive;
            rb.WakeUp();
        }

        // Reimplements AdjustJoints.cs methods SyncTargetRotation and SetTargetRotation
        private static void SyncTargetRotationX(string side, float targetRotationX)
        {
            if(!tittyMagic.enabled)
            {
                return;
            }

            if(side == LEFT)
            {
                breastControl.smoothedJoint2TargetRotation.x = targetRotationX;
                var rotation = breastControl.smoothedJoint2TargetRotation;
                rotation.x = -rotation.x;
                _dazBones[side].baseJointRotation = rotation;
            }
            else if(side == RIGHT)
            {
                breastControl.smoothedJoint1TargetRotation.x = targetRotationX;
                var rotation = breastControl.smoothedJoint1TargetRotation;
                rotation.x = -rotation.x;
                _dazBones[side].baseJointRotation = rotation;
            }
        }

        // Reimplements AdjustJoints.cs methods SyncTargetRotation and SetTargetRotation
        private static void SyncTargetRotationY(string side, float targetRotationY)
        {
            if(!tittyMagic.enabled)
            {
                return;
            }

            if(side == LEFT)
            {
                breastControl.smoothedJoint2TargetRotation.y = -targetRotationY;
                var rotation = breastControl.smoothedJoint2TargetRotation;
                _dazBones[side].baseJointRotation = rotation;
            }
            else if(side == RIGHT)
            {
                breastControl.smoothedJoint1TargetRotation.y = -targetRotationY;
                var rotation = breastControl.smoothedJoint1TargetRotation;
                _dazBones[side].baseJointRotation = rotation;
            }
        }

        private static void SyncTargetRotationZ(string side, float targetRotationZ)
        {
            if(!tittyMagic.enabled)
            {
                return;
            }

            if(side == LEFT)
            {
                breastControl.smoothedJoint2TargetRotation.z = targetRotationZ;
                var rotation = breastControl.smoothedJoint2TargetRotation;
                _dazBones[side].baseJointRotation = rotation;
            }
            else if(side == RIGHT)
            {
                breastControl.smoothedJoint1TargetRotation.z = targetRotationZ;
                var rotation = breastControl.smoothedJoint1TargetRotation;
                _dazBones[side].baseJointRotation = rotation;
            }
        }

        #endregion *** Sync functions ***

        public static void UpdatePhysics()
        {
            float softness = tittyMagic.softnessAmount;
            float quickness = tittyMagic.quicknessAmount;
            parameterGroups.Values
                .ToList()
                .ForEach(paramGroup => UpdateParam(paramGroup, softness, quickness));
        }

        public static void UpdateRateDependentPhysics()
        {
            float softness = tittyMagic.softnessAmount;
            float quickness = tittyMagic.quicknessAmount;
            parameterGroups.Values
                .Where(paramGroup => paramGroup.dependsOnPhysicsRate)
                .ToList()
                .ForEach(paramGroup => UpdateParam(paramGroup, softness, quickness));
        }

        private static void UpdateParam(PhysicsParameterGroup paramGroup, float softness, float quickness)
        {
            float massValue = paramGroup.usesRealMass ? realMassAmount : massAmount;
            paramGroup.UpdateValue(massValue, softness, quickness);
        }

        public static void RestoreOriginalPhysics()
        {
            if(!_isInitialized)
            {
                return;
            }

            foreach(string name in _breastControlFloatParamNames)
            {
                /* Set a value that is different from the original, then restore the original
                 * in order to trigger VaM's internal sync
                 */
                var paramJsf = breastControl.GetFloatJSONParam(name);
                float original = paramJsf.val;
                paramJsf.valNoCallback = Math.Abs(paramJsf.val - paramJsf.min) > 0.01f
                    ? paramJsf.min
                    : paramJsf.max;
                paramJsf.val = original;
            }
        }

        private static Dictionary<string, string> CreateInfoTexts()
        {
            string massText;
            {
                var sb = new StringBuilder();
                sb.Append("Mass of the pectoral joint.");
                sb.Append("\n\n");
                sb.Append("Since mass represents breast size, other physics parameters are adjusted based on its value.");
                if(personIsFemale)
                {
                    sb.Append("\n\n");
                    sb.Append("Fat Collider Radius and Fat Distance Limit are adjusted using the mass estimated from");
                    sb.Append(" volume, the rest are adjusted using the actual mass value that includes the offset.");
                }

                massText = sb.ToString();
            }

            string centerOfGravityText;
            {
                var sb = new StringBuilder();
                sb.Append("Position of the pectoral joint's center of mass.");
                sb.Append("\n\n");
                sb.Append("At 0, the center of mass is inside the chest at the pectoral joint. At 1, it is at the nipple.");
                sb.Append(" Between about 0.5 and 0.8, the center of mass is within the bulk of the breast volume.");
                centerOfGravityText = sb.ToString();
            }

            string springText;
            {
                var sb = new StringBuilder();
                sb.Append("Magnitude of the spring that pushes the pectoral joint towards its angle target.");
                sb.Append("\n\n");
                sb.Append("The angle target is defined by the Up/Down and Left/Right Angle Target parameters.");
                springText = sb.ToString();
            }

            string damperText;
            {
                var sb = new StringBuilder();
                sb.Append("Magnitude of the damper that reduces oscillation around the joint angle target.");
                sb.Append("\n\n");
                sb.Append("The higher the damper, the quicker breasts will stop swinging.");
                damperText = sb.ToString();
            }

            string positionSpringZText;
            {
                var sb = new StringBuilder();
                sb.Append("Magnitude of the spring that pushes the pectoral joint towards its position target along the Z axis.");
                sb.Append("\n\n");
                sb.Append("Directional force morphing along the forward-back axis depends on In/Out Spring being suitably low");
                sb.Append(" for the given breast mass.");
                positionSpringZText = sb.ToString();
            }

            string positionDamperZText;
            {
                var sb = new StringBuilder();
                sb.Append("Magnitude of the damper that reduces oscillation around the joint position target along the Z axis.");
                positionDamperZText = sb.ToString();
            }

            string targetRotationXText;
            {
                var sb = new StringBuilder();
                sb.Append("Vertical target angle of the pectoral joint.");
                sb.Append(" Negative values pull breasts down, positive values push them up.");
                sb.Append("\n\n");
                sb.Append("The offset shifts the center around which the final angle is calculated");
                sb.Append(" based on chest angle (see Gravity Multipliers)");
                targetRotationXText = sb.ToString();
            }

            string targetRotationYText;
            {
                var sb = new StringBuilder();
                sb.Append("Horizontal target angle of the pectoral joint.");
                sb.Append("\n\n");
                sb.Append("A negative offset pulls breasts apart, while a positive offset pushes them together.");
                targetRotationYText = sb.ToString();
            }

            string targetRotationZtext;
            {
                var sb = new StringBuilder();
                sb.Append("Forward axis target angle of the pectoral joint.");
                targetRotationZtext = sb.ToString();
            }

            return new Dictionary<string, string>
            {
                { MASS, massText },
                { CENTER_OF_GRAVITY_PERCENT, centerOfGravityText },
                { SPRING, springText },
                { DAMPER, damperText },
                { POSITION_SPRING_Z, positionSpringZText },
                { POSITION_DAMPER_Z, positionDamperZText },
                { TARGET_ROTATION_X, targetRotationXText },
                { TARGET_ROTATION_Y, targetRotationYText },
                { TARGET_ROTATION_Z, targetRotationZtext },
            };
        }

        public static void Destroy()
        {
            chestRb = null;
            breastControl = null;
            _joints = null;
            _pectoralRbs = null;
            _dazBones = null;
            _breastControlFloatParamNames = null;
            parameterGroups = null;
            massParameterGroup = null;
        }
    }
}
