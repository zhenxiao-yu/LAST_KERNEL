using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitScrollViewPro
{
    /// <summary>
    /// I needed to fetch the style of a visual element as it was set in the UI Builder.
    /// Sadly neither .style nor .resolvedStyle do that. I ended up reaching into the VisualElement via reflections.
    /// It may cause some boxing/unboxing so use with caution. If not used every frame it should do the job nicely (tested from Unity 2021.3.5f1 up to 6.0.27f1)
    /// 
    /// The source for the “m_Style” can be found here:
    /// https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/UIElements/Core/VisualElement.cs
    /// </summary>
    public static class StyleExtensions
    {
        private static readonly BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
        
        private static FieldInfo s_computedStyleField;
        private static readonly Dictionary<string, PropertyInfo> s_computedStylePropertyCache = new();
        
        private static object getComputedStyle(this VisualElement element)
        {
            if (s_computedStyleField == null)
            {
                s_computedStyleField = typeof(VisualElement).GetField("m_Style", Flags);
            }

            if (s_computedStyleField == null)
            {
                throw new Exception("Cannot find 'm_Style' field in VisualElement.");
            }

            return s_computedStyleField.GetValue(element);
        }
        
        public static T getComputedStyle<T>(this VisualElement element, string propertyName)
        {
            var computedStyle = element.getComputedStyle();
            var type = computedStyle.GetType();

            if (!s_computedStylePropertyCache.TryGetValue(propertyName, out var propertyInfo))
            {
                propertyInfo = type.GetProperty(propertyName, Flags);
                
                if (propertyInfo == null || propertyInfo.PropertyType != typeof(T))
                    throw new Exception($"Property '{propertyName}' not found or not of type {typeof(T).Name}");

                s_computedStylePropertyCache[propertyName] = propertyInfo;
            }

            return (T)propertyInfo.GetValue(computedStyle);
        }
        
        public static T getComputedStyle<T>(this VisualElement element, string propertyName, T failValue)
        {
            var computedStyle = element.getComputedStyle();
            var type = computedStyle.GetType();

            if (!s_computedStylePropertyCache.TryGetValue(propertyName, out var propertyInfo))
            {
                propertyInfo = type.GetProperty(propertyName, Flags);

                if (propertyInfo == null || propertyInfo.PropertyType != typeof(T))
                {
                    return failValue;
                }

                s_computedStylePropertyCache[propertyName] = propertyInfo;
            }

            var value = propertyInfo.GetValue(computedStyle);
            return (T) value;
        }
    }
}