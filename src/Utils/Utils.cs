// ReSharper disable UnusedMember.Global
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace TittyMagic
{
    public static class Utils
    {
        public static GenerateDAZMorphsControlUI morphsControlUI { get; set; }
        private static string _morphsPath;

        public static void Log(params object[] args)
        {
            if(Script.envIsDevelopment)
            {
                string message = string.Join(" ", args.Select(arg => arg.ToString()).ToArray());
                Debug.Log(message);
            }
        }

        // public static void Log(params object[] args)
        // {
        //     string message = string.Join(" ", args.Select(arg => arg.ToString()).ToArray());
        //     Debug.Log($"{Script.tittyMagic.containingAtom.uid}: {nameof(TittyMagic)}\n{message}");
        // }

        public static void LogError(params object[] args) =>
            SuperController.LogError(Format(args));

        public static void LogMessage(params object[] args) =>
            SuperController.LogMessage(Format(args));

        private static string Format(IEnumerable<object> args) =>
            $"{nameof(TittyMagic)} {Script.VERSION}: {string.Join(" ", args.Select(arg => arg.ToString()).ToArray())}";

        // ReSharper disable once UnusedMember.Global
        public static MVRScript FindPluginOnAtom(Atom atom, string search)
        {
            string match = atom.GetStorableIDs().FirstOrDefault(s => s.Contains(search));
            return match == null ? null : atom.GetStorableByID(match) as MVRScript;
        }

        public static void SetMorphPath(string packageId)
        {
            const string path = "Custom/Atom/Person/Morphs/female/everlaster";
            if(string.IsNullOrEmpty(packageId))
            {
                _morphsPath = $"{path}/{nameof(TittyMagic)}_dev";
            }
            else
            {
                _morphsPath = $"{packageId}:/{path}/{nameof(TittyMagic)}";
            }
        }

        public static DAZMorph GetMorph(string fileName)
        {
            string uid = $"{_morphsPath}/{fileName}.vmi";
            var dazMorph = morphsControlUI.GetMorphByUid(uid);
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

        public static bool GlobalAnimationFrozen()
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

        public static Rigidbody FindRigidbody(Atom atom, string name) =>
            atom.GetComponentsInChildren<Rigidbody>().ToList().Find(rb => rb.name == name);

        public static bool PluginIsDuplicate(Atom atom, string storeId)
        {
            var regex = new Regex($@"^plugin#\d+_{nameof(TittyMagic)}.{nameof(Script)}", RegexOptions.Compiled);
            var storables = atom.FindStorablesByRegexMatch(regex);
            return storables.Exists(storable => storable.storeId != storeId);
        }
    }
}
