﻿using System;

namespace TittyMagic
{
    internal class AtomScaleListener
    {
        private readonly JSONStorableFloat _atomScaleStorable;
        public float scale { get; private set; }

        public AtomScaleListener(JSONStorableFloat atomScaleStorable)
        {
            _atomScaleStorable = atomScaleStorable;
            scale = Calc.RoundToDecimals(atomScaleStorable.val, 1000f);
        }

        public bool Changed()
        {
            float value = Calc.RoundToDecimals(_atomScaleStorable.val, 1000f);
            bool changed = Math.Abs(value - scale) > 0.001f;

            if(changed)
            {
                scale = value;
            }

            return changed;
        }
    }
}
