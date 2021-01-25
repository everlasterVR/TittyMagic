using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
                //                       name                       angle type                smoothMin smoothMax   scale   softness
                new GravityPhysicsConfig("centerOfGravityPercent",  AngleTypes.LEAN_BACK,     0f,      -0.12f,       0.05f,  0.95f),
                new GravityPhysicsConfig("centerOfGravityPercent",  AngleTypes.LEAN_FORWARD,  0f,       0.05f,       0.05f,  0.95f),
                new GravityPhysicsConfig("targetRotationX",         AngleTypes.UPRIGHT,       0,       -10f,        1.00f,  1.00f),
                new GravityPhysicsConfig("targetRotationX",         AngleTypes.UPSIDE_DOWN,   0,        20f,        1.00f,  1.00f),
                new GravityPhysicsConfig("targetRotationY",         AngleTypes.ROLL_LEFT,     0,        15f,        1.00f,  1.00f),
                new GravityPhysicsConfig("targetRotationY",         AngleTypes.ROLL_RIGHT,    0,       -15f,        1.00f,  1.00f),
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

        public void Reset(string type = "")
        {
            physics
                .Where(it => type == "" || it.angleType == type)
                .ToList().ForEach(it => it.Reset());
        }

        public string GetStatus()
        {
            string text = "\nGRAVITY PHYSICS\n";
            physics.ForEach((it) =>
            {
                text = text + Formatting.NameValueString(it.name, it.setting.val, padRight: 25) + "\n";
            });
            return text;
        }

        private void AdjustPhysicsForRoll()
        {
            // left
            if(roll >= 0)
            {
                DoAdjustForRoll(AngleTypes.ROLL_LEFT, Calc.Remap(roll, 1));
            }
            // right
            else
            {
                DoAdjustForRoll(AngleTypes.ROLL_RIGHT, Calc.Remap(Mathf.Abs(roll), 1));
            }
        }

        private void DoAdjustForRoll(string type, float effect)
        {
            physics
                .Where(it => it.angleType == type)
                .ToList().ForEach(it => it.UpdateVal(effect, scale, softness));
        }

        private void AdjustPhysicsForPitch(float rollFactor)
        {
            // leaning forward
            if(pitch > 0)
            {
                // upright
                if(pitch <= 90)
                {
                    DoAdjustForPitch(AngleTypes.LEAN_FORWARD, Calc.Remap(pitch, rollFactor), true);
                    DoAdjustForPitch(AngleTypes.UPRIGHT, Calc.Remap(90 - pitch, rollFactor));
                }
                // upside down
                else
                {
                    DoAdjustForPitch(AngleTypes.LEAN_FORWARD, Calc.Remap(180 - pitch, rollFactor), true);
                    DoAdjustForPitch(AngleTypes.UPSIDE_DOWN, Calc.Remap(pitch - 90, rollFactor));
                }
            }
            // leaning back
            else
            {
                // upright
                if(pitch > -90)
                {
                    DoAdjustForPitch(AngleTypes.LEAN_BACK, Calc.Remap(Mathf.Abs(pitch), rollFactor), true);
                    DoAdjustForPitch(AngleTypes.UPRIGHT, Calc.Remap(90 - Mathf.Abs(pitch), rollFactor));
                }
                // upside down
                else
                {
                    DoAdjustForPitch(AngleTypes.LEAN_BACK, Calc.Remap(180 - Mathf.Abs(pitch), rollFactor), true);
                    DoAdjustForPitch(AngleTypes.UPSIDE_DOWN, Calc.Remap(Mathf.Abs(pitch) - 90, rollFactor));
                }
            }
        }

        private void DoAdjustForPitch(string type, float effect, bool adjustForMidpoint = false)
        {
            physics
                .Where(it => it.angleType == type)
                .ToList().ForEach(it => it.UpdateVal(effect, scale, softness, adjustForMidpoint));
        }
    }
}
