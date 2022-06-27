using System;
using System.Collections.Generic;
using TittyMagic.Configs;

namespace TittyMagic
{
    internal class PhysicsParameter
    {
        public string displayName { get; }
        public JSONStorableFloat baseValue { get; }
        public JSONStorableFloat currentValue { get; }
        public string valueFormat { get; }
        public string infoText { get; set; }

        public StaticPhysicsConfig config { get; set; }
        public StaticPhysicsConfigBase quicknessOffsetConfig { get; set; }
        public StaticPhysicsConfigBase slownessOffsetConfig { get; set; }
        public Dictionary<string, DynamicPhysicsConfig> gravityPhysicsConfigs { get; set; }
        public Dictionary<string, DynamicPhysicsConfig> forcePhysicsConfigs { get; set; }
        public Action<float> sync { private get; set; }

        public PhysicsParameter(string displayName, JSONStorableFloat baseValue, JSONStorableFloat currentValue, string valueFormat)
        {
            this.displayName = displayName;
            this.baseValue = baseValue;
            this.currentValue = currentValue;
            this.valueFormat = valueFormat;
        }

        public PhysicsParameter(string displayName, JSONStorableFloat baseValue, string valueFormat)
        {
            this.displayName = displayName;
            this.baseValue = baseValue;
            currentValue = Utils.NewCurrentValueStorable(baseValue.min, baseValue.max);
            this.valueFormat = valueFormat;
        }

        public void SetValue(float value)
        {
            baseValue?.SetVal(value);
            currentValue?.SetVal(value);
            sync(value);
        }

        public void AddValue(float value)
        {
            if(currentValue == null)
            {
                throw new ArgumentNullException($"currentValue must not be null in for a PhysicsParameter updated with AddValue");
            }

            currentValue.val = value;
            if(baseValue != null)
            {
                currentValue.val += baseValue.val;
            }
            sync(currentValue.val);
        }
    }
}
