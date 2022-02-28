using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TittyMagic
{
    public class BreastMassCalculator
    {
        private readonly List<DAZPhysicsMeshSoftVerticesSet> _rightBreastMainGroupSets;
        private readonly Rigidbody _chestRb;
        private float _softVolume; // cm^3; spheroid volume estimation of right breast

        public BreastMassCalculator(Rigidbody chestRb)
        {
            _chestRb = chestRb;
            _rightBreastMainGroupSets = Globals.BREAST_PHYSICS_MESH.softVerticesGroups
                .Find(it => it.name == "right")
                .softVerticesSets;
        }

        public float Calculate(float atomScale)
        {
            _softVolume = EstimateVolume(BoundsSize(), atomScale);
            return VolumeToMass(_softVolume);
        }

        private Vector3 BoundsSize()
        {
            var vertices = _rightBreastMainGroupSets
                .Select(it => Calc.RelativePosition(_chestRb, it.jointRB.position))
                .ToArray();

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
