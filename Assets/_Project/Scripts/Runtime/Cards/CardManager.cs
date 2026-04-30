// CardManager — Facade and public entry point for the card system.
//
// Implements ICardService and ICardEventSink as a MonoBehaviour singleton.
// All substantive logic lives in the sub-services it creates and wires:
//
//   StackRegistry          — active stack list, overlap, highlight, CanStack
//   CardSpawnService       — prefab factory, definition catalog
//   CardDiscoveryService   — discovered-card tracking, localized recipe labels
//   CardSaveRestoreService — save/load orchestration
//   FeedingSystem          — end-of-day feeding coroutine
//
// External callers (CardInstance, CardStack, UI, combat) continue using
// CardManager.Instance and the ICardService interface without change.

using System.Collections;
using System.Collections.Generic;
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
        public event System.Action<CardDefinition> OnCardEquipped;
        public event System.Action<StatsSnapshot> OnStatsChanged;
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

        private StackRegistry stackRegistry;
        private CardSpawnService spawnService;
        private CardDiscoveryService discoveryService;
        private CardSaveRestoreService saveRestoreService;
        private FeedingSystem feedingSystem;

        public IEnumerable<CardInstance> AllCards => stackRegistry.AllCards;
        public IReadOnlyList<CardStack> AllStacks => stackRegistry.AllStacks;

        #region Unity Lifecycle
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            stackRegistry = new StackRegistry(stackingMatrix, cardSettings);

            // discoveryService needs GetDefinitionById, which lives on spawnService.
            // The lambda captures the field; by the time it's invoked, spawnService is assigned.
            discoveryService = new CardDiscoveryService(id => spawnService?.GetDefinitionById(id));

            spawnService = new CardSpawnService(
                cardPrefabs, aggressiveMobPrefab, packPrefab, recipeCardTemplate,
                cardSettings, defaultSpawnCards, defaultSpawnPosition, defaultSpawnRadius,
                stackRegistry, discoveryService);

            spawnService.OnCardCreated += card =>
            {
                NotifyStatsChanged();
                OnCardCreated?.Invoke(card);
            };

            saveRestoreService = new CardSaveRestoreService(spawnService, stackRegistry, discoveryService);
            feedingSystem = new FeedingSystem(() => AllCards, cardSettings, NotifyStatsChanged);

            if (GameDirector.Instance != null)
            {
                GameDirector.Instance.OnSceneDataReady += HandleSceneDataReady;
                GameDirector.Instance.OnBeforeSave += HandleBeforeSave;
                discoveryService.RestoreFromGameData(GameDirector.Instance.GameData);
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

        #region ICardEventSink
        public void NotifyCardKilled(CardInstance card) => OnCardKilled?.Invoke(card);
        public void NotifyCardEquipped(CardDefinition card) => OnCardEquipped?.Invoke(card);
        public void NotifyStatsChanged() => OnStatsChanged?.Invoke(GetStatsSnapshot());
        #endregion

        #region ICardService — Definition & Factory
        public CardDefinition GetDefinitionById(string id) => spawnService.GetDefinitionById(id);

        public CardInstance CreateCardInstance(CardDefinition definition, Vector3 position, CardStack stackToIgnore = null)
            => spawnService.CreateCardInstance(definition, position, stackToIgnore);

        public PackInstance CreatePackInstance(PackDefinition definition, Vector3 position)
            => spawnService.CreatePackInstance(definition, position);

        public CardDefinition CreateRecipeCardDefinition(RecipeDefinition recipe)
            => spawnService.CreateRecipeCardDefinition(recipe);

        public void SpawnRecipeCard(RecipeDefinition recipe, CardStack craftingStack)
            => spawnService.SpawnRecipeCard(recipe, craftingStack);

        public CardInstance RestoreCardFromData(CardData data, Vector3 position)
            => saveRestoreService.RestoreCardFromData(data, position);

        public void RestoreTraveler(CardData cardData, Vector3 position)
            => saveRestoreService.RestoreTraveler(cardData, position);

        public void ReturnCardToBoard(CardInstance cardToDrop)
            => spawnService.ReturnCardToBoard(cardToDrop);
        #endregion

        #region ICardService — Stack Operations
        public void RegisterStack(CardStack stack) => stackRegistry.Register(stack);
        public void UnregisterStack(CardStack stack) => stackRegistry.Unregister(stack);
        public bool CanStack(CardDefinition bottom, CardDefinition top) => stackRegistry.CanStack(bottom, top);
        public void HighlightStackableStacks(CardInstance liftedCard) => stackRegistry.HighlightStackableStacks(liftedCard);
        public void TurnOffHighlightedCards() => stackRegistry.TurnOffHighlightedCards();
        public void ResolveOverlaps() => stackRegistry.ResolveOverlaps();
        public void ResolveOverlaps(CombatRect combatRect, CardStack stackToIgnore = null) => stackRegistry.ResolveOverlaps(combatRect, stackToIgnore);
        public void EnforceBoardLimits() => stackRegistry.EnforceBoardLimits();
        #endregion

        #region ICardService — Discovery & Stats
        public bool IsCardDiscovered(CardDefinition card) => discoveryService.IsDiscovered(card);
        public void MarkCardAsDiscovered(CardDefinition card) => discoveryService.MarkDiscovered(card);
        public StatsSnapshot GetStatsSnapshot() => CardStatsCalculator.Calculate(AllCards, cardSettings);
        #endregion

        #region ICardService — Game Cycles
        public IEnumerator FeedCharacters() => feedingSystem.FeedCharacters();
        #endregion

        #region Event Handlers
        private void HandleSceneDataReady(SceneData sceneData, bool wasLoaded)
            => saveRestoreService.HandleSceneDataReady(sceneData, wasLoaded);

        private void HandleBeforeSave(GameData gameData)
            => saveRestoreService.HandleBeforeSave(gameData);

        private void HandleLanguageChanged(GameLanguage _)
            => discoveryService.UpdateLocalizedRecipeCards(AllCards);
        #endregion
    }
}
