using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TittyMagic.UI
{
    // ReSharper disable MemberCanBePrivate.Global
    public static class UIHelpers
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
            return "\n" + SizeTag("\n", 12);
        }

        public static string ColorTag(string text, Color color)
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";
        }

        public static string SizeTag(string text, int size)
        {
            return $"<size={size}>{text}</size>";
        }

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

        // ReSharper disable once UnusedMember.Global
        public static void ApplyToggleStyle(UIDynamicToggle uiToggle)
        {
            bool val = uiToggle.toggle.interactable;
            uiToggle.textColor = val ? Color.black : gray;
        }

        public static void ApplySliderStyle(UIDynamicSlider uiSlider)
        {
            bool val = uiSlider.slider.interactable;
            uiSlider.labelText.color = val ? Color.black : gray;
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
