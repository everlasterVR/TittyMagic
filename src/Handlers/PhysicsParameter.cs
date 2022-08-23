using System;
using System.Collections.Generic;
using System.Linq;
using TittyMagic.Configs;
using UnityEngine;

namespace TittyMagic.Handlers
{
    internal class PhysicsParameterGroup
    {
        public string displayName { get; }
        public string infoText { get; set; }

        public bool dependsOnPhysicsRate
        {
            get { return left.dependsOnPhysicsRate && right.dependsOnPhysicsRate; }
            set
            {
                left.dependsOnPhysicsRate = value;
                right.dependsOnPhysicsRate = value;
            }
        }

        public bool usesRealMass { get; set; }
        public bool requiresRecalibration { get; set; }
        public bool rightIsInverted { get; set; }
        public JSONStorableBool offsetOnlyLeftBreastJsb { get; }

        public PhysicsParameter left { get; }
        public PhysicsParameter right { get; }

        public PhysicsParameterGroup(PhysicsParameter left, PhysicsParameter right, string displayName)
        {
            this.left = left;
            this.right = right;
            this.displayName = displayName;
            offsetOnlyLeftBreastJsb = new JSONStorableBool("offsetOnlyLeftBreast", false);
        }

        public void SetOffsetCallbackFunctions()
        {
            CreateOffsetPairCallback();
            foreach(var param in left.groupMultiplierParams)
            {
                CreateOffsetPairCallback(param.Value, right.groupMultiplierParams[param.Key]);
            }

            offsetOnlyLeftBreastJsb.setCallbackFunction = value =>
            {
                right.UpdateOffsetValue(value ? 0 : left.offsetJsf.val);
                foreach(var param in right.groupMultiplierParams)
                {
                    param.Value.UpdateOffsetValue(value ? 0 : left.groupMultiplierParams[param.Key].offsetJsf.val);
                }
            };
        }

        private void CreateOffsetPairCallback()
        {
            left.offsetJsf.setCallbackFunction = value =>
            {
                left.UpdateOffsetValue(value);
                float rightValue = rightIsInverted ? -value : value;
                right.UpdateOffsetValue(offsetOnlyLeftBreastJsb.val ? 0 : rightValue);
            };
        }

        private void CreateOffsetPairCallback(SoftGroupPhysicsParameter leftGroupParam, SoftGroupPhysicsParameter rightGroupParam)
        {
            leftGroupParam.offsetJsf.setCallbackFunction = value =>
            {
                leftGroupParam.UpdateOffsetValue(value);
                rightGroupParam.UpdateOffsetValue(offsetOnlyLeftBreastJsb.val ? 0 : value);
            };
        }

        public void UpdateValue(float massValue, float softness, float quickness)
        {
            left.UpdateValue(massValue, softness, quickness);
            right.UpdateValue(massValue, softness, quickness);
        }

        public void SetNippleErectionConfigs(
            Dictionary<string, DynamicPhysicsConfig> leftConfigs,
            Dictionary<string, DynamicPhysicsConfig> rightConfigs
        )
        {
            left.SetNippleErectionConfigs(leftConfigs);
            right.SetNippleErectionConfigs(rightConfigs);
        }

        public void UpdateNippleErectionGroupValues(float massValue, float softness, float nippleErection)
        {
            left.UpdateNippleErectionGroupValues(massValue, softness, nippleErection);
            right.UpdateNippleErectionGroupValues(massValue, softness, nippleErection);
        }

        public void SetGravityPhysicsConfigs(
            Dictionary<string, DynamicPhysicsConfig> leftConfigs,
            Dictionary<string, DynamicPhysicsConfig> rightConfigs
        )
        {
            left.gravityPhysicsConfigs = leftConfigs;
            right.gravityPhysicsConfigs = rightConfigs;
        }

        public void UpdateGravityValue(string direction, float effect, float massValue, float softness)
        {
            left.UpdateGravityValue(direction, effect, massValue, softness);
            right.UpdateGravityValue(direction, effect, massValue, softness);
        }

        public void ResetGravityValue(string direction)
        {
            left.ResetGravityValue(direction);
            right.ResetGravityValue(direction);
        }

        public void SetForcePhysicsConfigs(
            Dictionary<string, DynamicPhysicsConfig> leftConfigs,
            Dictionary<string, DynamicPhysicsConfig> rightConfigs
        )
        {
            left.forcePhysicsConfigs = leftConfigs;
            right.forcePhysicsConfigs = rightConfigs;
        }
    }

    internal class MassParameterGroup : PhysicsParameterGroup
    {
        public MassParameterGroup(PhysicsParameter left, PhysicsParameter right, string displayName) : base(left, right, displayName)
        {
        }

        public new void SetOffsetCallbackFunctions()
        {
            left.offsetJsf.setCallbackFunction = value =>
            {
                left.UpdateOffsetValue(value);
                float rightValue = rightIsInverted ? -value : value;
                right.UpdateOffsetValue(offsetOnlyLeftBreastJsb.val ? 0 : rightValue);
            };

            offsetOnlyLeftBreastJsb.setCallbackFunction = value =>
                right.UpdateOffsetValue(value ? 0 : left.offsetJsf.val);
        }

        public void UpdateValue(float volume)
        {
            float mass = Mathf.Clamp(
                Mathf.Pow(0.78f * volume, 1.5f),
                left.valueJsf.min,
                left.valueJsf.max
            );

            ((MassParameter) left).UpdateValue(mass);
            ((MassParameter) right).UpdateValue(mass);
        }
    }

    internal class PhysicsParameter
    {
        public JSONStorableFloat valueJsf { get; }
        public JSONStorableFloat baseValueJsf { get; }
        public JSONStorableFloat offsetJsf { get; }

        private readonly Dictionary<string, float> _additiveGravityAdjustments;
        protected readonly Dictionary<string, float> additiveForceAdjustments;
        public bool dependsOnPhysicsRate { get; set; }

        public StaticPhysicsConfig config { get; set; }
        public StaticPhysicsConfig quicknessOffsetConfig { get; set; }
        public StaticPhysicsConfig slownessOffsetConfig { get; set; }
        public Dictionary<string, DynamicPhysicsConfig> gravityPhysicsConfigs { get; set; }
        public Dictionary<string, DynamicPhysicsConfig> forcePhysicsConfigs { get; set; }
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
        }

        private void Sync()
        {
            float value = offsetJsf.val + baseValueJsf.val;
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
            float value = config.Calculate(massValue, softness);
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
            if(gravityPhysicsConfigs == null || !gravityPhysicsConfigs.ContainsKey(direction))
            {
                return;
            }

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
        }
    }

    internal class MassParameter : PhysicsParameter
    {
        public MassParameter(JSONStorableFloat valueJsf, JSONStorableFloat baseValueJsf, JSONStorableFloat offsetJsf)
            : base(valueJsf, baseValueJsf, offsetJsf)
        {
        }

        private void Sync()
        {
            float value = offsetJsf.val + baseValueJsf.val;
            valueJsf.val = value;
            sync?.Invoke(valueJsf.val);
        }

        public void UpdateValue(float massValue)
        {
            baseValueJsf.val = massValue;
            Sync();
            UpdateOffsetMinMax();
        }
    }

    internal class SoftGroupPhysicsParameter : PhysicsParameter
    {
        public DynamicPhysicsConfig nippleErectionConfig { get; set; }
        private float _nippleErectionMultiplier = 1;

        public SoftGroupPhysicsParameter(JSONStorableFloat valueJsf, JSONStorableFloat baseValueJsf, JSONStorableFloat offsetJsf)
            : base(valueJsf, baseValueJsf, offsetJsf)
        {
        }

        public void Sync()
        {
            float baseValue = _nippleErectionMultiplier * baseValueJsf.val;
            float value = offsetJsf.val + baseValue;
            if(forcePhysicsConfigs != null)
            {
                value += additiveForceAdjustments.Values.Sum();
            }

            valueJsf.val = value;
            sync?.Invoke(valueJsf.val);
        }

        public new void UpdateValue(float massValue, float softness, float quickness)
        {
            baseValueJsf.val = NewBaseValue(massValue, softness, quickness);
            Sync();
            UpdateOffsetMinMax();
        }

        public new void UpdateOffsetValue(float value)
        {
            offsetJsf.valNoCallback = value;
            Sync();
        }

        public void UpdateNippleErectionValue(float massValue, float softness, float nippleErection)
        {
            if(nippleErectionConfig == null)
            {
                return;
            }

            float nippleErectionValue = nippleErectionConfig.CalculateNippleGroupValue(nippleErection, massValue, softness);
            switch(nippleErectionConfig.applyMethod)
            {
                case ApplyMethod.MULTIPLICATIVE:
                    _nippleErectionMultiplier = 1 + nippleErectionValue;
                    break;
            }

            Sync();
        }
    }
}
