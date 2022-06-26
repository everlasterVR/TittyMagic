using System;
using System.Collections.Generic;
using SimpleJSON;
using TittyMagic.Configs;
using UnityEngine;

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

        private List<StaticPhysicsConfig> _mainPhysicsConfigs;

        public float realMassAmount { get; private set; }
        public float massAmount { get; private set; }

        public JSONStorableFloat mass { get; set; }

        private float _centerOfGravityLeft;
        private float _centerOfGravityRight;
        private float _springLeft;
        private float _springRight;
        private float _damperLeft;
        private float _damperRight;
        private float _positionSpringLeft;
        private float _positionSpringRight;
        private float _positionDamperLeft;
        private float _positionDamperRight;
        private float _targetRotationYLeft;
        private float _targetRotationYRight;
        private float _targetRotationXLeft;
        private float _targetRotationXRight;

        public MainPhysicsHandler(
            bool isFemale,
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
                "mass",
                "centerOfGravityPercent",
                "spring",
                "damper",
                "positionSpringZ",
                "positionDamperZ",
                "targetRotationX",
                "targetRotationY",
            };

            SaveOriginalPhysicsAndSetPluginDefaults();
        }

        public void UpdateMassValueAndAmounts(bool useNewMass, float volume)
        {
            float newMass = Mathf.Clamp(
                Mathf.Pow(0.78f * volume, 1.5f),
                mass.min,
                mass.max
            );
            realMassAmount = newMass / 2;
            if(useNewMass)
            {
                massAmount = realMassAmount;
                mass.val = newMass;
            }
            else
            {
                massAmount = mass.val / 2;
            }

            SyncMass(_pectoralRbLeft, mass.val);
            SyncMass(_pectoralRbRight, mass.val);
        }

        public void AddToLeftCenterOfGravity(float value)
        {
            SyncCenterOfGravity(_pectoralRbLeft, _centerOfGravityLeft + value);
        }

        public void AddToRightCenterOfGravity(float value)
        {
            SyncCenterOfGravity(_pectoralRbRight, _centerOfGravityRight + value);
        }

        public void AddToLeftJointSpring(float value)
        {
            SyncJoint(_jointLeft, _pectoralRbLeft, _springLeft + value, _damperLeft);
        }

        public void AddToRightJointSpring(float value)
        {
            SyncJoint(_jointRight, _pectoralRbRight, _springRight + value, _damperLeft);
        }

        public void AddToLeftJointDamper(float value)
        {
            SyncJoint(_jointLeft, _pectoralRbLeft, _springLeft, _damperLeft + value);
        }

        public void AddToRightJointDamper(float value)
        {
            SyncJoint(_jointRight, _pectoralRbRight, _springRight, _damperLeft + value);
        }

        public void AddToLeftJointPositionSpringZ(float value)
        {
            SyncJointPositionZDrive(_jointLeft, _pectoralRbLeft, _positionSpringLeft + value, _positionDamperLeft);
        }

        public void AddToRightJointPositionSpringZ(float value)
        {
            SyncJointPositionZDrive(_jointRight, _pectoralRbRight, _positionSpringRight + value, _positionDamperRight);
        }

        public void AddToLeftJointPositionDamperZ(float value)
        {
            SyncJointPositionZDrive(_jointLeft, _pectoralRbLeft, _positionSpringLeft, _positionDamperLeft + value);
        }

        public void AddToRightJointPositionDamperZ(float value)
        {
            SyncJointPositionZDrive(_jointRight, _pectoralRbRight, _positionSpringRight, _positionDamperRight + value);
        }

        public void SetTargetRotationYLeft(float value)
        {
            _targetRotationYLeft = value;
            SyncTargetRotationLeft(_targetRotationXLeft, _targetRotationYLeft);
        }

        public void SetTargetRotationYRight(float value)
        {
            _targetRotationYRight = value;
            SyncTargetRotationRight(_targetRotationXRight, _targetRotationYRight);
        }

        public void SetTargetRotationXLeft(float value)
        {
            _targetRotationXLeft = value;
            SyncTargetRotationLeft(_targetRotationXLeft, _targetRotationYLeft);
        }

        public void SetTargetRotationXRight(float value)
        {
            _targetRotationXRight = value;
            SyncTargetRotationRight(_targetRotationXRight, _targetRotationYRight);
        }

        public void LoadSettings(bool softPhysicsEnabled)
        {
            LoadMainPhysicsSettings(softPhysicsEnabled);
        }

        private void LoadMainPhysicsSettings(bool softPhysicsEnabled)
        {
            StaticPhysicsConfig centerOfGravityPercent;
            StaticPhysicsConfig damper;
            if(softPhysicsEnabled)
            {
                centerOfGravityPercent = new StaticPhysicsConfig(0.350f, 0.480f, 0.560f);

                damper = new StaticPhysicsConfig(2.4f, 2.8f, 0.9f)
                {
                    dependOnPhysicsRate = true,
                    quicknessOffsetConfig = new StaticPhysicsConfigBase(-0.6f, -0.75f, -0.4f),
                    slownessOffsetConfig = new StaticPhysicsConfigBase(0.4f, 0.5f, 0.27f),
                };
                damper.SetLinearCurvesAroundMidpoint(slope: 0.2f);
            }
            else
            {
                centerOfGravityPercent = new StaticPhysicsConfig(0.525f, 0.750f, 0.900f);

                damper = new StaticPhysicsConfig(1.8f, 2.1f, 0.675f)
                {
                    dependOnPhysicsRate = true,
                    quicknessOffsetConfig = new StaticPhysicsConfigBase(-0.45f, -0.56f, -0.3f),
                    slownessOffsetConfig = new StaticPhysicsConfigBase(0.3f, 0.38f, 0.2f),
                };
                damper.SetLinearCurvesAroundMidpoint(slope: 0.2f);
            }

            var spring = new StaticPhysicsConfig(82f, 96f, 45f)
            {
                quicknessOffsetConfig = new StaticPhysicsConfigBase(20f, 24f, 18f),
                slownessOffsetConfig = new StaticPhysicsConfigBase(-13f, -16f, -12f),
            };
            spring.SetLinearCurvesAroundMidpoint(slope: 0.135f);

            var positionSpringZ = new StaticPhysicsConfig(850f, 950f, 250f)
            {
                quicknessOffsetConfig = new StaticPhysicsConfigBase(90, 110, 50f),
                slownessOffsetConfig = new StaticPhysicsConfigBase(-60, -70, -33f),
            };
            positionSpringZ.SetLinearCurvesAroundMidpoint(slope: 0.33f);

            var positionDamperZ = new StaticPhysicsConfig(16f, 22f, 9f)
            {
                dependOnPhysicsRate = true,
            };

            centerOfGravityPercent.updateFunction = value =>
            {
                _centerOfGravityLeft = value;
                _centerOfGravityRight = value;
                SyncCenterOfGravity(_pectoralRbLeft, _centerOfGravityLeft);
                SyncCenterOfGravity(_pectoralRbRight, _centerOfGravityRight);
            };
            spring.updateFunction = value =>
            {
                _springLeft = value;
                _springRight = value;
                SyncJoint(_jointLeft, _pectoralRbLeft, _springLeft, _damperLeft);
                SyncJoint(_jointRight, _pectoralRbRight, _springRight, _damperRight);
            };
            damper.updateFunction = value =>
            {
                _damperLeft = value;
                _damperRight = value;
                SyncJoint(_jointLeft, _pectoralRbLeft, _springLeft, _damperLeft);
                SyncJoint(_jointRight, _pectoralRbRight, _springRight, _damperRight);
            };
            positionSpringZ.updateFunction = value =>
            {
                _positionSpringLeft = value;
                _positionSpringRight = value;
                SyncJointPositionZDrive(_jointLeft, _pectoralRbLeft, _positionSpringLeft, _positionDamperLeft);
                SyncJointPositionZDrive(_jointRight, _pectoralRbRight, _positionSpringRight, _positionDamperRight);
            };
            positionDamperZ.updateFunction = value =>
            {
                _positionDamperLeft = value;
                _positionDamperRight = value;
                SyncJointPositionZDrive(_jointLeft, _pectoralRbLeft, _positionSpringLeft, _positionDamperLeft);
                SyncJointPositionZDrive(_jointRight, _pectoralRbRight, _positionSpringRight, _positionDamperRight);
            };

            _mainPhysicsConfigs = new List<StaticPhysicsConfig>
            {
                centerOfGravityPercent,
                spring,
                damper,
                positionSpringZ,
                positionDamperZ,
            };
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

        // Reimplements AdjustJoints.cs method SyncJoint
        private void SyncJoint(ConfigurableJoint joint, Rigidbody rb, float spring, float damper)
        {
            // see AdjustJoints.cs method ScaleChanged
            float scalePow = Mathf.Pow(1.7f, _breastControl.scale - 1f);

            float scaledSpring = spring * scalePow;
            float scaledDamper = damper * scalePow;

            var slerpDrive = joint.slerpDrive;
            slerpDrive.positionSpring = scaledSpring;
            slerpDrive.positionDamper = scaledDamper;

            slerpDrive.maximumForce = DEFAULT_SLERP_MAX_FORCE * scalePow;
            joint.slerpDrive = slerpDrive;

            var angularXDrive = joint.angularXDrive;
            angularXDrive.positionSpring = scaledSpring;
            angularXDrive.positionDamper = scaledDamper;
            joint.angularXDrive = angularXDrive;

            var angularYZDrive = joint.angularYZDrive;
            angularYZDrive.positionSpring = scaledSpring;
            angularYZDrive.positionDamper = scaledDamper;
            joint.angularYZDrive = angularYZDrive;

            var angularXLimitSpring = joint.angularXLimitSpring;
            angularXLimitSpring.spring = scaledSpring * _breastControl.limitSpringMultiplier;
            angularXLimitSpring.damper = scaledDamper * _breastControl.limitDamperMultiplier;
            joint.angularXLimitSpring = angularXLimitSpring;

            var angularYZLimitSpring = joint.angularYZLimitSpring;
            angularYZLimitSpring.spring = scaledSpring * _breastControl.limitSpringMultiplier;
            angularYZLimitSpring.damper = scaledDamper * _breastControl.limitDamperMultiplier;
            joint.angularYZLimitSpring = angularYZLimitSpring;

            rb.WakeUp();
        }

        // Reimplements AdjustJoints.cs method SyncJointPositionZDrive
        private static void SyncJointPositionZDrive(ConfigurableJoint joint, Rigidbody rb, float spring, float damper)
        {
            var zDrive = joint.zDrive;
            zDrive.positionSpring = spring;
            zDrive.positionDamper = damper;
            joint.zDrive = zDrive;
            rb.WakeUp();
        }


        // Reimplements AdjustJoints.cs methods SyncTargetRotation and SetTargetRotation
        private void SyncTargetRotationLeft(float targetRotationX, float targetRotationY)
        {
            _breastControl.smoothedJoint2TargetRotation.x = targetRotationX;
            _breastControl.smoothedJoint2TargetRotation.y = targetRotationY;

            var dazBone = _jointLeft.GetComponent<DAZBone>();
            Vector3 rotation = _breastControl.smoothedJoint2TargetRotation;
            rotation.x = -rotation.x;
            dazBone.baseJointRotation = rotation;
        }

        // Reimplements AdjustJoints.cs methods SyncTargetRotation and SetTargetRotation
        // Circumvents default invertJoint2RotationX = true
        private void SyncTargetRotationRight(float targetRotationX, float targetRotationY)
        {
            _breastControl.smoothedJoint1TargetRotation.x = targetRotationX;
            _breastControl.smoothedJoint1TargetRotation.y = targetRotationY;

            var dazBone = _jointRight.GetComponent<DAZBone>();
            Vector3 rotation = _breastControl.smoothedJoint1TargetRotation;
            rotation.x = -rotation.x;
            dazBone.baseJointRotation = rotation;
        }

        public void UpdatePhysics(float softnessAmount, float quicknessAmount)
        {
            foreach(var config in _mainPhysicsConfigs)
            {
                config.updateFunction(NewValue(config, softnessAmount, quicknessAmount));
            }
        }

        public void UpdateRateDependentPhysics(float softnessAmount, float quicknessAmount)
        {
            foreach(var config in _mainPhysicsConfigs)
            {
                if(config.dependOnPhysicsRate)
                {
                    config.updateFunction(NewValue(config, softnessAmount, quicknessAmount));
                }
            }
        }

        // input mass, softness and quickness normalized to (0,1) range
        private float NewValue(StaticPhysicsConfig config, float softness, float quickness)
        {
            float result = config.Calculate(
                config.useRealMass ? realMassAmount : massAmount,
                softness,
                quickness
            );
            return config.dependOnPhysicsRate ? PhysicsRateMultiplier() * result : result;
        }

        // see UserPreferences.cs methods SetPhysics45, 60, 72 etc.
        private static float PhysicsRateMultiplier()
        {
            return 0.01666667f / Time.fixedDeltaTime;
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
            var json = new JSONClass();
            json["breastControlFloats"] = JSONArrayFromDictionary(_originalBreastControlFloats);
            json["pectoralRbLeftDetectCollisions"].AsBool = _originalPectoralRbLeftDetectCollisions;
            json["pectoralRbRightDetectCollisions"].AsBool = _originalPectoralRbRightDetectCollisions;
            return json;
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

        public void RestoreFromJSON(JSONClass originalPhysicsJSON)
        {
            var breastControlFloats = originalPhysicsJSON["breastControlFloats"].AsArray;
            foreach(JSONClass json in breastControlFloats)
            {
                _originalBreastControlFloats[json["paramName"].Value] = json["value"].AsFloat;
            }

            _originalPectoralRbLeftDetectCollisions = originalPhysicsJSON["pectoralRbLeftDetectCollisions"].AsBool;
            _originalPectoralRbRightDetectCollisions = originalPhysicsJSON["pectoralRbRightDetectCollisions"].AsBool;
        }
    }
}
