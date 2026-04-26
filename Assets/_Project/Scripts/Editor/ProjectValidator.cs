using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;

namespace Markyu.LastKernel
{
    public static class ProjectValidator
    {
        private const string PrefabsRoot = "Assets/_Project/Prefabs";
        private const string ProjectScriptsRoot = "Assets/_Project/Scripts";
        private const string BootScenePath = "Assets/_Project/Scenes/Boot.unity";

        private static readonly string[] AllowedBootRootNames =
        {
            "GameBootstrap",
            "AudioManager",
            "InputManager",
            "Localization",
            "SaveSystem"
        };

        private static readonly string[] ProjectNamespacePrefixes =
        {
            "Markyu.LastKernel"
        };

        [MenuItem("Tools/Validate Project")]
        public static void ValidateProject()
        {
            var summary = new ValidationSummary();

            Debug.Log("ProjectValidator: Starting project validation.");

            ValidateCardSettings(summary);
            ValidateCardDefinitions(summary);
            ValidateLocalizationKeys(summary);
            ValidatePrefabReferences(summary);
            ValidateCodeOwnership(summary);
            ValidateBootScene(summary);

            Debug.Log(
                $"ProjectValidator: Validation complete. Errors: {summary.ErrorCount}, Warnings: {summary.WarningCount}.");
        }

        private static void ValidateCardSettings(ValidationSummary summary)
        {
            foreach (CardSettings settings in LoadAssets<CardSettings>())
            {
                var serializedObject = new SerializedObject(settings);
                SerializedProperty feelProfile = serializedObject.FindProperty("feelProfile");
                if (feelProfile == null || feelProfile.objectReferenceValue == null)
                {
                    summary.Error($"CardSettings '{AssetDatabase.GetAssetPath(settings)}' is missing a CardFeelProfile.");
                }
            }
        }

        private static void ValidateCardDefinitions(ValidationSummary summary)
        {
            foreach (CardDefinition definition in LoadAssets<CardDefinition>())
            {
                string path = AssetDatabase.GetAssetPath(definition);
                var serializedObject = new SerializedObject(definition);

                ValidateNonEmptyString(serializedObject, "displayName", $"CardDefinition '{path}' is missing DisplayName.", summary);
                ValidateNonEmptyString(serializedObject, "description", $"CardDefinition '{path}' is missing Description.", summary);

                SerializedProperty artTexture = serializedObject.FindProperty("artTexture");
                if (artTexture == null || artTexture.objectReferenceValue == null)
                {
                    summary.Error($"CardDefinition '{path}' is missing Art.");
                }
            }
        }

        private static void ValidateLocalizationKeys(ValidationSummary summary)
        {
            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(UnityLocalizationBridge.DefaultStringTable);
            if (collection == null || collection.SharedData == null)
            {
                summary.Error($"Localization table '{UnityLocalizationBridge.DefaultStringTable}' was not found.");
                return;
            }

            ValidateLocalizationKeySet(
                LoadAssets<CardDefinition>().Where(asset => asset is not PackDefinition),
                "card",
                new[] { "name", "description" },
                collection,
                summary);

            ValidateLocalizationKeySet(
                LoadAssets<RecipeDefinition>(),
                "recipe",
                new[] { "name" },
                collection,
                summary);

            ValidateLocalizationKeySet(
                LoadAssets<Quest>(),
                "quest",
                new[] { "title", "description" },
                collection,
                summary);

            ValidateLocalizationKeySet(
                LoadAssets<PackDefinition>(),
                "pack",
                new[] { "name", "description" },
                collection,
                summary);
        }

        private static void ValidateLocalizationKeySet<T>(
            IEnumerable<T> assets,
            string category,
            IEnumerable<string> fields,
            StringTableCollection collection,
            ValidationSummary summary)
            where T : UnityEngine.Object
        {
            foreach (T asset in assets)
            {
                string path = AssetDatabase.GetAssetPath(asset);
                foreach (string field in fields)
                {
                    string key = LocalizationKeyBuilder.ForAsset(asset, category, field);
                    if (collection.SharedData.GetEntry(key) == null)
                    {
                        summary.Error($"Missing localization key '{key}' for asset '{path}'.");
                    }
                }
            }
        }

        private static void ValidatePrefabReferences(ValidationSummary summary)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabsRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = null;

                try
                {
                    root = PrefabUtility.LoadPrefabContents(path);
                    ValidateMissingScripts(root, path, summary);
                    ValidateMissingObjectReferences(root, path, summary);
                }
                catch (Exception exception)
                {
                    summary.Error($"Failed to inspect prefab '{path}'. {exception.Message}");
                }
                finally
                {
                    if (root != null)
                    {
                        PrefabUtility.UnloadPrefabContents(root);
                    }
                }
            }
        }

        private static void ValidateMissingScripts(GameObject root, string path, ValidationSummary summary)
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
                if (missingCount > 0)
                {
                    summary.Error(
                        $"Prefab '{path}' has {missingCount} missing script reference(s) on '{GetHierarchyPath(transform)}'.");
                }
            }
        }

        private static void ValidateMissingObjectReferences(GameObject root, string path, ValidationSummary summary)
        {
            foreach (Component component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null)
                {
                    continue;
                }

                var serializedObject = new SerializedObject(component);
                SerializedProperty iterator = serializedObject.GetIterator();
                bool enterChildren = true;

                while (iterator.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (iterator.propertyType != SerializedPropertyType.ObjectReference)
                    {
                        continue;
                    }

                    if (iterator.objectReferenceValue == null && iterator.objectReferenceInstanceIDValue != 0)
                    {
                        summary.Error(
                            $"Prefab '{path}' has a missing object reference on '{component.GetType().Name}.{iterator.propertyPath}' " +
                            $"at '{GetHierarchyPath(component.transform)}'.");
                    }
                }
            }
        }

        private static void ValidateCodeOwnership(ValidationSummary summary)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid).Replace('\\', '/');
                if (path.StartsWith(ProjectScriptsRoot, StringComparison.Ordinal))
                {
                    ValidateProjectOwnedScript(path, summary);
                    continue;
                }

                if (path.StartsWith("Assets/ThirdParty/", StringComparison.Ordinal))
                {
                    continue;
                }

                if (path.StartsWith("Assets/_Project/", StringComparison.Ordinal))
                {
                    summary.Warning($"Potential third-party script inside _Project: '{path}'.");
                    continue;
                }

                if (path.StartsWith("Assets/", StringComparison.Ordinal))
                {
                    summary.Warning($"Project gameplay script outside '{ProjectScriptsRoot}': '{path}'.");
                }
            }
        }

        private static void ValidateProjectOwnedScript(string path, ValidationSummary summary)
        {
            string contents;
            try
            {
                contents = File.ReadAllText(path);
            }
            catch (Exception exception)
            {
                summary.Warning($"Unable to inspect script '{path}'. {exception.Message}");
                return;
            }

            string namespaceName = ExtractNamespace(contents);
            if (string.IsNullOrWhiteSpace(namespaceName))
            {
                return;
            }

            if (!ProjectNamespacePrefixes.Any(prefix => namespaceName.StartsWith(prefix, StringComparison.Ordinal)))
            {
                summary.Warning($"Potential third-party script inside _Project: '{path}' declares namespace '{namespaceName}'.");
            }
        }

        private static void ValidateBootScene(ValidationSummary summary)
        {
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            Scene bootScene = default;

            try
            {
                bootScene = EditorSceneManager.OpenScene(BootScenePath, OpenSceneMode.Additive);
                foreach (GameObject rootObject in bootScene.GetRootGameObjects())
                {
                    if (!AllowedBootRootNames.Contains(rootObject.name, StringComparer.Ordinal))
                    {
                        summary.Warning($"Boot scene contains unexpected root object '{rootObject.name}'.");
                    }
                }
            }
            catch (Exception exception)
            {
                summary.Error($"Failed to validate Boot scene '{BootScenePath}'. {exception.Message}");
            }
            finally
            {
                if (bootScene.IsValid())
                {
                    EditorSceneManager.CloseScene(bootScene, true);
                }

                if (previousSetup != null && previousSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                }
            }
        }

        private static IEnumerable<T> LoadAssets<T>() where T : UnityEngine.Object
        {
            foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { "Assets/_Project" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                {
                    yield return asset;
                }
            }
        }

        private static void ValidateNonEmptyString(
            SerializedObject serializedObject,
            string fieldName,
            string message,
            ValidationSummary summary)
        {
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            if (property == null || string.IsNullOrWhiteSpace(property.stringValue))
            {
                summary.Error(message);
            }
        }

        private static string GetHierarchyPath(Transform transform)
        {
            if (transform == null)
            {
                return "<null>";
            }

            var names = new Stack<string>();
            Transform current = transform;
            while (current != null)
            {
                names.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", names);
        }

        private static string ExtractNamespace(string contents)
        {
            const string marker = "namespace ";
            int index = contents.IndexOf(marker, StringComparison.Ordinal);
            if (index < 0)
            {
                return string.Empty;
            }

            int start = index + marker.Length;
            int end = start;
            while (end < contents.Length)
            {
                char character = contents[end];
                if (!(char.IsLetterOrDigit(character) || character == '_' || character == '.'))
                {
                    break;
                }

                end++;
            }

            return contents.Substring(start, end - start).Trim();
        }

        private sealed class ValidationSummary
        {
            public int ErrorCount { get; private set; }
            public int WarningCount { get; private set; }

            public void Error(string message)
            {
                ErrorCount++;
                Debug.LogError($"ProjectValidator: {message}");
            }

            public void Warning(string message)
            {
                WarningCount++;
                Debug.LogWarning($"ProjectValidator: {message}");
            }
        }
    }
}
