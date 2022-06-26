using System;
using System.Collections.Generic;
using TittyMagic.Configs;
using UnityEngine;

namespace TittyMagic
{
    internal class PhysicsParameter
    {
        public string displayName { get; private set; }
        public float baseValue { get; private set; }
        public float currentValue { get; private set; }

        public StaticPhysicsConfig config { get; set; }
        public StaticPhysicsConfigBase quicknessOffsetConfig { get; set; }
        public StaticPhysicsConfigBase slownessOffsetConfig { get; set; }
        public Dictionary<string, DynamicPhysicsConfig> gravityPhysicsConfigs { get; set; }
        public Dictionary<string, DynamicPhysicsConfig> forcePhysicsConfigs { get; set; }
        public Action<float> sync { private get; set; }

        public PhysicsParameter(string displayName)
        {
            this.displayName = displayName;
        }

        public void SetValue(float value)
        {
            baseValue = value;
            currentValue = value;
            sync(currentValue);
        }

        public void AddValue(float value)
        {
            currentValue = baseValue + value;
            sync(currentValue);
        }
    }
}
