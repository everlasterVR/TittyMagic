using System;
using static TittyMagic.Calc;

namespace TittyMagic
{
    internal class AtomScaleListener
    {
        private readonly JSONStorableFloat _atomScaleStorable;
        public float Value { get; private set; }

        public AtomScaleListener(JSONStorableFloat atomScaleStorable)
        {
            _atomScaleStorable = atomScaleStorable;
            Value = RoundToDecimals(atomScaleStorable.val, 1000f);
        }

        public bool Changed()
        {
            float value = RoundToDecimals(_atomScaleStorable.val, 1000f);
            bool changed = Math.Abs(value - Value) > 0.001f;

            if(changed)
            {
                Value = value;
            }

            return changed;
        }
    }
}
