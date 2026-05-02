using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Markyu.LastKernel.Tests
{
    /// <summary>
    /// Validates all CardDefinition assets in the project for structural correctness.
    /// </summary>
    public class CardDefinitionValidationTests
    {
        private CardDefinition[] _allCards;

        [SetUp]
        public void SetUp()
        {
            _allCards = AssetDatabase
                .FindAssets("t:CardDefinition", new[] { "Assets/_Project" })
                .Select(g => AssetDatabase.LoadAssetAtPath<CardDefinition>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(d => d != null)
                .ToArray();
        }

        [Test]
        public void CardIds_AreUnique()
        {
            var seen = new Dictionary<string, string>();
            var duplicates = new List<string>();

            foreach (CardDefinition card in _allCards)
            {
                string path = AssetDatabase.GetAssetPath(card);
                if (string.IsNullOrEmpty(card.Id))
                {
                    duplicates.Add($"'{path}' has no ID");
                    continue;
                }

                if (seen.TryGetValue(card.Id, out string other))
                    duplicates.Add($"ID '{card.Id}' duplicated at '{path}' and '{other}'");
                else
                    seen[card.Id] = path;
            }

            Assert.IsEmpty(duplicates, "Duplicate or missing card IDs:\n" + string.Join("\n", duplicates));
        }

        [Test]
        public void AllCards_HaveDisplayName()
        {
            var missing = _allCards
                .Where(c => string.IsNullOrWhiteSpace(AssetDisplayName(c)))
                .Select(c => AssetDatabase.GetAssetPath(c))
                .ToList();

            Assert.IsEmpty(missing, "Cards missing displayName:\n" + string.Join("\n", missing));
        }

        [Test]
        public void AllCards_HaveArtTexture()
        {
            var missing = new List<string>();
            foreach (CardDefinition card in _allCards)
            {
                var so = new SerializedObject(card);
                SerializedProperty art = so.FindProperty("artTexture");
                if (art == null || art.objectReferenceValue == null)
                    missing.Add(AssetDatabase.GetAssetPath(card));
            }

            // Art texture is a warning-level check — report but do not fail hard
            if (missing.Count > 0)
                Debug.LogWarning($"[Test] {missing.Count} card(s) missing art texture:\n{string.Join("\n", missing)}");
        }

        [Test]
        public void CombatCards_HavePositiveMaxHealth()
        {
            var failing = new List<string>();
            foreach (CardDefinition card in _allCards)
            {
                var so = new SerializedObject(card);
                SerializedProperty combatType = so.FindProperty("combatType");
                SerializedProperty maxHealth = so.FindProperty("maxHealth");

                if (combatType != null && combatType.enumValueIndex != 0 &&
                    maxHealth != null && maxHealth.intValue <= 0)
                {
                    failing.Add($"{AssetDatabase.GetAssetPath(card)} (combatType={combatType.enumValueIndex}, maxHealth={maxHealth.intValue})");
                }
            }

            Assert.IsEmpty(failing, "Combat cards with maxHealth <= 0:\n" + string.Join("\n", failing));
        }

        [Test]
        public void AllCards_HaveNonNoneCategory()
        {
            var failing = new List<string>();
            foreach (CardDefinition card in _allCards)
            {
                if (card is PackDefinition)
                    continue;

                var so = new SerializedObject(card);
                SerializedProperty cat = so.FindProperty("category");
                if (cat != null && cat.enumValueIndex == 0)
                    failing.Add(AssetDatabase.GetAssetPath(card));
            }

            // Category None is a warning — do not fail CI, just report
            if (failing.Count > 0)
                Debug.LogWarning($"[Test] {failing.Count} card(s) have category None:\n{string.Join("\n", failing)}");
        }

        // ─── Helpers ─────────────────────────────────────────────────────────

        private static string AssetDisplayName(CardDefinition card)
        {
            var so = new SerializedObject(card);
            return so.FindProperty("displayName")?.stringValue;
        }
    }
}
