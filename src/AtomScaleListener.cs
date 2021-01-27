namespace everlaster
{
    class AtomScaleListener
    {
        private JSONStorableFloat atomScaleStorable;
        public float Value { get; set; }

        public AtomScaleListener(JSONStorableFloat atomScaleStorable)
        {
            this.atomScaleStorable = atomScaleStorable;
            Value = (float) Calc.RoundToDecimals(atomScaleStorable.val, 1000f);
        }

        public bool Changed()
        {
            float value = (float) Calc.RoundToDecimals(atomScaleStorable.val, 1000f);
            if (value != Value)
            {
                Value = value;
                return true;
            }
            return false;
        }
    }
}
