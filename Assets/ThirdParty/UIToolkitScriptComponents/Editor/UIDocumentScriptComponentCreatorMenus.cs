using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace Kamgam.UIToolkitScriptComponents
{
    public class UIDocumentScriptComponentCreatorMenus
    {
        [MenuItem("Tools/UI Toolkit Script Components/Add or Select Script", priority = 2)]
        [MenuItem("GameObject/UI Toolkit/Add or Select Script", priority = 102)]
        public static void AddScriptForSelectedVisualElements()
        {
            // Find document (by selection)
            UIDocument document = null;
            if (Selection.activeGameObject != null)
            {
                Selection.activeGameObject.GetComponentInParent<UIDocument>();
            }
            // If no UI Document is selected then search for one (select the first found)
            if (document == null)
            {
#if UNITY_2023_1_OR_NEWER
                var documents = GameObject.FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
#else
                var documents = GameObject.FindObjectsOfType<UIDocument>();
#endif 
                if (documents.Length == 1)
                {
                    document = documents[0];
                }
                else if (documents.Length > 1)
                {
                    Logger.LogWarning($"There are multiple UIDocuments in the scene. Please select the one you wish to use in the hierarchy.");
                }
                else if (documents.Length == 0)
                {
                    Logger.LogWarning($"There are no UIDocuments in the scene. Please add at least one.");
                }
            }

            if (document != null)
            {
                var creator = UIDocumentScriptComponentCreator.GetOrCreate(document.gameObject, selectInEditor: false);
                if (creator != null)
                {
                    var elements = UIBuilderWindowWrapper.Instance.Selection;
                    if (elements.Count > 0)
                    {
                        // Add link component to the selected game object if it has no link yet and if
                        // there is only one element selected (aka add a Link component).
                        if (Selection.activeGameObject != null
                            && Selection.activeGameObject.GetComponent<LinkToVisualElement>() == null
                            && Selection.activeGameObject.GetComponentInParent<UIDocumentScriptComponentCreator>() == creator
                            && Selection.activeGameObject.transform != creator.transform
                            && elements.Count == 1)
                        {
                            // Add component to existing game object
                            creator.CreateLink(elements[0], Selection.activeGameObject);
                        }
                        else
                        {
                            // Create a script component for every selected element
                            creator.CreateOrUpdateLinks(elements);
                        }
                    }
                    else
                    {
                        // Create an unlinked script component
                        creator.CreateGameObjectWithScript<LinkToVisualElement>(null);
                    }
                }
            }
        }

        [MenuItem("Tools/UI Toolkit Script Components/Add Script Creator", priority = 1)]
        [MenuItem("GameObject/UI Toolkit/Add Script Creator", priority = 101)]
        public static void AddScriptCreator()
        {
            if (Selection.activeGameObject == null)
                return;

            var document = Selection.activeGameObject.GetComponentInParent<UIDocument>(includeInactive: true);
            if (document == null)
                return;

            UIDocumentScriptComponentCreator.GetOrCreate(document.gameObject, selectInEditor: true);
        }

        [MenuItem("Tools/UI Toolkit Script Components/Add Script Creator", priority = 1, validate = true)]
        [MenuItem("GameObject/UI Toolkit/Add Script Creator", priority = 101, validate = true)]
        public static bool AddScriptCreatorValidate()
        {
            if (Selection.activeGameObject == null)
                return false;

            var document = Selection.activeGameObject.GetComponentInParent<UIDocument>(includeInactive: true);
            if (document == null)
                return false;

            var creator = document.gameObject.GetComponentInChildren<UIDocumentScriptComponentCreator>(includeInactive: true);
            return creator == null;
        }

        [MenuItem("Tools/UI Toolkit Script Components/Add Query", priority = 2)]
        [MenuItem("GameObject/UI Toolkit/Add Query", priority = 102)]
        public static void AddQuery()
        {
            if (Selection.activeGameObject == null)
                return;

            var document = Selection.activeGameObject.GetComponentInParent<UIDocument>(includeInactive: true);
            if (document == null)
                return;

            var creator = UIDocumentScriptComponentCreator.GetOrCreate(document.gameObject, selectInEditor: false);
            if (creator != null)
            {
                creator.CreateQuery();
            }
        }

        [MenuItem("Tools/UI Toolkit Script Components/Add Query", priority = 2, validate = true)]
        [MenuItem("GameObject/UI Toolkit/Add Query", priority = 102, validate = true)]
        public static bool AddQueryValidate()
        {
            if (Selection.activeGameObject == null)
                return false;

            var document = Selection.activeGameObject.GetComponentInParent<UIDocument>(includeInactive: true);
            return document != null;
        }
    }
}
