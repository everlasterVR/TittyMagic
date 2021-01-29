using System.Collections.Generic;
using UnityEngine;

namespace everlaster
{
    class GravityPhysicsHandler
    {
        private List<GravityPhysicsConfig> uprightPhysics;
        private List<GravityPhysicsConfig> upsideDownPhysics;
        private List<GravityPhysicsConfig> leanBackPhysics;
        private List<GravityPhysicsConfig> leanForwardPhysics;
        private List<GravityPhysicsConfig> rollLeftPhysics;
        private List<GravityPhysicsConfig> rollRightPhysics;

        private float roll;
        private float pitch;
        private float scale;
        private float softness;

        public GravityPhysicsHandler()
        {
            // Right/left angle target moves both breasts in the same direction
            Globals.BREAST_CONTROL.invertJoint2RotationY = false;

            // offset = how much the calculated value is offset from zero (defines a midpoint for each of x, y and z center of gravity)
            // offsetScaleMul = multiplier for how much offset increases (on top of the base offset) based on scale
            // logMaxX = maximum x value for the logarithmic function that affects (along with breast mass) the Max value for Mathf.SmoothStep
            // scaleMul = the relative impact of breast mass on the final value
            // softMul = the relative impact of breast softness on the final value
            //                           name                       offset    offsetScaleMul  logMaxX     scaleMul    softMul     
            uprightPhysics = new List<GravityPhysicsConfig>()
            {
                new GravityPhysicsConfig("targetRotationX",          0.10f,    0.40f,         -1.8f,       1.50f,      0.50f),
            };
            upsideDownPhysics = new List<GravityPhysicsConfig>()
            {
                new GravityPhysicsConfig("targetRotationX",          0.10f,    0.40f,          6.0f,       0.20f,      1.80f),
            };
            leanBackPhysics = new List<GravityPhysicsConfig>()
            {
                new GravityPhysicsConfig("centerOfGravityPercent",   0.30f,    0.09f,         -0.02f,      0.00f,      2.00f),
            };
            leanForwardPhysics = new List<GravityPhysicsConfig>()
            {
                new GravityPhysicsConfig("centerOfGravityPercent",   0.36f,    0.08f,          0.03f,      0.00f,      2.00f),
            };
            rollLeftPhysics = new List<GravityPhysicsConfig>()
            {
                new GravityPhysicsConfig("targetRotationY",          0f,       0f,             12f,        0.20f,      1.80f),
            };
            rollRightPhysics = new List<GravityPhysicsConfig>()
            {
                new GravityPhysicsConfig("targetRotationY",          0f,       0f,            -12f,        0.20f,      1.80f),
            };

            uprightPhysics.ForEach(it => it.InitStorable());
            upsideDownPhysics.ForEach(it => it.InitStorable());
            leanBackPhysics.ForEach(it => it.InitStorable());
            leanForwardPhysics.ForEach(it => it.InitStorable());
            rollLeftPhysics.ForEach(it => it.InitStorable());
            rollRightPhysics.ForEach(it => it.InitStorable());
            ResetAll();
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

        public void ResetAll()
        {
            uprightPhysics.ForEach(it => it.Reset());
            //upsideDownPhysics.ForEach(it => it.Reset());
            leanBackPhysics.ForEach(it => it.Reset());
            //leanBackPhysics.ForEach(it => it.Reset());
            rollLeftPhysics.ForEach(it => it.Reset());
            //rollRightPhysics.ForEach(it => it.Reset());
        }

        public string GetStatus()
        {
            string text = "\nGRAVITY PHYSICS\n";
            uprightPhysics.ForEach((it) => text = text + it.GetStatus());
            upsideDownPhysics.ForEach((it) => text = text + it.GetStatus());
            leanBackPhysics.ForEach((it) => text = text + it.GetStatus());
            leanForwardPhysics.ForEach((it) => text = text + it.GetStatus());
            rollLeftPhysics.ForEach((it) => text = text + it.GetStatus());
            rollRightPhysics.ForEach((it) => text = text + it.GetStatus());
            return text;
        }

        private void AdjustPhysicsForRoll()
        {
            // left
            if(roll >= 0)
            {
                Update(rollLeftPhysics, Calc.Remap(roll, 1));
            }
            // right
            else
            {
                Update(rollRightPhysics, Calc.Remap(Mathf.Abs(roll), 1));
            }
        }

        private void AdjustPhysicsForPitch(float rollFactor)
        {
            // leaning forward
            if(pitch > 0)
            {
                // upright
                if(pitch <= 90)
                {
                    Update(leanForwardPhysics, Calc.Remap(pitch, rollFactor));
                    Update(uprightPhysics, Calc.Remap(90 - pitch, rollFactor));
                }
                // upside down
                else
                {
                    Update(leanForwardPhysics, Calc.Remap(180 - pitch, rollFactor));
                    Update(upsideDownPhysics, Calc.Remap(pitch - 90, rollFactor));
                }
            }
            // leaning back
            else
            {
                // upright
                if(pitch > -90)
                {
                    Update(leanBackPhysics, Calc.Remap(Mathf.Abs(pitch), rollFactor));
                    Update(uprightPhysics, Calc.Remap(90 - Mathf.Abs(pitch), rollFactor));
                }
                // upside down
                else
                {
                    Update(leanBackPhysics, Calc.Remap(180 - Mathf.Abs(pitch), rollFactor));
                    Update(upsideDownPhysics, Calc.Remap(Mathf.Abs(pitch) - 90, rollFactor));
                }
            }
        }

        private void Update(List<GravityPhysicsConfig> physics, float effect)
        {
            physics.ForEach(it => it.UpdateVal(effect, scale, softness));
        }
    }
}
