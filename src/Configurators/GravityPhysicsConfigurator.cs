using SimpleJSON;
using System;
using System.Linq;
using System.Collections.Generic;

namespace TittyMagic
{
    internal class GravityPhysicsConfigurator : MVRScript
    {
        public Dictionary<string, string> titles = new Dictionary<string, string>
        {
            { Direction.DOWN, "Upright physics" },
            { Direction.UP, "Upside down physics" },
            { Direction.LEFT, "Roll left physics" },
            { Direction.RIGHT, "Roll right physics" },
        };

        private string _lastBrowseDir;
        private const string _saveExt = "json";

        private JSONStorableBool _enableAdjustment;
        private UIDynamicButton _saveButton;
        private UIDynamicButton _loadButton;

        public JSONStorableBool EnableAdjustment => _enableAdjustment;

        private Dictionary<string, Dictionary<string, ConfiguratorUISection>> _UISectionGroups;

        public void UpdateValueSlider(string sectionGroupName, string configName, float value)
        {
            if(!_UISectionGroups.ContainsKey(sectionGroupName))
            {
                return;
            }

            var sectionGroup = _UISectionGroups[sectionGroupName];
            if(sectionGroup.ContainsKey(configName))
            {
                var section = _UISectionGroups[sectionGroupName][configName];
                section.ValueStorable.val = value;
            }
        }

        public void InitMainUI()
        {
            ResetUISectionGroups();
            _enableAdjustment = UI.NewToggle(this, "Enable", true, false);
            UI.NewSpacer(this, 50f, false);
            _saveButton = CreateButton("Save JSON", true);
            _loadButton = CreateButton("Load JSON", true);
        }

        public void ResetUISectionGroups()
        {
            _UISectionGroups = new Dictionary<string, Dictionary<string, ConfiguratorUISection>> {
                { Direction.DOWN, new Dictionary<string, ConfiguratorUISection>() },
                { Direction.UP, new Dictionary<string, ConfiguratorUISection>() },
                { Direction.LEFT, new Dictionary<string, ConfiguratorUISection>() },
                { Direction.RIGHT, new Dictionary<string, ConfiguratorUISection>() },
            };
        }

        public void InitUISectionGroup(string key, List<GravityPhysicsConfig> configs)
        {
            UI.NewTextField(this, titles[key], titles[key], 40, 115, false);
            UI.NewSpacer(this, 115f, true);

            var group = _UISectionGroups[key];
            foreach(var config in configs)
            {
                group.Add(config.Name, new ConfiguratorUISection(this, config));
            }
        }

        public void AddButtonListeners()
        {
            AddSaveButtonListener(_saveButton);
            AddLoadButtonListener(_loadButton);
        }

        private void AddSaveButtonListener(UIDynamicButton button)
        {
            button.button.onClick.AddListener(() =>
            {
                //SuperController.singleton.NormalizeMediaPath($@"{Globals.PLUGIN_PATH}settings\morphmultipliers\animoptimized"); // Sets dir if path exists
                SuperController.singleton.NormalizeMediaPath(_lastBrowseDir ?? Persistence.MakeDefaultDir()); // Sets dir if path exists
                SuperController.singleton.GetMediaPathDialog((string path) => HandleSave(path), _saveExt);

                // Update the browser to be a Save browser
                uFileBrowser.FileBrowser browser = SuperController.singleton.mediaFileBrowserUI;
                browser.SetTextEntry(true);
                browser.fileEntryField.text = string.Format("{0}.{1}", ((int) (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString(), _saveExt);
                browser.ActivateFileNameField();
            });
        }

        private void AddLoadButtonListener(UIDynamicButton button)
        {
            button.button.onClick.AddListener(() =>
            {
                //SuperController.singleton.NormalizeMediaPath($@"{Globals.PLUGIN_PATH}settings\morphmultipliers\animoptimized"); // Sets dir if path exists
                SuperController.singleton.NormalizeMediaPath(_lastBrowseDir ?? Persistence.MakeDefaultDir()); // Sets dir if path exists
                SuperController.singleton.GetMediaPathDialog((string path) => HandleLoad(path), _saveExt);
            });
        }

        private void HandleSave(string path)
        {
            var json = new JSONClass();
            _UISectionGroups.Keys.ToList().ForEach(key =>
            {
                var sectionGroup = _UISectionGroups[key];
                var groupJson = new JSONClass();
                sectionGroup.Values.ToList().ForEach(item =>
                {
                    groupJson[item.Name]["IsNegative"].AsBool = item.IsNegativeStorable.val;
                    groupJson[item.Name]["Multiplier1"].AsFloat = Calc.RoundToDecimals(item.Multiplier1Storable.val, 1000f);
                    groupJson[item.Name]["Multiplier2"].AsFloat = Calc.RoundToDecimals(item.Multiplier2Storable.val, 1000f);
                });
                json[key] = groupJson;
            });

            Persistence.SaveToPath(this, json, path, _saveExt, (dir) =>
            {
                _lastBrowseDir = dir;
            });
        }

        private void HandleLoad(string path)
        {
            // TODO ímplement
        }
    }
}
