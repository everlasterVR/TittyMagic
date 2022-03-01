using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TittyMagic
{
    internal class BreastMassCalculator
    {
        private readonly DAZSkinV2 _skin;

        public BreastMassCalculator(DAZSkinV2 skin)
        {
            _skin = skin;
        }

        public float Calculate(float atomScale)
        {
            var boundsLeft = BoundsSize(GetPositions(VertexIndexGroups.LEFT_BREAST));
            var boundsRight = BoundsSize(GetPositions(VertexIndexGroups.RIGHT_BREAST));
            float leftVolume = EstimateVolume(boundsLeft, atomScale);
            float rightVolume = EstimateVolume(boundsRight, atomScale);
            return VolumeToMass((leftVolume + rightVolume) / 2);
        }

        private List<Vector3> GetPositions(IEnumerable<int> vertexIndices)
        {
            return vertexIndices.Select(i => _skin.rawSkinnedVerts[i]).ToList();
        }

        private static Vector3 BoundsSize(List<Vector3> vertices)
        {
            var min = Vector3.one * float.MaxValue;
            var max = Vector3.one * float.MinValue;
            foreach(var vertex in vertices)
            {
                min = Vector3.Min(min, vertex);
                max = Vector3.Max(max, vertex);
            }

            var bounds = new Bounds();
            bounds.min = min;
            bounds.max = max;

            return bounds.size;
        }

        // Ellipsoid volume
        private static float EstimateVolume(Vector3 size, float atomScale)
        {
            float toCm3 = Mathf.Pow(10, 6);
            float z = size.z * ResolveAtomScaleFactor(atomScale);
            return toCm3 * (4 * Mathf.PI * size.x / 2 * size.y / 2 * z / 2) / 3;
        }

        // compensates for the increasing outer size and hard colliders of larger breasts
        private static float VolumeToMass(float volume)
        {
            return Mathf.Pow(volume * 0.82f / 1000, 1.2f);
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
