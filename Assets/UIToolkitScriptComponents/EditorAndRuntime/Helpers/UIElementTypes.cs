// With 2021.2 UIToolkit was integrated with Unity instead of being a package.
#if KAMGAM_UI_ELEMENTS || UNITY_2021_2_OR_NEWER
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitScriptComponents
{
    public enum UIElementType
    {
        VisualElement,
        BindableElement,
        Button,
        Label,
        Scroller,
        TextField,
        Foldout,
        Slider,
        SliderInt,
        ProgressBar,
        DropdownField,
        TextElement
    }

    public static class UIElementTypes
    {
        public static System.Type GetElementType(UIElementType type)
        {
            switch (type)
            {
                case UIElementType.VisualElement:
                    return typeof(VisualElement);

                case UIElementType.BindableElement:
                    return typeof(BindableElement);

                case UIElementType.Button:
                    return typeof(Button);

                case UIElementType.Label:
                    return typeof(Label);

                case UIElementType.Scroller:
                    return typeof(Scroller);

                case UIElementType.TextField:
                    return typeof(TextField);

                case UIElementType.Foldout:
                    return typeof(Foldout);

                case UIElementType.Slider:
                    return typeof(Slider);

                case UIElementType.SliderInt:
                    return typeof(SliderInt);

                case UIElementType.ProgressBar:
                    return typeof(ProgressBar);

                case UIElementType.DropdownField:
                    return typeof(DropdownField);

                case UIElementType.TextElement:
                    return typeof(TextElement);

                default:
                    return null;
            }
        }

        public static UIElementType GetElementType(this VisualElement element)
        {
            if (element is Button)
                return UIElementType.Button;

            if (element is Label)
                return UIElementType.Label;

            if (element is Scroller)
                return UIElementType.Scroller;

            if (element is TextField)
                return UIElementType.TextField;

            if (element is Foldout)
                return UIElementType.Foldout;

            if (element is Slider)
                return UIElementType.Slider;

            if (element is SliderInt)
                return UIElementType.SliderInt;

            if (element is ProgressBar)
                return UIElementType.ProgressBar;

            if (element is DropdownField)
                return UIElementType.DropdownField;


            // Base Types

            if (element is TextElement)
                return UIElementType.TextElement;


            return UIElementType.VisualElement;
        }
    }
}
#endif