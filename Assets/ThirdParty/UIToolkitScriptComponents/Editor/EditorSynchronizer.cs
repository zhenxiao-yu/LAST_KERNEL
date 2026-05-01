// EDITOR ONLY
// With 2021.2 UIToolkit was integrated with Unity instead of being a package.
#if UNITY_EDITOR && (KAMGAM_UI_ELEMENTS || UNITY_2021_2_OR_NEWER)

using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;

namespace Kamgam.UIToolkitScriptComponents
{
    /// <summary>
    /// Synchronizes selections and attribute changes.
    /// TODO: Find a simpe way to access OnHierarchyChanged events.
    /// </summary>
    public class EditorSynchronizer
    {
        [UnityEditor.Callbacks.DidReloadScripts]
        public static void InitOnLoad()
        {
            UIBuilderWindowWrapper.Instance.OnSelectionChanged += onUIBuilderSelectionChanged;
            UIBuilderWindowWrapper.Instance.OnAttributeChanged += onUIBuilderAttributeChanged;
            Selection.selectionChanged += onSelectionChanged;
        }

        static void onUIBuilderAttributeChanged(VisualElement element)
        {
            if (UIBuilderWindowWrapper.Instance.BuilderWindow == null)
                return;

            UIDocumentScriptComponentCreator creator = UIDocumentScriptComponentCreator.FindInScene();

            if (creator != null)
            {
                // find the link for the element (if there is any) and refresh it
                var link = creator.GetLink(element);
                if (link != null)
                {
                    link.RefreshPath();
                    link.name = LinkToVisualElement.GenerateGameObjectName(link.Element);
                    EditorUtility.SetDirty(link);
                }

                // Iterate through all children in UI Builder and check if they have a link. If yes, then refresh their paths.
                walkTreeAndRefreshPath(element, creator);

                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }
        }

        static void walkTreeAndRefreshPath(VisualElement element, UIDocumentScriptComponentCreator creator)
        {
            if (element == null)
                return;

            // refresh link
            var link = creator.GetLink(element);
            if (link != null)
            {
                link.RefreshPath();
                EditorUtility.SetDirty(link);
            }

            // Recurse into chldren
            for (int i = 0; i < element.childCount; i++)
            {
                walkTreeAndRefreshPath(element[i], creator);
            }
        }

        static void onSelectionChanged()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode && !UIToolkitScriptComponentsSettings.GetOrCreateSettings().SyncInPlayMode)
                return;

            if (!UIToolkitScriptComponentsSettings.GetOrCreateSettings().SyncSelection)
                return;

            if (Selection.activeGameObject == null)
                return;

            if (UIBuilderWindowWrapper.Instance.BuilderWindow == null)
                return;

            var link = Selection.activeGameObject.GetComponentInParent<LinkToVisualElement>(includeInactive: true);
            if (link != null)
            {
                UIBuilderWindowWrapper.Instance.Select(link.ElementInBuilder);
            }
        }

        static void onUIBuilderSelectionChanged(List<VisualElement> obj)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode && !UIToolkitScriptComponentsSettings.GetOrCreateSettings().SyncInPlayMode)
                return;

            if (!UIToolkitScriptComponentsSettings.GetOrCreateSettings().SyncSelection)
                return;

            if (UIBuilderWindowWrapper.Instance.BuilderWindow == null)
                return;

            var selection = UIBuilderWindowWrapper.Instance.Selection;
            if (selection == null || selection.Count == 0)
                return;

            UIDocumentScriptComponentCreator creator = UIDocumentScriptComponentCreator.FindInScene();

            // It happened that the panel was null.
            // TODO: investigate (sadly hard to reproduce)
            if (creator != null && selection[0].panel != null)
            {
                var link = creator.GetLink(selection[0]);
                if (link != null)
                {
                    Selection.activeGameObject = link.gameObject;
                    link.RefreshElement();
                }
                else
                {
                    // Check if we are on a link object which has no path or builderElement. If yes then do not change selection.
                    bool stayOnSelected = false;
                    if (Selection.activeGameObject != null)
                    {
                        var selectedLink = Selection.activeGameObject.GetComponent<LinkToVisualElement>();
                        stayOnSelected = selectedLink != null && (!selectedLink.HasPath() || selectedLink.ElementInBuilder == null);
                    }

                    // Fall back to creator if no element exists yet.
                    if (!stayOnSelected)
                    {
                        Selection.activeGameObject = creator.gameObject;
                    }
                }
            }
        }
    }
}

#endif