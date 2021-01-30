using System.Collections.Generic;
using UnityEngine;

namespace everlaster
{
    class GravityMorphHandler
    {
        private List<GravityMorphConfig> uprightMorphs;
        private List<GravityMorphConfig> upsideDownMorphs;
        private List<GravityMorphConfig> leanBackMorphs;
        private List<GravityMorphConfig> leanForwardMorphs;
        private List<GravityMorphConfig> rollLeftMorphs;
        private List<GravityMorphConfig> rollRightMorphs;

        private float roll;
        private float pitch;
        private float scale;
        private float softness;
        private float sag;

        public GravityMorphHandler()
        {
            InitGravityMorphs();
            ResetAll();
        }

        public void Update(
            float roll,
            float pitch,
            float scale,
            float softness,
            float sag
        )
        {
            this.roll = roll;
            this.pitch = pitch;
            this.scale = scale;
            this.softness = softness;
            this.sag = sag;

            AdjustMorphsForRoll();
            AdjustMorphsForPitch(Calc.RollFactor(roll));
        }

        public void ResetAll()
        {
            uprightMorphs.ForEach(it => it.Reset());
            upsideDownMorphs.ForEach(it => it.Reset());
            leanBackMorphs.ForEach(it => it.Reset());
            leanForwardMorphs.ForEach(it => it.Reset());
            rollLeftMorphs.ForEach(it => it.Reset());
            rollRightMorphs.ForEach(it => it.Reset());
        }

        public string GetStatus()
        {
            string text = "UPRIGHT\n";
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

        private void InitGravityMorphs()
        {

            uprightMorphs = new List<GravityMorphConfig>
            {
                //                      name                            offset      baseMul    scaleMul   softMul
                new GravityMorphConfig("UPR_Chest Height",              0f,         0.20f,     1.50f,     0.50f),
                new GravityMorphConfig("UPR_Breast Move Down L",        0f,         0.25f,     1.50f,     0.50f),
                new GravityMorphConfig("UPR_Breast Move Down R",        0f,         0.25f,     1.50f,     0.50f),
                new GravityMorphConfig("UPR_Breast Rotate Up",          0f,         0.15f,     1.20f,     0.80f),
                new GravityMorphConfig("UPR_Breast Under Smoother1",    0.06f,     -0.12f,     1.50f,     0.50f),
                new GravityMorphConfig("UPR_Breast Under Smoother3",    0.12f,     -0.24f,     1.50f,     0.50f),
                new GravityMorphConfig("UPR_Breast Under Smoother4",    0.06f,     -0.12f,     1.50f,     0.50f),
                new GravityMorphConfig("UPR_Breasts Natural",           0f,         0.05f,     0.00f,     2.00f),
            };

            upsideDownMorphs = new List<GravityMorphConfig> {
                new GravityMorphConfig("UPSD_Areola UpDown",            0f,        -0.25f,     0.00f,     2.00f),
                new GravityMorphConfig("UPSD_Breast Diameter L",        0f,         0.10f,     0.00f,     2.00f),
                new GravityMorphConfig("UPSD_Breast Diameter R",        0f,         0.10f,     0.00f,     2.00f),
                new GravityMorphConfig("UPSD_Breast Diameter",          0f,         0.05f,     1.00f,     1.00f),
                new GravityMorphConfig("UPSD_Breast Height",            0f,         0.33f,     0.00f,     2.00f),
                new GravityMorphConfig("UPSD_Breast Height Upper",      0f,         0.10f,     0.00f,     2.00f),
                new GravityMorphConfig("UPSD_Breast flat",              0f,         0.08f,     0.50f,     1.50f),
                new GravityMorphConfig("UPSD_Breast Move Up",           0f,         0.10f,     0.00f,     2.00f),
                new GravityMorphConfig("UPSD_Breast Pointed",           0f,         0.25f,     1.00f,     0.00f),
                new GravityMorphConfig("UPSD_Breast Rotate Up",         0f,         0.25f,     0.50f,     1.50f),
                new GravityMorphConfig("UPSD_Breast Sag1",              0f,        -0.03f,     0.00f,     2.00f),
                new GravityMorphConfig("UPSD_Breast Sag2",              0f,        -0.05f,     0.00f,     2.00f),
                new GravityMorphConfig("UPSD_Breast Top Curve1",        0f,        -0.30f,     1.50f,     0.50f),
                new GravityMorphConfig("UPSD_Breast Top Curve2",        0f,        -0.75f,     1.50f,     0.50f),
                new GravityMorphConfig("UPSD_Breast Under Smoother1",   0.06f,      0.33f,     0.50f,     1.50f),
                new GravityMorphConfig("UPSD_Breast Under Smoother2",   0f,         0.20f,     0.50f,     1.50f),
                new GravityMorphConfig("UPSD_Breast Under Smoother3",   0.12f,     -0.25f,     0.50f,     1.50f),
                new GravityMorphConfig("UPSD_Breast Under Smoother4",   0.06f,      0.05f,     0.50f,     1.50f),
                new GravityMorphConfig("UPSD_Breasts Flatten",          0f,         0.05f,     1.00f,     1.00f),
                new GravityMorphConfig("UPSD_Breasts Hang Forward",     0f,         0.05f,     0.00f,     2.00f),
                new GravityMorphConfig("UPSD_Breasts Height",           0f,         0.05f,     1.00f,     1.00f),
                new GravityMorphConfig("UPSD_Breasts Implants",         0f,        -0.05f,     1.00f,     1.00f),
                new GravityMorphConfig("UPSD_Breasts Natural",          0f,        -0.05f,     0.00f,     2.00f),
                new GravityMorphConfig("UPSD_Breasts TogetherApart",    0f,         0.12f,    -1.00f,     1.00f),
                new GravityMorphConfig("UPSD_Breasts Upward Slope",     0f,         0.15f,     1.00f,     1.00f),
                new GravityMorphConfig("UPSD_BreastsShape2",            0f,         0.33f,     1.50f,     0.50f),
                new GravityMorphConfig("UPSD_Center Gap Depth",         0f,         0.05f,     1.00f,     1.00f),
                new GravityMorphConfig("UPSD_Center Gap Height",        0f,         0.10f,     1.00f,     1.00f),
                new GravityMorphConfig("UPSD_Center Gap UpDown",        0f,         0.10f,     1.00f,     1.00f),
                new GravityMorphConfig("UPSD_Chest Height",             0f,        -0.07f,     1.00f,     1.00f),
                new GravityMorphConfig("UPSD_Chest Smoother",           0f,         0.10f,     1.00f,     1.00f),
                new GravityMorphConfig("UPSD_ChestUnderBreast",         0f,        -0.15f,     1.00f,     1.00f),
                new GravityMorphConfig("UPSD_ChestUp",                  0f,         0.10f,     1.00f,     1.00f),
            };

            leanBackMorphs = new List<GravityMorphConfig>
            {
                new GravityMorphConfig("LBACK_Breast Depth Squash L",   0f,         0.25f,     0.00f,     2.00f),
                new GravityMorphConfig("LBACK_Breast Depth Squash R",   0f,         0.25f,     0.00f,     2.00f),
                new GravityMorphConfig("LBACK_Breast Diameter",         0f,         0.15f,     0.00f,     2.00f),
                new GravityMorphConfig("LBACK_Breast Height",           0f,         0.33f,     0.00f,     2.00f),
                new GravityMorphConfig("LBACK_Breast Height Upper",     0f,         0.10f,     0.00f,     2.00f),
                new GravityMorphConfig("LBACK_Breast Move S2S Out L",   0f,         0.08f,     0.50f,     1.50f),
                new GravityMorphConfig("LBACK_Breast Move S2S Out R",   0f,         0.08f,     0.50f,     1.50f),
                new GravityMorphConfig("LBACK_Breast Top Curve1",       0f,        -0.14f,     1.00f,     1.00f),
                new GravityMorphConfig("LBACK_Breast Top Curve2",       0f,        -0.28f,     1.00f,     1.00f),
                new GravityMorphConfig("LBACK_Breast Under Smoother1",  0f,         0.22f,     0.50f,     1.50f),
                new GravityMorphConfig("LBACK_Breast Under Smoother2",  0f,         0.20f,     1.00f,     1.00f),
                new GravityMorphConfig("LBACK_Breast Under Smoother3",  0f,         0.16f,     0.50f,     1.50f),
                new GravityMorphConfig("LBACK_Breast Zero",             0f,         0.10f,     0.00f,     2.00f),
                new GravityMorphConfig("LBACK_Breasts Flatten",         0f,         0.25f,     0.00f,     2.00f),
                new GravityMorphConfig("LBACK_Chest Smoother",          0f,         0.33f,     0.00f,     2.00f),
                new GravityMorphConfig("LBACK_ChestSmoothCenter",       0f,         0.15f,     1.00f,     1.00f),
                new GravityMorphConfig("LBACK_ChestUp",                 0f,         0.10f,     1.00f,     1.00f),
                new GravityMorphConfig("LBACK_Chest Height",            0f,        -0.07f,     1.00f,     1.00f),
                new GravityMorphConfig("LBACK_Center Gap Smooth",       0f,         0.33f,     1.00f,     1.00f),
            };

            leanForwardMorphs = new List<GravityMorphConfig> {
                new GravityMorphConfig("LFWD_Areola S2S L",             0f,         0.40f,     1.50f,     0.50f),
                new GravityMorphConfig("LFWD_Areola S2S R",             0f,         0.40f,     1.50f,     0.50f),
                new GravityMorphConfig("LFWD_Breast Depth L",           0f,         0.30f,     1.00f,     1.00f),
                new GravityMorphConfig("LFWD_Breast Depth R",           0f,         0.30f,     1.00f,     1.00f),
                new GravityMorphConfig("LFWD_Breast Diameter",          0f,        -0.04f,     0.50f,     1.50f),
                new GravityMorphConfig("LFWD_Breast Diameter L",        0f,         0.40f,     0.50f,     1.50f),
                new GravityMorphConfig("LFWD_Breast Diameter R",        0f,         0.40f,     0.50f,     1.50f),
                new GravityMorphConfig("LFWD_Breast Height2 L",         0f,        -0.20f,     0.50f,     1.50f),
                new GravityMorphConfig("LFWD_Breast Height2 R",         0f,        -0.20f,     0.50f,     1.50f),
                new GravityMorphConfig("LFWD_Breast Move Up R",         0f,         0.15f,     0.50f,     1.50f),
                new GravityMorphConfig("LFWD_Breast Move Up L",         0f,         0.15f,     0.50f,     1.50f),
                new GravityMorphConfig("LFWD_Breast Rotate Down L",     0f,         0.20f,     0.50f,     1.50f),
                new GravityMorphConfig("LFWD_Breast Rotate Down R",     0f,         0.20f,     0.50f,     1.50f),
                new GravityMorphConfig("LFWD_Breast Side Smoother",     0f,         0.20f,     0.50f,     1.50f),
                new GravityMorphConfig("LFWD_Breast Width L",           0f,        -0.14f,     0.50f,     1.50f),
                new GravityMorphConfig("LFWD_Breast Width R",           0f,        -0.14f,     0.50f,     1.50f),
                new GravityMorphConfig("LFWD_Breasts Hang Forward",     0f,         0.15f,    -0.50f,     2.00f),
                new GravityMorphConfig("LFWD_Breasts TogetherApart",    0f,         0.10f,    -1.00f,     2.00f),
                new GravityMorphConfig("LFWD_Sternum Width",            0f,         0.20f,     0.50f,     1.50f),
            };

            rollLeftMorphs = new List<GravityMorphConfig>
            {
                new GravityMorphConfig("RLEFT_Areola S2S L",            0f,         0.08f,     0.00f,     2.00f),
                new GravityMorphConfig("RLEFT_Areola S2S R",            0f,         0.30f,     0.00f,     2.00f),
                new GravityMorphConfig("RLEFT_Breast Depth Squash R",   0f,         0.22f,     0.00f,     2.00f),
                new GravityMorphConfig("RLEFT_Breast Diameter",         0f,        -0.10f,     0.00f,     2.00f),
                new GravityMorphConfig("RLEFT_Breast Move S2S In R",    0f,         0.12f,     0.00f,     2.00f),
                new GravityMorphConfig("RLEFT_Breast Move S2S Out L",   0f,         0.12f,     0.00f,     2.00f),
                new GravityMorphConfig("RLEFT_Breast Pointed",          0f,         0.10f,     0.00f,     2.00f),
                new GravityMorphConfig("RLEFT_Breast Rotate X In L",    0f,         0.03f,     0.00f,     2.00f),
                new GravityMorphConfig("RLEFT_Breast Rotate X In R",    0f,         0.08f,     0.00f,     2.00f),
                new GravityMorphConfig("RLEFT_Breast Width L",          0f,        -0.02f,     0.00f,     2.00f),
                new GravityMorphConfig("RLEFT_Breast Width R",          0f,         0.10f,     0.00f,     2.00f),
                new GravityMorphConfig("RLEFT_Breast Under Smoother1",  0f,         0.22f,     2.00f,     0.00f),
                new GravityMorphConfig("RLEFT_Breast Under Smoother3",  0f,         0.16f,     2.00f,     0.00f),
                new GravityMorphConfig("RLEFT_Breasts Hang Forward R",  0f,         0.12f,     0.00f,     2.00f),
                //new GravityMorphConfig("RLEFT_Breasts Implants L",      0f,         0.03f,     0.00f,     0.00f),
                new GravityMorphConfig("RLEFT_Breasts Implants R",      0f,         0.05f,     2.00f,     0.00f),
                new GravityMorphConfig("RLEFT_Centre Gap Narrow",       0f,         0.33f,     0.00f,     2.00f),
                new GravityMorphConfig("RLEFT_Center Gap Smooth",       0f,         0.24f,     0.00f,     2.00f),
            };

            rollRightMorphs = new List<GravityMorphConfig>
            {
                new GravityMorphConfig("RRIGHT_Areola S2S L",           0f,         0.30f,     0.00f,     2.00f),
                new GravityMorphConfig("RRIGHT_Areola S2S R",           0f,         0.08f,     0.00f,     2.00f),
                new GravityMorphConfig("RRIGHT_Breast Depth Squash L",  0f,         0.22f,     0.00f,     2.00f),
                new GravityMorphConfig("RRIGHT_Breast Diameter",        0f,        -0.10f,     0.00f,     2.00f),
                new GravityMorphConfig("RRIGHT_Breast Move S2S In L",   0f,         0.12f,     0.00f,     2.00f),
                new GravityMorphConfig("RRIGHT_Breast Move S2S Out R",  0f,         0.12f,     0.00f,     2.00f),
                new GravityMorphConfig("RRIGHT_Breast Pointed",         0f,         0.10f,     0.00f,     2.00f),
                new GravityMorphConfig("RRIGHT_Breast Rotate X In L",   0f,         0.08f,     0.00f,     2.00f),
                new GravityMorphConfig("RRIGHT_Breast Rotate X In R",   0f,         0.03f,     0.00f,     2.00f),
                new GravityMorphConfig("RRIGHT_Breast Width L",         0f,         0.10f,     0.00f,     2.00f),
                new GravityMorphConfig("RRIGHT_Breast Width R",         0f,        -0.02f,     0.00f,     2.00f),
                new GravityMorphConfig("RRIGHT_Breast Under Smoother1", 0f,         0.22f,     2.00f,     0.00f),
                new GravityMorphConfig("RRIGHT_Breast Under Smoother3", 0f,         0.16f,     2.00f,     0.00f),
                new GravityMorphConfig("RRIGHT_Breasts Hang Forward L", 0f,         0.12f,     0.00f,     2.00f),
                new GravityMorphConfig("RRIGHT_Breasts Implants L",     0f,         0.05f,     2.00f,     0.00f),
                //new GravityMorphConfig("RRIGHT_Breasts Implants R",     0f,         0.03f,     0.00f,     0.00f),
                new GravityMorphConfig("RRIGHT_Centre Gap Narrow",      0f,         0.33f,     0.00f,     2.00f),
                new GravityMorphConfig("RRIGHT_Center Gap Smooth",      0f,         0.24f,     0.00f,     2.00f),
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
            morphs.ForEach(it => it.UpdateVal(effect, scale, softness, sag));
        }

        private void Reset(List<GravityMorphConfig> morphs)
        {
            morphs.ForEach(it => it.Reset());
        }
    }
}
