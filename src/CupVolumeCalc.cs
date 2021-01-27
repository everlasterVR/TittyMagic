using UnityEngine;

namespace everlaster
{
    public static class CupVolumeCalc
    {
        private static float toCM3 = Mathf.Pow(10, 6);

        public static float EstimateVolume(Vector3 size, float atomScale)
        {
            float x = DiameterFix(size.x);
            float y = DiameterFix(size.y);
            float z = size.z * ResolveAtomScaleFactor(atomScale);
            //SuperController.LogMessage($"{x}, {y}, {z}");
            return toCM3 * ExpectedCupVolume(x/2, y/2, z/2);
        }

        // The bounding box seems to be too small for the physical size
        // when compared to a spheroid of the Vector3 size xyz dimensions
        private static float DiameterFix(float dimension)
        {
            return 1.15f * dimension;
        }

        // This oblate spheroid function fits known data for bra cup diameter
        // and volume for different cup sizes.
        private static float ExpectedCupVolume(float x, float y, float z)
        {
            return (2 * Mathf.PI * x * y * z)/3;
        }

        // This somewhat accurately scales breast volume to the apparent breast size when atom scale is adjusted.
        private static float ResolveAtomScaleFactor(float value)
        {
            if(value > 1)
            {
                float atomScaleAdjustment = 1 - Mathf.Abs(Mathf.Log10(Mathf.Pow(value, 3)));
                return value * atomScaleAdjustment;
            }

            if(value < 1)
            {
                float atomScaleAdjustment = 1 - Mathf.Abs(Mathf.Log10(Mathf.Pow(value, 3)));
                return value / atomScaleAdjustment;
            }

            return 1;
        }
    }
}
