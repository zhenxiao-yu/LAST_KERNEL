using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace Kamgam.UIToolkitGlow
{
    public static class ShadowExtensions
    {
        /// <summary>
        /// Wraps an existing VisualElement with a new parent VisualElement.
        /// </summary>
        public static VisualElement WrapInShadow(this VisualElement elementToWrap, string newParentName = null)
        {
            if (elementToWrap == null || elementToWrap.parent == null)
            {
                Debug.LogError("Element to wrap is null or has no parent.");
                return null;
            }

            // Create the new parent element
            VisualElement newParent = new Shadow();
            if (!string.IsNullOrEmpty(newParentName))
                newParent.name = newParentName;

            // Copy position and layout styles from the original element
            newParent.style.position = elementToWrap.getComputedStyle<Position>("position");
            newParent.style.left = elementToWrap.getComputedStyle<Length>("left");
            newParent.style.top = elementToWrap.getComputedStyle<Length>("top");
            newParent.style.right = elementToWrap.getComputedStyle<Length>("right");
            newParent.style.bottom = elementToWrap.getComputedStyle<Length>("bottom");
            newParent.style.width = elementToWrap.getComputedStyle<Length>("width");
            newParent.style.height = elementToWrap.getComputedStyle<Length>("height");
            newParent.style.marginLeft = elementToWrap.getComputedStyle<Length>("marginLeft");
            newParent.style.marginTop = elementToWrap.getComputedStyle<Length>("marginTop");
            newParent.style.marginRight = elementToWrap.getComputedStyle<Length>("marginRight");
            newParent.style.marginBottom = elementToWrap.getComputedStyle<Length>("marginBottom");
            newParent.style.alignSelf = elementToWrap.getComputedStyle<Align>("alignSelf");
            newParent.style.flexGrow = elementToWrap.getComputedStyle<float>("flexGrow");
            newParent.style.flexShrink = elementToWrap.getComputedStyle<float>("flexShrink");
            newParent.style.borderTopLeftRadius = elementToWrap.getComputedStyle<Length>("borderTopLeftRadius");
            newParent.style.borderTopRightRadius = elementToWrap.getComputedStyle<Length>("borderTopRightRadius");
            newParent.style.borderBottomRightRadius = elementToWrap.getComputedStyle<Length>("borderBottomRightRadius");
            newParent.style.borderBottomLeftRadius = elementToWrap.getComputedStyle<Length>("borderBottomLeftRadius");

            // Insert the new parent into the hierarchy
            elementToWrap.parent.Insert(elementToWrap.parent.IndexOf(elementToWrap), newParent);

            // Reparent the original element to the new parent
            newParent.Add(elementToWrap);

            // Make the original element fill the new parent
            elementToWrap.style.position = Position.Relative;
            elementToWrap.style.left = 0;
            elementToWrap.style.top = 0;
            elementToWrap.style.right = 0;
            elementToWrap.style.bottom = 0;
            elementToWrap.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            elementToWrap.style.height = StyleKeyword.Auto; //new StyleLength(new Length(100, LengthUnit.Percent));
            elementToWrap.style.marginLeft = 0;
            elementToWrap.style.marginTop = 0;
            elementToWrap.style.marginRight = 0;
            elementToWrap.style.marginBottom = 0;

            return newParent;
        }

        /// <summary>
        /// Unwraps a VisualElement, removing its parent wrapper and restoring its original layout.
        /// </summary>
        public static void UnwrapFromShadow(this VisualElement wrapper)
        {
            // Allow the child itself to the parameter too (find the parent shadow).
            if (wrapper != null && wrapper is not Shadow)
                wrapper = wrapper.parent;
                
            if (wrapper == null || wrapper.childCount != 1 || wrapper is not Shadow)
                return;

            VisualElement child = wrapper[0];
            VisualElement grandParent = wrapper.parent;

            if (grandParent == null)
            {
                Debug.LogError("Wrapper has no parent.");
                return;
            }

            // Store the wrapper's layout styles (originally copied from the child)
            var position = wrapper.style.position;
            var left = wrapper.style.left;
            var top = wrapper.style.top;
            var right = wrapper.style.right;
            var bottom = wrapper.style.bottom;
            var width = wrapper.style.width;
            var height = wrapper.style.height;
            var marginLeft = wrapper.style.marginLeft;
            var marginTop = wrapper.style.marginTop;
            var marginRight = wrapper.style.marginRight;
            var marginBottom = wrapper.style.marginBottom;
            var alignSelf = wrapper.style.alignSelf;
            var flexGrow = wrapper.style.flexGrow;
            var flexShrink = wrapper.style.flexShrink;

            // Reparent the child to the grandparent
            int wrapperIndex = grandParent.IndexOf(wrapper);
            grandParent.Insert(wrapperIndex, child);
            grandParent.Remove(wrapper);

            // Restore the child's original layout styles
            child.style.position = position;
            child.style.left = left;
            child.style.top = top;
            child.style.right = right;
            child.style.bottom = bottom;
            child.style.width = width;
            child.style.height = height;
            child.style.marginLeft = marginLeft;
            child.style.marginTop = marginTop;
            child.style.marginRight = marginRight;
            child.style.marginBottom = marginBottom;
            child.style.alignSelf = alignSelf;
            child.style.flexGrow = flexGrow;
            child.style.flexShrink = flexShrink;
        }
    }
}