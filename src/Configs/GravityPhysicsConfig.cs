using UnityEngine;

namespace TittyMagic
{
    internal class GravityPhysicsConfig
    {
        private readonly Log log = new Log(nameof(GravityPhysicsConfig));
        public JSONStorableFloat setting;
        public string name;
        private float offset;
        private float offsetScaleMul;
        private float logMaxX;
        private float? scaleMul;
        private float? gravityMul;
        private float originalValue;

        public GravityPhysicsConfig(string name, float offset, float offsetScaleMul, float logMaxX, float? scaleMul, float? gravityMul)
        {
            this.name = name;
            this.offset = offset;
            this.offsetScaleMul = offsetScaleMul;
            this.logMaxX = logMaxX;
            this.scaleMul = scaleMul;
            this.gravityMul = gravityMul;
        }

        public void InitStorable()
        {
            setting = Globals.BREAST_CONTROL.GetFloatJSONParam(name);
            if(setting == null)
            {
                log.Error($"BreastControl float param with name {name} not found!");
            }

            originalValue = setting.val;
        }

        public void UpdateVal(float effect, float scale, float gravity)
        {
            float scaleFactor = scaleMul.HasValue ? (float) scaleMul * scale : 1;
            float gravityFactor = gravityMul.HasValue ? (float) gravityMul * gravity : 1;
            float interpolatedEffect = Mathf.SmoothStep(0, ScaledSmoothMax(scale), effect);
            float value = (scaleFactor * interpolatedEffect / 2) + (gravityFactor * interpolatedEffect / 2);
            setting.val = offset + offsetScaleMul * scale + value;
        }

        public void Reset()
        {
            setting.val = originalValue;
        }

        public string GetStatus()
        {
            return Formatting.NameValueString(name, setting.val, padRight: 25) + "\n";
        }

        private float ScaledSmoothMax(float scale)
        {
            if(logMaxX < 0)
            {
                return -Mathf.Log(scale * Mathf.Abs(logMaxX) + 1);
            }

            return Mathf.Log(scale * logMaxX + 1);
        }
    }
}
