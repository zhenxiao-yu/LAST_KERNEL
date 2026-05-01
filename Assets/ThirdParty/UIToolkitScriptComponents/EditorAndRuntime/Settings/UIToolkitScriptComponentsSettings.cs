#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Kamgam.UIToolkitScriptComponents
{
    // Create a new type of Settings Asset.
    public class UIToolkitScriptComponentsSettings : ScriptableObject
    {
        public const string Version = "1.3.0";
        public const string SettingsFilePath = "Assets/UIToolkitScriptComponentsSettings.asset";

        [SerializeField, Tooltip(_logLevelTooltip)]
        public Logger.LogLevel LogLevel = Logger.LogLevel.Log;
        public const string _logLevelTooltip = "Any log above this log level will not be shown. To turn off all logs choose 'NoLogs'";

        [SerializeField, Tooltip(_syncSelection)]
        public bool SyncSelection = true;
        public const string _syncSelection = "Should the selection between the Hierarchy and the UI Builder be synced autoamtically?";

        [SerializeField, Tooltip(_syncInPlayMode)]
        public bool SyncInPlayMode = false;
        public const string _syncInPlayMode = "If syncing is ON then also synv while in play mode? Off by default.";

        [RuntimeInitializeOnLoadMethod]
        static void bindLoggerLevelToSetting()
        {
            // Notice: This does not yet create a setting instance!
            Logger.OnGetLogLevel = () => GetOrCreateSettings().LogLevel;
        }

        [InitializeOnLoadMethod]
        static void autoCreateSettings()
        {
            GetOrCreateSettings();
        }

        static UIToolkitScriptComponentsSettings cachedSettings;

        public static UIToolkitScriptComponentsSettings GetOrCreateSettings()
        {
            if (cachedSettings == null)
            {
                string typeName = typeof(UIToolkitScriptComponentsSettings).Name;

                cachedSettings = AssetDatabase.LoadAssetAtPath<UIToolkitScriptComponentsSettings>(SettingsFilePath);

                // Still not found? Then search for it.
                if (cachedSettings == null)
                {
                    string[] results = AssetDatabase.FindAssets("t:" + typeName);
                    if (results.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(results[0]);
                        cachedSettings = AssetDatabase.LoadAssetAtPath<UIToolkitScriptComponentsSettings>(path);
                        cachedSettings.SyncSelection = true;
                        cachedSettings.SyncInPlayMode = false;
                        cachedSettings.LogLevel = Logger.LogLevel.Log;
                    }
                }

                if (cachedSettings != null)
                {
                    SessionState.EraseBool(typeName + "WaitingForReload");
                }

                // Still not found? Then create settings.
                if (cachedSettings == null)
                {
                    CompilationPipeline.compilationStarted -= onCompilationStarted;
                    CompilationPipeline.compilationStarted += onCompilationStarted;

                    // Are the settings waiting for a recompile to finish? If yes then return null;
                    // This is important if an external script tries to access the settings before they
                    // are deserialized after a re-compile.
                    bool isWaitingForReloadAfterCompilation = SessionState.GetBool(typeName + "WaitingForReload", false);
                    if (isWaitingForReloadAfterCompilation)
                    {
                        Debug.LogWarning(typeName + " is waiting for assembly reload.");
                        return null;
                    }

                    cachedSettings = ScriptableObject.CreateInstance<UIToolkitScriptComponentsSettings>();
                    cachedSettings.LogLevel = Logger.LogLevel.Log;

                    AssetDatabase.CreateAsset(cachedSettings, SettingsFilePath);
                    AssetDatabase.SaveAssets();

                    // Import packages and then show welcome screen.
                    // None to import for this asset.
                    // PackageImporter.ImportDelayed(onSettingsCreated); 
                    onSettingsCreated();
                }
            }

            Menu.SetChecked(_syncSelectionMenu, cachedSettings.SyncSelection);

            return cachedSettings;
        }

        private static void onCompilationStarted(object obj)
        {
            string typeName = typeof(UIToolkitScriptComponentsSettings).Name;
            SessionState.SetBool(typeName + "WaitingForReload", true);
        }

        // We use this callback instead of CompilationPipeline.compilationFinished because
        // compilationFinished runs before the assemply has been reloaded but DidReloadScripts
        // runs after. And only after we can access the Settings asset.
        [UnityEditor.Callbacks.DidReloadScripts(999000)]
        public static void DidReloadScripts()
        {
            string typeName = typeof(UIToolkitScriptComponentsSettings).Name;
            SessionState.EraseBool(typeName + "WaitingForReload");
        }

        static void onSettingsCreated()
        {
            bool openExample = EditorUtility.DisplayDialog(
                    "UI Toolkit Script Components",
                    "Thank you for choosing UI Toolkit Script Components.\n\n" +
                    "Please start by reading the manual.\n\n" +
                    "If you can find the time I would appreciate your feedback in the form of a review.\n\n" +
                    "I have prepared some examples for you.",
                    "Open Example", "Open manual (web)"
                    );

            // To avoid  "Calling OpenScene from assembly reloading callbacks are not supported." errors.
            EditorApplication.delayCall += () =>
            {
                if (openExample)
                    OpenExample();
                else
                    OpenManual();
            };
        }

        const string _syncSelectionMenu = "Tools/UI Toolkit Script Components/Sync Selection";
        [MenuItem(_syncSelectionMenu, priority = 101)]
        public static void ToggleSync()
        {
            var settings = UIToolkitScriptComponentsSettings.GetOrCreateSettings();
            if (settings != null)
            {
                settings.SyncSelection = !settings.SyncSelection;
                Menu.SetChecked(_syncSelectionMenu, settings.SyncSelection);
            }
        }

        [MenuItem("Tools/UI Toolkit Script Components/Settings", priority = 200)]
        public static void OpenSettings()
        {
            var settings = UIToolkitScriptComponentsSettings.GetOrCreateSettings();
            if (settings != null)
            {
                Selection.activeObject = settings;
                EditorGUIUtility.PingObject(settings);
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "UI Toolkit Script Components Settings could not be found or created.", "Ok");
            }
        }

        [MenuItem("Tools/UI Toolkit Script Components/Manual", priority = 210)]
        public static void OpenManual()
        {
            Application.OpenURL("https://kamgam.com/unity/UIToolkitScriptComponentsManual.pdf");
        }

        [MenuItem("Tools/UI Toolkit Script Components/Open Example Scene", priority = 211)]
        public static void OpenExample()
        {
            string path = "Assets/UIToolkitScriptComponents/Examples/UIToolkitScriptComponentsDemo.unity";
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            EditorGUIUtility.PingObject(scene);
            EditorSceneManager.OpenScene(path);
        }

        [MenuItem("Tools/UI Toolkit Script Components/Please leave a review :-)", priority = 310)]
        public static void LeaveReview()
        {
            Application.OpenURL("https://assetstore.unity.com/packages/slug/248976");
        }

        [MenuItem("Tools/UI Toolkit Script Components/More Asset by KAMGAM", priority = 311)]
        public static void MoreAssets()
        {
            Application.OpenURL("https://kamgam.com/unity");
        }

        [MenuItem("Tools/UI Toolkit Script Components/Version " + Version, priority = 312)]
        public static void LogVersion()
        {
            Debug.Log("UI Toolkit Script Comonents Version " + Version + ", Unity: " + Application.unityVersion);
        }

        internal static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(GetOrCreateSettings());
        }

        public void Save()
        {
            EditorUtility.SetDirty(this);
#if UNITY_2021_1_OR_NEWER
            AssetDatabase.SaveAssetIfDirty(this);
#else
            AssetDatabase.SaveAssets();
#endif
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(UIToolkitScriptComponentsSettings))]
    public class UIToolkitScriptComponentsSettingsEditor : Editor
    {
        public UIToolkitScriptComponentsSettings settings;

        public void OnEnable()
        {
            settings = target as UIToolkitScriptComponentsSettings;
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Version: " + UIToolkitScriptComponentsSettings.Version);
            base.OnInspectorGUI();
        }
    }
#endif

    static class UIToolkitScriptComponentsSettingsProvider
    {
        [SettingsProvider]
        public static UnityEditor.SettingsProvider CreateUIToolkitScriptComponentsSettingsProvider()
        {
            var provider = new UnityEditor.SettingsProvider("Project/UI Toolkit Script Components", SettingsScope.Project)
            {
                label = "UI Toolkit Script Components",
                guiHandler = (searchContext) =>
                {
                    var settings = UIToolkitScriptComponentsSettings.GetSerializedSettings();

                    var style = new GUIStyle(GUI.skin.label);
                    style.wordWrap = true;

                    EditorGUILayout.LabelField("Version: " + UIToolkitScriptComponentsSettings.Version);
                    drawField("LogLevel", "Log Level", UIToolkitScriptComponentsSettings._logLevelTooltip, settings, style);
                    drawField("SyncSelection", "Sync Selection", UIToolkitScriptComponentsSettings._syncSelection, settings, style);
                    drawField("SyncInPlayMode", "Sync in Play Mode", UIToolkitScriptComponentsSettings._syncInPlayMode, settings, style);

                    if (drawButton(" Open Manual ", icon: "_Help"))
                    {
                        UIToolkitScriptComponentsSettings.OpenManual();
                    }

                    settings.ApplyModifiedProperties();
                },

                // Populate the search keywords to enable smart search filtering and label highlighting.
                keywords = new System.Collections.Generic.HashSet<string>(new[] { "settings", "options", "ui", "gui", "ugui", "generator", "creator", "uss", "unified", "controls" })
            };

            return provider;
        }

        static void drawField(string propertyName, string label, string tooltip, SerializedObject settings, GUIStyle style)
        {
            EditorGUILayout.PropertyField(settings.FindProperty(propertyName), new GUIContent(label));
            if (!string.IsNullOrEmpty(tooltip))
            {
                GUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label(tooltip, style);
                GUILayout.EndVertical();
            }
            GUILayout.Space(10);
        }

        static bool drawButton(string text, string tooltip = null, string icon = null, params GUILayoutOption[] options)
        {
            GUIContent content;

            // icon
            if (!string.IsNullOrEmpty(icon))
                content = EditorGUIUtility.IconContent(icon);
            else
                content = new GUIContent();

            // text
            content.text = text;

            // tooltip
            if (!string.IsNullOrEmpty(tooltip))
                content.tooltip = tooltip;

            return GUILayout.Button(content, options);
        }
    }
}
#endif