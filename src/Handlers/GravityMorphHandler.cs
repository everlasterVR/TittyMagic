using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace everlaster
{
    class GravityMorphHandler
    {
        private List<GravityMorphConfig> morphs;

        private float roll;
        private float pitch;
        private float scale;
        private float softness;
        private float sag;

        public GravityMorphHandler()
        {
            morphs = new List<GravityMorphConfig>();
            InitGravityMorphs();
            morphs.ForEach(it => it.Reset());
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

        public void Reset(string type = "")
        {
            morphs
                .Where(it => type == "" || it.angleType == type)
                .ToList().ForEach(it => it.Reset());
        }

        public string GetStatus()
        {
            string text = "";
            morphs.ForEach((it) =>
            {
                text = text + Formatting.NameValueString(it.name, it.morph.morphValue, 1000f, 30) + "\n";
            });
            return text;
        }

        // TODO refactor to not use Dictionary -> same morph can be listed multiple times for different angle types
        // Possible to merge to one morph per angle type?
        private void InitGravityMorphs()
        {
            string type = AngleTypes.UPRIGHT;
            morphs.AddRange(new List<GravityMorphConfig>
            { 
                //                      name                                     baseMul    scaleMul   softMul
                new GravityMorphConfig("TM_Breast Move Up",             type,   -0.07f,     0.33f,     1.67f),
                new GravityMorphConfig("TM_Breasts Natural",            type,    0.08f,     0.00f,     2.00f),
                new GravityMorphConfig("TM_Breast Rotate Up",           type,    0.15f,     1.20f,     0.80f),
                new GravityMorphConfig("TM_Breast Under Smoother1",     type,   -0.04f,     1.50f,     0.50f),
                new GravityMorphConfig("TM_Breast Under Smoother3",     type,   -0.08f,     1.00f,     1.00f),
            });

            type = AngleTypes.UPSIDE_DOWN;
            morphs.AddRange(new List<GravityMorphConfig> {
                new GravityMorphConfig("TM_Breast Move Up",             type,    0.07f,     0.33f,     1.67f),
                new GravityMorphConfig("TM_Breast Sag1",                type,   -0.03f,     0.75f,     1.25f),
                new GravityMorphConfig("TM_Breast Sag2",                type,   -0.05f,     0.75f,     1.25f),
                new GravityMorphConfig("TM_Breasts Hang Forward",       type,    0.05f,     0.80f,     1.50f),
                new GravityMorphConfig("TM_Breasts Natural",            type,   -0.04f,     0.00f,     2.00f),
                new GravityMorphConfig("TM_Breasts TogetherApart",      type,    0.10f,     0.80f,     1.50f),
                new GravityMorphConfig("TM_Areola UpDown",              type,   -0.15f,     0.67f,     1.33f),
                new GravityMorphConfig("TM_Center Gap Depth",           type,    0.05f,     1.50f,     0.50f),
                new GravityMorphConfig("TM_Center Gap Height",          type,    0.10f,     1.50f,     0.50f),
                new GravityMorphConfig("TM_Center Gap UpDown",          type,    0.10f,     1.50f,     0.50f),
                new GravityMorphConfig("TM_Chest Smoother",             type,    0.10f,     1.25f,     0.75f),
                new GravityMorphConfig("TM_ChestUnderBreast",           type,    0.15f,     0.25f,     1.75f),
                new GravityMorphConfig("TM_ChestUp",                    type,    0.05f,     0.25f,     1.75f),
                new GravityMorphConfig("TM_ChestUpperNarrow",           type,    0.10f,     0.80f,     1.50f),
                new GravityMorphConfig("TM_Breast Diameter",            type,    0.05f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breast flat",                type,    0.08f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breast Height",              type,    0.10f,     0.00f,     2.00f),
                new GravityMorphConfig("TM_Breast Pointed",             type,    0.33f,     1.00f,     0.00f),
                new GravityMorphConfig("TM_Breast Rotate Up",           type,    0.25f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breast Top Curve1",          type,   -0.04f,    -0.50f,     2.00f),
                new GravityMorphConfig("TM_Breast Top Curve2",          type,   -0.06f,     0.50f,     2.00f),
                new GravityMorphConfig("TM_Breast Under Smoother1",     type,    0.45f,     1.40f,     0.60f),
                new GravityMorphConfig("TM_Breast Under Smoother3",     type,    0.20f,    -1.00f,     1.00f),
                new GravityMorphConfig("TM_Breasts Flatten",            type,    0.10f,     0.60f,     1.40f),
                new GravityMorphConfig("TM_Breasts Height",             type,    0.10f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breasts Implants",           type,   -0.05f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breasts Upward Slope",       type,    0.15f,     0.80f,     1.20f),
                new GravityMorphConfig("TM_BreastsShape2",              type,    0.50f,     1.33f,     0.67f),
                new GravityMorphConfig("TM_Sternum Height",             type,   -0.30f,     null,      null),
            });

            type = AngleTypes.LEAN_FORWARD;
            morphs.AddRange(new List<GravityMorphConfig> {
                new GravityMorphConfig("TM_Breast Depth Left",              type,    0.22f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breast Depth Right",             type,    0.22f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breast Diameter Left",           type,    0.22f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breast Diameter Right",          type,    0.22f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breast Diameter (Copy)",         type,   -0.04f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breast Large",                   type,   -0.08f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breast Side Smoother",           type,    0.20f,     0.20f,     1.80f),
                new GravityMorphConfig("TM_Breasts Height (Copy)",          type,   -0.18f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breasts Hang Forward (Copy)",    type,    0.05f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breasts TogetherApart (Copy)",   type,    0.20f,     0.25f,     1.75f),
                new GravityMorphConfig("TM_Sternum Width",                  type,    0.25f,     0.75f,     1.25f),
            });

            type = AngleTypes.LEAN_BACK;
            morphs.AddRange(new List<GravityMorphConfig>
            {
                new GravityMorphConfig("TM_Breast Depth Squash Left",       type,   -0.20f,     0.00f,     2.00f),
                new GravityMorphConfig("TM_Breast Depth Squash Right",      type,   -0.20f,     0.00f,     2.00f),
                new GravityMorphConfig("TM_Breast Diameter (Copy)",         type,    0.08f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breast Large",                   type,   -0.08f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breast Side Smoother",           type,   -0.33f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breast Under Smoother1 (Copy)",  type,   -0.04f,     1.00f,     1.00f),
                new GravityMorphConfig("TM_Breast Under Smoother3 (Copy)",  type,   -0.10f,     1.00f,     1.00f),
                new GravityMorphConfig("TM_Breast Move S2S Out Left",       type,    0.08f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breast Move S2S Out Right",      type,    0.08f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breasts Flatten (Copy)",         type,    0.25f,     0.33f,     1.67f),
                new GravityMorphConfig("TM_Chest Smoother (Copy)",          type,    0.33f,     0.00f,     2.00f),
                new GravityMorphConfig("TM_ChestShape",                     type,   -0.20f,     1.00f,     1.00f),
                new GravityMorphConfig("TM_ChestSmoothCenter",              type,    0.15f,     0.33f,     1.67f),
                new GravityMorphConfig("TM_ChestUp (Copy)",                 type,    0.20f,     1.00f,     1.00f),
                new GravityMorphConfig("TM_Sternum Width",                  type,    0.33f,     1.33f,    -0.67f),
            });

            type = AngleTypes.ROLL_LEFT;
            morphs.AddRange(new List<GravityMorphConfig>
            {
                new GravityMorphConfig("TM_Areola S2S Left",                    type,   -0.40f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Areola S2S Right",                   type,    0.40f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breast Move S2S In Right",           type,    0.28f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breast Move S2S Out Left (Copy)",    type,    0.40f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breast Rotate X In Right",           type,    0.10f,     0.00f,     2.00f),
                new GravityMorphConfig("TM_Breast Width Left",                  type,   -0.03f,     0.40f,     1.60f),
                new GravityMorphConfig("TM_Breast Width Right",                 type,    0.07f,     0.40f,     1.60f),
                new GravityMorphConfig("TM_Breasts Diameter",                   type,   -0.05f,     0.40f,     1.60f),
                new GravityMorphConfig("TM_Centre Gap Narrow",                  type,    0.10f,     0.25f,     1.75f),
                new GravityMorphConfig("TM_Center Gap Smooth",                  type,    0.20f,     0.25f,     1.75f),
            });

            type = AngleTypes.ROLL_RIGHT;
            morphs.AddRange(new List<GravityMorphConfig>
            {
                new GravityMorphConfig("TM_Areola S2S Left",                    type,    0.40f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Areola S2S Right",                   type,   -0.40f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breast Move S2S In Left",            type,    0.28f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breast Move S2S Out Right (Copy)",   type,    0.40f,     0.50f,     1.50f),
                new GravityMorphConfig("TM_Breast Rotate X In Left",            type,    0.10f,     0.00f,     2.00f),
                new GravityMorphConfig("TM_Breast Width Left",                  type,    0.07f,     0.40f,     1.60f),
                new GravityMorphConfig("TM_Breast Width Right",                 type,   -0.03f,     0.40f,     1.60f),
                new GravityMorphConfig("TM_Breasts Diameter",                   type,   -0.05f,     0.40f,     1.60f),
                new GravityMorphConfig("TM_Centre Gap Narrow",                  type,    0.10f,     0.25f,     1.75f),
                new GravityMorphConfig("TM_Center Gap Smooth",                  type,    0.20f,     0.25f,     1.75f),
            });
        }

        private void AdjustMorphsForRoll()
        {
            // left
            if(roll >= 0)
            {
                Reset(AngleTypes.ROLL_RIGHT);
                DoAdjust(AngleTypes.ROLL_LEFT, Calc.Remap(roll, 1));
            }
            // right
            else
            {
                Reset(AngleTypes.ROLL_LEFT);
                DoAdjust(AngleTypes.ROLL_RIGHT, Calc.Remap(Mathf.Abs(roll), 1));
            }
        }

        private void AdjustMorphsForPitch(float rollFactor)
        {
            // leaning forward
            if(pitch > 0)
            {
                Reset(AngleTypes.LEAN_BACK);
                // upright
                if(pitch <= 90)
                {
                    Reset(AngleTypes.UPSIDE_DOWN);
                    DoAdjust(AngleTypes.LEAN_FORWARD, Calc.Remap(pitch, rollFactor));
                    DoAdjust(AngleTypes.UPRIGHT, Calc.Remap(90 - pitch, rollFactor));
                }
                // upside down
                else
                {
                    Reset(AngleTypes.UPRIGHT);
                    DoAdjust(AngleTypes.LEAN_FORWARD, Calc.Remap(180 - pitch, rollFactor));
                    DoAdjust(AngleTypes.UPSIDE_DOWN, Calc.Remap(pitch - 90, rollFactor));
                }
            }
            // leaning back
            else
            {
                Reset(AngleTypes.LEAN_FORWARD);
                // upright
                if(pitch > -90)
                {
                    Reset(AngleTypes.UPSIDE_DOWN);
                    DoAdjust(AngleTypes.LEAN_BACK, Calc.Remap(Mathf.Abs(pitch), rollFactor));
                    DoAdjust(AngleTypes.UPRIGHT, Calc.Remap(90 - Mathf.Abs(pitch), rollFactor));
                }
                // upside down
                else
                {
                    Reset(AngleTypes.UPRIGHT);
                    DoAdjust(AngleTypes.LEAN_BACK, Calc.Remap(180 - Mathf.Abs(pitch), rollFactor));
                    DoAdjust(AngleTypes.UPSIDE_DOWN, Calc.Remap(Mathf.Abs(pitch) - 90, rollFactor));
                }
            }
        }

        private void DoAdjust(string type, float effect)
        {
            morphs
                .Where(it => it.angleType == type)
                .ToList().ForEach(it => it.UpdateVal(effect, scale, softness, sag));
        }
    }
}
