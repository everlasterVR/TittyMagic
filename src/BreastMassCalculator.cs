using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TittyMagic
{
    public class BreastMassCalculator
    {
        private List<DAZPhysicsMeshSoftVerticesSet> _rightBreastMainGroupSets;
        private Transform _chestTransform;
        private float _softVolume; // cm^3; spheroid volume estimation of right breast

        public BreastMassCalculator(Transform chestTransform)
        {
            this._chestTransform = chestTransform;
            _rightBreastMainGroupSets = Globals.BREAST_PHYSICS_MESH.softVerticesGroups
                .Find(it => it.name == "right")
                .softVerticesSets;
        }

        public float Calculate(float atomScale)
        {
            _softVolume = EstimateVolume(BoundsSize(), atomScale);
            return VolumeToMass(_softVolume);
        }

        public string GetStatus(float atomScale)
        {
            float currentSoftVolume = EstimateVolume(BoundsSize(), atomScale);
            return $"volume: {_softVolume}\ncurrent volume: {currentSoftVolume}";
        }

        private Vector3 BoundsSize()
        {
            Vector3[] vertices = _rightBreastMainGroupSets
                .Select(it => Calc.RelativePosition(_chestTransform, it.jointRB.position))
                .ToArray();

            Vector3 min = Vector3.one * float.MaxValue;
            Vector3 max = Vector3.one * float.MinValue;
            for(int i = 0; i<vertices.Length; ++i)
            {
                min = Vector3.Min(min, vertices[i]);
                max = Vector3.Max(max, vertices[i]);
            }
            Bounds bounds = new Bounds();
            bounds.min = min;
            bounds.max = max;

            return bounds.size;
        }

        // Ellipsoid volume
        private float EstimateVolume(Vector3 size, float atomScale)
        {
            float toCM3 = Mathf.Pow(10, 6);
            float z = size.z * ResolveAtomScaleFactor(atomScale);
            return toCM3 * (4 * Mathf.PI * size.x/2 * size.y/2 * z/2)/3;
        }

        // compensates for the increasing outer size and hard colliders of larger breasts
        private float VolumeToMass(float volume)
        {
            return Mathf.Pow((volume * 0.82f) / 1000, 1.2f);
        }

        // This somewhat accurately scales breast volume to the apparent breast size when atom scale is adjusted.
        private float ResolveAtomScaleFactor(float value)
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
