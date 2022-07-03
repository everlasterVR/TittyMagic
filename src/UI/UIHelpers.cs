// ReSharper disable MemberCanBePrivate.Global
using UnityEngine;
using UnityEngine.UI;

namespace TittyMagic.UI
{
    public static class UIHelpers
    {
        public static Color darkOffGrayViolet = new Color(0.26f, 0.20f, 0.26f);
        public static Color darkerGray = new Color(0.4f, 0.4f, 0.4f);
        public static Color lightPink = new Color(1f, 0.925f, 0.925f);
        public static Color offGrayRed = new Color(0.45f, 0.4f, 0.4f);
        public static Color offGrayViolet = new Color(0.80f, 0.75f, 0.80f);
        public static Color sliderGray = new Color(0, 0, 0, 0.498f);
        public static Color funkyCyan = new Color(0.596f, 1.000f, 0.780f);
        public static Color titleGreen = new Color(0.285f, 0.690f, 0.489f);
        public static Color paleCyan = new Color32(195, 231, 212, 255);
        public static Color evenPalerCyan = new Color32(206, 228, 216, 255);

        public static Color defaultBtnNormalColor = new Color(0.8392157f, 0.8392157f, 0.8392157f);
        public static Color defaultBtnHighlightedColor = new Color(0.7f, 0.7f, 0.7f);
        public static Color defaultBtnPressedColor = Color.gray;
        public static Color defaultBtnDisabledColor = Color.gray;

        // ReSharper disable once UnusedMember.Global
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

        public static HorizontalLayoutGroup CreateHorizontalLayoutGroup(RectTransform uiContent)
        {
            var verticalLayoutGroup = uiContent.GetComponent<VerticalLayoutGroup>();

            var gameObj = new GameObject();
            gameObj.transform.SetParent(verticalLayoutGroup.transform, false);

            var horizontalLayoutGroup = gameObj.AddComponent<HorizontalLayoutGroup>();
            horizontalLayoutGroup.spacing = 16f;
            horizontalLayoutGroup.childForceExpandWidth = true;
            horizontalLayoutGroup.childControlHeight = false;

            return horizontalLayoutGroup;
        }

        public static UIDynamicTextField HeaderTextField(MVRScript script, JSONStorableString storable, string text, bool rightSide)
        {
            storable.val = "\n".Size(20) + text.Bold();
            var textField = script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 30;
            textField.UItext.alignment = TextAnchor.LowerCenter;
            textField.backgroundColor = Color.clear;

            var layout = textField.GetComponent<LayoutElement>();
            layout.preferredHeight = 62;
            layout.minHeight = 62;

            return textField;
        }

        public static UIDynamicTextField TitleTextField(MVRScript script, JSONStorableString storable, string displayName, int height, bool rightSide)
        {
            storable.val = "\n".Size(12) + displayName.Bold();
            var textField = script.CreateTextField(storable, rightSide);
            textField.UItext.alignment = TextAnchor.MiddleCenter;
            textField.backgroundColor = Color.clear;

            var layout = textField.GetComponent<LayoutElement>();
            layout.preferredHeight = height;
            layout.minHeight = height;

            return textField;
        }

        public static UIDynamicTextField NotificationTextField(MVRScript script, JSONStorableString storable, int height, bool rightSide)
        {
            var textField = script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 26;
            textField.UItext.color = new Color(0.15f, 0.15f, 0.15f);
            textField.UItext.alignment = TextAnchor.MiddleRight;
            textField.backgroundColor = Color.clear;

            var layout = textField.GetComponent<LayoutElement>();
            layout.preferredHeight = height;
            layout.minHeight = height;

            return textField;
        }

        public static Color MultiplierSliderColor(float value) =>
            value <= 1
                ? Color.Lerp(new Color(1, 1, 1, 0.25f), Color.white, value)
                : Color.Lerp(Color.white, new Color(1.0f, 0.2f, 0.2f), (value - 1) / 3);
    }
}
