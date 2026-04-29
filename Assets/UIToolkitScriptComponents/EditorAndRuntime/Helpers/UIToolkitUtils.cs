// With 2021.2 UIToolkit was integrated with Unity instead of being a package.
#if KAMGAM_UI_ELEMENTS || UNITY_2021_2_OR_NEWER
using UnityEngine.UIElements;

using UnityEngine;

namespace Kamgam.UIToolkitScriptComponents
{
    public static class UIToolkitUtils
    {
        /* Ain't working, probably because in UIBuilder the root is NOT the root of the document but the root of the builder window itself
        public static UIDocument FindDocumentContainingElement(VisualElement element)
        {
            var documents = GameObject.FindObjectsOfType<UIDocument>(includeInactive: true);

            foreach (var document in documents)
            {
                if (document.rootVisualElement == null)
                    continue;

                if (document.rootVisualElement == element.panel.visualTree)
                {
                    return document;
                }
            }

            return null;
        }
        */
    }
}
#endif