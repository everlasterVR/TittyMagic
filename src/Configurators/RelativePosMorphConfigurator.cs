using MVR.FileManagementSecure;
using SimpleJSON;
using System;
using System.Collections.Generic;

namespace TittyMagic
{
    internal class RelativePosMorphConfigurator : MVRScript
    {
        public Dictionary<string, string> titles = new Dictionary<string, string>
        {
            { Direction.DOWN, "Down force morphs" },
            { Direction.UP, "Up force morphs" },
            { Direction.BACK, "Back force morphs" },
            { Direction.FORWARD, "Forward force morphs" },
            { Direction.LEFT, "Left force morphs" },
            { Direction.RIGHT, "Right force morphs" },
        };

        private string _lastBrowseDir;
        private const string _saveExt = "json";

        private JSONStorableBool _enableAdjustment;
        private JSONStorableString _debugInfo;

        public JSONStorableBool EnableAdjustment => _enableAdjustment;
        public JSONStorableString DebugInfo => _debugInfo;

        private Dictionary<string, Dictionary<string, ConfiguratorUISection>> UISectionGroups;

        public void DoInit()
        {
            UISectionGroups = new Dictionary<string, Dictionary<string, ConfiguratorUISection>> {
                { Direction.DOWN, new Dictionary<string, ConfiguratorUISection>() },
                { Direction.UP, new Dictionary<string, ConfiguratorUISection>() },
                { Direction.BACK, new Dictionary<string, ConfiguratorUISection>() },
                { Direction.FORWARD, new Dictionary<string, ConfiguratorUISection>() },
                { Direction.LEFT, new Dictionary<string, ConfiguratorUISection>() },
                { Direction.RIGHT, new Dictionary<string, ConfiguratorUISection>() },
            };

            FileManagerSecure.CreateDirectory(Globals.SAVES_DIR);
            _lastBrowseDir = Globals.SAVES_DIR;
        }

        public void UpdateValueSlider(string sectionGroupName, string configName, float value)
        {
            if(UISectionGroups.ContainsKey(sectionGroupName))
            {
                var sectionGroup = UISectionGroups[sectionGroupName];
                if(sectionGroup.ContainsKey(configName))
                {
                    var section = UISectionGroups[sectionGroupName][configName];
                    section.ValueStorable.val = value;
                }
            }
        }

        public void InitMainUI()
        {
            _enableAdjustment = UI.NewToggle(this, "Enable", false, false);
            _enableAdjustment.toggle.onValueChanged.AddListener((bool val) =>
            {
                if(!val)
                {
                    //TODO
                    //ResetAll();
                }
            });

            _debugInfo = UI.NewTextField(this, "positionDiffInfo", 24, 115, true);
            UI.NewSpacer(this, 50f, false);
        }

        public void InitUISection(string key, List<MorphConfig> configs)
        {
            UI.NewTextField(this, titles[key], 32, 115, false);
            var saveButton = CreateButton("Save JSON", true);
            var loadButton = CreateButton("Load JSON", true);

            var group = UISectionGroups[key];

            foreach(var config in configs)
            {
                group.Add(config.Name, new ConfiguratorUISection(this, config));
            }

            AddSaveButtonListener(saveButton, group.Values.ToList());
            AddLoadButtonListener(loadButton, group.Values.ToList());
        }

        private void AddSaveButtonListener(UIDynamicButton button, List<ConfiguratorUISection> sections)
        {
            button.button.onClick.AddListener(() =>
            {
                SuperController.singleton.NormalizeMediaPath(_lastBrowseDir); // Sets dir if path exists
                SuperController.singleton.GetMediaPathDialog((string path) => HandleSave(path, sections), _saveExt);

                // Update the browser to be a Save browser
                uFileBrowser.FileBrowser browser = SuperController.singleton.mediaFileBrowserUI;
                browser.SetTextEntry(true);
                browser.fileEntryField.text = string.Format("{0}.{1}", ((int) (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString(), _saveExt);
                browser.ActivateFileNameField();
            });
        }

        protected void AddLoadButtonListener(UIDynamicButton button, List<ConfiguratorUISection> sections)
        {
            button.button.onClick.AddListener(() =>
            {
                SuperController.singleton.NormalizeMediaPath(_lastBrowseDir); // Sets dir if path exists
                SuperController.singleton.GetMediaPathDialog((string path) => HandleLoad(path, sections), _saveExt);
            });
        }

        protected void HandleSave(string path, List<ConfiguratorUISection> sections)
        {
            var json = new JSONClass();
            foreach(var item in sections)
            {
                json[item.Name].AsFloat = item.MultiplierStorable.val;
            }
            Persistence.SaveToPath(this, json, path, _saveExt, (dir) =>
            {
                _lastBrowseDir = dir;
            });
        }

        protected void HandleLoad(string path, List<ConfiguratorUISection> sections)
        {
            var json = Persistence.LoadFromPath(this, path, (dir) =>
            {
                _lastBrowseDir = dir;
            });
            foreach(var item in sections)
            {
                if(json.HasKey(item.Name))
                {
                    item.MultiplierStorable.val = json[item.Name].AsFloat;
                }
            }
        }
    }
}
