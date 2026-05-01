// With 2021.2 UIToolkit was integrated with Unity instead of being a package.
#if KAMGAM_UI_ELEMENTS || UNITY_2021_2_OR_NEWER
using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitScriptComponents
{
    public static class UIToolkitUSSExtensions
    {
        public static void SetDisplayEnabled(this VisualElement element, bool enabled)
        {
            if (element == null)
                return;

            element.style.display = enabled ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public static bool GetDisplayEnabled(this VisualElement element)
        {
            if (element == null)
                return false;

            return element.resolvedStyle.display != DisplayStyle.None;
        }

        public static void SetColor(this VisualElement element, Color color)
        {
            if (element == null)
                return;

            element.style.color = color;
        }

        public static Color GetColor(this VisualElement element)
        {
            if (element == null)
                return default;

            return element.resolvedStyle.color;
        }

        public static void SetBackgroundColor(this VisualElement element, Color color)
        {
            if (element == null)
                return;

            element.style.backgroundColor = color;
        }

        public static Color GetBackgroundColor(this VisualElement element)
        {
            if (element == null)
                return default;

            return element.resolvedStyle.backgroundColor;
        }
    }
}
#endif