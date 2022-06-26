// ReSharper disable RedundantUsingDirective
using System.Collections.Generic;
using UnityEngine;
using static TittyMagic.Utils;
using static TittyMagic.GravityEffectCalc;

namespace TittyMagic
{
    internal class ForcePhysicsHandler
    {
        private readonly MainPhysicsHandler _mainPhysicsHandler;
        private readonly SoftPhysicsHandler _softPhysicsHandler;

        private readonly TrackNipple _trackLeftNipple;
        private readonly TrackNipple _trackRightNipple;

        private const float SOFTNESS = 0.62f;
        private float _mass;
        private float _pitchMultiplier;
        private float _rollMultiplier;

        public Multiplier xMultiplier { get; }
        public Multiplier yMultiplier { get; }
        public Multiplier zMultiplier { get; }

        private Dictionary<string, List<Config>> _configSets;

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
            _configSets = new Dictionary<string, List<Config>>
            {
                { Direction.DOWN_L, DownLConfigs() },
                { Direction.DOWN_R, DownRConfigs() },
                { Direction.UP_L, UpLConfigs() },
                { Direction.UP_R, UpRConfigs() },
                { Direction.BACK_L, BackLConfigs() },
                { Direction.BACK_R, BackRConfigs() },
                { Direction.FORWARD_L, ForwardLConfigs() },
                { Direction.FORWARD_R, ForwardRConfigs() },
                { Direction.LEFT_L, LeftLConfigs() },
                { Direction.LEFT_R, LeftRConfigs() },
                { Direction.RIGHT_L, RightLConfigs() },
                { Direction.RIGHT_R, RightRConfigs() },
            };
        }

        private List<Config> DownLConfigs()
        {
            return new List<Config>
            {
            };
        }

        private List<Config> DownRConfigs()
        {
            return new List<Config>
            {
            };
        }

        private List<Config> UpLConfigs()
        {
            return new List<Config>
            {
            };
        }

        private List<Config> UpRConfigs()
        {
            return new List<Config>
            {
            };
        }

        private List<Config> BackLConfigs()
        {
            return new List<Config>
            {
            };
        }

        private List<Config> BackRConfigs()
        {
            return new List<Config>
            {
            };
        }

        private List<Config> ForwardLConfigs()
        {
            return new List<Config>
            {
            };
        }

        private List<Config> ForwardRConfigs()
        {
            return new List<Config>
            {
            };
        }

        private List<Config> LeftLConfigs()
        {
            return new List<Config>
            {
            };
        }

        private List<Config> LeftRConfigs()
        {
            return new List<Config>
            {
            };
        }

        private List<Config> RightLConfigs()
        {
            return new List<Config>
            {
            };
        }

        private List<Config> RightRConfigs()
        {
            return new List<Config>
            {
            };
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
                SOFTNESS * config.softnessMultiplier * effect +
                mass * config.massMultiplier * effect;
        }
    }
}
