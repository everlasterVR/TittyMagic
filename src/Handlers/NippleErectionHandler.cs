using System.Collections.Generic;
using TittyMagic.Configs;
using static TittyMagic.ParamName;
using static TittyMagic.Script;

namespace TittyMagic.Handlers
{
    internal class NippleErectionHandler
    {
        private MorphConfigBase _morphConfig;
        private List<PhysicsParameterGroup> _paramGroups;

        public JSONStorableFloat nippleErectionJsf { get; }

        public NippleErectionHandler()
        {
            nippleErectionJsf = tittyMagic.NewJSONStorableFloat("nippleErection", 0.00f, 0.00f, 1.00f);
            nippleErectionJsf.setCallbackFunction = _ => Update();
        }

        public void LoadSettings()
        {
            _morphConfig = new MorphConfigBase("TM_NippleErection", 1.0f);
            SetupPhysicsConfigs();
            _paramGroups = tittyMagic.softPhysicsHandler.parameterGroups.Values.ToList();
        }

        private static Dictionary<string, DynamicPhysicsConfig> NewSpringConfigs() =>
            new Dictionary<string, DynamicPhysicsConfig>
            {
                {
                    SoftColliderGroup.NIPPLE, new DynamicPhysicsConfig(
                        massMultiplier: 0.5f,
                        softnessMultiplier: 0.0f,
                        isNegative: false,
                        applyMethod: ApplyMethod.MULTIPLICATIVE,
                        massCurve: MainPhysicsHandler.InvertMass
                    )
                    {
                        baseMultiplier = 0.5f,
                    }
                },
                {
                    SoftColliderGroup.AREOLA, new DynamicPhysicsConfig(
                        massMultiplier: 0.25f,
                        softnessMultiplier: 0.00f,
                        isNegative: false,
                        applyMethod: ApplyMethod.MULTIPLICATIVE,
                        massCurve: MainPhysicsHandler.InvertMass
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
                        massMultiplier: 0.5f,
                        softnessMultiplier: 0.0f,
                        isNegative: false,
                        applyMethod: ApplyMethod.MULTIPLICATIVE,
                        massCurve: MainPhysicsHandler.InvertMass
                    )
                    {
                        baseMultiplier = 0.5f,
                    }
                },
                {
                    SoftColliderGroup.AREOLA, new DynamicPhysicsConfig(
                        massMultiplier: 0.25f,
                        softnessMultiplier: 0.00f,
                        isNegative: false,
                        applyMethod: ApplyMethod.MULTIPLICATIVE,
                        massCurve: MainPhysicsHandler.InvertMass
                    )
                    {
                        baseMultiplier = 0.25f,
                    }
                },
            };

        private void SetupPhysicsConfigs()
        {
            var paramGroups = tittyMagic.softPhysicsHandler.parameterGroups;
            paramGroups[SOFT_VERTICES_SPRING].SetNippleErectionConfigs(NewSpringConfigs(), NewSpringConfigs());
            paramGroups[SOFT_VERTICES_DAMPER].SetNippleErectionConfigs(NewDamperConfigs(), NewDamperConfigs());
        }

        public void Update()
        {
            _morphConfig.morph.morphValue = nippleErectionJsf.val * _morphConfig.multiplier;
            if(tittyMagic.settingsMonitor.softPhysicsEnabled)
            {
                float mass = tittyMagic.mainPhysicsHandler.massAmount;
                float softness = tittyMagic.softnessAmount;
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
