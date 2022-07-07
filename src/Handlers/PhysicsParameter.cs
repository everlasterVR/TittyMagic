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
        public string valueFormat { get; }
        public string infoText { get; set; }

        public bool dependOnPhysicsRate
        {
            get { return _left.dependOnPhysicsRate && _right.dependOnPhysicsRate; }
            set
            {
                _left.dependOnPhysicsRate = value;
                _right.dependOnPhysicsRate = value;
            }
        }

        public bool useRealMass { get; set; }

        private readonly PhysicsParameter _left;
        private readonly PhysicsParameter _right;

        public bool hasStaticConfig => _left.config != null && _right.config != null;
        public List<JSONStorableFloat> currentValueStorables => _left.GetCurrentValueStorables();
        public List<JSONStorableFloat> offsetStorables => _left.GetOffsetStorables();

        public PhysicsParameterGroup(PhysicsParameter left, PhysicsParameter right, string displayName, string valueFormat)
        {
            _left = left;
            _right = right;
            this.displayName = displayName;
            this.valueFormat = valueFormat;
        }

        public void UpdateValue(float massValue, float softness, float quickness)
        {
            _left.UpdateValue(massValue, softness, quickness);
            _right.UpdateValue(massValue, softness, quickness);
        }

        public void UpdateNippleValue(float massValue, float softness, float nippleErection)
        {
            _left.UpdateNippleValue(massValue, softness, nippleErection);
            _right.UpdateNippleValue(massValue, softness, nippleErection);
        }

        public void SetLinearCurvesAroundMidpoint(float slope, float cutoff = 0.6285f)
        {
            _left.SetLinearCurvesAroundMidpoint(null, slope, cutoff);
            _right.SetLinearCurvesAroundMidpoint(null, slope, cutoff);
        }

        public void SetLinearCurvesAroundMidpoint(string group, float slope, float cutoff = 0.6285f)
        {
            _left.SetLinearCurvesAroundMidpoint(group, slope, cutoff);
            _right.SetLinearCurvesAroundMidpoint(group, slope, cutoff);
        }

        public void SetGravityPhysicsConfigs(
            Dictionary<string, DynamicPhysicsConfig> leftConfigs,
            Dictionary<string, DynamicPhysicsConfig> rightConfigs
        )
        {
            _left.gravityPhysicsConfigs = leftConfigs;
            _right.gravityPhysicsConfigs = rightConfigs;
        }

        public void UpdateGravityValue(string direction, float effect, float massValue, float softness)
        {
            _left.UpdateGravityValue(direction, effect, massValue, softness);
            _right.UpdateGravityValue(direction, effect, massValue, softness);
        }

        public void SetForcePhysicsConfigs(
            Dictionary<string, DynamicPhysicsConfig> leftConfigs,
            Dictionary<string, DynamicPhysicsConfig> rightConfigs
        )
        {
            _left.forcePhysicsConfigs = leftConfigs;
            _right.forcePhysicsConfigs = rightConfigs;
        }

        public void UpdateForceValue(string direction, float effect, float massValue)
        {
            _left.UpdateForceValue(direction, effect, massValue);
            _right.UpdateForceValue(direction, effect, massValue);
        }
    }

    internal class PhysicsParameter
    {
        protected JSONStorableFloat currentValueJsf { get; }
        protected JSONStorableFloat baseValueJsf { get; }
        private JSONStorableFloat offsetJsf { get; }

        private float _additiveAdjustedValue = 0;
        public bool dependOnPhysicsRate { get; set; }

        public StaticPhysicsConfig config { get; set; }
        public StaticPhysicsConfig quicknessOffsetConfig { get; set; }
        public StaticPhysicsConfig slownessOffsetConfig { get; set; }
        public Dictionary<string, DynamicPhysicsConfig> gravityPhysicsConfigs { get; set; }
        public Dictionary<string, DynamicPhysicsConfig> forcePhysicsConfigs { get; set; }
        public Action<float> sync { protected get; set; }

        public Dictionary<string, SoftGroupPhysicsParameter> groupMultiplierParams { get; set; }

        public PhysicsParameter(JSONStorableFloat currentValueJsf, JSONStorableFloat baseValueJsf = null, JSONStorableFloat offsetJsf = null)
        {
            this.currentValueJsf = currentValueJsf;
            this.baseValueJsf = baseValueJsf ?? new JSONStorableFloat(Intl.BASE_VALUE, 0, currentValueJsf.min, currentValueJsf.max);
            this.offsetJsf = offsetJsf ?? new JSONStorableFloat(Intl.OFFSET, 0, -currentValueJsf.max, currentValueJsf.max);
            this.offsetJsf.setCallbackFunction = UpdateOffsetValue;
        }

        public void UpdateValue(float massValue, float softness, float quickness)
        {
            baseValueJsf.val = NewBaseValue(massValue, softness, quickness);
            float newValue = _additiveAdjustedValue + offsetJsf.val + baseValueJsf.val;
            currentValueJsf.val = newValue;
            sync(newValue);

            UpdateOffsetJsfMinMax();

            if(groupMultiplierParams != null)
            {
                foreach(var param in groupMultiplierParams)
                {
                    param.Value.UpdateValue(massValue, softness, quickness);
                }
            }
        }

        private void UpdateOffsetJsfMinMax()
        {
            offsetJsf.min = -(baseValueJsf.val - baseValueJsf.min);
            offsetJsf.max = baseValueJsf.max - baseValueJsf.val;
        }

        private void UpdateOffsetValue(float value)
        {
            float newValue = _additiveAdjustedValue + value + baseValueJsf.val;
            currentValueJsf.val = newValue;
            sync(newValue);

            if(groupMultiplierParams != null)
            {
                foreach(var param in groupMultiplierParams)
                {
                    param.Value.Sync();
                }
            }
        }

        public void UpdateNippleValue(float massValue, float softness, float nippleErection)
        {
            if(groupMultiplierParams != null && groupMultiplierParams.ContainsKey(SoftColliderGroup.NIPPLE))
            {
                groupMultiplierParams[SoftColliderGroup.NIPPLE].UpdateNippleValue(massValue, softness, nippleErection);
            }
        }

        private float NewBaseValue(float massValue, float softness, float quickness)
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

        private void AddValue(float value)
        {
            _additiveAdjustedValue = value;
            float newValue = _additiveAdjustedValue + offsetJsf.val + baseValueJsf.val;
            currentValueJsf.val = newValue;
            sync(newValue);
        }

        public void UpdateGravityValue(string direction, float effect, float massValue, float softness)
        {
            if(!gravityPhysicsConfigs.ContainsKey(direction))
            {
                return;
            }

            var dpConfig = gravityPhysicsConfigs[direction];
            float gravityValue = NewGravityValue(dpConfig, effect, massValue, softness);
            if(dpConfig.additive)
            {
                AddValue(gravityValue);
            }
            else
            {
                baseValueJsf.val = gravityValue;
                float newValue = offsetJsf.val + baseValueJsf.val;
                currentValueJsf.val = newValue;
                sync(newValue);
            }
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
            float value = NewForceValue(dpConfig, effect, massValue);
            AddValue(value);
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

        public void SetLinearCurvesAroundMidpoint(string group, float slope, float cutoff)
        {
            if(group == null)
            {
                config.SetLinearCurvesAroundMidpoint(slope, cutoff);
            }
            else if(groupMultiplierParams != null && groupMultiplierParams.ContainsKey(group))
            {
                groupMultiplierParams[group].config.SetLinearCurvesAroundMidpoint(slope, cutoff);
            }
        }

        public List<JSONStorableFloat> GetCurrentValueStorables()
        {
            var list = new List<JSONStorableFloat>
            {
                currentValueJsf,
            };
            if(groupMultiplierParams != null)
            {
                list.AddRange(groupMultiplierParams.Values.ToList().Select(param => param.currentValueJsf));
            }

            return list;
        }

        public List<JSONStorableFloat> GetOffsetStorables()
        {
            var list = new List<JSONStorableFloat>
            {
                offsetJsf,
            };
            if(groupMultiplierParams != null)
            {
                list.AddRange(groupMultiplierParams.Values.ToList().Select(param => param.offsetJsf));
            }

            return list;
        }
    }

    internal class SoftGroupPhysicsParameter : PhysicsParameter
    {
        public SoftGroupPhysicsParameter(JSONStorableFloat currentValueJsf, JSONStorableFloat baseValueJsf, JSONStorableFloat offsetJsf)
            : base(currentValueJsf, baseValueJsf, offsetJsf)
        {
        }

        public new void UpdateNippleValue(float massValue, float softness, float nippleErection)
        {
            float value = NewNippleValue(massValue, softness, nippleErection);
            currentValueJsf.val = value;
            baseValueJsf.val = value;
            sync(value);
        }

        private float NewNippleValue(float massValue, float softness, float nippleErection)
        {
            float value = config.Calculate(massValue, softness);
            return value + 1.25f * nippleErection;
        }

        public void Sync()
        {
            sync(baseValueJsf.val);
        }
    }
}
