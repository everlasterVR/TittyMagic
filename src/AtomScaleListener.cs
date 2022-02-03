using static TittyMagic.Calc;

namespace TittyMagic
{
    internal class AtomScaleListener
    {
        private JSONStorableFloat _atomScaleStorable;
        public float Value { get; set; }

        public AtomScaleListener(JSONStorableFloat atomScaleStorable)
        {
            this._atomScaleStorable = atomScaleStorable;
            Value = (float) RoundToDecimals(atomScaleStorable.val, 1000f);
        }

        public bool Changed()
        {
            float value = (float) RoundToDecimals(_atomScaleStorable.val, 1000f);
            if(value != Value)
            {
                Value = value;
                return true;
            }
            return false;
        }
    }
}
