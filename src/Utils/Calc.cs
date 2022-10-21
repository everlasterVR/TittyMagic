// ReSharper disable UnusedMember.Global
using UnityEngine;

namespace TittyMagic
{
    public static class Calc
    {
        // value returned is smoothed (for better animation) i.e. no longer maps linearly to the actual rotation angle
        public static float SmoothStep(float val) =>
            val > 0
                ? Mathf.SmoothStep(0, 1, val)
                : -Mathf.SmoothStep(0, 1, -val);

        public static float RoundToDecimals(float value, float roundFactor = 1000f) =>
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

        public static Vector3 RelativePosition(Transform transform, Vector3 position)
        {
            var difference = position - transform.position;
            return new Vector3(
                Vector3.Dot(difference, transform.right),
                Vector3.Dot(difference, transform.up),
                Vector3.Dot(difference, transform.forward)
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

        public static Vector3[] ExponentialMovingAverage(Vector3[] source, float k)
        {
            var result = new Vector3[source.Length];
            result[source.Length - 1] = source[source.Length - 1];
            for(int i = source.Length - 2; i >= 0; i--)
            {
                result[i] = new Vector3(
                    k * source[i].x + (1 - k) * result[i + 1].x,
                    k * source[i].y + (1 - k) * result[i + 1].y,
                    k * source[i].z + (1 - k) * result[i + 1].z
                );
            }

            return result;
        }
    }
}
