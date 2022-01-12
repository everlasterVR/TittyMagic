using System;
using System.Collections.Generic;
using SimpleJSON;
using MVR.FileManagementSecure;

namespace TittyMagic
{
    internal static class Persistence
    {
        private static Dictionary<string, string> settingsDirNames = new Dictionary<string, string>
        {
            { Mode.ANIM_OPTIMIZED, "animoptimized" },
            { Mode.BALANCED, "balanced" },
            { Mode.TOUCH_OPTIMIZED, "touchoptimized" },
        };

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

        public static void LoadModePhysicsSettings(MVRScript script, string mode, string fileName, Action<string, JSONClass> callback = null)
        {
            var path = $@"{Globals.PLUGIN_PATH}settings\staticphysics\{settingsDirNames[mode]}\{fileName}";
            LoadFromPath(script, path, callback);
        }

        public static void LoadModeMorphSettings(MVRScript script, string mode, string fileName, Action<string, JSONClass> callback = null)
        {
            var path = $@"{Globals.PLUGIN_PATH}settings\morphmultipliers\{settingsDirNames[mode]}\{fileName}";
            LoadFromPath(script, path, callback);
        }

        public static void LoadNippleMorphSettings(MVRScript script, Action<string, JSONClass> callback = null)
        {
            var path = $@"{Globals.PLUGIN_PATH}settings\morphmultipliers\nippleErection.json";
            LoadFromPath(script, path, callback);
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
