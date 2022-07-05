using System.Collections.Generic;
using System.Linq;

namespace TittyMagic
{
    internal class NippleErectionMorphHandler
    {
        private readonly List<SimpleMorphConfig> _configs;
        public JSONStorableFloat nippleErectionJsf { get; }

        public NippleErectionMorphHandler(Script script)
        {
            _configs = LoadSettings(script);
            nippleErectionJsf = script.NewJSONStorableFloat("nippleErection", 0.00f, 0.00f, 1.00f);
        }

        private static List<SimpleMorphConfig> LoadSettings(Script script)
        {
            var jsonClass = script.LoadJSON($@"{script.PluginPath()}\settings\morphmultipliers\nippleErection.json").AsObject;
            return jsonClass.Keys
                .Select(name => new SimpleMorphConfig(name, jsonClass[name]["Multiplier"].AsFloat))
                .ToList();
        }

        public void Update(float nippleErection) =>
            _configs.ForEach(config => config.morph.morphValue = nippleErection * config.multiplier);

        public void ResetAll() => _configs.ForEach(config => config.morph.morphValue = 0);
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
