namespace TittyMagic.Models
{
    public class MassParameter : PhysicsParameter
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
}
