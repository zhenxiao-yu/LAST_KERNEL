using System.Collections.Generic;
using System.Linq;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Keeps aggregate card-count/resource math out of CardManager's orchestration code.
    /// </summary>
    public static class CardStatsCalculator
    {
        public static StatsSnapshot Calculate(IEnumerable<CardInstance> cards, CardSettings settings)
        {
            var allCards = cards?
                .Where(card => card != null && card.Definition != null)
                .ToList() ?? new List<CardInstance>();

            int cardsOwned = CalculateCardsOwned(allCards);
            int totalBoost = CalculateTotalBoost(allCards);
            int baseCardLimit = settings != null ? settings.BaseCardLimit : 0;

            return new StatsSnapshot
            {
                TotalNutrition = CalculateTotalNutrition(allCards),
                NutritionNeed = CalculateNutritionNeed(allCards, settings),
                Currency = CalculateCurrency(allCards),
                CardsOwned = cardsOwned,
                TotalBoost = totalBoost,
                CardLimit = baseCardLimit + totalBoost,
                ExcessCards = cardsOwned - (baseCardLimit + totalBoost),
                TotalCharacters = CalculateCharacters(allCards)
            };
        }

        private static int CalculateTotalNutrition(IEnumerable<CardInstance> cards)
        {
            return cards
                .Where(card => card.Definition.Category == CardCategory.Consumable)
                .Sum(card => card.CurrentNutrition);
        }

        private static int CalculateNutritionNeed(IEnumerable<CardInstance> cards, CardSettings settings)
        {
            int hungerPerCharacter = settings != null ? settings.HungerPerCharacter : 0;

            return cards
                .Count(card => card.Definition.Category == CardCategory.Character) * hungerPerCharacter;
        }

        private static int CalculateCurrency(IEnumerable<CardInstance> cards)
        {
            int total = 0;

            foreach (var card in cards)
            {
                if (card.Definition.Category == CardCategory.Currency)
                {
                    total++;
                }

                if (card.TryGetComponent<ChestLogic>(out var chest))
                {
                    total += chest.StoredCoins;
                }
            }

            return total;
        }

        private static int CalculateCardsOwned(IEnumerable<CardInstance> cards)
        {
            return cards
                .Where(card => card is not PackInstance)
                .Count(card => card.Definition.Category != CardCategory.Currency);
        }

        private static int CalculateTotalBoost(IEnumerable<CardInstance> cards)
        {
            int totalBoost = 0;

            foreach (var card in cards)
            {
                if (card.Definition is LimitBoosterDefinition boosterDefinition)
                {
                    totalBoost += boosterDefinition.BoostAmount;
                }
            }

            return totalBoost;
        }

        private static int CalculateCharacters(IEnumerable<CardInstance> cards)
        {
            return cards.Count(card => card.Definition.Category == CardCategory.Character);
        }
    }
}
