using System;
using SimpleJSON;
using MVR.FileManagementSecure;
using static TittyMagic.Globals;

namespace TittyMagic
{
    internal static class Persistence
    {
        // ReSharper disable once UnusedMember.Global
        public static void SaveToPath(MVRScript script, JSONClass json, string path, string saveExt, Action<string> callback = null)
        {
            string browseDir = path.Substring(0, path.LastIndexOfAny(new[] { '/', '\\' })) + @"\";
            if(!path.ToLower().EndsWith(saveExt.ToLower()))
            {
                path += "." + saveExt;
            }

            script.SaveJSON(json, path);
            callback?.Invoke(browseDir);
        }

        public static void LoadFromPath(MVRScript script, string path, Action<string, JSONClass> callback = null)
        {
            string browseDir = path.Substring(0, path.LastIndexOfAny(new[] { '/', '\\' })) + @"\";
            var json = script.LoadJSON(path).AsObject;

            callback?.Invoke(browseDir, json);
        }

        // ReSharper disable once UnusedMember.Global
        public static string MakeDefaultDir()
        {
            FileManagerSecure.CreateDirectory(SAVES_DIR);
            return SAVES_DIR;
        }
    }
}
