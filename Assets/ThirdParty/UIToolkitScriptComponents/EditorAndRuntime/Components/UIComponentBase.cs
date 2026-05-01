// With 2021.2 UIToolkit was integrated with Unity instead of being a package.
#if KAMGAM_UI_ELEMENTS || UNITY_2021_2_OR_NEWER

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine.UIElements;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Kamgam.UIToolkitScriptComponents
{
    [RequireComponent(typeof(UIElementProvider))]
    public abstract class UIComponentBase<T> : MonoBehaviour where T : VisualElement
    {
        [System.NonSerialized]
        protected UIElementProvider _provider;

        public UIElementProvider Provider
        {
            get
            {
                if (_provider == null)
                {
                    _provider = this.GetComponent<UIElementProvider>();
                    _provider.OnAttach += OnAttach;
                    _provider.OnDetach += OnDetach;
                }
                return _provider;
            }
        }

        public virtual void OnEnable()
        {
            OnAttach();
        }

        public virtual void OnDestroy()
        {
            if (_provider != null)
            {
                _provider.OnAttach -= OnAttach;
                _provider.OnDetach -= OnDetach;
            }
        }

        /// <summary>
        /// All elements found via Links and Queries (may contain duplicates if Links and Queries overlap).
        /// </summary>
        [System.NonSerialized]
        protected List<T> _elements;
        public List<T> Elements
        {
            get
            {
                if (_elements == null)
                {
                    if (Provider != null && Provider.Elements != null)
                    {
                        _elements = Provider.Elements.Select(e => e as T).Where(e => e != null).ToList();
                    }
                }

                return _elements;
            }
        }

        /// <summary>
        /// The first element found either in Links or Queries.
        /// </summary>
        public T Element
        {
            get
            {
                if (HasElements())
                {
                    return Elements[0];
                }
                return null;
            }
        }

        public bool HasElements()
        {
            return Elements != null && Elements.Count > 0;
        }

        protected void executeOnEveryElement(System.Action<T> func)
        {
            if (HasElements())
            {
                foreach (var ele in Elements)
                {
                    if (ele == null)
                        continue;

                    func.Invoke(ele);
                }
            }
        }

        public virtual void OnAttach() { }
        public virtual void OnDetach() { }

    }
}
#endif
