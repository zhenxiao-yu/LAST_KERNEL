using System.Collections.Generic;
using UnityEngine;

namespace Markyu.FortStack
{
    /// <summary>
    /// Owns runtime card-definition lookup so CardManager can stay focused on scene orchestration and spawning.
    /// </summary>
    public sealed class CardDefinitionCatalog
    {
        private readonly Dictionary<string, CardDefinition> definitionsById = new();

        public int Count => definitionsById.Count;

        public static CardDefinitionCatalog LoadFromResources()
        {
            var catalog = new CardDefinitionCatalog();
            catalog.AddDefinitions(Resources.LoadAll<CardDefinition>("Cards"), "Cards");
            catalog.AddDefinitions(Resources.LoadAll<PackDefinition>("Packs"), "Packs");
            return catalog;
        }

        public bool TryGetDefinition(string id, out CardDefinition definition)
        {
            definition = null;
            return !string.IsNullOrWhiteSpace(id) && definitionsById.TryGetValue(id, out definition);
        }

        private void AddDefinitions(IEnumerable<CardDefinition> definitions, string sourceName)
        {
            if (definitions == null)
            {
                return;
            }

            foreach (var definition in definitions)
            {
                AddDefinition(definition, sourceName);
            }
        }

        private void AddDefinition(CardDefinition definition, string sourceName)
        {
            if (definition == null)
            {
                Debug.LogWarning($"CardDefinitionCatalog: Null definition found while loading Resources/{sourceName}.");
                return;
            }

            if (string.IsNullOrWhiteSpace(definition.Id))
            {
                Debug.LogWarning($"CardDefinitionCatalog: '{definition.name}' in Resources/{sourceName} has an empty id and will be ignored.", definition);
                return;
            }

            if (definitionsById.TryGetValue(definition.Id, out var existing))
            {
                Debug.LogWarning(
                    $"CardDefinitionCatalog: Duplicate card id '{definition.Id}' on '{definition.name}'. Keeping '{existing.name}' and ignoring the duplicate.",
                    definition);
                return;
            }

            definitionsById.Add(definition.Id, definition);
        }
    }
}
