using UnityEngine;

namespace everlaster
{
    class GravityPhysicsConfig
    {
        public JSONStorableFloat setting;
        public string name;
        public string angleType;
        private float smoothMin;
        private float smoothMax;
        private float? scaleMultiplier;
        private float? softnessMultiplier;
        private float centerOfGravityMidpoint = 0.5f;

        public GravityPhysicsConfig(string name, string angleType, float smoothMin, float smoothMax, float? scaleMultiplier, float? softnessMultiplier)
        {
            this.name = name;
            this.angleType = angleType;
            this.smoothMin = smoothMin;
            this.smoothMax = smoothMax;
            this.scaleMultiplier = scaleMultiplier;
            this.softnessMultiplier = softnessMultiplier;
        }

        public void InitStorable()
        {
            setting = Globals.BREAST_CONTROL.GetFloatJSONParam(name);
            if(setting == null)
            {
                Log.Error($"BreastControl float param with name {name} not found!", nameof(GravityPhysicsConfig));
            }
        }

        public void UpdateVal(float effect, float scale, float softness, bool adjustForMidpoint = false)
        {
            float scaleFactor = scaleMultiplier.HasValue ? (float) scaleMultiplier * scale : 1;
            float softnessFactor = softnessMultiplier.HasValue ? (float) softnessMultiplier * softness : 1;
            float interpolatedEffect = Mathf.SmoothStep(smoothMin, ScaledSmoothMax(scale), effect);
            float value = (scaleFactor * interpolatedEffect / 2) + (softnessFactor * interpolatedEffect / 2);
            if (adjustForMidpoint)
            {
                value = centerOfGravityMidpoint + value;
            }
            setting.val = value;
        }

        public void Reset()
        {
            setting.val = 0;
        }

        private float ScaledSmoothMax(float scale)
        {
            if (smoothMax < 0)
            {
                return -Mathf.Log(scale * Mathf.Abs(smoothMax) + 1);
            }

            return Mathf.Log(scale * smoothMax + 1);
        }
    }
}
