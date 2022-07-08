// ReSharper disable RedundantCast
using System;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using TittyMagic.Configs;
using UnityEngine;
using static TittyMagic.ParamName;
using static TittyMagic.Intl;

namespace TittyMagic
{
    internal class MainPhysicsHandler
    {
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

        public JSONStorableFloat massJsf { get; }

        public MainPhysicsHandler(
            MVRScript script,
            AdjustJoints breastControl,
            Rigidbody pectoralRbLeft,
            Rigidbody pectoralRbRight
        )
        {
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

            massJsf = script.NewJSONStorableFloat("breastMass", 0.1f, 0.1f, 3f);
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

        public void LoadSettings(bool softPhysicsEnabled)
        {
            SetupPhysicsParameterGroups(softPhysicsEnabled);

            var texts = CreateInfoTexts();
            foreach(var param in parameterGroups)
            {
                param.Value.infoText = texts[param.Key];
            }
        }

        private PhysicsParameter NewCenterOfGravityParameter(bool left, bool softPhysicsEnabled) =>
            new PhysicsParameter(CENTER_OF_GRAVITY_PERCENT, new JSONStorableFloat(VALUE, 0, 0, 1))
            {
                config = softPhysicsEnabled
                    ? new StaticPhysicsConfig(0.450f, 0.580f, 0.660f)
                    : new StaticPhysicsConfig(0.525f, 0.750f, 0.900f),
                valueFormat = "F2",
                sync = left
                    ? (Action<float>) (value => SyncCenterOfGravity(_pectoralRbLeft, value))
                    : (Action<float>) (value => SyncCenterOfGravity(_pectoralRbRight, value)),
            };

        private PhysicsParameter NewSpringParameter(bool left) =>
            new PhysicsParameter(SPRING, new JSONStorableFloat(VALUE, 0, 10, 200))
            {
                config = new StaticPhysicsConfig(72f, 84f, 45f),
                quicknessOffsetConfig = new StaticPhysicsConfig(20f, 24f, 18f),
                slownessOffsetConfig = new StaticPhysicsConfig(-13f, -16f, -12f),
                valueFormat = "F0",
                sync = left
                    ? (Action<float>) (value => SyncJointSpring(_jointLeft, _pectoralRbLeft, value))
                    : (Action<float>) (value => SyncJointSpring(_jointRight, _pectoralRbRight, value)),
            };

        private PhysicsParameter NewDamperParameter(bool left, bool softPhysicsEnabled) =>
            new PhysicsParameter(DAMPER, new JSONStorableFloat(VALUE, 0, 0, 10))
            {
                config = softPhysicsEnabled
                    ? new StaticPhysicsConfig(1.2f, 1.4f, 0.45f)
                    : new StaticPhysicsConfig(0.9f, 1.05f, 0.35f),
                quicknessOffsetConfig = softPhysicsEnabled
                    ? new StaticPhysicsConfig(-0.3f, -0.375f, -0.2f)
                    : new StaticPhysicsConfig(-0.225f, -0.28f, -0.15f),
                slownessOffsetConfig = softPhysicsEnabled
                    ? new StaticPhysicsConfig(0.2f, 0.25f, 0.135f)
                    : new StaticPhysicsConfig(0.15f, 0.19f, 0.1f),
                valueFormat = "F2",
                sync = left
                    ? (Action<float>) (value => SyncJointDamper(_jointLeft, _pectoralRbLeft, value))
                    : (Action<float>) (value => SyncJointDamper(_jointRight, _pectoralRbRight, value)),
            };

        private PhysicsParameter NewPositionSpringZParameter(bool left) =>
            new PhysicsParameter(POSITION_SPRING_Z, new JSONStorableFloat(VALUE, 0, 0, 1000))
            {
                config = new StaticPhysicsConfig(850f, 950f, 250f),
                quicknessOffsetConfig = new StaticPhysicsConfig(90, 110, 50f),
                slownessOffsetConfig = new StaticPhysicsConfig(-60, -70, -33f),
                valueFormat = "F0",
                sync = left
                    ? (Action<float>) (value => SyncJointPositionZDriveSpring(_jointLeft, _pectoralRbLeft, value))
                    : (Action<float>) (value => SyncJointPositionZDriveSpring(_jointRight, _pectoralRbRight, value)),
            };

        private PhysicsParameter NewPositionDamperZParameter(bool left) =>
            new PhysicsParameter(POSITION_SPRING_Z, new JSONStorableFloat(VALUE, 0, 0, 100))
            {
                config = new StaticPhysicsConfig(16f, 22f, 9f),
                quicknessOffsetConfig = new StaticPhysicsConfig(20f, 24f, 18f),
                slownessOffsetConfig = new StaticPhysicsConfig(-13f, -16f, -12f),
                valueFormat = "F0",
                sync = left
                    ? (Action<float>) (value => SyncJointPositionZDriveDamper(_jointLeft, _pectoralRbLeft, value))
                    : (Action<float>) (value => SyncJointPositionZDriveDamper(_jointRight, _pectoralRbRight, value)),
            };

        private PhysicsParameter NewTargetRotationYParameter(bool left) =>
            new PhysicsParameter(TARGET_ROTATION_Y, new JSONStorableFloat(VALUE, 0, -20, 20))
            {
                sync = left
                    ? (Action<float>) SyncTargetRotationYLeft
                    : (Action<float>) SyncTargetRotationYRight,
                valueFormat = "F2",
            };

        private PhysicsParameter NewTargetRotationXParameter(bool left) =>
            new PhysicsParameter(TARGET_ROTATION_X, new JSONStorableFloat(VALUE, 0, -20, 20))
            {
                sync = left
                    ? (Action<float>) SyncTargetRotationXLeft
                    : (Action<float>) SyncTargetRotationXRight,
                valueFormat = "F2",
            };

        private void SetupPhysicsParameterGroups(bool softPhysicsEnabled)
        {
            var centerOfGravityPercent = new PhysicsParameterGroup(
                NewCenterOfGravityParameter(true, softPhysicsEnabled),
                NewCenterOfGravityParameter(false, softPhysicsEnabled),
                "Center Of Gravity"
            )
            {
                requiresRecalibration = true,
            };

            var spring = new PhysicsParameterGroup(
                NewSpringParameter(true),
                NewSpringParameter(false),
                "Spring"
            )
            {
                requiresRecalibration = true,
            };
            spring.SetLinearCurvesAroundMidpoint(slope: 0.135f);

            var damper = new PhysicsParameterGroup(
                NewDamperParameter(true, softPhysicsEnabled),
                NewDamperParameter(false, softPhysicsEnabled),
                "Damper"
            )
            {
                dependOnPhysicsRate = true,
            };
            damper.SetLinearCurvesAroundMidpoint(slope: 0.2f);

            var positionSpringZ = new PhysicsParameterGroup(
                NewPositionSpringZParameter(true),
                NewPositionSpringZParameter(false),
                "In/Out Spring"
            )
            {
                requiresRecalibration = true,
            };
            positionSpringZ.SetLinearCurvesAroundMidpoint(slope: 0.33f);

            var positionDamperZ = new PhysicsParameterGroup(
                NewPositionDamperZParameter(true),
                NewPositionDamperZParameter(false),
                "In/Out Damper"
            )
            {
                dependOnPhysicsRate = true,
            };

            var targetRotationY = new PhysicsParameterGroup(
                NewTargetRotationYParameter(true),
                NewTargetRotationYParameter(false),
                "Right/Left Angle Target"
            )
            {
                requiresRecalibration = true,
            };

            var targetRotationX = new PhysicsParameterGroup(
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
            if(Calc.VectorEqualWithin(100f, rb.centerOfMass, newCenterOfMass))
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
        // Circumvents default invertJoint2RotationX = true
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
            _breastControl.smoothedJoint2TargetRotation.y = targetRotationY;
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
            _breastControl.smoothedJoint1TargetRotation.y = targetRotationY;
            var dazBone = _jointRight.GetComponent<DAZBone>();
            var rotation = _breastControl.smoothedJoint1TargetRotation;
            dazBone.baseJointRotation = rotation;
        }

        #endregion *** Sync functions ***

        public void UpdatePhysics(float softnessAmount, float quicknessAmount) =>
            parameterGroups.Values
                .Where(paramGroup => paramGroup.hasStaticConfig)
                .ToList()
                .ForEach(paramGroup => UpdateParam(paramGroup, softnessAmount, quicknessAmount));

        public void UpdateRateDependentPhysics(float softnessAmount, float quicknessAmount) =>
            parameterGroups.Values
                .Where(paramGroup => paramGroup.hasStaticConfig && paramGroup.dependOnPhysicsRate)
                .ToList()
                .ForEach(paramGroup => UpdateParam(paramGroup, softnessAmount, quicknessAmount));

        private void UpdateParam(PhysicsParameterGroup paramGroup, float softnessAmount, float quicknessAmount)
        {
            float massValue = paramGroup.useRealMass ? realMassAmount : massAmount;
            paramGroup.UpdateValue(massValue, softnessAmount, quicknessAmount);
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

        public JSONClass Serialize()
        {
            var jsonClass = new JSONClass();
            jsonClass["breastControlFloats"] = JSONUtils.JSONArrayFromDictionary(_originalBreastControlFloats);
            jsonClass["pectoralRbLeftDetectCollisions"].AsBool = _originalPectoralRbLeftDetectCollisions;
            jsonClass["pectoralRbRightDetectCollisions"].AsBool = _originalPectoralRbRightDetectCollisions;
            return jsonClass;
        }

        public void RestoreFromJSON(JSONClass originalJson)
        {
            if(originalJson.HasKey("breastControlFloats"))
            {
                var breastControlFloats = originalJson["breastControlFloats"].AsArray;
                foreach(JSONClass json in breastControlFloats)
                    _originalBreastControlFloats[json["paramName"].Value] = json["value"].AsFloat;
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

        private static Dictionary<string, string> CreateInfoTexts()
        {
            var texts = new Dictionary<string, string>();

            texts[CENTER_OF_GRAVITY_PERCENT] =
                $"";

            texts[SPRING] =
                $"";

            texts[DAMPER] =
                $"";

            texts[POSITION_SPRING_Z] =
                $"";

            texts[POSITION_DAMPER_Z] =
                $"";

            texts[TARGET_ROTATION_X] =
                $"Determines the vertical angle of the breast relative to the person." +
                $" Negative values pull breasts down, positive values pull up." +
                $"\n\n" +
                $"The offset shifts the center around which the final value is calculated" +
                $" based on chest angle (see Gravity Multipliers).";

            texts[TARGET_ROTATION_Y] =
                $"Determines the horizontal angle of the breast relative to the person." +
                $" Negative values pull breasts together, positive values pull outward." +
                $"\n\n" +
                $"The offset shifts the center around which the final value is calculated" +
                $" based on chest angle (see Gravity Multipliers).";

            return texts;
        }
    }
}
