using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Safe, non-destructive editor fixes. Never deletes, renames, or moves assets.
    /// Never changes gameplay data or recipe balance.
    /// </summary>
    public static class QuickFixTools
    {
        [MenuItem("LAST KERNEL/Dev/Fix Card Issues", false, 20)]
        public static void FixSafeIssues()
        {
            Debug.Log("=== QuickFixTools: Running safe fixes ===");
            int fixes = 0;

            fixes += CreateMissingFolders();
            fixes += AssignDefaultFeelProfileToSelectedCards();
            fixes += AddFeelPresenterToSelectedCards();
            fixes += AddCardControllerToSelectedCards();
            fixes += AddRenderOrderControllerToSelectedCards();

            Debug.Log($"=== QuickFixTools: Done — {fixes} fix(es) applied ===");
        }

        // ─── Folder Creation ─────────────────────────────────────────────────

        private static int CreateMissingFolders()
        {
            string[] required =
            {
                "Assets/_Project/Scenes/Test",
                "Assets/_Project/Tests/EditMode",
                "Assets/_Project/Tests/PlayMode",
                "Assets/_Project/Docs",
                "Assets/_Project/Data/Resources/Cards",
                "Assets/_Project/Data/Resources/Recipes",
                "Assets/_Project/Data/Resources/Packs",
                "Assets/_Project/Data/Resources/Quests",
            };

            int created = 0;
            foreach (string folder in required)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    EnsureFolder(folder);
                    Debug.Log($"[QuickFix] Created folder: {folder}");
                    created++;
                }
            }
            return created;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        // ─── CardFeelProfile Assignment ──────────────────────────────────────

        private static int AssignDefaultFeelProfileToSelectedCards()
        {
            // Only safe when exactly one default CardFeelProfile exists in the project
            string[] profileGuids = AssetDatabase.FindAssets("t:CardFeelProfile", new[] { "Assets/_Project" });
            if (profileGuids.Length != 1)
            {
                if (profileGuids.Length == 0)
                    Debug.LogWarning("[QuickFix] No CardFeelProfile found — skipping feel profile assignment.");
                else
                    Debug.LogWarning($"[QuickFix] {profileGuids.Length} CardFeelProfiles found — cannot auto-assign (ambiguous). Skipping.");
                return 0;
            }

            string profilePath = AssetDatabase.GUIDToAssetPath(profileGuids[0]);
            ScriptableObject profile = AssetDatabase.LoadAssetAtPath<ScriptableObject>(profilePath);

            int fixed_ = 0;
            foreach (CardSettings settings in Selection.GetFiltered<CardSettings>(SelectionMode.Assets))
            {
                var so = new SerializedObject(settings);
                SerializedProperty prop = so.FindProperty("feelProfile");
                if (prop != null && prop.objectReferenceValue == null)
                {
                    prop.objectReferenceValue = profile;
                    so.ApplyModifiedProperties();
                    Debug.Log($"[QuickFix] Assigned default CardFeelProfile to '{AssetDatabase.GetAssetPath(settings)}'.");
                    fixed_++;
                }
            }
            return fixed_;
        }

        // ─── CardFeelPresenter ───────────────────────────────────────────────

        private static int AddFeelPresenterToSelectedCards()
        {
            int fixed_ = 0;
            foreach (GameObject go in Selection.gameObjects)
            {
                if (go.GetComponent<CardInstance>() == null) continue;
                if (go.GetComponent<CardFeelPresenter>() != null) continue;

                go.AddComponent<CardFeelPresenter>();
                EditorUtility.SetDirty(go);
                Debug.Log($"[QuickFix] Added CardFeelPresenter to '{go.name}'.");
                fixed_++;
            }
            return fixed_;
        }

        // ─── CardController ──────────────────────────────────────────────────

        private static int AddCardControllerToSelectedCards()
        {
            int fixed_ = 0;
            foreach (GameObject go in Selection.gameObjects)
            {
                if (go.GetComponent<CardInstance>() == null) continue;
                if (go.GetComponent<CardController>() != null) continue;

                go.AddComponent<CardController>();
                EditorUtility.SetDirty(go);
                Debug.Log($"[QuickFix] Added CardController to '{go.name}'.");
                fixed_++;
            }
            return fixed_;
        }

        // ─── CardRenderOrderController ───────────────────────────────────────

        private static int AddRenderOrderControllerToSelectedCards()
        {
            int fixed_ = 0;
            foreach (GameObject go in Selection.gameObjects)
            {
                if (go.GetComponent<CardInstance>() == null) continue;
                if (go.GetComponent<CardRenderOrderController>() != null) continue;

                go.AddComponent<CardRenderOrderController>();
                EditorUtility.SetDirty(go);
                Debug.Log($"[QuickFix] Added CardRenderOrderController to '{go.name}'.");
                fixed_++;
            }
            return fixed_;
        }

        // ─── Batch: All Card Prefabs ─────────────────────────────────────────

        /// <summary>
        /// Processes every prefab under Assets/_Project/Prefabs/Cards/ that has a
        /// CardInstance component. Adds CardController and CardFeelPresenter if either
        /// is missing. Also assigns the default CardFeelProfile to any CardSettings
        /// asset that is missing one (when exactly one profile exists in the project).
        /// Safe to run multiple times — skips objects that already have the component.
        /// </summary>
        [MenuItem("LAST KERNEL/Dev/Fix All Card Prefabs", false, 21)]
        public static void FixAllCardPrefabs()
        {
            Debug.Log("=== QuickFixTools: Fix All Card Prefabs ===");
            int componentFixes = 0;
            int settingsFixes = 0;

            componentFixes += FixComponentsOnAllCardPrefabs();
            settingsFixes += AssignFeelProfileToAllCardSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"=== QuickFixTools: Done — {componentFixes} component fix(es), {settingsFixes} settings fix(es) ===");
        }

        private static int FixComponentsOnAllCardPrefabs()
        {
            const string cardPrefabsRoot = "Assets/_Project/Prefabs/Cards";
            int fixes = 0;

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { cardPrefabsRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = null;
                try
                {
                    prefab = PrefabUtility.LoadPrefabContents(path);
                    if (prefab.GetComponent<CardInstance>() == null) continue;

                    bool changed = false;

                    if (prefab.GetComponent<CardController>() == null)
                    {
                        prefab.AddComponent<CardController>();
                        Debug.Log($"[QuickFix] Added CardController → '{path}'");
                        changed = true;
                        fixes++;
                    }

                    if (prefab.GetComponent<CardFeelPresenter>() == null)
                    {
                        prefab.AddComponent<CardFeelPresenter>();
                        Debug.Log($"[QuickFix] Added CardFeelPresenter → '{path}'");
                        changed = true;
                        fixes++;
                    }

                    if (prefab.GetComponent<CardRenderOrderController>() == null)
                    {
                        prefab.AddComponent<CardRenderOrderController>();
                        Debug.Log($"[QuickFix] Added CardRenderOrderController → '{path}'");
                        changed = true;
                        fixes++;
                    }

                    if (changed)
                        PrefabUtility.SaveAsPrefabAsset(prefab, path);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[QuickFix] Skipped '{path}': {ex.Message}");
                }
                finally
                {
                    if (prefab != null) PrefabUtility.UnloadPrefabContents(prefab);
                }
            }

            return fixes;
        }

        private static int AssignFeelProfileToAllCardSettings()
        {
            string[] profileGuids = AssetDatabase.FindAssets("t:CardFeelProfile", new[] { "Assets/_Project" });
            if (profileGuids.Length == 0)
            {
                Debug.LogWarning("[QuickFix] No CardFeelProfile found — skipping feel profile assignment.");
                return 0;
            }
            if (profileGuids.Length > 1)
            {
                Debug.LogWarning($"[QuickFix] {profileGuids.Length} CardFeelProfiles found — cannot auto-assign (ambiguous). Assign manually.");
                return 0;
            }

            string profilePath = AssetDatabase.GUIDToAssetPath(profileGuids[0]);
            ScriptableObject profile = AssetDatabase.LoadAssetAtPath<ScriptableObject>(profilePath);

            int fixes = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:CardSettings", new[] { "Assets/_Project" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CardSettings settings = AssetDatabase.LoadAssetAtPath<CardSettings>(path);
                if (settings == null) continue;

                var so = new SerializedObject(settings);
                SerializedProperty prop = so.FindProperty("feelProfile");
                if (prop == null || prop.objectReferenceValue != null) continue;

                prop.objectReferenceValue = profile;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(settings);
                Debug.Log($"[QuickFix] Assigned CardFeelProfile → '{path}'");
                fixes++;
            }

            return fixes;
        }
    }
}
