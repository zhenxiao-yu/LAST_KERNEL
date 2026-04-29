// With 2021.2 UIToolkit was integrated with Unity instead of being a package.
#if KAMGAM_UI_ELEMENTS || UNITY_2021_2_OR_NEWER

using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEngine;

namespace Kamgam.UIToolkitScriptComponents
{
    /// <summary>
    /// This contains a path to an element in a UI Toolkit visual tree.<br />
    /// You can resolve the path relative to any given visual tree like:
    /// a) in the UIDocument (GetElement())<br />
    /// b) in the UIBuilder Window (GetElementInBuilder())<br />
    /// </summary>
    [System.Serializable]
    public class VisualElementPath
    {
        public List<VisualElementSiblingIdentifier> Elements = new List<VisualElementSiblingIdentifier>();

        public static VisualElementPath Create(VisualElement element)
        {
            var path = new VisualElementPath();
            path.SetElement(element);
            return path;
        }

        public bool IsEmpty()
        {
            return !HasElements();
        }

        public bool HasElements()
        {
            return Elements != null && Elements.Count > 0;
        }

        public void SetElement(VisualElement element)
        {
            Elements = create(element, Elements);
        }

        public VisualElementSiblingIdentifier GetLast()
        {
            if (IsEmpty())
                return null;

            return Elements[Elements.Count - 1];
        }

        /// <summary>
        /// Resolves the path relative to the given UIDocument.
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        public VisualElement Resolve(UIDocument document)
        {
            if (document == null)
                return null;

            return resolve(document);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Resolves the path relative to the document root in the UI Builder window.
        /// </summary>
        /// <returns></returns>
        public VisualElement ResolveInUIBuilderWindow()
        {
            if (!HasElements())
            {
                return null;
            }

            if (   UIBuilderWindowWrapper.Instance == null
                || UIBuilderWindowWrapper.Instance.BuilderWindow == null
                || UIBuilderWindowWrapper.Instance.DocumentRootElement == null)
            {
                return null;
            }

            var root = UIBuilderWindowWrapper.Instance.DocumentRootElement.GetDocumentRoot();
            if (root == null)
            {
                return null;
            }

            return resolve(root);
        }
#endif

        /// <summary>
        /// Creates a path from the root down to this.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        protected List<VisualElementSiblingIdentifier> create(VisualElement element, List<VisualElementSiblingIdentifier> elements = null)
        {
            if (element == null)
                return null;

            var root = element.GetDocumentRoot();

            var ele = element;
            if (elements == null)
            {
                elements = new List<VisualElementSiblingIdentifier>();
            }
            elements.Clear();
            while (ele != root && ele.parent != null)
            {
                var identifier = VisualElementSiblingIdentifier.CreateOrUpdate(ele);
                if (identifier != null)
                {
                    elements.Add(identifier);
                }

                // advance to parent 
                ele = ele.parent;
            }

            elements.Reverse();

            return elements;
        }

        protected VisualElement resolve(UIDocument document)
        {
            if (document == null || document.rootVisualElement == null)
                return null;

            if (IsEmpty())
                return null;

            VisualElement start = document.rootVisualElement;
            return resolve(start);
        }

        protected List<VisualElement> _tmpResolveList;
        protected List<VisualElementSiblingIdentifier> _tmpResolveIdentifierList;

        /// <summary>
        /// Resolve the path relative to the element.<br />
        /// The element itself is not considered part of the path.
        /// </summary>
        /// <param name="start">Start itself must not be part of the path but the parent.</param>
        /// <param name="looseStart">If enabled then the starting point of the path does not have to be start but can be any child element of start.<br /><br />
        /// It will use the first element matching path[0] as the starting point. Enable this is you are not 100% sure start is actually at the start of path.</param>
        /// <returns></returns>
        protected VisualElement resolve(VisualElement start, bool looseStart = true)
        {
            if (IsEmpty())
                return null;

            if (start.panel == null)
            {
                Logger.LogWarning("Start element is detached. Returning null.");
                return null;
            }

            // Check if the final element name is unique, if yes then us that instead of following the path.
            // We do this since that is more robust than path finding and in many cases this is sufficient.
            if (Elements.Count > 0 && !string.IsNullOrEmpty(Elements[Elements.Count - 1].Name))
            {
                var nameMatch = findByUniqueName(start, Elements[Elements.Count - 1].Name);
                if (nameMatch != null)
                {
                    return nameMatch;
                }
            }
            // Do the same for the parent. TODO: investigate, maybe do it for all along the path.
            if (Elements.Count > 1 && !string.IsNullOrEmpty(Elements[Elements.Count - 2].Name))
            {
                var nameMatchParent = findByUniqueName(start, Elements[Elements.Count - 2].Name);
                if (nameMatchParent != null)
                {
                    if (_tmpResolveIdentifierList == null)
                    {
                        _tmpResolveIdentifierList = new List<VisualElementSiblingIdentifier>();
                    }
                    _tmpResolveIdentifierList.Clear();
                    int startIndex = Elements.Count - 2 + 1; // +1 because the element itself must not be part of the new path.
                    for (int i = startIndex; i < Elements.Count; i++)
                    {
                        _tmpResolveIdentifierList.Add(Elements[i]);
                    }

                    return resolveElementPath(nameMatchParent, _tmpResolveIdentifierList, looseStart);
                }
            }

            return resolveElementPath(start, Elements, looseStart);
        }

        private VisualElement resolveElementPath(VisualElement start, List<VisualElementSiblingIdentifier> elementPath, bool looseStart)
        {
            // Loose start (find first matching start)
            if (looseStart && !elementPath[0].Matches(start))
            {
                var current = start;
                while (current != null)
                {
                    if (elementPath[0].Matches(current))
                    {
                        start = current.parent;
                        break;
                    }

                    if (current.childCount > 0)
                    {
                        current = current[0];
                    }
                    else
                    {
                        current = null;
                    }
                }
            }

            VisualElement element = start;
            foreach (var identifier in elementPath)
            {
                if (identifier == null)
                    return null;

                element = identifier.FindInChildren(element);
                if (element == null)
                {
                    return null;
                }
            }

            return element;
        }

        private VisualElement findByUniqueName(VisualElement start, string name)
        {
            if (_tmpResolveList == null)
                _tmpResolveList = new List<VisualElement>();
            _tmpResolveList.Clear();

            start.Query(name).Build().ToList(_tmpResolveList);
            if (_tmpResolveList.Count == 1)
            {
                return _tmpResolveList[0];
            }

            return null;
        }

        /// <summary>
        /// Does this path match the given path?
        /// </summary>
        /// <param name="path"></param>
        /// <returns>TRUE or FALSE</returns>
        public bool Matches(VisualElementPath path)
        {
            if (Elements == null || path == null || path.IsEmpty())
                return false;

            if (Elements.Count != path.Elements.Count)
                return false;

            for (int i = 0; i < Elements.Count; i++)
            {
                var a = Elements[i];
                var b = path.Elements[i];
                if (!a.Matches(b))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns true if the path relies on the index to be unique.<br />
        /// Returns false if the path only relies on hierarchy (parent/child) and names or dataKeys.
        /// </summary>
        /// <returns></returns>
        public bool IsIdentifiedByIndex()
        {
            if (Elements == null || Elements.Count == 0)
                return false;

            for (int i = 0; i < Elements.Count; i++)
            {
                if (string.IsNullOrEmpty(Elements[i].Name) && string.IsNullOrEmpty(Elements[i].DataKey))
                {
                    return true;
                }
            }

            return false;
        }

        protected List<VisualElement> _tmpIsIdentifiedByUniqueNameList;

        public bool IsIdentifiedByUniqueName(VisualElement root)
        {
            if (Elements == null || Elements.Count == 0 || root == null)
                return false;

            var name = GetLast().Name;
            if (string.IsNullOrEmpty(name))
                return false;

            if (_tmpIsIdentifiedByUniqueNameList == null)
            {
                _tmpIsIdentifiedByUniqueNameList = new List<VisualElement>();
            }
            _tmpIsIdentifiedByUniqueNameList.Clear();

            root.Query(name: name).Build().ToList(_tmpIsIdentifiedByUniqueNameList);

            return _tmpIsIdentifiedByUniqueNameList.Count == 1;
        }

        public void Clear()
        {
            Elements.Clear();
        }
    }
}
#endif
