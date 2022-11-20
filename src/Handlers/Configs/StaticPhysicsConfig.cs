using System;

namespace TittyMagic.Handlers.Configs
{
    public class StaticPhysicsConfig
    {
        public float baseValue { private get; set; } // value at min mass and min softness

        public Func<float, float> massCurve { private get; set; }
        public Func<float, float> softnessCurve { private get; set; }

        public StaticPhysicsConfig()
        {
            massCurve = x => 0;
            softnessCurve = x => 0;
        }

        public float Calculate(float mass, float softness) =>
            baseValue * (1 + massCurve(mass) + softnessCurve(softness));
    }
}
