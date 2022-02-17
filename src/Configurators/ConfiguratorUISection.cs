namespace TittyMagic
{
    internal class ConfiguratorUISection
    {
        public string Name { get; set; }
        public JSONStorableString TypeStorable { get; set; }
        public JSONStorableBool IsNegativeStorable { get; set; }
        public JSONStorableFloat Multiplier1Storable { get; set; }
        public JSONStorableFloat Multiplier2Storable { get; set; }
        public JSONStorableFloat ValueStorable { get; set; }

        public ConfiguratorUISection(MVRScript script, MorphConfig config)
        {
            Name = config.Name;
            script.NewTextField(Name, $"\n{Name}", 32);

            IsNegativeStorable = new JSONStorableBool("IsNegative", config.IsNegative);

            Multiplier1Storable = script.NewFloatSlider("Multiplier1", config.Multiplier1, -3, 3, "F3", true);
            Multiplier1Storable.slider.onValueChanged.AddListener((float val) =>
            {
                config.Multiplier1 = val;
            });

            ValueStorable = script.NewFloatSlider("value", config.Morph.morphValue, 2 * config.Morph.min, 2 * config.Morph.max, "F3");
            ValueStorable.slider.interactable = false;

            Multiplier2Storable = script.NewFloatSlider("Multiplier2", config.Multiplier2, -3, 3, "F3", true);
            Multiplier2Storable.slider.onValueChanged.AddListener((float val) =>
            {
                config.Multiplier2 = val;
            });
        }

        public ConfiguratorUISection(MVRScript script, GravityPhysicsConfig config)
        {
            Name = config.Name;
            script.NewTextField(Name, $"\n{Name}", 32);

            TypeStorable = new JSONStorableString("Type", config.Type);
            IsNegativeStorable = new JSONStorableBool("IsNegative", config.IsNegative);

            Multiplier1Storable = script.NewFloatSlider("Multiplier1", config.Multiplier1, -40, 40, "F2", true);
            Multiplier1Storable.slider.onValueChanged.AddListener((float val) =>
            {
                config.Multiplier1 = val;
            });

            ValueStorable = script.NewFloatSlider("value", config.Setting.val, config.Setting.min, config.Setting.max, "F2");
            ValueStorable.slider.interactable = false;

            Multiplier2Storable = script.NewFloatSlider("Multiplier2", config.Multiplier2, -40, 40, "F2", true);
            Multiplier2Storable.slider.onValueChanged.AddListener((float val) =>
            {
                config.Multiplier2 = val;
            });
        }
    }
}
