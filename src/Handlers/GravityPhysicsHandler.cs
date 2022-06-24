// ReSharper disable RedundantUsingDirective
using System.Collections.Generic;
using static TittyMagic.Utils;
using static TittyMagic.GravityEffectCalc;

namespace TittyMagic
{
    internal class GravityPhysicsHandler
    {
        private readonly Script _script;

        private float _mass;
        private float _softness;

        public Multiplier xMultiplier { get; }
        public Multiplier yMultiplier { get; }
        public Multiplier zMultiplier { get; }

        private Dictionary<string, List<Config>> _configSets;

        public GravityPhysicsHandler(Script script)
        {
            _script = script;
            xMultiplier = new Multiplier();
            yMultiplier = new Multiplier();
            zMultiplier = new Multiplier();

            Globals.BREAST_CONTROL.invertJoint2RotationY = false;
        }

        public void LoadSettings()
        {
            _configSets = LoadSettingsFromFile();
        }

        private Dictionary<string, List<Config>> LoadSettingsFromFile()
        {
            var configSets = new Dictionary<string, List<Config>>();

            Persistence.LoadFromPath(
                _script,
                $@"{Globals.PLUGIN_PATH}settings\physicsmultipliers\female.json",
                (dir, json) =>
                {
                    foreach(string direction in json.Keys)
                    {
                        var configs = new List<Config>();
                        var groupJson = json[direction].AsObject;
                        foreach(string name in groupJson.Keys)
                        {
                            configs.Add(
                                new GravityPhysicsConfig(
                                    name,
                                    groupJson[name]["Type"],
                                    groupJson[name]["IsNegative"].AsBool,
                                    groupJson[name]["Multiplier1"].AsFloat,
                                    groupJson[name]["Multiplier2"].AsFloat,
                                    groupJson[name]["MultiplyInvertedMass"].AsBool
                                )
                            );
                        }

                        configSets[direction] = configs;
                    }
                }
            );
            return configSets;
        }

        public void SetBaseValues()
        {
            foreach(var kvp in _configSets)
            {
                foreach(var config in kvp.Value)
                {
                    var gravityPhysicsConfig = (GravityPhysicsConfig) config;
                    if(gravityPhysicsConfig.type == "additive")
                    {
                        gravityPhysicsConfig.baseValue = gravityPhysicsConfig.setting.val;
                    }
                }
            }
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
                ResetPhysics(Direction.RIGHT);
                UpdatePhysics(Direction.LEFT, effect);
            }
            // right
            else
            {
                ResetPhysics(Direction.LEFT);
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
                    ResetPhysics(Direction.UP);
                    UpdatePhysics(Direction.DOWN, effect);
                }
                // upside down
                else
                {
                    ResetPhysics(Direction.DOWN);
                    UpdatePhysics(Direction.UP, effect);
                }
            }
            // leaning back
            else
            {
                // upright
                if(pitch >= -1)
                {
                    ResetPhysics(Direction.UP);
                    UpdatePhysics(Direction.DOWN, effect);
                }
                // upside down
                else
                {
                    ResetPhysics(Direction.DOWN);
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
                ResetPhysics(Direction.BACK);
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
                ResetPhysics(Direction.FORWARD);
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
                UpdateValue(gravityPhysicsConfig, effect);
            }
        }

        private void UpdateValue(GravityPhysicsConfig config, float effect)
        {
            float value = CalculateValue(config, effect);
            bool inRange = config.isNegative ? value < 0 : value > 0;

            if(config.type == "direct")
            {
                config.setting.val = inRange ? value : 0;
            }
            else if(config.type == "additive")
            {
                config.setting.val = inRange ? config.baseValue + value : config.baseValue;
            }
        }

        private float CalculateValue(GravityPhysicsConfig config, float effect)
        {
            float mass = config.multiplyInvertedMass ? 1 - _mass : _mass;
            return
                _softness * config.multiplier1 * effect +
                mass * config.multiplier2 * effect;
        }

        public void ResetAll()
        {
            _configSets?.Keys.ToList().ForEach(ResetPhysics);
        }

        private void ResetPhysics(string configSetName)
        {
            foreach(var config in _configSets[configSetName])
            {
                var gravityPhysicsConfig = (GravityPhysicsConfig) config;
                float newValue = gravityPhysicsConfig.type == "additive" ? gravityPhysicsConfig.baseValue : gravityPhysicsConfig.originalValue;
                gravityPhysicsConfig.setting.val = newValue;
            }
        }
    }
}
