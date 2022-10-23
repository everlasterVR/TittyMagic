using System.Linq;
using TittyMagic.Handlers.Configs;

namespace TittyMagic.Models
{
    public class SoftGroupPhysicsParameter : PhysicsParameter
    {
        public DynamicPhysicsConfig nippleErectionConfig { get; set; }
        private float _nippleErectionMultiplier = 1;

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

            float nippleErectionValue = nippleErectionConfig.Calculate(nippleErection, massValue, softness);
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
