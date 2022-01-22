using System.Collections.Generic;

namespace TittyMagic
{
    internal interface IConfigurator
    {
        JSONStorableBool EnableAdjustment { get; }
        JSONStorableString DebugInfo { get; }

        void InitMainUI();

        void ResetUISectionGroups();

        void InitUISectionGroup(string key, List<Config> configs);

        void AddButtonListeners();

        void UpdateValueSlider(string sectionGroupName, string configName, float value);
    }
}
