using System.Collections.Generic;
using UnityEngine;

namespace TittyMagic
{
    internal class RelativePosMorphHandler
    {
        private HashSet<PositionMorphConfig> downForceMorphs;
        private HashSet<PositionMorphConfig> upForceMorphs;
        private HashSet<PositionMorphConfig> backForceMorphs;
        private HashSet<PositionMorphConfig> forwardForceMorphs;
        private HashSet<PositionMorphConfig> rightForceMorphs;
        private HashSet<PositionMorphConfig> leftForceMorphs;

        private float scale;
        private float softness;

        public RelativePosMorphHandler()
        {
            //should use same morphs as uprightMorphs in GravityMorphHandler
            downForceMorphs = new HashSet<PositionMorphConfig>
            {
                //                      name                            baseMul    scaleMul   softnessMul
                //new PositionMorphConfig("TM_Upright1",                   1.00f,     1.00f,     1.00f),
                new PositionMorphConfig("TM_Upright1",                   0.2f,     1.00f,     1.00f),
                //new PositionMorphConfig("UPR_Breast Move Down",          0.5f,     1.00f,     1.00f),
                //new PositionMorphConfig("UPR_Chest Height",              0.4f,     1.00f,     1.00f),

                //new PositionMorphConfig("TM_Upright2",                   1.00f,     1.50f,     0.50f),
                new PositionMorphConfig("TM_Upright2",                   0.2f,     1.50f,     0.50f),
                //new PositionMorphConfig("UPR_Breast Rotate Up",          0.3f,     1.50f,     0.50f),

                //new PositionMorphConfig("TM_UprightSmoother",           -2.00f,     1.50f,     0.50f),
                new PositionMorphConfig("TM_UprightSmoother",           -0.40f,     1.50f,     0.50f),
                //new PositionMorphConfig("UPR_Breast Under Smoother1",   -0.24f,     1.50f,     0.50f),
                //new PositionMorphConfig("UPR_Breast Under Smoother3",   -0.48f,     1.50f,     0.50f),
                //new PositionMorphConfig("UPR_Breast Under Smoother4",   -0.24f,     1.50f,     0.50f),

                //new PositionMorphConfig("TM_Upright3",                   2.00f,     0.00f,     2.00f),
                new PositionMorphConfig("TM_Upright3",                   0.40f,     0.00f,     2.00f),
                //new PositionMorphConfig("UPR_Breasts Natural",           0.1f,     0.00f,     2.00f),
            };

            //should use same morphs as upsideDownMorphs in GravityMorphHandler
            upForceMorphs = new HashSet<PositionMorphConfig> {
                new PositionMorphConfig("TM_UpsideDown1",                1.40f,     0.00f,     2.00f),
                //new PositionMorphConfig("UPSD_Areola UpDown",           -0.25f,     0.00f,     2.00f),
                //new PositionMorphConfig("UPSD_Breast Diameter(Pose)",    0.10f,     0.00f,     2.00f),
                //new PositionMorphConfig("UPSD_Breast Height",            0.36f,     0.00f,     2.00f),
                //new PositionMorphConfig("UPSD_Breast Height Upper",      0.04f,     0.00f,     2.00f),
                //new PositionMorphConfig("UPSD_Breast Move Up",           0.10f,     0.00f,     2.00f),
                //new PositionMorphConfig("UPSD_Breast Sag1",             -0.03f,     0.00f,     2.00f),
                //new PositionMorphConfig("UPSD_Breast Sag2",             -0.15f,     0.00f,     2.00f),
                //new PositionMorphConfig("UPSD_Breasts Hang Forward",     0.05f,     0.00f,     2.00f),
                //new PositionMorphConfig("UPSD_Breasts Natural",         -0.05f,     0.00f,     2.00f),

                //new PositionMorphConfig("TM_UpsideDown2",                1.40f,     0.50f,     1.50f),
                new PositionMorphConfig("TM_UpsideDown2",                1.40f,     0.00f,     1.50f),
                //new PositionMorphConfig("UPSD_Breast flat",              0.08f,     0.50f,     1.50f),
                //new PositionMorphConfig("UPSD_Breast Rotate Up",         0.25f,     0.50f,     1.50f),
                //new PositionMorphConfig("UPSD_Breast Under Smoother1",   0.33f,     0.50f,     1.50f),
                //new PositionMorphConfig("UPSD_Breast Under Smoother2",   0.20f,     0.50f,     1.50f),
                //new PositionMorphConfig("UPSD_Breast Under Smoother3",  -0.25f,     0.50f,     1.50f),
                //new PositionMorphConfig("UPSD_Breast Under Smoother4",   0.05f,     0.50f,     1.50f),

                //new PositionMorphConfig("TM_UpsideDown3",                1.00f,     1.00f,     1.00f),
                new PositionMorphConfig("TM_UpsideDown3",                1.00f,     0.00f,     1.00f),
                //new PositionMorphConfig("UPSD_Breast Diameter",          0.05f,     1.00f,     1.00f),
                //new PositionMorphConfig("UPSD_Breasts Flatten",          0.05f,     1.00f,     1.00f),
                //new PositionMorphConfig("UPSD_Breasts Height",           0.05f,     1.00f,     1.00f),
                //new PositionMorphConfig("UPSD_Breasts Implants",        -0.05f,     1.00f,     1.00f),
                //new PositionMorphConfig("UPSD_Breasts Upward Slope",     0.15f,     1.00f,     1.00f),
                //new PositionMorphConfig("UPSD_Center Gap Depth",         0.05f,     1.00f,     1.00f),
                //new PositionMorphConfig("UPSD_Center Gap Height",        0.10f,     1.00f,     1.00f),
                //new PositionMorphConfig("UPSD_Center Gap UpDown",        0.10f,     1.00f,     1.00f),
                //new PositionMorphConfig("UPSD_Chest Height",            -0.07f,     1.00f,     1.00f),
                //new PositionMorphConfig("UPSD_Chest Smoother",           0.10f,     1.00f,     1.00f),
                //new PositionMorphConfig("UPSD_ChestUnderBreast",        -0.15f,     1.00f,     1.00f),
                //new PositionMorphConfig("UPSD_ChestUp",                  0.10f,     1.00f,     1.00f),

                //new PositionMorphConfig("TM_UpsideDown4",                1.40f,     1.00f,     0.00f),
                new PositionMorphConfig("TM_UpsideDown4",                1.40f,     0.00f,     0.00f),
                //new PositionMorphConfig("UPSD_Breast Pointed",           0.25f,     1.00f,     0.00f),

                //new PositionMorphConfig("TM_UpsideDown5",                1.40f,     1.50f,     0.50f),
                new PositionMorphConfig("TM_UpsideDown5",                1.40f,     0.00f,     0.50f),
                //new PositionMorphConfig("UPSD_Breast Top Curve1",       -0.30f,     1.50f,     0.50f),
                //new PositionMorphConfig("UPSD_Breast Top Curve2",       -0.75f,     1.50f,     0.50f),
                //new PositionMorphConfig("UPSD_BreastsShape2",            0.30f,     1.50f,     0.50f),

                //new PositionMorphConfig("TM_UpsideDown6",                1.40f,    -0.50f,     2.00f),
                new PositionMorphConfig("TM_UpsideDown6",                1.40f,    0.00f,     2.00f),
                //new PositionMorphConfig("UPSD_Breasts TogetherApart",    0.12f,    -1.00f,     1.00f),
            };

            //should use same morphs as leanBackMorphs in GravityMorphHandler
            backForceMorphs = new HashSet<PositionMorphConfig>
            {
                new PositionMorphConfig("TM_LeanBack1",                  1.00f,     0.00f,     2.00f),
                //new PositionMorphConfig("LBACK_Breast Diameter",         0.12f,     0.50f,     1.50f),
                //new PositionMorphConfig("LBACK_Breast Height",           0.08f,     0.50f,     1.50f),
                //new PositionMorphConfig("LBACK_Breast Height Upper",     0.04f,     0.50f,     1.50f),
                //new PositionMorphConfig("LBACK_Breast Zero",             0.10f,     0.50f,     1.50f),
                //new PositionMorphConfig("LBACK_Breasts Flatten",         0.25f,     0.50f,     1.50f),
                //new PositionMorphConfig("LBACK_Chest Smoother",          0.10f,     0.50f,     1.50f),

                new PositionMorphConfig("TM_LeanBack2",                  1.33f,     0.50f,     1.50f),
                //new PositionMorphConfig("LBACK_Breast Depth Squash",     0.16f,     0.50f,     1.50f),
                //new PositionMorphConfig("LBACK_Breast Move S2S Out",     0.08f,     0.50f,     1.50f),
                //new PositionMorphConfig("LBACK_Breast Top Curve1",      -0.06f,     0.50f,     1.50f),
                //new PositionMorphConfig("LBACK_Breast Top Curve2",      -0.12f,     0.50f,     1.50f),
                //new PositionMorphConfig("LBACK_Breast Under Smoother1",  0.04f,     0.50f,     1.50f),
                //new PositionMorphConfig("LBACK_Breast Under Smoother3",  0.03f,     0.50f,     1.50f),

                new PositionMorphConfig("TM_LeanBack3",                  1.00f,     1.00f,     1.00f),
                //new PositionMorphConfig("LBACK_Breast Under Smoother2",  0.20f,     1.00f,     1.00f),
                //new PositionMorphConfig("LBACK_Breast Rotate Up",        0.20f,     1.00f,     1.00f),
                //new PositionMorphConfig("LBACK_Center Gap Smooth",       0.30f,     1.00f,     1.00f),
                //new PositionMorphConfig("LBACK_Chest Height",           -0.07f,     1.00f,     1.00f),
                //new PositionMorphConfig("LBACK_ChestSmoothCenter",       0.15f,     1.00f,     1.00f),
                //new PositionMorphConfig("LBACK_ChestUp",                 0.10f,     1.00f,     1.00f),
            };

            //should use same morphs as leanForwardMorphs in GravityMorphHandler
            forwardForceMorphs = new HashSet<PositionMorphConfig> {
                //new PositionMorphConfig("TM_LeanForward1",               1.20f,     0.50f,     1.50f),
                new PositionMorphConfig("TM_LeanForward1",               1.20f,     0.50f,     1.50f),
                //new PositionMorphConfig("LFWD_Breast Diameter",         -0.06f,     0.50f,     1.50f),
                //new PositionMorphConfig("LFWD_Breast Diameter(Pose)",    0.22f,     0.50f,     1.50f),
                //new PositionMorphConfig("LFWD_Breast Height2",          -0.05f,     0.50f,     1.50f),
                //new PositionMorphConfig("LFWD_Breast Move Up",           0.15f,     0.50f,     1.50f),
                //new PositionMorphConfig("LFWD_Breast Side Smoother",     0.20f,     0.50f,     1.50f),
                //new PositionMorphConfig("LFWD_Breast Width",            -0.05f,     0.50f,     1.50f),
                //new PositionMorphConfig("LFWD_Sternum Width",            0.20f,     0.50f,     1.50f),

                //new PositionMorphConfig("TM_LeanForward2",               1.20f,     1.50f,     0.50f),
                new PositionMorphConfig("TM_LeanForward2",               1.20f,     1.50f,     0.50f),
                //new PositionMorphConfig("LFWD_Areola S2S",               0.40f,     1.50f,     0.50f),

                //new PositionMorphConfig("TM_LeanForward3",               1.33f,     1.00f,     1.00f),
                new PositionMorphConfig("TM_LeanForward3",               1.33f,     1.00f,     1.00f),
                //new PositionMorphConfig("LFWD_Breast Depth",             0.30f,     1.00f,     1.00f),

                //new PositionMorphConfig("TM_LeanForward4",               1.33f,    -0.50f,     2.00f),
                new PositionMorphConfig("TM_LeanForward4",               1.33f,    -0.50f,     2.00f),
                //new PositionMorphConfig("LFWD_Breasts Hang Forward",     0.15f,    -0.50f,     2.00f),

                //new PositionMorphConfig("TM_LeanForward5",               1.20f,    -1.00f,     2.00f),
                new PositionMorphConfig("TM_LeanForward5",               1.20f,    -1.00f,     2.00f),
                //new PositionMorphConfig("LFWD_Breasts TogetherApart",    0.10f,    -1.00f,     2.00f),
            };

            //should use same morphs as rollLeftMorphs in GravityMorphHandler
            rightForceMorphs = new HashSet<PositionMorphConfig>
            {
                //new PositionMorphConfig("TM_RollLeft1",                  1.40f,     0.00f,     2.00f),
                new PositionMorphConfig("TM_RollLeft1",                  1.40f,     0.00f,     2.00f),
                //new PositionMorphConfig("RLEFT_Areola S2S L",            0.08f,     0.00f,     2.00f),
                //new PositionMorphConfig("RLEFT_Areola S2S R",            0.30f,     0.00f,     2.00f),
                //new PositionMorphConfig("RLEFT_Breast Depth Squash R",   0.22f,     0.00f,     2.00f),
                //new PositionMorphConfig("RLEFT_Breast Diameter",        -0.10f,     0.00f,     2.00f),
                //new PositionMorphConfig("RLEFT_Breast Move S2S In R",    0.12f,     0.00f,     2.00f),
                //new PositionMorphConfig("RLEFT_Breast Move S2S Out L",   0.12f,     0.00f,     2.00f),
                //new PositionMorphConfig("RLEFT_Breast Pointed",          0.10f,     0.00f,     2.00f),
                //new PositionMorphConfig("RLEFT_Breast Rotate X In L",    0.03f,     0.00f,     2.00f),
                //new PositionMorphConfig("RLEFT_Breast Rotate X In R",    0.08f,     0.00f,     2.00f),
                //new PositionMorphConfig("RLEFT_Breast Width L",         -0.02f,     0.00f,     2.00f),
                //new PositionMorphConfig("RLEFT_Breast Width R",          0.10f,     0.00f,     2.00f),
                //new PositionMorphConfig("RLEFT_Breasts Hang Forward R",  0.12f,     0.00f,     2.00f),
                //new PositionMorphConfig("RLEFT_Center Gap Smooth",       0.24f,     0.00f,     2.00f),
                //new PositionMorphConfig("RLEFT_Centre Gap Narrow",       0.30f,     0.00f,     2.00f),

                //new PositionMorphConfig("TM_RollLeft2",                  1.40f,     2.00f,     0.00f),
                new PositionMorphConfig("TM_RollLeft2",                  1.40f,     2.00f,     0.00f),
                //new PositionMorphConfig("RLEFT_Breast Under Smoother1",  0.22f,     2.00f,     0.00f),
                //new PositionMorphConfig("RLEFT_Breast Under Smoother3",  0.16f,     2.00f,     0.00f),
                //new PositionMorphConfig("RLEFT_Breasts Implants R",      0.05f,     2.00f,     0.00f),
            };

            //should use same morphs as rollRightMorphs in GravityMorphHandler
            leftForceMorphs = new HashSet<PositionMorphConfig>
            {
                //new PositionMorphConfig("TM_RollRight1",                  1.40f,     0.00f,     2.00f),
                new PositionMorphConfig("TM_RollRight1",                  1.40f,     0.00f,     2.00f),
                //new PositionMorphConfig("RRIGHT_Areola S2S L",           0.30f,     0.00f,     2.00f),
                //new PositionMorphConfig("RRIGHT_Areola S2S R",           0.08f,     0.00f,     2.00f),
                //new PositionMorphConfig("RRIGHT_Breast Depth Squash L",  0.22f,     0.00f,     2.00f),
                //new PositionMorphConfig("RRIGHT_Breast Diameter",       -0.10f,     0.00f,     2.00f),
                //new PositionMorphConfig("RRIGHT_Breast Move S2S In L",   0.12f,     0.00f,     2.00f),
                //new PositionMorphConfig("RRIGHT_Breast Move S2S Out R",  0.12f,     0.00f,     2.00f),
                //new PositionMorphConfig("RRIGHT_Breast Pointed",         0.10f,     0.00f,     2.00f),
                //new PositionMorphConfig("RRIGHT_Breast Rotate X In L",   0.08f,     0.00f,     2.00f),
                //new PositionMorphConfig("RRIGHT_Breast Rotate X In R",   0.03f,     0.00f,     2.00f),
                //new PositionMorphConfig("RRIGHT_Breast Width L",         0.10f,     0.00f,     2.00f),
                //new PositionMorphConfig("RRIGHT_Breast Width R",        -0.02f,     0.00f,     2.00f),
                //new PositionMorphConfig("RRIGHT_Breasts Hang Forward L", 0.12f,     0.00f,     2.00f),
                //new PositionMorphConfig("RRIGHT_Center Gap Smooth",      0.24f,     0.00f,     2.00f),
                //new PositionMorphConfig("RRIGHT_Centre Gap Narrow",      0.30f,     0.00f,     2.00f),

                //new PositionMorphConfig("TM_RollRight2",                  1.40f,     2.00f,     0.00f),
                new PositionMorphConfig("TM_RollRight2",                  1.40f,     2.00f,     0.00f),
                //new PositionMorphConfig("RRIGHT_Breast Under Smoother1", 0.22f,     2.00f,     0.00f),
                //new PositionMorphConfig("RRIGHT_Breast Under Smoother3", 0.16f,     2.00f,     0.00f),
                //new PositionMorphConfig("RRIGHT_Breasts Implants L",     0.05f,     2.00f,     0.00f),
            };
        }

        public void Update(
            Vector3 positionDiff,
            float scale,
            float softness
        )
        {
            this.scale = scale;
            this.softness = softness;

            // left
            if(positionDiff.x <= 0)
            {
                Reset(leftForceMorphs);
                UpdateSet(rightForceMorphs, -positionDiff.x, 1.00f);
            }
            // right
            else
            {
                Reset(rightForceMorphs);
                UpdateSet(leftForceMorphs, positionDiff.x, 1.00f);
            }

            // up
            if(positionDiff.y <= 0)
            {
                Reset(downForceMorphs);
                // TODO morph specific logMaxX..?
                UpdateSet(upForceMorphs, -positionDiff.y, 5.00f);
            }
            // down
            else
            {
                Reset(upForceMorphs);
                UpdateSet(downForceMorphs, positionDiff.y, 1.00f);
            }

            // forward
            if(positionDiff.z <= 0)
            {
                Reset(backForceMorphs);
                UpdateSet(forwardForceMorphs, -positionDiff.z, 1.00f);
            }
            // back
            else
            {
                Reset(forwardForceMorphs);
                UpdateSet(backForceMorphs, positionDiff.z, 1.00f);
            }
        }

        private void UpdateSet(HashSet<PositionMorphConfig> morphs, float effect, float logMaxX)
        {
            foreach(var it in morphs)
            {
                it.UpdateVal(effect, scale, softness, logMaxX);
            }
        }

        public void ResetAll()
        {
            Reset(leftForceMorphs);
            Reset(rightForceMorphs);
            Reset(upForceMorphs);
            Reset(downForceMorphs);
            Reset(forwardForceMorphs);
            Reset(backForceMorphs);
        }

        private void Reset(HashSet<PositionMorphConfig> morphs)
        {
            foreach(var it in morphs)
            {
                it.Reset();
            }
        }

        public string GetStatus()
        {
            string text = "";
            text += "LEFT\n\n";
            foreach(var it in leftForceMorphs)
                text += it.GetStatus();

            text += "RIGHT\n\n";
            foreach(var it in rightForceMorphs)
                text += it.GetStatus();

            text += "UP\n\n";
            foreach(var it in upForceMorphs)
                text += it.GetStatus();

            text += "DOWN\n\n";
            foreach(var it in downForceMorphs)
                text += it.GetStatus();

            text += "FORWARD\n\n";
            foreach(var it in forwardForceMorphs)
                text += it.GetStatus();

            text += "BACK\n\n";
            foreach(var it in backForceMorphs)
                text += it.GetStatus();

            return text;
        }
    }
}
