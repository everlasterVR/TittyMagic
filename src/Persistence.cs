using System;
using SimpleJSON;
using MVR.FileManagementSecure;

namespace TittyMagic
{
    internal static class Persistence
    {
        public static void SaveToPath(MVRScript script, JSONClass json, string path, string saveExt, Action<string> callback = null)
        {
            if(string.IsNullOrEmpty(path))
            {
                return;
            }
            var browseDir = path.Substring(0, path.LastIndexOfAny(new char[] { '/', '\\' })) + @"\";
            if(!path.ToLower().EndsWith(saveExt.ToLower()))
            {
                path += "." + saveExt;
            }

            script.SaveJSON(json, path);
            callback?.Invoke(browseDir);
        }

        public static void LoadFromPath(MVRScript script, string path, Action<string, JSONClass> callback = null)
        {
            if(string.IsNullOrEmpty(path))
            {
                return;
            }
            var browseDir = path.Substring(0, path.LastIndexOfAny(new char[] { '/', '\\' })) + @"\";
            var json = script.LoadJSON(path).AsObject;

            callback?.Invoke(browseDir, json);
        }

        public static string MakeDefaultDir()
        {
            FileManagerSecure.CreateDirectory(Globals.SAVES_DIR);
            return Globals.SAVES_DIR;
        }
    }
}
