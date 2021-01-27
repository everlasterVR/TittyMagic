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
                // offset = how much the calculated value is offset from zero (defines a midpoint for each of x, y and z center of gravity)
                // offsetScaleMul = multiplier for how much offset increases (on top of the base offset) based on scale
                // logMaxX = maximum x value for the logarithmic function that affects (along with breast mass) the Max value for Mathf.SmoothStep
                // scaleMul = the relative impact of breast mass on the final value
                // softMul = the relative impact of breast softness on the final value
                //                       name                       angle type                offset    offsetScaleMul  logMaxX     scaleMul    softMul     
                new GravityPhysicsConfig("centerOfGravityPercent",  AngleTypes.LEAN_BACK,     0.34f,    0.040f,        -0.02f,      0.00f,      2.00f),
                new GravityPhysicsConfig("centerOfGravityPercent",  AngleTypes.LEAN_FORWARD,  0.34f,    0.040f,         0.04f,      0.00f,      2.00f),
                new GravityPhysicsConfig("targetRotationX",         AngleTypes.UPRIGHT,      -2.00f,    0.50f,         -2f,         0.20f,      1.80f),
                new GravityPhysicsConfig("targetRotationX",         AngleTypes.UPSIDE_DOWN,  -2.00f,    0.50f,          20f,        0.20f,      1.80f),
                new GravityPhysicsConfig("targetRotationY",         AngleTypes.ROLL_LEFT,     0f,       0f,             15f,        0.40f,      1.60f),
                new GravityPhysicsConfig("targetRotationY",         AngleTypes.ROLL_RIGHT,    0f,       0f,            -15f,        0.40f,      1.60f),
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
                    DoAdjustForPitch(AngleTypes.LEAN_FORWARD, Calc.Remap(pitch, rollFactor));
                    DoAdjustForPitch(AngleTypes.UPRIGHT, Calc.Remap(90 - pitch, rollFactor));
                }
                // upside down
                else
                {
                    DoAdjustForPitch(AngleTypes.LEAN_FORWARD, Calc.Remap(180 - pitch, rollFactor));
                    DoAdjustForPitch(AngleTypes.UPSIDE_DOWN, Calc.Remap(pitch - 90, rollFactor));
                }
            }
            // leaning back
            else
            {
                // upright
                if(pitch > -90)
                {
                    DoAdjustForPitch(AngleTypes.LEAN_BACK, Calc.Remap(Mathf.Abs(pitch), rollFactor));
                    DoAdjustForPitch(AngleTypes.UPRIGHT, Calc.Remap(90 - Mathf.Abs(pitch), rollFactor));
                }
                // upside down
                else
                {
                    DoAdjustForPitch(AngleTypes.LEAN_BACK, Calc.Remap(180 - Mathf.Abs(pitch), rollFactor));
                    DoAdjustForPitch(AngleTypes.UPSIDE_DOWN, Calc.Remap(Mathf.Abs(pitch) - 90, rollFactor));
                }
            }
        }

        private void DoAdjustForPitch(string type, float effect)
        {
            physics
                .Where(it => it.angleType == type)
                .ToList().ForEach(it => it.UpdateVal(effect, scale, softness));
        }
    }
}
