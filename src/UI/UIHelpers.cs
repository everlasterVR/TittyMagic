using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TittyMagic.UI
{
    // ReSharper disable MemberCanBePrivate.Global
    public static class UIHelpers
    {
        public static Color darkOffGrayViolet = new Color(0.26f, 0.20f, 0.26f);
        public static Color darkerGray = new Color(0.4f, 0.4f, 0.4f);
        public static Color lightPink = new Color(1f, 0.925f, 0.925f);
        public static Color offGrayRed = new Color(0.45f, 0.4f, 0.4f);
        public static Color offGrayViolet = new Color(0.80f, 0.75f, 0.80f);
        public static Color sliderGray = new Color(0, 0, 0, 0.498f);
        public static Color funkyCyan = new Color(0.596f, 1.000f, 0.780f);
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
            // var groupTransform = horizontalLayoutGroup.transform;
            // groupTransform.SetParent(leftUIContent, false);
            // leftUIElements.Add(groupTransform);

            return horizontalLayoutGroup;
        }
    }
    // ReSharper restore MemberCanBePrivate.Global
}
