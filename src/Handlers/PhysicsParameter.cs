using System;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using TittyMagic.Configs;
using UnityEngine;

namespace TittyMagic
{
    internal class PhysicsParameterGroup
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Global MemberCanBePrivate.Global MemberCanBeProtected.Global
        public string id { get; }
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
        public bool invertRight { get; set; }
        public JSONStorableBool offsetOnlyLeftBreastJsb { get; }

        public PhysicsParameter left { get; }
        public PhysicsParameter right { get; }

        public PhysicsParameterGroup(string id, PhysicsParameter left, PhysicsParameter right, string displayName)
        {
            this.id = id;
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
                float rightValue = invertRight ? -value : value;
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

        public JSONClass GetJSON()
        {
            var jsonClass = new JSONClass();
            var leftJson = left.GetJSON();
            if(leftJson != null)
            {
                jsonClass["id"] = id;
                jsonClass["left"] = leftJson;
            }

            return jsonClass.Keys.Any() ? jsonClass : null;
        }

        public void RestoreFromJSON(JSONClass jsonClass)
        {
            if(jsonClass.HasKey("left"))
            {
                left.RestoreFromJSON(jsonClass["left"].AsObject);
            }
        }
    }

    internal class MassParameterGroup : PhysicsParameterGroup
    {
        public MassParameterGroup(string id, PhysicsParameter left, PhysicsParameter right, string displayName) : base(id, left, right, displayName)
        {
        }

        public new void SetOffsetCallbackFunctions()
        {
            left.offsetJsf.setCallbackFunction = value =>
            {
                left.UpdateOffsetValue(value);
                float rightValue = invertRight ? -value : value;
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
        public bool dependOnPhysicsRate { get; set; }

        public StaticPhysicsConfig config { get; set; }
        public StaticPhysicsConfig quicknessOffsetConfig { get; set; }
        public StaticPhysicsConfig slownessOffsetConfig { get; set; }
        public Dictionary<string, DynamicPhysicsConfig> gravityPhysicsConfigs { get; set; }
        public Dictionary<string, DynamicPhysicsConfig> forcePhysicsConfigs { get; set; }
        public string valueFormat { get; set; }
        public Action<float> sync { protected get; set; }

        public Dictionary<string, SoftGroupPhysicsParameter> groupMultiplierParams { get; set; }

        public PhysicsParameter(JSONStorableFloat valueJsf, JSONStorableFloat baseValueJsf = null, JSONStorableFloat offsetJsf = null)
        {
            this.valueJsf = valueJsf;
            this.baseValueJsf = baseValueJsf ?? new JSONStorableFloat(Intl.BASE_VALUE, valueJsf.val, valueJsf.min, valueJsf.max);
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

        public SoftGroupPhysicsParameter GetGroupParam(string group)
        {
            if(groupMultiplierParams == null)
            {
                return null;
            }

            return groupMultiplierParams[group];
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

        public JSONClass GetJSON()
        {
            var jsonClass = new JSONClass();
            if(offsetJsf.val != 0)
            {
                jsonClass["offset"].AsFloat = offsetJsf.val;
            }

            if(groupMultiplierParams != null)
            {
                foreach(var param in groupMultiplierParams)
                {
                    if(param.Value.offsetJsf.val != 0)
                    {
                        jsonClass[$"{param.Key}Offset"].AsFloat = param.Value.offsetJsf.val;
                    }
                }
            }

            return jsonClass.Keys.Any() ? jsonClass : null;
        }

        public void RestoreFromJSON(JSONClass jsonClass)
        {
            if(jsonClass.HasKey("offset"))
            {
                offsetJsf.val = jsonClass["offset"].AsFloat;
            }

            if(groupMultiplierParams != null)
            {
                foreach(var param in groupMultiplierParams)
                {
                    if(jsonClass.HasKey($"{param.Key}Offset"))
                    {
                        param.Value.offsetJsf.val = jsonClass[$"{param.Key}Offset"].AsFloat;
                    }
                }
            }
        }
    }

    internal class MassParameter : PhysicsParameter
    {
        public MassParameter(JSONStorableFloat valueJsf, JSONStorableFloat baseValueJsf = null, JSONStorableFloat offsetJsf = null)
            : base(valueJsf, baseValueJsf, offsetJsf)
        {
        }

        private void Sync()
        {
            float value = offsetJsf.val + baseValueJsf.val;
            valueJsf.val = value;
            sync?.Invoke(value);
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
            sync?.Invoke(value);
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
