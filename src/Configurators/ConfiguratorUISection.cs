namespace TittyMagic
{
    internal class ConfiguratorUISection
    {
        private string _name;
        private float _startingValue;
        private JSONStorableFloat _multiplierStorable;
        private JSONStorableFloat _valueStorable;

        public string Name => _name;
        public JSONStorableFloat MultiplierStorable => _multiplierStorable;
        public JSONStorableFloat ValueStorable => _valueStorable;

        public ConfiguratorUISection(MVRScript script, MorphConfig config)
        {
            _name = config.Name;
            _multiplierStorable = UI.NewFloatSlider(script, _name, config.SoftnessMultiplier, -3, 3, "F3");
            _valueStorable = UI.NewFloatSlider(script, "value", config.Morph.morphValue, -2, 2, "F3", true);
            _valueStorable.slider.interactable = false;
            _multiplierStorable.slider.onValueChanged.AddListener((float val) =>
            {
                config.SoftnessMultiplier = val;
            });
        }
    }
}
