#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kamgam.UIToolkitTextAnimation
{
    /// <summary>
    /// On script reloads or whenever the UI Builder is started it will check all
    /// panels for TextElements with animation tags. If it finds any then it will add a
    /// text animation manipulator to each of them.
    /// 
    /// It will do nothing in PLAY MODE. This class exists only to display the text animation while
    /// editing in the UI Builder. Usually it adds the animation to the UI Builder window and
    /// the Game Window while editing.
    /// </summary>
    public static class UIEditorPanelObserver
    {
        private static double editorLoadDomainReloadTime;
        
        [DidReloadScripts, InitializeOnLoadMethod]
        public static void DidReloadScripts()
        {
            EditorApplication.update -= onEditorUpdate;
            EditorApplication.update += onEditorUpdate;

            // Needed because we do not have a classListChanged event, see:
            // https://discussions.unity.com/t/event-for-visualelement-class-list-change/911738
            UnityEditor.EditorApplication.update += onEditorUpdate;

            // Ensure panel refresh after building.
            BuildProcessObserver.OnBuildEnded -= onBuildCompleted;
            BuildProcessObserver.OnBuildEnded += onBuildCompleted;

            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened -= onSceneOpened;
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += onSceneOpened;
            
            editorLoadDomainReloadTime = EditorApplication.timeSinceStartup;
        }

        private static void onSceneOpened(Scene scene, OpenSceneMode openSceneMode)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            // Search in roots for UI Document
            // Caveat: This ignores UI Documents that are 
            var roots = scene.GetRootGameObjects();
            foreach (var go in roots)
            {
                if (go.TryGetComponent<TextAnimationDocument>(out var doc))
                {
                    doc.AddOrRemoveManipulators();
                }
            }

            if (TextAnimationManipulator.ActiveManipulators.Count > 0)
            {
                UIEditorPanelObserver.RefreshGameView(forceRebuild: false);
            }
        }

        private static void onSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (TextAnimationManipulator.ActiveManipulators.Count > 0)
            {
                UIEditorPanelObserver.RefreshGameView(forceRebuild: true);
            }
        }

        private static void onBuildCompleted(BuildReport obj)
        {
            OnUpdateAfterScriptReload();
        }

        private static double _lastEditorCheckTime = -1;
        
        private static void onEditorUpdate()
        {
            // Check once every second.
            var timeSinceLastCheck = EditorApplication.timeSinceStartup - _lastEditorCheckTime;
            if (timeSinceLastCheck > 1)
            {
                _lastEditorCheckTime = EditorApplication.timeSinceStartup;
                onEditorCheckElementsInUIBuilderPanel();
            }
        }

        private static void onEditorCheckElementsInUIBuilderPanel()
        {
            // Do nothing during play mode.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            
            foreach (var kv in _editorPanels)
            {
                var textPanel = kv.Value;
                
                // Check for elements that had the text-animation class added.
                textPanel.RootVisualElement
                    .Query<TextElement>(className: TextAnimationManipulator.TEXT_ANIMATION_CLASSNAME)
                    .ForEach(TextAnimationManipulator.AddOrRemoveManipulator);
                
                // Check for elements that had the text-animation class removed.
                foreach (var manipulator in TextAnimationManipulator.ManipulatorRegistry)
                {
                    if (manipulator.target == null)
                        continue;

                    if (!manipulator.target.ClassListContains(TextAnimationManipulator.TEXT_ANIMATION_CLASSNAME))
                    {
                        TextAnimationManipulator.AddOrRemoveManipulator(manipulator.TargetTextElement);
                    }
                }
            }
        }
        
        [DidReloadScripts]
        public static void OnUpdateAfterScriptReload()
        {
            Initialize();
        }

        [MenuItem("Tools/" + Installer.AssetName + "/Refresh Preview", priority = 1)]
        public static void ForceRefresh()
        {
            Initialize();
            RefreshGameView(forceRebuild: false);
        }
        
        [MenuItem("Tools/" + Installer.AssetName + "/Refresh Preview (force rebuild)", priority = 2)]
        public static void ForceRefreshAndRebuild()
        {
            Initialize();
            RefreshGameView(forceRebuild: true);
        }
        
        public static void Initialize()
        {
            TextAnimations.FindAssetInEditor()?.Refresh();
            
            EditorApplication.update -= ForceRefresh;
            EditorApplication.playModeStateChanged -= onPlayModeChanged;
            EditorApplication.playModeStateChanged += onPlayModeChanged;

            var wrapper = UIBuilderWindowWrapper.Instance;
            wrapper.OnBuilderWindowOpened -= RefreshPanels;
            wrapper.OnBuilderWindowOpened += RefreshPanels;

            wrapper.OnDocumentLoaded -= OnDocumentLoaded;
            wrapper.OnDocumentLoaded += OnDocumentLoaded;

            wrapper.OnHierarchyChanged -= onHierarchyChanged;
            wrapper.OnHierarchyChanged += onHierarchyChanged;

            wrapper.OnClassNameAdded -= onClassNameAdded;
            wrapper.OnClassNameAdded += onClassNameAdded;
            wrapper.OnClassNameRemoved -= onClassNameRemoved;
            wrapper.OnClassNameRemoved += onClassNameRemoved;

            Undo.undoRedoPerformed -= onUndo;
            Undo.undoRedoPerformed += onUndo;

            // Trigger refresh on panels
            EditorApplication.delayCall += RefreshPanels;
        }

        private static void onClassNameAdded(string className)
        {
            if (className.StartsWith(TextAnimationManipulator.TEXT_ANIMATION_CLASSNAME))
                AddComponentToUIDocumentIfNeeded();
            
            RefreshGameView(forceRebuild: true);
        }

        private static void onClassNameRemoved(string className)
        {
            RefreshGameView(forceRebuild: true);
        }

        public static void AddComponentToUIDocumentIfNeeded()
        {
            int sceneCount = EditorSceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                var roots = EditorSceneManager.GetSceneAt(i).GetRootGameObjects();
                foreach (var root in roots)
                {
                    var uiDocuments = root.GetComponentsInChildren<UIDocument>(includeInactive: true);
                    foreach (var doc in uiDocuments)
                    {
                        var anim = doc.rootVisualElement.Q(className: TextAnimationManipulator.TEXT_ANIMATION_CLASSNAME);
                        if (anim != null)
                        {
                            var animationDoc = doc.gameObject.GetComponent<TextAnimationDocument>();
                            if (animationDoc == null)
                            {
                                doc.gameObject.AddComponent<TextAnimationDocument>();
                                EditorUtility.SetDirty(doc.gameObject);
                                EditorGUIUtility.PingObject(doc.gameObject);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Updates the game view. It does so by triggering a change event on the "focusable" toggle in the UI Builder.
        /// This then trigger the default "refresh" process (I did not find out yet how it works internally but okay).
        /// See: https://discussions.unity.com/t/class-names-are-not-synced-between-ui-builder-and-game-view/1602653
        /// 
        /// NOTICE: This works only if an element is selected in the UI Builder, but that is okay here.
        /// </summary>
        public static void RefreshGameView(bool forceRebuild)
        {
            // Abort if playing or will be playing. Otherwise it will cause the game view to rebuild at runtime,
            // not good (all references are lost).
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (forceRebuild)
            {
                // Remember and restore focus.
                var focusedWindow = EditorWindow.focusedWindow;
                if (focusedWindow != null && !focusedWindow.GetType().FullName.Contains("Builder"))
                    EditorScheduler.Schedule(0.2f, () => focusedWindow.Focus());

                if (UIBuilderWindowWrapper.Instance == null ||
                    UIBuilderWindowWrapper.Instance.RootVisualElement == null)
                {
                    // We no longer show it since it is annoying people.
                    // Debug.LogWarning("Refreshing UITK Text Animations failed. Please open the UI Builder window first.");
                    return;
                }

                if (UIBuilderWindowWrapper.Instance.HasWindow &&
                    !UIBuilderWindowWrapper.Instance.BuilderWindow.hasFocus)
                {
                    UIBuilderWindowWrapper.Instance.BuilderWindow.Focus();
                }

                var focusableToggle = UIBuilderWindowWrapper.Instance.RootVisualElement.Query<Toggle>()
                    .Where(elem => elem.bindingPath == "focusable").First();
                // No focusable selected? Try to select one;
                VisualElement previousSelection = null; // <- to revert.
                if (focusableToggle == null)
                {
                    var selection = UIBuilderWindowWrapper.Instance.Selection;
                    if (selection != null && selection.Count > 0)
                        previousSelection = UIBuilderWindowWrapper.Instance.Selection[0];
                    var textAnimationElement =
                        UIBuilderWindowWrapper.Instance.RootVisualElement.Q<TextElement>(className: "text-animation");
                    if (textAnimationElement != null)
                    {
                        UIBuilderWindowWrapper.Instance.Select(textAnimationElement);
                        focusableToggle = UIBuilderWindowWrapper.Instance.RootVisualElement.Query<Toggle>()
                            .Where(elem => elem.bindingPath == "focusable").First();
                        UIBuilderWindowWrapper.Instance.Select(previousSelection);
                    }
                }

                // Still no focusable? Then we are at a loss and can not update the game view.
                // TODO: Investigate if there is a better solution that does not require this toggle event trickery.
                if (focusableToggle != null)
                {
                    using (ChangeEvent<bool> pooled =
                           ChangeEvent<bool>.GetPooled(!focusableToggle.value, focusableToggle.value))
                    {
                        pooled.target = focusableToggle;
                        focusableToggle.SendEvent(pooled);
                    }
                }
                else
                {
                    // TODO: Investigate why this sometimes happens even if the UI Builder is selected.
                    // if (EditorApplication.timeSinceStartup - editorLoadDomainReloadTime > 2f && !EditorApplication.isPlayingOrWillChangePlaymode)
                    //    Debug.LogWarning("Refreshing UITK Text Animations failed. Please select an element with a text-animation class on it in the UI Builder.");
                }
            }
            else
            {
                // In most cases this is sufficient. Except for classname list changes
                // (weirdly those are not synced so we have to use the heavy code from above).
                EditorApplication.QueuePlayerLoopUpdate();
            }
        }

        private static VisualElement getGameViewViewport()
        {
            // Find all panels
            _tmpAllPanels.Clear();
            UIElementsUtilityProxy.GetAllPanels(_tmpAllPanels);

            // Check if there are new panels that need a new panel.
            // Our goal is to only affect the UI BUILDER and the GAME view.
            foreach (var panel in _tmpAllPanels)
            {
                if (panel.contextType == ContextType.Player)
                {
                    return panel.visualTree;
                }
            }

            return null;
        }

        private static void onHierarchyChanged()
        {
            RefreshPanelsDelayed();
        }

        private static void onUndo()
        {
            var wrapper = UIBuilderWindowWrapper.Instance;
            if(wrapper.HasWindow)
            {
                RefreshPanelsDelayed();

                // Another delay for UNDOs (maybe the reload of the UI is taking too long?)
                // TODO: Investigate, delaying via EditorApplication.delayCall is too short for the game view.
                // Find a logical solution.
                EditorScheduler.Schedule(0.5f, RefreshPanelsDelayed);
            }
        }

        private static void onPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode)
            {
                ClearEditorManipulators();
            }

            if (change == PlayModeStateChange.EnteredEditMode)
            {
                RefreshPanelsDelayed();
            }
        }

        public static void ClearEditorManipulators()
        {
            foreach (var panel in _editorPanels.Values)
            {
                panel.Destroy();
            }
            _editorPanels.Clear();
        }

        static Dictionary<IPanel, TextAnimationPanel> _editorPanels = new Dictionary<IPanel, TextAnimationPanel>();
        static List<IPanel> _tmpAllPanels = new List<IPanel>();
        static List<IPanel> _tmpPanelsToRemove = new List<IPanel>();

        public static void OnDocumentLoaded()
        {
            RefreshPanelsDelayed();
        }

        public static void RefreshPanelsDelayed()
        {
            EditorApplication.delayCall += () =>
            {
                RefreshPanels();
            };
        }

        /// <summary>
        /// Goes through all editor and player panels and adds manipulators to any element that has the text-animation class.
        /// </summary>
        public static void RefreshPanels()
        {
            // Find all panels
            _tmpAllPanels.Clear();
            UIElementsUtilityProxy.GetAllPanels(_tmpAllPanels);

            // Check if there are new panels that need a new panel.
            // Our goal is to only affect the UI BUILDER and the GAME view.
            int pCount = _tmpAllPanels.Count;
            for (int i = 0; i < pCount; i++)
            {
                var panel = _tmpAllPanels[i];

                // Skip GAME view panel if we are in play mode (because these are then updated by the regular player code).
                if (panel.contextType == ContextType.Player && EditorApplication.isPlayingOrWillChangePlaymode)
                    continue;

                // Skip all panels except UI BUILDER and GAME view.
                if (!UIElementsUtilityProxy.IsUIBuilderPanel(panel) && !UIElementsUtilityProxy.IsGameViewPanel(panel))
                    continue;

                // The panel may be updated twice here. Once after the geometry changed event (for after compilation).
                panel.visualTree.RegisterCallback<GeometryChangedEvent>(onGeometryChanged);
                // And once regularly (for play mode changes, loaded document changes).
                refreshPanel(panel);
            }

            // Remove unused panels
            // Usually not needed as we clean them up in ClearEditorManipulators().
            _tmpPanelsToRemove.Clear();
            foreach (var panel in _editorPanels.Keys)
            {
                if (!_tmpAllPanels.Contains(panel))
                {
                    _tmpPanelsToRemove.Add(panel);
                }
            }
            foreach (var panel in _tmpPanelsToRemove)
            {
                _editorPanels[panel].Destroy();
                _editorPanels.Remove(panel);
            }
        }

        private static void onGeometryChanged(GeometryChangedEvent evt)
        {
            var ve = evt.target as VisualElement;
            ve.panel.visualTree.UnregisterCallback<GeometryChangedEvent>(onGeometryChanged);

            refreshPanel(ve.panel);
        }

        private static void refreshPanel(IPanel panel)
        {
            TextAnimationPanel textAnimationPanel;
            if (_editorPanels.TryGetValue(panel, out var editorPanel))
            {
                textAnimationPanel = editorPanel;
            }
            else
            {
                textAnimationPanel = new TextAnimationPanel(panel.visualTree, null);
                _editorPanels.Add(panel, textAnimationPanel);
            }

            if (textAnimationPanel.Configs == null)
            {
                textAnimationPanel.Configs = TextAnimationsProvider.GetAnimations();
            }

            textAnimationPanel.AddOrRemoveManipulators();
            textAnimationPanel.UpdateManipulatorsAfterClassChange();
        }
    }
}

#endif