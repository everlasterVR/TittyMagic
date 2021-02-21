using System.Collections.Generic;
using UnityEngine;

namespace TittyMagic
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
        private float gravity;

        public GravityPhysicsHandler()
        {
            // Right/left angle target moves both breasts in the same direction
            Globals.BREAST_CONTROL.invertJoint2RotationY = false;

            // offset = how much the calculated value is offset from zero (defines a midpoint for each of x, y and z center of gravity)
            // offsetScaleMul = multiplier for how much offset increases (on top of the base offset) based on scale
            // logMaxX = maximum x value for the logarithmic function that affects (along with breast mass) the Max value for Mathf.SmoothStep
            // scaleMul = the relative impact of breast mass on the final value
            // gravityMul = the relative impact of breast gravity on the final value
            //                           name                        offset    offsetScaleMul  logMaxX     scaleMul    gravityMul     
            uprightPhysics = new List<GravityPhysicsConfig>()
            {
                new GravityPhysicsConfig("targetRotationX",          1.00f,    0.40f,         -0.8f,       1.50f,      0.50f),
            };
            upsideDownPhysics = new List<GravityPhysicsConfig>()
            {
                new GravityPhysicsConfig("targetRotationX",          1.00f,    0.40f,          1.6f,       1.50f,      0.50f),
            };
            leanBackPhysics = new List<GravityPhysicsConfig>()
            {
                new GravityPhysicsConfig("centerOfGravityPercent",   0.40f,    0.05f,         -0.01f,      0.50f,      1.50f),
            };
            leanForwardPhysics = new List<GravityPhysicsConfig>()
            {
                new GravityPhysicsConfig("centerOfGravityPercent",   0.40f,    0.05f,          0.05f,      0.50f,      1.50f),
            };
            rollLeftPhysics = new List<GravityPhysicsConfig>()
            {
                new GravityPhysicsConfig("targetRotationY",          0f,       0f,             12f,        0.50f,      1.50f),
            };
            rollRightPhysics = new List<GravityPhysicsConfig>()
            {
                new GravityPhysicsConfig("targetRotationY",          0f,       0f,            -12f,        0.50f,      1.50f),
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
            float gravity
        )
        {
            this.roll = roll;
            this.pitch = pitch;
            this.scale = scale;
            this.gravity = gravity;

            AdjustPhysicsForRoll();
            AdjustPhysicsForPitch(AngleCalc.RollFactor(roll));
        }

        public void ResetAll()
        {
            uprightPhysics.ForEach(it => it.Reset());
            //upsideDownPhysics.ForEach(it => it.Reset());
            leanForwardPhysics.ForEach(it => it.Reset());
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
                Update(rollLeftPhysics, AngleCalc.Remap(roll, 1));
            }
            // right
            else
            {
                Update(rollRightPhysics, AngleCalc.Remap(Mathf.Abs(roll), 1));
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
                    Update(leanForwardPhysics, AngleCalc.Remap(pitch, rollFactor));
                    Update(uprightPhysics, AngleCalc.Remap(90 - pitch, rollFactor));
                }
                // upside down
                else
                {
                    Update(leanForwardPhysics, AngleCalc.Remap(180 - pitch, rollFactor));
                    Update(upsideDownPhysics, AngleCalc.Remap(pitch - 90, rollFactor));
                }
            }
            // leaning back
            else
            {
                // upright
                if(pitch > -90)
                {
                    Update(leanBackPhysics, AngleCalc.Remap(Mathf.Abs(pitch), rollFactor));
                    Update(uprightPhysics, AngleCalc.Remap(90 - Mathf.Abs(pitch), rollFactor));
                }
                // upside down
                else
                {
                    Update(leanBackPhysics, AngleCalc.Remap(180 - Mathf.Abs(pitch), rollFactor));
                    Update(upsideDownPhysics, AngleCalc.Remap(Mathf.Abs(pitch) - 90, rollFactor));
                }
            }
        }

        private void Update(List<GravityPhysicsConfig> physics, float effect)
        {
            physics.ForEach(it => it.UpdateVal(effect, scale, gravity));
        }
    }
}
