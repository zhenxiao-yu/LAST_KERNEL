#if UNITY_EDITOR
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Kamgam.UIToolkitTextAnimation
{
    [UnityEditor.CustomEditor(typeof(TextAnimation), editorForChildClasses: true)]
    public class TextAnimationEditor : UnityEditor.Editor
    {
        TextAnimation config;

        float m_time = 0f;
        float m_maxTime = 2f;
        bool m_autoPlay = false;
        double m_lastAutoPlayTime = 0f;
        private int m_lastNumOfModules;
        
        public void OnEnable()
        {
            config = target as TextAnimation;
            
            // Delay auto assign to allow file name confirmation (otherwise config.name would be "" all the time).
            EditorApplication.delayCall += () =>
            {
                // Auto assign an id
                if (config != null && string.IsNullOrEmpty(config.Id) && !string.IsNullOrEmpty(config.name))
                {
                    int lastDelimiter = config.name.LastIndexOf(" ");
                    if (lastDelimiter < 0) lastDelimiter = config.name.LastIndexOf("_");
                    if (lastDelimiter < 0) lastDelimiter = config.name.LastIndexOf("-");
                    if (lastDelimiter < 0) lastDelimiter = config.name.LastIndexOf(".");
                    if (lastDelimiter >= 0)
                    {
                        if (config is TextAnimationTypewriter)
                            config.Id = "w-" + config.name.Substring(lastDelimiter + 1).ToLower();
                        else
                            config.Id = config.name.Substring(lastDelimiter + 1).ToLower();
                        EditorUtility.SetDirty(config);
                        AssetDatabase.SaveAssetIfDirty(config);
                    }
                }
            };

            m_lastNumOfModules = config.GetModuleCount();
        }

        public static void RefreshPreview()
        {
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                UIEditorPanelObserver.ForceRefreshAndRebuild();
            }
        }

        public override void OnInspectorGUI()
        {
            // Key shortcut for refreshing.
            if (Event.current.isKey && Event.current.control && Event.current.keyCode == KeyCode.R)
            {
                UIEditorPanelObserver.ForceRefreshAndRebuild();
            }
            
            if (string.IsNullOrEmpty(config.Id))
            {
                EditorUtility.SetDirty(config);
            }

            if (GUILayout.Button("<- Back to UI Document"))
            {
                var doc = TextAnimationDocument.FindFirst(requireConfigRoot: false);
                if (doc != null)
                {
                    UnityEditor.Selection.objects = new GameObject[] { doc.gameObject };
                    UnityEditor.EditorGUIUtility.PingObject(doc.gameObject);
                }
            }
            
            if (GUILayout.Button("<- Back to Animations List"))
            {
                var list = TextAnimations.FindAssetInEditor();
                if (list != null)
                {
                    UnityEditor.Selection.objects = new Object[] { list };
                    UnityEditor.EditorGUIUtility.PingObject(list);
                }
            }

            // Check if asset is in global config list
            var configRoot = TextAnimations.FindAssetInEditor();
            if(configRoot != null)
            {
                if (!configRoot.Assets.Contains(config))
                {
                    configRoot.Assets.Add(config);
                    EditorUtility.SetDirty(configRoot);
                }
                
                configRoot.DefragAssets();
            }

            
            GUILayout.Space(10);
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Randomize Timing", "Calls the RandomizeTiming() method of the config.")))
            {
                Undo.RecordObject(config, "Randomize Timing");
                config.RandomizeTiming();
                EditorUtility.SetDirty(config);
                
                if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    UIEditorPanelObserver.ForceRefreshAndRebuild();
                }
            }
            
            if (GUILayout.Button(new GUIContent("Randomize", "Calls the Randomize() method of the config. Whether or not it is implemented depends on the creator.")))
            {
                Undo.RecordObject(config, "Randomize");
                config.Randomize();
                EditorUtility.SetDirty(config);

                RefreshPreview();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            
            // base.OnInspectorGUI(); equivalent
            {
                serializedObject.Update();

                // Draw default gui and check for updates.
                var serializedProperty = serializedObject.GetIterator();
                
                // Script
                GUI.enabled = false;
                serializedProperty.NextVisible(enterChildren: true);
                EditorGUILayout.PropertyField(serializedProperty, new GUIContent(serializedProperty.name), includeChildren: true);
                GUI.enabled = true;
                
                bool enterChildren = true;
                while (serializedProperty.NextVisible(enterChildren))
                {
                    enterChildren = false;

                    string name = serializedProperty.name;
                    if (name == "m_id") name = "Id";
                    name = Regex.Replace(name, "(?<!^)([A-Z])", " $1"); // CamelCase to spaces (like Unity does).
                    
                    EditorGUILayout.PropertyField(serializedProperty, new GUIContent(name), includeChildren: true);

                    if (serializedProperty.name == "Modules")
                    {
                        // Character modules
                        if (config is TextAnimationCharacter characterAnimation)
                        {
                            foreach (var type in ModuleTypeFinder.CharacterTypes)
                            {
                                string typeName = type.Name.Replace("TextAnimationCharacter", "").Replace("Module", "");
                                if (GUILayout.Button("+ " + typeName))
                                {
                                    var module = (ITextAnimationCharacterModule) TextAnimationModulePool.GetFromPool(type);
                                    if (module != null)
                                        characterAnimation.Modules.Add(module);
                                }
                            }
                        }
                        // Typewriter modules 
                        else if (config is TextAnimationTypewriter typewriterAnimation)
                        {
                            foreach (var type in ModuleTypeFinder.TypewriterTypes)
                            {
                                string typeName = type.Name.Replace("TextAnimationTypewriter", "").Replace("Module", "");
                                if (GUILayout.Button("+ " + typeName + " Typewriter"))
                                {
                                    var module = (ITextAnimationTypewriterModule) TextAnimationModulePool.GetFromPool(type);
                                    if (module != null)
                                        typewriterAnimation.Modules.Add(module);
                                }
                            }
                        }
                    }
                }

                if (serializedObject.hasModifiedProperties)
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }
            
            GUILayout.Space(20);
            
            GUILayout.Label("Utils:");
            GUILayout.Space(5);
            
            if (GUILayout.Button(new GUIContent("Refresh Preview", "Sometimes the preview in UI Builder or the Game View does not update automatically. Use this to force and update in the UI Builder and the Game View.")))
            {
                RefreshPreview();
            }
            EditorGUILayout.HelpBox(new GUIContent($"You can also trigger the refresh via Ctrl + R"));
            
            
            // Auto refresh
            GUI.enabled = !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode;
            
            // Timeline slider
            GUILayout.BeginHorizontal();
            GUI.enabled = !m_autoPlay;
            GUILayout.Label(new GUIContent("Time:","The current playback time of the animations."), GUILayout.Width(70));
            float prevTime = m_time;
            if (m_autoPlay)
            {
                // only display during auto play
                EditorGUILayout.Slider((float)(EditorApplication.timeSinceStartup - m_lastAutoPlayTime), 0f, m_maxTime);
            }
            else
                m_time = EditorGUILayout.Slider(m_time, 0f, m_maxTime);
            if (prevTime != m_time)
            {
                TextAnimationManipulator.RestartAllManipulators(m_time, paused: m_time > 0f);
                EditorApplication.QueuePlayerLoopUpdate();
            }
            GUI.enabled = true;
            m_maxTime = EditorGUILayout.FloatField(m_maxTime, GUILayout.Width(30));
            GUI.enabled = !m_autoPlay;
            if (GUILayout.Button(new GUIContent("Play", "If you tick the box on the right it will auto-play every # seconds."), GUILayout.Width(60)))
            {
                TextAnimationManipulator.RestartAllManipulators(m_time, paused: false);
                EditorApplication.QueuePlayerLoopUpdate();
            }
            GUI.enabled = true;
            m_autoPlay = EditorGUILayout.Toggle(m_autoPlay, GUILayout.Width(20));
            if (m_autoPlay && EditorApplication.timeSinceStartup - m_lastAutoPlayTime > m_maxTime)
            {
                m_lastAutoPlayTime = EditorApplication.timeSinceStartup;
                UIEditorPanelObserver.ForceRefreshAndRebuild();
            }
            if (m_autoPlay)
            {
                EditorApplication.delayCall += Repaint;
            }
            GUILayout.EndHorizontal();
            GUI.enabled = true;
            

            GUILayout.Space(10);
            if (config is TextAnimationTypewriter)
            {
                string typewriterClassName = TextAnimationManipulator.TEXT_TYPEWRITER_CLASSNAME + config.Id;
                
                GUILayout.Label("Classname(s):");
                GUILayout.BeginHorizontal();
                GUILayout.TextField(TextAnimationManipulator.TEXT_ANIMATION_CLASSNAME);
                if (GUILayout.Button("Copy", GUILayout.Width(70)))
                {
                    EditorGUIUtility.systemCopyBuffer = TextAnimationManipulator.TEXT_ANIMATION_CLASSNAME;
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.TextField(typewriterClassName);
                if (GUILayout.Button("Copy", GUILayout.Width(70)))
                {
                    EditorGUIUtility.systemCopyBuffer = typewriterClassName;
                }
                GUILayout.EndHorizontal();
                EditorGUILayout.HelpBox(new GUIContent($"Add '{TextAnimationManipulator.TEXT_ANIMATION_CLASSNAME}' and '{typewriterClassName}' to your TextElement class list to use this typewriter animation."));
            }
            else
            {
                GUILayout.Label("Classname:");
                GUILayout.BeginHorizontal();
                GUILayout.TextField(TextAnimationManipulator.TEXT_ANIMATION_CLASSNAME);
                if (GUILayout.Button("Copy", GUILayout.Width(70)))
                {
                    EditorGUIUtility.systemCopyBuffer = TextAnimationManipulator.TEXT_ANIMATION_CLASSNAME;
                }
                GUILayout.EndHorizontal();
                
                GUILayout.Label("Tag:");
                GUILayout.BeginHorizontal();
                string linkSample = $"<link anim=\"{config.Id}\">text</link>";
                GUILayout.TextField(linkSample);
                if (GUILayout.Button("Copy", GUILayout.Width(70)))
                {
                    EditorGUIUtility.systemCopyBuffer = linkSample;
                }
                GUILayout.EndHorizontal();
                EditorGUILayout.HelpBox(new GUIContent($"Add '{TextAnimationManipulator.TEXT_ANIMATION_CLASSNAME}' your TextElement class list and then use the tag '<link anim=\"{config.Id}\">animated text</link>' to use this animation in your text."));
                
            }

            if (config.GetModuleCount() != m_lastNumOfModules)
            {
                m_lastNumOfModules = config.GetModuleCount();
                RefreshPreview();
            }
        }

    }
}
#endif
