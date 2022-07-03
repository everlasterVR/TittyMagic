﻿using System.Collections.Generic;
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

        public JSONStorableFloat baseJsf { get; }
        public JSONStorableFloat upJsf { get; }
        public JSONStorableFloat downJsf { get; }
        public JSONStorableFloat forwardJsf { get; }
        public JSONStorableFloat backJsf { get; }
        public JSONStorableFloat leftRightJsf { get; }

        private float upMultiplier => baseJsf.val * upJsf.val;
        public float downMultiplier => baseJsf.val * downJsf.val;
        private float forwardMultiplier => baseJsf.val * forwardJsf.val;
        private float backMultiplier => baseJsf.val * backJsf.val;
        private float leftRightMultiplier => baseJsf.val * leftRightJsf.val;

        public GravityPhysicsHandler(Script script)
        {
            _script = script;

            baseJsf = script.NewJSONStorableFloat("gravityPhysicsBase", 1.00f, 0.00f, 2.00f);
            upJsf = script.NewJSONStorableFloat("gravityPhysicsUp", 1.00f, 0.00f, 2.00f);
            downJsf = script.NewJSONStorableFloat("gravityPhysicsDown", 1.00f, 0.00f, 2.00f);
            forwardJsf = script.NewJSONStorableFloat("gravityPhysicsForward", 1.00f, 0.00f, 2.00f);
            backJsf = script.NewJSONStorableFloat("gravityPhysicsBack", 1.00f, 0.00f, 2.00f);
            leftRightJsf = script.NewJSONStorableFloat("gravityPhysicsLeftRight", 1.00f, 0.00f, 2.00f);

            baseJsf.setCallbackFunction = value => _script.needsRecalibration = true;
            upJsf.setCallbackFunction = value => _script.needsRecalibration = true;
            downJsf.setCallbackFunction = value => _script.needsRecalibration = true;
            forwardJsf.setCallbackFunction = value => _script.needsRecalibration = true;
            backJsf.setCallbackFunction = value => _script.needsRecalibration = true;
            leftRightJsf.setCallbackFunction = value => _script.needsRecalibration = true;
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
                { Direction.BACK, new DynamicPhysicsConfig(-0.071f, -0.053f, isNegative: true, multiplyInvertedMass: true) },
                { Direction.FORWARD, new DynamicPhysicsConfig(0.141f, 0.106f, isNegative: false, multiplyInvertedMass: true) },
            };

            parameters[SPRING].gravityPhysicsConfigs = new Dictionary<string, DynamicPhysicsConfig>
            {
                { Direction.BACK, new DynamicPhysicsConfig(-7.0f, -5.3f, isNegative: true) },
                { Direction.FORWARD, new DynamicPhysicsConfig(-7.0f, -5.3f, isNegative: true) },
            };

            parameters[DAMPER].gravityPhysicsConfigs = new Dictionary<string, DynamicPhysicsConfig>
            {
                { Direction.BACK, new DynamicPhysicsConfig(-0.27f, -0.36f, isNegative: true) },
                { Direction.FORWARD, new DynamicPhysicsConfig(-0.27f, -0.36f, isNegative: true) },
            };

            parameters[POSITION_SPRING_Z].gravityPhysicsConfigs = new Dictionary<string, DynamicPhysicsConfig>
            {
                { Direction.BACK, new DynamicPhysicsConfig(-180f, -140f, isNegative: true) },
                { Direction.FORWARD, new DynamicPhysicsConfig(-180f, -140f, isNegative: true) },
            };

            parameters[POSITION_DAMPER_Z].gravityPhysicsConfigs = new Dictionary<string, DynamicPhysicsConfig>
            {
                { Direction.BACK, new DynamicPhysicsConfig(-15f, 5f, isNegative: true, multiplyInvertedMass: true) },
                { Direction.FORWARD, new DynamicPhysicsConfig(0f, 0f) },
            };

            parameters[TARGET_ROTATION_X].gravityPhysicsConfigs = new Dictionary<string, DynamicPhysicsConfig>
            {
                { Direction.DOWN, new DynamicPhysicsConfig(-16f, -12f, isNegative: true, additive: false) },
                { Direction.UP, new DynamicPhysicsConfig(10.7f, 8f, additive: false) },
            };

            parameters[TARGET_ROTATION_Y].gravityPhysicsConfigs = new Dictionary<string, DynamicPhysicsConfig>
            {
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

            // for some reason, if left right is adjusted after down, down physics is not correctly applied
            AdjustLeftRightPhysics(smoothRoll);
            AdjustUpPhysics(smoothPitch, smoothRoll);
            AdjustDownPhysics(smoothPitch, smoothRoll);
            AdjustForwardPhysics(smoothPitch, smoothRoll);
            AdjustBackPhysics(smoothPitch, smoothRoll);
        }

        private void AdjustLeftRightPhysics(float roll)
        {
            float effect = CalculateRollEffect(roll, leftRightMultiplier);
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

        private void AdjustUpPhysics(float pitch, float roll)
        {
            float effect = CalculateUpDownEffect(pitch, roll, upMultiplier);
            // leaning forward,  upside down
            if(pitch >= 1)
            {
                UpdatePhysics(Direction.UP, effect);
            }
            // leaning back, upside down
            else if(pitch < -1)
            {
                UpdatePhysics(Direction.UP, effect);
            }
        }

        private void AdjustDownPhysics(float pitch, float roll)
        {
            float effect = CalculateUpDownEffect(pitch, roll, downMultiplier);
            // leaning forward, upright
            if(pitch >= 0 && pitch < 1)
            {
                UpdatePhysics(Direction.DOWN, effect);
            }
            // leaning back
            else if(pitch >= -1 && pitch < 0)
            {
                UpdatePhysics(Direction.DOWN, effect);
            }
        }

        private void AdjustForwardPhysics(float pitch, float roll)
        {
            float effect = CalculateDepthEffect(pitch, roll, forwardMultiplier);
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
        }

        private void AdjustBackPhysics(float pitch, float roll)
        {
            float effect = CalculateDepthEffect(pitch, roll, backMultiplier);
            // leaning back
            if(pitch < 0)
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
            {
                return;
            }

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
