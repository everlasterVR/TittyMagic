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

        private Vector3 positionDiff;
        private float scale;
        private float softness;

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

        private void AdjustLeftRightMorphs()
        {
            float effectX = Mathf.InverseLerp(0, 0.100f, Mathf.Abs(positionDiff.x));

            // left
            if(positionDiff.x <= 0)
            {
                Reset(leftForceMorphs);
                UpdateSet(rightForceMorphs, effectX);
            }
            // right
            else
            {
                Reset(rightForceMorphs);
                UpdateSet(leftForceMorphs, effectX);
            }
        }

        private void AdjustUpDownMorphs()
        {
            float effectY = Mathf.InverseLerp(0, 0.120f, Mathf.Abs(positionDiff.y));

            // up
            if(positionDiff.y <= 0)
            {
                Reset(downForceMorphs);
                UpdateSet(upForceMorphs, effectY);
            }
            // down
            else
            {
                Reset(upForceMorphs);
                UpdateSet(downForceMorphs, effectY);
            }
        }

        private void AdjustForwardBackMorphs()
        {
            float effectZ = Mathf.InverseLerp(0, 0.100f, Mathf.Abs(positionDiff.z));

            // forward
            if(positionDiff.z <= 0)
            {
                Reset(backForceMorphs);
                UpdateSet(forwardForceMorphs, effectZ);
            }
            // back
            else
            {
                Reset(forwardForceMorphs);
                UpdateSet(backForceMorphs, effectZ);
            }
        }

        public void Update(
            Vector3 positionDiff,
            float scale,
            float softness
        )
        {
            this.positionDiff = positionDiff;
            this.scale = scale;
            this.softness = softness;

            AdjustLeftRightMorphs();
            AdjustUpDownMorphs();
            AdjustForwardBackMorphs();
        }

        public void UpdateOld(
            Vector3 positionDiff,
            float scale,
            float softness
        )
        {
            float effectX = Mathf.InverseLerp(0, 0.100f, Mathf.Abs(positionDiff.x));
            float effectY = Mathf.InverseLerp(0, 0.120f, Mathf.Abs(positionDiff.y));
            float effectZ = Mathf.InverseLerp(0, 0.100f, Mathf.Abs(positionDiff.z));

            if(positionDiff.x <= 0)
            {
                Reset(rightForceMorphs);
                foreach(var it in rightForceMorphs)
                    it.UpdateVal(effectX, scale, softness);
            }
            else
            {
                Reset(leftForceMorphs);
                foreach(var it in rightForceMorphs)
                    it.UpdateVal(effectX, scale, softness);
            }

            if(positionDiff.y <= 0)
            {
                Reset(downForceMorphs);
                foreach(var it in upForceMorphs)
                    it.UpdateVal(effectY, scale, softness);
            }
            else
            {
                Reset(upForceMorphs);
                foreach(var it in downForceMorphs)
                    it.UpdateVal(effectY, scale, softness);
            }

            if(positionDiff.z <= 0)
            {
                Reset(backForceMorphs);
                foreach(var it in forwardForceMorphs)
                    it.UpdateVal(effectZ, scale, softness);
            }
            else
            {
                Reset(forwardForceMorphs);
                foreach(var it in backForceMorphs)
                    it.UpdateVal(effectZ, scale, softness);
            }
        }

        private void UpdateSet(HashSet<PositionMorphConfig> morphs, float effect)
        {
            foreach(var it in morphs)
            {
                it.UpdateVal(effect, scale, softness);
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
            string text = "LEFT\n";
            foreach(var it in leftForceMorphs)
                text += it.GetStatus();

            text += "\nRIGHT\n";
            foreach(var it in rightForceMorphs)
                text += it.GetStatus();

            text += "\nUP\n";
            foreach(var it in upForceMorphs)
                text += it.GetStatus();

            text += "\nDOWN\n";
            foreach(var it in downForceMorphs)
                text += it.GetStatus();

            text += "\nFORWARD\n";
            foreach(var it in forwardForceMorphs)
                text += it.GetStatus();

            text += "\nBACK\n";
            foreach(var it in backForceMorphs)
                text += it.GetStatus();

            return text;
        }
    }
}
