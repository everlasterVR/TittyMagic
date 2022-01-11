using System.Collections.Generic;
using UnityEngine;

namespace TittyMagic
{
    internal class RelativePosMorphHandler
    {
        private RelativePosMorphConfigurator _configurator;

        private float mass;
        private float softness;

        private Dictionary<string, List<MorphConfig>> _configSets;

        private List<MorphConfig> _downForceConfigs = new List<MorphConfig>
        {
            { new MorphConfig("UPR_Breast Move Down") },
            { new MorphConfig("UPR_Chest Height") },
            { new MorphConfig("UPR_Breast Rotate Up") },
            { new MorphConfig("UPR_Breast Under Smoother1") },
            { new MorphConfig("UPR_Breast Under Smoother3") },
            { new MorphConfig("UPR_Breast Under Smoother4") },
            { new MorphConfig("UPR_Breasts Natural") },
        };

        private List<MorphConfig> _upForceConfigs = new List<MorphConfig>
        {
            { new MorphConfig("UPSD_Breast Move Up") },
            { new MorphConfig("Breast look up") },
            { new MorphConfig("UPSD_Breast Top Curve1") },
            { new MorphConfig("UPSD_Breasts Height") },
            { new MorphConfig("Breast Height Lower") },
            { new MorphConfig("Breasts Under Curve") },
            { new MorphConfig("UPSD_ChestUnderBreast") },
            { new MorphConfig("UPSD_Breast Under Smoother1") },
            { new MorphConfig("UPSD_Breast Under Smoother2") },
            { new MorphConfig("UPSD_Breast Under Smoother3") },
            { new MorphConfig("UPSD_Breast Under Smoother4") },
            { new MorphConfig("UPSD_Breast Height Upper") },
            { new MorphConfig("UPSD_Breasts Upward Slope") },
            { new MorphConfig("UPSD_Chest Height") },
            { new MorphConfig("Breast upper down") },
            { new MorphConfig("Breasts Small Top Slope") },
            { new MorphConfig("UPSD_Center Gap Depth") },
            { new MorphConfig("UPSD_Center Gap Height") },
            { new MorphConfig("UPSD_Center Gap UpDown") },
            { new MorphConfig("UPSD_Chest Smoother") },
            { new MorphConfig("ChestSeparateBreasts") },
        };

        private List<MorphConfig> _backForceConfigs = new List<MorphConfig>
        {
            { new MorphConfig("LBACK_Breast Diameter") },
            { new MorphConfig("LBACK_Breast Height") },
            { new MorphConfig("LBACK_Breast Height Upper") },
            { new MorphConfig("LBACK_Breast Zero") },
            { new MorphConfig("LBACK_Breasts Flatten") },
            { new MorphConfig("LBACK_Chest Smoother") },
            { new MorphConfig("LBACK_Breast Depth Squash") },
            { new MorphConfig("LBACK_Breast Move S2S Out") },
            { new MorphConfig("LBACK_Breast Top Curve1") },
            { new MorphConfig("LBACK_Breast Top Curve2") },
            { new MorphConfig("LBACK_Breast Under Smoother1") },
            { new MorphConfig("LBACK_Breast Under Smoother3") },
            { new MorphConfig("LBACK_Breast Under Smoother2") },
            { new MorphConfig("LBACK_Breast Rotate Up") },
            { new MorphConfig("LBACK_Center Gap Smooth") },
            { new MorphConfig("LBACK_Chest Height") },
            { new MorphConfig("LBACK_ChestSmoothCenter") },
            { new MorphConfig("LBACK_ChestUp") },
        };

        private List<MorphConfig> _forwardForceConfigs = new List<MorphConfig>
        {
            { new MorphConfig("LFWD_Breast Diameter") },
            { new MorphConfig("LFWD_Breast Diameter(Pose)") },
            { new MorphConfig("LFWD_Breast Height2") },
            { new MorphConfig("LFWD_Breast Move Up") },
            { new MorphConfig("LFWD_Breast Side Smoother") },
            { new MorphConfig("LFWD_Breast Width") },
            { new MorphConfig("LFWD_Sternum Width") },
            { new MorphConfig("LFWD_Areola S2S") },
            { new MorphConfig("LFWD_Breast Depth") },
            { new MorphConfig("LFWD_Breasts Hang Forward") },
            { new MorphConfig("LFWD_Breasts TogetherApart") },
        };

        private List<MorphConfig> _leftForceConfigs = new List<MorphConfig>
        {
            { new MorphConfig("RLEFT_Areola S2S L") },
            { new MorphConfig("RLEFT_Areola S2S R") },
            { new MorphConfig("RLEFT_Breast Depth Squash R") },
            { new MorphConfig("RLEFT_Breast Diameter") },
            { new MorphConfig("RLEFT_Breast Move S2S In R") },
            { new MorphConfig("RLEFT_Breast Move S2S Out L") },
            { new MorphConfig("RLEFT_Breast Pointed") },
            { new MorphConfig("RLEFT_Breast Rotate X In L") },
            { new MorphConfig("RLEFT_Breast Rotate X In R") },
            { new MorphConfig("RLEFT_Breast Width L") },
            { new MorphConfig("RLEFT_Breast Width R") },
            { new MorphConfig("RLEFT_Breasts Hang Forward R") },
            { new MorphConfig("RLEFT_Center Gap Smooth") },
            { new MorphConfig("RLEFT_Centre Gap Narrow") },
        };

        private List<MorphConfig> _rightForceConfigs = new List<MorphConfig>
        {
            { new MorphConfig("RLEFT_Breast Under Smoother1") },
            { new MorphConfig("RLEFT_Breast Under Smoother3") },
            { new MorphConfig("RLEFT_Breasts Implants R") },
            { new MorphConfig("RRIGHT_Areola S2S L") },
            { new MorphConfig("RRIGHT_Areola S2S R") },
            { new MorphConfig("RRIGHT_Breast Depth Squash L") },
            { new MorphConfig("RRIGHT_Breast Diameter") },
            { new MorphConfig("RRIGHT_Breast Move S2S In L") },
            { new MorphConfig("RRIGHT_Breast Move S2S Out R") },
            { new MorphConfig("RRIGHT_Breast Pointed") },
            { new MorphConfig("RRIGHT_Breast Rotate X In L") },
            { new MorphConfig("RRIGHT_Breast Rotate X In R") },
            { new MorphConfig("RRIGHT_Breast Width L") },
            { new MorphConfig("RRIGHT_Breast Width R") },
            { new MorphConfig("RRIGHT_Breasts Hang Forward L") },
            { new MorphConfig("RRIGHT_Center Gap Smooth") },
            { new MorphConfig("RRIGHT_Centre Gap Narrow") },
            { new MorphConfig("RRIGHT_Breast Under Smoother1") },
            { new MorphConfig("RRIGHT_Breast Under Smoother3") },
            { new MorphConfig("RRIGHT_Breasts Implants L") },
        };

        public RelativePosMorphHandler(MVRScript configurator)
        {
            if(configurator != null)
            {
                _configurator = configurator as RelativePosMorphConfigurator;
                _configurator.DoInit();
            }

            _configSets = new Dictionary<string, List<MorphConfig>>
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

        private void SetInitialValues(string fileName, List<MorphConfig> configs)
        {
            Persistence.LoadFromPath(_configurator, $@"{Globals.PLUGIN_PATH}\settings\morphmultipliers\{fileName}.json", (dir, json) =>
            {
                foreach(var config in configs)
                {
                    if(json.HasKey(config.Name))
                    {
                        float value = json[config.Name].AsFloat;
                        config.SoftnessMultiplier = value;
                        config.MassMultiplier = 0f; //TODO actual values
                    }
                }
            });
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
            foreach(var config in _configSets[configSetName])
            {
                UpdateValue(config, diffVal, mass, softness);
                _configurator.UpdateValueSlider(configSetName, config.Name, config.Morph.morphValue);
            }
        }

        private void UpdateValue(MorphConfig config, float effect, float mass, float gravity)
        {
            config.Morph.morphValue =
                mass * config.MassMultiplier * effect / 2 +
                gravity * config.SoftnessMultiplier * effect / 2;
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
            foreach(var config in _configSets[configSetName])
            {
                config.Morph.morphValue = 0;
                _configurator.UpdateValueSlider(configSetName, config.Name, 0f);
            }
        }
    }
}
