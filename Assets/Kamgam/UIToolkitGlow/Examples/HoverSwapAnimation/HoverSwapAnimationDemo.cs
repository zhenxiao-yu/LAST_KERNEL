using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitGlow
{
    public class HoverSwapAnimationDemo : MonoBehaviour
    {
        [Header("Toggle Glow")]

        [Tooltip("The class that will be toggled on hover.")]
        public string HoverGlowClassA = "g-radial-color";
        public string HoverGlowClassB = "g-outline-animation";

        [Tooltip("The animation name that will be toggled on hover.")]
        public string AnimationName = BlobAnimationAsset.DefaultName;

        protected UIDocument _document;
        public UIDocument Document
        {
            get
            {
                if (_document == null)
                {
                    _document = this.GetComponent<UIDocument>();
                }
                return _document;
            }
        }

        public void OnEnable()
        {
            if (Document != null && Document.rootVisualElement != null)
            {
                // Register glow on hover
                var hoverElementsQuery = Document.rootVisualElement.Query<Button>();
                hoverElementsQuery.Build().ForEach((b) =>
                {
                    b.RegisterCallback<MouseOverEvent>(onMouseOverAction);
                    b.RegisterCallback<MouseOutEvent>(onMouseOutAction);
                });
            }
        }

        private void onMouseOverAction(MouseOverEvent evt)
        {
            GlowPanel.RemoveClass(evt.target as VisualElement, HoverGlowClassA);
            GlowPanel.AddClass(evt.target as VisualElement, HoverGlowClassB);
        }
        
        private void onMouseOutAction(MouseOutEvent evt)
        {
            GlowPanel.RemoveClass(evt.target as VisualElement, HoverGlowClassB);
            GlowPanel.AddClass(evt.target as VisualElement, HoverGlowClassA);
        }

        public void OnDisable() 
        {
            if (Document != null && Document.rootVisualElement != null)
            {
                var hoverElementsQuery = Document.rootVisualElement.Query<Button>();
                hoverElementsQuery.Build().ForEach((b) =>
                {
                    b.UnregisterCallback<MouseOverEvent>(onMouseOverAction);
                    b.UnregisterCallback<MouseOutEvent>(onMouseOutAction);
                });
            }
        }
    }

}