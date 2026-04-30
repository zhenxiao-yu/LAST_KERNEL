// Orchestrates save/load for the card board:
//   • On load  — restores stacks, equipment, active crafts, and discovered cards
//   • On save  — serializes stacks and discovered-card IDs into GameData
//
// Delegates card creation to CardSpawnService and stack registration to StackRegistry.

using UnityEngine;

namespace Markyu.LastKernel
{
    internal sealed class CardSaveRestoreService
    {
        private readonly CardSpawnService spawnService;
        private readonly StackRegistry stackRegistry;
        private readonly CardDiscoveryService discoveryService;

        public CardSaveRestoreService(
            CardSpawnService spawnService,
            StackRegistry stackRegistry,
            CardDiscoveryService discoveryService)
        {
            this.spawnService = spawnService;
            this.stackRegistry = stackRegistry;
            this.discoveryService = discoveryService;
        }

        public void HandleSceneDataReady(SceneData sceneData, bool wasLoaded)
        {
            if (wasLoaded)
            {
                foreach (var stackData in sceneData.SavedStacks)
                    RestoreStack(stackData);

                stackRegistry.ResolveOverlaps();
            }
            else
            {
                spawnService.SpawnDefaultCards();
            }
        }

        public void HandleBeforeSave(GameData gameData)
        {
            if (gameData.TryGetScene(out var sceneData))
                sceneData.SaveStacks(stackRegistry.AllStacks);

            discoveryService.SaveToGameData(gameData);
        }

        private void RestoreStack(StackData stackData)
        {
            if (stackData.Cards == null || stackData.Cards.Count == 0) return;

            CardStack mainStack = null;
            Vector3 stackPos = stackData.GetPosition();

            for (int i = 0; i < stackData.Cards.Count; i++)
            {
                var cardData = stackData.Cards[i];
                CardDefinition def = spawnService.GetDefinitionById(cardData.Id);
                if (def == null) continue;

                if (def is PackDefinition packDef)
                {
                    var packInstance = spawnService.CreatePackInstance(packDef, stackPos);
                    packInstance.RestoreSavedStats(cardData);
                    continue;
                }

                CardInstance newCard = spawnService.CreateCardInstance(def, stackPos, CardStack.RefuseAll);
                if (newCard == null)
                {
                    Debug.LogWarning($"CardSaveRestoreService: Failed to restore card '{cardData.Id}' in saved stack.");
                    continue;
                }

                newCard.RestoreSavedStats(cardData);

                if (newCard.EquipperComponent != null && cardData.EquippedItems != null && cardData.EquippedItems.Count > 0)
                    RestoreEquipmentForCard(newCard, cardData);

                if (i == 0)
                {
                    // The first card becomes the "anchor" of the stack.
                    mainStack = newCard.Stack;
                    mainStack.SetTargetPosition(stackPos, instant: true);
                }
                else
                {
                    // Subsequent cards created their own temporary stacks on Init.
                    // Steal them and add to the main stack.
                    CardStack tempStack = newCard.Stack;
                    tempStack.RemoveCard(newCard);
                    mainStack.AddCard(newCard);
                }
            }

            if (mainStack != null)
            {
                mainStack.SetTargetPosition(stackPos, instant: true);

                if (stackData.ActiveCraft != null && !string.IsNullOrEmpty(stackData.ActiveCraft.RecipeId))
                {
                    CraftingManager.Instance?.RestoreCraftingTask(
                        mainStack,
                        stackData.ActiveCraft.RecipeId,
                        stackData.ActiveCraft.Progress
                    );
                }
            }
        }

        private void RestoreEquipmentForCard(CardInstance character, CardData data)
        {
            // If this personnel card changed class, preserve the Recruit base state
            // so the equipment system knows it is restoring an upgraded card.
            if (!string.IsNullOrEmpty(data.OriginalId))
            {
                var originalDef = spawnService.GetDefinitionById(data.OriginalId);
                character.EquipperComponent?.SetOriginalDefinition(originalDef);
            }

            foreach (var itemData in data.EquippedItems)
            {
                var itemDef = spawnService.GetDefinitionById(itemData.Id);
                if (itemDef == null) continue;

                // Created in the void — Equip() will immediately remove it from the board.
                CardInstance itemCard = spawnService.CreateCardInstance(itemDef, Vector3.zero, CardStack.RefuseAll);
                if (itemCard == null)
                {
                    Debug.LogWarning($"CardSaveRestoreService: Failed to restore equipped item '{itemData.Id}' on '{character.Definition.DisplayName}'.");
                    continue;
                }

                itemCard.RestoreSavedStats(itemData);
                character.EquipperComponent?.Equip(itemCard);
            }
        }

        public CardInstance RestoreCardFromData(CardData data, Vector3 position)
        {
            string spawnId = !string.IsNullOrEmpty(data.OriginalId) ? data.OriginalId : data.Id;
            CardDefinition baseDef = spawnService.GetDefinitionById(spawnId);
            if (baseDef == null) return null;

            CardInstance card = spawnService.CreateCardInstance(baseDef, position, CardStack.RefuseAll);
            card.RestoreSavedStats(data);
            RestoreEquipmentForCard(card, data);
            return card;
        }

        public void RestoreTraveler(CardData cardData, Vector3 position)
        {
            CardDefinition def = spawnService.GetDefinitionById(cardData.Id);
            if (def == null) return;

            CardInstance newCard = spawnService.CreateCardInstance(def, position, CardStack.RefuseAll);
            if (newCard == null) return;

            newCard.RestoreSavedStats(cardData);

            if (newCard.EquipperComponent != null && cardData.EquippedItems != null && cardData.EquippedItems.Count > 0)
                RestoreEquipmentForCard(newCard, cardData);
        }
    }
}
