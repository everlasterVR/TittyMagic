using UnityEngine;

namespace TittyMagic
{
    public static class Calc
    {
        public static float RoundToDecimals(float value, float roundFactor)
        {
            return Mathf.Round(value * roundFactor) / roundFactor;
        }

        // This is used to scale pitch effect by roll angle's distance from 90/-90 = person is sideways
        //-> if person is sideways, pitch related adjustments have less effect
        public static float RollFactor(float roll)
        {
            return (90 - Mathf.Abs(roll)) / 90;
        }
    }
}
