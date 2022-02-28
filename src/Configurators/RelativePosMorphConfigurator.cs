using System;
using System.Collections.Generic;
using SimpleJSON;
using static TittyMagic.Globals;

namespace TittyMagic
{
    internal class RelativePosMorphConfigurator : MVRScript, IConfigurator
    {
        private readonly Dictionary<string, string> _titles = new Dictionary<string, string>
        {
            { Direction.DOWN_L, "Down force morphs L" },
            { Direction.DOWN_R, "Down force morphs R" },
            { Direction.UP_L, "Up force morphs L" },
            { Direction.UP_R, "Up force morphs R" },
            { Direction.UP_C, "Up force morphs center" },
            { Direction.BACK_L, "Back force morphs L" },
            { Direction.BACK_R, "Back force morphs R" },
            { Direction.BACK_C, "Back force morphs center" },
            { Direction.FORWARD_L, "Forward force morphs L" },
            { Direction.FORWARD_R, "Forward force morphs R" },
            { Direction.FORWARD_C, "Forward force morphs center" },
            { Direction.LEFT_L, "Left force morphs L" },
            { Direction.LEFT_R, "Left force morphs R" },
            { Direction.RIGHT_L, "Right force morphs L" },
            { Direction.RIGHT_R, "Right force morphs R" },
        };

        private string _lastBrowseDir;
        private const string SAVE_EXT = "json";

        public JSONStorableBool EnableAdjustment { get; private set; }

        public JSONStorableString DebugInfo { get; private set; }

        private Dictionary<string, Dictionary<string, ConfiguratorUISection>> _uiSectionGroups;

        public void UpdateValueSlider(string sectionGroupName, string configName, float value)
        {
            if(!_uiSectionGroups.ContainsKey(sectionGroupName))
            {
                return;
            }

            var sectionGroup = _uiSectionGroups[sectionGroupName];
            if(sectionGroup.ContainsKey(configName))
            {
                var section = _uiSectionGroups[sectionGroupName][configName];
                section.ValueStorable.val = value;
            }
        }

        public void InitMainUI()
        {
            ResetUISectionGroups();
            EnableAdjustment = this.NewToggle("Enable", true);
            DebugInfo = this.NewTextField("positionDiffInfo", "", 20, 115, true);
            var exportValuesButton = CreateButton("Export values JSON");
            AddExportButtonListener(exportValuesButton);
        }

        private void AddExportButtonListener(UIDynamicButton button)
        {
            button.button.onClick.AddListener(
                () =>
                {
                    SuperController.singleton.NormalizeMediaPath($@"{PLUGIN_PATH}settings\"); // Sets dir if path exists
                    SuperController.singleton.GetMediaPathDialog(
                        path =>
                        {
                            var json = new JSONClass();
                            _uiSectionGroups.Keys.ToList()
                                .ForEach(
                                    key =>
                                    {
                                        var sectionGroup = _uiSectionGroups[key];
                                        var groupJson = new JSONClass();
                                        sectionGroup.Values.ToList().ForEach(item => { groupJson[item.Name]["Value"].AsFloat = Calc.RoundToDecimals(item.ValueStorable.val, 1000f); });
                                        json[key] = groupJson;
                                    }
                                );
                            Persistence.SaveToPath(this, json, path, SAVE_EXT, dir => { _lastBrowseDir = dir; });
                        },
                        SAVE_EXT
                    );

                    // Update the browser to be a Save browser
                    var browser = SuperController.singleton.mediaFileBrowserUI;
                    browser.SetTextEntry(true);
                    browser.fileEntryField.text = $"{((int) (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString()}.{SAVE_EXT}";
                    browser.ActivateFileNameField();
                }
            );
        }

        public void ResetUISectionGroups()
        {
            _uiSectionGroups = new Dictionary<string, Dictionary<string, ConfiguratorUISection>>
            {
                { Direction.DOWN_L, new Dictionary<string, ConfiguratorUISection>() },
                { Direction.DOWN_R, new Dictionary<string, ConfiguratorUISection>() },
                { Direction.UP_L, new Dictionary<string, ConfiguratorUISection>() },
                { Direction.UP_R, new Dictionary<string, ConfiguratorUISection>() },
                { Direction.UP_C, new Dictionary<string, ConfiguratorUISection>() },
                // { Direction.BACK, new Dictionary<string, ConfiguratorUISection>() },
                // { Direction.FORWARD, new Dictionary<string, ConfiguratorUISection>() },
                { Direction.LEFT_L, new Dictionary<string, ConfiguratorUISection>() },
                { Direction.LEFT_R, new Dictionary<string, ConfiguratorUISection>() },
                { Direction.RIGHT_L, new Dictionary<string, ConfiguratorUISection>() },
                { Direction.RIGHT_R, new Dictionary<string, ConfiguratorUISection>() },
            };
        }

        public void InitUISectionGroup(string key, List<Config> configs)
        {
            this.NewTextField(_titles[key], $"{_titles[key]}", 40, 115);
            var saveButton = CreateButton("Save JSON", true);
            var loadButton = CreateButton("Load JSON", true);

            var group = _uiSectionGroups[key];

            foreach(var config in configs)
            {
                var morphConfig = (MorphConfig) config;
                group.Add(morphConfig.Name, new ConfiguratorUISection(this, morphConfig));
            }

            AddSaveButtonListener(saveButton, group.Values.ToList());
            AddLoadButtonListener(loadButton, group.Values.ToList());
        }

        // dummy
        public void AddButtonListeners()
        {
        }

        private void AddSaveButtonListener(UIDynamicButton button, List<ConfiguratorUISection> sections)
        {
            button.button.onClick.AddListener(
                () =>
                {
                    SuperController.singleton.NormalizeMediaPath(_lastBrowseDir ?? Persistence.MakeDefaultDir()); // Sets dir if path exists
                    SuperController.singleton.GetMediaPathDialog(path => HandleSave(path, sections), SAVE_EXT);

                    // Update the browser to be a Save browser
                    var browser = SuperController.singleton.mediaFileBrowserUI;
                    browser.SetTextEntry(true);
                    browser.fileEntryField.text = $"{((int) (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString()}.{SAVE_EXT}";
                    browser.ActivateFileNameField();
                }
            );
        }

        private void AddLoadButtonListener(UIDynamicButton button, List<ConfiguratorUISection> sections)
        {
            button.button.onClick.AddListener(
                () =>
                {
                    SuperController.singleton.NormalizeMediaPath(_lastBrowseDir ?? Persistence.MakeDefaultDir()); // Sets dir if path exists
                    SuperController.singleton.GetMediaPathDialog(path => HandleLoad(path, sections), SAVE_EXT);
                }
            );
        }

        private void HandleSave(string path, List<ConfiguratorUISection> sections)
        {
            var json = new JSONClass();
            foreach(var item in sections)
            {
                json[item.Name]["IsNegative"].AsBool = item.IsNegativeStorable.val;
                json[item.Name]["Multiplier1"].AsFloat = Calc.RoundToDecimals(item.Multiplier1Storable.val, 1000f);
                json[item.Name]["Multiplier2"].AsFloat = Calc.RoundToDecimals(item.Multiplier2Storable.val, 1000f);
            }

            Persistence.SaveToPath(this, json, path, SAVE_EXT, dir => { _lastBrowseDir = dir; });
        }

        // TODO fix, doesn't work
        private void HandleLoad(string path, List<ConfiguratorUISection> sections)
        {
            Persistence.LoadFromPath(
                this,
                path,
                (dir, json) =>
                {
                    _lastBrowseDir = dir;
                    foreach(var item in sections)
                    {
                        if(json.HasKey(item.Name))
                        {
                            item.Multiplier1Storable.val = json[item.Name]["Multiplier1"].AsFloat;
                            item.Multiplier2Storable.val = json[item.Name]["Multiplier2"].AsFloat;
                        }
                    }
                }
            );
        }
    }
}
