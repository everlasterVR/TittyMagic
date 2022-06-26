// ReSharper disable MemberCanBePrivate.Global
using System;

namespace TittyMagic.Extensions
{
    public static class MVRScriptExtensions
    {
        public static string GetMorphsPath(this MVRScript script)
        {
            string packageId = GetPackageId(script);
            const string path = "Custom/Atom/Person/Morphs/female/everlaster";

            if(string.IsNullOrEmpty(packageId))
            {
                return $"{path}/{nameof(TittyMagic)}_dev";
            }

            return packageId + $":/{path}/{nameof(TittyMagic)}";
        }

        public static string PluginPath(this MVRScript script)
        {
            return $@"{script.GetPackagePath()}Custom\Scripts\everlaster\TittyMagic";
        }

        public static string GetPackagePath(this MVRScript script)
        {
            string packageId = script.GetPackageId();
            return packageId == "" ? "" : $"{packageId}:/";
        }

        //MacGruber / Discord 20.10.2020
        //Get path prefix of the package that contains this plugin
        public static string GetPackageId(this MVRScript script)
        {
            string id = script.name.Substring(0, script.name.IndexOf('_'));
            string filename = script.manager.GetJSON()["plugins"][id].Value;
            int idx = filename.IndexOf(":/", StringComparison.Ordinal);
            return idx >= 0 ? filename.Substring(0, idx) : "";
        }
    }
}
