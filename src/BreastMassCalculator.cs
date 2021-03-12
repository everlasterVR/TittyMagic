using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TittyMagic
{
    public class BreastMassCalculator
    {
        private List<DAZPhysicsMeshSoftVerticesSet> rightBreastMainGroupSets;
        private Mesh inMemoryMesh;
        private float softVolume; // cm^3; spheroid volume estimation of right breast

        public BreastMassCalculator()
        {
            rightBreastMainGroupSets = Globals.BREAST_PHYSICS_MESH.softVerticesGroups
                .Find(it => it.name == "right")
                .softVerticesSets;
            inMemoryMesh = new Mesh();
        }

        public float Calculate(float atomScale)
        {
            softVolume = EstimateVolume(BoundsSize(), atomScale);
            return VolumeToMass(softVolume);
        }

        // roughly estimate the legacy scale value from automatically calculated mass
        public float LegacyScale(float massEstimate)
        {
            return 1.21f * massEstimate - 0.03f;
        }

        public string GetStatus(float atomScale)
        {
            float currentSoftVolume = EstimateVolume(BoundsSize(), atomScale);
            return $"volume: {softVolume}\ncurrent volume: {currentSoftVolume}";
        }

        private Vector3 BoundsSize()
        {
            Vector3[] vertices = rightBreastMainGroupSets
                .Select(it => it.jointRB.position).ToArray();

            inMemoryMesh.vertices = vertices;
            inMemoryMesh.RecalculateBounds();
            return inMemoryMesh.bounds.size;
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
            return Mathf.Pow((volume * 0.9f) / 1000, 1.25f) + 0.04f;
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
