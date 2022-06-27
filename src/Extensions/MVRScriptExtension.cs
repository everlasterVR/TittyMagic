// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
using System;

public static class MVRScriptExtension
{
    public static string GetMorphsPath(this MVRScript script)
    {
        string packageId = GetPackageId(script);
        const string path = "Custom/Atom/Person/Morphs/female/everlaster";

        if(string.IsNullOrEmpty(packageId))
        {
            return $"{path}/{nameof(TittyMagic)}_dev";
        }

        return packageId + $":/{path}/{nameof(TittyMagic)}";
    }

    public static string PluginPath(this MVRScript script)
    {
        return $@"{script.GetPackagePath()}Custom\Scripts\everlaster\TittyMagic";
    }

    public static string GetPackagePath(this MVRScript script)
    {
        string packageId = script.GetPackageId();
        return packageId == "" ? "" : $"{packageId}:/";
    }

    //MacGruber / Discord 20.10.2020
    //Get path prefix of the package that contains this plugin
    public static string GetPackageId(this MVRScript script)
    {
        string id = script.name.Substring(0, script.name.IndexOf('_'));
        string filename = script.manager.GetJSON()["plugins"][id].Value;
        int idx = filename.IndexOf(":/", StringComparison.Ordinal);
        return idx >= 0 ? filename.Substring(0, idx) : "";
    }

    public static UIDynamicTextField InstantiateTextField(this MVRScript script)
    {
        return UnityEngine.Object.Instantiate(script.manager.configurableTextFieldPrefab).GetComponent<UIDynamicTextField>();
    }

    public static UIDynamicButton InstantiateButton(this MVRScript script)
    {
        return UnityEngine.Object.Instantiate(script.manager.configurableButtonPrefab).GetComponent<UIDynamicButton>();
    }

    public static UIDynamicSlider InstantiateSlider(this MVRScript script)
    {
        return UnityEngine.Object.Instantiate(script.manager.configurableSliderPrefab).GetComponent<UIDynamicSlider>();
    }

    public static UIDynamicToggle InstantiateToggle(this MVRScript script)
    {
        return UnityEngine.Object.Instantiate(script.manager.configurableTogglePrefab).GetComponent<UIDynamicToggle>();
    }

    public static UIDynamicPopup InstantiatePopup(this MVRScript script)
    {
        return UnityEngine.Object.Instantiate(script.manager.configurablePopupPrefab).GetComponent<UIDynamicPopup>();
    }

    public static UIDynamicColorPicker InstantiateColorPicker(this MVRScript script)
    {
        return UnityEngine.Object.Instantiate(script.manager.configurableColorPickerPrefab).GetComponent<UIDynamicColorPicker>();
    }

    public static void RemoveElement(this MVRScript script, UIDynamic element)
    {
        var textField = element as UIDynamicTextField;
        if(textField != null)
        {
            script.RemoveTextField(textField);
            return;
        }

        var button = element as UIDynamicButton;
        if(button != null)
        {
            script.RemoveButton(button);
            return;
        }

        var slider = element as UIDynamicSlider;
        if(slider != null)
        {
            script.RemoveSlider(slider);
            return;
        }

        var toggle = element as UIDynamicToggle;
        if(toggle != null)
        {
            script.RemoveToggle(toggle);
            return;
        }

        var popup = element as UIDynamicPopup;
        if(popup != null)
        {
            script.RemovePopup(popup);
            return;
        }

        var colorPicker = element as UIDynamicColorPicker;
        if(colorPicker != null)
        {
            script.RemoveColorPicker(colorPicker);
            return;
        }

        script.RemoveSpacer(element);
    }
}
