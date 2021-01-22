using UnityEngine;

namespace everlaster
{
    class GravityPhysicsConfig
    {
        public JSONStorableFloat Setting { get; set; }
        public string Name { get; set; }
        public string AngleType { get; set; }
        public float Min { get; set; }
        public float Max { get; set; }
        public float? ScaleMultiplier { get; set; }
        public float? SoftnessMultiplier { get; set; }

        public GravityPhysicsConfig(string name, string angleType, float min, float max, float? scaleMultiplier, float? softnessMultiplier)
        {
            Name = name;
            AngleType = angleType;
            Min = min;
            Max = max;
            ScaleMultiplier = scaleMultiplier;
            SoftnessMultiplier = softnessMultiplier;
        }

        public void InitStorable()
        {
            Setting = Globals.BREAST_CONTROL.GetFloatJSONParam(Name);
            if(Setting == null)
            {
                Log.Error($"BreastControl float param with name {Name} not found!", nameof(GravityPhysicsConfig));
            }
        }

        public void UpdateVal(float effect, float scale, float softness)
        {
            float scaleFactor = ScaleMultiplier.HasValue ? (float) ScaleMultiplier * scale : 1;
            float softnessFactor = SoftnessMultiplier.HasValue ? (float) SoftnessMultiplier * softness : 1;
            float value = (scaleFactor * effect / 2) + (softnessFactor * effect / 2);

            Setting.SetVal(Mathf.SmoothStep(Min, Max, value));
        }
    }
}
