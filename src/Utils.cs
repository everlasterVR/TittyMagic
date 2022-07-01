// ReSharper disable MemberCanBePrivate.Global
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TittyMagic
{
    internal static class Utils
    {
        public static string morphsPath { get; set; }
        public static GenerateDAZMorphsControlUI morphsControlUI { get; set; }

        public static void LogError(string message, string name = "")
        {
            SuperController.LogError(Format(message, name));
        }

        public static void LogMessage(string message, string name = "")
        {
            SuperController.LogMessage(Format(message, name));
        }

        private static string Format(string message, string name)
        {
            return $"{nameof(TittyMagic)} {Script.VERSION}: {message}{(string.IsNullOrEmpty(name) ? "" : $" [{name}]")}";
        }

        // ReSharper disable once UnusedMember.Global
        public static MVRScript FindPluginOnAtom(Atom atom, string search)
        {
            string match = atom.GetStorableIDs().FirstOrDefault(s => s.Contains(search));
            return match == null ? null : atom.GetStorableByID(match) as MVRScript;
        }

        public static DAZMorph GetMorph(string file)
        {
            string uid = $"{morphsPath}/{file}.vmi";
            var dazMorph = morphsControlUI.GetMorphByUid(uid);
            if(dazMorph == null)
            {
                LogError($"Morph with uid '{uid}' not found!");
            }

            return dazMorph;
        }

        public static JSONStorableBool NewJSONStorableBool(this MVRScript script, string paramName, bool startingValue)
        {
            var storable = new JSONStorableBool(paramName, startingValue);
            storable.storeType = JSONStorableParam.StoreType.Full;
            script.RegisterBool(storable);
            return storable;
        }

        public static JSONStorableFloat NewJSONStorableFloat(
            this MVRScript script,
            string paramName,
            float startingValue,
            float minValue,
            float maxValue
        )
        {
            var storable = new JSONStorableFloat(paramName, startingValue, minValue, maxValue);
            storable.storeType = JSONStorableParam.StoreType.Full;
            script.RegisterFloat(storable);
            return storable;
        }

        public static JSONStorableFloat NewBaseValueStorable(float min, float max)
        {
            return new JSONStorableFloat("Base Value", 0, min, max);
        }

        public static JSONStorableFloat NewCurrentValueStorable(float min, float max)
        {
            return new JSONStorableFloat("Current Value", 0, min, max);
        }

        public static float PhysicsRateMultiplier()
        {
            return 0.01666667f / Time.fixedDeltaTime;
        }
    }

    internal static class Curves
    {
        public static float QuadraticRegression(float f)
        {
            return -0.173f * f * f + 1.142f * f;
        }

        public static float QuadraticRegressionLesser(float f)
        {
            return -0.115f * f * f + 1.12f * f;
        }

        // ReSharper disable once UnusedMember.Local
        private static float Polynomial(
            float x,
            float a = 1,
            float b = 1,
            float c = 1,
            float p = 1,
            float q = 1
        )
        {
            return a * Mathf.Pow(x, p) + b * Mathf.Pow(x, q) + c * x;
        }
    }

    internal static class Calc
    {
        public static float Roll(Quaternion q)
        {
            return 2 * InverseLerpToPi(Mathf.Asin(2 * q.x * q.y + 2 * q.z * q.w));
        }

        public static float Pitch(Quaternion q)
        {
            return InverseLerpToPi(Mathf.Atan2(2 * q.x * q.w - 2 * q.y * q.z, 1 - 2 * q.x * q.x - 2 * q.z * q.z));
        }

        private static float InverseLerpToPi(float val)
        {
            if(val > 0)
            {
                return Mathf.InverseLerp(0, Mathf.PI, val);
            }

            return -Mathf.InverseLerp(0, Mathf.PI, -val);
        }

        // value returned is smoothed (for better animation) i.e. no longer maps linearly to the actual rotation angle
        public static float SmoothStep(float val)
        {
            if(val > 0)
            {
                return Mathf.SmoothStep(0, 1, val);
            }

            return -Mathf.SmoothStep(0, 1, -val);
        }

        // https://www.desmos.com/calculator/crrr1uryep
        // ReSharper disable once UnusedMember.Global
        public static float InverseSmoothStep(float value, float b, float curvature, float midpoint)
        {
            if(value < 0)
                return 0;
            if(value > b)
                return 1;

            float s = curvature < -2.99f ? -2.99f : curvature > 0.99f ? 0.99f : curvature;
            float p = midpoint * b;
            p = p < 0 ? 0 : p > b ? b : p;
            float c = 2 / (1 - s) - p / b;

            if(value < p)
                return F1(value, b, p, c);

            return 1 - F1(b - value, b, b - p, c);
        }

        private static float F1(float value, float b, float n, float c)
        {
            return Mathf.Pow(value, c) / (b * Mathf.Pow(n, c - 1));
        }

        public static float RoundToDecimals(float value, float roundFactor)
        {
            return Mathf.Round(value * roundFactor) / roundFactor;
        }

        public static Vector3 RelativePosition(Rigidbody origin, Vector3 position)
        {
            var difference = position - origin.position;
            return new Vector3(
                Vector3.Dot(difference, origin.transform.right),
                Vector3.Dot(difference, origin.transform.up),
                Vector3.Dot(difference, origin.transform.forward)
            );
        }

        // ReSharper disable once UnusedMember.Global
        public static Vector3 AveragePosition(List<Vector3> positions)
        {
            var sum = Vector3.zero;
            foreach(var position in positions)
            {
                sum += position;
            }

            return sum / positions.Count;
        }

        private static bool EqualWithin(float roundFactor, float v1, float v2)
        {
            return Mathf.Abs(v1 - v2) < 1 / roundFactor;
        }

        // ReSharper disable once UnusedMember.Global
        public static bool DeviatesAtLeast(float v1, float v2, int percent)
        {
            if(v1 > v2)
            {
                return (v1 - v2) / v1 > (float) percent / 100;
            }

            return (v2 - v1) / v2 > (float) percent / 100;
        }

        public static bool VectorEqualWithin(float roundFactor, Vector3 v1, Vector3 v2)
        {
            return EqualWithin(roundFactor, v1.x, v2.x) && EqualWithin(roundFactor, v1.y, v2.y) && EqualWithin(roundFactor, v1.z, v2.z);
        }

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
