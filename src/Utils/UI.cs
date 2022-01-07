using UnityEngine;

namespace TittyMagic
{
    public static class UI
    {
        public static Color black = UnityEngine.Color.black;
        public static Color darkOffGrayViolet = new Color(0.26f, 0.20f, 0.26f);
        public static Color offGrayViolet = new Color(0.80f, 0.75f, 0.80f);
        public static Color white = UnityEngine.Color.white;

        public static string RadioButtonLabel(string name, bool selected)
        {
            string radio = $"{Size(selected ? "  ●" : "  ○", 36)}";
            return Color($"{radio}  {name}", selected ? white : offGrayViolet);
        }

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
            int fontSize,
            int height = 120,
            bool rightSide = false
        )
        {
            JSONStorableString storable = new JSONStorableString(paramName, paramName);
            UIDynamicTextField textField = script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = fontSize;
            textField.height = height;
            return storable;
        }

        public static void NewSpacer(MVRScript script, float height, bool rightSide = false)
        {
            UIDynamic spacer = script.CreateSpacer(rightSide);
            spacer.height = height;
        }
    }
}
