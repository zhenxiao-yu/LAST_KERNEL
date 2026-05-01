using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitScrollViewPro
{
    public static class UIToolkitExtensions
    {
        public static bool IsChildOf(this VisualElement element, VisualElement parent)
        {
            return parent.Contains(element);
        }
        
        public static bool IsChildOfType(this IEventHandler handler, string typeName, bool includeSelf = true)
        {
            if (handler == null)
                return false;
            
            var element = handler as VisualElement;
            return IsChildOfType(element, typeName, includeSelf);
        }
        
        public static bool IsChildOfType(this VisualElement element, string typeName, bool includeSelf = true)
        {
            if (element == null)
                return false;

            if (includeSelf && element.GetType().Name == typeName)
            {
                return true;
            }

            var parent = element.parent;
            while (parent != null)
            {
                if (parent.GetType().Name == typeName)
                    return true;

                parent = parent.parent;
            }

            return false;
        }

        public static bool IsChildOfClass(this VisualElement element, string className, bool includeSelf = true)
        {
            if (element == null)
                return false;

            if (includeSelf && element.ClassListContains(className))
            {
                return true;
            }

            var parent = element.parent;
            while (parent != null)
            {
                if (parent.ClassListContains(className))
                    return true;

                parent = parent.parent;
            }

            return false;
        }

        /// <summary>
        /// Returns true only if the element is a child (or self) of classNamePositive with classNameNegative negating the positive class.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="classNamePositive"></param>
        /// <param name="classNameNegative"></param>
        /// <param name="includeSelf"></param>
        /// <param name="preferNegative"></param>
        /// <returns></returns>
        public static bool IsChildOfClass(this VisualElement element, string classNamePositive, string classNameNegative, bool includeSelf = true, bool preferNegative = true)
        {
            if (element == null)
                return false;

            if (includeSelf)
            {
                bool containsNegative = element.ClassListContains(classNameNegative);
                if (containsNegative && preferNegative)
                    return false;

                if (element.ClassListContains(classNamePositive))
                    return true;

                if (containsNegative)
                    return false;
            }

            var parent = element.parent;
            while (parent != null)
            {
                // Stop at the first found positive or negative class.
                bool containsNegative = parent.ClassListContains(classNameNegative);
                if (containsNegative && preferNegative)
                    return false;

                if (parent.ClassListContains(classNamePositive))
                    return true;

                if (containsNegative)
                    return false;

                parent = parent.parent;
            }

            return false;
        }
        
        public static bool IsInsideOrOverlapping(this VisualElement element, VisualElement other, float margin = 0f)
        {
            Rect elementRect = element.worldBound;
            Rect otherRect = other.worldBound;

            elementRect = new Rect(
                elementRect.x - margin,
                elementRect.y - margin,
                elementRect.width + 2 * margin,
                elementRect.height + 2 * margin);

            otherRect = new Rect(
                otherRect.x,
                otherRect.y,
                otherRect.width,
                otherRect.height);

            return elementRect.Overlaps(otherRect) ||
                   otherRect.Contains(elementRect.min) ||
                   otherRect.Contains(elementRect.max);
        }
    }
}
