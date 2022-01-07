using System.Collections.Generic;
using UnityEngine;

namespace TittyMagic
{
    internal class RelativePosMorphConfigurator : MVRScript
    {
        private float scale;
        private float softness;

        private List<UIConfigurationUnit> downForceConfigs = new List<UIConfigurationUnit>
        {
            { new UIConfigurationUnit("UPR_Breast Move Down") },
            { new UIConfigurationUnit("UPR_Chest Height") },
            { new UIConfigurationUnit("UPR_Breast Rotate Up") },
            { new UIConfigurationUnit("UPR_Breast Under Smoother1") },
            { new UIConfigurationUnit("UPR_Breast Under Smoother3") },
            { new UIConfigurationUnit("UPR_Breast Under Smoother4") },
            { new UIConfigurationUnit("UPR_Breasts Natural") },
        };

        private List<UIConfigurationUnit> upForceConfigs = new List<UIConfigurationUnit>
        {
            { new UIConfigurationUnit("UPSD_Areola UpDown") },
            { new UIConfigurationUnit("UPSD_Breast Diameter(Pose)") },
            { new UIConfigurationUnit("UPSD_Breast Height") },
            { new UIConfigurationUnit("UPSD_Breast Height Upper") },
            { new UIConfigurationUnit("UPSD_Breast Move Up") },
            { new UIConfigurationUnit("UPSD_Breast Sag1") },
            { new UIConfigurationUnit("UPSD_Breast Sag2") },
            { new UIConfigurationUnit("UPSD_Breasts Hang Forward") },
            { new UIConfigurationUnit("UPSD_Breasts Natural") },
            { new UIConfigurationUnit("UPSD_Breast flat") },
            { new UIConfigurationUnit("UPSD_Breast Rotate Up") },
            { new UIConfigurationUnit("UPSD_Breast Under Smoother1") },
            { new UIConfigurationUnit("UPSD_Breast Under Smoother2") },
            { new UIConfigurationUnit("UPSD_Breast Under Smoother3") },
            { new UIConfigurationUnit("UPSD_Breast Under Smoother4") },
            { new UIConfigurationUnit("UPSD_Breast Diameter") },
            { new UIConfigurationUnit("UPSD_Breasts Flatten") },
            { new UIConfigurationUnit("UPSD_Breasts Height") },
            { new UIConfigurationUnit("UPSD_Breasts Implants") },
            { new UIConfigurationUnit("UPSD_Breasts Upward Slope") },
            { new UIConfigurationUnit("UPSD_Center Gap Depth") },
            { new UIConfigurationUnit("UPSD_Center Gap Height") },
            { new UIConfigurationUnit("UPSD_Center Gap UpDown") },
            { new UIConfigurationUnit("UPSD_Chest Height") },
            { new UIConfigurationUnit("UPSD_Chest Smoother") },
            { new UIConfigurationUnit("UPSD_ChestUnderBreast") },
            { new UIConfigurationUnit("UPSD_ChestUp") },
            { new UIConfigurationUnit("UPSD_Breast Pointed") },
            { new UIConfigurationUnit("UPSD_Breast Top Curve1") },
            { new UIConfigurationUnit("UPSD_Breast Top Curve2") },
            { new UIConfigurationUnit("UPSD_BreastsShape2") },
            { new UIConfigurationUnit("UPSD_Breasts TogetherApart") },
        };

        private List<UIConfigurationUnit> backForceConfigs = new List<UIConfigurationUnit>
        {
            { new UIConfigurationUnit("LBACK_Breast Diameter") },
            { new UIConfigurationUnit("LBACK_Breast Height") },
            { new UIConfigurationUnit("LBACK_Breast Height Upper") },
            { new UIConfigurationUnit("LBACK_Breast Zero") },
            { new UIConfigurationUnit("LBACK_Breasts Flatten") },
            { new UIConfigurationUnit("LBACK_Chest Smoother") },
            { new UIConfigurationUnit("LBACK_Breast Depth Squash") },
            { new UIConfigurationUnit("LBACK_Breast Move S2S Out") },
            { new UIConfigurationUnit("LBACK_Breast Top Curve1") },
            { new UIConfigurationUnit("LBACK_Breast Top Curve2") },
            { new UIConfigurationUnit("LBACK_Breast Under Smoother1") },
            { new UIConfigurationUnit("LBACK_Breast Under Smoother3") },
            { new UIConfigurationUnit("LBACK_Breast Under Smoother2") },
            { new UIConfigurationUnit("LBACK_Breast Rotate Up") },
            { new UIConfigurationUnit("LBACK_Center Gap Smooth") },
            { new UIConfigurationUnit("LBACK_Chest Height") },
            { new UIConfigurationUnit("LBACK_ChestSmoothCenter") },
            { new UIConfigurationUnit("LBACK_ChestUp") },
        };

        private List<UIConfigurationUnit> forwardForceConfigs = new List<UIConfigurationUnit>
        {
            { new UIConfigurationUnit("LFWD_Breast Diameter") },
            { new UIConfigurationUnit("LFWD_Breast Diameter(Pose)") },
            { new UIConfigurationUnit("LFWD_Breast Height2") },
            { new UIConfigurationUnit("LFWD_Breast Move Up") },
            { new UIConfigurationUnit("LFWD_Breast Side Smoother") },
            { new UIConfigurationUnit("LFWD_Breast Width") },
            { new UIConfigurationUnit("LFWD_Sternum Width") },
            { new UIConfigurationUnit("LFWD_Areola S2S") },
            { new UIConfigurationUnit("LFWD_Breast Depth") },
            { new UIConfigurationUnit("LFWD_Breasts Hang Forward") },
            { new UIConfigurationUnit("LFWD_Breasts TogetherApart") },
        };

        private List<UIConfigurationUnit> leftForceConfigs = new List<UIConfigurationUnit>
        {
            { new UIConfigurationUnit("RLEFT_Areola S2S L") },
            { new UIConfigurationUnit("RLEFT_Areola S2S R") },
            { new UIConfigurationUnit("RLEFT_Breast Depth Squash R") },
            { new UIConfigurationUnit("RLEFT_Breast Diameter") },
            { new UIConfigurationUnit("RLEFT_Breast Move S2S In R") },
            { new UIConfigurationUnit("RLEFT_Breast Move S2S Out L") },
            { new UIConfigurationUnit("RLEFT_Breast Pointed") },
            { new UIConfigurationUnit("RLEFT_Breast Rotate X In L") },
            { new UIConfigurationUnit("RLEFT_Breast Rotate X In R") },
            { new UIConfigurationUnit("RLEFT_Breast Width L") },
            { new UIConfigurationUnit("RLEFT_Breast Width R") },
            { new UIConfigurationUnit("RLEFT_Breasts Hang Forward R") },
            { new UIConfigurationUnit("RLEFT_Center Gap Smooth") },
            { new UIConfigurationUnit("RLEFT_Centre Gap Narrow") },
        };

        private List<UIConfigurationUnit> rightForceConfigs = new List<UIConfigurationUnit>
        {
            { new UIConfigurationUnit("RLEFT_Breast Under Smoother1") },
            { new UIConfigurationUnit("RLEFT_Breast Under Smoother3") },
            { new UIConfigurationUnit("RLEFT_Breasts Implants R") },
            { new UIConfigurationUnit("RRIGHT_Areola S2S L") },
            { new UIConfigurationUnit("RRIGHT_Areola S2S R") },
            { new UIConfigurationUnit("RRIGHT_Breast Depth Squash L") },
            { new UIConfigurationUnit("RRIGHT_Breast Diameter") },
            { new UIConfigurationUnit("RRIGHT_Breast Move S2S In L") },
            { new UIConfigurationUnit("RRIGHT_Breast Move S2S Out R") },
            { new UIConfigurationUnit("RRIGHT_Breast Pointed") },
            { new UIConfigurationUnit("RRIGHT_Breast Rotate X In L") },
            { new UIConfigurationUnit("RRIGHT_Breast Rotate X In R") },
            { new UIConfigurationUnit("RRIGHT_Breast Width L") },
            { new UIConfigurationUnit("RRIGHT_Breast Width R") },
            { new UIConfigurationUnit("RRIGHT_Breasts Hang Forward L") },
            { new UIConfigurationUnit("RRIGHT_Center Gap Smooth") },
            { new UIConfigurationUnit("RRIGHT_Centre Gap Narrow") },
            { new UIConfigurationUnit("RRIGHT_Breast Under Smoother1") },
            { new UIConfigurationUnit("RRIGHT_Breast Under Smoother3") },
            { new UIConfigurationUnit("RRIGHT_Breasts Implants L") },
        };

        public override void Init()
        {
        }

        public void DoInit()
        {
            UI.NewTextField(this, "Down force morphs", 32, 100, false);
            UI.NewSpacer(this, 100, true);
            foreach(var item in downForceConfigs)
                item.Init(this);

            UI.NewTextField(this, "Up force morphs", 32, 100, false);
            UI.NewSpacer(this, 100, true);
            foreach(var item in upForceConfigs)
                item.Init(this);

            UI.NewTextField(this, "Back force morphs", 32, 100, false);
            UI.NewSpacer(this, 100, true);
            foreach(var item in backForceConfigs)
                item.Init(this);

            UI.NewTextField(this, "Forward force morphs", 32, 100, false);
            UI.NewSpacer(this, 100, true);
            foreach(var item in forwardForceConfigs)
                item.Init(this);

            UI.NewTextField(this, "Left force morphs", 32, 100, false);
            UI.NewSpacer(this, 100, true);
            foreach(var item in leftForceConfigs)
                item.Init(this);

            UI.NewTextField(this, "Right force morphs", 32, 100, false);
            UI.NewSpacer(this, 100, true);
            foreach(var item in rightForceConfigs)
                item.Init(this);
        }

        public void Update(
            Vector3 positionDiff,
            float scale,
            float softness
        )
        {
            this.scale = scale;
            this.softness = softness;
            float x = WithDeadZone(positionDiff.x, 0.002f);
            float y = WithDeadZone(positionDiff.y, 0.008f);
            float z = WithDeadZone(positionDiff.z, 0.002f);

            // TODO separate l/r morphs only, separate calculation of diff
            ////left
            if(x <= 0)
            {
                ResetMorphs(leftForceConfigs);
                UpdateMorphs(rightForceConfigs, -x);
            }
            // right
            else
            {
                ResetMorphs(rightForceConfigs);
                UpdateMorphs(leftForceConfigs, x);
            }

            // up
            if(y <= 0)
            {
                ResetMorphs(downForceConfigs);
                UpdateMorphs(upForceConfigs, -y);
            }
            // down
            else
            {
                ResetMorphs(upForceConfigs);
                UpdateMorphs(downForceConfigs, y);
            }

            ////// forward
            if(z <= 0)
            {
                ResetMorphs(backForceConfigs);
                UpdateMorphs(forwardForceConfigs, -z);
            }
            // back
            else
            {
                ResetMorphs(forwardForceConfigs);
                UpdateMorphs(backForceConfigs, z);
            }
        }

        private void UpdateMorphs(List<UIConfigurationUnit> configs, float diff)
        {
            foreach(var item in configs)
            {
                item.UpdateMorphValue(PositionDiffVal(diff), scale, softness);
            }
        }

        private float WithDeadZone(float diff, float deadZone)
        {
            if(diff >= 0)
            {
                return (diff - deadZone) > 0 ? diff - deadZone : 0;
            }

            return (diff + deadZone) < 0 ? diff + deadZone : 0;
        }

        private float PositionDiffVal(float diff)
        {
            return Mathf.SmoothStep(0, 1, Mathf.Pow(diff, 1/2f));
        }

        public void ResetAll()
        {
            ResetMorphs(leftForceConfigs);
            ResetMorphs(rightForceConfigs);
            ResetMorphs(upForceConfigs);
            ResetMorphs(downForceConfigs);
            ResetMorphs(forwardForceConfigs);
            ResetMorphs(backForceConfigs);
        }

        private void ResetMorphs(List<UIConfigurationUnit> configs)
        {
            foreach(var item in configs)
            {
                item.ResetMorphValue();
            }
        }
    }
}
