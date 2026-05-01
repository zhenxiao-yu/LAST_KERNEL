using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace Kamgam.UIToolkitScriptComponents
{
    [CustomEditor(typeof(UIDocumentScriptComponentCreator))]
    public class UIDocumentScriptComponentCreatorEditor : Editor
    {
        protected UIDocumentScriptComponentCreator _creator;

        public void OnEnable()
        {
            _creator = target as UIDocumentScriptComponentCreator;

            if (UIBuilderWindowWrapper.Instance.BuilderWindow != null)
            {
                UIBuilderWindowWrapper.Instance.OnSelectionChanged += onSelectionInUIBuilderChanged;
            }
        }

        public void OnDisable()
        {
            if (UIBuilderWindowWrapper.Instance.BuilderWindow != null)
            {
                UIBuilderWindowWrapper.Instance.OnSelectionChanged -= onSelectionInUIBuilderChanged;
            }
        }

        private void onSelectionInUIBuilderChanged(List<VisualElement> obj)
        {
            Repaint();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (!EditorApplication.isPlaying)
            {
                if (UIBuilderWindowWrapper.Instance.BuilderWindow != null && UIBuilderWindowWrapper.Instance.Selection.Count > 0)
                {
                    string name = "Selected";
                    if (UIBuilderWindowWrapper.Instance.Selection.Count == 1)
                    {
                        name = UIBuilderWindowWrapper.Instance.Selection[0].GetType().Name;
                        if (!string.IsNullOrEmpty(UIBuilderWindowWrapper.Instance.Selection[0].name))
                        {
                            name += " (" + UIBuilderWindowWrapper.Instance.Selection[0].name + ")";
                        }
                    }
                        
                    if (GUILayout.Button("Create Script for " + name))
                    {
                        var elements = UIBuilderWindowWrapper.Instance.Selection;
                        _creator.CreateOrUpdateLinks(elements); 
                    }
                }
                else
                {
                    if (GUILayout.Button("Create New Script"))
                    {
                        var go = _creator.CreateGameObjectWithScript<LinkToVisualElement>(null);
                    }
                }
            }
        }
    }
}
