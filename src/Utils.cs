﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TittyMagic
{
    internal static class Utils
    {
        public static void LogError(string message, string name = "")
        {
            SuperController.LogError(Format(message, name));
        }

        public static void LogMessage(string message, string name = "")
        {
            SuperController.LogMessage(Format(message, name));
        }

        private static string Format(string message, string name)
        {
            return $"{nameof(TittyMagic)} v{Script.VERSION}: {message}{(string.IsNullOrEmpty(name) ? "" : $" [{name}]")}";
        }

        // ReSharper disable once UnusedMember.Global
        public static MVRScript FindPluginOnAtom(Atom atom, string search)
        {
            string match = atom.GetStorableIDs().FirstOrDefault(s => s.Contains(search));
            return match == null ? null : atom.GetStorableByID(match) as MVRScript;
        }

        //MacGruber / Discord 20.10.2020
        //Get path prefix of the package that contains this plugin
        public static string GetPackageId(MVRScript script)
        {
            string id = script.name.Substring(0, script.name.IndexOf('_'));
            string filename = script.manager.GetJSON()["plugins"][id].Value;
            int idx = filename.IndexOf(":/", StringComparison.Ordinal);
            return idx >= 0 ? filename.Substring(0, idx) : "";
        }

        public static string GetPackagePath(MVRScript script)
        {
            string packageId = GetPackageId(script);
            return packageId == "" ? "" : $"{packageId}:/";
        }

        public static string NameValueString(
            string name,
            float value,
            float roundFactor = 1000f
        )
        {
            return $"{name} {Calc.RoundToDecimals(value, roundFactor)}";
        }
    }

    internal static class Curves
    {
        public static float QuadraticRegression(float f)
        {
            return (-0.173f * f * f) + (1.142f * f);
        }

        public static float QuadraticRegressionLesser(float f)
        {
            return (-0.115f * f * f) + (1.12f * f);
        }
    }

    internal static class Calc
    {
        public static float Roll(Quaternion q)
        {
            return 2 * InverseLerpToPi(Mathf.Asin((2 * q.x * q.y) + (2 * q.z * q.w)));
        }

        public static float Pitch(Quaternion q)
        {
            return InverseLerpToPi(Mathf.Atan2((2 * q.x * q.w) - (2 * q.y * q.z), 1 - (2 * q.x * q.x) - (2 * q.z * q.z)));
        }

        private static float InverseLerpToPi(float val)
        {
            if(val > 0)
            {
                return Mathf.InverseLerp(0, Mathf.PI, val);
            }

            return -Mathf.InverseLerp(0, Mathf.PI, -val);
        }

        // value returned is smoothed (for better animation) i.e. no longer maps linearly to the actual rotation angle
        public static float SmoothStep(float val)
        {
            if(val > 0)
            {
                return Mathf.SmoothStep(0, 1, val);
            }

            return -Mathf.SmoothStep(0, 1, -val);
        }

        // https://www.desmos.com/calculator/crrr1uryep
        // ReSharper disable once UnusedMember.Global
        public static float InverseSmoothStep(float value, float b, float curvature, float midpoint)
        {
            if(value < 0)
                return 0;
            if(value > b)
                return 1;

            float s = curvature < -2.99f ? -2.99f : curvature > 0.99f ? 0.99f : curvature;
            float p = midpoint * b;
            p = p < 0 ? 0 : p > b ? b : p;
            float c = (2 / (1 - s)) - (p / b);

            if(value < p)
                return F1(value, b, p, c);

            return 1 - F1(b - value, b, b - p, c);
        }

        private static float F1(float value, float b, float n, float c)
        {
            return Mathf.Pow(value, c) / (b * Mathf.Pow(n, c - 1));
        }

        public static float RoundToDecimals(float value, float roundFactor)
        {
            return Mathf.Round(value * roundFactor) / roundFactor;
        }

        public static Vector3 RelativePosition(Rigidbody origin, Vector3 position)
        {
            var difference = position - origin.position;
            return new Vector3(
                Vector3.Dot(difference, origin.transform.right),
                Vector3.Dot(difference, origin.transform.up),
                Vector3.Dot(difference, origin.transform.forward)
            );
        }

        // ReSharper disable once UnusedMember.Global
        public static Vector3 AveragePosition(List<Vector3> positions)
        {
            var sum = Vector3.zero;
            foreach(var position in positions)
            {
                sum += position;
            }

            return sum / positions.Count;
        }

        private static bool EqualWithin(float roundFactor, float v1, float v2)
        {
            return Mathf.Abs(v1 - v2) < 1 / roundFactor;
        }

        public static bool DeviatesAtLeast(float v1, float v2, int percent)
        {
            if(v1 > v2)
            {
                return (v1 - v2) / v1 > (float) percent / 100;
            }

            return (v2 - v1) / v2 > (float) percent / 100;
        }

        public static bool VectorEqualWithin(float roundFactor, Vector3 v1, Vector3 v2)
        {
            return EqualWithin(roundFactor, v1.x, v2.x) && EqualWithin(roundFactor, v1.y, v2.y) && EqualWithin(roundFactor, v1.z, v2.z);
        }

        public static float[] ExponentialMovingAverage(float[] source, float k)
        {
            float[] result = new float[source.Length];
            result[source.Length - 1] = source[source.Length - 1];
            for(int i = source.Length - 2; i >= 0; i--)
            {
                result[i] = (k * source[i]) + ((1 - k) * result[i + 1]);
            }

            return result;
        }
    }

    // ReSharper disable UnusedMember.Global MemberCanBePrivate.Global
    internal static class UI
    {
        public static Color darkOffGrayViolet = new Color(0.26f, 0.20f, 0.26f);
        public static Color gray = new Color(0.4f, 0.4f, 0.4f);
        public static Color lightPink = new Color(1f, 0.925f, 0.925f);
        public static Color offGrayRed = new Color(0.45f, 0.4f, 0.4f);
        public static Color offGrayViolet = new Color(0.80f, 0.75f, 0.80f);
        public static Color sliderGray = new Color(0, 0, 0, 0.498f);
        public static Color funkyCyan = new Color(0.596f, 1.000f, 0.780f);

        public static string LineBreak()
        {
            return "\n" + Size("\n", 12);
        }

        public static string Color(string text, Color color)
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";
        }

        public static string Size(string text, int size)
        {
            return $"<size={size}>{text}</size>";
        }

        public static JSONStorableFloat NewFloatSlider(
            this MVRScript script,
            string paramName,
            float startingValue,
            float minValue,
            float maxValue,
            string valueFormat,
            bool rightSide = false
        )
        {
            var storable = new JSONStorableFloat(paramName, startingValue, minValue, maxValue);
            storable.storeType = JSONStorableParam.StoreType.Full;
            script.RegisterFloat(storable);
            var slider = script.CreateSlider(storable, rightSide);
            slider.valueFormat = valueFormat;
            return storable;
        }

        public static UIDynamicSlider NewFloatSlider(
            this MVRScript script,
            JSONStorableFloat storable,
            string valueFormat,
            bool rightSide = false
        )
        {
            storable.storeType = JSONStorableParam.StoreType.Full;
            script.RegisterFloat(storable);
            var slider = script.CreateSlider(storable, rightSide);
            slider.valueFormat = valueFormat;
            return slider;
        }

        public static JSONStorableFloat NewIntSlider(
            this MVRScript script,
            string paramName,
            float startingValue,
            float minValue,
            float maxValue,
            bool rightSide = false
        )
        {
            var storable = new JSONStorableFloat(paramName, startingValue, minValue, maxValue);
            storable.storeType = JSONStorableParam.StoreType.Full;
            script.RegisterFloat(storable);
            var slider = script.CreateSlider(storable, rightSide);
            slider.valueFormat = "0f";
            slider.slider.wholeNumbers = true;
            return storable;
        }

        public static UIDynamicSlider NewIntSlider(
            this MVRScript script,
            JSONStorableFloat storable,
            bool rightSide = false
        )
        {
            storable.storeType = JSONStorableParam.StoreType.Full;
            script.RegisterFloat(storable);
            var slider = script.CreateSlider(storable, rightSide);
            slider.valueFormat = "0f";
            slider.slider.wholeNumbers = true;
            return slider;
        }

        public static JSONStorableString NewTextField(
            this MVRScript script,
            string paramName,
            string initialValue,
            int fontSize,
            int height = 120,
            bool rightSide = false
        )
        {
            var storable = new JSONStorableString(paramName, initialValue);
            var textField = script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = fontSize;
            textField.height = height;
            return storable;
        }

        public static InputField NewInputField(UIDynamicTextField textField)
        {
            var inputField = textField.gameObject.AddComponent<InputField>();
            inputField.textComponent = textField.UItext;
            inputField.text = textField.text;
            inputField.textComponent.fontSize = textField.UItext.fontSize;

            var layoutElement = inputField.GetComponent<LayoutElement>();
            layoutElement.minHeight = 0f;
            layoutElement.preferredHeight = textField.height;

            return inputField;
        }

        public static JSONStorableBool NewToggle(
            this MVRScript script,
            string paramName,
            bool startingValue,
            bool rightSide = false
        )
        {
            var storable = new JSONStorableBool(paramName, startingValue);
            storable.storeType = JSONStorableParam.StoreType.Full;
            script.CreateToggle(storable, rightSide);
            script.RegisterBool(storable);
            return storable;
        }

        public static UIDynamicToggle NewToggle(
            this MVRScript script,
            JSONStorableBool storable,
            bool rightSide = false
        )
        {
            storable.storeType = JSONStorableParam.StoreType.Full;
            script.RegisterBool(storable);
            var toggle = script.CreateToggle(storable, rightSide);
            return toggle;
        }

        // ReSharper disable once UnusedMethodReturnValue.Global
        public static UIDynamic NewSpacer(
            this MVRScript script,
            float height,
            bool rightSide = false
        )
        {
            var spacer = script.CreateSpacer(rightSide);
            spacer.height = height;
            return spacer;
        }

        public static Dictionary<string, UIDynamicButton> CreateRadioButtonGroup(
            this MVRScript script,
            JSONStorableStringChooser jsc,
            bool rightSide = false
        )
        {
            var buttons = new Dictionary<string, UIDynamicButton>();
            jsc.choices.ForEach(
                choice =>
                {
                    var btn = script.CreateButton(RadioButtonLabel(choice, choice == jsc.defaultVal), rightSide);
                    btn.buttonText.alignment = TextAnchor.MiddleLeft;
                    btn.buttonColor = darkOffGrayViolet;
                    btn.height = 60f;
                    buttons.Add(choice, btn);
                }
            );

            buttons.Keys.ToList().ForEach(name => { buttons[name].button.onClick.AddListener(() => { jsc.val = name; }); });

            return buttons;
        }

        public static void UpdateButtonLabels(Dictionary<string, UIDynamicButton> buttons, string selected)
        {
            buttons[selected].label = RadioButtonLabel(selected, true);
            buttons.Where(kvp => kvp.Key != selected)
                .ToList()
                .ForEach(kvp => kvp.Value.label = RadioButtonLabel(kvp.Key, false));
        }

        private static string RadioButtonLabel(string name, bool selected)
        {
            string radio = $"{Size(selected ? "  ●" : "  ○", 36)}";
            return Color(
                $"{radio}  {name}",
                selected ? UnityEngine.Color.white : offGrayViolet
            );
        }

        public static void ApplyToggleStyle(UIDynamicToggle uiToggle)
        {
            bool val = uiToggle.toggle.interactable;
            uiToggle.textColor = val ? UnityEngine.Color.black : gray;
        }

        public static void ApplySliderStyle(UIDynamicSlider uiSlider)
        {
            bool val = uiSlider.slider.interactable;
            uiSlider.labelText.color = val ? UnityEngine.Color.black : gray;
        }
    }
    // ReSharper restore UnusedMember.Global MemberCanBePrivate.Global
}
