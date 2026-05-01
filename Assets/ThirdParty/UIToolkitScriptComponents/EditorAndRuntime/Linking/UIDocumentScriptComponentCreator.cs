// With 2021.2 UIToolkit was integrated with Unity instead of being a package.
#if KAMGAM_UI_ELEMENTS || UNITY_2021_2_OR_NEWER

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace Kamgam.UIToolkitScriptComponents
{
    public class UIDocumentScriptComponentCreator : MonoBehaviour
    {
        protected UIDocument _document;
        public UIDocument Document
        {
            get
            {
                if (_document == null)
                {
                    _document = GetComponentInParent<UIDocument>();
                }

                return _document;
            }
        }

        public static UIDocumentScriptComponentCreator GetOrCreate(GameObject gameObjectWithUIDocument, bool selectInEditor = false)
        {
            var document = gameObjectWithUIDocument.GetComponent<UIDocument>();
            if (document == null)
                return null;

            string name = "Scripts";
            var root = gameObjectWithUIDocument.transform.Find(name);
            if (root == null)
            {
                var go = new GameObject(name);
                go.transform.SetParent(document.gameObject.transform);
                go.transform.rotation = Quaternion.identity;
                go.transform.localPosition = Vector3.zero;

                root = go.transform;

#if UNITY_EDITOR
                if (selectInEditor)
                {
                    EditorGUIUtility.PingObject(go);
                    Selection.activeGameObject = go;
                }
#endif
            }

            var creator = root.GetComponentInChildren<UIDocumentScriptComponentCreator>();
            if (creator == null)
            {
                creator = root.gameObject.AddComponent<UIDocumentScriptComponentCreator>();
            }

            return creator;
        }

        public static UIDocumentScriptComponentCreator FindInScene()
        {
            // Find creator ..
            UIDocumentScriptComponentCreator creator = null;
#if UNITY_EDITOR
            // .. based on selected game objects
            if (Selection.activeGameObject != null)
            {
                creator = Selection.activeGameObject.GetComponentInParent<UIDocumentScriptComponentCreator>(includeInactive: true);
            }
#endif
            // .. based on the UIDocuments in the scene (take the first)
            if (creator == null)
            {
#if UNITY_2023_1_OR_NEWER
                var creators = GameObject.FindObjectsByType<UIDocumentScriptComponentCreator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
                var creators = GameObject.FindObjectsOfType<UIDocumentScriptComponentCreator>(includeInactive: true);
#endif
                if (creators.Length > 0)
                {
                    creator = creators[0];
                }
            }

            return creator;
        }

        public void OnEnable()
        {
            // According to docs it is guaranteed that the uxml is loaded because it loads in OnEnable but with an early script exectution order.
            RefreshAll();
        }

        public void RefreshAll()
        {
            var links = GetComponentsInChildren<LinkToVisualElement>(includeInactive: true);
            foreach (var link in links)
            {
                link.RefreshElement();
            }
        }

        public void DestroyAll()
        {
            if (Document == null)
            {
                Logger.LogError("No UIDocument found: There is no UIDocument Component on the selected object -> aborting.");
                return;
            }

            // Destroy all
            var resolvers = Document.transform.GetComponentsInChildren<LinkToVisualElement>();
            for (int i = resolvers.Length - 1; i >= 0; i--)
            {
#if UNITY_EDITOR
                if (!UnityEditor.EditorApplication.isPlaying)
                {
                    DestroyImmediate(resolvers[i].gameObject);
                }
                else
                {
                    Destroy(resolvers[i].gameObject);
                }
#else
                Destroy(resolvers[i].gameObject);
#endif
            }
        }

        public void CreateOrUpdateLinks(List<VisualElement> elements)
        {
            foreach (var ele in elements)
            {
                var link = GetLink(ele);
                if (link != null)
                {
                    Logger.Log($"There already is an element linked to '{ele.name}'. Skipping creation.");
                }
                else
                {
                    link = CreateGameObjectWithScript<LinkToVisualElement>(ele);
                }

#if UNITY_EDITOR
                EditorGUIUtility.PingObject(link.gameObject);
                EditorUtility.SetDirty(link);
                Selection.activeGameObject = link.gameObject;
#endif
            }
        }

        public void CreateLink(VisualElement element, GameObject gameObject)
        {
            var link = GetLink(element);
            if (link != null)
            {
                Logger.Log($"There already is an element linked to '{element.name}'. Skipping creation.");
            }
            else
            {
                link = CreateGameObjectWithScript<LinkToVisualElement>(element, gameObject);
            }

#if UNITY_EDITOR
            EditorGUIUtility.PingObject(link.gameObject);
            EditorUtility.SetDirty(link);
            Selection.activeGameObject = link.gameObject;
#endif
        }

        /// <summary>
        /// Adds a game object with TScript based on the given element TVisualElements.<br />
        /// </summary>
        /// <typeparam name="TScript"></typeparam>
        /// <param name="element"></param>
        /// <param name="gameObject">The gameobject which to attach the component too. Will create a new game object if NULL.</param>
        /// <returns></returns>
        public TScript CreateGameObjectWithScript<TScript>(VisualElement element, GameObject gameObject = null)
        where TScript : LinkToVisualElement
        {
            string objName = LinkToVisualElement.GenerateGameObjectName(element);
            if (gameObject == null)
                gameObject = new GameObject(objName);
            gameObject.transform.SetParent(transform);
            gameObject.transform.rotation = Quaternion.identity;
            gameObject.transform.localPosition = Vector3.zero;
            gameObject.SetActive(false);

            var link = gameObject.AddComponent<TScript>();
            link.Element = element;
            link.RefreshObjectName(); // Necessary because OnValidate may have messed up the name.

            gameObject.SetActive(true);

#if UNITY_EDITOR
            EditorGUIUtility.PingObject(gameObject);
            Selection.activeGameObject = gameObject;
#endif

            return link;
        }

        /// <summary>
        /// Searches for a link component matching the given element.<br />
        /// The search is done based on a generated path. Therefore you can pass in any element (UI Builder or UI Document origin).<br />
        /// The first check uses the cached references to the element in UIDocument and (if in editor) element in UI Builder.<br />
        /// If there are no cached elements and if the path does not match then it will not find the element.
        /// </summary>
        /// <param name="ele"></param>
        /// <returns>NULL if not found.</returns>
        public LinkToVisualElement GetLink(VisualElement ele)
        {
            if (ele == null)
                return null;

            if (ele.panel == null)
            {
                Logger.LogWarning("The element is detached. Will return null.");
                return null;
            }

            var existing = gameObject.GetComponentsInChildren<LinkToVisualElement>(includeInactive: true);
            var elementLinkPath = VisualElementPath.Create(ele);

            foreach (var link in existing)
            {
                // First try to find by direct reference.
                if (link.Element == ele || link.GetRawElement() == ele)
                {
                    return link;
                }

#if UNITY_EDITOR
                if (link.ElementInBuilder == ele || link.GetRawElementInBuilder() == ele)
                {
                    return link;
                }
#endif

                // Second try to find by path.
                if (link.Matches(elementLinkPath))
                {
                    return link;
                }
            }

            return null;
        }

        public LinkToVisualElement[] GetLinks()
        {
            return GetComponentsInChildren<LinkToVisualElement>(includeInactive: true);
        }

        public bool HasLink(VisualElement ele)
        {
            return GetLink(ele) != null;
        }

        /// <summary>
        /// Create a query game object or add a QueryForVisualElements to the given gameObject.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        public QueryForVisualElements CreateQuery(GameObject gameObject = null)
        {
            string objName = "Query";
            if (gameObject == null)
                gameObject = new GameObject(objName);
            gameObject.transform.SetParent(transform);
            gameObject.transform.rotation = Quaternion.identity;
            gameObject.transform.localPosition = Vector3.zero;
            gameObject.SetActive(false);

            var query = gameObject.AddComponent<QueryForVisualElements>();
            gameObject.SetActive(true);

#if UNITY_EDITOR
            EditorGUIUtility.PingObject(gameObject);
            Selection.activeGameObject = gameObject;
#endif

            return query;
        }
    }
}
#endif
