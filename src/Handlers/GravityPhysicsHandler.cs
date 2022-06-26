// ReSharper disable RedundantUsingDirective
using System.Collections.Generic;
using static TittyMagic.Utils;
using static TittyMagic.GravityEffectCalc;

namespace TittyMagic
{
    internal class GravityPhysicsHandler
    {
        private readonly MainPhysicsHandler _mainPhysicsHandler;

        private float _mass;
        private float _softness;

        public Multiplier xMultiplier { get; }
        public Multiplier yMultiplier { get; }
        public Multiplier zMultiplier { get; }

        private Dictionary<string, List<Config>> _configSets;

        public GravityPhysicsHandler(MainPhysicsHandler mainPhysicsHandler)
        {
            _mainPhysicsHandler = mainPhysicsHandler;
            xMultiplier = new Multiplier();
            yMultiplier = new Multiplier();
            zMultiplier = new Multiplier();
        }

        public void LoadSettings()
        {
            _configSets = new Dictionary<string, List<Config>>
            {
                { Direction.DOWN, DownConfigs() },
                { Direction.UP, UpConfigs() },
                { Direction.BACK, BackConfigs() },
                { Direction.FORWARD, ForwardConfigs() },
                { Direction.LEFT, LeftConfigs() },
                { Direction.RIGHT, RightConfigs() },
            };
        }

        private List<Config> DownConfigs()
        {
            var targetRotationX = new GravityPhysicsConfig(-16f, -12f, isNegative: true);

            targetRotationX.updateFunction = value =>
            {
                float newValue = NewValue(targetRotationX, value);
                _mainPhysicsHandler.SetTargetRotationXLeft(newValue);
                _mainPhysicsHandler.SetTargetRotationXRight(newValue);
            };

            return new List<Config>
            {
                targetRotationX,
            };
        }

        private List<Config> UpConfigs()
        {
            var targetRotationX = new GravityPhysicsConfig(10.7f, 8f);

            targetRotationX.updateFunction = value =>
            {
                float newValue = NewValue(targetRotationX, value);
                _mainPhysicsHandler.SetTargetRotationXLeft(newValue);
                _mainPhysicsHandler.SetTargetRotationXRight(newValue);
            };

            return new List<Config>
            {
                targetRotationX,
            };
        }

        private List<Config> BackConfigs()
        {
            var centerOfGravityPercent = new GravityPhysicsConfig(-0.071f, -0.053f, isNegative: true, multiplyInvertedMass: true);
            var spring = new GravityPhysicsConfig(-7.0f, -5.3f, isNegative: true);
            var damper = new GravityPhysicsConfig(-0.27f, -0.36f, isNegative: true);
            var positionSpringZ = new GravityPhysicsConfig(-180f, -140f, isNegative: true);
            var positionDamperZ = new GravityPhysicsConfig(-15f, 5f, isNegative: true, multiplyInvertedMass: true);

            centerOfGravityPercent.updateFunction = value =>
            {
                float newValue = NewValue(centerOfGravityPercent, value);
                _mainPhysicsHandler.AddToLeftCenterOfGravity(newValue);
                _mainPhysicsHandler.AddToRightCenterOfGravity(newValue);
            };
            spring.updateFunction = value =>
            {
                float newValue = NewValue(spring, value);
                _mainPhysicsHandler.AddToLeftJointSpring(newValue);
                _mainPhysicsHandler.AddToRightJointSpring(newValue);
            };
            damper.updateFunction = value =>
            {
                float newValue = NewValue(damper, value);
                _mainPhysicsHandler.AddToLeftJointDamper(newValue);
                _mainPhysicsHandler.AddToRightJointDamper(newValue);
            };
            positionSpringZ.updateFunction = value =>
            {
                float newValue = NewValue(positionSpringZ, value);
                _mainPhysicsHandler.AddToLeftJointPositionSpringZ(newValue);
                _mainPhysicsHandler.AddToRightJointPositionSpringZ(newValue);
            };
            positionDamperZ.updateFunction = value =>
            {
                float newValue = NewValue(positionDamperZ, value);
                _mainPhysicsHandler.AddToLeftJointPositionDamperZ(newValue);
                _mainPhysicsHandler.AddToRightJointPositionDamperZ(newValue);
            };

            return new List<Config>
            {
                centerOfGravityPercent,
                spring,
                damper,
                positionSpringZ,
                positionDamperZ,
            };
        }

        private List<Config> ForwardConfigs()
        {
            var centerOfGravityPercent = new GravityPhysicsConfig(0.141f, 0.106f, isNegative: false, multiplyInvertedMass: true);
            var spring = new GravityPhysicsConfig(-7.0f, -5.3f, isNegative: true);
            var damper = new GravityPhysicsConfig(-0.27f, -0.36f, isNegative: true);
            var positionSpringZ = new GravityPhysicsConfig(-180f, -140f, isNegative: true);

            centerOfGravityPercent.updateFunction = value =>
            {
                float newValue = NewValue(centerOfGravityPercent, value);
                _mainPhysicsHandler.AddToLeftCenterOfGravity(newValue);
                _mainPhysicsHandler.AddToRightCenterOfGravity(newValue);
            };
            spring.updateFunction = value =>
            {
                float newValue = NewValue(spring, value);
                _mainPhysicsHandler.AddToLeftJointSpring(newValue);
                _mainPhysicsHandler.AddToRightJointSpring(newValue);
            };
            damper.updateFunction = value =>
            {
                float newValue = NewValue(damper, value);
                _mainPhysicsHandler.AddToLeftJointDamper(newValue);
                _mainPhysicsHandler.AddToRightJointDamper(newValue);
            };
            positionSpringZ.updateFunction = value =>
            {
                float newValue = NewValue(positionSpringZ, value);
                _mainPhysicsHandler.AddToLeftJointPositionSpringZ(newValue);
                _mainPhysicsHandler.AddToRightJointPositionSpringZ(newValue);
            };

            return new List<Config>
            {
                centerOfGravityPercent,
                spring,
                damper,
                positionSpringZ,
            };
        }

        private List<Config> LeftConfigs()
        {
            var targetRotationY = new GravityPhysicsConfig(16f, 12f);

            targetRotationY.updateFunction = value =>
            {
                float newValue = NewValue(targetRotationY, value);
                _mainPhysicsHandler.SetTargetRotationYLeft(newValue);
                _mainPhysicsHandler.SetTargetRotationYRight(newValue);
            };

            return new List<Config>
            {
                targetRotationY,
            };
        }

        private List<Config> RightConfigs()
        {
            var targetRotationY = new GravityPhysicsConfig(-16f, -12f, isNegative: true);

            targetRotationY.updateFunction = value =>
            {
                float newValue = NewValue(targetRotationY, value);
                _mainPhysicsHandler.SetTargetRotationYLeft(newValue);
                _mainPhysicsHandler.SetTargetRotationYRight(newValue);
            };

            return new List<Config>
            {
                targetRotationY,
            };
        }

        public void Update(
            float roll,
            float pitch,
            float mass,
            float amount
        )
        {
            _mass = mass;
            _softness = amount;

            float smoothRoll = Calc.SmoothStep(roll);
            float smoothPitch = 2 * Calc.SmoothStep(pitch);

            AdjustRollPhysics(smoothRoll);
            AdjustUpDownPhysics(smoothPitch, smoothRoll);
            AdjustDepthPhysics(smoothPitch, smoothRoll);
        }

        private void AdjustRollPhysics(float roll)
        {
            float effect = CalculateRollEffect(roll, xMultiplier);
            // left
            if(roll >= 0)
            {
                UpdatePhysics(Direction.LEFT, effect);
            }
            // right
            else
            {
                UpdatePhysics(Direction.RIGHT, effect);
            }
        }

        private void AdjustUpDownPhysics(float pitch, float roll)
        {
            float effect = CalculateUpDownEffect(pitch, roll, yMultiplier);
            // leaning forward
            if(pitch >= 0)
            {
                // upright
                if(pitch < 1)
                {
                    UpdatePhysics(Direction.DOWN, effect);
                }
                // upside down
                else
                {
                    UpdatePhysics(Direction.UP, effect);
                }
            }
            // leaning back
            else
            {
                // upright
                if(pitch >= -1)
                {
                    UpdatePhysics(Direction.DOWN, effect);
                }
                // upside down
                else
                {
                    UpdatePhysics(Direction.UP, effect);
                }
            }
        }

        private void AdjustDepthPhysics(float pitch, float roll)
        {
            float effect = CalculateDepthEffect(pitch, roll, zMultiplier);
            // leaning forward
            if(pitch >= 0)
            {
                // upright
                if(pitch < 1)
                {
                    UpdatePhysics(Direction.FORWARD, effect);
                }
                // upside down
                else
                {
                    UpdatePhysics(Direction.FORWARD, effect);
                }
            }
            // leaning back
            else
            {
                // upright
                if(pitch >= -1)
                {
                    UpdatePhysics(Direction.BACK, effect);
                }
                // upside down
                else
                {
                    UpdatePhysics(Direction.BACK, effect);
                }
            }
        }

        private void UpdatePhysics(string configSetName, float effect)
        {
            foreach(var config in _configSets[configSetName])
            {
                var gravityPhysicsConfig = (GravityPhysicsConfig) config;
                gravityPhysicsConfig.updateFunction(effect);
            }
        }

        private float NewValue(GravityPhysicsConfig config, float effect)
        {
            float value = CalculateValue(config, effect);
            bool inRange = config.isNegative ? value < 0 : value > 0;
            return inRange ? value : 0;
        }

        private float CalculateValue(GravityPhysicsConfig config, float effect)
        {
            float mass = config.multiplyInvertedMass ? 1 - _mass : _mass;
            return
                _softness * config.softnessMultiplier * effect +
                mass * config.massMultiplier * effect;
        }
    }
}
