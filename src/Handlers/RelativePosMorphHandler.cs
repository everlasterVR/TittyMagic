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

        //private Vector3 positionDiff;
        //private float scale;
        //private float softness;

        public RelativePosMorphHandler()
        {
            //should use same morphs as uprightMorphs in GravityMorphHandler
            downForceMorphs = new HashSet<PositionMorphConfig>
            {
                //                      name                            baseMul    scaleMul   gravityMul
                new PositionMorphConfig("TM_Upright1",                   1.00f,     1.00f,     1.00f),
                new PositionMorphConfig("TM_Upright2",                   1.00f,     1.50f,     0.50f),
                new PositionMorphConfig("TM_UprightSmoother",           -2.00f,     1.50f,     0.50f),
                new PositionMorphConfig("TM_Upright3",                   2.00f,     0.00f,     2.00f),
            };

            //should use same morphs as upsideDownMorphs in GravityMorphHandler
            upForceMorphs = new HashSet<PositionMorphConfig> {
                new PositionMorphConfig("TM_UpsideDown1",                1.40f,     0.00f,     2.00f),
                new PositionMorphConfig("TM_UpsideDown2",                1.40f,     0.50f,     1.50f),
                new PositionMorphConfig("TM_UpsideDown3",                1.00f,     1.00f,     1.00f),
                new PositionMorphConfig("TM_UpsideDown4",                1.40f,     1.00f,     0.00f),
                new PositionMorphConfig("TM_UpsideDown5",                1.40f,     1.50f,     0.50f),
                new PositionMorphConfig("TM_UpsideDown6",                1.40f,    -0.50f,     2.00f),
            };

            //should use same morphs as leanBackMorphs in GravityMorphHandler
            backForceMorphs = new HashSet<PositionMorphConfig>
            {
                new PositionMorphConfig("TM_LeanBack1",                  1.00f,     0.00f,     2.00f),
                new PositionMorphConfig("TM_LeanBack2",                  1.33f,     0.50f,     1.50f),
                new PositionMorphConfig("TM_LeanBack3",                  1.00f,     1.00f,     1.00f),
            };

            //should use same morphs as leanForwardMorphs in GravityMorphHandler
            forwardForceMorphs = new HashSet<PositionMorphConfig> {
                new PositionMorphConfig("TM_LeanForward1",               1.20f,     0.50f,     1.50f),
                new PositionMorphConfig("TM_LeanForward2",               1.20f,     1.50f,     0.50f),
                new PositionMorphConfig("TM_LeanForward3",               1.33f,     1.00f,     1.00f),
                new PositionMorphConfig("TM_LeanForward4",               1.33f,    -0.50f,     2.00f),
                new PositionMorphConfig("TM_LeanForward5",               1.20f,    -1.00f,     2.00f),
            };

            //should use same morphs as rollLeftMorphs in GravityMorphHandler
            rightForceMorphs = new HashSet<PositionMorphConfig>
            {
                new PositionMorphConfig("TM_RollLeft1",                  1.40f,     0.00f,     2.00f),
                new PositionMorphConfig("TM_RollLeft2",                  1.40f,     2.00f,     0.00f),
            };

            //should use same morphs as rollRightMorphs in GravityMorphHandler
            leftForceMorphs = new HashSet<PositionMorphConfig>
            {
                new PositionMorphConfig("TM_RollRight1",                  1.40f,     0.00f,     2.00f),
                new PositionMorphConfig("TM_RollRight2",                  1.40f,     2.00f,     0.00f),
            };
        }

        public void Update(
            Vector3 positionDiff,
            float scale,
            float softness
        )
        {
            float effectX = Mathf.InverseLerp(0, 0.075f, Mathf.Abs(positionDiff.x));
            if(positionDiff.x <= 0)
            {
                foreach(var it in leftForceMorphs)
                    it.UpdateVal(effectX, scale, softness);
            }
            else
            {
                foreach(var it in rightForceMorphs)
                    it.UpdateVal(effectX, scale, softness);
            }

            float effectY = Mathf.InverseLerp(0, 0.075f, Mathf.Abs(positionDiff.y));
            if(positionDiff.y <= 0)
            {
                foreach(var it in upForceMorphs)
                    it.UpdateVal(effectY, scale, softness);
            }
            else
            {
                foreach(var it in downForceMorphs)
                    it.UpdateVal(effectY, scale, softness);
            }

            float effectZ = Mathf.InverseLerp(0, 0.075f, Mathf.Abs(positionDiff.z));
            if(positionDiff.z <= 0)
            {
                foreach(var it in forwardForceMorphs)
                    it.UpdateVal(effectZ, scale, softness);
            }
            else
            {
                foreach(var it in backForceMorphs)
                    it.UpdateVal(effectZ, scale, softness);
            }
        }
    }
}
