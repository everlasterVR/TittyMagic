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
        private float softness;
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
            //                           name                        offset    logMaxX     scaleMul    gravityMul
            uprightPhysics = new HashSet<GravityPhysicsConfig>()
            {
                new GravityPhysicsConfig("targetRotationX",          0.00f,   -20f,        1.00f,      1.00f),
            };
            upsideDownPhysics = new HashSet<GravityPhysicsConfig>()
            {
                new GravityPhysicsConfig("targetRotationX",          0.00f,    5f,         1.00f,      1.00f),
            };
            leanBackPhysics = new HashSet<GravityPhysicsConfig>()
            {
                new GravityPhysicsConfig("centerOfGravityPercent",   0.30f,   -0.01f,      1.00f,      0f),
            };
            leanForwardPhysics = new HashSet<GravityPhysicsConfig>()
            {
                new GravityPhysicsConfig("centerOfGravityPercent",   0.30f,    0.05f,      1.00f,      0f),
            };
            rollLeftPhysics = new HashSet<GravityPhysicsConfig>()
            {
                new GravityPhysicsConfig("targetRotationY",          0f,       12f,        1.00f,      0f),
            };
            rollRightPhysics = new HashSet<GravityPhysicsConfig>()
            {
                new GravityPhysicsConfig("targetRotationY",          0f,      -12f,        1.00f,      0f),
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
            float softness,
            float gravity
        )
        {
            this.roll = roll;
            this.pitch = pitch;
            this.scale = scale;
            this.softness = softness;
            this.gravity = gravity;

            AdjustPhysicsForRoll();
            AdjustPhysicsForPitch();
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
                UpdateRollPhysics(rollLeftPhysics, roll);
            }
            // right
            else
            {
                UpdateRollPhysics(rollRightPhysics, -roll);
            }
        }

        private void AdjustPhysicsForPitch()
        {
            // leaning forward
            if(pitch >= 0)
            {
                // upright
                if(pitch < 1)
                {
                    UpdatePitchPhysics(leanForwardPhysics, pitch);
                    UpdatePitchPhysics(uprightPhysics, 1 - pitch, softness);
                }
                // upside down
                else
                {
                    UpdatePitchPhysics(leanForwardPhysics, 2 - pitch);
                    UpdatePitchPhysics(upsideDownPhysics, pitch - 1, softness);
                }
            }
            // leaning back
            else
            {
                // upright
                if(pitch >= -1)
                {
                    UpdatePitchPhysics(leanBackPhysics, -pitch);
                    UpdatePitchPhysics(uprightPhysics, 1 + pitch, softness);
                }
                // upside down
                else
                {
                    UpdatePitchPhysics(leanBackPhysics, 2 + pitch);
                    UpdatePitchPhysics(upsideDownPhysics, -pitch - 1, softness);
                }
            }
        }

        private void UpdateRollPhysics(HashSet<GravityPhysicsConfig> configs, float effect)
        {
            foreach(var it in configs)
                it.UpdateVal(effect, scale, gravity);
        }

        private void UpdatePitchPhysics(HashSet<GravityPhysicsConfig> configs, float effect, float softness = 1f)
        {
            foreach(var it in configs)
                it.UpdateVal(effect * (1 - Mathf.Abs(roll)), scale * softness, gravity);
        }
    }
}
