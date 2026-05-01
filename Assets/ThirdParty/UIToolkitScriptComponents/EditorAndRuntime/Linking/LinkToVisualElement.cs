// With 2021.2 UIToolkit was integrated with Unity instead of being a package.
#if KAMGAM_UI_ELEMENTS || UNITY_2021_2_OR_NEWER

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine.UIElements;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace Kamgam.UIToolkitScriptComponents
{
    /// <summary>
    /// Add this as a CHILD to a UIDocument.
    /// This is the component which contains the link to the visual element.
    /// </summary>
    [AddComponentMenu("UI Toolkit/Scripts/Link to Visual Element")]
    public class LinkToVisualElement : MonoBehaviour, ILink
    {
        public VisualElementPath Path = new VisualElementPath();

        protected UIDocument _document;
        public UIDocument Document
        {
            get
            {
                if (_document == null)
                {
                    _document = this.GetComponentInParent<UIDocument>(includeInactive: true);
                }
                return _document;
            }
        }

        [System.NonSerialized]
        protected VisualElement _elementInDocument;
        public VisualElement Element
        {
            get
            {
                if ((_elementInDocument == null || _elementInDocument.panel == null) && Path != null && Path.HasElements())
                {
                    _elementInDocument = Path.Resolve(Document);
                }

                return _elementInDocument;
            }
            set
            {
                Path.SetElement(value);
                RefreshElement();
            }
        }

        public VisualElement GetElement()
        {
            return _elementInDocument;
        }

        public VisualElement GetRawElement()
        {
            return _elementInDocument;
        }

#if UNITY_EDITOR
        /// <summary>
        /// The visual element in the UIBuilder.<br />
        /// NOTICe: This is a different instance than the element in the UIDocument.
        /// </summary>
        [System.NonSerialized]
        protected VisualElement _elementInBuilder;
        public VisualElement ElementInBuilder
        {
            get
            {
                if ((_elementInBuilder == null || _elementInBuilder.panel == null) && Path != null)
                {
                    _elementInBuilder = Path.ResolveInUIBuilderWindow();
                }

                return _elementInBuilder;
            }
        }

        public VisualElement GetRawElementInBuilder()
        {
            return _elementInBuilder;
        }
#endif

        public void RefreshElementOrPath()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
            {
                // If the build elements is not null but is not found via the current path then try
                // to regenerate the path based on the builder element.
                if (_elementInBuilder != null && _elementInBuilder.panel != null)
                {
                    var path = VisualElementPath.Create(_elementInBuilder);
                    if (UIBuilderWindowWrapper.Instance.BuilderWindow != null && path.HasElements())
                    {
                        var pathElement = path.ResolveInUIBuilderWindow();
                        if (pathElement != _elementInBuilder && pathElement != null)
                        {
                            _elementInBuilder = pathElement;
                            Path.SetElement(_elementInBuilder);
                            return;
                        }
                    }
                }
            }
#endif
            // If the elements is not null but is not found via the current path then try
            // to regenerate the path based on the element.
            if (_elementInDocument != null && _elementInDocument.panel != null)
            {
                var path = VisualElementPath.Create(_elementInDocument);
                if (Document != null && path.HasElements())
                {
                    var pathElement = path.Resolve(Document);
                    if (pathElement != _elementInDocument && pathElement != null)
                    {
                        _elementInDocument = pathElement;
                        Path.SetElement(_elementInDocument);
                        return;
                    }
                }
            }

            // If the the path has not been updated from elements the try the other way and update
            // the elements from the path.
            RefreshElement();
        }

        public void RefreshElement()
        {
            _elementInDocument = Element;

#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
            {
                _elementInBuilder = ElementInBuilder;
            }
#endif

            registerListeners();
        }

        public void RefreshObjectName()
        {
            var name = GenerateGameObjectName(Element);
            if(gameObject.name != name)
            {
                gameObject.name = name;
#if UNITY_EDITOR
                EditorUtility.SetDirty(this.gameObject);
#endif
            }

        }

        public void Unlink()
        {
#if UNITY_EDITOR
            _elementInBuilder = null;
#endif
            _elementInDocument = null;
            Path.Clear();
            gameObject.name = GenerateGameObjectName(null);
        }

        public bool HasPath()
        {
            return Path.HasElements();
        }

        public void RefreshPath()
        {
#if UNITY_EDITOR
            if (_elementInBuilder != null && _elementInBuilder.panel != null)
            {
                Path.SetElement(_elementInBuilder);
            }
            else
#endif
            if (_elementInDocument != null)
            {
                Path.SetElement(_elementInDocument);
            }
        }

        protected void registerListeners()
        {
            if (_elementInDocument != null)
            {
                _elementInDocument.UnregisterCallback<DetachFromPanelEvent>(onDetachFromPanel);
                _elementInDocument.RegisterCallback<DetachFromPanelEvent>(onDetachFromPanel);
            }

#if UNITY_EDITOR
            if (_elementInBuilder != null)
            {
                _elementInBuilder.UnregisterCallback<DetachFromPanelEvent>(onDetachFromPanelInBuilder);
                _elementInBuilder.RegisterCallback<DetachFromPanelEvent>(onDetachFromPanelInBuilder);
            }
#endif
        }

        protected void onDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (_elementInDocument != null)
            {
                _elementInDocument.UnregisterCallback<DetachFromPanelEvent>(onDetachFromPanel);
            }
            _elementInDocument = null;
            _elementInDocument = Element;
        }

#if UNITY_EDITOR
        protected void onDetachFromPanelInBuilder(DetachFromPanelEvent evt)
        {
            if (_elementInBuilder != null)
            {
                _elementInBuilder.UnregisterCallback<DetachFromPanelEvent>(onDetachFromPanelInBuilder);
            }
            _elementInBuilder = null;
            _elementInBuilder = ElementInBuilder;

            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
#endif

        /// <summary>
        /// Does this path match the given path?
        /// </summary>
        /// <param name="path"></param>
        /// <returns>TRUE or FALSE</returns>
        public bool Matches(VisualElementPath path)
        {
            return Path.Matches(path);
        }

        /// <summary>
        /// Returns true if the path relies on the index to be unique.<br />
        /// Returns false if the path only relies on hierarchy (parent/child) and names or dataKeys.
        /// </summary>
        /// <returns></returns>
        public bool IsIdentifiedByIndex()
        {
            return Path.IsIdentifiedByIndex();
        }

        /// <summary>
        /// Returns true if the last element in the path has a unique name.
        /// </summary>
        /// <returns></returns>
        public bool IsIdentifiedByUniqueName()
        {
            if (Document == null)
                return false;

            var root = Document.rootVisualElement.GetDocumentRoot();
            return Path.IsIdentifiedByUniqueName(root);
        }

#if UNITY_EDITOR
        [UnityEditor.Callbacks.DidReloadScripts]
#endif
        public static void UpdateAllVisualElementLinks()
        {
#if UNITY_2023_1_OR_NEWER
            var links = GameObject.FindObjectsByType<LinkToVisualElement>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            var links = GameObject.FindObjectsOfType<LinkToVisualElement>(includeInactive: true);
#endif
            foreach (var link in links)
            {
                link.RefreshElement();
            }
        }

#if UNITY_EDITOR
        public void OnValidate()
        {
            // Refresh only if object is selected.
            if (Selection.activeGameObject == this.gameObject)
            {
                RefreshElementOrPath();
                RefreshObjectName();
            }
        }
#endif


        #region Static API

        public static string GenerateGameObjectName(VisualElement element)
        {
            var identifier = VisualElementSiblingIdentifier.CreateOrUpdate(element);

            string objName = "Script";
            if (element != null)
            {
                objName = identifier.Type + " (" + firstNoneEmpty(identifier.Name, identifier.DataKey, identifier.Index.ToString()) + ")";
            }

            return objName;
        }

        static string firstNoneEmpty(params string[] strings)
        {
            for (int i = 0; i < strings.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(strings[i]))
                    continue;

                return strings[i];
            }

            return "";
        }

        #endregion
    }
}
#endif
