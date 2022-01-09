using MVR.FileManagementSecure;
using SimpleJSON;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TittyMagic
{
    internal class RelativePosMorphConfigurator : MVRScript
    {
        private float _scale;
        private float _softness;

        private string _saveDir = SuperController.singleton.savesDir + @"tmconfig\";
        private string _lastBrowseDir;
        private const string _saveExt = "json";

        private JSONStorableBool _enableAdjustment;

        public bool Enabled => _enableAdjustment.val;

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
            { new UIConfigurationUnit("UPSD_Breast Move Up") },
            { new UIConfigurationUnit("UPSD_ChestUp") },
            { new UIConfigurationUnit("UPSD_Breast Height") },
            { new UIConfigurationUnit("UPSD_Breast Sag1") },
            { new UIConfigurationUnit("UPSD_Breast Sag2") },
            { new UIConfigurationUnit("UPSD_Breasts Natural") },
            { new UIConfigurationUnit("UPSD_Areola UpDown") },
            { new UIConfigurationUnit("UPSD_Breast Rotate Up") },
            { new UIConfigurationUnit("UPSD_Breast Top Curve2") },
            { new UIConfigurationUnit("Breast look up") },
            { new UIConfigurationUnit("UPSD_Breast Top Curve1") },
            { new UIConfigurationUnit("UPSD_Breasts Height") },
            { new UIConfigurationUnit("Breast Height Lower") },
            { new UIConfigurationUnit("Breasts Under Curve") },
            { new UIConfigurationUnit("UPSD_ChestUnderBreast") },
            { new UIConfigurationUnit("UPSD_Breast Under Smoother1") },
            { new UIConfigurationUnit("UPSD_Breast Under Smoother2") },
            { new UIConfigurationUnit("UPSD_Breast Under Smoother3") },
            { new UIConfigurationUnit("UPSD_Breast Under Smoother4") },
            { new UIConfigurationUnit("UPSD_Breast Height Upper") },
            { new UIConfigurationUnit("UPSD_Breasts Upward Slope") },
            { new UIConfigurationUnit("UPSD_Chest Height") },
            { new UIConfigurationUnit("Breast upper down") },
            { new UIConfigurationUnit("Breasts Small Top Slope") },
            { new UIConfigurationUnit("Breasts Size") },
            { new UIConfigurationUnit("UPSD_Breasts Implants") },
            { new UIConfigurationUnit("UPSD_Breast Diameter") },
            { new UIConfigurationUnit("LBACK_Breast Zero") },
            { new UIConfigurationUnit("UPSD_Breast flat") },
            { new UIConfigurationUnit("UPSD_Breasts Flatten") },
            { new UIConfigurationUnit("UPSD_Center Gap Depth") },
            { new UIConfigurationUnit("UPSD_Center Gap Height") },
            { new UIConfigurationUnit("UPSD_Center Gap UpDown") },
            { new UIConfigurationUnit("UPSD_Chest Smoother") },
            { new UIConfigurationUnit("UPSD_Breasts Hang Forward") },
            { new UIConfigurationUnit("UPSD_Breast Pointed") },
            { new UIConfigurationUnit("UPSD_Breast Diameter(Pose)") },
            { new UIConfigurationUnit("UPSD_BreastsShape2") },
            { new UIConfigurationUnit("Breast Round") },
            { new UIConfigurationUnit("Breast move inside") },
            { new UIConfigurationUnit("UPSD_Breasts TogetherApart") },
            { new UIConfigurationUnit("ChestSeparateBreasts") },
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
            FileManagerSecure.CreateDirectory(_saveDir);
            _lastBrowseDir = _saveDir;
        }

        public void DoInit()
        {
            _enableAdjustment = UI.NewToggle(this, "Enable", true, false);
            _enableAdjustment.toggle.onValueChanged.AddListener((bool val) =>
            {
                if(!val)
                {
                    ResetAll();
                }
            });
            UI.NewSpacer(this, 50, true);

            InitUISection("Down force morphs", downForceConfigs);
            InitUISection("Up force morphs", upForceConfigs);
            InitUISection("Back force morphs", backForceConfigs);
            InitUISection("Forward force morphs", forwardForceConfigs);
            InitUISection("Left force morphs", leftForceConfigs);
            InitUISection("Right force morphs", rightForceConfigs);

            try
            {
                HandleLoad(_saveDir + "upForceMultipliers.json", upForceConfigs);
            }
            catch(Exception)
            {
                Log.Error($"Default configuration file 'upForceMultipliers.json' not found in default path {_saveDir}");
            }
        }

        private void InitUISection(string title, List<UIConfigurationUnit> configs)
        {
            var saveButton = CreateButton("Save JSON", true);
            var loadButton = CreateButton("Load JSON", true);

            UI.NewTextField(this, title, 32, 115, false);
            foreach(var item in configs)
                item.Init(this);

            AddSaveButtonListener(saveButton, configs);
            AddLoadButtonListener(loadButton, configs);
        }

        private void AddSaveButtonListener(UIDynamicButton button, List<UIConfigurationUnit> configs)
        {
            button.button.onClick.AddListener(() =>
            {
                SuperController.singleton.NormalizeMediaPath(_lastBrowseDir); // Sets dir if path exists
                SuperController.singleton.GetMediaPathDialog((string path) => HandleSave(path, configs), _saveExt);

                // Update the browser to be a Save browser
                uFileBrowser.FileBrowser browser = SuperController.singleton.mediaFileBrowserUI;
                browser.SetTextEntry(true);
                browser.fileEntryField.text = string.Format("{0}.{1}", ((int) (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString(), _saveExt);
                browser.ActivateFileNameField();
            });
        }

        private void HandleSave(string path, List<UIConfigurationUnit> configs)
        {
            if(string.IsNullOrEmpty(path))
            {
                return;
            }
            _lastBrowseDir = path.Substring(0, path.LastIndexOfAny(new char[] { '/', '\\' })) + @"\";

            if(!path.ToLower().EndsWith(_saveExt.ToLower()))
            {
                path += "." + _saveExt;
            }

            var json = new JSONClass();
            foreach(var item in configs)
            {
                json[item.Name].AsFloat = item.GetMultiplier();
            }
            SaveJSON(json, path);
        }

        private void AddLoadButtonListener(UIDynamicButton button, List<UIConfigurationUnit> configs)
        {
            button.button.onClick.AddListener(() =>
            {
                SuperController.singleton.NormalizeMediaPath(_lastBrowseDir); // Sets dir if path exists
                SuperController.singleton.GetMediaPathDialog((string path) => HandleLoad(path, configs), _saveExt);
            });
        }

        private void HandleLoad(string path, List<UIConfigurationUnit> configs)
        {
            if(string.IsNullOrEmpty(path))
            {
                return;
            }
            _lastBrowseDir = path.Substring(0, path.LastIndexOfAny(new char[] { '/', '\\' })) + @"\";
            JSONClass json = LoadJSON(path).AsObject;
            foreach(var item in configs)
            {
                if(json.HasKey(item.Name))
                {
                    item.SetMultiplier(json[item.Name].AsFloat);
                }
            }
        }

        public void Update(
            Vector3 positionDiff,
            float scale,
            float softness
        )
        {
            _scale = scale;
            _softness = softness;
            float x = positionDiff.x;
            float y = positionDiff.y;
            float z = positionDiff.z;

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
            // max absolute diff is quite small so multiply before interpolating
            // diff value at high forces at max softness should be close to 1
            float diffVal = CustomSmoothStep(4 * diff);
            foreach(var item in configs)
            {
                item.UpdateMorphValue(diffVal, _scale, _softness);
            }
        }

        // https://www.desmos.com/calculator/3zhzwbfrxd
        private float CustomSmoothStep(float val)
        {
            float p = 0.18f;
            float s = 0.295f;
            float c = 2/(1 - s) - 1;

            if(val >= 1)
            {
                return 1;
            }

            if(val <= p)
            {
                return f1(val, p, c);
            }

            return 1 - f1(1 - val, 1 - p, c);
        }

        private float f1(float val, float p, float c)
        {
            return Mathf.Pow(val, c)/Mathf.Pow(p, c - 1);
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
