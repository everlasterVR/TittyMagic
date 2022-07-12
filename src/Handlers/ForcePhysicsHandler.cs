using System.Collections.Generic;
using TittyMagic.Configs;
using UnityEngine;
using static TittyMagic.ParamName;
using static TittyMagic.GravityEffectCalc;

namespace TittyMagic
{
    internal class ForcePhysicsHandler
    {
        private readonly Script _script;
        private readonly MainPhysicsHandler _mainPhysicsHandler;
        private readonly SoftPhysicsHandler _softPhysicsHandler;

        private List<PhysicsParameterGroup> _mainParamGroups;
        private List<PhysicsParameterGroup> _softParamGroups;

        private readonly TrackNipple _trackLeftNipple;
        private readonly TrackNipple _trackRightNipple;

        private float _pitchMultiplier;
        private float _rollMultiplier;

        public JSONStorableFloat yMultiplierJsf { get; }
        public JSONStorableFloat zMultiplierJsf { get; }
        public JSONStorableFloat xMultiplierJsf { get; }

        public ForcePhysicsHandler(
            Script script,
            TrackNipple trackLeftNipple,
            TrackNipple trackRightNipple
        )
        {
            _script = script;
            _mainPhysicsHandler = _script.mainPhysicsHandler;
            _softPhysicsHandler = _script.softPhysicsHandler;
            _trackLeftNipple = trackLeftNipple;
            _trackRightNipple = trackRightNipple;

            yMultiplierJsf = _script.NewJSONStorableFloat("forcePhysicsUpDown", 1.00f, 0.00f, 2.00f);
            zMultiplierJsf = _script.NewJSONStorableFloat("forcePhysicsForwardBack", 1.00f, 0.00f, 2.00f);
            xMultiplierJsf = _script.NewJSONStorableFloat("forcePhysicsLeftRight", 1.00f, 0.00f, 2.00f);
        }

        public void LoadSettings()
        {
            SetupMainForcePhysicsConfigs();
            SetupSoftForcePhysicsConfigs();

            _mainParamGroups = _mainPhysicsHandler.parameterGroups.Values.ToList();
            _softParamGroups = _softPhysicsHandler.parameterGroups.Values.ToList();
        }

        private static Dictionary<string, DynamicPhysicsConfig> NewCenterOfGravityConfigs() =>
            new Dictionary<string, DynamicPhysicsConfig>
            {
            };

        private static Dictionary<string, DynamicPhysicsConfig> NewSpringConfigs() =>
            new Dictionary<string, DynamicPhysicsConfig>
            {
            };

        private static Dictionary<string, DynamicPhysicsConfig> NewDamperConfigs() =>
            new Dictionary<string, DynamicPhysicsConfig>
            {
            };

        private static Dictionary<string, DynamicPhysicsConfig> NewPositionSpringZConfigs() =>
            new Dictionary<string, DynamicPhysicsConfig>
            {
            };

        private static Dictionary<string, DynamicPhysicsConfig> NewPositionDamperZConfigs() =>
            new Dictionary<string, DynamicPhysicsConfig>
            {
            };

        private static Dictionary<string, DynamicPhysicsConfig> NewPositionTargetRotationXConfigs() =>
            new Dictionary<string, DynamicPhysicsConfig>
            {
            };

        private static Dictionary<string, DynamicPhysicsConfig> NewPositionTargetRotationYConfigs() =>
            new Dictionary<string, DynamicPhysicsConfig>
            {
            };

        private void SetupMainForcePhysicsConfigs()
        {
            var paramGroups = _script.mainPhysicsHandler.parameterGroups;
            paramGroups[CENTER_OF_GRAVITY_PERCENT].SetForcePhysicsConfigs(NewCenterOfGravityConfigs(), NewCenterOfGravityConfigs());
            paramGroups[SPRING].SetForcePhysicsConfigs(NewSpringConfigs(), NewSpringConfigs());
            paramGroups[DAMPER].SetForcePhysicsConfigs(NewDamperConfigs(), NewDamperConfigs());
            paramGroups[POSITION_SPRING_Z].SetForcePhysicsConfigs(NewPositionSpringZConfigs(), NewPositionSpringZConfigs());
            paramGroups[POSITION_DAMPER_Z].SetForcePhysicsConfigs(NewPositionDamperZConfigs(), NewPositionDamperZConfigs());
            paramGroups[TARGET_ROTATION_X].SetForcePhysicsConfigs(NewPositionTargetRotationXConfigs(), NewPositionTargetRotationXConfigs());
            paramGroups[TARGET_ROTATION_Y].SetForcePhysicsConfigs(NewPositionTargetRotationYConfigs(), NewPositionTargetRotationYConfigs());
        }

        private void SetupSoftForcePhysicsConfigs()
        {
            var paramGroups = _script.softPhysicsHandler.parameterGroups;
            //TODO
        }

        public void Update(float roll, float pitch)
        {
            _rollMultiplier = 1;
            _pitchMultiplier = 1;

            AdjustUpDownPhysics();
            AdjustDepthPhysics();
            AdjustLeftRightPhysics();
        }

        private void AdjustUpDownPhysics()
        {
            // float multiplier = yMultiplier.mainMultiplier * (yMultiplier.extraMultiplier ?? 1);
            float multiplier = 1;
            float effectYLeft = CalculateYEffect(_trackLeftNipple.angleY, multiplier);
            float effectYRight = CalculateYEffect(_trackRightNipple.angleY, multiplier);

            // up force on left breast
            if(_trackLeftNipple.angleY >= 0)
            {
                UpdatePhysics(Direction.UP_L, effectYLeft);
            }
            // down force on left breast
            else
            {
                UpdatePhysics(Direction.DOWN_L, effectYLeft);
            }

            // // up force on right breast
            if(_trackRightNipple.angleY >= 0)
            {
                UpdatePhysics(Direction.UP_R, effectYRight);
            }
            // down force on right breast
            else
            {
                UpdatePhysics(Direction.DOWN_R, effectYLeft);
            }
        }

        private void AdjustDepthPhysics()
        {
            // float forwardMultiplier = zMultiplier.mainMultiplier * (zMultiplier.extraMultiplier ?? 1);
            // float backMultiplier = zMultiplier.mainMultiplier * (zMultiplier.oppositeExtraMultiplier ?? 1);

            float forwardMultiplier = 1;
            float backMultiplier = 1;

            float leftMultiplier = _trackLeftNipple.depthDiff < 0 ? forwardMultiplier : backMultiplier;
            float rightMultiplier = _trackRightNipple.depthDiff < 0 ? forwardMultiplier : backMultiplier;

            float effectZLeft = CalculateZEffect(_trackLeftNipple.depthDiff, leftMultiplier);
            float effectZRight = CalculateZEffect(_trackRightNipple.depthDiff, rightMultiplier);

            // forward force on left breast
            if(_trackLeftNipple.depthDiff <= 0)
            {
                UpdatePhysics(Direction.FORWARD_L, effectZLeft);
            }
            // back force on left breast
            else
            {
                UpdatePhysics(Direction.BACK_L, effectZLeft);
            }

            // forward force on right breast
            if(_trackRightNipple.depthDiff <= 0)
            {
                UpdatePhysics(Direction.FORWARD_R, effectZRight);
            }
            // back force on right breast
            else
            {
                UpdatePhysics(Direction.BACK_R, effectZRight);
            }
        }

        private void AdjustLeftRightPhysics()
        {
            // float multiplier = xMultiplier.mainMultiplier * (xMultiplier.extraMultiplier ?? 1);
            float multiplier = 1;
            float effectXLeft = CalculateXEffect(_trackLeftNipple.angleX, multiplier);
            float effectXRight = CalculateXEffect(_trackRightNipple.angleX, multiplier);

            // left force on left breast
            if(_trackLeftNipple.angleX >= 0)
            {
                UpdatePhysics(Direction.RIGHT_L, effectXLeft);
            }
            // right force on left breast
            else
            {
                UpdatePhysics(Direction.LEFT_L, effectXLeft);
            }

            // // left force on right breast
            if(_trackRightNipple.angleX >= 0)
            {
                UpdatePhysics(Direction.RIGHT_R, effectXRight);
            }
            // right force on right breast
            else
            {
                UpdatePhysics(Direction.LEFT_R, effectXRight);
            }
        }

        private static float CalculatePitchMultiplier(float pitch, float roll) =>
            Mathf.Lerp(0.72f, 1f, CalculateDiffFromHorizontal(pitch, roll));

        private static float CalculateRollMultiplier(float roll) =>
            Mathf.Lerp(1.25f, 1f, Mathf.Abs(roll));

        private float CalculateYEffect(float angle, float multiplier) =>
            multiplier * _pitchMultiplier * Mathf.Abs(angle) / 10;

        private float CalculateXEffect(float angle, float multiplier) =>
            multiplier * _rollMultiplier * Mathf.Abs(angle) / 30;
        // multiplier * Curve(_pitchMultiplier * Mathf.Abs(angle) / 75);

        private static float CalculateZEffect(float distance, float multiplier) =>
            multiplier * Mathf.Abs(distance) * 12;
        // multiplier * Curve(_rollMultiplier * Mathf.Abs(angle) / 60);

        // return multiplier * Curve(Mathf.Abs(distance) * 12);
        private static float Curve(float effect) => Calc.InverseSmoothStep(effect, 10, 0.8f, 0f);

        private void UpdatePhysics(string direction, float effect)
        {
            float mass = _script.mainPhysicsHandler.realMassAmount;
            _mainParamGroups.ForEach(paramGroup =>
                paramGroup.UpdateForceValue(direction, effect, mass));

            // _softParamGroups.ForEach(paramGroup =>
            //     paramGroup.UpdateForceValue(direction, effect, mass));
        }
    }
}
