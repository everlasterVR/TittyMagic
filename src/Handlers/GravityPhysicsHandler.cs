using System.Collections.Generic;
using TittyMagic.Configs;
using static TittyMagic.MVRParamName;
using static TittyMagic.GravityEffectCalc;

namespace TittyMagic
{
    internal class GravityPhysicsHandler
    {
        private readonly Script _script;
        private List<PhysicsParameter> _leftBreastParams;
        private List<PhysicsParameter> _rightBreastParams;

        private float _mass;
        private float _softness;

        public JSONStorableFloat xMultiplierJsf { get; }
        public JSONStorableFloat yMultiplierJsf { get; }
        public JSONStorableFloat zMultiplierJsf { get; }

        public Multiplier xMultiplier { get; }
        public Multiplier yMultiplier { get; }
        public Multiplier zMultiplier { get; }

        public GravityPhysicsHandler(Script script)
        {
            _script = script;

            xMultiplierJsf = script.NewJSONStorableFloat("gravityPhysicsLeftRight", 1.00f, 0.00f, 2.00f);
            yMultiplierJsf = script.NewJSONStorableFloat("gravityPhysicsUpDown", 1.00f, 0.00f, 2.00f);
            zMultiplierJsf = script.NewJSONStorableFloat("gravityPhysicsForwardBack", 1.00f, 0.00f, 2.00f);

            xMultiplier = new Multiplier();
            yMultiplier = new Multiplier();
            zMultiplier = new Multiplier();

            xMultiplier.mainMultiplier = xMultiplierJsf.val;
            yMultiplier.mainMultiplier = yMultiplierJsf.val;
            zMultiplier.mainMultiplier = zMultiplierJsf.val;
        }

        public void LoadSettings()
        {
            var left = _script.mainPhysicsHandler.leftBreastParameters;
            var right = _script.mainPhysicsHandler.rightBreastParameters;
            SetupGravityPhysicsConfigs(left);
            SetupGravityPhysicsConfigs(right);
            _leftBreastParams = left.Values.ToList();
            _rightBreastParams = right.Values.ToList();
        }

        private static void SetupGravityPhysicsConfigs(Dictionary<string, PhysicsParameter> parameters)
        {
            parameters[CENTER_OF_GRAVITY_PERCENT].gravityPhysicsConfigs = new Dictionary<string, DynamicPhysicsConfig>
            {
                { Direction.DOWN, null },
                { Direction.UP, null },
                { Direction.BACK, new DynamicPhysicsConfig(-0.071f, -0.053f, isNegative: true, multiplyInvertedMass: true) },
                { Direction.FORWARD, new DynamicPhysicsConfig(0.141f, 0.106f, isNegative: false, multiplyInvertedMass: true) },
                { Direction.LEFT, null },
                { Direction.RIGHT, null },
            };

            parameters[SPRING].gravityPhysicsConfigs = new Dictionary<string, DynamicPhysicsConfig>
            {
                { Direction.DOWN, null },
                { Direction.UP, null },
                { Direction.BACK, new DynamicPhysicsConfig(-7.0f, -5.3f, isNegative: true) },
                { Direction.FORWARD, new DynamicPhysicsConfig(-7.0f, -5.3f, isNegative: true) },
                { Direction.LEFT, null },
                { Direction.RIGHT, null },
            };

            parameters[DAMPER].gravityPhysicsConfigs = new Dictionary<string, DynamicPhysicsConfig>
            {
                { Direction.DOWN, null },
                { Direction.UP, null },
                { Direction.BACK, new DynamicPhysicsConfig(-0.27f, -0.36f, isNegative: true) },
                { Direction.FORWARD, new DynamicPhysicsConfig(-0.27f, -0.36f, isNegative: true) },
                { Direction.LEFT, null },
                { Direction.RIGHT, null },
            };

            parameters[POSITION_SPRING_Z].gravityPhysicsConfigs = new Dictionary<string, DynamicPhysicsConfig>
            {
                { Direction.DOWN, null },
                { Direction.UP, null },
                { Direction.BACK, new DynamicPhysicsConfig(-180f, -140f, isNegative: true) },
                { Direction.FORWARD, new DynamicPhysicsConfig(-180f, -140f, isNegative: true) },
                { Direction.LEFT, null },
                { Direction.RIGHT, null },
            };

            parameters[POSITION_DAMPER_Z].gravityPhysicsConfigs = new Dictionary<string, DynamicPhysicsConfig>
            {
                { Direction.DOWN, null },
                { Direction.UP, null },
                { Direction.BACK, new DynamicPhysicsConfig(-15f, 5f, isNegative: true, multiplyInvertedMass: true) },
                { Direction.FORWARD, new DynamicPhysicsConfig(0f, 0f) },
                { Direction.LEFT, null },
                { Direction.RIGHT, null },
            };

            parameters[TARGET_ROTATION_X].gravityPhysicsConfigs = new Dictionary<string, DynamicPhysicsConfig>
            {
                { Direction.DOWN, new DynamicPhysicsConfig(-16f, -12f, isNegative: true, additive: false) },
                { Direction.UP, new DynamicPhysicsConfig(10.7f, 8f, additive: false) },
                { Direction.BACK, null },
                { Direction.FORWARD, null },
                { Direction.LEFT, null },
                { Direction.RIGHT, null },
            };

            parameters[TARGET_ROTATION_Y].gravityPhysicsConfigs = new Dictionary<string, DynamicPhysicsConfig>
            {
                { Direction.DOWN, null },
                { Direction.UP, null },
                { Direction.BACK, null },
                { Direction.FORWARD, null },
                { Direction.LEFT, new DynamicPhysicsConfig(16f, 12f, additive: false) },
                { Direction.RIGHT, new DynamicPhysicsConfig(-16f, -12f, isNegative: true, additive: false) },
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

        private void UpdatePhysics(string direction, float effect)
        {
            _leftBreastParams.ForEach(param => UpdateParam(param, direction, effect));
            _rightBreastParams.ForEach(param => UpdateParam(param, direction, effect));
        }

        private void UpdateParam(PhysicsParameter param, string direction, float effect)
        {
            if(!param.gravityPhysicsConfigs.ContainsKey(direction))
                return;

            var config = param.gravityPhysicsConfigs[direction];
            if(config != null)
            {
                float value = NewValue(config, effect);
                if(config.additive)
                {
                    param.AddValue(value);
                }
                else
                {
                    param.SetValue(value);
                }
            }
        }

        private float NewValue(DynamicPhysicsConfig config, float effect)
        {
            float mass = config.multiplyInvertedMass ? 1 - _mass : _mass;
            float value =
                _softness * config.softnessMultiplier * effect +
                mass * config.massMultiplier * effect;

            bool inRange = config.isNegative ? value < 0 : value > 0;
            return inRange ? value : 0;
        }
    }
}
