// With 2021.2 UIToolkit was integrated with Unity instead of being a package.
#if KAMGAM_UI_ELEMENTS || UNITY_2021_2_OR_NEWER

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine.UIElements;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Kamgam.UIToolkitScriptComponents
{
    [DisallowMultipleComponent]
    public class UIElementProvider : MonoBehaviour
    {
        public event System.Action OnAttach;
        public event System.Action OnDetach;

        [System.NonSerialized]
        protected bool _linkInitialized = false;
        [System.NonSerialized]
        protected ILink _link;
        public ILink Link
        {
            get
            {
                // Skip unless the gameobject has become null (most likely been deleted)
                if (_linkInitialized && (_link == null || _link.gameObject != null))
                    return _link;

                _linkInitialized = true;

                if (_link == null || _link.gameObject == null)
                {
                    _link = this.GetComponent<ILink>();
                }
                return _link;
            }
        }

        public VisualElement LinkedElement
        {
            get
            {
                if (Link != null && Link.gameObject != null)
                {
                    return Link.GetElement();
                }
                else
                {
                    return null;
                }
            }
        }

        [System.NonSerialized]
        protected bool _queryInitialized = false;
        [System.NonSerialized]
        protected IQuery _query;
        public IQuery Query
        {
            get
            {
                // Skip unless the gameobject has become null (most likely been deleted)
                if (_queryInitialized && (_query == null || _query.gameObject != null))
                    return _query;

                _queryInitialized = true;

                if (_query == null || _query.gameObject == null)
                {
                    _query = this.GetComponent<IQuery>();
                }
                return _query;
            }
        }

        public IList<VisualElement> QueriedElements
        {
            get
            {
                if (Query != null)
                {
                    return Query.GetElements();
                }
                else
                {
                    return null;
                }
            }
        }

        [System.NonSerialized]
        protected List<VisualElement> _elements;
        public List<VisualElement> Elements
        {
            get
            {
                if (_elements == null)
                {
                    if (Link != null && LinkedElement != null)
                    {
                        if(_elements == null)
                        {
                            _elements = new List<VisualElement>();
                        }
                        _elements.Add(LinkedElement);
                    }

                    if (Query != null && QueriedElements != null && QueriedElements.Count > 0)
                    {
                        if (_elements == null)
                        {
                            _elements = new List<VisualElement>();
                        }
                        foreach (var ele in QueriedElements)
                        {
                            if (ele == null)
                                continue;

                            _elements.Add(ele);
                        }
                    }
                }

                return _elements;
            }
        }

        public void ClearElementCache()
        {
            _linkInitialized = false;
            _link = null;

            _queryInitialized = false;
            _query = null;

            _elements = null;
        }

        public virtual void OnEnable()
        {
            onAttach();
        }

        protected void onAttach()
        {
            ClearElementCache();

            if (Query != null)
            {
                Query.Execute();
            }

            if (Link != null)
            {
                Link.RefreshElement();
            }

            registerDetachEvent();
            _ignoreFurtherDetachEvents = false;

            OnAttach?.Invoke();
        }

        protected void registerDetachEvent()
        {
            if (Elements != null && Elements.Count > 0)
            {
                foreach (var ele in Elements)
                {
                    ele.UnregisterCallback<DetachFromPanelEvent>(onDetachedFromPanel);
                    ele.RegisterCallback<DetachFromPanelEvent>(onDetachedFromPanel);
                }
            }
        }

        protected void unregisterDetachEvent()
        {
            if (_elements != null && _elements.Count > 0)
            {
                foreach (var ele in _elements)
                {
                    ele.UnregisterCallback<DetachFromPanelEvent>(onDetachedFromPanel);
                }
            }
        }

        [System.NonSerialized]
        protected Coroutine _reattachCoroutine;
        [System.NonSerialized]
        protected bool _ignoreFurtherDetachEvents;

        protected void onDetachedFromPanel(DetachFromPanelEvent evt)
        {
            if (!_ignoreFurtherDetachEvents && this != null && this.isActiveAndEnabled)
            {
                _ignoreFurtherDetachEvents = true; // make sure onl one detach call is propagated.
                unregisterDetachEvent();

                // Try to reattach afterwards (need for example if the UI is reloaded in the editor).
                if (_reattachCoroutine != null)
                {
                    StopCoroutine(_reattachCoroutine);
                    _reattachCoroutine = null;
                }
                _reattachCoroutine = StartCoroutine(reAttach());

                OnDetach?.Invoke();
            }
        }

        protected IEnumerator reAttach()
        {
            yield return null;

            _ignoreFurtherDetachEvents = false;

            onAttach();
        }
    }
}
#endif
