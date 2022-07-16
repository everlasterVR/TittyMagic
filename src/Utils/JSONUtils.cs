using System.Collections.Generic;
using SimpleJSON;

namespace TittyMagic
{
    internal static class JSONUtils
    {
        public static JSONArray JSONArrayFromDictionary(Dictionary<string, float> dictionary)
        {
            var jsonArray = new JSONArray();
            foreach(var kvp in dictionary)
            {
                var entry = new JSONClass();
                entry["id"] = kvp.Key;
                entry["value"].AsFloat = kvp.Value;
                jsonArray.Add(entry);
            }

            return jsonArray;
        }

        public static JSONArray JSONArrayFromDictionary(Dictionary<string, bool> dictionary)
        {
            var jsonArray = new JSONArray();
            foreach(var kvp in dictionary)
            {
                var entry = new JSONClass();
                entry["id"] = kvp.Key;
                entry["value"].AsBool = kvp.Value;
                jsonArray.Add(entry);
            }

            return jsonArray;
        }
    }
}
