﻿using System.Collections.Generic;
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

        private static Dictionary<string, DynamicPhysicsConfig> NewSpringConfigs()
        {
            var nippleConfig = new DynamicPhysicsConfig
            {
                baseMultiplier = 0.50f,
                massMultiplier = 0.50f,
                applyMethod = ApplyMethod.MULTIPLICATIVE,
                massCurve = MainPhysicsHandler.InvertMass,
            };
            var areolaConfig = new DynamicPhysicsConfig
            {
                baseMultiplier = 0.25f,
                massMultiplier = 0.25f,
                applyMethod = ApplyMethod.MULTIPLICATIVE,
                massCurve = MainPhysicsHandler.InvertMass,
            };
            return new Dictionary<string, DynamicPhysicsConfig>
            {
                { SoftColliderGroup.NIPPLE, nippleConfig },
                { SoftColliderGroup.AREOLA, areolaConfig },
            };
        }

        private static Dictionary<string, DynamicPhysicsConfig> NewDamperConfigs()
        {
            var nippleConfig = new DynamicPhysicsConfig
            {
                baseMultiplier = 0.50f,
                massMultiplier = 0.50f,
                applyMethod = ApplyMethod.MULTIPLICATIVE,
                massCurve = MainPhysicsHandler.InvertMass,
            };
            var areolaConfig = new DynamicPhysicsConfig
            {
                baseMultiplier = 0.25f,
                massMultiplier = 0.25f,
                applyMethod = ApplyMethod.MULTIPLICATIVE,
                massCurve = MainPhysicsHandler.InvertMass,
            };
            return new Dictionary<string, DynamicPhysicsConfig>
            {
                { SoftColliderGroup.NIPPLE, nippleConfig },
                { SoftColliderGroup.AREOLA, areolaConfig },
            };
        }

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
