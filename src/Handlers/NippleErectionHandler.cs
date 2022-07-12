using System.Collections.Generic;
using TittyMagic.Configs;
using static TittyMagic.ParamName;

namespace TittyMagic
{
    internal class NippleErectionHandler
    {
        private readonly Script _script;
        private MorphConfigBase _morphConfig;
        private List<PhysicsParameterGroup> _paramGroups;

        public JSONStorableFloat nippleErectionJsf { get; }

        public NippleErectionHandler(Script script)
        {
            _script = script;
            nippleErectionJsf = _script.NewJSONStorableFloat("nippleErection", 0.00f, 0.00f, 1.00f);
            nippleErectionJsf.setCallbackFunction = _ => Update();
        }

        public void LoadSettings()
        {
            _morphConfig = new MorphConfigBase("TM_NippleErection", 1.0f);
            SetupPhysicsConfigs();
            _paramGroups = _script.softPhysicsHandler.parameterGroups.Values.ToList();
        }

        private static Dictionary<string, DynamicPhysicsConfig> NewSpringConfigs() =>
            new Dictionary<string, DynamicPhysicsConfig>
            {
                {
                    SoftColliderGroup.NIPPLE, new DynamicPhysicsConfig(
                        0f,
                        0.5f,
                        isNegative: false,
                        multiplyInvertedMass: true,
                        applyMethod: ApplyMethod.MULTIPLICATIVE
                    )
                    {
                        baseMultiplier = 0.5f,
                    }
                },
                {
                    SoftColliderGroup.AREOLA, new DynamicPhysicsConfig(
                        0f,
                        0.25f,
                        isNegative: false,
                        multiplyInvertedMass: true,
                        applyMethod: ApplyMethod.MULTIPLICATIVE
                    )
                    {
                        baseMultiplier = 0.25f,
                    }
                },
            };

        private static Dictionary<string, DynamicPhysicsConfig> NewDamperConfigs() =>
            new Dictionary<string, DynamicPhysicsConfig>
            {
                {
                    SoftColliderGroup.NIPPLE, new DynamicPhysicsConfig(
                        0f,
                        0.5f,
                        isNegative: false,
                        multiplyInvertedMass: true,
                        applyMethod: ApplyMethod.MULTIPLICATIVE
                    )
                    {
                        baseMultiplier = 0.5f,
                    }
                },
                {
                    SoftColliderGroup.AREOLA, new DynamicPhysicsConfig(
                        0f,
                        0.25f,
                        isNegative: false,
                        multiplyInvertedMass: true,
                        applyMethod: ApplyMethod.MULTIPLICATIVE
                    )
                    {
                        baseMultiplier = 0.25f,
                    }
                },
            };

        private void SetupPhysicsConfigs()
        {
            var paramGroups = _script.softPhysicsHandler.parameterGroups;
            paramGroups[SOFT_VERTICES_SPRING].SetNippleErectionConfigs(NewSpringConfigs(), NewSpringConfigs());
            paramGroups[SOFT_VERTICES_DAMPER].SetNippleErectionConfigs(NewDamperConfigs(), NewDamperConfigs());
        }

        public void Update()
        {
            _morphConfig.morph.morphValue = nippleErectionJsf.val * _morphConfig.multiplier;
            if(_script.settingsMonitor.softPhysicsEnabled)
            {
                float mass = _script.mainPhysicsHandler.massAmount;
                float softness = _script.softnessAmount;
                _paramGroups.ForEach(paramGroup =>
                    paramGroup.UpdateNippleErectionGroupValues(mass, softness, nippleErectionJsf.val)
                );
            }
        }

        public void Reset()
        {
            _morphConfig.morph.morphValue = 0;
        }
    }
}
