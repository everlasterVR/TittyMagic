using SimpleJSON;
using System;
using System.Collections.Generic;
using static TittyMagic.Globals;

namespace TittyMagic
{
    internal class GravityMorphConfigurator : MVRScript, IConfigurator
    {
        private readonly Dictionary<string, string> _titles = new Dictionary<string, string>
        {
            { Direction.DOWN, "Upright morphs" },
            { Direction.UP, "Upside down morphs" },
            { Direction.UP_C, "Upside down center morphs" },
            { Direction.BACK, "Lean back morphs" },
            { Direction.BACK_C, "Lean back center morphs" },
            { Direction.FORWARD, "Lean forward morphs" },
            { Direction.FORWARD_C, "Lean forward center morphs" },
            { Direction.LEFT, "Roll left morphs" },
            { Direction.RIGHT, "Roll right morphs" },
        };

        private string _lastBrowseDir;
        private const string SAVE_EXT = "json";

        public JSONStorableBool enableAdjustment { get; private set; }

        public JSONStorableString debugInfo { get; private set; }

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
                section.valueStorable.val = value;
            }
        }

        public void InitMainUI()
        {
            ResetUISectionGroups();
            enableAdjustment = this.NewToggle("Enable", true);
            debugInfo = this.NewTextField("positionDiffInfo", "", 20, 115, true);
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
                                        sectionGroup.Values.ToList().ForEach(item => { groupJson[item.name]["Value"].AsFloat = Calc.RoundToDecimals(item.valueStorable.val, 1000f); });
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
                { Direction.DOWN, new Dictionary<string, ConfiguratorUISection>() },
                { Direction.UP, new Dictionary<string, ConfiguratorUISection>() },
                { Direction.UP_C, new Dictionary<string, ConfiguratorUISection>() },
                { Direction.BACK, new Dictionary<string, ConfiguratorUISection>() },
                { Direction.FORWARD, new Dictionary<string, ConfiguratorUISection>() },
                { Direction.LEFT, new Dictionary<string, ConfiguratorUISection>() },
                { Direction.RIGHT, new Dictionary<string, ConfiguratorUISection>() },
            };
        }

        public void InitUISectionGroup(string key, List<Config> configs)
        {
            this.NewTextField(_titles[key], _titles[key], 40, 115);
            var saveButton = CreateButton("Save JSON", true);
            var loadButton = CreateButton("Load JSON", true);

            var group = _uiSectionGroups[key];

            foreach(var config in configs)
            {
                var morphConfig = (MorphConfig) config;
                group.Add(morphConfig.name, new ConfiguratorUISection(this, morphConfig));
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
                json[item.name]["IsNegative"].AsBool = item.isNegativeStorable.val;
                json[item.name]["Multiplier1"].AsFloat = Calc.RoundToDecimals(item.multiplier1Storable.val, 1000f);
                json[item.name]["Multiplier2"].AsFloat = Calc.RoundToDecimals(item.multiplier2Storable.val, 1000f);
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
                        if(json.HasKey(item.name))
                        {
                            item.multiplier1Storable.val = json[item.name]["Multiplier1"].AsFloat;
                            item.multiplier2Storable.val = json[item.name]["Multiplier2"].AsFloat;
                        }
                    }
                }
            );
        }
    }
}
