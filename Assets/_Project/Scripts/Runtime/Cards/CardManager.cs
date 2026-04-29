// CardManager — Central card registry, factory, and board state manager.
//
// Single authority for all card instances on the board. Responsible for:
//   • Spawning, tracking, and destroying CardInstances and PackInstances
//   • Maintaining the list of all active CardStacks and their positions
//   • Enforcing stacking rules via StackingRulesMatrix
//   • Triggering physics overlap resolution when the board changes
//   • Tracking which card definitions the player has ever encountered
//   • Orchestrating the end-of-day feeding coroutine
//
// Key dependencies:
//   CraftingManager  — recipe card label generation
//   CombatManager    — AllCards includes cards in active combat
//   Board            — placement bounds enforcement
//   CardPhysicsSolver — overlap resolution algorithm
//   GameDirector     — save/load event hooks
//   GameLocalization — runtime recipe card display names
//
// NOTE: This class is a refactor candidate. Card factory logic, feeding logic,
//       and discovery tracking could each live in dedicated subsystems.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Markyu.LastKernel
{
    public class CardManager : MonoBehaviour, ICardService, ICardEventSink
    {
        #region Singleton & Events
        public static CardManager Instance { get; private set; }

        public event System.Action<CardInstance> OnCardCreated;

        public event System.Action<CardInstance> OnCardKilled;
        public void NotifyCardKilled(CardInstance card) => OnCardKilled?.Invoke(card);

        public event System.Action<CardDefinition> OnCardEquipped;
        public void NotifyCardEquipped(CardDefinition card) => OnCardEquipped?.Invoke(card);

        public event System.Action<StatsSnapshot> OnStatsChanged;
        public void NotifyStatsChanged()
        {
            currentStats = GetStatsSnapshot();
            OnStatsChanged?.Invoke(currentStats);
        }
        #endregion

        #region Serialized Fields
        [BoxGroup("Defaults")]
        [SerializeField, Tooltip("The radius within which default cards will spawn randomly around the default spawn position.")]
        private float defaultSpawnRadius = 1f;

        [BoxGroup("Defaults")]
        [SerializeField, Tooltip("The center point for where the initial set of cards will be spawned when a new game starts.")]
        private Vector3 defaultSpawnPosition = Vector3.zero;

        [BoxGroup("Defaults")]
        [SerializeField, Tooltip("A list of CardDefinitions to be instantiated when a new game session is created.")]
        private List<CardDefinition> defaultSpawnCards;

        [BoxGroup("References")]
        [SerializeField, Tooltip("The prefab used for all Card Packs (e.g., Starter Pack, Booster Pack).")]
        private PackInstance packPrefab;

        [BoxGroup("References")]
        [TableList(AlwaysExpanded = true)]
        [SerializeField, Tooltip("Maps CardCategory enums to their corresponding CardInstance prefabs.")]
        private List<CategoryEntry> cardPrefabs;

        [BoxGroup("References")]
        [SerializeField, Tooltip("Prefab used specifically for Mobs marked as Aggressive.")]
        private CardInstance aggressiveMobPrefab;

        [BoxGroup("References")]
        [SerializeField, Tooltip("The base ScriptableObject used to dynamically create temporary Recipe Card Definitions at runtime.")]
        private CardDefinition recipeCardTemplate;

        [BoxGroup("References")]
        [SerializeField, Tooltip("The ScriptableObject containing all the defined rules for which card categories can stack on top of others.")]
        private StackingRulesMatrix stackingMatrix;

        [BoxGroup("References")]
        [SerializeField, Tooltip("Reference to the global CardSettings ScriptableObject for configuration values like radii and limits.")]
        private CardSettings cardSettings;
        #endregion

        private readonly List<CardStack> stacks = new();

        // Recipe card definitions are created at runtime and cannot be stored in the
        // asset database. This prefix makes their IDs distinct from asset-based definitions
        // so GetDefinitionById can reconstruct them on demand (e.g. after a save/load).
        private const string RecipeDefinitionIdPrefix = "Recipe:";

        // Cards engaged in active combat are temporarily removed from their stacks
        // and owned by CombatManager. Including them here ensures every consumer of
        // AllCards — feeding, stats, quest checks — accounts for every living card.
        public IEnumerable<CardInstance> AllCards
        {
            get
            {
                var stackedCards = stacks.SelectMany(s => s.Cards);

                if (CombatManager.Instance == null) return stackedCards;

                var combatCards = CombatManager.Instance.ActiveCombats
                    .Where(c => c.IsOngoing)
                    .SelectMany(c => c.Attackers.Concat(c.Defenders));

                return stackedCards.Concat(combatCards);
            }
        }

        public IReadOnlyList<CardStack> AllStacks => stacks;

        private Dictionary<CardCategory, CardInstance> prefabLookup;
        private CardDefinitionCatalog definitionCatalog;

        // Cards currently showing the "stackable" highlight so they can all be
        // cleared in a single pass without iterating every card on the board.
        private List<CardInstance> highlightedCards = new();

        // Most recent snapshot cached by NotifyStatsChanged. Kept so HUD and
        // quest listeners can read the last value without triggering a recalculation.
        private StatsSnapshot currentStats;

        // Persistent across the run. Recipe-category cards are intentionally excluded
        // because they are ephemeral runtime objects, not player-discoverable content.
        private readonly HashSet<CardDefinition> discoveredCards = new();

        #region Unity Lifecycle
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializePrefabLookup();
            BuildDefinitionDatabase();

            if (GameDirector.Instance != null)
            {
                GameDirector.Instance.OnSceneDataReady += HandleSceneDataReady;
                GameDirector.Instance.OnBeforeSave += HandleBeforeSave;

                RestoreDiscoveredCards();
            }

            GameLocalization.LanguageChanged += HandleLanguageChanged;
        }

        private void OnDestroy()
        {
            if (GameDirector.Instance != null)
            {
                GameDirector.Instance.OnSceneDataReady -= HandleSceneDataReady;
                GameDirector.Instance.OnBeforeSave -= HandleBeforeSave;
            }

            GameLocalization.LanguageChanged -= HandleLanguageChanged;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(defaultSpawnPosition, defaultSpawnRadius);
        }
#endif
        #endregion

        #region Initialization
        private void InitializePrefabLookup()
        {
            prefabLookup = new Dictionary<CardCategory, CardInstance>();

            if (cardPrefabs == null || cardPrefabs.Count == 0)
            {
                Debug.LogWarning("CardManager: No card prefab mappings are assigned. Card spawning will fail until mappings are configured.", this);
                return;
            }

            foreach (var mapping in cardPrefabs)
            {
                if (mapping.prefab == null)
                {
                    Debug.LogWarning($"CardManager: Prefab for category '{mapping.category}' is missing.", this);
                    continue;
                }

                if (prefabLookup.ContainsKey(mapping.category))
                {
                    Debug.LogWarning($"CardManager: Duplicate entry for category '{mapping.category}' detected. Ignoring second entry.", this);
                }
                else
                {
                    prefabLookup.Add(mapping.category, mapping.prefab);
                }
            }
        }

        private void BuildDefinitionDatabase()
        {
            definitionCatalog = CardDefinitionCatalog.LoadFromResources();
        }

        /// <summary>
        /// Retrieves a <see cref="CardDefinition"/> based on its unique ID string.
        /// </summary>
        /// <remarks>
        /// This method handles lookup for both static definitions (loaded at startup) 
        /// and dynamic definitions (e.g., those created at runtime for Recipe Cards).
        /// </remarks>
        /// <param name="id">The unique ID string of the <see cref="CardDefinition"/>.</param>
        /// <returns>The found <see cref="CardDefinition"/>, or null if the ID is not recognized.</returns>
        public CardDefinition GetDefinitionById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                Debug.LogWarning("CardManager: Cannot look up a card definition with an empty id.", this);
                return null;
            }

            if (definitionCatalog != null && definitionCatalog.TryGetDefinition(id, out var def))
            {
                return def;
            }

            if (id.StartsWith(RecipeDefinitionIdPrefix))
            {
                string recipeId = id.Substring(RecipeDefinitionIdPrefix.Length);
                var recipe = CraftingManager.Instance?.GetRecipeById(recipeId);

                if (recipe != null)
                {
                    return CreateRecipeCardDefinition(recipe);
                }
            }

            Debug.LogError($"CardManager: Could not find CardDefinition with ID '{id}'.", this);
            return null;
        }
        #endregion

        #region Save & Load
        private void HandleSceneDataReady(SceneData sceneData, bool wasLoaded)
        {
            if (wasLoaded)
            {
                foreach (var stackData in sceneData.SavedStacks)
                {
                    RestoreStack(stackData);
                }

                ResolveOverlaps();
            }
            else
            {
                SpawnDefaultCards();
            }
        }

        private void RestoreStack(StackData stackData)
        {
            if (stackData.Cards == null || stackData.Cards.Count == 0) return;

            CardStack mainStack = null;
            Vector3 stackPos = stackData.GetPosition();

            for (int i = 0; i < stackData.Cards.Count; i++)
            {
                var cardData = stackData.Cards[i];

                CardDefinition def = GetDefinitionById(cardData.Id);

                if (def == null) continue;

                if (def is PackDefinition packDef)
                {
                    var packInstance = CreatePackInstance(packDef, stackPos);
                    packInstance.RestoreSavedStats(cardData);
                    continue;
                }

                CardInstance newCard = CreateCardInstance(def, stackPos, CardStack.RefuseAll);
                if (newCard == null)
                {
                    Debug.LogWarning($"CardManager: Failed to restore card '{cardData.Id}' in saved stack.", this);
                    continue;
                }

                newCard.RestoreSavedStats(cardData);

                if (newCard.EquipperComponent != null && cardData.EquippedItems != null && cardData.EquippedItems.Count > 0)
                {
                    RestoreEquipmentForCard(newCard, cardData);
                }

                if (i == 0)
                {
                    // The first card becomes the "anchor" of our stack.
                    mainStack = newCard.Stack;

                    // Ensure the base is locked to the saved position immediately.
                    mainStack.SetTargetPosition(stackPos, instant: true);
                }
                else
                {
                    // Subsequent cards created their own temporary stacks on Init.
                    // We must "steal" them and add them to the main stack.
                    CardStack tempStack = newCard.Stack;

                    // 1. Remove from temp stack (this unregisters the temp stack if it becomes empty).
                    tempStack.RemoveCard(newCard);

                    // 2. Add to the main reconstructed stack.
                    mainStack.AddCard(newCard);
                }
            }

            if (mainStack != null)
            {
                // Immediately align all cards in the stack to their correct visual stacking offsets.
                mainStack.SetTargetPosition(stackPos, instant: true);

                // If this stack had an active crafting task when saved, restore and resume it.
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
                var originalDef = GetDefinitionById(data.OriginalId);
                character.EquipperComponent?.SetOriginalDefinition(originalDef);
            }

            // Instantiate and equip saved items.
            foreach (var itemData in data.EquippedItems)
            {
                var itemDef = GetDefinitionById(itemData.Id);
                if (itemDef == null) continue;

                // Create the item "in the void" (Vector3.zero)
                // We temporarily create it on the board, but Equip() will immediately remove it.
                CardInstance itemCard = CreateCardInstance(itemDef, Vector3.zero, CardStack.RefuseAll);
                if (itemCard == null)
                {
                    Debug.LogWarning($"CardManager: Failed to restore equipped item '{itemData.Id}' on '{character.Definition.DisplayName}'.", this);
                    continue;
                }

                // Restore stats (durability, etc)
                itemCard.RestoreSavedStats(itemData);

                // Perform the Equip
                // Because we set OriginalDefinition above, this won't accidentally double-transform the class.
                character.EquipperComponent?.Equip(itemCard);
            }
        }

        /// <summary>
        /// Reconstructs a <see cref="CardInstance"/> from serialized <see cref="CardData"/>.
        /// </summary>
        /// <param name="data">The serialized data containing the card's state, equipment, and original ID.</param>
        /// <param name="position">The world-space position where the card should be instantiated.</param>
        /// <returns>A fully initialized <see cref="CardInstance"/>; returns <c>null</c> if the definition cannot be found.</returns>
        public CardInstance RestoreCardFromData(CardData data, Vector3 position)
        {
            string spawnId = !string.IsNullOrEmpty(data.OriginalId) ? data.OriginalId : data.Id;
            CardDefinition baseDef = GetDefinitionById(spawnId);

            if (baseDef == null) return null;
            CardInstance card = CreateCardInstance(baseDef, position, CardStack.RefuseAll);
            card.RestoreSavedStats(data);
            RestoreEquipmentForCard(card, data);

            return card;
        }

        /// <summary>
        /// Creates and initializes a single card instance using saved <see cref="CardData"/>, 
        /// restoring its basic stats, equipped items, and any class transformations it had.
        /// </summary>
        /// <param name="cardData">The saved data object containing the card's definition ID, stats, and equipment.</param>
        /// <param name="position">The world position at which to spawn the card.</param>
        public void RestoreTraveler(CardData cardData, Vector3 position)
        {
            CardDefinition def = GetDefinitionById(cardData.Id);
            if (def == null) return;

            // 1. Create the base card instance
            CardInstance newCard = CreateCardInstance(def, position, CardStack.RefuseAll);
            if (newCard == null) return;

            // 2. Restore basic stats (Health, Uses, Nutrition)
            newCard.RestoreSavedStats(cardData);

            // 3. Restore Equipment & Class Changes
            if (newCard.EquipperComponent != null &&
                cardData.EquippedItems != null &&
                cardData.EquippedItems.Count > 0)
            {
                RestoreEquipmentForCard(newCard, cardData);
            }
        }

        private void RestoreDiscoveredCards()
        {
            if (GameDirector.Instance.GameData != null)
            {
                foreach (string cardId in GameDirector.Instance.GameData.DiscoveredCards)
                {
                    var def = GetDefinitionById(cardId);
                    if (def != null && !discoveredCards.Contains(def))
                    {
                        discoveredCards.Add(def);
                    }
                }
            }
        }

        private void HandleBeforeSave(GameData gameData)
        {
            if (gameData.TryGetScene(out var sceneData))
            {
                sceneData.SaveStacks(stacks);
            }

            foreach (var cardDef in discoveredCards)
            {
                if (cardDef != null)
                {
                    gameData.DiscoveredCards.Add(cardDef.Id);
                }
            }
        }
        #endregion

        #region Card Factory
        private void SpawnDefaultCards()
        {
            if (defaultSpawnCards == null || defaultSpawnCards.Count == 0)
            {
                Debug.LogWarning("CardManager: No default spawn cards configured for a new game.", this);
                return;
            }

            foreach (var card in defaultSpawnCards)
            {
                if (card == null)
                {
                    Debug.LogWarning("CardManager: Default spawn list contains a missing card definition.", this);
                    continue;
                }

                Vector3 randomPos = Random.insideUnitSphere * defaultSpawnRadius;
                Vector3 spawnPos = defaultSpawnPosition + randomPos.Flatten();

                if (card is PackDefinition pack)
                {
                    CreatePackInstance(pack, spawnPos);
                }
                else
                {
                    CreateCardInstance(card, spawnPos, CardStack.RefuseAll);
                }
            }
        }

        /// <summary>
        /// Instantiates a new <see cref="CardInstance"/> GameObject in the world, initializes it with the given definition,
        /// and registers its resulting stack.
        /// </summary>
        /// <remarks>
        /// This factory method handles logic for discovering new cards, selecting the correct
        /// prefab (including overrides for aggressive mobs), and attaching necessary logic
        /// components (like <see cref="EnclosureLogic"/> or <see cref="ChestLogic"/>) based on the card's definition.
        /// If Friendly Mode is enabled, aggressive mobs will not be spawned.
        /// </remarks>
        /// <param name="definition">The data definition for the card to be created.</param>
        /// <param name="position">The initial world position for the card.</param>
        /// <param name="stackToIgnore">A specific stack to ignore when the new card attempts to merge into nearby stacks.</param>
        /// <returns>The newly created <see cref="CardInstance"/> object, or null if spawning failed (e.g., in Friendly Mode).</returns>
        public CardInstance CreateCardInstance(CardDefinition definition, Vector3 position, CardStack stackToIgnore = null)
        {
            if (definition == null)
            {
                Debug.LogError("CardManager: Cannot create a card instance from a null definition.", this);
                return null;
            }

            if (cardSettings == null)
            {
                Debug.LogError($"CardManager: Cannot spawn '{definition.DisplayName}' because CardSettings is not assigned.", this);
                return null;
            }

            MarkCardAsDiscovered(definition);

            bool friendlyMode = GameDirector.Instance?.GameData?.GameplayPrefs?.IsFriendlyMode ?? false;
            if (friendlyMode && definition.IsAggressive)
            {
                return null;
            }

            CardInstance prefabToSpawn = null;

            // 1. Check for specific override first
            if (definition.Category == CardCategory.Mob && definition.IsAggressive && aggressiveMobPrefab != null)
            {
                prefabToSpawn = aggressiveMobPrefab;
            }
            // 2. Fallback to standard category lookup
            else if (prefabLookup.TryGetValue(definition.Category, out var genericPrefab))
            {
                prefabToSpawn = genericPrefab;
            }

            if (prefabToSpawn == null)
            {
                Debug.LogError($"CardManager: No prefab found for category '{definition.Category}'. Cannot spawn card '{definition.DisplayName}'.", this);
                return null;
            }

            CardInstance newCard = Instantiate(prefabToSpawn, position, Quaternion.identity);
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

            NotifyStatsChanged();
            OnCardCreated?.Invoke(newCard);
            return newCard;
        }

        /// <summary>
        /// Instantiates the generic <see cref="PackInstance"/> prefab and initializes it using a specific <see cref="PackDefinition"/>.
        /// </summary>
        /// <param name="definition">The data definition for the card pack to be created.</param>
        /// <param name="position">The initial world position for the pack.</param>
        /// <returns>The newly created <see cref="PackInstance"/> object.</returns>
        public PackInstance CreatePackInstance(PackDefinition definition, Vector3 position)
        {
            if (definition == null)
            {
                Debug.LogError("CardManager: Cannot create a pack from a null definition.", this);
                return null;
            }

            if (packPrefab == null)
            {
                Debug.LogError($"CardManager: Cannot spawn pack '{definition.DisplayName}' because Pack Prefab is not assigned.", this);
                return null;
            }

            if (cardSettings == null)
            {
                Debug.LogError($"CardManager: Cannot spawn pack '{definition.DisplayName}' because CardSettings is not assigned.", this);
                return null;
            }

            PackInstance newPack = Instantiate(packPrefab, position, Quaternion.identity);
            newPack.Initialize(definition, cardSettings);
            return newPack;
        }

        /// <summary>
        /// Creates a new, temporary <see cref="CardDefinition"/> instance for a specific recipe.
        /// </summary>
        /// <remarks>
        /// This dynamic definition uses the assigned recipe card template, sets a unique
        /// ID based on the recipe's ID, and populates the display name and description
        /// with the recipe's result and required ingredients.
        /// </remarks>
        /// <param name="recipe">The <see cref="RecipeDefinition"/> containing the information for the card.</param>
        /// <returns>A dynamically created <see cref="CardDefinition"/> representing the recipe.</returns>
        public CardDefinition CreateRecipeCardDefinition(RecipeDefinition recipe)
        {
            if (recipe == null)
            {
                Debug.LogError("CardManager: Cannot create a recipe card from a null recipe.", this);
                return null;
            }

            if (recipeCardTemplate == null)
            {
                Debug.LogError("CardManager: 'Recipe Card Template' is not assigned in the Inspector.", this);
                return null;
            }

            CardDefinition dynamicDef = Instantiate(recipeCardTemplate);

            dynamicDef.SetId($"{RecipeDefinitionIdPrefix}{recipe.Id}");

            if (recipe.ResultingCard != null)
            {
                dynamicDef.SetDisplayName(GameLocalization.Format("recipe.blueprint", recipe.ResultingCard.DisplayName));
            }
            else
            {
                dynamicDef.SetDisplayName(GameLocalization.Get("recipe.unknown"));
            }

            string description = CraftingManager.Instance?.GetFormattedIngredients(recipe);
            dynamicDef.SetDescription(description);
            return dynamicDef;
        }

        /// <summary>
        /// Creates a dynamic <see cref="CardDefinition"/> for the given recipe and spawns a <see cref="CardInstance"/>
        /// representing that recipe onto the board near the crafting stack.
        /// </summary>
        /// <param name="recipe">The <see cref="RecipeDefinition"/> used to generate the card's data.</param>
        /// <param name="craftingStack">The <see cref="CardStack"/> that is currently crafting, used for positioning the new card.</param>
        public void SpawnRecipeCard(RecipeDefinition recipe, CardStack craftingStack)
        {
            if (craftingStack == null)
            {
                Debug.LogWarning("CardManager: Cannot spawn a recipe card without a target crafting stack.", this);
                return;
            }

            var dynamicDef = CreateRecipeCardDefinition(recipe);
            if (dynamicDef == null)
            {
                return;
            }

            var spawnPos = craftingStack.TargetPosition.Flatten();
            CreateCardInstance(dynamicDef, spawnPos, craftingStack);
        }

        /// <summary>
        /// Re-introduces a previously equipped card back onto the board as a new, standalone stack.
        /// </summary>
        /// <remarks>
        /// The card is placed at its current world position, a new <see cref="CardStack"/> is created and registered
        /// for it, and the CardManager's overlap resolution is triggered to ensure proper placement.
        /// This method is used, for example, when an item is unequipped.
        /// </remarks>
        /// <param name="cardToDrop">The equipment <see cref="CardInstance"/> being returned to the game board.</param>
        public void ReturnCardToBoard(CardInstance cardToDrop)
        {
            if (cardToDrop == null)
            {
                Debug.LogWarning("CardManager: Attempted to return a null card to the board.", this);
                return;
            }

            var dropPosition = cardToDrop.transform.position;
            var newStack = new CardStack(cardToDrop, dropPosition.Flatten());
            RegisterStack(newStack);
            ResolveOverlaps();
        }
        #endregion

        #region Stacking
        /// <summary>
        /// Adds a new <see cref="CardStack"/> to the manager's internal tracking list, allowing it to participate
        /// in physics resolution and global operations.
        /// </summary>
        /// <param name="stack">The <see cref="CardStack"/> to register.</param>
        public void RegisterStack(CardStack stack)
        {
            if (stack != null && !stacks.Contains(stack))
                stacks.Add(stack);
        }

        /// <summary>
        /// Removes a <see cref="CardStack"/> from the manager's tracking list.
        /// Used when a stack is fully destroyed or absorbed into another stack.
        /// </summary>
        /// <param name="stack">The <see cref="CardStack"/> to unregister.</param>
        public void UnregisterStack(CardStack stack)
        {
            if (stack != null) stacks.Remove(stack);
        }

        /// <summary>
        /// Determines if one card definition (top) can physically stack on top of another (bottom)
        /// based on the global <see cref="StackingRulesMatrix"/> configuration.
        /// </summary>
        /// <param name="bottom">The definition of the card on the bottom of the stack.</param>
        /// <param name="top">The definition of the card to be placed on top.</param>
        /// <returns>True if the stacking rule allows the placement; otherwise, false.</returns>
        public bool CanStack(CardDefinition bottom, CardDefinition top)
        {
            if (bottom == null || top == null)
            {
                return false;
            }

            if (stackingMatrix == null)
            {
                Debug.LogWarning("CardManager: Stacking matrix is not assigned; refusing stack operation.", this);
                return false;
            }

            var rule = stackingMatrix.GetRule(bottom.Category, top.Category);

            switch (rule)
            {
                case StackingRule.None:
                    return false;
                case StackingRule.CategoryWide:
                    return true;
                case StackingRule.SameDefinition:
                    return bottom == top;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Visually highlights the bottom card of all eligible CardStacks that the liftedCard
        /// is physically allowed to merge with.
        /// </summary>
        /// <param name="liftedCard">The card currently being dragged or moved.</param>
        public void HighlightStackableStacks(CardInstance liftedCard)
        {
            foreach (var stack in stacks)
            {
                if (stack.BottomCard == null) continue;

                bool canStack = CanStack(liftedCard.Definition, stack.BottomCard.Definition);
                bool sameCard = liftedCard == stack.BottomCard;
                bool sameStack = liftedCard.Stack == stack;

                if (canStack && !sameCard && !sameStack && !stack.IsCrafting)
                {
                    stack.BottomCard.SetHighlighted(true);
                    highlightedCards.Add(stack.BottomCard);
                }
            }
        }

        /// <summary>
        /// Clears the visual highlight state from all cards that were previously marked
        /// as stackable targets.
        /// </summary>
        public void TurnOffHighlightedCards()
        {
            highlightedCards.ForEach(card => card.SetHighlighted(false));
            highlightedCards.Clear();
        }

        /// <summary>
        /// Triggers the <see cref="CardPhysicsSolver"/> to detect and resolve physical overlaps 
        /// between all known card stacks and active combat areas on the board.
        /// </summary>
        public void ResolveOverlaps()
        {
            int maxIterations = cardSettings != null ? cardSettings.MaxIterations : 10;
            var combatRects = CombatManager.Instance != null
                ? CombatManager.Instance.ActiveCombatRects
                : null;

            CardPhysicsSolver.ResolveOverlaps(
                stacks,
                combatRects,
                maxIterations
            );
        }

        /// <summary>
        /// Triggers the <see cref="CardPhysicsSolver"/> to detect and resolve physical overlaps 
        /// between all known card stacks and active combat areas on the board.
        /// </summary>
        /// <param name="combatRect">A reference to an active combat area.</param>
        /// <param name="stackToIgnore">A specific stack to ignore during the overlap resolution process.</param>
        public void ResolveOverlaps(CombatRect combatRect, CardStack stackToIgnore = null)
        {
            int maxIterations = cardSettings != null ? cardSettings.MaxIterations : 10;
            var combatRects = CombatManager.Instance != null
                ? CombatManager.Instance.ActiveCombatRects
                : null;

            CardPhysicsSolver.ResolveOverlaps(
                stacks,
                combatRects,
                maxIterations
            );
        }

        /// <summary>
        /// Iterates through all unlocked CardStacks and moves any that are found to be 
        /// outside the valid board boundaries back into the playable area.
        /// </summary>
        /// <remarks>
        /// If any stack is moved, a full overlap resolution is performed afterwards.
        /// </remarks>
        public void EnforceBoardLimits()
        {
            if (Board.Instance == null) return;

            bool anyMoved = false;

            foreach (var stack in stacks)
            {
                if (stack.IsLocked) continue;

                Vector3 currentPos = stack.TargetPosition;
                Vector3 validPos = Board.Instance.EnforcePlacementRules(currentPos, stack);

                if (Vector3.SqrMagnitude(currentPos - validPos) > 0.001f)
                {
                    stack.SetTargetPosition(validPos, instant: false);
                    anyMoved = true;
                }
            }

            if (anyMoved)
            {
                ResolveOverlaps();
            }
        }
        #endregion

        #region Discovery
        /// <summary>
        /// Checks if the provided <see cref="CardDefinition"/> has been officially added to the game's set of discovered cards.
        /// </summary>
        /// <param name="card">The card definition to check.</param>
        /// <returns>True if the card is non-null and is in the discovered set; otherwise, false.</returns>
        public bool IsCardDiscovered(CardDefinition card)
        {
            return card != null && discoveredCards.Contains(card);
        }

        /// <summary>
        /// Adds a card definition to the permanent global set of discovered cards.
        /// </summary>
        /// <remarks>
        /// Recipe cards are explicitly ignored by this discovery process as they are usually temporary.
        /// </remarks>
        /// <param name="card">The definition of the card that was just encountered or created.</param>
        public void MarkCardAsDiscovered(CardDefinition card)
        {
            if (card != null && !discoveredCards.Contains(card))
            {
                if (card.Category == CardCategory.Recipe) return;
                discoveredCards.Add(card);
            }
        }

        private void HandleLanguageChanged(GameLanguage _)
        {
            UpdateLocalizedRecipeCards();
        }

        private void UpdateLocalizedRecipeCards()
        {
            foreach (var card in AllCards)
            {
                if (card == null || card.Definition == null || card.Definition.Category != CardCategory.Recipe)
                    continue;

                string definitionId = card.Definition.Id;
                if (string.IsNullOrEmpty(definitionId) || !definitionId.StartsWith(RecipeDefinitionIdPrefix))
                    continue;

                string recipeId = definitionId.Substring(RecipeDefinitionIdPrefix.Length);
                RecipeDefinition recipe = CraftingManager.Instance?.GetRecipeById(recipeId);
                if (recipe == null)
                    continue;

                if (recipe.ResultingCard != null)
                {
                    card.Definition.SetDisplayName(GameLocalization.Format("recipe.blueprint", recipe.ResultingCard.DisplayName));
                }
                else
                {
                    card.Definition.SetDisplayName(GameLocalization.Get("recipe.unknown"));
                }

                card.Definition.SetDescription(CraftingManager.Instance?.GetFormattedIngredients(recipe) ?? string.Empty);
                card.SetDefinition(card.Definition);
            }
        }
        #endregion

        #region  Game Cycles
        /// <summary>
        /// Executes the global feeding phase, where all Character cards attempt to consume
        /// available Consumable cards to satisfy their hunger.
        /// </summary>
        /// <returns>An IEnumerator to run the feeding process as a coroutine, managing animations and delays.</returns>
        // NOTE: Lives in CardManager rather than DayCycleManager because it needs direct access
        // to cardSettings (HungerPerCharacter, DragHeight) and the camera controller. Exposing
        // those through ICardService would increase the interface surface without real benefit.
        // DayCycleManager orchestrates WHEN feeding happens; CardManager handles HOW.
        public IEnumerator FeedCharacters()
        {
            var allCards = AllCards.ToList();

            var characterCards = allCards
                .Where(card => card.Definition.Category == CardCategory.Character)
                .ToList();

            var consumableCards = allCards
                .Where(card => card.Definition.Category == CardCategory.Consumable &&
                       card.CurrentNutrition > 0
                )
                .ToList();

            foreach (var character in characterCards)
            {
                if (character == null) continue;

                if (TryGetCameraController(out var cam))
                {
                    yield return cam.MoveTo(character.transform.position);
                }

                int hungerLeft = cardSettings.HungerPerCharacter;

                while (hungerLeft > 0)
                {
                    if (consumableCards.Count == 0)
                    {
                        Vector3 position = character.transform.position;
                        character.Kill();
                        break;
                    }

                    var nearestConsumable = consumableCards
                        .OrderBy(c => Vector3.Distance(character.transform.position, c.transform.position))
                        .First();

                    consumableCards.Remove(nearestConsumable);

                    yield return nearestConsumable.Consume(
                        character,
                        hungerLeft,
                        nutrition =>
                        {
                            hungerLeft -= nutrition;

                            float healFraction = (float)nutrition / cardSettings.HungerPerCharacter;
                            float healPercent = 0.5f * healFraction;
                            int maxHealth = character.Stats.MaxHealth.Value;
                            int healAmount = Mathf.RoundToInt(maxHealth * healPercent);
                            int maxPossibleHeal = maxHealth - character.CurrentHealth;
                            healAmount = Mathf.Min(healAmount, maxPossibleHeal);

                            character.Heal(healAmount);
                        }
                    );

                    if (nearestConsumable != null && nearestConsumable.CurrentNutrition > 0)
                    {
                        consumableCards.Add(nearestConsumable);
                    }

                    NotifyStatsChanged();
                }

                yield return new WaitForSecondsRealtime(0.5f);
            }
        }
        #endregion

        #region Stats Snapshot
        /// <summary>
        /// Calculates and returns a comprehensive snapshot of the current global game statistics.
        /// </summary>
        /// <remarks>
        /// This includes totals for nutrition, currency, cards owned, character count, 
        /// and the calculation of the dynamic card limit and any excess cards.
        /// </remarks>
        /// <returns>A <see cref="StatsSnapshot"/> struct containing the current state of tracked resources and limits.</returns>
        public StatsSnapshot GetStatsSnapshot()
        {
            return CardStatsCalculator.Calculate(AllCards, cardSettings);
        }

        private bool TryGetCameraController(out CameraController cameraController)
        {
            cameraController = null;

            Camera mainCamera = Camera.main;
            if (mainCamera == null || mainCamera.transform.parent == null)
            {
                return false;
            }

            return mainCamera.transform.parent.TryGetComponent(out cameraController);
        }
        #endregion
    }

}

