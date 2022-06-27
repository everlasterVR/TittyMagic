using System.Collections.Generic;
using TittyMagic.Configs;
using UnityEngine;
using static TittyMagic.MVRParamName;
using static TittyMagic.GravityEffectCalc;

namespace TittyMagic
{
    internal class ForcePhysicsHandler
    {
        private readonly MainPhysicsHandler _mainPhysicsHandler;
        private readonly SoftPhysicsHandler _softPhysicsHandler;

        private List<PhysicsParameter> _leftBreastMainParams;
        private List<PhysicsParameter> _rightBreastMainParams;
        private List<PhysicsParameter> _leftBreastSoftParams;
        private List<PhysicsParameter> _rightBreastSoftParams;

        private readonly TrackNipple _trackLeftNipple;
        private readonly TrackNipple _trackRightNipple;

        private const float SOFTNESS = 0.62f;
        private float _mass;
        private float _pitchMultiplier;
        private float _rollMultiplier;

        public Multiplier xMultiplier { get; }
        public Multiplier yMultiplier { get; }
        public Multiplier zMultiplier { get; }

        public ForcePhysicsHandler(
            MainPhysicsHandler mainPhysicsHandler,
            SoftPhysicsHandler softPhysicsHandler,
            TrackNipple trackLeftNipple,
            TrackNipple trackRightNipple
        )
        {
            _mainPhysicsHandler = mainPhysicsHandler;
            _softPhysicsHandler = softPhysicsHandler;
            _trackLeftNipple = trackLeftNipple;
            _trackRightNipple = trackRightNipple;
            xMultiplier = new Multiplier();
            yMultiplier = new Multiplier();
            zMultiplier = new Multiplier();
        }

        public void LoadSettings()
        {
            SetupMainForcePhysicsConfigs(_mainPhysicsHandler.leftBreastParameters);
            SetupMainForcePhysicsConfigs(_mainPhysicsHandler.rightBreastParameters);
            _leftBreastMainParams = _mainPhysicsHandler.leftBreastParameters.Values.ToList();
            _rightBreastMainParams = _mainPhysicsHandler.rightBreastParameters.Values.ToList();

            SetupSoftForcePhysicsConfigs(_softPhysicsHandler.leftBreastParameters);
            SetupSoftForcePhysicsConfigs(_softPhysicsHandler.rightBreastParameters);
            _leftBreastSoftParams = _softPhysicsHandler.leftBreastParameters.Values.ToList();
            _rightBreastSoftParams = _softPhysicsHandler.rightBreastParameters.Values.ToList();
        }

        private static void SetupMainForcePhysicsConfigs(Dictionary<string, PhysicsParameter> parameters)
        {
            parameters[CENTER_OF_GRAVITY_PERCENT].forcePhysicsConfigs = new Dictionary<string, DynamicPhysicsConfig>
            {
                { Direction.DOWN, null },
                { Direction.UP, null },
                { Direction.BACK, null },
                { Direction.FORWARD, null },
                { Direction.LEFT, null },
                { Direction.RIGHT, null },
            };

            parameters[SPRING].forcePhysicsConfigs = new Dictionary<string, DynamicPhysicsConfig>
            {
                { Direction.DOWN, null },
                { Direction.UP, null },
                { Direction.BACK, null },
                { Direction.FORWARD, null },
                { Direction.LEFT, null },
                { Direction.RIGHT, null },
            };

            parameters[DAMPER].forcePhysicsConfigs = new Dictionary<string, DynamicPhysicsConfig>
            {
                { Direction.DOWN, null },
                { Direction.UP, null },
                { Direction.BACK, null },
                { Direction.FORWARD, null },
                { Direction.LEFT, null },
                { Direction.RIGHT, null },
            };

            parameters[POSITION_SPRING_Z].forcePhysicsConfigs = new Dictionary<string, DynamicPhysicsConfig>
            {
                { Direction.DOWN, null },
                { Direction.UP, null },
                { Direction.BACK, null },
                { Direction.FORWARD, null },
                { Direction.LEFT, null },
                { Direction.RIGHT, null },
            };

            parameters[POSITION_DAMPER_Z].forcePhysicsConfigs = new Dictionary<string, DynamicPhysicsConfig>
            {
                { Direction.DOWN, null },
                { Direction.UP, null },
                { Direction.BACK, null },
                { Direction.FORWARD, null },
                { Direction.LEFT, null },
                { Direction.RIGHT, null },
            };

            parameters[TARGET_ROTATION_X].forcePhysicsConfigs = new Dictionary<string, DynamicPhysicsConfig>
            {
                { Direction.DOWN, null },
                { Direction.UP, null },
                { Direction.BACK, null },
                { Direction.FORWARD, null },
                { Direction.LEFT, null },
                { Direction.RIGHT, null },
            };

            parameters[TARGET_ROTATION_Y].forcePhysicsConfigs = new Dictionary<string, DynamicPhysicsConfig>
            {
                { Direction.DOWN, null },
                { Direction.UP, null },
                { Direction.BACK, null },
                { Direction.FORWARD, null },
                { Direction.LEFT, null },
                { Direction.RIGHT, null },
            };
        }

        private static void SetupSoftForcePhysicsConfigs(Dictionary<string, PhysicsParameter> parameters)
        {
            //TODO
        }

        public void Update(
            float roll,
            float pitch,
            float mass
        )
        {
            _rollMultiplier = 1;
            _pitchMultiplier = 1;
            _mass = mass;

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

        private static float CalculatePitchMultiplier(float pitch, float roll)
        {
            return Mathf.Lerp(0.72f, 1f, CalculateDiffFromHorizontal(pitch, roll));
        }

        private static float CalculateRollMultiplier(float roll)
        {
            return Mathf.Lerp(1.25f, 1f, Mathf.Abs(roll));
        }

        private float CalculateYEffect(float angle, float multiplier)
        {
            return multiplier * _pitchMultiplier * Mathf.Abs(angle) / 10;
            // return multiplier * Curve(_pitchMultiplier * Mathf.Abs(angle) / 75);
        }

        private float CalculateXEffect(float angle, float multiplier)
        {
            return multiplier * _rollMultiplier * Mathf.Abs(angle) / 30;
            // return multiplier * Curve(_rollMultiplier * Mathf.Abs(angle) / 60);
        }

        private static float CalculateZEffect(float distance, float multiplier)
        {
            return multiplier * Mathf.Abs(distance) * 12;
            // return multiplier * Curve(Mathf.Abs(distance) * 12);
        }

        private static float Curve(float effect)
        {
            return Calc.InverseSmoothStep(effect, 10, 0.8f, 0f);
        }

        private void UpdatePhysics(string direction, float effect)
        {
            _leftBreastMainParams.ForEach(param => UpdateParam(param, direction, effect));
            _rightBreastMainParams.ForEach(param => UpdateParam(param, direction, effect));
            // _leftBreastSoftParams.ForEach(param => UpdateParam(param, direction, effect));
            // _rightBreastSoftParams.ForEach(param => UpdateParam(param, direction, effect));
        }

        private float NewValue(DynamicPhysicsConfig config, float effect)
        {
            float value = CalculateValue(config, effect);
            bool inRange = config.isNegative ? value < 0 : value > 0;
            return inRange ? value : 0;
        }

        private void UpdateParam(PhysicsParameter param, string direction, float effect)
        {
            if(!param.forcePhysicsConfigs.ContainsKey(direction))
                return;

            var config = param.forcePhysicsConfigs[direction];
            if(config != null)
            {
                float value = NewValue(config, effect);
                param.AddValue(value);
            }
        }

        private float CalculateValue(DynamicPhysicsConfig config, float effect)
        {
            float mass = config.multiplyInvertedMass ? 1 - _mass : _mass;
            return
                SOFTNESS * config.softnessMultiplier * effect +
                mass * config.massMultiplier * effect;
        }
    }
}
