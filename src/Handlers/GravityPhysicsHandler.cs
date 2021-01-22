using System.Collections.Generic;
using System.Linq;

namespace everlaster
{
    class GravityPhysicsHandler
    {
        private List<GravityPhysicsConfig> physics;

        private float roll;
        private float pitch;
        private float scale;
        private float softness;

        public GravityPhysicsHandler()
        {
            physics = new List<GravityPhysicsConfig>()
            {
                //                       name                       angle type           min     max     scale   softness
                new GravityPhysicsConfig("centerOfGravityPercent",  AngleTypes.PITCH,    0.40f,  0.574f, 1f,     null),
                new GravityPhysicsConfig("targetRotationX",         AngleTypes.PITCH,    0f,     8f,     2f,     2f),
                new GravityPhysicsConfig("targetRotationY",         AngleTypes.ROLL,     0f,     8f,     2f,     2f),
            };
            physics.ForEach(it => it.InitStorable());
        }

        public void Update(
            float roll,
            float pitch,
            float scale,
            float softness
        )
        {
            this.roll = roll;
            this.pitch = pitch;
            this.scale = scale;
            this.softness = softness;

            AdjustPhysicsForRoll();
            AdjustPhysicsForPitch(Calc.RollFactor(roll));
        }

        public string GetStatus()
        {
            string text = "\nGRAVITY PHYSICS\n";
            physics.ForEach((it) =>
            {
                text = text + Formatting.NameValueString(it.Name, it.Setting.val, padRight: 25) + "\n";
            });
            return text;
        }

        private void AdjustPhysicsForRoll()
        {
            float effect = Calc.Remap(roll, 1);
            physics
                .Where(it => it.AngleType == AngleTypes.ROLL)
                .ToList().ForEach(it => it.UpdateVal(effect, scale, softness));
        }

        private void AdjustPhysicsForPitch(float rollFactor)
        {
            float effect = Calc.Remap(pitch, rollFactor);
            physics
                .Where(it => it.AngleType == AngleTypes.PITCH)
                .ToList().ForEach(it => it.UpdateVal(effect, scale, softness));
        }
    }
}
