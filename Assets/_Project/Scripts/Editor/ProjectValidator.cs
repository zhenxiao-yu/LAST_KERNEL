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
        private const string SandboxScenesRoot = "Assets/_Project/Scenes/Test";
        private const string AudioManagerPrefabPath = "Assets/_Project/Prefabs/Systems/Core/AudioManager.prefab";

        private static readonly string[] RequiredBuildScenes =
        {
            "Assets/_Project/Scenes/Boot.unity",
            "Assets/_Project/Scenes/MainMenu.unity",
            "Assets/_Project/Scenes/Game.unity",
        };

        private static readonly string[] AllowedBootRootNames =
        {
            "GameBootstrap", "AudioManager", "InputManager", "Localization", "SaveSystem"
        };

        private static readonly string[] ProjectNamespacePrefixes = { "Markyu.LastKernel" };

        // ─── Entry Points ─────────────────────────────────────────────────────

        [MenuItem("LAST KERNEL/Validate/Full Validate", false, 1)]
        public static void ValidateProject()
        {
            var summary = new ValidationSummary();
            Debug.Log("=== ProjectValidator: Starting full project validation ===");

            Section("Cards");
            ValidateCardSettings(summary);
            ValidateCardDefinitions(summary);

            Section("Recipes");
            ValidateRecipes(summary);

            Section("Packs");
            ValidatePacks(summary);

            Section("Quests");
            ValidateQuests(summary);

            Section("Prefabs");
            ValidatePrefabReferences(summary);

            Section("Scenes");
            ValidateScenes(summary);

            Section("Localization");
            ValidateLocalizationKeys(summary);

            Section("Audio");
            ValidateAudio(summary);

            Section("Odin Migration");
            ValidateOdinMigration(summary);

            Section("Code Ownership");
            ValidateCodeOwnership(summary);

            Debug.Log($"=== ProjectValidator: Done — Errors: {summary.ErrorCount}, Warnings: {summary.WarningCount} ===");
        }

        [MenuItem("LAST KERNEL/Validate/Game Data", false, 50)]
        public static void ValidateAllGameData()
        {
            var summary = new ValidationSummary();
            Debug.Log("=== ProjectValidator: Validating all game data ===");
            Section("Cards");    ValidateCardSettings(summary); ValidateCardDefinitions(summary);
            Section("Recipes");  ValidateRecipes(summary);
            Section("Packs");    ValidatePacks(summary);
            Section("Quests");   ValidateQuests(summary);
            Section("Audio");    ValidateAudio(summary);
            Debug.Log($"=== Done — Errors: {summary.ErrorCount}, Warnings: {summary.WarningCount} ===");
        }

        [MenuItem("LAST KERNEL/Validate/Duplicate IDs", false, 51)]
        public static void DetectDuplicateIds()
        {
            var summary = new ValidationSummary();
            Debug.Log("=== ProjectValidator: Detecting duplicate IDs ===");
            DetectDuplicateCardIds(summary);
            DetectDuplicateRecipeIds(summary);
            DetectDuplicateQuestIds(summary);
            Debug.Log($"=== Done — Errors: {summary.ErrorCount}, Warnings: {summary.WarningCount} ===");
        }

        [MenuItem("LAST KERNEL/Validate/Missing References", false, 52)]
        public static void FindMissingReferences()
        {
            var summary = new ValidationSummary();
            Debug.Log("=== ProjectValidator: Scanning for missing references in prefabs ===");
            Section("Prefabs");
            ValidatePrefabReferences(summary);
            Debug.Log($"=== Done — Errors: {summary.ErrorCount}, Warnings: {summary.WarningCount} ===");
        }

        [MenuItem("LAST KERNEL/Validate/Card Prefabs", false, 53)]
        public static void ValidateCardPrefabsOnly()
        {
            var summary = new ValidationSummary();
            Debug.Log("=== ProjectValidator: Validating card prefabs ===");
            Section("Card Prefabs");
            ValidatePrefabReferences(summary);
            Debug.Log($"=== Done — Errors: {summary.ErrorCount}, Warnings: {summary.WarningCount} ===");
        }

        [MenuItem("LAST KERNEL/Validate/Recipes", false, 54)]
        public static void ValidateRecipesOnly()
        {
            var summary = new ValidationSummary();
            Debug.Log("=== ProjectValidator: Validating recipes ===");
            ValidateRecipes(summary);
            Debug.Log($"=== Done — Errors: {summary.ErrorCount}, Warnings: {summary.WarningCount} ===");
        }

        [MenuItem("LAST KERNEL/Validate/Addressable Strings", false, 100)]
        public static void ValidateLocalizationKeysOnly()
        {
            var summary = new ValidationSummary();
            Debug.Log("=== ProjectValidator: Validating localization keys ===");
            ValidateLocalizationKeys(summary);
            Debug.Log($"=== Done — Errors: {summary.ErrorCount}, Warnings: {summary.WarningCount} ===");
        }

        [MenuItem("LAST KERNEL/Validate/Audio IDs", false, 55)]
        public static void ValidateAudioIdsOnly()
        {
            var summary = new ValidationSummary();
            Debug.Log("=== ProjectValidator: Validating audio IDs ===");
            ValidateAudio(summary);
            Debug.Log($"=== Done — Errors: {summary.ErrorCount}, Warnings: {summary.WarningCount} ===");
        }

        [MenuItem("LAST KERNEL/Validate/Odin Migration", false, 56)]
        public static void AuditOdinMigrationOnly()
        {
            var summary = new ValidationSummary();
            Debug.Log("=== ProjectValidator: Auditing Odin Inspector migration ===");
            ValidateOdinMigration(summary);
            Debug.Log($"=== Done — Errors: {summary.ErrorCount}, Warnings: {summary.WarningCount} ===");
        }

        [MenuItem("LAST KERNEL/Data/Open Game Database", false, 1)]
        public static void OpenGameDatabase()
        {
            string[] guids = AssetDatabase.FindAssets("t:GameDatabase", new[] { "Assets/_Project" });
            if (guids.Length == 0)
            {
                Debug.LogWarning("[Validator] No GameDatabase asset found. Create one via Right-click > Last Kernel > Game Database.");
                return;
            }
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            UnityEditor.Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private static void Section(string name) =>
            Debug.Log($"--- [Validator] {name} ---");

        // ─── Targeted ID checks (used by DetectDuplicateIds) ─────────────────

        private static void DetectDuplicateCardIds(ValidationSummary summary)
        {
            var seen = new Dictionary<string, string>();
            foreach (CardDefinition def in LoadAssets<CardDefinition>())
            {
                string path = AssetDatabase.GetAssetPath(def);
                var so = new SerializedObject(def);
                string id = so.FindProperty("id")?.stringValue ?? string.Empty;
                if (!string.IsNullOrEmpty(id))
                {
                    if (seen.TryGetValue(id, out string other))
                        summary.Error($"Duplicate card ID '{id}' at '{path}' and '{other}'.");
                    else
                        seen[id] = path;
                }
            }
        }

        private static void DetectDuplicateRecipeIds(ValidationSummary summary)
        {
            var seen = new Dictionary<string, string>();
            foreach (RecipeDefinition recipe in LoadAssets<RecipeDefinition>())
            {
                string path = AssetDatabase.GetAssetPath(recipe);
                var so = new SerializedObject(recipe);
                string id = so.FindProperty("id")?.stringValue ?? string.Empty;
                if (!string.IsNullOrEmpty(id))
                {
                    if (seen.TryGetValue(id, out string other))
                        summary.Error($"Duplicate recipe ID '{id}' at '{path}' and '{other}'.");
                    else
                        seen[id] = path;
                }
            }
        }

        private static void DetectDuplicateQuestIds(ValidationSummary summary)
        {
            var seen = new Dictionary<string, string>();
            foreach (Quest quest in LoadAssets<Quest>())
            {
                string path = AssetDatabase.GetAssetPath(quest);
                var so = new SerializedObject(quest);
                string id = so.FindProperty("id")?.stringValue ?? string.Empty;
                if (!string.IsNullOrEmpty(id))
                {
                    if (seen.TryGetValue(id, out string other))
                        summary.Error($"Duplicate quest ID '{id}' at '{path}' and '{other}'.");
                    else
                        seen[id] = path;
                }
            }
        }

        // ─── Cards ───────────────────────────────────────────────────────────

        private static void ValidateCardSettings(ValidationSummary summary)
        {
            foreach (CardSettings settings in LoadAssets<CardSettings>())
            {
                string path = AssetDatabase.GetAssetPath(settings);
                var so = new SerializedObject(settings);
                SerializedProperty feelProfile = so.FindProperty("feelProfile");
                if (feelProfile == null || feelProfile.objectReferenceValue == null)
                    summary.Error($"CardSettings '{path}' is missing a CardFeelProfile.");
            }
        }

        private static void ValidateCardDefinitions(ValidationSummary summary)
        {
            var seenIds = new Dictionary<string, string>();

            foreach (CardDefinition def in LoadAssets<CardDefinition>())
            {
                string path = AssetDatabase.GetAssetPath(def);
                var so = new SerializedObject(def);

                // ID uniqueness
                string id = so.FindProperty("id")?.stringValue ?? string.Empty;
                if (string.IsNullOrEmpty(id))
                {
                    summary.Error($"CardDefinition '{path}' has no ID (OnValidate may not have run).");
                }
                else if (seenIds.TryGetValue(id, out string other))
                {
                    summary.Error($"Duplicate card ID '{id}' at '{path}' and '{other}'.");
                }
                else
                {
                    seenIds[id] = path;
                }

                // Required text fields
                CheckNonEmpty(so, "displayName", $"CardDefinition '{path}' missing DisplayName.", summary);
                CheckNonEmpty(so, "description", $"CardDefinition '{path}' missing Description.", summary);

                // Art
                SerializedProperty art = so.FindProperty("artTexture");
                if (art == null || art.objectReferenceValue == null)
                    summary.Warning($"CardDefinition '{path}' has no art texture.");

                // Category — index 0 is None
                SerializedProperty cat = so.FindProperty("category");
                if (cat != null && cat.enumValueIndex == 0)
                    summary.Warning($"CardDefinition '{path}' has category None.");

                // Sell price when sellable
                SerializedProperty isSellable = so.FindProperty("isSellable");
                SerializedProperty sellPrice = so.FindProperty("sellPrice");
                if (isSellable != null && isSellable.boolValue && sellPrice != null && sellPrice.intValue <= 0)
                    summary.Warning($"CardDefinition '{path}' is sellable but sellPrice <= 0.");

                // Combat stats when card has a combat type
                SerializedProperty combatType = so.FindProperty("combatType");
                if (combatType != null && combatType.enumValueIndex != 0)
                {
                    SerializedProperty maxHealth = so.FindProperty("maxHealth");
                    if (maxHealth != null && maxHealth.intValue <= 0)
                        summary.Error($"CardDefinition '{path}' has CombatType but maxHealth <= 0.");
                }
            }
        }

        // ─── Recipes ─────────────────────────────────────────────────────────

        private static void ValidateRecipes(ValidationSummary summary)
        {
            var seenIds = new Dictionary<string, string>();

            foreach (RecipeDefinition recipe in LoadAssets<RecipeDefinition>())
            {
                string path = AssetDatabase.GetAssetPath(recipe);
                var so = new SerializedObject(recipe);

                // ID uniqueness
                string id = so.FindProperty("id")?.stringValue ?? string.Empty;
                if (string.IsNullOrEmpty(id))
                    summary.Error($"RecipeDefinition '{path}' has no ID.");
                else if (seenIds.TryGetValue(id, out string other))
                    summary.Error($"Duplicate recipe ID '{id}' at '{path}' and '{other}'.");
                else
                    seenIds[id] = path;

                // Inputs
                SerializedProperty inputs = so.FindProperty("requiredIngredients");
                if (inputs == null || inputs.arraySize == 0)
                {
                    summary.Error($"RecipeDefinition '{path}' has no required ingredients.");
                }
                else
                {
                    for (int i = 0; i < inputs.arraySize; i++)
                    {
                        SerializedProperty cardRef = inputs.GetArrayElementAtIndex(i).FindPropertyRelative("card");
                        if (cardRef == null || cardRef.objectReferenceValue == null)
                            summary.Error($"RecipeDefinition '{path}' ingredient [{i}] is null.");
                    }
                }

                // Output — TravelRecipe, ResearchRecipe, and ExplorationRecipe resolve their
                // output through custom Execute() logic, not from resultingCard.
                SerializedProperty output = so.FindProperty("resultingCard");
                if ((output == null || output.objectReferenceValue == null)
                    && recipe is not TravelRecipe
                    && recipe is not ResearchRecipe
                    && recipe is not ExplorationRecipe)
                    summary.Error($"RecipeDefinition '{path}' has no resulting card (output).");

                // Duration
                SerializedProperty duration = so.FindProperty("craftingDuration");
                if (duration != null && duration.floatValue <= 0f)
                    summary.Error($"RecipeDefinition '{path}' craftingDuration <= 0.");

                // Display name
                CheckNonEmpty(so, "displayName", $"RecipeDefinition '{path}' missing displayName.", summary, warn: true);
            }
        }

        // ─── Packs ───────────────────────────────────────────────────────────

        private static void ValidatePacks(ValidationSummary summary)
        {
            foreach (PackDefinition pack in LoadAssets<PackDefinition>())
            {
                string path = AssetDatabase.GetAssetPath(pack);
                var so = new SerializedObject(pack);

                SerializedProperty slots = so.FindProperty("slots");
                if (slots == null || slots.arraySize == 0)
                {
                    summary.Error($"PackDefinition '{path}' has no slots (empty pack).");
                }
                else
                {
                    for (int i = 0; i < slots.arraySize; i++)
                    {
                        SerializedProperty card = slots.GetArrayElementAtIndex(i).FindPropertyRelative("card");
                        if (card != null && card.objectReferenceValue == null)
                            summary.Error($"PackDefinition '{path}' slot [{i}] has null card reference.");
                    }
                }

                SerializedProperty buyPrice = so.FindProperty("buyPrice");
                if (buyPrice != null && buyPrice.intValue <= 0)
                    summary.Warning($"PackDefinition '{path}' buyPrice <= 0.");
            }
        }

        // ─── Quests ──────────────────────────────────────────────────────────

        private static void ValidateQuests(ValidationSummary summary)
        {
            var seenIds = new Dictionary<string, string>();

            foreach (Quest quest in LoadAssets<Quest>())
            {
                string path = AssetDatabase.GetAssetPath(quest);
                var so = new SerializedObject(quest);

                string id = so.FindProperty("id")?.stringValue ?? string.Empty;
                if (string.IsNullOrEmpty(id))
                    summary.Error($"Quest '{path}' has no ID.");
                else if (seenIds.TryGetValue(id, out string other))
                    summary.Error($"Duplicate quest ID '{id}' at '{path}' and '{other}'.");
                else
                    seenIds[id] = path;

                CheckNonEmpty(so, "title", $"Quest '{path}' missing title.", summary);
                CheckNonEmpty(so, "description", $"Quest '{path}' missing description.", summary);

                SerializedProperty amount = so.FindProperty("targetAmount");
                if (amount != null && amount.intValue <= 0)
                    summary.Warning($"Quest '{path}' targetAmount <= 0.");
            }
        }

        // ─── Prefabs ─────────────────────────────────────────────────────────

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
                    ValidateCardPrefab(root, path, summary);
                }
                catch (Exception ex)
                {
                    summary.Error($"Failed to inspect prefab '{path}': {ex.Message}");
                }
                finally
                {
                    if (root != null) PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        private static void ValidateCardPrefab(GameObject root, string path, ValidationSummary summary)
        {
            if (root.GetComponent<CardInstance>() == null) return;

            if (root.GetComponent<CardController>() == null)
                summary.Warning($"Card prefab '{path}' has CardInstance but no CardController.");

            bool hasCollider = root.GetComponent<Collider>() != null ||
                               root.GetComponent<Collider2D>() != null;
            if (!hasCollider)
                summary.Warning($"Card prefab '{path}' has CardInstance but no Collider.");

            if (root.GetComponent<CardFeelPresenter>() == null)
                summary.Warning($"Card prefab '{path}' has CardInstance but no CardFeelPresenter.");
        }

        private static void ValidateMissingScripts(GameObject root, string path, ValidationSummary summary)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                if (count > 0)
                    summary.Error($"Prefab '{path}' has {count} missing script(s) on '{HierarchyPath(t)}'.");
            }
        }

        private static void ValidateMissingObjectReferences(GameObject root, string path, ValidationSummary summary)
        {
            foreach (Component comp in root.GetComponentsInChildren<Component>(true))
            {
                if (comp == null) continue;
                var so = new SerializedObject(comp);
                SerializedProperty it = so.GetIterator();
                bool enter = true;
                while (it.NextVisible(enter))
                {
                    enter = false;
                    if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
#pragma warning disable CS0618
                    if (it.objectReferenceValue == null && it.objectReferenceInstanceIDValue != 0)
#pragma warning restore CS0618
                        summary.Error($"Prefab '{path}' missing ref '{comp.GetType().Name}.{it.propertyPath}' on '{HierarchyPath(comp.transform)}'.");
                }
            }
        }

        // ─── Scenes ──────────────────────────────────────────────────────────

        private static void ValidateScenes(ValidationSummary summary)
        {
            var buildPaths = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (string required in RequiredBuildScenes)
            {
                if (!buildPaths.Contains(required))
                    summary.Error($"Required scene '{required}' is not in Build Settings (or disabled).");
            }

            // Sandbox scenes must NOT be in Build Settings
            foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { SandboxScenesRoot }))
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(guid);
                if (buildPaths.Contains(scenePath))
                    summary.Error($"Sandbox scene '{scenePath}' must not be in Build Settings.");
            }

            ValidateBootScene(summary);
        }

        private static void ValidateBootScene(ValidationSummary summary)
        {
            SceneSetup[] prev = EditorSceneManager.GetSceneManagerSetup();
            Scene boot = default;
            try
            {
                boot = EditorSceneManager.OpenScene(BootScenePath, OpenSceneMode.Additive);
                foreach (GameObject go in boot.GetRootGameObjects())
                {
                    if (!AllowedBootRootNames.Contains(go.name, StringComparer.Ordinal))
                        summary.Warning($"Boot scene has unexpected root object '{go.name}'.");
                }
            }
            catch (Exception ex)
            {
                summary.Error($"Failed to validate Boot scene: {ex.Message}");
            }
            finally
            {
                if (boot.IsValid()) EditorSceneManager.CloseScene(boot, true);
                if (prev != null && prev.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(prev);
            }
        }

        // ─── Localization ────────────────────────────────────────────────────

        private static void ValidateLocalizationKeys(ValidationSummary summary)
        {
            // Check locale presence
            var locales = LocalizationEditorSettings.GetLocales();
            if (!locales.Any(l => l.Identifier.Code == "en"))
                summary.Error("No English locale ('en') found in Localization settings.");
            if (!locales.Any(l => l.Identifier.Code.StartsWith("zh", StringComparison.OrdinalIgnoreCase)))
                summary.Warning("No Chinese locale ('zh*') found in Localization settings.");

            StringTableCollection collection = LocalizationEditorSettings
                .GetStringTableCollection(UnityLocalizationBridge.DefaultStringTable);
            if (collection == null || collection.SharedData == null)
            {
                summary.Error($"Localization table '{UnityLocalizationBridge.DefaultStringTable}' not found.");
                return;
            }

            // Duplicate keys in shared data
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (SharedTableData.SharedTableEntry entry in collection.SharedData.Entries)
            {
                if (!seen.Add(entry.Key))
                    summary.Warning($"Duplicate localization key '{entry.Key}'.");
            }

            // Empty translations
            foreach (StringTable table in collection.StringTables)
            {
                foreach (KeyValuePair<long, StringTableEntry> pair in table)
                {
                    if (string.IsNullOrWhiteSpace(pair.Value.Value))
                    {
                        SharedTableData.SharedTableEntry shared = collection.SharedData.GetEntry(pair.Key);
                        string keyName = shared?.Key ?? pair.Key.ToString();
                        summary.Warning($"Empty translation in '{table.name}' for key '{keyName}'.");
                    }
                }
            }

            // Per-asset key coverage
            ValidateLocalizationKeySet(
                LoadAssets<CardDefinition>().Where(a => a is not PackDefinition),
                "card", new[] { "name", "description" }, collection, summary);
            ValidateLocalizationKeySet(
                LoadAssets<RecipeDefinition>(),
                "recipe", new[] { "name" }, collection, summary);
            ValidateLocalizationKeySet(
                LoadAssets<Quest>(),
                "quest", new[] { "title", "description" }, collection, summary);
            ValidateLocalizationKeySet(
                LoadAssets<PackDefinition>(),
                "pack", new[] { "name", "description" }, collection, summary);
        }

        private static void ValidateLocalizationKeySet<T>(
            IEnumerable<T> assets,
            string category,
            IEnumerable<string> fields,
            StringTableCollection collection,
            ValidationSummary summary) where T : UnityEngine.Object
        {
            foreach (T asset in assets)
            {
                string path = AssetDatabase.GetAssetPath(asset);
                foreach (string field in fields)
                {
                    string key = LocalizationKeyBuilder.ForAsset(asset, category, field);
                    if (collection.SharedData.GetEntry(key) == null)
                        summary.Error($"Missing localization key '{key}' for '{path}'.");
                }
            }
        }

        // ─── Audio ───────────────────────────────────────────────────────────

        private static void ValidateAudio(ValidationSummary summary)
        {
            GameObject amPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AudioManagerPrefabPath);
            if (amPrefab == null)
            {
                summary.Error($"AudioManager prefab not found at '{AudioManagerPrefabPath}'.");
                return;
            }

            AudioManager am = amPrefab.GetComponent<AudioManager>();
            if (am == null)
            {
                summary.Error("AudioManager prefab is missing the AudioManager component.");
                return;
            }

            var so = new SerializedObject(am);

            if (so.FindProperty("_audioMixer")?.objectReferenceValue == null)
                summary.Warning("AudioManager: _audioMixer not assigned.");
            if (so.FindProperty("_SFXAudioGroup")?.objectReferenceValue == null)
                summary.Warning("AudioManager: _SFXAudioGroup not assigned.");
            if (so.FindProperty("_BGMAudioGroup")?.objectReferenceValue == null)
                summary.Warning("AudioManager: _BGMAudioGroup not assigned.");

            SerializedProperty sfxList = so.FindProperty("_SFXDataList");
            if (sfxList == null || sfxList.arraySize == 0)
            {
                summary.Warning("AudioManager: _SFXDataList is empty.");
                return;
            }

            var seenIds = new HashSet<int>();
            for (int i = 0; i < sfxList.arraySize; i++)
            {
                SerializedProperty entry = sfxList.GetArrayElementAtIndex(i);
                SerializedProperty idProp = entry.FindPropertyRelative("audioId");
                SerializedProperty clipProp = entry.FindPropertyRelative("audioClip");

                if (clipProp != null && clipProp.objectReferenceValue == null)
                    summary.Warning($"AudioManager SFX [{i}]: no AudioClip assigned.");

                if (idProp != null && !seenIds.Add(idProp.enumValueIndex))
                    summary.Error($"AudioManager SFX [{i}]: duplicate AudioId value '{idProp.enumValueIndex}'.");
            }

            int enumCount = Enum.GetValues(typeof(AudioId)).Length;
            if (sfxList.arraySize < enumCount)
                summary.Warning($"AudioManager SFX list has {sfxList.arraySize} entries; AudioId enum has {enumCount} values — some IDs may be unregistered.");
        }

        // ─── Odin Migration ──────────────────────────────────────────────────

        private static void ValidateOdinMigration(ValidationSummary summary)
        {
            string runtimeRoot = "Assets/_Project/Scripts/Runtime";
            string[] guids = AssetDatabase.FindAssets("t:MonoScript", new[] { runtimeRoot });

            var legacyFiles = new List<string>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string text;
                try { text = File.ReadAllText(path); }
                catch { continue; }

                if (text.Contains("[Header("))
                    legacyFiles.Add(path.Replace('\\', '/'));
            }

            if (legacyFiles.Count == 0)
            {
                Debug.Log("[Validator] Odin migration: no legacy [Header] attributes found in Runtime scripts.");
                return;
            }

            foreach (string path in legacyFiles)
                summary.Warning($"Odin migration: '[Header]' not replaced in '{path}'.");
        }

        // ─── Code Ownership ──────────────────────────────────────────────────

        private static void ValidateCodeOwnership(ValidationSummary summary)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid).Replace('\\', '/');

                if (path.StartsWith(ProjectScriptsRoot, StringComparison.Ordinal))
                {
                    ValidateProjectScript(path, summary);
                }
                else if (path.StartsWith("Assets/ThirdParty/", StringComparison.Ordinal)
                      || path.StartsWith("Assets/UIToolkitScriptComponents/", StringComparison.Ordinal))
                {
                    // expected third-party — skip
                }
                else if (path.StartsWith("Assets/_Project/", StringComparison.Ordinal))
                {
                    summary.Warning($"Script inside _Project but outside Scripts/: '{path}'.");
                }
                else if (path.StartsWith("Assets/", StringComparison.Ordinal))
                {
                    summary.Warning($"Script outside '{ProjectScriptsRoot}': '{path}'.");
                }
            }
        }

        private static void ValidateProjectScript(string path, ValidationSummary summary)
        {
            string text;
            try { text = File.ReadAllText(path); }
            catch (Exception ex) { summary.Warning($"Cannot read '{path}': {ex.Message}"); return; }

            string ns = ExtractNamespace(text);
            if (string.IsNullOrWhiteSpace(ns)) return;
            if (!ProjectNamespacePrefixes.Any(p => ns.StartsWith(p, StringComparison.Ordinal)))
                summary.Warning($"Unexpected namespace '{ns}' in project script '{path}'.");
        }

        // ─── Helpers ─────────────────────────────────────────────────────────

        private static IEnumerable<T> LoadAssets<T>() where T : UnityEngine.Object
        {
            foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { "Assets/_Project" }))
            {
                T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) yield return asset;
            }
        }

        private static void CheckNonEmpty(
            SerializedObject so, string field, string message, ValidationSummary summary, bool warn = false)
        {
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null || string.IsNullOrWhiteSpace(prop.stringValue))
            {
                if (warn) summary.Warning(message);
                else summary.Error(message);
            }
        }

        private static string HierarchyPath(Transform t)
        {
            if (t == null) return "<null>";
            var names = new Stack<string>();
            for (Transform cur = t; cur != null; cur = cur.parent) names.Push(cur.name);
            return string.Join("/", names);
        }

        private static string ExtractNamespace(string text)
        {
            const string marker = "namespace ";
            int idx = text.IndexOf(marker, StringComparison.Ordinal);
            if (idx < 0) return string.Empty;
            int start = idx + marker.Length;
            int end = start;
            while (end < text.Length && (char.IsLetterOrDigit(text[end]) || text[end] == '_' || text[end] == '.'))
                end++;
            return text.Substring(start, end - start).Trim();
        }

        // ─── Summary ─────────────────────────────────────────────────────────

        private sealed class ValidationSummary
        {
            public int ErrorCount { get; private set; }
            public int WarningCount { get; private set; }

            public void Error(string msg) { ErrorCount++; Debug.LogError($"[Validator] {msg}"); }
            public void Warning(string msg) { WarningCount++; Debug.LogWarning($"[Validator] {msg}"); }
        }
    }
}
