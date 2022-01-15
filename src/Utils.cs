using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
            return $"{nameof(TittyMagic)} v{Script.version}: {message}{(string.IsNullOrEmpty(name) ? "" : $" [{name}]")}";
        }

        public static MVRScript FindPluginOnAtom(Atom atom, string search)
        {
            var match = atom.GetStorableIDs().FirstOrDefault(s => s.Contains(search));
            return match == null ? null : atom.GetStorableByID(match) as MVRScript;
        }

        //MacGruber / Discord 20.10.2020
        //Get path prefix of the package that contains this plugin
        public static string GetPackagePath(MVRScript script)
        {
            string id = script.name.Substring(0, script.name.IndexOf('_'));
            string filename = script.manager.GetJSON()["plugins"][id].Value;
            int idx = filename.IndexOf(":/");
            return idx >= 0 ? filename.Substring(0, idx + 2) : "";
        }

        public static string NameValueString(
            string name,
            float value,
            float roundFactor = 1000f,
            int padRight = 0
        )
        {
            float rounded = Calc.RoundToDecimals(value, roundFactor);
            string printName = StripPrefixes(name).PadRight(padRight, ' ');
            return string.Format("{0} {1}", printName, $"{rounded}");
        }

        private static string StripPrefixes(string text)
        {
            string result = StripPrefix(text, "TM_");
            result = StripPrefix(result, "UPR_");
            result = StripPrefix(result, "UPSD_");
            result = StripPrefix(result, "LBACK_");
            result = StripPrefix(result, "LFWD_");
            result = StripPrefix(result, "RLEFT_");
            result = StripPrefix(result, "RRIGHT_");
            return result;
        }

        private static string StripPrefix(string text, string prefix)
        {
            return text.StartsWith(prefix) ? text.Substring(prefix.Length) : text;
        }
    }

    internal static class Calc
    {
        // value between -1 and +1
        // +1 = leaning 90 degrees left
        // -1 = leaning 90 degrees right
        public static float Roll(Quaternion q)
        {
            return 2 * InverseSmoothStepToPi(Mathf.Asin(2 * q.x * q.y + 2 * q.z * q.w));
        }

        // value between -2 and 2
        // +2 = upright
        // +1 = horizontal, on stomach
        // -1 = horizontal, on back
        // -2 = upside down
        public static float Pitch(Quaternion q)
        {
            return 2 * InverseSmoothStepToPi(Mathf.Atan2(2 * q.x * q.w - 2 * q.y * q.z, 1 - 2 * q.x * q.x - 2 * q.z * q.z));
        }

        // value returned is smoothed (for better animation) i.e. no longer maps linearly to the actual rotation angle
        private static float InverseSmoothStepToPi(float val)
        {
            if(val > 0)
            {
                return InverseSmoothStep(Mathf.PI, val, -0.1f, Mathf.PI/2);
            }

            return -InverseSmoothStep(Mathf.PI, -val, -0.1f, Mathf.PI/2);
        }

        public static float RoundToDecimals(float value, float roundFactor)
        {
            return Mathf.Round(value * roundFactor) / roundFactor;
        }

        public static Vector3 RelativePosition(Transform origin, Vector3 position)
        {
            Vector3 distance = position - origin.position;
            return new Vector3(
                Vector3.Dot(distance, origin.right.normalized),
                Vector3.Dot(distance, origin.up.normalized),
                Vector3.Dot(distance, origin.forward.normalized)
            );
        }

        public static bool EqualWithin(float roundFactor, float v1, float v2)
        {
            return Mathf.Round(v1 * roundFactor) / roundFactor == Mathf.Round(v2 * roundFactor) / roundFactor;
        }

        public static bool VectorEqualWithin(float roundFactor, Vector3 v1, Vector3 v2)
        {
            return Mathf.Round(v1.x * roundFactor) / roundFactor == Mathf.Round(v2.x * roundFactor) / roundFactor
                && Mathf.Round(v1.y * roundFactor) / roundFactor == Mathf.Round(v2.y * roundFactor) / roundFactor
                && Mathf.Round(v1.z * roundFactor) / roundFactor == Mathf.Round(v2.z * roundFactor) / roundFactor;
        }

        public static float ScaledSmoothMax(float value, float logMaxX)
        {
            if(logMaxX < 0)
            {
                return -Mathf.Log(value * Mathf.Abs(logMaxX) + 1);
            }

            return Mathf.Log(value * logMaxX + 1);
        }

        // https://www.desmos.com/calculator/crrr1uryep
        public static float InverseSmoothStep(float b, float value, float curvature, float midpoint)
        {
            if(value < 0)
                return 0;
            if(value > b)
                return 1;

            float s = curvature < -2.99f ? -2.99f : (curvature > 0.99f ? 0.99f : curvature);
            float p = midpoint * b;
            p = p < 0 ? 0 : (p > b ? b : p);
            float c = 2/(1 - s) - p/b;

            if(value < p)
                return f1(value, b, p, c);

            return 1 - f1(b - value, b, b - p, c);
        }

        private static float f1(float value, float b, float n, float c)
        {
            return Mathf.Pow(value, c)/(b * Mathf.Pow(n, c - 1));
        }
    }

    internal static class UI
    {
        public static Color black = UnityEngine.Color.black;
        public static Color darkOffGrayViolet = new Color(0.26f, 0.20f, 0.26f);
        public static Color offGrayViolet = new Color(0.80f, 0.75f, 0.80f);
        public static Color white = UnityEngine.Color.white;

        public static string LineBreak()
        {
            return "\n" + Size("\n", 12);
        }

        public static string Color(string text, Color color)
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";
        }

        public static string Bold(string text)
        {
            return $"<b>{text}</b>";
        }

        public static string Italic(string text)
        {
            return $"<i>{text}</i>";
        }

        public static string Size(string text, int size)
        {
            return $"<size={size}>{text}</size>";
        }

        public static JSONStorableFloat NewFloatSlider(
            MVRScript script,
            string paramName,
            float startingValue,
            float minValue,
            float maxValue,
            string valueFormat,
            bool rightSide = false
        )
        {
            JSONStorableFloat storable = new JSONStorableFloat(paramName, startingValue, minValue, maxValue);
            storable.storeType = JSONStorableParam.StoreType.Physical;
            script.RegisterFloat(storable);
            UIDynamicSlider slider = script.CreateSlider(storable, rightSide);
            slider.valueFormat = valueFormat;
            return storable;
        }

        public static JSONStorableString NewTextField(
            MVRScript script,
            string paramName,
            string initialValue,
            int fontSize,
            int height = 120,
            bool rightSide = false
        )
        {
            JSONStorableString storable = new JSONStorableString(paramName, initialValue);
            UIDynamicTextField textField = script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = fontSize;
            textField.height = height;
            return storable;
        }

        public static JSONStorableBool NewToggle(MVRScript script, string paramName, bool startingValue, bool rightSide = false)
        {
            JSONStorableBool storable = new JSONStorableBool(paramName, startingValue);
            script.CreateToggle(storable, rightSide);
            script.RegisterBool(storable);
            return storable;
        }

        public static void NewSpacer(MVRScript script, float height, bool rightSide = false)
        {
            UIDynamic spacer = script.CreateSpacer(rightSide);
            spacer.height = height;
        }

        public static Dictionary<string, UIDynamicButton> CreateRadioButtonGroup(MVRScript script, JSONStorableStringChooser jsc, bool rightSide = false)
        {
            Dictionary<string, UIDynamicButton> buttons = new Dictionary<string, UIDynamicButton>();
            jsc.choices.ForEach((choice) =>
            {
                UIDynamicButton btn = script.CreateButton(RadioButtonLabel(choice, choice == jsc.defaultVal), rightSide);
                btn.buttonText.alignment = TextAnchor.MiddleLeft;
                btn.buttonColor = darkOffGrayViolet;
                btn.height = 60f;
                buttons.Add(choice, btn);
            });

            buttons.Keys.ToList().ForEach(name =>
            {
                buttons[name].button.onClick.AddListener(() =>
                {
                    jsc.val = name;
                });
            });

            return buttons;
        }

        public static void UpdateButtonLabels(Dictionary<string, UIDynamicButton> buttons, string selected)
        {
            buttons[selected].label = RadioButtonLabel(selected, true);
            buttons.Where(kvp => kvp.Key != selected).ToList()
                .ForEach(kvp => kvp.Value.label = RadioButtonLabel(kvp.Key, false));
        }

        private static string RadioButtonLabel(string name, bool selected)
        {
            string radio = $"{Size(selected ? "  ●" : "  ○", 36)}";
            return Color($"{radio}  {name}", selected ? white : offGrayViolet);
        }
    }
}
