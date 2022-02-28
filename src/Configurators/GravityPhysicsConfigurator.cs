using SimpleJSON;
using System;
using System.Collections.Generic;

namespace TittyMagic
{
    internal class GravityPhysicsConfigurator : MVRScript, IConfigurator
    {
        private readonly Dictionary<string, string> _titles = new Dictionary<string, string>
        {
            { Direction.DOWN, "Upright physics" },
            { Direction.UP, "Upside down physics" },
            { Direction.BACK, "Lean back physics" },
            { Direction.FORWARD, "Lean forward physics" },
            { Direction.LEFT, "Roll left physics" },
            { Direction.RIGHT, "Roll right physics" },
        };

        private string _lastBrowseDir;
        private const string SAVE_EXT = "json";

        private UIDynamicButton _saveButton;
        private UIDynamicButton _loadButton;

        public JSONStorableBool EnableAdjustment { get; private set; }

        // dummy
        public JSONStorableString DebugInfo => null;

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
            this.NewSpacer(50f);
            _saveButton = CreateButton("Save JSON", true);
            _loadButton = CreateButton("Load JSON", true);
        }

        public void ResetUISectionGroups()
        {
            _uiSectionGroups = new Dictionary<string, Dictionary<string, ConfiguratorUISection>>
            {
                { Direction.DOWN, new Dictionary<string, ConfiguratorUISection>() },
                { Direction.UP, new Dictionary<string, ConfiguratorUISection>() },
                { Direction.BACK, new Dictionary<string, ConfiguratorUISection>() },
                { Direction.FORWARD, new Dictionary<string, ConfiguratorUISection>() },
                { Direction.LEFT, new Dictionary<string, ConfiguratorUISection>() },
                { Direction.RIGHT, new Dictionary<string, ConfiguratorUISection>() },
            };
        }

        public void InitUISectionGroup(string key, List<Config> configs)
        {
            this.NewTextField(_titles[key], _titles[key], 40, 115);
            this.NewSpacer(115f, true);

            var group = _uiSectionGroups[key];
            foreach(var config in configs)
            {
                var gravityPhysicsConfig = (GravityPhysicsConfig) config;
                group.Add(gravityPhysicsConfig.Name, new ConfiguratorUISection(this, gravityPhysicsConfig));
            }
        }

        public void AddButtonListeners()
        {
            AddSaveButtonListener(_saveButton);
            AddLoadButtonListener(_loadButton);
        }

        private void AddSaveButtonListener(UIDynamicButton button)
        {
            button.button.onClick.AddListener(
                () =>
                {
                    SuperController.singleton.NormalizeMediaPath(_lastBrowseDir ?? Persistence.MakeDefaultDir()); // Sets dir if path exists
                    SuperController.singleton.GetMediaPathDialog(HandleSave, SAVE_EXT);

                    // Update the browser to be a Save browser
                    var browser = SuperController.singleton.mediaFileBrowserUI;
                    browser.SetTextEntry(true);
                    browser.fileEntryField.text = $"{((int) (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString()}.{SAVE_EXT}";
                    browser.ActivateFileNameField();
                }
            );
        }

        private void AddLoadButtonListener(UIDynamicButton button)
        {
            button.button.onClick.AddListener(
                () =>
                {
                    SuperController.singleton.NormalizeMediaPath(_lastBrowseDir ?? Persistence.MakeDefaultDir()); // Sets dir if path exists
                    SuperController.singleton.GetMediaPathDialog(HandleLoad, SAVE_EXT);
                }
            );
        }

        private void HandleSave(string path)
        {
            var json = new JSONClass();
            _uiSectionGroups.Keys.ToList()
                .ForEach(
                    key =>
                    {
                        var sectionGroup = _uiSectionGroups[key];
                        var groupJson = new JSONClass();
                        sectionGroup.Values.ToList()
                            .ForEach(
                                item =>
                                {
                                    groupJson[item.Name]["Type"] = item.TypeStorable.val;
                                    groupJson[item.Name]["IsNegative"].AsBool = item.IsNegativeStorable.val;
                                    groupJson[item.Name]["Multiplier1"].AsFloat = Calc.RoundToDecimals(item.Multiplier1Storable.val, 1000f);
                                    groupJson[item.Name]["Multiplier2"].AsFloat = Calc.RoundToDecimals(item.Multiplier2Storable.val, 1000f);
                                }
                            );
                        json[key] = groupJson;
                    }
                );

            Persistence.SaveToPath(this, json, path, SAVE_EXT, dir => { _lastBrowseDir = dir; });
        }

        private static void HandleLoad(string path)
        {
            // TODO implement
        }
    }
}
