// ReSharper disable UnusedMember.Global
using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace TittyMagic
{
    public static class Utils
    {
        public static string morphsPath { get; set; }

        public static void Log(string message)
        {
            if(Script.envIsDevelopment)
            {
                Debug.Log(message);
            }
        }

        public static void LogError(string message) => SuperController.LogError(Format(message));

        public static void LogMessage(string message) => SuperController.LogMessage(Format(message));

        private static string Format(string message) => $"{nameof(TittyMagic)} v{Script.VERSION}: {message}";

        // ReSharper disable once UnusedMember.Global
        public static MVRScript FindPluginOnAtom(Atom atom, string search)
        {
            string match = atom.GetStorableIDs().FirstOrDefault(s => s.Contains(search));
            return match == null ? null : atom.GetStorableByID(match) as MVRScript;
        }

        public static DAZMorph GetMorph(string file)
        {
            string uid = $"{morphsPath}/{file}.vmi";
            var dazMorph = Script.morphsControlUI.GetMorphByUid(uid);
            if(dazMorph == null)
            {
                LogError($"Morph with uid '{uid}' not found!");
            }

            return dazMorph;
        }

        public static float PhysicsRateMultiplier() =>
            0.01666667f / Time.fixedDeltaTime;

        public static string ObjectPropertiesString(object obj)
        {
            string result = "";
            foreach(PropertyDescriptor descriptor in TypeDescriptor.GetProperties(obj))
            {
                result += $"{descriptor.Name} = {descriptor.GetValue(obj)}\n";
            }

            return result;
        }

        public static void LogEvery(string str, int every = 1)
        {
            if(Calc.RoundToDecimals(Time.unscaledTime, 10f) % every == 0)
            {
                Log(str);
            }
        }

        /// <summary>
        /// Get a string containing a visual representation of a Transform's children hierarchy.
        /// </summary>
        /// <param name="root">The parent Transform (GameObject)</param>
        /// <param name="maxDepth">Maximum depth to traverse the hierarchy.</param>
        /// <param name="propertyDel">A delegate function that takes a Transform and returns the string to print as an entry in the hierarchy.
        /// For example, to print a hierarchy of each transform's name: <code>thisTransform => thisTransform.name</code> A null value will print names.</param>
        /// <returns>A string containing a visual hierarchy of all child transforms, including the root</returns>
        public static string ObjectHierarchyToString(Transform root, int? maxDepth = null, Func<Transform, string> propertyDel = null)
        {
            if(propertyDel == null)
            {
                propertyDel = t => t.name;
            }

            var builder = new StringBuilder();
            ObjectHierarchyToString(root, propertyDel, builder, maxDepth);

            if(builder.Length < 1024)
            {
                return builder.ToString();
            }

            return
                $"Output string length {builder.Length} may be too large for viewing in VAM. " +
                $"See %userprofile%/AppData/LocalLow/MeshedVR/VaM/output_log.txt for the full output.\n{builder}";
        }

        private static void ObjectHierarchyToString(
            Transform root,
            Func<Transform, string> propertyDel,
            StringBuilder builder,
            int? maxDepth,
            int currentDepth = 0
        )
        {
            if(currentDepth > maxDepth)
            {
                return;
            }

            for(int i = 0; i < currentDepth; i++)
            {
                builder.Append("|   ");
            }

            builder.Append(propertyDel(root) + "\n");
            foreach(Transform child in root)
            {
                ObjectHierarchyToString(child, propertyDel, builder, maxDepth, currentDepth + 1);
            }
        }

        public static bool AnimationIsFrozen()
        {
            bool mainToggleFrozen =
                SuperController.singleton.freezeAnimationToggle != null &&
                SuperController.singleton.freezeAnimationToggle.isOn;
            bool altToggleFrozen =
                SuperController.singleton.freezeAnimationToggleAlt != null &&
                SuperController.singleton.freezeAnimationToggleAlt.isOn;
            return mainToggleFrozen || altToggleFrozen;
        }

        public static Transform DestroyLayout(Transform transform)
        {
            UnityEngine.Object.Destroy(transform.GetComponent<LayoutElement>());
            return transform;
        }

        public static void Enable(MonoBehaviour monoBehaviour)
        {
            monoBehaviour.enabled = true;
        }

        public static void Disable(MonoBehaviour monoBehaviour)
        {
            monoBehaviour.enabled = false;
        }
    }
}
