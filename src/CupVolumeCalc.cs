using System;
using UnityEngine;

namespace everlaster
{
    public static class CupVolumeCalc
    {
        private static double toCM3 = Math.Pow(10, 6);

        public static double EstimateVolume(Vector3 size, float atomScale)
        {
            double x = DiameterFix(size.x);
            double y = DiameterFix(size.y);
            double z = DepthFix(size.z) * ResolveAtomScaleFactor(atomScale);
            return toCM3 * ExpectedCupVolume(x/2, y/2, z/2);
        }

        // The bounding box seems to be too small for the physical size
        // when compared to objects of known width (e.g. a cube shape atom).
        private static double DiameterFix(float dimension)
        {
            return 1.15 * dimension;
        }

        // Adjusts small breast depth to be smaller, and large breast depth to be larger
        // than reported by the bounding box z dimension. This seems to improve
        // the mass adjustment of breast maorphs that drastically change breast depth
        // (e.g. Breast large).
        private static double DepthFix(double z)
        {
            return Math.Pow(3.5 * z, 2);
        }

        // This oblate spheroid function fits known data for bra cup diameter
        // and volume for different cup sizes.
        private static double ExpectedCupVolume(double x, double y, double z)
        {
            return (2 * Math.PI * x * y * z)/3;
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
