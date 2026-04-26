using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Markyu.LastKernel
{
    public interface ICardService
    {
        event System.Action<CardInstance> OnCardCreated;
        event System.Action<CardInstance> OnCardKilled;
        event System.Action<CardDefinition> OnCardEquipped;
        event System.Action<StatsSnapshot> OnStatsChanged;

        IEnumerable<CardInstance> AllCards { get; }

        CardDefinition GetDefinitionById(string id);
        CardInstance CreateCardInstance(CardDefinition definition, Vector3 position, CardStack stackToIgnore = null);
        PackInstance CreatePackInstance(PackDefinition definition, Vector3 position);
        CardDefinition CreateRecipeCardDefinition(RecipeDefinition recipe);
        void SpawnRecipeCard(RecipeDefinition recipe, CardStack craftingStack);
        CardInstance RestoreCardFromData(CardData data, Vector3 position);
        void RestoreTraveler(CardData cardData, Vector3 position);
        void ReturnCardToBoard(CardInstance cardToDrop);

        void RegisterStack(CardStack stack);
        void UnregisterStack(CardStack stack);
        bool CanStack(CardDefinition bottom, CardDefinition top);
        void HighlightStackableStacks(CardInstance liftedCard);
        void TurnOffHighlightedCards();
        void ResolveOverlaps();
        void ResolveOverlaps(CombatRect combatRect, CardStack stackToIgnore = null);
        void EnforceBoardLimits();

        bool IsCardDiscovered(CardDefinition card);
        void MarkCardAsDiscovered(CardDefinition card);
        StatsSnapshot GetStatsSnapshot();

        IEnumerator FeedCharacters();
    }
}
