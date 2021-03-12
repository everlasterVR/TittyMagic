using System.Collections.Generic;
using UnityEngine;

namespace TittyMagic
{
    internal class GravityPhysicsHandler
    {
        private HashSet<GravityPhysicsConfig> uprightPhysics;
        private HashSet<GravityPhysicsConfig> upsideDownPhysics;
        private HashSet<GravityPhysicsConfig> leanBackPhysics;
        private HashSet<GravityPhysicsConfig> leanForwardPhysics;
        private HashSet<GravityPhysicsConfig> rollLeftPhysics;
        private HashSet<GravityPhysicsConfig> rollRightPhysics;

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
            uprightPhysics = new HashSet<GravityPhysicsConfig>()
            {
                new GravityPhysicsConfig("targetRotationX",          1.00f,    0.40f,         -0.8f,       1.50f,      0.50f),
            };
            upsideDownPhysics = new HashSet<GravityPhysicsConfig>()
            {
                new GravityPhysicsConfig("targetRotationX",          1.00f,    0.40f,          1.6f,       1.50f,      0.50f),
            };
            leanBackPhysics = new HashSet<GravityPhysicsConfig>()
            {
                new GravityPhysicsConfig("centerOfGravityPercent",   0.40f,    0.05f,         -0.01f,      0.50f,      1.50f),
            };
            leanForwardPhysics = new HashSet<GravityPhysicsConfig>()
            {
                new GravityPhysicsConfig("centerOfGravityPercent",   0.40f,    0.05f,          0.05f,      0.50f,      1.50f),
            };
            rollLeftPhysics = new HashSet<GravityPhysicsConfig>()
            {
                new GravityPhysicsConfig("targetRotationY",          0f,       0f,             12f,        0.50f,      1.50f),
            };
            rollRightPhysics = new HashSet<GravityPhysicsConfig>()
            {
                new GravityPhysicsConfig("targetRotationY",          0f,       0f,            -12f,        0.50f,      1.50f),
            };

            foreach(var it in uprightPhysics)
                it.InitStorable();
            foreach(var it in upsideDownPhysics)
                it.InitStorable();
            foreach(var it in leanBackPhysics)
                it.InitStorable();
            foreach(var it in leanForwardPhysics)
                it.InitStorable();
            foreach(var it in rollLeftPhysics)
                it.InitStorable();
            foreach(var it in rollRightPhysics)
                it.InitStorable();
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
            AdjustPhysicsForPitch(Calc.RollFactor(roll));
        }

        public void ResetAll()
        {
            //foreach(var it in upsideDownPhysics)
            foreach(var it in uprightPhysics)
                it.Reset();
            //foreach(var it in leanBackPhysics)
            foreach(var it in leanForwardPhysics)
                it.Reset();
            //foreach(var it in rollRightPhysics)
            foreach(var it in rollLeftPhysics)
                it.Reset();
        }

        public string GetStatus()
        {
            string text = "\nGRAVITY PHYSICS\n";
            foreach(var it in uprightPhysics)
                text += it.GetStatus();
            foreach(var it in upsideDownPhysics)
                text += it.GetStatus();
            foreach(var it in leanBackPhysics)
                text += it.GetStatus();
            foreach(var it in leanForwardPhysics)
                text += it.GetStatus();
            foreach(var it in rollLeftPhysics)
                text += it.GetStatus();
            foreach(var it in rollRightPhysics)
                text += it.GetStatus();

            return text;
        }

        private void AdjustPhysicsForRoll()
        {
            // left
            if(roll >= 0)
            {
                Update(rollLeftPhysics, roll);
            }
            // right
            else
            {
                Update(rollRightPhysics, Mathf.Abs(roll), 1);
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
                    Update(leanForwardPhysics, pitch, rollFactor);
                    Update(uprightPhysics, 90 - pitch, rollFactor);
                }
                // upside down
                else
                {
                    Update(leanForwardPhysics, 180 - pitch, rollFactor);
                    Update(upsideDownPhysics, pitch - 90, rollFactor);
                }
            }
            // leaning back
            else
            {
                // upright
                if(pitch > -90)
                {
                    Update(leanBackPhysics, Mathf.Abs(pitch), rollFactor);
                    Update(uprightPhysics, 90 - Mathf.Abs(pitch), rollFactor);
                }
                // upside down
                else
                {
                    Update(leanBackPhysics, 180 - Mathf.Abs(pitch), rollFactor);
                    Update(upsideDownPhysics, Mathf.Abs(pitch) - 90, rollFactor);
                }
            }
        }

        private void Update(HashSet<GravityPhysicsConfig> physics, float angle, float rollFactor = 1f)
        {
            float effect = rollFactor * angle / 90;
            foreach(var it in physics)
            {
                it.UpdateVal(effect, scale, gravity);
            }
        }
    }
}
