using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using System;
using System.Linq;

namespace Kamgam.UIToolkitScriptComponents
{
    [CustomEditor(typeof(LinkToVisualElement))]
    public class LinkToVisualElementEditor : Editor
    {
        protected LinkToVisualElement _link;

        // We store the name and viewDataKey of the UIBuilder element here to detect changes.
        // TODO: investigate if there is a proper API in the UIBuilder itself (sadly the notification interface is inaccessible).
        protected string _cachedLinkElementName = null;
        protected string _cachedLinkElementViewDataKey = null;

        public void OnEnable()
        {
            _link = target as LinkToVisualElement;
            _link.OnValidate();

            if (UIBuilderWindowWrapper.Instance.BuilderWindow != null)
            {
                UIBuilderWindowWrapper.Instance.OnSelectionChanged += onSelectionInUIBuilderChanged;
            }

            _cachedLinkElementName = null;
            _cachedLinkElementViewDataKey = null;
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
                if (UIBuilderWindowWrapper.Instance.BuilderWindow != null)
                {
                    var selection = UIBuilderWindowWrapper.Instance.Selection;

                    // auto refresh path
                    VisualElement elementInBuilder = null;
                    if (_link != null)
                    {
                        elementInBuilder = _link.ElementInBuilder;
                    }
                    if (elementInBuilder != null)
                    {
                        if (_cachedLinkElementName != elementInBuilder.name || _cachedLinkElementViewDataKey != elementInBuilder.viewDataKey)
                        {
                            _cachedLinkElementName = elementInBuilder.name;
                            _cachedLinkElementViewDataKey = elementInBuilder.viewDataKey;
                            _link.RefreshPath();
                        }
                    }

                    // Select in Builder button
                    GUI.enabled = elementInBuilder != null;
                    if (GUILayout.Button("Select in UIBuilder"))
                    {
                        if (elementInBuilder != null)
                        {
                            UIBuilderWindowWrapper.Instance.Select(elementInBuilder);
                        }
                        else
                        {
                            Logger.LogWarning("Element not found in UI Builder");
                        }
                    }
                    if (GUILayout.Button("Refresh Path"))
                    {
                        _link.RefreshPath();
                    }
                    if (GUILayout.Button("Refresh Element"))
                    {
                        _link.RefreshElement();
                    }
                    GUI.enabled = true;

                    // Draw element info
                    var col = GUI.color;
                    if (elementInBuilder != null && _link.Element != null)
                    {
                        GUI.color = Color.green;
                        GUILayout.Label($"Element: " + LinkToVisualElement.GenerateGameObjectName(_link.Element));
                    }
                    else
                    {
                        GUI.color = Color.red;
                        if (_link.Element == null)
                        {
                            GUILayout.Label($"Element not found in Builder. This is NOT linked.\nPlease try saving both the scene and the layout in the UI Builder and then unlink/relink this element.");
                        }
                        else
                        {
                            GUILayout.Label($"Element not found in Builder. Element is detached.\nMaybe the UI Builder window is closed or hidden.");
                        }
                        GUI.color = col;
                    }
                    GUI.color = col;

                    // Duplicate info
                    var creator = _link.Document.GetComponentInChildren<UIDocumentScriptComponentCreator>();
                    if (creator != null)
                    {
                        var duplicates = creator.GetLinks().Where(l => l.Matches(_link.Path)).ToArray();
                        if (duplicates.Count() > 1)
                        {
                            GUI.color = Color.yellow;
                            GUILayout.Label("There are multiple links with these same path!");
                            foreach (var duplicate in duplicates)
                            {
                                if (duplicate.gameObject != _link.gameObject)
                                {
                                    GUILayout.Label("  * Duplicate in '"+duplicate.gameObject.name+"'");
                                }
                            }
                            GUI.color = col;
                        }
                    }

                    GUILayout.Space(3);

                    // Path not unique info
                    if (_link.IsIdentifiedByIndex() && !_link.IsIdentifiedByUniqueName())
                    {
                        EditorGUIHelpers.DrawLabel("There are elements without names or dataKeys in the path. Please consider naming them.", color: Color.yellow);
                    }

                    GUILayout.Space(7);

                    // Link to selected
                    GUI.enabled = selection.Count > 0;
                    if (GUILayout.Button(new GUIContent("Link THIS to current UI Builder selection", "Links this script to the currently selected visual element in the UI Builder.")))
                    {
                        Undo.RecordObject(_link, "Unlink");
                        _link.Element = selection[0];
                        _link.gameObject.name = LinkToVisualElement.GenerateGameObjectName(_link.Element);
                        EditorUtility.SetDirty(_link);
                    }
                    GUI.enabled = true;
                    
                    if (_link.HasPath())
                    {
                        // Unlink
                        if (GUILayout.Button("Unlink"))
                        {
                            Undo.RecordObject(_link, "Unlink");
                            _link.Unlink();
                            EditorUtility.SetDirty(_link);
                        }
                    }

                    // Delete btn
                    if (GUILayout.Button("Delete"))
                    {
                        var go = _link.gameObject;
                        EditorApplication.delayCall += () =>
                        {
                            Undo.DestroyObjectImmediate(go);
                        };
                    }

                    // Warning that the current element does not match the element selected in the UI Builder
                    if (selection.Count > 0 && selection[0] != elementInBuilder && _link.HasPath())
                    {
                        GUILayout.Space(7);
                        EditorGUIHelpers.DrawLabel("<b>NOTICE:</b>\nThis Link does NOT match the current selection in UI Builder.", color: Color.white);
                        if (creator != null)
                        {
                            
                            if (GUILayout.Button(new GUIContent("Select Link matching UI Builder")))
                            {
                                var link = creator.GetLink(selection[0]);
                                if (link != null)
                                {
                                    EditorGUIUtility.PingObject(link.gameObject);
                                    Selection.activeGameObject = link.gameObject;
                                }
                                else
                                {
                                    Logger.LogMessage("There is no script link matching the current selection in the UI Builder. Selecting the creator instead.");
                                    if (creator != null)
                                    {
                                        EditorGUIUtility.PingObject(creator.gameObject);
                                        Selection.activeGameObject = creator.gameObject;
                                    }
                                }
                            }
                        }
                    }
                    GUI.enabled = true;
                }
                else
                {
                    EditorGUIHelpers.DrawLabel("Please open the UI Builder window to unlock more options (Window > UI Toolkit > UI Builder).", color: Color.yellow);
                }
            }
            else
            {
                // Draw element info
                var elementInDocument = _link.Element;
                var col = GUI.color;
                if (elementInDocument != null)
                {
                    GUI.color = Color.green;
                    GUILayout.Label($"Element: {_link.Element.GetType().Name} ({_link.Element.name})");
                }
                else
                {
                    GUI.color = Color.red;
                    GUILayout.Label($"Element not found in UIDocument. This is NOT linked.");
                    GUI.color = col;
                }
                GUI.color = col;
            }
        }
    }
}
