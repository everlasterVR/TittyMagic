using SimpleJSON;

namespace TittyMagic
{
    internal class UIConfigurationUnit
    {
        private string _name;
        private float _startingValue;
        private JSONStorableFloat _multiplierStorable;
        private JSONStorableFloat _valueStorable;
        private PositionMorphConfig _config;
        public string Name => _name;

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
                _config.softnessMul = val;
            });
        }

        public float GetMultiplier()
        {
            return _multiplierStorable.val;
        }

        public void SetMultiplier(float val)
        {
            _multiplierStorable.val = val;
        }

        public void UpdateMorphValue(float effect, float mass, float softness)
        {
            if(_config == null)
            {
                return;
            }
            var value = _config.UpdateVal(effect, mass, softness);
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
