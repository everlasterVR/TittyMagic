using UnityEngine;

namespace TittyMagic
{
    internal class GravityPhysicsConfig
    {
        public JSONStorableFloat setting;
        public string name;
        private float offset;
        private float logMaxX;
        private float scaleMul;
        private float gravityMul;
        private float originalValue;

        public GravityPhysicsConfig(string name, float offset, float logMaxX, float scaleMul, float gravityMul)
        {
            this.name = name;
            this.offset = offset;
            this.logMaxX = logMaxX;
            this.scaleMul = scaleMul;
            this.gravityMul = gravityMul;
        }

        public void InitStorable()
        {
            setting = Globals.BREAST_CONTROL.GetFloatJSONParam(name);
            if(setting == null)
            {
                Log.Error($"BreastControl float param with name {name} not found!", nameof(GravityPhysicsConfig));
            }

            originalValue = setting.val;
        }

        public void UpdateVal(float effect, float combinedScaleSoftness, float gravity)
        {
            float interpolatedEffect = Mathf.SmoothStep(0, Calc.ScaledSmoothMax(combinedScaleSoftness, logMaxX), effect);
            float value =
                (scaleMul * combinedScaleSoftness * interpolatedEffect / 2) +
                (gravityMul * gravity * interpolatedEffect / 2);
            setting.val = offset + value;
        }

        public void Reset()
        {
            setting.val = originalValue;
        }

        public string GetStatus()
        {
            return Formatting.NameValueString(name, setting.val, padRight: 25) + "\n";
        }
    }
}
