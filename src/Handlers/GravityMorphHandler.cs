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

        private Dictionary<string, List<MorphConfig>> _configSets;

        //private List<MorphConfig> _gravityOffsetMorphs = new List<MorphConfig>
        //{
        //    { new MorphConfig("TM_UprightSmootherOffset") },
        //    { new MorphConfig("UPR_Breast Under Smoother1") },
        //    { new MorphConfig("UPR_Breast Under Smoother3") },
        //    { new MorphConfig("UPR_Breast Under Smoother4") },
        //};

        private List<MorphConfig> _uprightConfigs = new List<MorphConfig>
        {
            { new MorphConfig("UPR_Breast Move Down") },
            { new MorphConfig("UPR_Chest Height") },
            { new MorphConfig("UPR_Breast Rotate Up") },
            { new MorphConfig("UPR_Breast Under Smoother1") },
            { new MorphConfig("UPR_Breast Under Smoother3") },
            { new MorphConfig("UPR_Breast Under Smoother4") },
            { new MorphConfig("UPR_Breasts Natural") },
        };

        private List<MorphConfig> _upsideDownConfigs = new List<MorphConfig>
        {
            //{ new MorphConfig("UPSD_Breast Move Up") },
            { new MorphConfig("UPSD_ChestUp") },
            { new MorphConfig("UPSD_Breast Height") },
            { new MorphConfig("UPSD_Breast Sag1") },
            { new MorphConfig("UPSD_Breast Sag2") },
            { new MorphConfig("UPSD_Breasts Natural") },
            { new MorphConfig("UPSD_Areola UpDown") },
            { new MorphConfig("UPSD_Breast Rotate Up") },
            { new MorphConfig("UPSD_Breast Top Curve2") },
            //{ new MorphConfig("Breast look up") },//this
            //{ new MorphConfig("UPSD_Breast Top Curve1") },//this
            //{ new MorphConfig("UPSD_Breasts Height") },//this
            //{ new MorphConfig("Breast Height Lower") },//this
            //{ new MorphConfig("Breasts Under Curve") },//this
            //{ new MorphConfig("UPSD_ChestUnderBreast") },//this
            //{ new MorphConfig("UPSD_Breast Under Smoother1") },//this
            //{ new MorphConfig("UPSD_Breast Under Smoother2") },//this
            //{ new MorphConfig("UPSD_Breast Under Smoother3") },//this
            //{ new MorphConfig("UPSD_Breast Under Smoother4") },//this
            //{ new MorphConfig("UPSD_Breast Height Upper") },//this
            //{ new MorphConfig("UPSD_Breasts Upward Slope") },//this
            //{ new MorphConfig("UPSD_Chest Height") },//this
            //{ new MorphConfig("Breast upper down") },//this
            //{ new MorphConfig("Breasts Small Top Slope") },//this
            //{ new MorphConfig("Breasts Size") },
            { new MorphConfig("UPSD_Breasts Implants") },
            { new MorphConfig("UPSD_Breast Diameter") },
            { new MorphConfig("LBACK_Breast Zero") },
            { new MorphConfig("UPSD_Breast flat") },
            { new MorphConfig("UPSD_Breasts Flatten") },
            //{ new MorphConfig("UPSD_Center Gap Depth") },//this
            //{ new MorphConfig("UPSD_Center Gap Height") },//this
            //{ new MorphConfig("UPSD_Center Gap UpDown") },//this
            //{ new MorphConfig("UPSD_Chest Smoother") },//this
            { new MorphConfig("UPSD_Breasts Hang Forward") },
            //{ new MorphConfig("UPSD_Breast Pointed") },
            //{ new MorphConfig("UPSD_Breast Diameter(Pose)") },
            { new MorphConfig("UPSD_BreastsShape2") },
            //{ new MorphConfig("Breast Round") },
            { new MorphConfig("Breast move inside") },
            { new MorphConfig("UPSD_Breasts TogetherApart") },
            //{ new MorphConfig("ChestSeparateBreasts") },//this
        };

        private List<MorphConfig> _leanBackConfigs = new List<MorphConfig>
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

        private List<MorphConfig> _leanForwardConfigs = new List<MorphConfig>
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

        private List<MorphConfig> _rollLeftConfigs = new List<MorphConfig>
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

        private List<MorphConfig> _rollRightConfigs = new List<MorphConfig>
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

        public GravityMorphHandler(MVRScript configurator)
        {
            if(configurator != null)
            {
                _configurator = configurator as GravityMorphConfigurator;
                _configurator.DoInit();
            }

            _configSets = new Dictionary<string, List<MorphConfig>>
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

        private void SetInitialValues(string fileName, List<MorphConfig> configs)
        {
            //TODO use packagePath for default config location
            var json = Persistence.LoadFromPath(_configurator, $@"{Globals.PLUGIN_PATH}\settings\morphmultipliers\{fileName}.json");
            foreach(var config in configs)
            {
                if(json.HasKey(config.Name))
                {
                    float value = json[config.Name].AsFloat;
                    config.SoftnessMultiplier = value;
                    config.MassMultiplier = 0f;
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
                UpdateValue(config, effect, mass, gravity);
                _configurator.UpdateValueSlider(configSetName, config.Name, config.Morph.morphValue);
            }
        }

        private void UpdatePitchMorphs(string configSetName, float effect)
        {
            float adjusted = effect * (1 - Mathf.Abs(roll));
            foreach(var config in _configSets[configSetName])
            {
                UpdateValue(config, adjusted, mass, gravity);
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
            foreach(var config in _configSets[configSetName])
            {
                config.Morph.morphValue = 0;
                _configurator.UpdateValueSlider(configSetName, config.Name, 0);
            }
        }
    }
}
