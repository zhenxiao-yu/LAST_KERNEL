#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitSoundEffects
{
    public static class UIBuilderWindowInspector
    {
        private static bool _mainFoldoutState = true;

        private static VisualElement _target;
        
        private static EnumField _event0EventTypeField;
        private static ObjectField _event0Clip0Field;
        private static string _newEffectId;

        public static void Init()
        {
            UIBuilderWindowWrapper.Instance.OnSelectionChanged += onSelectionChanged;
            UIBuilderWindowWrapper.Instance.OnClassNameAdded += onClassNameAdded;
            UIBuilderWindowWrapper.Instance.OnClassNameRemoved += onClassNameRemoved;
        }

        private static void onClassNameAdded(string className)
        {
            if (className.StartsWith(SoundEffect.CLASSNAME_ID_PREFX))
            {
                if (s_ignoreNextClassAddEvent)
                {
                    s_ignoreNextClassAddEvent = false;
                    return;
                }
                
                rebuildUI(UIBuilderWindowWrapper.Instance.Selection, firstBuildAfterEffectAddition: true);
            }
        }
        
        private static void onClassNameRemoved(string className)
        {
            if (className.StartsWith(SoundEffect.CLASSNAME_ID_PREFX))
                rebuildUI(UIBuilderWindowWrapper.Instance.Selection, firstBuildAfterEffectAddition: false);
        }

        private static void onSelectionChanged(List<VisualElement> elements)
        {
            s_ignoreNextClassAddEvent = false;
            
            if (UIBuilderWindowWrapper.TimeSinceLastCompilation < 1f)
            {
                // After compilation we have to wait for the UI to be rebuilt.
                // A logic based approach would be better but this is good enough for now.
                EditorScheduler.Schedule(0.1f, () => rebuildUI(elements));
            }
            else
            {
                rebuildUI(elements);
            }
        }
        
        private static void rebuildUI(List<VisualElement> elements, bool firstBuildAfterEffectAddition = false)
        {
            EditorApplication.playModeStateChanged -= onPlayModeStateChanged;
            EditorApplication.playModeStateChanged += onPlayModeStateChanged;
            
            // Skip if multi select
            if (elements.Count > 1)
                return;

            _target = null;
            if (elements.Count > 0)
                _target = elements[0];
                
            var builder = UIBuilderWindowWrapper.Instance;
            if (builder == null)
                return;

            if (builder.RootVisualElement == null)
                return;

            var inspectorScrollView = builder.RootVisualElement.Q<ScrollView>(name: "inspector-scroll-view");
            if (inspectorScrollView == null || inspectorScrollView.contentContainer == null)
                return;
            
            var inspectorContainer = inspectorScrollView.contentContainer;

            string foldoutName = "kamgam-sound-effects";
            var foldout = inspectorContainer.Q<Foldout>(foldoutName);
            
            // Delete foldout before regenerating it.
            if (foldout != null)
            {
                foldout.UnregisterCallback<DetachFromPanelEvent>(onDetachFromPanel);
                foldout.parent.Remove(foldout);
            }

            if (_target == null)
                return;
            
            bool hasAudioEffectsClass = _target.ClassListContains(SoundEffect.CLASSNAME);
            
            // Load effects
            var effects = SoundEffects.GetOrCreate();
            
            // Do not show audio effect in inspector unless the class has been added if this option is enabled.
            if (effects.InspectorOnlyIfClass && !hasAudioEffectsClass)
                return;

            foldout = new Foldout();
            inspectorContainer.Add(foldout);
            foldout.value = _mainFoldoutState;
            foldout.RegisterCallback<DetachFromPanelEvent>(onDetachFromPanel);
            foldout.name = foldoutName;
            foldout.text = "Sound Effects";
            foldout.contentContainer.style.paddingBottom = 5;
            foldout.style.paddingBottom = 10;

            // Styles to match Unity's default styles.
            var toggle = foldout.Q(classes: "unity-toggle");
            toggle.style.paddingLeft = 4;
            toggle.style.paddingTop = 1;
            toggle.style.paddingBottom = 1;
#if UNITY_6000_0_OR_NEWER
            toggle.style.backgroundColor = EditorGUIUtility.isProSkin ? new Color(80/255f, 80/255f, 80/255f) : new Color(150/255f, 150/255f, 150/255f);
#else
            toggle.style.backgroundColor = EditorGUIUtility.isProSkin ? new Color(32/255f, 32/255f, 32/255f) : new Color(150/255f, 150/255f, 150/255f);
#endif
            toggle.style.borderTopColor = new Color(117/255f, 117/255f, 117/255f);
            toggle.style.borderTopWidth = 1;
            toggle.style.borderBottomColor = new Color(117/255f, 117/255f, 117/255f);
            toggle.style.borderBottomWidth = 1;
            
            // Get content
            var foldoutContent = foldout.Q<VisualElement>("unity-content");
            foldoutContent.style.marginLeft = 6;
            foldoutContent.style.paddingBottom = 10;

            // Use the id from the uss class. If no id class was found then try the new id.
            var id = SoundEffect.GetIdFromClassList(_target);

            // Add/Remove Buttons
            if (hasAudioEffectsClass && !string.IsNullOrEmpty(id))
            {
                var removeButton = new Button(removeSoundEffect);
                removeButton.name = "remove-sound-effect";
                removeButton.text = "Remove Sound Effect";
                foldoutContent.Add(removeButton);
            }
            else
            {
                var addButton = new Button(() => addSoundEffect(_target));
                addButton.name = "add-sound-effect";
                addButton.text = "Create New Sound Effect";
                
                var addIdField = new TextField();
                addIdField.label = "Id:";
                addIdField.tooltip = "(Optional) If set it will search for an existing effect with that id and use it.\n" +
                                     " If none is found it will create a new one with the given id.\n" +
                                     " If left empty it will generate a new id (guid).";
                addIdField.RegisterValueChangedCallback((e) =>
                {
                    _newEffectId = e.newValue.Trim().ToLower();
                    var existingEffect = effects.GetEffect(_newEffectId);
                    if (existingEffect == null)
                    {
                        addButton.text = "Create New Sound Effect";
                        addIdField.Q<VisualElement>(name:"unity-text-input").style.backgroundColor = StyleKeyword.Null;
                    }
                    else
                    {
                        addButton.text = "Add Existing Sound Effect";
                        addIdField.Q<VisualElement>(name:"unity-text-input").style.backgroundColor = new Color(0.0f, 0.3f, 0.0f);
                    }
                });

                foldoutContent.Add(addIdField);
                foldoutContent.Add(addButton);
                // Existing ID scroll view.
                if (effects.Effects.Count > 0)
                {
                    var existingIdsCtn = new Foldout();
                    existingIdsCtn.text = "Existing Ids"; 
                    existingIdsCtn.name = "existingIdScrollView";
                    // existingIdsCtn.style.marginBottom = 5;
                    foldoutContent.Add(existingIdsCtn);

                    foreach (var eff in effects.Effects)
                    {
                        var btn = new Button(() =>
                        {
                            addIdField.value = eff.Id;
                            Submit(addButton);
                        });
                        btn.text = eff.Id;
                        existingIdsCtn.Add(btn);
                    }
                }
            }
            
            // Stop after add button if no sfx class exists.
            if (!hasAudioEffectsClass)
                return;
            
            
            
            if (string.IsNullOrEmpty(id))
            {
                return;
            }
            
            // Find or create effect and draw the inspector
            var effect = effects.GetOrCreateEffect(id);
            if (effect == null)
            {
                Debug.LogWarning($"Could not create effect for id '{id}'.");
                return;
            }
            
            // increment if new
            if (firstBuildAfterEffectAddition)
                effect.UIBuilderUsedByCount++;

            // Create clips and event objects inside effect if needed
            effect.EditorInitIfNecessary();
            
            // Create inspector
            var editor = Editor.CreateEditor(effect);
            var editorRoot = editor.CreateInspectorGUI();
            if (editorRoot != null)
            {
                editorRoot.styleSheets.Add(SoundEffects.FindInspectorStyleSheet());
                editorRoot.Bind(editor.serializedObject);
                foldoutContent.Add(editorRoot);

                // Style some fields because we can not do it via USS, see: 
                // https://discussions.unity.com/t/why-does-unity-use-colons-in-class-names-how-to-style-them/1624120
                var scriptField = editorRoot.Q(name: "PropertyField:m_Script");
                if (scriptField != null) // Sometimes this is null?
                    scriptField.style.display = DisplayStyle.None;

                var idField = editorRoot.Q(name: "PropertyField:Id");
                if (idField != null)
                    idField.SetEnabled(false);
                
                // simple view of first event
                {
                    var simpleGroup = new VisualElement();

                    _event0EventTypeField = new EnumField("Event Type:", effect.Events[0].eventType);
                    _event0EventTypeField.RegisterValueChangedCallback((e) =>
                    {
                        effect.EditorInitIfNecessary();
                        effect.Events[0].eventType = (ElementEventType) e.newValue;
                        editor.serializedObject.Update();
                        // force update 
                        if (EditorApplication.isPlayingOrWillChangePlaymode)
                            forceUpdateOnManipulatorsAtRuntime();
                    });
                    simpleGroup.Add(_event0EventTypeField);

                    var clipHorizontalGroup = new VisualElement();
                    _event0Clip0Field = new ObjectField();
                    _event0Clip0Field.objectType = typeof(AudioClip);
                    _event0Clip0Field.label = "Audio Clip:";
                    effect.CreateFirstEventIfNoneExists(addNullClip: true);
                    _event0Clip0Field.value = effect.Events[0].Clips[0];
                    _event0Clip0Field.RegisterValueChangedCallback((e) =>
                    {
                        effect.EditorInitIfNecessary();
                        effect.Events[0].Clips[0] = e.newValue as AudioClip;
                        editor.serializedObject.Update();
                        // force update 
                        if (EditorApplication.isPlayingOrWillChangePlaymode)
                            forceUpdateOnManipulatorsAtRuntime();
                    });
                    _event0Clip0Field.style.flexGrow = 1f;
                    var playClipButton = new Button(() =>
                    {
                        EditorAudioUtils.PlayClip(_event0Clip0Field.value as AudioClip);
                    });
                    playClipButton.text = "▶";
                    playClipButton.style.flexGrow = 0f;
                    playClipButton.style.flexShrink = 1f;
                    clipHorizontalGroup.style.flexDirection = FlexDirection.Row;
                    clipHorizontalGroup.Add(_event0Clip0Field);
                    clipHorizontalGroup.Add(playClipButton);
                    simpleGroup.Add(clipHorizontalGroup);

                    idField.parent.Insert(idField.parent.IndexOf(idField)+1, simpleGroup);
                }
            }
            else
            {
                // Fallback for imgui (should never be used).
                var imguiContainer = new IMGUIContainer(editor.OnInspectorGUI);
                foldoutContent.Add(imguiContainer);
            }

            // We use ".eventType" a lot here to determine whether or not all the ui has been generated.

            // Some parts of the ui are generated later (on demand) and thus we register to the parent for change events.
            // https://discussions.unity.com/t/when-is-the-layout-created-after-binding-customeditor/1624594
            // But wait, change events for enums are a mess in UI Toolkit, see:
            // https://discussions.unity.com/t/uielements-developer-guide/736410/40
            // *sigh*, thanks Unity.
            foldoutContent.RegisterCallback<SerializedPropertyChangeEvent>((e) =>
            {
                // Listen for eventType changes
                if (e.changedProperty.propertyType == SerializedPropertyType.Enum
                    && e.changedProperty.propertyPath.EndsWith(".eventType"))
                {
                    var eventType = (ElementEventType)e.changedProperty.intValue;
                    
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                        forceUpdateOnManipulatorsAtRuntime();
                    
                    if(e.changedProperty.propertyPath.EndsWith("[0].eventType"))
                    {
                        // Update simple field for audio event type
                        _event0EventTypeField.value = eventType;    
                    }
                }
                
                // Listen for changes in the first clip
                if (e.changedProperty.propertyType == SerializedPropertyType.ObjectReference
                    && e.changedProperty.propertyPath.EndsWith("[0].Clips.Array.data[0]"))
                {
                    // Update simple field for audio clip
                    _event0Clip0Field.value = e.changedProperty.objectReferenceValue;
                    
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                        forceUpdateOnManipulatorsAtRuntime();
                }
            });
        }

        private static List<SoundEffectManipulator> s_tmpManipulators = new List<SoundEffectManipulator>();

        private static void forceUpdateOnManipulatorsAtRuntime()
        {
            var id = SoundEffect.GetIdFromClassList(_target);
            
            var manipulators = SoundEffectManipulator.GetManipulators(id, s_tmpManipulators);
            foreach (var manipulator in manipulators)
            {
                SoundEffectManipulator.CreateOrUpdate(manipulator.target);
            }
            s_tmpManipulators.Clear();
        }

        private static void onPlayModeStateChanged(PlayModeStateChange obj)
        {
            if (obj == PlayModeStateChange.ExitingPlayMode)
                rebuildUI(UIBuilderWindowWrapper.Instance.Selection);
        }

        private static void onDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (UIBuilderWindowWrapper.Instance == null || !UIBuilderWindowWrapper.Instance.HasWindow || UIBuilderWindowWrapper.Instance.RootVisualElement == null)
                return;
            
            // Auto rebuild UI after detach due to inspector rebuild.
            (evt.target as VisualElement)?.UnregisterCallback<DetachFromPanelEvent>(onDetachFromPanel);
            EditorScheduler.Schedule(0.1f, () => rebuildUI(UIBuilderWindowWrapper.Instance.Selection));
        }

        private static bool s_ignoreNextClassAddEvent;

        private static void addSoundEffect(VisualElement element)
        {
            // Fill the textfield
            var root = UIBuilderWindowWrapper.Instance.RootVisualElement;
            var input = root.Q(name: "add-class-controls-container").Q<TextField>(name: "add-class-field");
            var btn = root.Q(name: "add-class-button");
            
            // Fill the textfield
            input.value = SoundEffect.CLASSNAME;
            s_ignoreNextClassAddEvent = true;
            Submit(btn);
            
            // Why all the waiting? Well, it seems all these buttons actions are not executed synchronously and
            // I did not care enough to dig into the source to find the logical solution (should do it though). 
            EditorScheduler.Schedule(0.05f, () =>
            {
                // Get new or existing id.
                string id = _newEffectId;
                if (string.IsNullOrEmpty(_newEffectId))
                    id = SoundEffect.GetNewId();
                
                // Reset new id so it won't be used again.
                _newEffectId = null;
                
                input.value = SoundEffect.CLASSNAME_ID_PREFX + id;
                Submit(btn);
                
                // Rebuild UI (will trigger creation of the effect asset).
                EditorScheduler.Schedule(0.1f, () => 
                    rebuildUI(UIBuilderWindowWrapper.Instance.Selection, firstBuildAfterEffectAddition: true)
                    );
            });
        }
        
        private static void removeSoundEffect()
        {
            // Fill the textfield
            var root = UIBuilderWindowWrapper.Instance.RootVisualElement;
            
            // Trigger removing the base class
            var classElement = root.Query<Label>(name: "class-name-label")
                .Where(lbl => lbl.text == "." + SoundEffect.CLASSNAME)
                .First();
            if (classElement != null)
            {
                var btn = classElement.parent.Q<Button>();
                Submit(btn);
            }

            // Trigger removing the id class
            // Why all the waiting? Well, it seems all these buttons actions are not executed synchronously and
            // I did not care enough to dig into the source to find the logical solution (should do it though).
            EditorScheduler.Schedule(0.05f, () =>
            {
                var lbl = root.Query<Label>(name: "class-name-label")
                    .Where(lbl => lbl.text.StartsWith("." + SoundEffect.CLASSNAME_ID_PREFX))
                    .First();
                if (lbl != null)
                {
                    var btn = lbl.parent.Q<Button>();
                    Submit(btn);
                }

                string id = lbl.text.Replace("." + SoundEffect.CLASSNAME_ID_PREFX, "").Trim();
                var effect = SoundEffects.GetOrCreate().GetEffect(id);
                if (effect != null && effect.UIBuilderUsedByCount <= 1)
                {
                    effect.UIBuilderUsedByCount = 0;
                    SoundEffects.GetOrCreate().DestroyEffect(id);
                }
                else if (effect != null)
                {
                    effect.UIBuilderUsedByCount--;
                }

                // Rebuild UI
                EditorScheduler.Schedule(0.1f, () => rebuildUI(UIBuilderWindowWrapper.Instance.Selection));
            });
            
        }

        private static void Submit(VisualElement btn)
        {
            using (var e = new NavigationSubmitEvent() { target = btn })
            {
                btn.SendEvent(e);
            }
        }
    }
}
#endif