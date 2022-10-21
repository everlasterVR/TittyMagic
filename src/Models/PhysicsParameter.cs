using System;
using System.Collections.Generic;
using System.Linq;
using TittyMagic.Handlers.Configs;
using UnityEngine;

namespace TittyMagic.Models
{
    public class PhysicsParameter
    {
        public JSONStorableFloat valueJsf { get; }
        public JSONStorableFloat baseValueJsf { get; }
        public JSONStorableFloat offsetJsf { get; }

        public Dictionary<string, DynamicPhysicsConfig> gravityPhysicsConfigs { get; set; }
        private readonly Dictionary<string, float> _additiveGravityAdjustments;

        public Dictionary<string, DynamicPhysicsConfig> forcePhysicsConfigs { get; set; }
        protected readonly Dictionary<string, float> additiveForceAdjustments;

        public DynamicPhysicsConfig inverseFrictionConfig { get; set; }
        private float _additiveFrictionAdjustment;

        public bool dependsOnPhysicsRate { get; set; }

        public StaticPhysicsConfig config { get; set; }
        public StaticPhysicsConfig altConfig { get; set; }
        public StaticPhysicsConfig quicknessOffsetConfig { get; set; }
        public StaticPhysicsConfig slownessOffsetConfig { get; set; }
        public string valueFormat { get; set; }
        public Action<float> sync { protected get; set; }

        public Dictionary<string, SoftGroupPhysicsParameter> groupMultiplierParams { get; }

        public PhysicsParameter(JSONStorableFloat valueJsf, JSONStorableFloat baseValueJsf, JSONStorableFloat offsetJsf)
        {
            this.valueJsf = valueJsf;
            this.baseValueJsf = baseValueJsf;
            this.offsetJsf = offsetJsf;
            groupMultiplierParams = new Dictionary<string, SoftGroupPhysicsParameter>();
            _additiveGravityAdjustments = new Dictionary<string, float>();
            additiveForceAdjustments = new Dictionary<string, float>();
            _additiveFrictionAdjustment = 0;
        }

        private void Sync()
        {
            float value = offsetJsf.val + baseValueJsf.val + _additiveFrictionAdjustment;
            if(gravityPhysicsConfigs != null)
            {
                value += _additiveGravityAdjustments.Values.Sum();
            }

            if(forcePhysicsConfigs != null)
            {
                value += additiveForceAdjustments.Values.Sum();
            }

            valueJsf.val = value;
            sync?.Invoke(valueJsf.val);
        }

        protected void UpdateOffsetMinMax()
        {
            offsetJsf.min = -(baseValueJsf.val - baseValueJsf.min);
            offsetJsf.max = baseValueJsf.max - baseValueJsf.val;
        }

        public void UpdateValue(float massValue, float softness, float quickness)
        {
            baseValueJsf.val = NewBaseValue(massValue, softness, quickness);
            Sync();
            UpdateOffsetMinMax();

            foreach(var param in groupMultiplierParams)
            {
                param.Value.UpdateValue(massValue, softness, quickness);
            }
        }

        public void UpdateOffsetValue(float value)
        {
            offsetJsf.valNoCallback = value;
            Sync();

            foreach(var param in groupMultiplierParams)
            {
                param.Value.Sync();
            }
        }

        public void SetNippleErectionConfigs(Dictionary<string, DynamicPhysicsConfig> configs)
        {
            foreach(var kvp in configs)
            {
                var groupMultiplierParam = groupMultiplierParams[kvp.Key];
                groupMultiplierParam.nippleErectionConfig = kvp.Value;
            }
        }

        public void UpdateNippleErectionGroupValues(float massValue, float softness, float nippleErection)
        {
            groupMultiplierParams
                .Where(param => param.Key == SoftColliderGroup.NIPPLE || param.Key == SoftColliderGroup.AREOLA)
                .ToList()
                .ForEach(param => param.Value.UpdateNippleErectionValue(massValue, softness, nippleErection));
        }

        protected float NewBaseValue(float massValue, float softness, float quickness)
        {
            var activeConfig = !Script.tittyMagic.settingsMonitor.softPhysicsEnabled && altConfig != null ? altConfig : config;
            float value = activeConfig.Calculate(massValue, softness);
            if(quicknessOffsetConfig != null && quickness > 0)
            {
                float maxQuicknessOffset = quicknessOffsetConfig.Calculate(massValue, softness);
                value += Mathf.Lerp(0, maxQuicknessOffset, quickness);
            }

            if(slownessOffsetConfig != null && quickness < 0)
            {
                float maxSlownessOffset = slownessOffsetConfig.Calculate(massValue, softness);
                value += Mathf.Lerp(0, maxSlownessOffset, -quickness);
            }

            return dependsOnPhysicsRate ? Utils.PhysicsRateMultiplier() * value : value;
        }

        public void UpdateGravityValue(string direction, float effect, float massValue, float softness)
        {
            var dpConfig = gravityPhysicsConfigs[direction];
            float gravityValue = dpConfig.Calculate(effect, massValue, softness);

            switch(dpConfig.applyMethod)
            {
                case ApplyMethod.ADDITIVE:
                    _additiveGravityAdjustments[direction] = gravityValue;
                    break;
            }

            Sync();
        }

        public void ResetGravityValue(string direction)
        {
            if(gravityPhysicsConfigs == null || !gravityPhysicsConfigs.ContainsKey(direction))
            {
                return;
            }

            var dpConfig = gravityPhysicsConfigs[direction];
            switch(dpConfig.applyMethod)
            {
                case ApplyMethod.ADDITIVE:
                    _additiveGravityAdjustments[direction] = 0;
                    break;
            }

            Sync();
        }

        public void UpdateForceValue(string direction, float effect, float massValue, float softness)
        {
            if(forcePhysicsConfigs == null || !forcePhysicsConfigs.ContainsKey(direction))
            {
                return;
            }

            var dpConfig = forcePhysicsConfigs[direction];
            float forceValue = dpConfig.Calculate(effect, massValue, softness);

            switch(dpConfig.applyMethod)
            {
                case ApplyMethod.ADDITIVE:
                    additiveForceAdjustments[direction] = forceValue;
                    break;
            }

            Sync();
        }

        public void ResetForceValue(string direction)
        {
            if(forcePhysicsConfigs == null || !forcePhysicsConfigs.ContainsKey(direction))
            {
                return;
            }

            var dpConfig = forcePhysicsConfigs[direction];
            switch(dpConfig.applyMethod)
            {
                case ApplyMethod.ADDITIVE:
                    additiveForceAdjustments[direction] = 0;
                    break;
            }

            Sync();
        }

        public void UpdateInverseFrictionValue(float friction, float massValue, float softness)
        {
            switch(inverseFrictionConfig.applyMethod)
            {
                case ApplyMethod.ADDITIVE:
                    _additiveFrictionAdjustment = Calc.RoundToDecimals(inverseFrictionConfig.Calculate(friction, massValue, softness), 10000f);
                    break;
            }

            Sync();
        }

        public void ResetInverseFrictionValue()
        {
            switch(inverseFrictionConfig.applyMethod)
            {
                case ApplyMethod.ADDITIVE:
                    _additiveFrictionAdjustment = 0;
                    break;
            }

            Sync();
        }
    }
}
