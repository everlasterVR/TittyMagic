using System;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using TittyMagic.Configs;
using UnityEngine;
using static TittyMagic.MVRParamName;
using static TittyMagic.Utils;

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

        public Dictionary<string, PhysicsParameter> leftBreastParameters { get; private set; }
        public Dictionary<string, PhysicsParameter> rightBreastParameters { get; private set; }

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
                massJsf.val = newMass;
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
            SetupPhysicsParameters(true, softPhysicsEnabled);
            SetupPhysicsParameters(false, softPhysicsEnabled);
        }

        private void SetupPhysicsParameters(bool leftBreast, bool softPhysicsEnabled)
        {
            var centerOfGravityPercent = new PhysicsParameter(
                "Center Of Gravity",
                NewBaseValueStorable(0, 1),
                "F3"
            );
            var spring = new PhysicsParameter(
                "Spring",
                NewBaseValueStorable(0, 100),
                "F2"
            );
            var damper = new PhysicsParameter(
                "Damper",
                NewBaseValueStorable(0, 5),
                "F2"
            );
            var positionSpringZ = new PhysicsParameter(
                "In/Out Spring",
                NewBaseValueStorable(0, 1000),
                "F2"
            );
            var positionDamperZ = new PhysicsParameter(
                "In/Out Damper",
                NewBaseValueStorable(0, 1000),
                "F3"
            );
            var targetRotationY = new PhysicsParameter(
                "Right/Left Angle Target",
                null,
                NewCurrentValueStorable(-20, 20),
                "F2"
            );
            var targetRotationX = new PhysicsParameter(
                "Up/Down Angle Target",
                null,
                NewCurrentValueStorable(-20, 20),
                "F2"
            );

            if(softPhysicsEnabled)
            {
                centerOfGravityPercent.config = new StaticPhysicsConfig(0.350f, 0.480f, 0.560f);

                damper.config = new StaticPhysicsConfig(2.4f, 2.8f, 0.9f);
                damper.config.dependOnPhysicsRate = true;
                damper.config.SetLinearCurvesAroundMidpoint(slope: 0.2f);
                damper.quicknessOffsetConfig = new StaticPhysicsConfigBase(-0.6f, -0.75f, -0.4f);
                damper.slownessOffsetConfig = new StaticPhysicsConfigBase(0.4f, 0.5f, 0.27f);
            }
            else
            {
                centerOfGravityPercent.config = new StaticPhysicsConfig(0.525f, 0.750f, 0.900f);

                damper.config = new StaticPhysicsConfig(1.8f, 2.1f, 0.675f);
                damper.config.dependOnPhysicsRate = true;
                damper.config.SetLinearCurvesAroundMidpoint(slope: 0.2f);
                damper.quicknessOffsetConfig = new StaticPhysicsConfigBase(-0.45f, -0.56f, -0.3f);
                damper.slownessOffsetConfig = new StaticPhysicsConfigBase(0.3f, 0.38f, 0.2f);
            }

            spring.config = new StaticPhysicsConfig(82f, 96f, 45f);
            spring.config.SetLinearCurvesAroundMidpoint(slope: 0.135f);
            spring.quicknessOffsetConfig = new StaticPhysicsConfigBase(20f, 24f, 18f);
            spring.slownessOffsetConfig = new StaticPhysicsConfigBase(-13f, -16f, -12f);

            positionSpringZ.config = new StaticPhysicsConfig(850f, 950f, 250f);
            positionSpringZ.config.SetLinearCurvesAroundMidpoint(slope: 0.33f);
            positionSpringZ.quicknessOffsetConfig = new StaticPhysicsConfigBase(90, 110, 50f);
            positionSpringZ.slownessOffsetConfig = new StaticPhysicsConfigBase(-60, -70, -33f);

            positionDamperZ.config = new StaticPhysicsConfig(16f, 22f, 9f);
            positionDamperZ.config.dependOnPhysicsRate = true;

            if(leftBreast)
            {
                centerOfGravityPercent.sync = value => SyncCenterOfGravity(_pectoralRbLeft, value);
                spring.sync = value => SyncJointSpring(_jointLeft, _pectoralRbLeft, value);
                damper.sync = value => SyncJointDamper(_jointLeft, _pectoralRbLeft, value);
                positionSpringZ.sync = value => SyncJointPositionZDriveSpring(_jointLeft, _pectoralRbLeft, value);
                positionDamperZ.sync = value => SyncJointPositionZDriveDamper(_jointLeft, _pectoralRbLeft, value);
                targetRotationX.sync = SyncTargetRotationXLeft;
                targetRotationY.sync = SyncTargetRotationYLeft;
            }
            else
            {
                centerOfGravityPercent.sync = value => SyncCenterOfGravity(_pectoralRbRight, value);
                spring.sync = value => SyncJointSpring(_jointRight, _pectoralRbRight, value);
                damper.sync = value => SyncJointDamper(_jointRight, _pectoralRbRight, value);
                positionSpringZ.sync = value => SyncJointPositionZDriveSpring(_jointRight, _pectoralRbRight, value);
                positionDamperZ.sync = value => SyncJointPositionZDriveDamper(_jointRight, _pectoralRbRight, value);
                targetRotationX.sync = SyncTargetRotationXRight;
                targetRotationY.sync = SyncTargetRotationYRight;
            }

            var parameters = new Dictionary<string, PhysicsParameter>
            {
                { CENTER_OF_GRAVITY_PERCENT, centerOfGravityPercent },
                { SPRING, spring },
                { DAMPER, damper },
                { POSITION_SPRING_Z, positionSpringZ },
                { POSITION_DAMPER_Z, positionDamperZ },
                { TARGET_ROTATION_X, targetRotationX },
                { TARGET_ROTATION_Y, targetRotationY },
            };

            var texts = CreateInfoTexts();
            foreach(var param in parameters)
            {
                param.Value.infoText = texts[param.Key];
            }

            if(leftBreast)
                leftBreastParameters = parameters;
            else
                rightBreastParameters = parameters;
        }

        // Reimplements AdjustJoints.cs method SyncMass
        private static void SyncMass(Rigidbody rb, float value)
        {
            if(Math.Abs(rb.mass - value) > 0.001f)
            {
                rb.mass = value;
                rb.WakeUp();
            }
        }

        // Reimplements AdjustJoints.cs method SyncCenterOfGravity
        private void SyncCenterOfGravity(Rigidbody rb, float value)
        {
            var newCenterOfMass = Vector3.Lerp(_breastControl.lowCenterOfGravity, _breastControl.highCenterOfGravity, value);
            if(rb.centerOfMass != newCenterOfMass)
            {
                rb.centerOfMass = newCenterOfMass;
                rb.WakeUp();
            }
        }

        private void SyncJointSpring(ConfigurableJoint joint, Rigidbody rb, float spring)
        {
            // see AdjustJoints.cs method ScaleChanged
            float scalePow = Mathf.Pow(1.7f, _breastControl.scale - 1f);

            float scaledSpring = spring * scalePow;

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
            zDrive.positionSpring = spring;
            joint.zDrive = zDrive;
            rb.WakeUp();
        }

        // Reimplements AdjustJoints.cs method SyncJointPositionZDrive
        private static void SyncJointPositionZDriveDamper(ConfigurableJoint joint, Rigidbody rb, float damper)
        {
            var zDrive = joint.zDrive;
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
            Vector3 rotation = _breastControl.smoothedJoint2TargetRotation;
            rotation.x = -rotation.x;
            dazBone.baseJointRotation = rotation;
        }

        // Reimplements AdjustJoints.cs methods SyncTargetRotation and SetTargetRotation
        private void SyncTargetRotationYLeft(float targetRotationY)
        {
            _breastControl.smoothedJoint2TargetRotation.y = targetRotationY;
            var dazBone = _jointLeft.GetComponent<DAZBone>();
            Vector3 rotation = _breastControl.smoothedJoint2TargetRotation;
            dazBone.baseJointRotation = rotation;
        }

        // Reimplements AdjustJoints.cs methods SyncTargetRotation and SetTargetRotation
        private void SyncTargetRotationXRight(float targetRotationX)
        {
            _breastControl.smoothedJoint1TargetRotation.x = targetRotationX;
            var dazBone = _jointRight.GetComponent<DAZBone>();
            Vector3 rotation = _breastControl.smoothedJoint1TargetRotation;
            rotation.x = -rotation.x;
            dazBone.baseJointRotation = rotation;
        }

        // Reimplements AdjustJoints.cs methods SyncTargetRotation and SetTargetRotation
        private void SyncTargetRotationYRight(float targetRotationY)
        {
            _breastControl.smoothedJoint1TargetRotation.y = targetRotationY;
            var dazBone = _jointRight.GetComponent<DAZBone>();
            Vector3 rotation = _breastControl.smoothedJoint1TargetRotation;
            dazBone.baseJointRotation = rotation;
        }

        public void UpdatePhysics(float softnessAmount, float quicknessAmount)
        {
            leftBreastParameters.Values
                .Concat(rightBreastParameters.Values)
                .Where(param => param.config != null).ToList()
                .ForEach(param => UpdateParam(param, softnessAmount, quicknessAmount));
        }

        public void UpdateRateDependentPhysics(float softnessAmount, float quicknessAmount)
        {
            leftBreastParameters.Values
                .Concat(rightBreastParameters.Values)
                .Where(param => param.config != null && param.config.dependOnPhysicsRate).ToList()
                .ForEach(param => UpdateParam(param, softnessAmount, quicknessAmount));
        }

        private void UpdateParam(PhysicsParameter param, float softnessAmount, float quicknessAmount)
        {
            float massValue = param.config.useRealMass ? realMassAmount : massAmount;
            float value = NewBaseValue(param, massValue, softnessAmount, quicknessAmount);
            param.SetValue(value);
        }

        public static float NewBaseValue(PhysicsParameter param, float massValue, float softness, float quickness)
        {
            float value = param.config.Calculate(massValue, softness);
            if(param.quicknessOffsetConfig != null && quickness > 0)
            {
                float maxQuicknessOffset = param.quicknessOffsetConfig.Calculate(massValue, softness);
                value += Mathf.Lerp(0, maxQuicknessOffset, quickness);
            }

            if(param.slownessOffsetConfig != null && quickness < 0)
            {
                float maxSlownessOffset = param.slownessOffsetConfig.Calculate(massValue, softness);
                value += Mathf.Lerp(0, maxSlownessOffset, -quickness);
            }

            return param.config.dependOnPhysicsRate ? PhysicsRateMultiplier() * value : value;
        }

        public void SaveOriginalPhysicsAndSetPluginDefaults()
        {
            // disable pectoral collisions, they cause breasts to "jump" when touched
            _originalPectoralRbLeftDetectCollisions = _pectoralRbLeft.detectCollisions;
            _originalPectoralRbRightDetectCollisions = _pectoralRbRight.detectCollisions;
            _pectoralRbLeft.detectCollisions = false;
            _pectoralRbRight.detectCollisions = false;

            _originalBreastControlFloats = new Dictionary<string, float>();
            foreach(string name in _adjustJointsFloatParamNames)
            {
                var param = _breastControl.GetFloatJSONParam(name);
                _originalBreastControlFloats[name] = param.val;
                param.val = 0;
            }
        }

        public void RestoreOriginalPhysics()
        {
            _pectoralRbLeft.detectCollisions = _originalPectoralRbLeftDetectCollisions;
            _pectoralRbRight.detectCollisions = _originalPectoralRbRightDetectCollisions;
            foreach(string name in _adjustJointsFloatParamNames)
            {
                _breastControl.GetFloatJSONParam(name).val = _originalBreastControlFloats[name];
            }
        }

        public JSONClass Serialize()
        {
            var jsonClass = new JSONClass();
            jsonClass["breastControlFloats"] = JSONArrayFromDictionary(_originalBreastControlFloats);
            jsonClass["pectoralRbLeftDetectCollisions"].AsBool = _originalPectoralRbLeftDetectCollisions;
            jsonClass["pectoralRbRightDetectCollisions"].AsBool = _originalPectoralRbRightDetectCollisions;
            return jsonClass;
        }

        private static JSONArray JSONArrayFromDictionary(Dictionary<string, float> dictionary)
        {
            var jsonArray = new JSONArray();
            foreach(var kvp in dictionary)
            {
                var entry = new JSONClass();
                entry["paramName"] = kvp.Key;
                entry["value"].AsFloat = kvp.Value;
                jsonArray.Add(entry);
            }
            return jsonArray;
        }

        public void RestoreFromJSON(JSONClass originalJson)
        {
            var breastControlFloats = originalJson["breastControlFloats"].AsArray;
            foreach(JSONClass json in breastControlFloats)
            {
                _originalBreastControlFloats[json["paramName"].Value] = json["value"].AsFloat;
            }

            _originalPectoralRbLeftDetectCollisions = originalJson["pectoralRbLeftDetectCollisions"].AsBool;
            _originalPectoralRbRightDetectCollisions = originalJson["pectoralRbRightDetectCollisions"].AsBool;
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
                $"";

            texts[TARGET_ROTATION_Y] =
                $"";

            return texts;
        }
    }
}
