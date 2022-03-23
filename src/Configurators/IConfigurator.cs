using System.Collections.Generic;

namespace TittyMagic
{
    internal interface IConfigurator
    {
        JSONStorableBool enableAdjustment { get; }
        JSONStorableString debugInfo { get; }

        void InitMainUI();

        void ResetUISectionGroups();

        // ReSharper disable once UnusedMemberInSuper.Global
        void InitUISectionGroup(string key, List<Config> configs);

        void AddButtonListeners();

        void UpdateValueSlider(string sectionGroupName, string configName, float value);
    }
}
