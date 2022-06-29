using System.Collections.Generic;
using System.Linq;

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
            var jsonClass = script.LoadJSON($@"{script.PluginPath()}\settings\morphmultipliers\nippleErection.json").AsObject;
            return jsonClass.Keys.Select(name => new SimpleMorphConfig(
                name,
                jsonClass[name]["Multiplier"].AsFloat
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
