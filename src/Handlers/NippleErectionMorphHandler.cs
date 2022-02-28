using System.Collections.Generic;

namespace TittyMagic
{
    internal class NippleErectionMorphHandler
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
            Persistence.LoadNippleMorphSettings(
                script,
                (dir, json) =>
                {
                    foreach(var config in configs)
                    {
                        if(json.HasKey(config.Name))
                        {
                            float value = json[config.Name].AsFloat;
                            config.BaseMultiplier = value;
                        }
                    }
                }
            );
        }

        public void Update(float nippleErection)
        {
            foreach(var config in _configs)
            {
                config.Morph.morphValue = nippleErection * config.BaseMultiplier;
            }
        }

        public void ResetAll()
        {
            foreach(var config in _configs)
            {
                config.Morph.morphValue = 0;
            }
        }
    }
}
