using System.Collections.Generic;
using UnityEngine;

namespace TittyMagic
{
    internal class GravityMorphHandler
    {
        private GravityMorphConfigurator _configurator;

        private float roll;
        private float pitch;
        private float mass;
        private float gravity;

        private Dictionary<string, List<PositionMorphConfig>> _configSets;

        //private List<PositionMorphConfig> _gravityOffsetMorphs = new List<PositionMorphConfig>
        //{
        //    { new PositionMorphConfig("TM_UprightSmootherOffset") },
        //    { new PositionMorphConfig("UPR_Breast Under Smoother1") },
        //    { new PositionMorphConfig("UPR_Breast Under Smoother3") },
        //    { new PositionMorphConfig("UPR_Breast Under Smoother4") },
        //};

        private List<PositionMorphConfig> _uprightConfigs = new List<PositionMorphConfig>
        {
            { new PositionMorphConfig("UPR_Breast Move Down") },
            { new PositionMorphConfig("UPR_Chest Height") },
            { new PositionMorphConfig("UPR_Breast Rotate Up") },
            { new PositionMorphConfig("UPR_Breast Under Smoother1") },
            { new PositionMorphConfig("UPR_Breast Under Smoother3") },
            { new PositionMorphConfig("UPR_Breast Under Smoother4") },
            { new PositionMorphConfig("UPR_Breasts Natural") },
        };

        private List<PositionMorphConfig> _upsideDownConfigs = new List<PositionMorphConfig>
        {
            //{ new PositionMorphConfig("UPSD_Breast Move Up") },
            { new PositionMorphConfig("UPSD_ChestUp") },
            { new PositionMorphConfig("UPSD_Breast Height") },
            { new PositionMorphConfig("UPSD_Breast Sag1") },
            { new PositionMorphConfig("UPSD_Breast Sag2") },
            { new PositionMorphConfig("UPSD_Breasts Natural") },
            { new PositionMorphConfig("UPSD_Areola UpDown") },
            { new PositionMorphConfig("UPSD_Breast Rotate Up") },
            { new PositionMorphConfig("UPSD_Breast Top Curve2") },
            //{ new PositionMorphConfig("Breast look up") },//this
            //{ new PositionMorphConfig("UPSD_Breast Top Curve1") },//this
            //{ new PositionMorphConfig("UPSD_Breasts Height") },//this
            //{ new PositionMorphConfig("Breast Height Lower") },//this
            //{ new PositionMorphConfig("Breasts Under Curve") },//this
            //{ new PositionMorphConfig("UPSD_ChestUnderBreast") },//this
            //{ new PositionMorphConfig("UPSD_Breast Under Smoother1") },//this
            //{ new PositionMorphConfig("UPSD_Breast Under Smoother2") },//this
            //{ new PositionMorphConfig("UPSD_Breast Under Smoother3") },//this
            //{ new PositionMorphConfig("UPSD_Breast Under Smoother4") },//this
            //{ new PositionMorphConfig("UPSD_Breast Height Upper") },//this
            //{ new PositionMorphConfig("UPSD_Breasts Upward Slope") },//this
            //{ new PositionMorphConfig("UPSD_Chest Height") },//this
            //{ new PositionMorphConfig("Breast upper down") },//this
            //{ new PositionMorphConfig("Breasts Small Top Slope") },//this
            //{ new PositionMorphConfig("Breasts Size") },
            { new PositionMorphConfig("UPSD_Breasts Implants") },
            { new PositionMorphConfig("UPSD_Breast Diameter") },
            { new PositionMorphConfig("LBACK_Breast Zero") },
            { new PositionMorphConfig("UPSD_Breast flat") },
            { new PositionMorphConfig("UPSD_Breasts Flatten") },
            //{ new PositionMorphConfig("UPSD_Center Gap Depth") },//this
            //{ new PositionMorphConfig("UPSD_Center Gap Height") },//this
            //{ new PositionMorphConfig("UPSD_Center Gap UpDown") },//this
            //{ new PositionMorphConfig("UPSD_Chest Smoother") },//this
            { new PositionMorphConfig("UPSD_Breasts Hang Forward") },
            //{ new PositionMorphConfig("UPSD_Breast Pointed") },
            //{ new PositionMorphConfig("UPSD_Breast Diameter(Pose)") },
            { new PositionMorphConfig("UPSD_BreastsShape2") },
            //{ new PositionMorphConfig("Breast Round") },
            { new PositionMorphConfig("Breast move inside") },
            { new PositionMorphConfig("UPSD_Breasts TogetherApart") },
            //{ new PositionMorphConfig("ChestSeparateBreasts") },//this
        };

        private List<PositionMorphConfig> _leanBackConfigs = new List<PositionMorphConfig>
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

        private List<PositionMorphConfig> _leanForwardConfigs = new List<PositionMorphConfig>
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

        private List<PositionMorphConfig> _rollLeftConfigs = new List<PositionMorphConfig>
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

        private List<PositionMorphConfig> _rollRightConfigs = new List<PositionMorphConfig>
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

        public GravityMorphHandler(MVRScript configurator)
        {
            if(configurator != null)
            {
                _configurator = configurator as GravityMorphConfigurator;
                _configurator.DoInit();
            }

            _configSets = new Dictionary<string, List<PositionMorphConfig>>
            {
                { Direction.DOWN, _uprightConfigs },
                { Direction.UP, _upsideDownConfigs },
                { Direction.BACK, _leanBackConfigs },
                { Direction.FORWARD, _leanForwardConfigs },
                { Direction.LEFT, _rollLeftConfigs },
                { Direction.RIGHT, _rollRightConfigs },
            };

            //SetInitialValues("upright", uprightConfigs);
            SetInitialValues("upsideDown", _upsideDownConfigs);
            //SetInitialValues("leanBack", leanBackConfigs);
            //SetInitialValues("leanForward", leanForwardConfigs);
            //SetInitialValues("rollLeft", rollLeftConfigs);
            //SetInitialValues("rollRight", rollRightConfigs);

            _configurator.InitMainUI();
            _configurator.EnableAdjustment.toggle.onValueChanged.AddListener((bool val) =>
            {
                if(!val)
                {
                    ResetAll();
                }
            });
            //_configurator.InitUISection("Upright morphs", uprightConfigs);
            _configurator.InitUISection(Direction.UP, _upsideDownConfigs);
            //_configurator.InitUISection("Lean back morphs", leanBackConfigs);
            //_configurator.InitUISection("Lean forward morphs", leanForwardConfigs);
            //_configurator.InitUISection("Roll left morphs", rollLeftConfigs);
            //_configurator.InitUISection("Roll right morphs", rollRightConfigs);
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
            float roll,
            float pitch,
            float mass,
            float gravity
        )
        {
            this.roll = roll;
            this.pitch = pitch;
            this.mass = mass;
            this.gravity = gravity;

            //foreach(var it in gravityOffsetMorphs)
            //{
            //    it.UpdateVal();
            //}

            AdjustMorphsForRoll();
            AdjustMorphsForPitch();
        }

        private void AdjustMorphsForRoll()
        {
            // left
            if(roll >= 0)
            {
                ResetMorphs(Direction.RIGHT);
                UpdateRollMorphs(Direction.LEFT, roll);
            }
            // right
            else
            {
                ResetMorphs(Direction.LEFT);
                UpdateRollMorphs(Direction.RIGHT, -roll);
            }
        }

        private void AdjustMorphsForPitch()
        {
            // leaning forward
            if(pitch >= 0)
            {
                ResetMorphs(Direction.BACK);
                // upright
                if(pitch < 1)
                {
                    ResetMorphs(Direction.UP);
                    UpdatePitchMorphs(Direction.FORWARD, pitch);
                    UpdatePitchMorphs(Direction.DOWN, 1 - pitch);
                }
                // upside down
                else
                {
                    ResetMorphs(Direction.DOWN);
                    UpdatePitchMorphs(Direction.FORWARD, 2 - pitch);
                    UpdatePitchMorphs(Direction.UP, pitch - 1);
                }
            }
            // leaning back
            else
            {
                ResetMorphs(Direction.FORWARD);
                // upright
                if(pitch >= -1)
                {
                    ResetMorphs(Direction.UP);
                    UpdatePitchMorphs(Direction.BACK, -pitch);
                    UpdatePitchMorphs(Direction.DOWN, 1 + pitch);
                }
                // upside down
                else
                {
                    ResetMorphs(Direction.DOWN);
                    UpdatePitchMorphs(Direction.BACK, 2 + pitch);
                    UpdatePitchMorphs(Direction.UP, -pitch - 1);
                }
            }
        }

        private void UpdateRollMorphs(string configSetName, float effect)
        {
            foreach(var config in _configSets[configSetName])
            {
                float value = config.UpdateVal(effect, mass, gravity);
                _configurator.UpdateValueSlider(configSetName, config.Name, value);
            }
        }

        private void UpdatePitchMorphs(string configSetName, float effect)
        {
            foreach(var config in _configSets[configSetName])
            {
                float newValue = config.UpdateVal(effect * (1 - Mathf.Abs(roll)), mass, gravity);
                _configurator.UpdateValueSlider(configSetName, config.Name, newValue);
            }
        }

        public void ResetAll()
        {
            //foreach(var it in gravityOffsetMorphs)
            //    it.Reset();
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
