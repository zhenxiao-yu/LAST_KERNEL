// Owns the card and pack prefab factory, definition catalog lookup,
// and recipe-card definition generation.
//
// Consumed exclusively by CardManager and CardSaveRestoreService.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Markyu.LastKernel
{
    internal sealed class CardSpawnService
    {
        private readonly StackRegistry stackRegistry;
        private readonly CardDiscoveryService discoveryService;
        private readonly Dictionary<CardCategory, CardInstance> prefabLookup;
        private readonly CardInstance aggressiveMobPrefab;
        private readonly PackInstance packPrefab;
        private readonly CardDefinition recipeCardTemplate;
        private readonly CardSettings cardSettings;
        private readonly List<CardDefinition> defaultSpawnCards;
        private readonly Vector3 defaultSpawnPosition;
        private readonly float defaultSpawnRadius;
        private readonly CardDefinitionCatalog definitionCatalog;

        // Recipe card definitions are created at runtime and cannot be stored in the
        // asset database. This prefix makes their IDs distinct from asset-based definitions
        // so GetDefinitionById can reconstruct them on demand (e.g. after a save/load).
        public const string RecipeDefinitionIdPrefix = "Recipe:";

        public event Action<CardInstance> OnCardCreated;

        public CardSpawnService(
            List<CategoryEntry> cardPrefabMappings,
            CardInstance aggressiveMobPrefab,
            PackInstance packPrefab,
            CardDefinition recipeCardTemplate,
            CardSettings cardSettings,
            List<CardDefinition> defaultSpawnCards,
            Vector3 defaultSpawnPosition,
            float defaultSpawnRadius,
            StackRegistry stackRegistry,
            CardDiscoveryService discoveryService)
        {
            this.aggressiveMobPrefab = aggressiveMobPrefab;
            this.packPrefab = packPrefab;
            this.recipeCardTemplate = recipeCardTemplate;
            this.cardSettings = cardSettings;
            this.defaultSpawnCards = defaultSpawnCards;
            this.defaultSpawnPosition = defaultSpawnPosition;
            this.defaultSpawnRadius = defaultSpawnRadius;
            this.stackRegistry = stackRegistry;
            this.discoveryService = discoveryService;

            prefabLookup = BuildPrefabLookup(cardPrefabMappings);
            definitionCatalog = CardDefinitionCatalog.LoadFromResources();
        }

        private static Dictionary<CardCategory, CardInstance> BuildPrefabLookup(List<CategoryEntry> mappings)
        {
            var lookup = new Dictionary<CardCategory, CardInstance>();
            if (mappings == null || mappings.Count == 0)
            {
                Debug.LogWarning("CardSpawnService: No card prefab mappings assigned. Card spawning will fail until configured.");
                return lookup;
            }
            foreach (var mapping in mappings)
            {
                if (mapping.prefab == null)
                {
                    Debug.LogWarning($"CardSpawnService: Prefab for category '{mapping.category}' is missing.");
                    continue;
                }
                if (lookup.ContainsKey(mapping.category))
                {
                    Debug.LogWarning($"CardSpawnService: Duplicate entry for category '{mapping.category}'. Ignoring second entry.");
                    continue;
                }
                lookup.Add(mapping.category, mapping.prefab);
            }
            return lookup;
        }

        public CardDefinition GetDefinitionById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                Debug.LogWarning("CardSpawnService: Cannot look up a card definition with an empty id.");
                return null;
            }

            if (definitionCatalog != null && definitionCatalog.TryGetDefinition(id, out var def))
                return def;

            if (id.StartsWith(RecipeDefinitionIdPrefix))
            {
                string recipeId = id.Substring(RecipeDefinitionIdPrefix.Length);
                var recipe = CraftingManager.Instance?.GetRecipeById(recipeId);
                if (recipe != null) return CreateRecipeCardDefinition(recipe);
            }

            Debug.LogError($"CardSpawnService: Could not find CardDefinition with ID '{id}'.");
            return null;
        }

        public CardInstance CreateCardInstance(CardDefinition definition, Vector3 position, CardStack stackToIgnore = null)
        {
            if (definition == null)
            {
                Debug.LogError("CardSpawnService: Cannot create a card instance from a null definition.");
                return null;
            }
            if (cardSettings == null)
            {
                Debug.LogError($"CardSpawnService: Cannot spawn '{definition.DisplayName}' because CardSettings is not assigned.");
                return null;
            }

            discoveryService.MarkDiscovered(definition);

            bool friendlyMode = GameDirector.Instance?.GameData?.GameplayPrefs?.IsFriendlyMode ?? false;
            if (friendlyMode && definition.IsAggressive) return null;

            CardInstance prefabToSpawn = null;
            if (definition.Category == CardCategory.Mob && definition.IsAggressive && aggressiveMobPrefab != null)
                prefabToSpawn = aggressiveMobPrefab;
            else if (prefabLookup.TryGetValue(definition.Category, out var genericPrefab))
                prefabToSpawn = genericPrefab;

            if (prefabToSpawn == null)
            {
                Debug.LogError($"CardSpawnService: No prefab found for category '{definition.Category}'. Cannot spawn card '{definition.DisplayName}'.");
                return null;
            }

            CardInstance newCard = UnityEngine.Object.Instantiate(prefabToSpawn, position, Quaternion.identity);
            newCard.Initialize(definition, cardSettings, stackToIgnore);

            if (definition is EnclosureDefinition enclosureDef)
            {
                var enclosureLogic = newCard.gameObject.AddComponent<EnclosureLogic>();
                enclosureLogic.Initialize(enclosureDef.Capacity);
            }
            else if (definition is ChestDefinition chestDef)
            {
                var chestLogic = newCard.gameObject.AddComponent<ChestLogic>();
                chestLogic.Initialize(newCard);
            }
            else if (definition is MarketDefinition)
            {
                var marketLogic = newCard.gameObject.AddComponent<MarketLogic>();
                marketLogic.Initialize(newCard);
            }

            if (definition.Category == CardCategory.Character)
            {
                newCard.gameObject.AddComponent<CardAI>();
                newCard.gameObject.AddComponent<VillagerLockToggle>();
            }

            OnCardCreated?.Invoke(newCard);
            return newCard;
        }

        public PackInstance CreatePackInstance(PackDefinition definition, Vector3 position)
        {
            if (definition == null)
            {
                Debug.LogError("CardSpawnService: Cannot create a pack from a null definition.");
                return null;
            }
            if (packPrefab == null)
            {
                Debug.LogError($"CardSpawnService: Cannot spawn pack '{definition.DisplayName}' because Pack Prefab is not assigned.");
                return null;
            }
            if (cardSettings == null)
            {
                Debug.LogError($"CardSpawnService: Cannot spawn pack '{definition.DisplayName}' because CardSettings is not assigned.");
                return null;
            }

            PackInstance newPack = UnityEngine.Object.Instantiate(packPrefab, position, Quaternion.identity);
            newPack.Initialize(definition, cardSettings);
            return newPack;
        }

        public CardDefinition CreateRecipeCardDefinition(RecipeDefinition recipe)
        {
            if (recipe == null)
            {
                Debug.LogError("CardSpawnService: Cannot create a recipe card from a null recipe.");
                return null;
            }
            if (recipeCardTemplate == null)
            {
                Debug.LogError("CardSpawnService: 'Recipe Card Template' is not assigned.");
                return null;
            }

            CardDefinition dynamicDef = UnityEngine.Object.Instantiate(recipeCardTemplate);
            dynamicDef.SetId($"{RecipeDefinitionIdPrefix}{recipe.Id}");

            if (recipe.ResultingCard != null)
                dynamicDef.SetDisplayName(GameLocalization.Format("recipe.blueprint", recipe.ResultingCard.DisplayName));
            else
                dynamicDef.SetDisplayName(GameLocalization.Get("recipe.unknown"));

            dynamicDef.SetDescription(CraftingManager.Instance?.GetFormattedIngredients(recipe));
            return dynamicDef;
        }

        public void SpawnRecipeCard(RecipeDefinition recipe, CardStack craftingStack)
        {
            if (craftingStack == null)
            {
                Debug.LogWarning("CardSpawnService: Cannot spawn a recipe card without a target crafting stack.");
                return;
            }
            var dynamicDef = CreateRecipeCardDefinition(recipe);
            if (dynamicDef == null) return;
            CreateCardInstance(dynamicDef, craftingStack.TargetPosition.Flatten(), craftingStack);
        }

        public void ReturnCardToBoard(CardInstance cardToDrop)
        {
            if (cardToDrop == null)
            {
                Debug.LogWarning("CardSpawnService: Attempted to return a null card to the board.");
                return;
            }
            var newStack = new CardStack(cardToDrop, cardToDrop.transform.position.Flatten());
            stackRegistry.Register(newStack);
            stackRegistry.ResolveOverlaps();
        }

        public void SpawnDefaultCards()
        {
            if (defaultSpawnCards == null || defaultSpawnCards.Count == 0)
            {
                Debug.LogWarning("CardSpawnService: No default spawn cards configured for a new game.");
                return;
            }
            foreach (var card in defaultSpawnCards)
            {
                if (card == null)
                {
                    Debug.LogWarning("CardSpawnService: Default spawn list contains a missing card definition.");
                    continue;
                }
                Vector3 randomPos = UnityEngine.Random.insideUnitSphere * defaultSpawnRadius;
                Vector3 spawnPos = defaultSpawnPosition + randomPos.Flatten();

                if (card is PackDefinition pack)
                    CreatePackInstance(pack, spawnPos);
                else
                    CreateCardInstance(card, spawnPos, CardStack.RefuseAll);
            }
        }
    }
}
