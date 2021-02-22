using System.Collections.Generic;
using UnityEngine;

namespace TittyMagic
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
                //new BasicMorphConfig("UPR_Breast Under Smoother1",    0.06f), // TODO copy
                //new BasicMorphConfig("UPR_Breast Under Smoother3",    0.12f),
                //new BasicMorphConfig("UPR_Breast Under Smoother4",    0.06f),
            };
        }

        private void InitGravityMorphs()
        {

            uprightMorphs = new List<GravityMorphConfig>
            {
                //                      name                            baseMul    scaleMul   gravityMul
                new GravityMorphConfig("TM_Upright1",                   1.00f,     1.00f,     1.00f),
                //new GravityMorphConfig("UPR_Breast Move Down L",        0.25f,     1.50f,     0.50f),
                //new GravityMorphConfig("UPR_Breast Move Down R",        0.25f,     1.50f,     0.50f),
                //new GravityMorphConfig("UPR_Chest Height",              0.20f,     1.50f,     0.50f),

                new GravityMorphConfig("TM_Upright2",                   2.00f,     1.20f,     0.80f),
                //new GravityMorphConfig("UPR_Breast Rotate Up",          0.15f,     1.20f,     0.80f),

                new GravityMorphConfig("TM_UprightSmoother",           -2.00f,     1.50f,     0.50f),
                //new GravityMorphConfig("UPR_Breast Under Smoother1",   -0.12f,     1.50f,     0.50f),
                //new GravityMorphConfig("UPR_Breast Under Smoother3",   -0.24f,     1.50f,     0.50f),
                //new GravityMorphConfig("UPR_Breast Under Smoother4",   -0.12f,     1.50f,     0.50f),

                new GravityMorphConfig("TM_Upright3",                   2.00f,     0.00f,     2.00f),
                //new GravityMorphConfig("UPR_Breasts Natural",           0.05f,     0.00f,     2.00f),
            };

            upsideDownMorphs = new List<GravityMorphConfig> {
                new GravityMorphConfig("TM_UpsideDown1",                1.40f,     0.00f,     2.00f),
                //new GravityMorphConfig("UPSD_Areola UpDown",           -0.25f,     0.00f,     2.00f),
                //new GravityMorphConfig("UPSD_Breast Diameter L",        0.10f,     0.00f,     2.00f),
                //new GravityMorphConfig("UPSD_Breast Diameter R",        0.10f,     0.00f,     2.00f),
                //new GravityMorphConfig("UPSD_Breast Height",            0.36f,     0.00f,     2.00f),
                //new GravityMorphConfig("UPSD_Breast Height Upper",      0.04f,     0.00f,     2.00f),
                //new GravityMorphConfig("UPSD_Breast Move Up",           0.10f,     0.00f,     2.00f),
                //new GravityMorphConfig("UPSD_Breast Sag1",             -0.03f,     0.00f,     2.00f),
                //new GravityMorphConfig("UPSD_Breast Sag2",             -0.15f,     0.00f,     2.00f),
                //new GravityMorphConfig("UPSD_Breasts Hang Forward",     0.05f,     0.00f,     2.00f),
                //new GravityMorphConfig("UPSD_Breasts Natural",         -0.05f,     0.00f,     2.00f),

                new GravityMorphConfig("TM_UpsideDown2",                1.40f,     0.50f,     1.50f),
                //new GravityMorphConfig("UPSD_Breast flat",              0.08f,     0.50f,     1.50f),
                //new GravityMorphConfig("UPSD_Breast Rotate Up",         0.25f,     0.50f,     1.50f),
                //new GravityMorphConfig("UPSD_Breast Under Smoother1",   0.33f,     0.50f,     1.50f),
                //new GravityMorphConfig("UPSD_Breast Under Smoother2",   0.20f,     0.50f,     1.50f),
                //new GravityMorphConfig("UPSD_Breast Under Smoother3",  -0.25f,     0.50f,     1.50f),
                //new GravityMorphConfig("UPSD_Breast Under Smoother4",   0.05f,     0.50f,     1.50f),

                new GravityMorphConfig("TM_UpsideDown3",                1.00f,     1.00f,     1.00f),
                //new GravityMorphConfig("UPSD_Breast Diameter",          0.05f,     1.00f,     1.00f),
                //new GravityMorphConfig("UPSD_Breasts Flatten",          0.05f,     1.00f,     1.00f),
                //new GravityMorphConfig("UPSD_Breasts Height",           0.05f,     1.00f,     1.00f),
                //new GravityMorphConfig("UPSD_Breasts Implants",        -0.05f,     1.00f,     1.00f),
                //new GravityMorphConfig("UPSD_Breasts Upward Slope",     0.15f,     1.00f,     1.00f),
                //new GravityMorphConfig("UPSD_Center Gap Depth",         0.05f,     1.00f,     1.00f),
                //new GravityMorphConfig("UPSD_Center Gap Height",        0.10f,     1.00f,     1.00f),
                //new GravityMorphConfig("UPSD_Center Gap UpDown",        0.10f,     1.00f,     1.00f),
                //new GravityMorphConfig("UPSD_Chest Height",            -0.07f,     1.00f,     1.00f),
                //new GravityMorphConfig("UPSD_Chest Smoother",           0.10f,     1.00f,     1.00f),
                //new GravityMorphConfig("UPSD_ChestUnderBreast",        -0.15f,     1.00f,     1.00f),
                //new GravityMorphConfig("UPSD_ChestUp",                  0.10f,     1.00f,     1.00f),

                new GravityMorphConfig("TM_UpsideDown4",                1.40f,     1.00f,     0.00f),
                //new GravityMorphConfig("UPSD_Breast Pointed",           0.25f,     1.00f,     0.00f),

                new GravityMorphConfig("TM_UpsideDown5",                1.40f,     1.50f,     0.50f),
                //new GravityMorphConfig("UPSD_Breast Top Curve1",       -0.30f,     1.50f,     0.50f),
                //new GravityMorphConfig("UPSD_Breast Top Curve2",       -0.75f,     1.50f,     0.50f),
                //new GravityMorphConfig("UPSD_BreastsShape2",            0.30f,     1.50f,     0.50f),
                
                new GravityMorphConfig("TM_UpsideDown6",                1.40f,    -0.50f,     2.00f),
                //new GravityMorphConfig("UPSD_Breasts TogetherApart",    0.12f,    -1.00f,     1.00f),

            };

            leanBackMorphs = new List<GravityMorphConfig>
            {
                new GravityMorphConfig("TM_LeanBack1",                  1.00f,     0.00f,     2.00f),
                //new GravityMorphConfig("LBACK_Breast Diameter",         0.12f,     0.50f,     1.50f),
                //new GravityMorphConfig("LBACK_Breast Height",           0.08f,     0.50f,     1.50f),
                //new GravityMorphConfig("LBACK_Breast Height Upper",     0.02f,     0.50f,     1.50f),
                //new GravityMorphConfig("LBACK_Breast Zero",             0.10f,     0.50f,     1.50f),
                //new GravityMorphConfig("LBACK_Breasts Flatten",         0.20f,     0.50f,     1.50f),
                //new GravityMorphConfig("LBACK_Chest Smoother",          0.15f,     0.50f,     1.50f),

                new GravityMorphConfig("TM_LeanBack2",                  1.33f,     0.50f,     1.50f),
                //new GravityMorphConfig("LBACK_Breast Depth Squash L",   0.16f,     0.50f,     1.50f),
                //new GravityMorphConfig("LBACK_Breast Depth Squash R",   0.16f,     0.50f,     1.50f),
                //new GravityMorphConfig("LBACK_Breast Move S2S Out L",   0.08f,     0.50f,     1.50f),
                //new GravityMorphConfig("LBACK_Breast Move S2S Out R",   0.08f,     0.50f,     1.50f),
                //new GravityMorphConfig("LBACK_Breast Top Curve1",      -0.06f,     0.50f,     1.50f),
                //new GravityMorphConfig("LBACK_Breast Top Curve2",      -0.12f,     0.50f,     1.50f),
                //new GravityMorphConfig("LBACK_Breast Under Smoother1",  0.22f,     0.50f,     1.50f),
                //new GravityMorphConfig("LBACK_Breast Under Smoother3",  0.16f,     0.50f,     1.50f),
                
                new GravityMorphConfig("TM_LeanBack3",                  1.00f,     1.00f,     1.00f),
                //new GravityMorphConfig("LBACK_Breast Under Smoother2",  0.20f,     1.00f,     1.00f),
                //new GravityMorphConfig("LBACK_Center Gap Smooth",       0.30f,     1.00f,     1.00f),
                //new GravityMorphConfig("LBACK_Chest Height",           -0.07f,     1.00f,     1.00f),
                //new GravityMorphConfig("LBACK_ChestSmoothCenter",       0.15f,     1.00f,     1.00f),
                //new GravityMorphConfig("LBACK_ChestUp",                 0.10f,     1.00f,     1.00f),
            };

            leanForwardMorphs = new List<GravityMorphConfig> {
                new GravityMorphConfig("TM_LeanForward1",               1.20f,     0.50f,     1.50f),
                //new GravityMorphConfig("LFWD_Breast Diameter",         -0.21f,     0.50f,     1.50f),
                //new GravityMorphConfig("LFWD_Breast Diameter L",        0.65f,     0.50f,     1.50f),
                //new GravityMorphConfig("LFWD_Breast Diameter R",        0.65f,     0.50f,     1.50f),
                //new GravityMorphConfig("LFWD_Breast Height2 L",        -0.20f,     0.50f,     1.50f),
                //new GravityMorphConfig("LFWD_Breast Height2 R",        -0.20f,     0.50f,     1.50f),
                //new GravityMorphConfig("LFWD_Breast Move Up R",         0.15f,     0.50f,     1.50f),
                //new GravityMorphConfig("LFWD_Breast Move Up L",         0.15f,     0.50f,     1.50f),
                //new GravityMorphConfig("LFWD_Breast Rotate Down L",     0.18f,     0.50f,     1.50f),
                //new GravityMorphConfig("LFWD_Breast Rotate Down R",     0.18f,     0.50f,     1.50f),
                //new GravityMorphConfig("LFWD_Breast Side Smoother",     0.20f,     0.50f,     1.50f),
                //new GravityMorphConfig("LFWD_Breast Width L",          -0.14f,     0.50f,     1.50f),
                //new GravityMorphConfig("LFWD_Breast Width R",          -0.14f,     0.50f,     1.50f),
                //new GravityMorphConfig("LFWD_Sternum Width",            0.20f,     0.50f,     1.50f),

                new GravityMorphConfig("TM_LeanForward2",               1.20f,     1.50f,     0.50f),
                //new GravityMorphConfig("LFWD_Areola S2S L",             0.40f,     1.50f,     0.50f),
                //new GravityMorphConfig("LFWD_Areola S2S R",             0.40f,     1.50f,     0.50f),

                new GravityMorphConfig("TM_LeanForward3",               1.33f,     1.00f,     1.00f),
                //new GravityMorphConfig("LFWD_Breast Depth L",           0.30f,     1.00f,     1.00f),
                //new GravityMorphConfig("LFWD_Breast Depth R",           0.30f,     1.00f,     1.00f),

                new GravityMorphConfig("TM_LeanForward4",               1.33f,    -0.50f,     2.00f),
                //new GravityMorphConfig("LFWD_Breasts Hang Forward",     0.15f,    -0.50f,     2.00f),

                new GravityMorphConfig("TM_LeanForward5",               1.20f,    -1.00f,     2.00f),
                //new GravityMorphConfig("LFWD_Breasts TogetherApart",    0.10f,    -1.00f,     2.00f),
            };

            rollLeftMorphs = new List<GravityMorphConfig>
            {
                new GravityMorphConfig("TM_RollLeft1",                  1.40f,     0.00f,     2.00f),
                //new GravityMorphConfig("RLEFT_Areola S2S L",            0.08f,     0.00f,     2.00f),
                //new GravityMorphConfig("RLEFT_Areola S2S R",            0.30f,     0.00f,     2.00f),
                //new GravityMorphConfig("RLEFT_Breast Depth Squash R",   0.22f,     0.00f,     2.00f),
                //new GravityMorphConfig("RLEFT_Breast Diameter",        -0.10f,     0.00f,     2.00f),
                //new GravityMorphConfig("RLEFT_Breast Move S2S In R",    0.12f,     0.00f,     2.00f),
                //new GravityMorphConfig("RLEFT_Breast Move S2S Out L",   0.12f,     0.00f,     2.00f),
                //new GravityMorphConfig("RLEFT_Breast Pointed",          0.10f,     0.00f,     2.00f),
                //new GravityMorphConfig("RLEFT_Breast Rotate X In L",    0.03f,     0.00f,     2.00f),
                //new GravityMorphConfig("RLEFT_Breast Rotate X In R",    0.08f,     0.00f,     2.00f),
                //new GravityMorphConfig("RLEFT_Breast Width L",         -0.02f,     0.00f,     2.00f),
                //new GravityMorphConfig("RLEFT_Breast Width R",          0.10f,     0.00f,     2.00f),
                //new GravityMorphConfig("RLEFT_Breasts Hang Forward R",  0.12f,     0.00f,     2.00f),
                //new GravityMorphConfig("RLEFT_Center Gap Smooth",       0.24f,     0.00f,     2.00f),
                //new GravityMorphConfig("RLEFT_Centre Gap Narrow",       0.30f,     0.00f,     2.00f),

                new GravityMorphConfig("TM_RollLeft2",                  1.40f,     2.00f,     0.00f),
                //new GravityMorphConfig("RLEFT_Breast Under Smoother1",  0.22f,     2.00f,     0.00f),
                //new GravityMorphConfig("RLEFT_Breast Under Smoother3",  0.16f,     2.00f,     0.00f),
                //new GravityMorphConfig("RLEFT_Breasts Implants R",      0.05f,     2.00f,     0.00f),
            };

            rollRightMorphs = new List<GravityMorphConfig>
            {
                new GravityMorphConfig("TM_RollRight1",                  1.40f,     0.00f,     2.00f),
                //new GravityMorphConfig("RRIGHT_Areola S2S L",           0.30f,     0.00f,     2.00f),
                //new GravityMorphConfig("RRIGHT_Areola S2S R",           0.08f,     0.00f,     2.00f),
                //new GravityMorphConfig("RRIGHT_Breast Depth Squash L",  0.22f,     0.00f,     2.00f),
                //new GravityMorphConfig("RRIGHT_Breast Diameter",       -0.10f,     0.00f,     2.00f),
                //new GravityMorphConfig("RRIGHT_Breast Move S2S In L",   0.12f,     0.00f,     2.00f),
                //new GravityMorphConfig("RRIGHT_Breast Move S2S Out R",  0.12f,     0.00f,     2.00f),
                //new GravityMorphConfig("RRIGHT_Breast Pointed",         0.10f,     0.00f,     2.00f),
                //new GravityMorphConfig("RRIGHT_Breast Rotate X In L",   0.08f,     0.00f,     2.00f),
                //new GravityMorphConfig("RRIGHT_Breast Rotate X In R",   0.03f,     0.00f,     2.00f),
                //new GravityMorphConfig("RRIGHT_Breast Width L",         0.10f,     0.00f,     2.00f),
                //new GravityMorphConfig("RRIGHT_Breast Width R",        -0.02f,     0.00f,     2.00f),
                //new GravityMorphConfig("RRIGHT_Breasts Hang Forward L", 0.12f,     0.00f,     2.00f),
                //new GravityMorphConfig("RRIGHT_Center Gap Smooth",      0.24f,     0.00f,     2.00f),
                //new GravityMorphConfig("RRIGHT_Centre Gap Narrow",      0.30f,     0.00f,     2.00f),

                new GravityMorphConfig("TM_RollRight2",                  1.40f,     2.00f,     0.00f),
                //new GravityMorphConfig("RRIGHT_Breast Under Smoother1", 0.22f,     2.00f,     0.00f),
                //new GravityMorphConfig("RRIGHT_Breast Under Smoother3", 0.16f,     2.00f,     0.00f),
                //new GravityMorphConfig("RRIGHT_Breasts Implants L",     0.05f,     2.00f,     0.00f),
            };
        }

        private void AdjustMorphsForRoll()
        {
            // left
            if(roll >= 0)
            {
                Reset(rollRightMorphs);
                Update(rollLeftMorphs, roll);
            }
            // right
            else
            {
                Reset(rollLeftMorphs);
                Update(rollRightMorphs, Mathf.Abs(roll));
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
                    Update(leanForwardMorphs, pitch, rollFactor);
                    Update(uprightMorphs, 90 - pitch, rollFactor);
                }
                // upside down
                else
                {
                    Reset(uprightMorphs);
                    Update(leanForwardMorphs, 180 - pitch, rollFactor);
                    Update(upsideDownMorphs, pitch - 90, rollFactor);
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
                    Update(leanBackMorphs, Mathf.Abs(pitch), rollFactor);
                    Update(uprightMorphs, 90 - Mathf.Abs(pitch), rollFactor);
                }
                // upside down
                else
                {
                    Reset(uprightMorphs);
                    Update(leanBackMorphs, 180 - Mathf.Abs(pitch), rollFactor);
                    Update(upsideDownMorphs, Mathf.Abs(pitch) - 90, rollFactor);
                }
            }
        }

        private void Update(List<GravityMorphConfig> morphs, float angle, float rollFactor = 1f)
        {
            float effect = rollFactor * angle / 90;
            morphs.ForEach(it => it.UpdateVal(effect, scale, gravity));
        }

        private void Reset(List<GravityMorphConfig> morphs)
        {
            morphs.ForEach(it => it.Reset());
        }
    }
}
