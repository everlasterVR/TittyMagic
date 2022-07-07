using System.ComponentModel;
using System.Linq;
using UnityEngine;

namespace TittyMagic
{
    internal static class Utils
    {
        public static string morphsPath { get; set; }
        public static GenerateDAZMorphsControlUI morphsControlUI { get; set; }

        public static void LogError(string message, string name = "") =>
            SuperController.LogError(Format(message, name));

        public static void LogMessage(string message, string name = "") =>
            SuperController.LogMessage(Format(message, name));

        private static string Format(string message, string name) =>
            $"{nameof(TittyMagic)} {Script.VERSION}: {message}{(string.IsNullOrEmpty(name) ? "" : $" [{name}]")}";

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

        public static JSONStorableBool NewJSONStorableBool(
            this MVRScript script,
            string paramName,
            bool startingValue,
            bool register = true
        )
        {
            var storable = new JSONStorableBool(paramName, startingValue);
            storable.storeType = JSONStorableParam.StoreType.Full;
            if(register)
            {
                script.RegisterBool(storable);
            }

            return storable;
        }

        public static JSONStorableFloat NewJSONStorableFloat(
            this MVRScript script,
            string paramName,
            float startingValue,
            float minValue,
            float maxValue,
            bool register = true
        )
        {
            var storable = new JSONStorableFloat(paramName, startingValue, minValue, maxValue);
            storable.storeType = JSONStorableParam.StoreType.Full;
            if(register)
            {
                script.RegisterFloat(storable);
            }

            return storable;
        }

        public static float PhysicsRateMultiplier() =>
            0.01666667f / Time.fixedDeltaTime;

        // ReSharper disable once UnusedMember.Global
        public static void DumpObjectToLog(object obj)
        {
            string result = "";
            foreach(PropertyDescriptor descriptor in TypeDescriptor.GetProperties(obj))
            {
                result += $"{descriptor.Name} = {descriptor.GetValue(obj)}\n";
            }

            Debug.Log(result);
        }
    }
}
