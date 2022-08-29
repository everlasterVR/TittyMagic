﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TittyMagic
{
    internal class BreastVolumeCalculator
    {
        private readonly Script _script;
        private readonly Rigidbody _chestRb;

        public BreastVolumeCalculator(Script script, Rigidbody chestRb)
        {
            _script = script;
            _chestRb = chestRb;
        }

        public float Calculate(float atomScale)
        {
            var boundsLeft = BoundsSize(GetPositions(VertexIndexGroup.LEFT_BREAST));
            var boundsRight = BoundsSize(GetPositions(VertexIndexGroup.RIGHT_BREAST));
            float leftVolume = EstimateVolume(boundsLeft, atomScale);
            float rightVolume = EstimateVolume(boundsRight, atomScale);
            return (leftVolume + rightVolume) / (2 * 1000);
        }

        private List<Vector3> GetPositions(IEnumerable<int> vertexIndices) =>
            vertexIndices
                .Select(i => Calc.RelativePosition(_chestRb, _script.skin.rawSkinnedVerts[i]))
                .ToList();

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

            float atomScaleFactor;
            /* This somewhat accurately scales breast volume to the apparent breast size when atom scale is adjusted. */
            {
                float atomScaleAdjustment = 1 - Mathf.Abs(Mathf.Log10(Mathf.Pow(atomScale, 3)));

                if(atomScale >= 1)
                {
                    atomScaleFactor = atomScale * atomScaleAdjustment;
                }
                else
                {
                    atomScaleFactor = atomScale / atomScaleAdjustment;
                }
            }

            float z = size.z * atomScaleFactor;
            float volume = toCm3 * (4 * Mathf.PI * size.x / 2 * size.y / 2 * z / 2) / 3;
            // * 0.75f compensates for change in estimated volume compared to pre v3.2 bounds calculation
            return volume * 0.75f;
        }
    }
}
