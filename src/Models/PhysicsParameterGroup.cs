using System.Collections.Generic;
using TittyMagic.Handlers.Configs;

namespace TittyMagic.Models
{
    public class PhysicsParameterGroup
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
        public bool allowsSoftColliderVisualization { get; set; }
        public bool rightInverted { get; set; }
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
                float rightValue = rightInverted ? -left.offsetJsf.val : left.offsetJsf.val;
                right.UpdateOffsetValue(value ? 0 : rightValue);
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
                float rightValue = rightInverted ? -value : value;
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

        public void UpdateGravityValue(string direction, float effectLeft, float effectRight, float massValue, float softness)
        {
            left.UpdateGravityValue(direction, effectLeft, massValue, softness);
            right.UpdateGravityValue(direction, effectRight, massValue, softness);
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

        public void SetFrictionConfig(DynamicPhysicsConfig leftConfig, DynamicPhysicsConfig rightConfig)
        {
            left.inverseFrictionConfig = leftConfig;
            right.inverseFrictionConfig = rightConfig;
        }

        public void UpdateInverseFrictionValue(float friction, float massValue, float softness)
        {
            left.UpdateInverseFrictionValue(friction, massValue, softness);
            right.UpdateInverseFrictionValue(friction, massValue, softness);
        }

        public void ResetInverseFrictionValue()
        {
            left.ResetInverseFrictionValue();
            right.ResetInverseFrictionValue();
        }
    }
}
