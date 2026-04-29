using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitCustomShaderImageURP
{
    public static class CustomShaderImageStyles
    {
        public static CustomStyleProperty<float> MeshCornerOverlap = new CustomStyleProperty<float>("--mesh-corner-overlap");
        public static CustomStyleProperty<int> MeshCornerSegments = new CustomStyleProperty<int>("--mesh-corner-segments");

        public static float ResolveStyle(CustomStyleProperty<float> property, VisualElement element, float defaultValue)
        {
            if (element.customStyle.TryGetValue(property, out var value))
            {
                return value;
            }

            return defaultValue;
        }

        public static int ResolveStyle(CustomStyleProperty<int> property, VisualElement element, int defaultValue)
        {
            if (element.customStyle.TryGetValue(property, out var value))
            {
                return value;
            }

            return defaultValue;
        }

        public static bool ResolveStyle(CustomStyleProperty<bool> property, VisualElement element, bool defaultValue)
        {
            if (element.customStyle.TryGetValue(property, out var value))
            {
                return value;
            }

            return defaultValue;
        }

        public static Color ResolveStyle(CustomStyleProperty<Color> property, VisualElement element, Color defaultValue)
        {
            if (element.customStyle.TryGetValue(property, out var value))
            {
                return value;
            }

            return defaultValue;
        }

        public static string ResolveStyle(CustomStyleProperty<string> property, VisualElement element, string defaultValue)
        {
            if (element.customStyle.TryGetValue(property, out var value))
            {
                return value;
            }

            return defaultValue;
        }
    }
}