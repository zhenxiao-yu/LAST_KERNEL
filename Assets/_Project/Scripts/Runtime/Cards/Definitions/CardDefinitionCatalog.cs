using System.Collections.Generic;
using UnityEngine;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Owns runtime card-definition lookup so CardManager can stay focused on scene orchestration and spawning.
    ///
    /// Load order:
    ///   1. If a GameDatabase asset exists at Resources/GameDatabase, use its pre-built lists.
    ///      This is faster (no filesystem scan) and lets the GameDatabase act as the source of truth.
    ///   2. Otherwise fall back to Resources.LoadAll so the game still works without a GameDatabase asset.
    /// </summary>
    public sealed class CardDefinitionCatalog
    {
        private readonly Dictionary<string, CardDefinition> definitionsById = new();

        public int Count => definitionsById.Count;

        public static CardDefinitionCatalog LoadFromResources()
        {
            var catalog = new CardDefinitionCatalog();

            var db = Resources.Load<GameDatabase>("GameDatabase");
            if (db != null && db.Cards.Count > 0)
            {
                catalog.AddDefinitions(db.Cards, "GameDatabase/Cards");
                catalog.AddDefinitions(db.Packs, "GameDatabase/Packs");
            }
            else
            {
                // Fallback: scan Resources folders directly.
                catalog.AddDefinitions(Resources.LoadAll<CardDefinition>("Cards"), "Cards");
                catalog.AddDefinitions(Resources.LoadAll<PackDefinition>("Packs"), "Packs");
            }

            return catalog;
        }

        public bool TryGetDefinition(string id, out CardDefinition definition)
        {
            definition = null;
            return !string.IsNullOrWhiteSpace(id) && definitionsById.TryGetValue(id, out definition);
        }

        private void AddDefinitions(IEnumerable<CardDefinition> definitions, string sourceName)
        {
            if (definitions == null) return;
            foreach (var definition in definitions)
                AddDefinition(definition, sourceName);
        }

        private void AddDefinition(CardDefinition definition, string sourceName)
        {
            if (definition == null)
            {
                Debug.LogWarning($"CardDefinitionCatalog: Null definition in {sourceName}.");
                return;
            }

            if (string.IsNullOrWhiteSpace(definition.Id))
            {
                Debug.LogWarning($"CardDefinitionCatalog: '{definition.name}' in {sourceName} has an empty ID and will be ignored.", definition);
                return;
            }

            if (definitionsById.TryGetValue(definition.Id, out var existing))
            {
                Debug.LogWarning(
                    $"CardDefinitionCatalog: Duplicate card ID '{definition.Id}' on '{definition.name}'. Keeping '{existing.name}'.",
                    definition);
                return;
            }

            definitionsById.Add(definition.Id, definition);
        }
    }
}
