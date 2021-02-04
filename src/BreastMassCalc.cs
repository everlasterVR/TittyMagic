using UnityEngine;

namespace TittyMagic
{
    public static class BreastMassCalc
    {
        private static float toCM3 = Mathf.Pow(10, 6);

        // Ellipsoid volume
        public static float EstimateVolume(Vector3 size, float atomScale)
        {
            float z = size.z * ResolveAtomScaleFactor(atomScale);
            return toCM3 * (4 * Mathf.PI * size.x/2 * size.y/2 * z/2)/3;
        }

        // compensates for the increasing outer size and hard colliders of larger breasts
        public static float VolumeToMass(float volume)
        {
            return Mathf.Pow((volume * 0.9f) / 1000, 1.25f) + 0.04f;
        }

        // roughly estimate the legacy scale value from automatically calculated mass
        public static float LegacyScale(float massEstimate)
        {
            return 1.21f * massEstimate - 0.03f;
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
