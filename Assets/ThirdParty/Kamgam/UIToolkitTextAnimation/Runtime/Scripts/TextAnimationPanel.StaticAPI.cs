using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitTextAnimation
{
    public partial class TextAnimationPanel
    {
        public static List<TextAnimationPanel> Panels = new List<TextAnimationPanel>();

        public static TextAnimationPanel GetPanel(VisualElement element)
        {
            var docs = TextAnimationDocument.GetDocuments();
            foreach (var doc in docs)
            { 
                if (doc.Document == null || doc.Document.gameObject == null || doc.Document.rootVisualElement == null)
                    continue;

                // If the element is null then simply return the first found document.
                if (element == null)
                    return doc.Panel;

                if (doc.Document.rootVisualElement.Contains(element))
                {
                    return doc.Panel;
                }
            }

            // Second try:
            // Try with parent of element (useful for dynamically added elements that may not yet be part of the panel).
            element = element.parent;
            foreach (var doc in docs)
            {
                if (doc.Document == null || doc.Document.gameObject == null || doc.Document.rootVisualElement == null)
                    continue;

                // If the element is null then simply return the first found document.
                if (element == null)
                    return doc.Panel;

                if (doc.Document.rootVisualElement.Contains(element))
                {
                    return doc.Panel;
                }
            }

            return null;
        }
    }
}