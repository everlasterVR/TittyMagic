using System.Collections.Generic;
using UnityEngine;

namespace everlaster
{
    class GravityMorphHandler
    {
        private List<BasicMorphConfig> gravityOffsetMorphs;
        private List<GravityMorphConfig> uprightMorphs;
        private List<GravityMorphConfig> upsideDownMorphs;
        private List<GravityMorphConfig> leanBackMorphs;
        private List<GravityMorphConfig> leanForwardMorphs;
        private List<GravityMorphConfig> rollLeftMorphs;
        private List<GravityMorphConfig> rollRightMorphs;

        private float roll;
        private float pitch;
        private float scale;
        private float gravity;

        public GravityMorphHandler()
        {
            InitGravityOffsetMorphs();
            InitGravityMorphs();
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

            gravityOffsetMorphs.ForEach(it => it.UpdateVal());
            AdjustMorphsForRoll();
            AdjustMorphsForPitch(Calc.RollFactor(roll));
        }

        public void ResetAll()
        {
            gravityOffsetMorphs.ForEach(it => it.Reset());
            uprightMorphs.ForEach(it => it.Reset());
            upsideDownMorphs.ForEach(it => it.Reset());
            leanBackMorphs.ForEach(it => it.Reset());
            leanForwardMorphs.ForEach(it => it.Reset());
            rollLeftMorphs.ForEach(it => it.Reset());
            rollRightMorphs.ForEach(it => it.Reset());
        }

        public string GetStatus()
        {
            string text = "OFFSET\n";
            gravityOffsetMorphs.ForEach((it) => text = text + it.GetStatus());
            text = text + "\nUPRIGHT\n";
            uprightMorphs.ForEach((it) => text = text + it.GetStatus());
            text = text + "\nUPSIDE DOWN\n";
            upsideDownMorphs.ForEach((it) => text = text + it.GetStatus());
            text = text + "\nLEAN BACK\n";
            leanBackMorphs.ForEach((it) => text = text + it.GetStatus());
            text = text + "\nLEAN FORWARD\n";
            leanForwardMorphs.ForEach((it) => text = text + it.GetStatus());
            text = text + "\nROLL LEFT\n";
            rollLeftMorphs.ForEach((it) => text = text + it.GetStatus());
            text = text + "\nROLL RIGHT\n";
            rollRightMorphs.ForEach((it) => text = text + it.GetStatus());
            return text;
        }

        private void InitGravityOffsetMorphs()
        {
            gravityOffsetMorphs = new List<BasicMorphConfig>
            {
                new BasicMorphConfig("TM_UprightSmootherOffset",      1f),
            };
        }

        private void InitGravityMorphs()
        {

            uprightMorphs = new List<GravityMorphConfig>
            {
                //                      name                            baseMul    scaleMul   gravityMul
                new GravityMorphConfig("TM_Upright1",                   1.00f,     1.00f,     1.00f),
                new GravityMorphConfig("TM_Upright2",                   2.00f,     1.20f,     0.80f),
                new GravityMorphConfig("TM_UprightSmoother",           -2.00f,     1.50f,     0.50f),
                new GravityMorphConfig("TM_Upright3",                   2.00f,     0.00f,     2.00f),
            };

            upsideDownMorphs = new List<GravityMorphConfig> {
                new GravityMorphConfig("TM_UpsideDown1",                1.40f,     0.00f,     2.00f),
                new GravityMorphConfig("TM_UpsideDown2",                1.40f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_UpsideDown3",                1.00f,     1.00f,     1.00f),
                new GravityMorphConfig("TM_UpsideDown4",                1.40f,     1.00f,     0.00f),
                new GravityMorphConfig("TM_UpsideDown5",                1.40f,     1.50f,     0.50f),
                new GravityMorphConfig("TM_UpsideDown6",                1.40f,    -0.50f,     2.00f),
            };

            leanBackMorphs = new List<GravityMorphConfig>
            {
                new GravityMorphConfig("TM_LeanBack1",                  1.00f,     0.00f,     2.00f),
                new GravityMorphConfig("TM_LeanBack2",                  1.33f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_LeanBack3",                  1.00f,     1.00f,     1.00f),
            };

            leanForwardMorphs = new List<GravityMorphConfig> {
                new GravityMorphConfig("TM_LeanForward1",               1.20f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_LeanForward2",               1.20f,     1.50f,     0.50f),
                new GravityMorphConfig("TM_LeanForward3",               1.33f,     1.00f,     1.00f),
                new GravityMorphConfig("TM_LeanForward4",               1.33f,    -0.50f,     2.00f),
                new GravityMorphConfig("TM_LeanForward5",               1.20f,    -1.00f,     2.00f),
            };

            rollLeftMorphs = new List<GravityMorphConfig>
            {
                new GravityMorphConfig("TM_RollLeft1",                  1.40f,     0.00f,     2.00f),
                new GravityMorphConfig("TM_RollLeft2",                  1.40f,     2.00f,     0.00f),
            };

            rollRightMorphs = new List<GravityMorphConfig>
            {
                new GravityMorphConfig("TM_RollRight1",                  1.40f,     0.00f,     2.00f),
                new GravityMorphConfig("TM_RollRight2",                  1.40f,     2.00f,     0.00f),
            };
        }

        private void AdjustMorphsForRoll()
        {
            // left
            if(roll >= 0)
            {
                Reset(rollRightMorphs);
                Update(rollLeftMorphs, Calc.Remap(roll, 1));
            }
            // right
            else
            {
                Reset(rollLeftMorphs);
                Update(rollRightMorphs, Calc.Remap(Mathf.Abs(roll), 1));
            }
        }

        private void AdjustMorphsForPitch(float rollFactor)
        {
            // leaning forward
            if(pitch > 0)
            {
                Reset(leanBackMorphs);
                // upright
                if(pitch <= 90)
                {
                    Reset(upsideDownMorphs);
                    Update(leanForwardMorphs, Calc.Remap(pitch, rollFactor));
                    Update(uprightMorphs, Calc.Remap(90 - pitch, rollFactor));
                }
                // upside down
                else
                {
                    Reset(uprightMorphs);
                    Update(leanForwardMorphs, Calc.Remap(180 - pitch, rollFactor));
                    Update(upsideDownMorphs, Calc.Remap(pitch - 90, rollFactor));
                }
            }
            // leaning back
            else
            {
                Reset(leanForwardMorphs);
                // upright
                if(pitch > -90)
                {
                    Reset(upsideDownMorphs);
                    Update(leanBackMorphs, Calc.Remap(Mathf.Abs(pitch), rollFactor));
                    Update(uprightMorphs, Calc.Remap(90 - Mathf.Abs(pitch), rollFactor));
                }
                // upside down
                else
                {
                    Reset(uprightMorphs);
                    Update(leanBackMorphs, Calc.Remap(180 - Mathf.Abs(pitch), rollFactor));
                    Update(upsideDownMorphs, Calc.Remap(Mathf.Abs(pitch) - 90, rollFactor));
                }
            }
        }

        private void Update(List<GravityMorphConfig> morphs, float effect)
        {
            morphs.ForEach(it => it.UpdateVal(effect, scale, gravity));
        }

        private void Reset(List<GravityMorphConfig> morphs)
        {
            morphs.ForEach(it => it.Reset());
        }
    }
}
