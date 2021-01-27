using UnityEngine;

namespace everlaster
{
    class GravityPhysicsConfig
    {
        public JSONStorableFloat setting;
        public string name;
        public string angleType;
        private float offset;
        private float offsetScaleMul;
        private float logMaxX;
        private float? scaleMul;
        private float? softMul;

        public GravityPhysicsConfig(string name, string angleType, float offset, float offsetScaleMul, float logMaxX, float? scaleMul, float? softMul)
        {
            this.name = name;
            this.angleType = angleType;
            this.offset = offset;
            this.offsetScaleMul = offsetScaleMul;
            this.logMaxX = logMaxX;
            this.scaleMul = scaleMul;
            this.softMul = softMul;
        }

        public void InitStorable()
        {
            setting = Globals.BREAST_CONTROL.GetFloatJSONParam(name);
            if(setting == null)
            {
                Log.Error($"BreastControl float param with name {name} not found!", nameof(GravityPhysicsConfig));
            }
        }

        public void UpdateVal(float effect, float scale, float softness)
        {
            float scaleFactor = scaleMul.HasValue ? (float) scaleMul * scale : 1;
            float softFactor = softMul.HasValue ? (float) softMul * softness : 1;
            float interpolatedEffect = Mathf.SmoothStep(0, ScaledSmoothMax(scale), effect);
            float value = (scaleFactor * interpolatedEffect / 2) + (softFactor * interpolatedEffect / 2);
            setting.val = offset + offsetScaleMul * scale + value;
        }

        public void Reset()
        {
            setting.val = 0;
        }

        private float ScaledSmoothMax(float scale)
        {
            if (logMaxX < 0)
            {
                return -Mathf.Log(scale * Mathf.Abs(logMaxX) + 1);
            }

            return Mathf.Log(scale * logMaxX + 1);
        }
    }
}
