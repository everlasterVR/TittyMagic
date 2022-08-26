// ReSharper disable UnusedMember.Global
using System.Collections.Generic;
using UnityEngine;

namespace TittyMagic
{
    internal static class Calc
    {
        public static float Roll(Quaternion q) =>
            2 * InverseLerpToPi(Mathf.Asin(2 * q.x * q.y + 2 * q.z * q.w));

        public static float Pitch(Quaternion q) =>
            InverseLerpToPi(Mathf.Atan2(2 * q.x * q.w - 2 * q.y * q.z, 1 - 2 * q.x * q.x - 2 * q.z * q.z));

        private static float InverseLerpToPi(float val) =>
            val > 0
                ? Mathf.InverseLerp(0, Mathf.PI, val)
                : -Mathf.InverseLerp(0, Mathf.PI, -val);

        // value returned is smoothed (for better animation) i.e. no longer maps linearly to the actual rotation angle
        public static float SmoothStep(float val) =>
            val > 0
                ? Mathf.SmoothStep(0, 1, val)
                : -Mathf.SmoothStep(0, 1, -val);

        public static float RoundToDecimals(float value, float roundFactor) =>
            Mathf.Round(value * roundFactor) / roundFactor;

        public static Vector3 RelativePosition(Rigidbody origin, Vector3 position)
        {
            var difference = position - origin.position;
            return new Vector3(
                Vector3.Dot(difference, origin.transform.right),
                Vector3.Dot(difference, origin.transform.up),
                Vector3.Dot(difference, origin.transform.forward)
            );
        }

        public static Vector3 AveragePosition(Vector3[] positions)
        {
            var sum = Vector3.zero;
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach(var position in positions)
            {
                sum += position;
            }

            return sum / positions.Length;
        }

        private static bool IsEqualWithin(float diff, float v1, float v2) =>
            Mathf.Abs(v1 - v2) < diff;

        // ReSharper disable once UnusedMember.Global
        public static bool DeviatesAtLeast(float v1, float v2, int percent)
        {
            bool result;
            if(v1 > v2)
            {
                result = (v1 - v2) / v1 > (float) percent / 100;
            }
            else
            {
                result = (v2 - v1) / v2 > (float) percent / 100;
            }

            return result;
        }

        public static bool VectorIsEqualWithin(float diff, Vector3 v1, Vector3 v2) =>
            IsEqualWithin(diff, v1.x, v2.x) &&
            IsEqualWithin(diff, v1.y, v2.y) &&
            IsEqualWithin(diff, v1.z, v2.z);

        public static float[] ExponentialMovingAverage(float[] source, float k)
        {
            float[] result = new float[source.Length];
            result[source.Length - 1] = source[source.Length - 1];
            for(int i = source.Length - 2; i >= 0; i--)
            {
                result[i] = k * source[i] + (1 - k) * result[i + 1];
            }

            return result;
        }
    }
}
