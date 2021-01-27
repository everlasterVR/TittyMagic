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
            string text = "";
            uprightMorphs.ForEach((it) => text = text + it.GetStatus());
            upsideDownMorphs.ForEach((it) => text = text + it.GetStatus());
            leanBackMorphs.ForEach((it) => text = text + it.GetStatus());
            leanForwardMorphs.ForEach((it) => text = text + it.GetStatus());
            rollLeftMorphs.ForEach((it) => text = text + it.GetStatus());
            rollRightMorphs.ForEach((it) => text = text + it.GetStatus());
            return text;
        }

        // TODO refactor to not use Dictionary -> same morph can be listed multiple times for different angle types
        // Possible to merge to one morph per angle type?
        private void InitGravityMorphs()
        {
            uprightMorphs = new List<GravityMorphConfig>
            { 
                //                      name                                     baseMul    scaleMul   softMul
                new GravityMorphConfig("TM_Breast Move Up",             -0.07f,     0.33f,     1.67f),
                new GravityMorphConfig("TM_Breasts Natural",             0.08f,     0.00f,     2.00f),
                new GravityMorphConfig("TM_Breast Rotate Up",            0.15f,     1.20f,     0.80f),
                new GravityMorphConfig("TM_Breast Under Smoother1",     -0.04f,     1.50f,     0.50f),
                new GravityMorphConfig("TM_Breast Under Smoother3",     -0.08f,     1.00f,     1.00f),
            };

            upsideDownMorphs = new List<GravityMorphConfig> {
                new GravityMorphConfig("TM_Breast Move Up",              0.07f,     0.33f,     1.67f),
                new GravityMorphConfig("TM_Breast Sag1",                -0.03f,     0.75f,     1.25f),
                new GravityMorphConfig("TM_Breast Sag2",                -0.05f,     0.75f,     1.25f),
                new GravityMorphConfig("TM_Breasts Hang Forward",        0.05f,     0.80f,     1.50f),
                new GravityMorphConfig("TM_Breasts Natural",            -0.04f,     0.00f,     2.00f),
                new GravityMorphConfig("TM_Breasts TogetherApart",       0.10f,     0.80f,     1.50f),
                new GravityMorphConfig("TM_Areola UpDown",              -0.15f,     0.67f,     1.33f),
                new GravityMorphConfig("TM_Center Gap Depth",            0.05f,     1.50f,     0.50f),
                new GravityMorphConfig("TM_Center Gap Height",           0.10f,     1.50f,     0.50f),
                new GravityMorphConfig("TM_Center Gap UpDown",           0.10f,     1.50f,     0.50f),
                new GravityMorphConfig("TM_Chest Smoother",              0.10f,     1.25f,     0.75f),
                new GravityMorphConfig("TM_ChestUnderBreast",            0.15f,     0.25f,     1.75f),
                new GravityMorphConfig("TM_ChestUp",                     0.05f,     0.25f,     1.75f),
                new GravityMorphConfig("TM_ChestUpperNarrow",            0.10f,     0.80f,     1.50f),
                new GravityMorphConfig("TM_Breast Diameter",             0.05f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breast flat",                 0.08f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breast Height",               0.10f,     0.00f,     2.00f),
                new GravityMorphConfig("TM_Breast Pointed",              0.33f,     1.00f,     0.00f),
                new GravityMorphConfig("TM_Breast Rotate Up",            0.25f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breast Top Curve1",          -0.04f,    -0.50f,     2.00f),
                new GravityMorphConfig("TM_Breast Top Curve2",          -0.06f,     0.50f,     2.00f),
                new GravityMorphConfig("TM_Breast Under Smoother1",      0.45f,     1.40f,     0.60f),
                new GravityMorphConfig("TM_Breast Under Smoother3",      0.20f,    -1.00f,     1.00f),
                new GravityMorphConfig("TM_Breasts Flatten",             0.10f,     0.60f,     1.40f),
                new GravityMorphConfig("TM_Breasts Height",              0.10f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breasts Implants",           -0.05f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breasts Upward Slope",        0.15f,     0.80f,     1.20f),
                new GravityMorphConfig("TM_BreastsShape2",               0.50f,     1.33f,     0.67f),
                new GravityMorphConfig("TM_Sternum Height",             -0.30f,     null,      null),
            };

            leanBackMorphs = new List<GravityMorphConfig>
            {
                new GravityMorphConfig("TM_Breast Depth Squash Left",       -0.20f,     0.00f,     2.00f),
                new GravityMorphConfig("TM_Breast Depth Squash Right",      -0.20f,     0.00f,     2.00f),
                new GravityMorphConfig("TM_Breast Diameter (Copy)",          0.08f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breast Large",                   -0.08f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breast Side Smoother",           -0.33f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breast Under Smoother1 (Copy)",  -0.04f,     1.00f,     1.00f),
                new GravityMorphConfig("TM_Breast Under Smoother3 (Copy)",  -0.10f,     1.00f,     1.00f),
                new GravityMorphConfig("TM_Breast Move S2S Out Left",        0.08f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breast Move S2S Out Right",       0.08f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breasts Flatten (Copy)",          0.25f,     0.33f,     1.67f),
                new GravityMorphConfig("TM_Chest Smoother (Copy)",           0.33f,     0.00f,     2.00f),
                new GravityMorphConfig("TM_ChestShape",                     -0.20f,     1.00f,     1.00f),
                new GravityMorphConfig("TM_ChestSmoothCenter",               0.15f,     0.33f,     1.67f),
                new GravityMorphConfig("TM_ChestUp (Copy)",                  0.20f,     1.00f,     1.00f),
                new GravityMorphConfig("TM_Sternum Width",                   0.33f,     1.33f,    -0.67f),
            };

            leanForwardMorphs = new List<GravityMorphConfig> {
                new GravityMorphConfig("TM_Breast Depth Left",               0.22f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breast Depth Right",              0.22f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breast Diameter Left",            0.22f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breast Diameter Right",           0.22f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breast Diameter (Copy)",         -0.04f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breast Large",                   -0.08f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breast Side Smoother",            0.20f,     0.20f,     1.80f),
                new GravityMorphConfig("TM_Breasts Height (Copy)",          -0.18f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breasts Hang Forward (Copy)",     0.05f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breasts TogetherApart (Copy)",    0.20f,     0.25f,     1.75f),
                new GravityMorphConfig("TM_Sternum Width",                   0.25f,     0.75f,     1.25f),
            };

            rollLeftMorphs = new List<GravityMorphConfig>
            {
                new GravityMorphConfig("TM_Areola S2S Left",                    -0.40f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Areola S2S Right",                    0.40f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breast Move S2S In Right",            0.28f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breast Move S2S Out Left (Copy)",     0.40f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breast Rotate X In Right",            0.10f,     0.00f,     2.00f),
                new GravityMorphConfig("TM_Breast Width Left",                  -0.03f,     0.40f,     1.60f),
                new GravityMorphConfig("TM_Breast Width Right",                  0.07f,     0.40f,     1.60f),
                new GravityMorphConfig("TM_Breasts Diameter",                   -0.05f,     0.40f,     1.60f),
                new GravityMorphConfig("TM_Centre Gap Narrow",                   0.10f,     0.25f,     1.75f),
                new GravityMorphConfig("TM_Center Gap Smooth",                   0.20f,     0.25f,     1.75f),
            };

            rollRightMorphs = new List<GravityMorphConfig>
            {
                new GravityMorphConfig("TM_Areola S2S Left",                     0.40f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Areola S2S Right",                   -0.40f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breast Move S2S In Left",             0.28f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breast Move S2S Out Right (Copy)",    0.40f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breast Rotate X In Left",             0.10f,     0.00f,     2.00f),
                new GravityMorphConfig("TM_Breast Width Left",                   0.07f,     0.40f,     1.60f),
                new GravityMorphConfig("TM_Breast Width Right",                 -0.03f,     0.40f,     1.60f),
                new GravityMorphConfig("TM_Breasts Diameter",                   -0.05f,     0.40f,     1.60f),
                new GravityMorphConfig("TM_Centre Gap Narrow",                   0.10f,     0.25f,     1.75f),
                new GravityMorphConfig("TM_Center Gap Smooth",                   0.20f,     0.25f,     1.75f),
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
