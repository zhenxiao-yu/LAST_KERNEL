// With 2021.2 UIToolkit was integrated with Unity instead of being a package.
#if KAMGAM_UI_ELEMENTS || UNITY_2021_2_OR_NEWER
using UnityEngine.UIElements;

using System.Collections.Generic;

namespace Kamgam.UIToolkitScriptComponents
{
    public static class UIToolkitExtensions
    {
        public static VisualElement QueryType(this UIDocument document, UIElementType type, string name = null, string className = null, System.Predicate<VisualElement> predicate = null)
        {
            var sysType = UIElementTypes.GetElementType(type);
            if(sysType != null)
            {
                return QueryType(document, sysType, name, className, predicate);
            }

            return null;
        }

        public static VisualElement QueryType(this UIDocument document, System.Type type, string name = null, string className = null, System.Predicate<VisualElement> predicate = null)
        {
            if (document == null || document.rootVisualElement == null)
            {
                return null;
            }

            var iter = document.rootVisualElement.Query<VisualElement>(name, className).Build();
            foreach (var ele in iter)
            {
                if (predicate != null && !predicate.Invoke(ele))
                    continue;

                var eleType = ele.GetType();
                if (eleType == type || eleType.IsSubclassOf(type))
                {
                    return ele;
                }
            }

            return null;
        }

        public static List<VisualElement> QueryTypes(this UIDocument document, UIElementType type, string name = null, string className = null, System.Predicate<VisualElement> predicate = null, List<VisualElement> list = null)
        {
            var sysType = UIElementTypes.GetElementType(type);
            if (sysType != null)
            {
                return QueryTypes(document, sysType, name, className, predicate, list);
            }

            return list;
        }

        public static List<VisualElement> QueryTypes(this UIDocument document, System.Type type, string name = null, string className = null, System.Predicate<VisualElement> predicate = null, List<VisualElement> list = null)
        {
            if (list == null)
                list = new List<VisualElement>();

            list.Clear();

            if (document == null || document.rootVisualElement == null)
            {
                return list;
            }

            var iter = document.rootVisualElement.Query<VisualElement>(name, className).Build();
            foreach (var ele in iter)
            {
                if (predicate != null && !predicate.Invoke(ele))
                    continue;

                var eleType = ele.GetType();
                if (eleType == type || eleType.IsSubclassOf(type))
                {
                    list.Add(ele);
                }
            }

            return list;
        }

        /// <summary>
        /// Tries to find the root element just above the normal UXML content elements.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static VisualElement GetDocumentRoot(this VisualElement element)
        {
            if (element == null)
                return null;

            if (element.panel == null)
            {
                // Element is detached. Will return null
                return null;
            }

            if (element.IsPartOfUIDocumentTree())
            {
                return element.panel.visualTree.Q(name: "UIDocument-container"); // Is there always an element with name "UIDocument-container" at the root in UIDocument?
            }
#if UNITY_EDITOR
            else if (element.IsPartOfBuilderTree())
            { 
                // In the UIBuilder the visualTree is NOT the root of the document
                // but the root of the UIBuilder UI itself (the document is just a child).
                // To find the actual document root we have to use a query.
                return element.panel.visualTree.Q(name: "document"); // Is there always an element with name "document" at the root in UIBuilder?
            }
#endif
            else
            {
                return element.panel.visualTree;
            }
        }

        /// <summary>
        /// TODO: investigate on how to make this more robust.
        /// </summary>
        /// <param name="element"></param>
        /// <returns>TRUE if the element is part of the UI Builder tree.</returns>
        public static bool IsPartOfBuilderTree(this VisualElement element)
        {
            // Search upwards
            var ele = element;
            while (ele != null)
            {
                var type = ele.GetType().Name;
                if (type == "BuilderCanvas" || type == "BuilderPane" || type == "BuilderViewport")
                {
                    return true;
                }
                ele = ele.parent;
            }

            // Search downwards
            ele = element;
            while (ele != null)
            {
                var type = ele.GetType().Name;
                if (type == "BuilderCanvas" || type == "BuilderPane" || type == "BuilderViewport")
                {
                    return true;
                }

                if (ele.childCount > 0)
                {
                    ele = ele[0];
                }
                else
                {
                    ele = null;
                }
            }

            return false;
        }

        /// <summary>
        /// TODO: investigate on how to make this more robust.
        /// </summary>
        /// <param name="element"></param>
        /// <returns>TRUE if the element is part of the UI Document tree.</returns>
        public static bool IsPartOfUIDocumentTree(this VisualElement element)
        {
            // Search upwards
            var ele = element;
            while (ele != null)
            {
                var type = ele.GetType().Name;
                var name = ele.name;
                if (type == "TemplateContainer" && (name == "UIDocument-container" || type == "UIDocument"))
                {
                    return true;
                }
                ele = ele.parent;
            }

            // Search downwards
            ele = element;
            while (ele != null)
            {
                var type = ele.GetType().Name;
                var name = ele.name;
                if (type == "TemplateContainer" && (name == "UIDocument-container" || type == "UIDocument"))
                {
                    return true;
                }

                if (ele.childCount > 0)
                {
                    ele = ele[0];
                }
                else
                {
                    ele = null;
                }
            }

            return false;
        }
    }
}
#endif