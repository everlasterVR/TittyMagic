// ReSharper disable RedundantCast
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimpleJSON;
using TittyMagic.Configs;
using UnityEngine;
using static TittyMagic.ParamName;
using static TittyMagic.Intl;

namespace TittyMagic
{
    internal class MainPhysicsHandler
    {
        private readonly Script _script;
        private readonly AdjustJoints _breastControl;
        private readonly ConfigurableJoint _jointLeft;
        private readonly ConfigurableJoint _jointRight;
        private readonly Rigidbody _pectoralRbLeft;
        private readonly Rigidbody _pectoralRbRight;

        private readonly List<string> _adjustJointsFloatParamNames;
        private Dictionary<string, float> _originalBreastControlFloats;
        private bool _originalPectoralRbLeftDetectCollisions;
        private bool _originalPectoralRbRightDetectCollisions;

        // AdjustJoints.joint1.slerpDrive.maximumForce value logged on plugin Init
        private const float DEFAULT_SLERP_MAX_FORCE = 500;

        public Dictionary<string, PhysicsParameterGroup> parameterGroups { get; private set; }

        public float realMassAmount { get; private set; }
        public float massAmount { get; private set; }
        public float normalizedInvertedMass => Mathf.InverseLerp(0, 1.45f, InvertMass(massAmount));

        // hack. 1.5f because 3f is the max mass and massValue is actual mass / 2
        public static float InvertMass(float x) => 1.5f - x;

        public JSONStorableFloat massJsf { get; }

        public MainPhysicsHandler(
            Script script,
            AdjustJoints breastControl,
            Rigidbody pectoralRbLeft,
            Rigidbody pectoralRbRight
        )
        {
            _script = script;
            _breastControl = breastControl;
            _jointLeft = _breastControl.joint2;
            _jointRight = _breastControl.joint1;
            _pectoralRbLeft = pectoralRbLeft;
            _pectoralRbRight = pectoralRbRight;

            _adjustJointsFloatParamNames = new List<string>
            {
                MASS,
                CENTER_OF_GRAVITY_PERCENT,
                SPRING,
                DAMPER,
                POSITION_SPRING_Z,
                POSITION_DAMPER_Z,
                TARGET_ROTATION_X,
                TARGET_ROTATION_Y,
            };

            SaveOriginalPhysicsAndSetPluginDefaults();

            massJsf = _script.NewJSONStorableFloat("breastMass", 0.1f, 0.1f, 3f);
        }

        public void UpdateMassValueAndAmounts(bool useNewMass, float volume)
        {
            float newMass = Mathf.Clamp(
                Mathf.Pow(0.78f * volume, 1.5f),
                massJsf.min,
                massJsf.max
            );
            realMassAmount = newMass / 2;
            if(useNewMass)
            {
                massAmount = realMassAmount;
                massJsf.valNoCallback = newMass;
            }
            else
            {
                massAmount = massJsf.val / 2;
            }

            SyncMass(_pectoralRbLeft, massJsf.val);
            SyncMass(_pectoralRbRight, massJsf.val);
        }

        public void LoadSettings()
        {
            SetupPhysicsParameterGroups();

            var texts = CreateInfoTexts();
            foreach(var param in parameterGroups)
            {
                param.Value.infoText = texts[param.Key];
            }
        }

        private PhysicsParameter NewCenterOfGravityParameter(bool left, bool softPhysicsEnabled) =>
            new PhysicsParameter(new JSONStorableFloat(VALUE, 0, 0, 1.00f))
            {
                config = softPhysicsEnabled
                    ? new StaticPhysicsConfig(
                        0.50f,
                        massCurve: x => 0.30f * x,
                        softnessCurve: x => 0.10f * x
                    )
                    : new StaticPhysicsConfig(
                        0.50f,
                        massCurve: x => 0.45f * x,
                        softnessCurve: x => 0.15f * x
                    ),
                valueFormat = "F2",
                sync = left
                    ? (Action<float>) (value => SyncCenterOfGravity(_pectoralRbLeft, value))
                    : (Action<float>) (value => SyncCenterOfGravity(_pectoralRbRight, value)),
            };

        private PhysicsParameter NewSpringParameter(bool left) =>
            new PhysicsParameter(new JSONStorableFloat(VALUE, 10, 10, 100))
            {
                config = new StaticPhysicsConfig(
                    70f,
                    massCurve: x => 0.14f * x,
                    // https://www.desmos.com/calculator/nxyosar9o6
                    softnessCurve: x => -0.45f * Curves.InverseSmoothStep(x, 1.00f, 0.24f, 0.61f)
                ),
                quicknessOffsetConfig = new StaticPhysicsConfig(
                    20f,
                    softnessCurve: x => -0.10f * x
                ),
                slownessOffsetConfig = new StaticPhysicsConfig(
                    -13f,
                    softnessCurve: x => -0.10f * x
                ),
                valueFormat = "F0",
                sync = left
                    ? (Action<float>) (value => SyncJointSpring(_jointLeft, _pectoralRbLeft, value))
                    : (Action<float>) (value => SyncJointSpring(_jointRight, _pectoralRbRight, value)),
            };

        private PhysicsParameter NewDamperParameter(bool left, bool softPhysicsEnabled) =>
            new PhysicsParameter(new JSONStorableFloat(VALUE, 0, 0, 10.00f))
            {
                config = softPhysicsEnabled
                    ? new StaticPhysicsConfig(
                        1.20f,
                        // https://www.desmos.com/calculator/y3akvzgr1s
                        massCurve: x => 1.35f * Curves.InverseSmoothStep(2 / 3f * x, 1.00f, 0.30f, 0.60f),
                        // https://www.desmos.com/calculator/nxyosar9o6
                        softnessCurve: x => -0.80f * Curves.InverseSmoothStep(x, 1.00f, 0.24f, 0.61f)
                    )
                    : new StaticPhysicsConfig(
                        0.79f,
                        massCurve: x => 0.19f * x,
                        softnessCurve: x => -0.38f * x
                    ),
                quicknessOffsetConfig = softPhysicsEnabled
                    ? new StaticPhysicsConfig(
                        -0.30f,
                        softnessCurve: x => -0.33f * x
                    )
                    : new StaticPhysicsConfig(
                        -0.23f,
                        softnessCurve: x => -0.33f * x
                    ),
                slownessOffsetConfig = softPhysicsEnabled
                    ? new StaticPhysicsConfig(
                        0.20f,
                        softnessCurve: x => -0.30f * x
                    )
                    : new StaticPhysicsConfig(
                        0.15f,
                        softnessCurve: x => -0.30f * x
                    ),
                valueFormat = "F2",
                sync = left
                    ? (Action<float>) (value => SyncJointDamper(_jointLeft, _pectoralRbLeft, value))
                    : (Action<float>) (value => SyncJointDamper(_jointRight, _pectoralRbRight, value)),
            };

        private PhysicsParameter NewPositionSpringZParameter(bool left) =>
            new PhysicsParameter(new JSONStorableFloat(VALUE, 0, 0, 1000))
            {
                config = new StaticPhysicsConfig(
                    635f,
                    // https://www.desmos.com/calculator/cz7o7u8v6w
                    massCurve: x => -0.29f * InvertMass(x),
                    softnessCurve: x => -0.21f * Curves.Exponential1(x, 1.9f, 1.74f, 1.17f)
                ),
                quicknessOffsetConfig = new StaticPhysicsConfig(
                    90,
                    softnessCurve: x => -0.44f * x
                ),
                slownessOffsetConfig = new StaticPhysicsConfig(
                    -60,
                    softnessCurve: x => -0.44f * x
                ),
                valueFormat = "F0",
                sync = left
                    ? (Action<float>) (value => SyncJointPositionZDriveSpring(_jointLeft, _pectoralRbLeft, value))
                    : (Action<float>) (value => SyncJointPositionZDriveSpring(_jointRight, _pectoralRbRight, value)),
            };

        private PhysicsParameter NewPositionDamperZParameter(bool left) =>
            new PhysicsParameter(new JSONStorableFloat(VALUE, 0, 0, 100))
            {
                config = new StaticPhysicsConfig(8f),
                valueFormat = "F0",
                sync = left
                    ? (Action<float>) (value => SyncJointPositionZDriveDamper(_jointLeft, _pectoralRbLeft, value))
                    : (Action<float>) (value => SyncJointPositionZDriveDamper(_jointRight, _pectoralRbRight, value)),
            };

        private PhysicsParameter NewTargetRotationYParameter(bool left) =>
            new PhysicsParameter(new JSONStorableFloat(VALUE, 0, -20.00f, 20.00f))
            {
                config = new StaticPhysicsConfig(0.00f),
                sync = left
                    ? (Action<float>) SyncTargetRotationYLeft
                    : (Action<float>) SyncTargetRotationYRight,
                valueFormat = "F2",
            };

        private PhysicsParameter NewTargetRotationXParameter(bool left) =>
            new PhysicsParameter(new JSONStorableFloat(VALUE, 0, -20.00f, 20.00f))
            {
                config = new StaticPhysicsConfig(0.00f),
                sync = left
                    ? (Action<float>) SyncTargetRotationXLeft
                    : (Action<float>) SyncTargetRotationXRight,
                valueFormat = "F2",
            };

        private void SetupPhysicsParameterGroups()
        {
            bool softPhysicsEnabled = _script.settingsMonitor.softPhysicsEnabled;

            var centerOfGravityPercent = new PhysicsParameterGroup(
                CENTER_OF_GRAVITY_PERCENT,
                NewCenterOfGravityParameter(true, softPhysicsEnabled),
                NewCenterOfGravityParameter(false, softPhysicsEnabled),
                "Center Of Gravity"
            )
            {
                requiresRecalibration = true,
            };

            var spring = new PhysicsParameterGroup(
                SPRING,
                NewSpringParameter(true),
                NewSpringParameter(false),
                "Spring"
            )
            {
                requiresRecalibration = true,
            };

            var damper = new PhysicsParameterGroup(
                DAMPER,
                NewDamperParameter(true, softPhysicsEnabled),
                NewDamperParameter(false, softPhysicsEnabled),
                "Damper"
            )
            {
                dependOnPhysicsRate = true,
            };

            var positionSpringZ = new PhysicsParameterGroup(
                POSITION_SPRING_Z,
                NewPositionSpringZParameter(true),
                NewPositionSpringZParameter(false),
                "In/Out Spring"
            )
            {
                requiresRecalibration = true,
            };

            var positionDamperZ = new PhysicsParameterGroup(
                POSITION_DAMPER_Z,
                NewPositionDamperZParameter(true),
                NewPositionDamperZParameter(false),
                "In/Out Damper"
            )
            {
                dependOnPhysicsRate = true,
            };

            var targetRotationY = new PhysicsParameterGroup(
                TARGET_ROTATION_Y,
                NewTargetRotationYParameter(true),
                NewTargetRotationYParameter(false),
                "Right/Left Angle Target"
            )
            {
                requiresRecalibration = true,
                invertRight = true,
            };

            var targetRotationX = new PhysicsParameterGroup(
                TARGET_ROTATION_X,
                NewTargetRotationXParameter(true),
                NewTargetRotationXParameter(false),
                "Up/Down Angle Target"
            )
            {
                requiresRecalibration = true,
            };

            centerOfGravityPercent.SetOffsetCallbackFunctions();
            spring.SetOffsetCallbackFunctions();
            damper.SetOffsetCallbackFunctions();
            positionSpringZ.SetOffsetCallbackFunctions();
            positionDamperZ.SetOffsetCallbackFunctions();
            targetRotationY.SetOffsetCallbackFunctions();
            targetRotationX.SetOffsetCallbackFunctions();

            parameterGroups = new Dictionary<string, PhysicsParameterGroup>
            {
                { CENTER_OF_GRAVITY_PERCENT, centerOfGravityPercent },
                { SPRING, spring },
                { DAMPER, damper },
                { POSITION_SPRING_Z, positionSpringZ },
                { POSITION_DAMPER_Z, positionDamperZ },
                { TARGET_ROTATION_X, targetRotationX },
                { TARGET_ROTATION_Y, targetRotationY },
            };
        }

        #region *** Sync functions ***

        // Reimplements AdjustJoints.cs method SyncMass
        private static void SyncMass(Rigidbody rb, float value)
        {
            if(Math.Abs(rb.mass - value) <= 0.001f)
            {
                return;
            }

            rb.mass = value;
            rb.WakeUp();
        }

        // Reimplements AdjustJoints.cs method SyncCenterOfGravity
        private void SyncCenterOfGravity(Rigidbody rb, float value)
        {
            var newCenterOfMass = Vector3.Lerp(_breastControl.lowCenterOfGravity, _breastControl.highCenterOfGravity, value);
            if(Calc.VectorEqualWithin(1 / 100f, rb.centerOfMass, newCenterOfMass))
            {
                return;
            }

            rb.centerOfMass = newCenterOfMass;
            rb.WakeUp();
        }

        private void SyncJointSpring(ConfigurableJoint joint, Rigidbody rb, float spring)
        {
            // see AdjustJoints.cs method ScaleChanged
            float scalePow = Mathf.Pow(1.7f, _breastControl.scale - 1f);

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
            angularXLimitSpring.spring = scaledSpring * _breastControl.limitSpringMultiplier;
            joint.angularXLimitSpring = angularXLimitSpring;

            var angularYZLimitSpring = joint.angularYZLimitSpring;
            angularYZLimitSpring.spring = scaledSpring * _breastControl.limitSpringMultiplier;
            joint.angularYZLimitSpring = angularYZLimitSpring;

            rb.WakeUp();
        }

        private void SyncJointDamper(ConfigurableJoint joint, Rigidbody rb, float damper)
        {
            // see AdjustJoints.cs method ScaleChanged
            float scalePow = Mathf.Pow(1.7f, _breastControl.scale - 1f);

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
            angularXLimitSpring.damper = scaledDamper * _breastControl.limitDamperMultiplier;
            joint.angularXLimitSpring = angularXLimitSpring;

            var angularYZLimitSpring = joint.angularYZLimitSpring;
            angularYZLimitSpring.damper = scaledDamper * _breastControl.limitDamperMultiplier;
            joint.angularYZLimitSpring = angularYZLimitSpring;

            rb.WakeUp();
        }

        // Reimplements AdjustJoints.cs method SyncJointPositionZDrive
        private static void SyncJointPositionZDriveSpring(ConfigurableJoint joint, Rigidbody rb, float spring)
        {
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
        private void SyncTargetRotationXLeft(float targetRotationX)
        {
            _breastControl.smoothedJoint2TargetRotation.x = targetRotationX;
            var dazBone = _jointLeft.GetComponent<DAZBone>();
            var rotation = _breastControl.smoothedJoint2TargetRotation;
            rotation.x = -rotation.x;
            dazBone.baseJointRotation = rotation;
        }

        // Reimplements AdjustJoints.cs methods SyncTargetRotation and SetTargetRotation
        private void SyncTargetRotationYLeft(float targetRotationY)
        {
            _breastControl.smoothedJoint2TargetRotation.y = -targetRotationY;
            var dazBone = _jointLeft.GetComponent<DAZBone>();
            var rotation = _breastControl.smoothedJoint2TargetRotation;
            dazBone.baseJointRotation = rotation;
        }

        // Reimplements AdjustJoints.cs methods SyncTargetRotation and SetTargetRotation
        private void SyncTargetRotationXRight(float targetRotationX)
        {
            _breastControl.smoothedJoint1TargetRotation.x = targetRotationX;
            var dazBone = _jointRight.GetComponent<DAZBone>();
            var rotation = _breastControl.smoothedJoint1TargetRotation;
            rotation.x = -rotation.x;
            dazBone.baseJointRotation = rotation;
        }

        // Reimplements AdjustJoints.cs methods SyncTargetRotation and SetTargetRotation
        private void SyncTargetRotationYRight(float targetRotationY)
        {
            _breastControl.smoothedJoint1TargetRotation.y = -targetRotationY;
            var dazBone = _jointRight.GetComponent<DAZBone>();
            var rotation = _breastControl.smoothedJoint1TargetRotation;
            dazBone.baseJointRotation = rotation;
        }

        #endregion *** Sync functions ***

        public void UpdatePhysics()
        {
            float softness = _script.softnessAmount;
            float quickness = _script.quicknessAmount;
            parameterGroups.Values
                .Where(paramGroup => paramGroup.hasStaticConfig)
                .ToList()
                .ForEach(paramGroup => UpdateParam(paramGroup, softness, quickness));
        }

        public void UpdateRateDependentPhysics()
        {
            float softness = _script.softnessAmount;
            float quickness = _script.quicknessAmount;
            parameterGroups.Values
                .Where(paramGroup => paramGroup.hasStaticConfig && paramGroup.dependOnPhysicsRate)
                .ToList()
                .ForEach(paramGroup => UpdateParam(paramGroup, softness, quickness));
        }

        private void UpdateParam(PhysicsParameterGroup paramGroup, float softness, float quickness)
        {
            float massValue = paramGroup.useRealMass ? realMassAmount : massAmount;
            paramGroup.UpdateValue(massValue, softness, quickness);
        }

        public void SaveOriginalPhysicsAndSetPluginDefaults()
        {
            // disable pectoral collisions, they cause breasts to "jump" when touched
            _originalPectoralRbLeftDetectCollisions = _pectoralRbLeft.detectCollisions;
            _originalPectoralRbRightDetectCollisions = _pectoralRbRight.detectCollisions;
            _pectoralRbLeft.detectCollisions = false;
            _pectoralRbRight.detectCollisions = false;

            _originalBreastControlFloats = new Dictionary<string, float>();
            foreach(string jsonParamName in _adjustJointsFloatParamNames)
            {
                var paramJsf = _breastControl.GetFloatJSONParam(jsonParamName);
                _originalBreastControlFloats[jsonParamName] = paramJsf.val;
                paramJsf.val = 0;
            }
        }

        public void RestoreOriginalPhysics()
        {
            _pectoralRbLeft.detectCollisions = _originalPectoralRbLeftDetectCollisions;
            _pectoralRbRight.detectCollisions = _originalPectoralRbRightDetectCollisions;
            foreach(string jsonParamName in _adjustJointsFloatParamNames)
            {
                _breastControl.GetFloatJSONParam(jsonParamName).val = _originalBreastControlFloats[jsonParamName];
            }
        }

        public JSONClass GetJSON()
        {
            var jsonClass = new JSONClass();
            jsonClass["originals"] = OriginalsJSON();
            jsonClass["parameters"] = ParametersJSONArray();
            return jsonClass;
        }

        private JSONClass OriginalsJSON()
        {
            var jsonClass = new JSONClass();
            jsonClass["breastControlFloats"] = JSONUtils.JSONArrayFromDictionary(_originalBreastControlFloats);
            jsonClass["pectoralRbLeftDetectCollisions"].AsBool = _originalPectoralRbLeftDetectCollisions;
            jsonClass["pectoralRbRightDetectCollisions"].AsBool = _originalPectoralRbRightDetectCollisions;
            return jsonClass;
        }

        private JSONArray ParametersJSONArray()
        {
            var jsonArray = new JSONArray();
            foreach(var group in parameterGroups)
            {
                var groupJsonClass = group.Value.GetJSON();
                if(groupJsonClass != null)
                {
                    jsonArray.Add(groupJsonClass);
                }
            }

            return jsonArray;
        }

        public void RestoreFromJSON(JSONClass jsonClass)
        {
            if(jsonClass.HasKey("originals"))
            {
                RestoreOriginalsFromJSON(jsonClass["originals"].AsObject);
            }

            if(jsonClass.HasKey("parameters"))
            {
                RestoreParametersFromJSON(jsonClass["parameters"].AsArray);
            }
        }

        private void RestoreOriginalsFromJSON(JSONClass originalJson)
        {
            if(originalJson.HasKey("breastControlFloats"))
            {
                var breastControlFloats = originalJson["breastControlFloats"].AsArray;
                foreach(JSONClass jc in breastControlFloats)
                    _originalBreastControlFloats[jc["id"].Value] = jc["value"].AsFloat;
            }

            if(originalJson.HasKey("pectoralRbLeftDetectCollisions"))
            {
                _originalPectoralRbLeftDetectCollisions = originalJson["pectoralRbLeftDetectCollisions"].AsBool;
            }

            if(originalJson.HasKey("pectoralRbRightDetectCollisions"))
            {
                _originalPectoralRbRightDetectCollisions = originalJson["pectoralRbRightDetectCollisions"].AsBool;
            }
        }

        private void RestoreParametersFromJSON(JSONArray jsonArray)
        {
            foreach(JSONClass jc in jsonArray)
            {
                parameterGroups[jc["id"].Value].RestoreFromJSON(jc);
            }
        }

        private static Dictionary<string, string> CreateInfoTexts()
        {
            Func<string> centerOfGravityInfoText = () =>
            {
                var sb = new StringBuilder();
                sb.Append("The position of the pectoral joint's center of mass.");
                sb.Append("\n\n");
                sb.Append("At 0, the center of mass is inside the chest at the pectoral joint. At 1, it is at the nipple.");
                sb.Append(" Between about 0.5 and 0.8, the center of mass is within the bulk of the breast volume.");
                return sb.ToString();
            };

            Func<string> springInfoText = () =>
            {
                var sb = new StringBuilder();
                sb.Append("The magnitude of the spring that pushes the pectoral joint towards its angle target.");
                sb.Append("\n\n");
                sb.Append("The angle target is defined by the Up/Down and Left/Right Angle Target parameters.");
                return sb.ToString();
            };

            Func<string> damperInfoText = () =>
            {
                var sb = new StringBuilder();
                sb.Append("The magnitude of the damper that reduces oscillation around the joint angle target.");
                sb.Append("\n\n");
                sb.Append("The higher the damper, the quicker breasts will stop swinging.");
                return sb.ToString();
            };

            Func<string> positionSpringZInfoText = () =>
            {
                var sb = new StringBuilder();
                sb.Append("The magnitude of the spring that pushes the pectoral joint towards its position target along the Z axis.");
                sb.Append("\n\n");
                sb.Append("Directional force morphing along the forward-back axis depends on In/Out Spring being suitably low");
                sb.Append(" for the given breast mass.");
                return sb.ToString();
            };

            Func<string> positionDamperZInfoText = () =>
            {
                var sb = new StringBuilder();
                sb.Append("The magnitude of the damper that reduces oscillation around the joint position target along the Z axis.");
                return sb.ToString();
            };

            Func<string> targetRotationXInfoText = () =>
            {
                var sb = new StringBuilder();
                sb.Append("The vertical target angle of the pectoral joint.");
                sb.Append(" Negative values pull breasts down, positive values push them up.");
                sb.Append("\n\n");
                sb.Append("The offset shifts the center around which the final angle is calculated");
                sb.Append(" based on chest angle (see Gravity Multipliers)");
                return sb.ToString();
            };

            Func<string> targetRotationYInfoText = () =>
            {
                var sb = new StringBuilder();
                sb.Append("The horizontal target angle of the pectoral joint.");
                sb.Append("\n\n");
                sb.Append("Negative values push breasts apart, positive values pull them together.");
                return sb.ToString();
            };

            return new Dictionary<string, string>()
            {
                { CENTER_OF_GRAVITY_PERCENT, centerOfGravityInfoText() },
                { SPRING, springInfoText() },
                { DAMPER, damperInfoText() },
                { POSITION_SPRING_Z, positionSpringZInfoText() },
                { POSITION_DAMPER_Z, positionDamperZInfoText() },
                { TARGET_ROTATION_X, targetRotationXInfoText() },
                { TARGET_ROTATION_Y, targetRotationYInfoText() },
            };
        }
    }
}
