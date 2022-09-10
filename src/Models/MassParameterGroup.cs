using UnityEngine;

namespace TittyMagic.Models
{
    public class MassParameterGroup : PhysicsParameterGroup
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
}
