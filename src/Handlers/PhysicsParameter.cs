using System;
using System.Collections.Generic;
using System.Linq;
using TittyMagic.Configs;
using UnityEngine;

namespace TittyMagic
{
    internal class PhysicsParameterGroup
    {
        public string displayName { get; }
        public string infoText { get; set; }

        public bool dependOnPhysicsRate
        {
            get { return left.dependOnPhysicsRate && right.dependOnPhysicsRate; }
            set
            {
                left.dependOnPhysicsRate = value;
                right.dependOnPhysicsRate = value;
            }
        }

        public bool useRealMass { get; set; }
        public bool requiresRecalibration { get; set; }
        public JSONStorableBool offsetOnlyLeftBreastJsb { get; }

        public PhysicsParameter left { get; }
        public PhysicsParameter right { get; }

        public bool hasStaticConfig => left.config != null && right.config != null;

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
            if(left.groupMultiplierParams != null)
            {
                foreach(var param in left.groupMultiplierParams)
                {
                    CreateOffsetPairCallback(param.Value, right.groupMultiplierParams[param.Key]);
                }
            }

            offsetOnlyLeftBreastJsb.setCallbackFunction = value =>
            {
                right.UpdateOffsetValue(value ? 0 : left.offsetJsf.val);
                if(right.groupMultiplierParams != null)
                {
                    foreach(var param in right.groupMultiplierParams)
                    {
                        param.Value.UpdateOffsetValue(value ? 0 : left.groupMultiplierParams[param.Key].offsetJsf.val);
                    }
                }
            };
        }

        private void CreateOffsetPairCallback()
        {
            left.offsetJsf.setCallbackFunction = value =>
            {
                left.UpdateOffsetValue(value);
                right.UpdateOffsetValue(offsetOnlyLeftBreastJsb.val ? 0 : value);
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

        public void SetForcePhysicsConfigs(
            Dictionary<string, DynamicPhysicsConfig> leftConfigs,
            Dictionary<string, DynamicPhysicsConfig> rightConfigs
        )
        {
            left.forcePhysicsConfigs = leftConfigs;
            right.forcePhysicsConfigs = rightConfigs;
        }

        public void UpdateForceValue(string direction, float effect, float massValue)
        {
            left.UpdateForceValue(direction, effect, massValue);
            right.UpdateForceValue(direction, effect, massValue);
        }
    }

    internal class PhysicsParameter
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Global MemberCanBePrivate.Global MemberCanBeProtected.Global
        public string id { get; }
        protected internal JSONStorableFloat valueJsf { get; }
        protected JSONStorableFloat baseValueJsf { get; }
        internal JSONStorableFloat offsetJsf { get; }

        private readonly Dictionary<string, float> _additiveGravityAdjustments;
        protected readonly Dictionary<string, float> additiveForceAdjustments;
        public bool dependOnPhysicsRate { get; set; }

        public StaticPhysicsConfig config { get; set; }
        public StaticPhysicsConfig quicknessOffsetConfig { get; set; }
        public StaticPhysicsConfig slownessOffsetConfig { get; set; }
        public Dictionary<string, DynamicPhysicsConfig> gravityPhysicsConfigs { get; set; }
        public Dictionary<string, DynamicPhysicsConfig> forcePhysicsConfigs { get; set; }
        public string valueFormat { get; set; }
        public Action<float> sync { protected get; set; }

        public Dictionary<string, SoftGroupPhysicsParameter> groupMultiplierParams { get; set; }

        public PhysicsParameter(string id, JSONStorableFloat valueJsf, JSONStorableFloat baseValueJsf = null, JSONStorableFloat offsetJsf = null)
        {
            this.id = id;
            this.valueJsf = valueJsf;
            this.baseValueJsf = baseValueJsf ?? new JSONStorableFloat(Intl.BASE_VALUE, 0, valueJsf.min, valueJsf.max);
            this.offsetJsf = offsetJsf ?? new JSONStorableFloat(Intl.OFFSET, 0, -valueJsf.max, valueJsf.max);
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
            sync?.Invoke(value);
            offsetJsf.min = -(baseValueJsf.val - baseValueJsf.min);
            offsetJsf.max = baseValueJsf.max - baseValueJsf.val;
        }

        public void UpdateValue(float massValue, float softness, float quickness)
        {
            baseValueJsf.val = NewBaseValue(massValue, softness, quickness);
            Sync();

            if(groupMultiplierParams != null)
            {
                foreach(var param in groupMultiplierParams)
                {
                    param.Value.UpdateValue(massValue, softness, quickness);
                }
            }
        }

        public void UpdateOffsetValue(float value)
        {
            offsetJsf.valNoCallback = value;
            Sync();

            if(groupMultiplierParams != null)
            {
                foreach(var param in groupMultiplierParams)
                {
                    param.Value.Sync();
                }
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
            if(groupMultiplierParams != null)
            {
                groupMultiplierParams
                    .Where(param => param.Key == SoftColliderGroup.NIPPLE || param.Key == SoftColliderGroup.AREOLA)
                    .ToList()
                    .ForEach(param => param.Value.UpdateNippleErectionValue(massValue, softness, nippleErection));
            }
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

            return dependOnPhysicsRate ? Utils.PhysicsRateMultiplier() * value : value;
        }

        public void UpdateGravityValue(string direction, float effect, float massValue, float softness)
        {
            if(gravityPhysicsConfigs == null || !gravityPhysicsConfigs.ContainsKey(direction))
            {
                return;
            }

            var dpConfig = gravityPhysicsConfigs[direction];
            float gravityValue = NewGravityValue(dpConfig, effect, massValue, softness);

            switch(dpConfig.applyMethod)
            {
                case ApplyMethod.ADDITIVE:
                    _additiveGravityAdjustments[direction] = gravityValue;
                    break;

                case ApplyMethod.DIRECT:
                    baseValueJsf.val = gravityValue;
                    break;
            }

            Sync();
        }

        private static float NewGravityValue(DynamicPhysicsConfig dpConfig, float effect, float massValue, float softness)
        {
            float mass = dpConfig.multiplyInvertedMass ? 1 - massValue : massValue;
            float value =
                softness * dpConfig.softnessMultiplier * effect +
                mass * dpConfig.massMultiplier * effect;

            bool inRange = dpConfig.isNegative ? value < 0 : value > 0;
            return inRange ? value : 0;
        }

        public void UpdateForceValue(string direction, float effect, float massValue)
        {
            if(!forcePhysicsConfigs.ContainsKey(direction))
            {
                return;
            }

            var dpConfig = forcePhysicsConfigs[direction];
            float forceValue = NewForceValue(dpConfig, effect, massValue);
            additiveForceAdjustments[direction] = forceValue;
            float newValue = _additiveGravityAdjustments.Values.Sum() + additiveForceAdjustments.Values.Sum() + offsetJsf.val + baseValueJsf.val;
            valueJsf.val = newValue;
            sync.Invoke(newValue);
        }

        private static float NewForceValue(DynamicPhysicsConfig dpConfig, float effect, float massValue)
        {
            const float softness = 0.62f;
            float mass = dpConfig.multiplyInvertedMass ? 1 - massValue : massValue;
            float value =
                softness * dpConfig.softnessMultiplier * effect +
                mass * dpConfig.massMultiplier * effect;

            bool inRange = dpConfig.isNegative ? value < 0 : value > 0;
            return inRange ? value : 0;
        }

        public List<JSONStorableFloat> GetGroupMultiplierStorables()
        {
            var list = new List<JSONStorableFloat>();
            if(groupMultiplierParams != null)
            {
                list.AddRange(groupMultiplierParams.Values.ToList().Select(param => param.valueJsf));
            }

            return list;
        }

        public IEnumerable<JSONStorableFloat> GetGroupOffsetStorables()
        {
            var list = new List<JSONStorableFloat>();
            if(groupMultiplierParams != null)
            {
                list.AddRange(groupMultiplierParams.Values.ToList().Select(param => param.offsetJsf));
            }

            return list;
        }
    }

    internal class SoftGroupPhysicsParameter : PhysicsParameter
    {
        public DynamicPhysicsConfig nippleErectionConfig { get; set; }
        private float _nippleErectionMultiplier = 1;

        public SoftGroupPhysicsParameter(string id, JSONStorableFloat valueJsf, JSONStorableFloat baseValueJsf, JSONStorableFloat offsetJsf)
            : base(id, valueJsf, baseValueJsf, offsetJsf)
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
            sync?.Invoke(value);
            offsetJsf.min = -(baseValue - baseValueJsf.min);
            offsetJsf.max = baseValueJsf.max - baseValue;
        }

        public new void UpdateValue(float massValue, float softness, float quickness)
        {
            baseValueJsf.val = NewBaseValue(massValue, softness, quickness);
            Sync();
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

            float nippleErectionValue = NewNippleErectionValue(massValue, softness, nippleErection);
            switch(nippleErectionConfig.applyMethod)
            {
                case ApplyMethod.MULTIPLICATIVE:
                    _nippleErectionMultiplier = 1 + nippleErectionValue;
                    break;
            }

            Sync();
        }

        private float NewNippleErectionValue(float massValue, float softness, float effect)
        {
            float mass = nippleErectionConfig.multiplyInvertedMass ? 1 - massValue : massValue;
            float value =
                nippleErectionConfig.baseMultiplier * effect +
                softness * nippleErectionConfig.softnessMultiplier * effect +
                mass * nippleErectionConfig.massMultiplier * effect;

            bool inRange = nippleErectionConfig.isNegative ? value < 0 : value > 0;
            return inRange ? value : 0;
        }
    }
}
