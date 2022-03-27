using System.Collections.Generic;

namespace TittyMagic
{
    public class NippleErectionMorphHandler
    {
        private readonly List<MorphConfig> _configs = new List<MorphConfig>
        {
            new MorphConfig("TM_NippleErection"),
            // new MorphConfig("Nipples Depth"), // Spacedog.Import_Reloaded_Lite.2
            // new MorphConfig("Natural Nipples"), // Spacedog.Import_Reloaded_Lite.2
            // new MorphConfig("Nipple"), // Spacedog.Import_Reloaded_Lite.2
            // new MorphConfig("Nipple Length"),
            // new MorphConfig("Nipples Apply"),
            // new MorphConfig("Nipples Bulbous"), // kemenate.Morphs.10
            // new MorphConfig("Nipples Sag"), // kemenate.Morphs.10
            // new MorphConfig("Nipples Tilt") // kemenate.Morphs.10
        };

        public NippleErectionMorphHandler(MVRScript script)
        {
            LoadSettings(script, _configs);
        }

        private static void LoadSettings(MVRScript script, List<MorphConfig> configs)
        {
            Persistence.LoadFromPath(
                script,
                $@"{Globals.PLUGIN_PATH}settings\morphmultipliers\nippleErection.json",
                (dir, json) =>
                {
                    foreach(var config in configs)
                    {
                        if(json.HasKey(config.name))
                        {
                            float value = json[config.name].AsFloat;
                            config.baseMultiplier = value;
                        }
                    }
                }
            );
        }

        public void Update(float nippleErection)
        {
            foreach(var config in _configs)
            {
                config.morph.morphValue = nippleErection * config.baseMultiplier;
            }
        }

        public void ResetAll()
        {
            foreach(var config in _configs)
            {
                config.morph.morphValue = 0;
            }
        }
    }
}
