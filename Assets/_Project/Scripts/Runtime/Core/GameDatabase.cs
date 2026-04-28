using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Markyu.LastKernel
{
    /// <summary>
    /// Central editor-browsable registry of all game data assets.
    /// Loaded via Resources.Load so it can optionally replace Resources.LoadAll in the future.
    /// For now this is editor-first: use it to browse, search, and validate data.
    /// Runtime systems still use Resources.LoadAll through CardDefinitionCatalog etc.
    /// </summary>
    [CreateAssetMenu(menuName = "Last Kernel/Game Database", fileName = "GameDatabase")]
    public class GameDatabase : ScriptableObject
    {
        // ── Cards ─────────────────────────────────────────────────────────────

        [TitleGroup("Cards")]
        [SerializeField, TableList(ShowIndexLabels = true, IsReadOnly = true)]
        private List<CardDefinition> cards = new();

        [TitleGroup("Cards")]
        [SerializeField, TableList(ShowIndexLabels = true, IsReadOnly = true)]
        private List<PackDefinition> packs = new();

        // ── Recipes ───────────────────────────────────────────────────────────

        [TitleGroup("Recipes")]
        [SerializeField, TableList(ShowIndexLabels = true, IsReadOnly = true)]
        private List<RecipeDefinition> recipes = new();

        // ── Quests ────────────────────────────────────────────────────────────

        [TitleGroup("Quests")]
        [SerializeField, TableList(ShowIndexLabels = true, IsReadOnly = true)]
        private List<Quest> quests = new();

        // ── Runtime Access ────────────────────────────────────────────────────

        public IReadOnlyList<CardDefinition> Cards  => cards;
        public IReadOnlyList<PackDefinition>  Packs   => packs;
        public IReadOnlyList<RecipeDefinition> Recipes => recipes;
        public IReadOnlyList<Quest> Quests => quests;

        // ── Editor Tools ──────────────────────────────────────────────────────

#if UNITY_EDITOR
        [TitleGroup("Tools")]
        [Button("Rebuild All from Resources", ButtonSizes.Large), GUIColor(0.4f, 0.9f, 0.5f)]
        public void RebuildFromResources()
        {
            cards   = new List<CardDefinition>(
                Resources.LoadAll<CardDefinition>("Cards"));
            packs   = new List<PackDefinition>(
                Resources.LoadAll<PackDefinition>("Packs"));
            recipes = new List<RecipeDefinition>(
                Resources.LoadAll<RecipeDefinition>("Recipes"));
            quests  = new List<Quest>(
                Resources.LoadAll<Quest>("Quests"));

            // Exclude PackDefinitions from the cards list (packs inherit CardDefinition)
            cards.RemoveAll(c => c is PackDefinition);

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();

            Debug.Log($"[GameDatabase] Rebuilt: {cards.Count} cards, {packs.Count} packs, " +
                      $"{recipes.Count} recipes, {quests.Count} quests.");
        }

        [TitleGroup("Tools")]
        [Button("Validate All Game Data", ButtonSizes.Medium), GUIColor(0.4f, 0.8f, 1f)]
        public void ValidateAll()
        {
            // Invoke via menu path so GameDatabase (Runtime assembly) doesn't need to
            // reference ProjectValidator (Editor assembly).
            EditorApplication.ExecuteMenuItem("Tools/LAST KERNEL/Validate All Game Data");
        }

        [TitleGroup("Tools")]
        [Button("Detect Duplicate IDs", ButtonSizes.Medium), GUIColor(1f, 0.85f, 0.4f)]
        public void CheckDuplicates()
        {
            EditorApplication.ExecuteMenuItem("Tools/LAST KERNEL/Detect Duplicate IDs");
        }
#endif

        [TitleGroup("Stats"), ShowInInspector, ReadOnly]
        private string DatabaseSummary =>
            $"{cards.Count} cards | {packs.Count} packs | {recipes.Count} recipes | {quests.Count} quests";
    }
}
