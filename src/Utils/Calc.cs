using UnityEngine;

namespace TittyMagic
{
    public static class Calc
    {
        public static float RoundToDecimals(float value, float roundFactor)
        {
            return Mathf.Round(value * roundFactor) / roundFactor;
        }
    }
}
