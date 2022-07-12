using TittyMagic.Configs;

namespace TittyMagic
{
    internal class NippleErectionHandler
    {
        private readonly Script _script;
        private readonly MorphConfigBase _morphConfig;
        public JSONStorableFloat nippleErectionJsf { get; }

        public NippleErectionHandler(Script script)
        {
            _script = script;
            _morphConfig = new MorphConfigBase("TM_NippleErection", 1);
            nippleErectionJsf = _script.NewJSONStorableFloat("nippleErection", 0.00f, 0.00f, 1.00f);
            nippleErectionJsf.setCallbackFunction = _ => Update();
        }

        public void Update()
        {
            _morphConfig.morph.morphValue = nippleErectionJsf.val * _morphConfig.multiplier;
            if(_script.settingsMonitor.softPhysicsEnabled)
            {
                _script.softPhysicsHandler.UpdateNipplePhysics(nippleErectionJsf.val);
            }
        }

        public void Reset()
        {
            _morphConfig.morph.morphValue = 0;
        }
    }
}
