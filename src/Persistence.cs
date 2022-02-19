using System;
using System.Collections.Generic;
using SimpleJSON;
using MVR.FileManagementSecure;
using static TittyMagic.Globals;

namespace TittyMagic
{
    internal static class Persistence
    {
        private static Dictionary<string, string> settingsNames = new Dictionary<string, string>
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
            var path = $@"{PLUGIN_PATH}settings\staticphysics\{settingsNames[mode]}\{fileName}";
            LoadFromPath(script, path, callback);
        }

        public static void LoadModeGravityPhysicsSettings(MVRScript script, string mode, Action<string, JSONClass> callback = null)
        {
            var path = $@"{PLUGIN_PATH}settings\gravityphysics\{settingsNames[mode]}.json";
            LoadFromPath(script, path, callback);
        }

        public static void LoadModeMorphSettings(MVRScript script, string mode, string fileName, Action<string, JSONClass> callback = null)
        {
            var path = $@"{PLUGIN_PATH}settings\morphmultipliers\{settingsNames[mode]}\{fileName}";
            LoadFromPath(script, path, callback);
        }

        public static void LoadNippleMorphSettings(MVRScript script, Action<string, JSONClass> callback = null)
        {
            var path = $@"{PLUGIN_PATH}settings\morphmultipliers\nippleErection.json";
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
            FileManagerSecure.CreateDirectory(SAVES_DIR);
            return SAVES_DIR;
        }
    }
}
