// ReSharper disable MemberCanBePrivate.Global UnusedMember.Global UnusedMethodReturnValue.Global UnusedType.Global
using System;
using UnityEngine;

public static class MVRScriptExtension
{
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

    public static Transform InstantiateTextField(this MVRScript script, Transform parent = null) =>
        UnityEngine.Object.Instantiate(script.manager.configurableTextFieldPrefab, parent, false);

    public static Transform InstantiateButton(this MVRScript script, Transform parent = null) =>
        UnityEngine.Object.Instantiate(script.manager.configurableButtonPrefab, parent, false);

    public static Transform InstantiateSlider(this MVRScript script, Transform parent = null) =>
        UnityEngine.Object.Instantiate(script.manager.configurableSliderPrefab, parent, false);

    public static Transform InstantiateToggle(this MVRScript script, Transform parent = null) =>
        UnityEngine.Object.Instantiate(script.manager.configurableTogglePrefab, parent, false);

    public static Transform InstantiatePopup(this MVRScript script, Transform parent = null) =>
        UnityEngine.Object.Instantiate(script.manager.configurablePopupPrefab, parent, false);

    public static Transform InstantiateColorPicker(this MVRScript script, Transform parent = null) =>
        UnityEngine.Object.Instantiate(script.manager.configurableColorPickerPrefab, parent, false);

    public static Transform InstantiateSpacer(this MVRScript script, Transform parent = null) =>
        UnityEngine.Object.Instantiate(script.manager.configurableSpacerPrefab, parent, false);

    public static JSONStorableString NewJSONStorableString(
        this MVRScript script,
        string paramName,
        string startingValue,
        bool shouldRegister = true
    )
    {
        var storable = new JSONStorableString(paramName, startingValue);
        storable.storeType = JSONStorableParam.StoreType.Full;
        if(shouldRegister)
        {
            script.RegisterString(storable);
        }

        return storable;
    }

    public static JSONStorableBool NewJSONStorableBool(
        this MVRScript script,
        string paramName,
        bool startingValue,
        bool shouldRegister = true
    )
    {
        var storable = new JSONStorableBool(paramName, startingValue);
        storable.storeType = JSONStorableParam.StoreType.Full;
        if(shouldRegister)
        {
            script.RegisterBool(storable);
        }

        return storable;
    }

    public static JSONStorableFloat NewJSONStorableFloat(
        this MVRScript script,
        string paramName,
        float startingValue,
        float minValue,
        float maxValue,
        bool shouldRegister = true
    )
    {
        var storable = new JSONStorableFloat(paramName, startingValue, minValue, maxValue);
        storable.storeType = JSONStorableParam.StoreType.Full;
        if(shouldRegister)
        {
            script.RegisterFloat(storable);
        }

        return storable;
    }

    public static JSONStorableAction NewJSONStorableAction(
        this MVRScript script,
        string paramName,
        JSONStorableAction.ActionCallback callback,
        bool shouldRegister = true
    )
    {
        var storable = new JSONStorableAction(paramName, callback);
        if(shouldRegister)
        {
            script.RegisterAction(storable);
        }

        return storable;
    }

    public static UIDynamic NewSpacer(
        this MVRScript script,
        float height,
        bool rightSide = false
    )
    {
        if(height <= 0)
        {
            return null;
        }

        var spacer = script.CreateSpacer(rightSide);
        spacer.height = height;
        return spacer;
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
