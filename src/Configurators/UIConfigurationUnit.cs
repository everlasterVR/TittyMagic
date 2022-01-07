namespace TittyMagic
{
    internal class UIConfigurationUnit
    {
        private string _name;
        private float _startingValue;
        private JSONStorableFloat _multiplierStorable;
        private JSONStorableFloat _valueStorable;
        private PositionMorphConfig _config;

        public UIConfigurationUnit(string name, float startingValue = 0f)
        {
            _name = name;
            _startingValue = startingValue;
        }

        public void Init(MVRScript script)
        {
            _multiplierStorable = UI.NewFloatSlider(script, _name, _startingValue, -3, 3, "F3");
            _config = new PositionMorphConfig(_name, _multiplierStorable.val);
            _valueStorable = UI.NewFloatSlider(script, "value", _config.morph.morphValue, -2, 2, "F3", true);
            _multiplierStorable.slider.onValueChanged.AddListener((float val) =>
            {
                _config.SetSoftnessMul(val);
            });
        }

        public void UpdateMorphValue(float effect, float scale, float softness)
        {
            if(_config == null)
            {
                return;
            }
            var value = _config.UpdateVal(effect, scale, softness);
            _valueStorable.SetVal(value);
        }

        public void ResetMorphValue()
        {
            if(_config == null)
            {
                return;
            }
            _config.Reset();
            _valueStorable.SetVal(0);
        }
    }
}
