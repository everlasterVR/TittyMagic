using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TittyMagic.Configs;
using UnityEngine;
using static TittyMagic.ParamName;
using static TittyMagic.Side;

namespace TittyMagic
{
    internal class MainPhysicsHandler
    {
        private readonly Script _script;
        private readonly BreastVolumeCalculator _breastVolumeCalculator;
        private readonly AdjustJoints _breastControl;
        private readonly Dictionary<string, ConfigurableJoint> _joints;
        private readonly Dictionary<string, Rigidbody> _pectoralRbs;
        private readonly Dictionary<string, DAZBone> _dazBones;

        private readonly List<string> _breastControlFloatParamNames;

        // AdjustJoints.joint1.slerpDrive.maximumForce value logged on plugin Init
        private const float DEFAULT_SLERP_MAX_FORCE = 500;

        public Dictionary<string, PhysicsParameterGroup> parameterGroups { get; private set; }

        public float realMassAmount { get; private set; }
        public float massAmount { get; private set; }

        // hack. 1.5f because 3f is the max mass and massValue is actual mass / 2
        public static float InvertMass(float x) => 1.5f - x;
        public float normalizedMass => Mathf.InverseLerp(0, 1.45f, massAmount);
        public float normalizedRealMass => Mathf.InverseLerp(0, 1.50f, realMassAmount);
        public float normalizedInvertedMass => Mathf.InverseLerp(0, 1.45f, InvertMass(massAmount));

        public MassParameterGroup massParameterGroup { get; private set; }

        public MainPhysicsHandler(
            Script script,
            AdjustJoints breastControl,
            BreastVolumeCalculator breastVolumeCalculator
        )
        {
            _script = script;
            _breastControl = breastControl;
            _breastVolumeCalculator = breastVolumeCalculator;
            _joints = new Dictionary<string, ConfigurableJoint>
            {
                { LEFT, _breastControl.joint2 },
                { RIGHT, _breastControl.joint1 },
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
            };
        }

        public void UpdateMassValueAndAmounts()
        {
            float volume = _breastVolumeCalculator.Calculate(_script.atomScaleListener.scale);
            massParameterGroup.UpdateValue(volume);
            realMassAmount = massParameterGroup.left.baseValueJsf.val / 2;
            massAmount = massParameterGroup.left.valueJsf.val / 2;
        }

        public void LoadSettings()
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

        private MassParameter NewMassParameter(string side)
        {
            string jsfName = $"{MASS}{(side == LEFT ? "" : side)}";
            var valueJsf = new JSONStorableFloat($"{jsfName}Value", 0.100f, 0.100f, 3.000f);
            var parameter = new MassParameter(
                valueJsf,
                baseValueJsf: new JSONStorableFloat($"{jsfName}BaseValue", valueJsf.val, valueJsf.min, valueJsf.max),
                offsetJsf: _script.NewJSONStorableFloat($"{jsfName}Offset", 0, -valueJsf.max, valueJsf.max, register: side == LEFT)
            );
            parameter.valueFormat = "F3";
            parameter.sync = value => SyncMass(_pectoralRbs[side], value);
            return parameter;
        }

        private PhysicsParameter NewPhysicsParameter(string paramName, string side, float startingValue, float minValue, float maxValue)
        {
            string jsfName = $"{paramName}{(side == LEFT ? "" : side)}";
            var valueJsf = new JSONStorableFloat($"{jsfName}Value", startingValue, minValue, maxValue);
            return new PhysicsParameter(
                valueJsf,
                baseValueJsf: new JSONStorableFloat($"{jsfName}BaseValue", valueJsf.val, valueJsf.min, valueJsf.max),
                offsetJsf: _script.NewJSONStorableFloat($"{jsfName}Offset", 0, -valueJsf.max, valueJsf.max, register: side == LEFT)
            );
        }

        private PhysicsParameter NewCenterOfGravityParameter(string side)
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

        private PhysicsParameter NewSpringParameter(string side)
        {
            var parameter = NewPhysicsParameter(SPRING, side, 10, 10, 100);
            parameter.config = new StaticPhysicsConfig(
                70f,
                massCurve: x => 0.14f * x,
                // https://www.desmos.com/calculator/nxyosar9o6
                softnessCurve: x => -0.45f * Curves.InverseSmoothStep(x, 1.00f, 0.24f, 0.61f)
            );
            parameter.quicknessOffsetConfig = new StaticPhysicsConfig(40f);
            parameter.slownessOffsetConfig = new StaticPhysicsConfig(-20f);
            parameter.valueFormat = "F0";
            parameter.sync = value => SyncJointSpring(_joints[side], _pectoralRbs[side], value);
            return parameter;
        }

        private PhysicsParameter NewDamperParameter(string side)
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

        private PhysicsParameter NewPositionSpringZParameter(string side)
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

        private PhysicsParameter NewPositionDamperZParameter(string side)
        {
            var parameter = NewPhysicsParameter(POSITION_DAMPER_Z, side, 0, 0, 100);
            parameter.config = new StaticPhysicsConfig(8f);
            parameter.valueFormat = "F0";
            parameter.sync = value => SyncJointPositionZDriveDamper(_joints[side], _pectoralRbs[side], value);
            return parameter;
        }

        private PhysicsParameter NewTargetRotationYParameter(string side)
        {
            var parameter = NewPhysicsParameter(TARGET_ROTATION_Y, side, 0, -20.00f, 20.00f);
            parameter.config = new StaticPhysicsConfig(0.00f);
            parameter.valueFormat = "F2";
            parameter.sync = value => SyncTargetRotationY(side, value);
            return parameter;
        }

        private PhysicsParameter NewTargetRotationXParameter(string side)
        {
            var parameter = NewPhysicsParameter(TARGET_ROTATION_X, side, 0, -20.00f, 20.00f);
            parameter.config = new StaticPhysicsConfig(0.00f);
            parameter.valueFormat = "F2";
            parameter.sync = value => SyncTargetRotationX(side, value);
            return parameter;
        }

        private void SetupPhysicsParameterGroups()
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
                dependOnPhysicsRate = true,
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
                dependOnPhysicsRate = true,
            };

            var targetRotationY = new PhysicsParameterGroup(
                NewTargetRotationYParameter(LEFT),
                NewTargetRotationYParameter(RIGHT),
                "Right/Left Angle Target"
            )
            {
                requiresRecalibration = true,
                invertRight = true,
            };

            var targetRotationX = new PhysicsParameterGroup(
                NewTargetRotationXParameter(LEFT),
                NewTargetRotationXParameter(RIGHT),
                "Up/Down Angle Target"
            )
            {
                requiresRecalibration = true,
            };

            massParameterGroup.SetOffsetCallbackFunctions();
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

        #endregion *** Parameter setup ***

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
        private void SyncTargetRotationX(string side, float targetRotationX)
        {
            if(side == LEFT)
            {
                _breastControl.smoothedJoint2TargetRotation.x = targetRotationX;
                var rotation = _breastControl.smoothedJoint2TargetRotation;
                rotation.x = -rotation.x;
                _dazBones[side].baseJointRotation = rotation;
            }
            else if(side == RIGHT)
            {
                _breastControl.smoothedJoint1TargetRotation.x = targetRotationX;
                var rotation = _breastControl.smoothedJoint1TargetRotation;
                rotation.x = -rotation.x;
                _dazBones[side].baseJointRotation = rotation;
            }
        }

        // Reimplements AdjustJoints.cs methods SyncTargetRotation and SetTargetRotation
        private void SyncTargetRotationY(string side, float targetRotationY)
        {
            if(side == LEFT)
            {
                _breastControl.smoothedJoint2TargetRotation.y = -targetRotationY;
                var rotation = _breastControl.smoothedJoint2TargetRotation;
                _dazBones[side].baseJointRotation = rotation;
            }
            else if(side == RIGHT)
            {
                _breastControl.smoothedJoint1TargetRotation.y = -targetRotationY;
                var rotation = _breastControl.smoothedJoint1TargetRotation;
                _dazBones[side].baseJointRotation = rotation;
            }
        }

        #endregion *** Sync functions ***

        public void UpdatePhysics()
        {
            float softness = _script.softnessAmount;
            float quickness = _script.quicknessAmount;
            parameterGroups.Values
                .ToList()
                .ForEach(paramGroup => UpdateParam(paramGroup, softness, quickness));
        }

        public void UpdateRateDependentPhysics()
        {
            float softness = _script.softnessAmount;
            float quickness = _script.quicknessAmount;
            parameterGroups.Values
                .Where(paramGroup => paramGroup.dependOnPhysicsRate)
                .ToList()
                .ForEach(paramGroup => UpdateParam(paramGroup, softness, quickness));
        }

        private void UpdateParam(PhysicsParameterGroup paramGroup, float softness, float quickness)
        {
            float massValue = paramGroup.useRealMass ? realMassAmount : massAmount;
            paramGroup.UpdateValue(massValue, softness, quickness);
        }

        public void RestoreOriginalPhysics()
        {
            foreach(string name in _breastControlFloatParamNames)
            {
                /* Set a value that is different from the original, then restore the original
                 * in order to trigger VaM's internal sync
                 */
                var paramJsf = _breastControl.GetFloatJSONParam(name);
                float original = paramJsf.val;
                paramJsf.valNoCallback = Math.Abs(paramJsf.val - paramJsf.min) > 0.01f
                    ? paramJsf.min
                    : paramJsf.max;
                paramJsf.val = original;
            }
        }

        private static Dictionary<string, string> CreateInfoTexts()
        {
            Func<string> massText = () =>
            {
                var sb = new StringBuilder();
                sb.Append("Mass of the pectoral joint.");
                sb.Append("\n\n");
                sb.Append("Since mass represents breast size, other physics parameters are adjusted based on its value.");
                if(Gender.isFemale)
                {
                    sb.Append("\n\n");
                    sb.Append("Fat Collider Radius and Fat Distance Limit are adjusted using the mass estimated from");
                    sb.Append(" volume, the rest are adjusted using the actual mass value that includes the offset.");
                }

                return sb.ToString();
            };

            Func<string> centerOfGravityText = () =>
            {
                var sb = new StringBuilder();
                sb.Append("Position of the pectoral joint's center of mass.");
                sb.Append("\n\n");
                sb.Append("At 0, the center of mass is inside the chest at the pectoral joint. At 1, it is at the nipple.");
                sb.Append(" Between about 0.5 and 0.8, the center of mass is within the bulk of the breast volume.");
                return sb.ToString();
            };

            Func<string> springText = () =>
            {
                var sb = new StringBuilder();
                sb.Append("Magnitude of the spring that pushes the pectoral joint towards its angle target.");
                sb.Append("\n\n");
                sb.Append("The angle target is defined by the Up/Down and Left/Right Angle Target parameters.");
                return sb.ToString();
            };

            Func<string> damperText = () =>
            {
                var sb = new StringBuilder();
                sb.Append("Magnitude of the damper that reduces oscillation around the joint angle target.");
                sb.Append("\n\n");
                sb.Append("The higher the damper, the quicker breasts will stop swinging.");
                return sb.ToString();
            };

            Func<string> positionSpringZText = () =>
            {
                var sb = new StringBuilder();
                sb.Append("Magnitude of the spring that pushes the pectoral joint towards its position target along the Z axis.");
                sb.Append("\n\n");
                sb.Append("Directional force morphing along the forward-back axis depends on In/Out Spring being suitably low");
                sb.Append(" for the given breast mass.");
                return sb.ToString();
            };

            Func<string> positionDamperZText = () =>
            {
                var sb = new StringBuilder();
                sb.Append("Magnitude of the damper that reduces oscillation around the joint position target along the Z axis.");
                return sb.ToString();
            };

            Func<string> targetRotationXText = () =>
            {
                var sb = new StringBuilder();
                sb.Append("Vertical target angle of the pectoral joint.");
                sb.Append(" Negative values pull breasts down, positive values push them up.");
                sb.Append("\n\n");
                sb.Append("The offset shifts the center around which the final angle is calculated");
                sb.Append(" based on chest angle (see Gravity Multipliers)");
                return sb.ToString();
            };

            Func<string> targetRotationYText = () =>
            {
                var sb = new StringBuilder();
                sb.Append("Horizontal target angle of the pectoral joint.");
                sb.Append("\n\n");
                sb.Append("A negative offset pulls breasts apart, while a positive offset pushes them together.");
                return sb.ToString();
            };

            return new Dictionary<string, string>()
            {
                { MASS, massText() },
                { CENTER_OF_GRAVITY_PERCENT, centerOfGravityText() },
                { SPRING, springText() },
                { DAMPER, damperText() },
                { POSITION_SPRING_Z, positionSpringZText() },
                { POSITION_DAMPER_Z, positionDamperZText() },
                { TARGET_ROTATION_X, targetRotationXText() },
                { TARGET_ROTATION_Y, targetRotationYText() },
            };
        }
    }
}
