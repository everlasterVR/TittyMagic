using System.Collections.Generic;
using TittyMagic.Handlers.Configs;

namespace TittyMagic.Models
{
    public class PhysicsParameterGroup
    {
        public string displayName { get; set; }
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
        public bool requiresCalibration { get; set; }
        public bool hasSoftColliderVisualization { get; set; }
        public bool rightInverted { get; set; }
        public bool allowOffsetOnlyLeftBreast { get; set; }
        public JSONStorableBool offsetOnlyLeftBreastJsb { get; }

        public PhysicsParameter left { get; set; }
        public PhysicsParameter right { get; set; }

        public PhysicsParameterGroup()
        {
            allowOffsetOnlyLeftBreast = true;
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

        private void CreateOffsetPairCallback() =>
            left.offsetJsf.setCallbackFunction = value =>
            {
                left.UpdateOffsetValue(value);
                float rightValue = rightInverted ? -value : value;
                right.UpdateOffsetValue(offsetOnlyLeftBreastJsb.val ? 0 : rightValue);
            };

        private void CreateOffsetPairCallback(SoftGroupPhysicsParameter leftGroupParam, SoftGroupPhysicsParameter rightGroupParam) =>
            leftGroupParam.offsetJsf.setCallbackFunction = value =>
            {
                leftGroupParam.UpdateOffsetValue(value);
                rightGroupParam.UpdateOffsetValue(offsetOnlyLeftBreastJsb.val ? 0 : value);
            };

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

        public void SetGravityPhysicsConfigs(Dictionary<string, Dictionary<string, DynamicPhysicsConfig>> configs)
        {
            left.gravityPhysicsConfigs = configs[Side.LEFT];
            right.gravityPhysicsConfigs = configs[Side.RIGHT];
        }

        public void SetForcePhysicsConfigs(Dictionary<string, Dictionary<string, DynamicPhysicsConfig>> configs)
        {
            left.forcePhysicsConfigs = configs[Side.LEFT];
            right.forcePhysicsConfigs = configs[Side.RIGHT];
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
