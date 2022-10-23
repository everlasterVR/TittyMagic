namespace TittyMagic.Models
{
    public class MassParameter : PhysicsParameter
    {
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
