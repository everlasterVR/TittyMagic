using System.Collections.Generic;
using TittyMagic.Handlers.Configs;
using TittyMagic.Models;
using static TittyMagic.ParamName;
using static TittyMagic.Script;

namespace TittyMagic.Handlers
{
    public static class NippleErectionHandler
    {
        private static MorphConfigBase _morphConfig;
        private static List<PhysicsParameterGroup> _paramGroups;

        public static JSONStorableFloat nippleErectionJsf { get; private set; }

        public static void Init()
        {
            nippleErectionJsf = tittyMagic.NewJSONStorableFloat("nippleErection", 0.00f, 0.00f, 1.00f);
            nippleErectionJsf.setCallbackFunction = _ => Update();
        }

        public static void LoadSettings()
        {
            _morphConfig = new MorphConfigBase("TM_NippleErection", 1.0f);
            if(personIsFemale)
            {
                var paramGroups = SoftPhysicsHandler.parameterGroups;
                paramGroups[SOFT_VERTICES_SPRING].SetNippleErectionConfigs(NewSpringConfigs(), NewSpringConfigs());
                paramGroups[SOFT_VERTICES_DAMPER].SetNippleErectionConfigs(NewDamperConfigs(), NewDamperConfigs());
                _paramGroups = SoftPhysicsHandler.parameterGroups.Values.ToList();
            }
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

        public static void Update()
        {
            _morphConfig.morph.morphValue = nippleErectionJsf.val * _morphConfig.multiplier;
            if(tittyMagic.settingsMonitor.softPhysicsEnabled)
            {
                float mass = MainPhysicsHandler.massAmount;
                float softness = tittyMagic.softnessAmount;
                _paramGroups.ForEach(paramGroup =>
                    paramGroup.UpdateNippleErectionGroupValues(mass, softness, nippleErectionJsf.val)
                );
            }
        }

        public static void Reset()
        {
            _morphConfig.morph.morphValue = 0;
        }

        public static void Destroy()
        {
            _morphConfig = null;
            _paramGroups = null;
            nippleErectionJsf = null;
        }
    }
}
