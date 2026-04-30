// Tracks which CardDefinitions the player has ever encountered.
// Persists to GameData on save and restores on load.
// Also owns the localized recipe-card label refresh on language change.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Markyu.LastKernel
{
    internal sealed class CardDiscoveryService
    {
        private readonly HashSet<CardDefinition> discoveredCards = new();

        // Late-bound so CardSpawnService can be wired after this service is constructed.
        private readonly Func<string, CardDefinition> getDefinitionById;

        public CardDiscoveryService(Func<string, CardDefinition> getDefinitionById)
        {
            this.getDefinitionById = getDefinitionById;
        }

        public bool IsDiscovered(CardDefinition card) => card != null && discoveredCards.Contains(card);

        // Recipe cards are ephemeral runtime objects, not player-discoverable content.
        public void MarkDiscovered(CardDefinition card)
        {
            if (card != null && card.Category != CardCategory.Recipe && !discoveredCards.Contains(card))
                discoveredCards.Add(card);
        }

        public void RestoreFromGameData(GameData gameData)
        {
            if (gameData == null) return;

            foreach (string cardId in gameData.DiscoveredCards)
            {
                var def = getDefinitionById(cardId);
                if (def != null && !discoveredCards.Contains(def))
                    discoveredCards.Add(def);
            }
        }

        public void SaveToGameData(GameData gameData)
        {
            foreach (var cardDef in discoveredCards)
            {
                if (cardDef != null)
                    gameData.DiscoveredCards.Add(cardDef.Id);
            }
        }

        public void UpdateLocalizedRecipeCards(IEnumerable<CardInstance> allCards)
        {
            foreach (var card in allCards)
            {
                if (card == null || card.Definition == null || card.Definition.Category != CardCategory.Recipe)
                    continue;

                string definitionId = card.Definition.Id;
                if (string.IsNullOrEmpty(definitionId) || !definitionId.StartsWith(CardSpawnService.RecipeDefinitionIdPrefix))
                    continue;

                string recipeId = definitionId.Substring(CardSpawnService.RecipeDefinitionIdPrefix.Length);
                RecipeDefinition recipe = CraftingManager.Instance?.GetRecipeById(recipeId);
                if (recipe == null) continue;

                if (recipe.ResultingCard != null)
                    card.Definition.SetDisplayName(GameLocalization.Format("recipe.blueprint", recipe.ResultingCard.DisplayName));
                else
                    card.Definition.SetDisplayName(GameLocalization.Get("recipe.unknown"));

                card.Definition.SetDescription(CraftingManager.Instance?.GetFormattedIngredients(recipe) ?? string.Empty);
                card.SetDefinition(card.Definition);
            }
        }
    }
}
