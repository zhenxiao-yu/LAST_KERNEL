// With 2021.2 UIToolkit was integrated with Unity instead of being a package.
#if KAMGAM_UI_ELEMENTS || UNITY_2021_2_OR_NEWER
using UnityEngine.UIElements;
#endif

using System.Collections.Generic;
using UnityEngine;

namespace Kamgam.UIToolkitScriptComponents
{
    /// <summary>
    /// Add this as a CHILD to a UIDocument.
    /// This allows you to add event listeners via Unity Events.
    /// </summary>
    [AddComponentMenu("UI Toolkit/Scripts/Query for Visual Element")]
    public class QueryForVisualElements : MonoBehaviour, IQuery
    {
#if KAMGAM_UI_ELEMENTS || UNITY_2021_2_OR_NEWER
        [Header("Query Criteria")]

        [Tooltip("Only elements of this type are used.")]
        public UIElementType Type;

        [Tooltip("If set then the element will be searched by the class name.\nIf an element name is set then both have to match.")]
        public string Class;

        [Tooltip("If set then the element will be searched by the element name.\nIf a class name is set then both have to match.")]
        public string Name;

        [Tooltip("If enabled then all elements matching the criteria are used. If disabled the one the first match is returned.")]
        public bool MultiSelect = false;

        [System.NonSerialized]
        protected bool _executed = false;

        protected UIDocument _document;
        public UIDocument Document
        {
            get
            {
                if (_document == null)
                {
                    _document = this.GetComponentInParent<UIDocument>(includeInactive: true);
                }

#if UNITY_EDITOR
                if (_document == null)
                {
                    Debug.LogWarning("UIElement: No UIDocument found. Please check if you have really added this as a CHILD of a UIDocument!", this.gameObject);
                }
#endif

                return _document;
            }
        }


        [System.NonSerialized]
        protected List<VisualElement> _elements = new List<VisualElement>();
        public List<VisualElement> Elements
        {
            get
            {
                if (!_executed)
                {
                    Execute();
                    _executed = true;
                }

                return _elements;
            }
        }

        public IList<VisualElement> GetElements()
        {
            return Elements;
        }

        /// <summary>
        /// If set then the elements found by Class and/or Name are then filtered by this predicate.
        /// </summary>
        public System.Predicate<VisualElement> Predicate;

        public virtual void OnEnable()
        {
            Execute();
        }

        public virtual void OnDisable()
        {
            _executed = false;
        }

        /// <summary>
        /// Runs the query and stores the result in Elements.
        /// </summary>
        public virtual void Execute()
        {
            if (Document == null || Document.rootVisualElement == null)
            {
                return;
            }

            _elements.Clear();

            if (MultiSelect)
            {
                Document.QueryTypes(
                    Type,
                    name: string.IsNullOrEmpty(Name) ? null : Name,
                    className: string.IsNullOrEmpty(Class) ? null : Class,
                    Predicate,
                    list: _elements);
            }
            else
            {
                var ele = Document.QueryType(
                    Type,
                    name: string.IsNullOrEmpty(Name) ? null : Name,
                    className: string.IsNullOrEmpty(Class) ? null : Class,
                    Predicate);
                if (ele != null)
                {
                    _elements.Add(ele);
                }
            }
        }
#endif
    }
}
