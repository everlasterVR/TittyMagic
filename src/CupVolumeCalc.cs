using System;
using UnityEngine;

namespace everlaster
{
    public static class CupVolumeCalc
    {
        private static double toCM3 = Math.Pow(10, 6);

        public static double EstimateVolume(Vector3 size, float atomScale)
        {
            double cupDiameter = (size.x + size.y) / 2;
            double depth = size.z * DepthFix(size.z/size.x);
            return toCM3 * ExpectedCupVolume(cupDiameter, depth) * ResolveAtomScaleFactor(atomScale);
        }

        // This oblate spheroid function fits known data for bra cup diameter and volume for different cup sizes
        // e.g. https://www.wikipedia.com/en/Bra_size
        private static double ExpectedCupVolume(double diameter, double depth)
        {
            return (Math.PI/6) * Math.Pow(diameter, 2) * depth;
        }

        // This reduces depth to closer to the realistic half of cup diameter
        // and exaggerates the effect of depth's difference from diameter
        // e.g. very deep breasts -> volume is estimated to be accordingly higher
        // and very flat breasts -> volume is estimated to be accordingly lower
        private static double DepthFix(double depthToHorizontalDiameterRatio)
        {
            return 0.80 * Math.Pow(depthToHorizontalDiameterRatio, 2.00);
        }

        // This somewhat accurately scales breast volume to the apparent breast size when atom scale is adjusted.
        private static float ResolveAtomScaleFactor(float value)
        {
            float atomScaleAdjustment = 1 - (float) Math.Abs(Math.Log10(Math.Pow(value, 3)));
            if(value > 1)
            {
                return value * atomScaleAdjustment;
            }

            if(value < 1)
            {
                return value / atomScaleAdjustment;
            }

            return 1;
        }
    }
}
