namespace everlaster
{
    class PhysicsConfig
    {
        public JSONStorableFloat Setting { get; set; }
        public string Name { get; set; }
        public string AngleType { get; set; }
        public float Min { get; set; }
        public float Max { get; set; }
        public float? ScaleMultiplier { get; set; }
        public float? SoftnessMultiplier { get; set; }

        public PhysicsConfig(string name, string angleType, float min, float max, float? scaleMultiplier, float? softnessMultiplier)
        {
            Name = name;
            AngleType = angleType;
            Min = min;
            Max = max;
            ScaleMultiplier = scaleMultiplier;
            SoftnessMultiplier = softnessMultiplier;
        }
    }
}
