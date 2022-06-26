using System.Collections.Generic;
using System.Linq;
using TittyMagic.Extensions;

namespace TittyMagic
{
    public class NippleErectionMorphHandler
    {
        private readonly List<SimpleMorphConfig> _configs;

        public NippleErectionMorphHandler(MVRScript script)
        {
            _configs = LoadSettings(script);
        }

        private static List<SimpleMorphConfig> LoadSettings(MVRScript script)
        {
            var json = script.LoadJSON($@"{script.PluginPath()}\settings\morphmultipliers\nippleErection.json").AsObject;
            return json.Keys.Select(name => new SimpleMorphConfig(
                name,
                json[name]["Multiplier"].AsFloat
            )).ToList();
        }

        public void Update(float nippleErection)
        {
            foreach(var config in _configs)
            {
                config.morph.morphValue = nippleErection * config.multiplier;
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

    internal class SimpleMorphConfig
    {
        public float multiplier { get; }
        public DAZMorph morph { get; }

        public SimpleMorphConfig(string name, float multiplier)
        {
            this.multiplier = multiplier;
            morph = Utils.GetMorph(name);
        }
    }
}
