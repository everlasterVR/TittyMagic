namespace TittyMagic
{
    internal class ConfiguratorUISection
    {
        public string name { get; }
        public JSONStorableString typeStorable { get; }
        public JSONStorableBool isNegativeStorable { get; }
        public JSONStorableFloat multiplier1Storable { get; }
        public JSONStorableFloat multiplier2Storable { get; }
        public JSONStorableFloat valueStorable { get; }

        public ConfiguratorUISection(MVRScript script, MorphConfig config)
        {
            name = config.name;
            script.NewTextField(name, $"\n{name}", 32);

            isNegativeStorable = new JSONStorableBool("IsNegative", config.isNegative);

            multiplier1Storable = script.NewFloatSlider("Multiplier1", config.multiplier1, -3, 3, "F3", true);
            multiplier1Storable.slider.onValueChanged.AddListener(val => { config.multiplier1 = val; });

            valueStorable = script.NewFloatSlider("value", config.morph.morphValue, 2 * config.morph.min, 2 * config.morph.max, "F3");
            valueStorable.slider.interactable = false;

            multiplier2Storable = script.NewFloatSlider("Multiplier2", config.multiplier2, -3, 3, "F3", true);
            multiplier2Storable.slider.onValueChanged.AddListener(val => { config.multiplier2 = val; });
        }

        public ConfiguratorUISection(MVRScript script, GravityPhysicsConfig config)
        {
            name = config.name;
            script.NewTextField(name, $"\n{name}", 32);

            typeStorable = new JSONStorableString("Type", config.type);
            isNegativeStorable = new JSONStorableBool("IsNegative", config.isNegative);

            multiplier1Storable = script.NewFloatSlider("Multiplier1", config.multiplier1, -40, 40, "F2", true);
            multiplier1Storable.slider.onValueChanged.AddListener(val => { config.multiplier1 = val; });

            valueStorable = script.NewFloatSlider("value", config.setting.val, config.setting.min, config.setting.max, "F2");
            valueStorable.slider.interactable = false;

            multiplier2Storable = script.NewFloatSlider("Multiplier2", config.multiplier2, -40, 40, "F2", true);
            multiplier2Storable.slider.onValueChanged.AddListener(val => { config.multiplier2 = val; });
        }
    }
}
