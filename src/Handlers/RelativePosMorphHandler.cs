using System.Collections.Generic;
using UnityEngine;

namespace TittyMagic
{
    internal class RelativePosMorphHandler
    {
        private RelativePosMorphConfigurator _configurator;

        private float mass;
        private float softness;

        private Dictionary<string, List<PositionMorphConfig>> _configSets;

        private List<PositionMorphConfig> _downForceConfigs = new List<PositionMorphConfig>
        {
            { new PositionMorphConfig("UPR_Breast Move Down") },
            { new PositionMorphConfig("UPR_Chest Height") },
            { new PositionMorphConfig("UPR_Breast Rotate Up") },
            { new PositionMorphConfig("UPR_Breast Under Smoother1") },
            { new PositionMorphConfig("UPR_Breast Under Smoother3") },
            { new PositionMorphConfig("UPR_Breast Under Smoother4") },
            { new PositionMorphConfig("UPR_Breasts Natural") },
        };

        private List<PositionMorphConfig> _upForceConfigs = new List<PositionMorphConfig>
        {
            { new PositionMorphConfig("UPSD_Breast Move Up") },
            { new PositionMorphConfig("Breast look up") },
            { new PositionMorphConfig("UPSD_Breast Top Curve1") },
            { new PositionMorphConfig("UPSD_Breasts Height") },
            { new PositionMorphConfig("Breast Height Lower") },
            { new PositionMorphConfig("Breasts Under Curve") },
            { new PositionMorphConfig("UPSD_ChestUnderBreast") },
            { new PositionMorphConfig("UPSD_Breast Under Smoother1") },
            { new PositionMorphConfig("UPSD_Breast Under Smoother2") },
            { new PositionMorphConfig("UPSD_Breast Under Smoother3") },
            { new PositionMorphConfig("UPSD_Breast Under Smoother4") },
            { new PositionMorphConfig("UPSD_Breast Height Upper") },
            { new PositionMorphConfig("UPSD_Breasts Upward Slope") },
            { new PositionMorphConfig("UPSD_Chest Height") },
            { new PositionMorphConfig("Breast upper down") },
            { new PositionMorphConfig("Breasts Small Top Slope") },
            { new PositionMorphConfig("UPSD_Center Gap Depth") },
            { new PositionMorphConfig("UPSD_Center Gap Height") },
            { new PositionMorphConfig("UPSD_Center Gap UpDown") },
            { new PositionMorphConfig("UPSD_Chest Smoother") },
            { new PositionMorphConfig("ChestSeparateBreasts") },
        };

        private List<PositionMorphConfig> _backForceConfigs = new List<PositionMorphConfig>
        {
            { new PositionMorphConfig("LBACK_Breast Diameter") },
            { new PositionMorphConfig("LBACK_Breast Height") },
            { new PositionMorphConfig("LBACK_Breast Height Upper") },
            { new PositionMorphConfig("LBACK_Breast Zero") },
            { new PositionMorphConfig("LBACK_Breasts Flatten") },
            { new PositionMorphConfig("LBACK_Chest Smoother") },
            { new PositionMorphConfig("LBACK_Breast Depth Squash") },
            { new PositionMorphConfig("LBACK_Breast Move S2S Out") },
            { new PositionMorphConfig("LBACK_Breast Top Curve1") },
            { new PositionMorphConfig("LBACK_Breast Top Curve2") },
            { new PositionMorphConfig("LBACK_Breast Under Smoother1") },
            { new PositionMorphConfig("LBACK_Breast Under Smoother3") },
            { new PositionMorphConfig("LBACK_Breast Under Smoother2") },
            { new PositionMorphConfig("LBACK_Breast Rotate Up") },
            { new PositionMorphConfig("LBACK_Center Gap Smooth") },
            { new PositionMorphConfig("LBACK_Chest Height") },
            { new PositionMorphConfig("LBACK_ChestSmoothCenter") },
            { new PositionMorphConfig("LBACK_ChestUp") },
        };

        private List<PositionMorphConfig> _forwardForceConfigs = new List<PositionMorphConfig>
        {
            { new PositionMorphConfig("LFWD_Breast Diameter") },
            { new PositionMorphConfig("LFWD_Breast Diameter(Pose)") },
            { new PositionMorphConfig("LFWD_Breast Height2") },
            { new PositionMorphConfig("LFWD_Breast Move Up") },
            { new PositionMorphConfig("LFWD_Breast Side Smoother") },
            { new PositionMorphConfig("LFWD_Breast Width") },
            { new PositionMorphConfig("LFWD_Sternum Width") },
            { new PositionMorphConfig("LFWD_Areola S2S") },
            { new PositionMorphConfig("LFWD_Breast Depth") },
            { new PositionMorphConfig("LFWD_Breasts Hang Forward") },
            { new PositionMorphConfig("LFWD_Breasts TogetherApart") },
        };

        private List<PositionMorphConfig> _leftForceConfigs = new List<PositionMorphConfig>
        {
            { new PositionMorphConfig("RLEFT_Areola S2S L") },
            { new PositionMorphConfig("RLEFT_Areola S2S R") },
            { new PositionMorphConfig("RLEFT_Breast Depth Squash R") },
            { new PositionMorphConfig("RLEFT_Breast Diameter") },
            { new PositionMorphConfig("RLEFT_Breast Move S2S In R") },
            { new PositionMorphConfig("RLEFT_Breast Move S2S Out L") },
            { new PositionMorphConfig("RLEFT_Breast Pointed") },
            { new PositionMorphConfig("RLEFT_Breast Rotate X In L") },
            { new PositionMorphConfig("RLEFT_Breast Rotate X In R") },
            { new PositionMorphConfig("RLEFT_Breast Width L") },
            { new PositionMorphConfig("RLEFT_Breast Width R") },
            { new PositionMorphConfig("RLEFT_Breasts Hang Forward R") },
            { new PositionMorphConfig("RLEFT_Center Gap Smooth") },
            { new PositionMorphConfig("RLEFT_Centre Gap Narrow") },
        };

        private List<PositionMorphConfig> _rightForceConfigs = new List<PositionMorphConfig>
        {
            { new PositionMorphConfig("RLEFT_Breast Under Smoother1") },
            { new PositionMorphConfig("RLEFT_Breast Under Smoother3") },
            { new PositionMorphConfig("RLEFT_Breasts Implants R") },
            { new PositionMorphConfig("RRIGHT_Areola S2S L") },
            { new PositionMorphConfig("RRIGHT_Areola S2S R") },
            { new PositionMorphConfig("RRIGHT_Breast Depth Squash L") },
            { new PositionMorphConfig("RRIGHT_Breast Diameter") },
            { new PositionMorphConfig("RRIGHT_Breast Move S2S In L") },
            { new PositionMorphConfig("RRIGHT_Breast Move S2S Out R") },
            { new PositionMorphConfig("RRIGHT_Breast Pointed") },
            { new PositionMorphConfig("RRIGHT_Breast Rotate X In L") },
            { new PositionMorphConfig("RRIGHT_Breast Rotate X In R") },
            { new PositionMorphConfig("RRIGHT_Breast Width L") },
            { new PositionMorphConfig("RRIGHT_Breast Width R") },
            { new PositionMorphConfig("RRIGHT_Breasts Hang Forward L") },
            { new PositionMorphConfig("RRIGHT_Center Gap Smooth") },
            { new PositionMorphConfig("RRIGHT_Centre Gap Narrow") },
            { new PositionMorphConfig("RRIGHT_Breast Under Smoother1") },
            { new PositionMorphConfig("RRIGHT_Breast Under Smoother3") },
            { new PositionMorphConfig("RRIGHT_Breasts Implants L") },
        };

        public RelativePosMorphHandler(RelativePosMorphConfigurator configurator)
        {
            _configurator = configurator;
            _configurator.DoInit();

            _configSets = new Dictionary<string, List<PositionMorphConfig>>
            {
                { Direction.DOWN, _downForceConfigs },
                { Direction.UP, _upForceConfigs },
                { Direction.BACK, _backForceConfigs },
                { Direction.FORWARD, _forwardForceConfigs },
                { Direction.LEFT, _leftForceConfigs },
                { Direction.RIGHT, _rightForceConfigs },
            };

            //SetInitialValues("downForce", downForceConfigs);
            SetInitialValues("upForce", _upForceConfigs);
            //SetInitialValues("backForce", backForceConfigs);
            //SetInitialValues("forwardForce", forwardForceConfigs);
            //SetInitialValues("leftForce", leftForceConfigs);
            //SetInitialValues("rightForce", rightForceConfigs);

            _configurator.InitMainUI();
            _configurator.EnableAdjustment.toggle.onValueChanged.AddListener((bool val) =>
            {
                if(!val)
                {
                    ResetAll();
                }
            });
            //_configurator.InitUISection("Upright morphs", downForceConfigs);
            _configurator.InitUISection(Direction.UP, _upForceConfigs);
            //_configurator.InitUISection("Lean back morphs", backForceConfigs);
            //_configurator.InitUISection("Lean forward morphs", forwardForceConfigs);
            //_configurator.InitUISection("Roll left morphs", rightForceConfigs);
            //_configurator.InitUISection("Roll right morphs", rightForceConfigs);
        }

        private void SetInitialValues(string fileName, List<PositionMorphConfig> configs)
        {
            //TODO use packagePath for default config location
            var json = Persistence.LoadFromPath(_configurator, $"{Globals.SAVES_DIR}{fileName}.json");
            foreach(var config in configs)
            {
                if(json.HasKey(config.Name))
                {
                    float value = json[config.Name].AsFloat;
                    // TODO massmul
                    config.SetMultipliers(value, 0f);
                }
            }
        }

        public bool IsEnabled()
        {
            return _configurator.EnableAdjustment.val;
        }

        public void UpdateDebugInfo(string text)
        {
            _configurator.DebugInfo.val = text;
        }

        public void Update(
            Vector3 positionDiff,
            float mass,
            float softness
        )
        {
            this.mass = mass;
            this.softness = softness;
            float x = positionDiff.x;
            float y = positionDiff.y;
            float z = positionDiff.z;

            // TODO separate l/r morphs only, separate calculation of diff
            //left
            if(x <= 0)
            {
                ResetMorphs(Direction.LEFT);
                UpdateMorphs(Direction.RIGHT, -x);
            }
            // right
            else
            {
                ResetMorphs(Direction.RIGHT);
                UpdateMorphs(Direction.LEFT, x);
            }

            // up
            if(y <= 0)
            {
                ResetMorphs(Direction.DOWN);
                UpdateMorphs(Direction.UP, -y);
            }
            // down
            else
            {
                ResetMorphs(Direction.UP);
                UpdateMorphs(Direction.DOWN, y);
            }

            // forward
            if(z <= 0)
            {
                ResetMorphs(Direction.BACK);
                UpdateMorphs(Direction.FORWARD, -z);
            }
            // back
            else
            {
                ResetMorphs(Direction.FORWARD);
                UpdateMorphs(Direction.BACK, z);
            }
        }

        private void UpdateMorphs(string configSetName, float diff)
        {
            float cubeRt = Mathf.Pow(diff, 1/3f);
            float diffVal = Calc.InverseSmoothStep(1, cubeRt, 0.15f, 0.5f);
            foreach(var configSet in _configSets[configSetName])
            {
                float newValue = configSet.UpdateVal(diffVal, mass, softness);
                _configurator.UpdateValueSlider(configSetName, configSet.Name, newValue);
            }
        }

        public void ResetAll()
        {
            ResetMorphs(Direction.DOWN);
            ResetMorphs(Direction.UP);
            ResetMorphs(Direction.BACK);
            ResetMorphs(Direction.FORWARD);
            ResetMorphs(Direction.LEFT);
            ResetMorphs(Direction.RIGHT);
        }

        private void ResetMorphs(string configSetName)
        {
            foreach(var configSet in _configSets[configSetName])
            {
                configSet.Reset();
                _configurator.UpdateValueSlider(configSetName, configSet.Name, 0f);
            }
        }
    }
}
