using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Markyu.LastKernel.Achievements
{
    public class AchievementService : MonoBehaviour, IAchievementService
    {
        public static AchievementService Instance { get; private set; }

        public event System.Action<AchievementDefinition> OnAchievementUnlocked;

        [SerializeField, Required]
        private AchievementDatabase database;

        [SerializeField]
        private AchievementPlatformType platformType = AchievementPlatformType.InGame;

        private IAchievementPlatform _platform;
        private Dictionary<string, AchievementSaveData> _progress = new();

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            // If parented under GameDirector it's already DDOL — only call on root objects.
            if (transform.parent == null)
                DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            _platform = CreatePlatform(platformType);
            _platform.Initialize();

            LoadProgress();
            SubscribeToServices();

            if (GameDirector.Instance != null)
                GameDirector.Instance.OnBeforeSave += HandleBeforeSave;
        }

        private void OnDestroy()
        {
            UnsubscribeFromServices();
            if (GameDirector.Instance != null)
                GameDirector.Instance.OnBeforeSave -= HandleBeforeSave;
        }

        #endregion

        #region IAchievementService

        public bool IsUnlocked(AchievementDefinition definition)
            => _progress.TryGetValue(definition.Id, out var d) && d.IsUnlocked;

        public float GetProgressNormalized(AchievementDefinition definition)
        {
            if (!_progress.TryGetValue(definition.Id, out var d)) return 0f;
            return definition.TargetCount <= 1
                ? (d.IsUnlocked ? 1f : 0f)
                : Mathf.Clamp01((float)d.CurrentProgress / definition.TargetCount);
        }

        public int GetProgressCount(AchievementDefinition definition)
            => _progress.TryGetValue(definition.Id, out var d) ? d.CurrentProgress : 0;

        public void NotifyCustom(string achievementId)
            => EvaluateTrigger(AchievementTrigger.Custom, achievementId);

        public void NotifyNightSurvived()
            => EvaluateTrigger(AchievementTrigger.NightSurvived, string.Empty);

        public void NotifyDayReached(int day)
            => EvaluateTrigger(AchievementTrigger.DayReached, string.Empty, value: day);

        public void NotifyGameWon()
            => EvaluateTrigger(AchievementTrigger.GameWon, string.Empty);

        public void NotifyGameLost()
            => EvaluateTrigger(AchievementTrigger.GameLost, string.Empty);

        public void NotifyCardDiscovered(string cardId)
            => EvaluateTrigger(AchievementTrigger.CardDiscovered, cardId);

        #endregion

        #region Service Event Subscriptions

        private void SubscribeToServices()
        {
            if (CardManager.Instance is ICardService cards)
            {
                cards.OnCardCreated  += OnCardCreated;
                cards.OnCardKilled   += OnCardKilled;
                cards.OnCardEquipped += OnCardEquipped;
            }

            if (QuestManager.Instance is IQuestService quests)
                quests.OnQuestCompleted += OnQuestCompleted;

            if (CraftingManager.Instance is ICraftingService crafting)
            {
                crafting.OnRecipeDiscovered += OnRecipeDiscovered;
                crafting.OnCraftingFinished += OnCraftingFinished;
            }
        }

        private void UnsubscribeFromServices()
        {
            if (CardManager.Instance is ICardService cards)
            {
                cards.OnCardCreated  -= OnCardCreated;
                cards.OnCardKilled   -= OnCardKilled;
                cards.OnCardEquipped -= OnCardEquipped;
            }

            if (QuestManager.Instance is IQuestService quests)
                quests.OnQuestCompleted -= OnQuestCompleted;

            if (CraftingManager.Instance is ICraftingService crafting)
            {
                crafting.OnRecipeDiscovered -= OnRecipeDiscovered;
                crafting.OnCraftingFinished -= OnCraftingFinished;
            }
        }

        private void OnCardCreated(CardInstance card)
            => EvaluateTrigger(AchievementTrigger.CardCreated, card.Definition.Id);

        private void OnCardKilled(CardInstance card)
            => EvaluateTrigger(AchievementTrigger.CardKilled, card.Definition.Id);

        private void OnCardEquipped(CardDefinition def)
            => EvaluateTrigger(AchievementTrigger.CardEquipped, def.Id);

        private void OnQuestCompleted(QuestInstance quest)
            => EvaluateTrigger(AchievementTrigger.QuestCompleted, quest.QuestData.Id);

        private void OnRecipeDiscovered(string recipeId)
            => EvaluateTrigger(AchievementTrigger.RecipeDiscovered, recipeId);

        private void OnCraftingFinished(CardDefinition def)
            => EvaluateTrigger(AchievementTrigger.RecipeCrafted, def.Id);

        #endregion

        #region Core Evaluation

        // value: for Cumulative = amount to add; for Threshold = the current absolute value to compare.
        private void EvaluateTrigger(AchievementTrigger trigger, string filterId, int value = 1)
        {
            if (database == null) return;

            foreach (var def in database.All)
            {
                if (def == null || def.Trigger != trigger) continue;
                if (IsUnlocked(def)) continue;
                if (!string.IsNullOrEmpty(def.FilterId) && def.FilterId != filterId) continue;

                var data = GetOrCreate(def.Id);

                if (def.CountMode == AchievementCountMode.Threshold)
                    data.CurrentProgress = Mathf.Max(data.CurrentProgress, value);
                else
                    data.CurrentProgress += value;

                if (data.CurrentProgress >= def.TargetCount)
                    Unlock(def, data);
                else if (def.ShowProgressBar)
                    _platform.SetProgress(def.Id, data.CurrentProgress, def.TargetCount);
            }
        }

        private void Unlock(AchievementDefinition def, AchievementSaveData data)
        {
            data.IsUnlocked = true;
            data.CurrentProgress = def.TargetCount;
            data.UnlockTimestampTicks = System.DateTime.UtcNow.Ticks;

            _platform.Unlock(def.Id);
            _platform.StoreStats();

            OnAchievementUnlocked?.Invoke(def);
        }

        #endregion

        #region Persistence

        private void LoadProgress()
        {
            var gameData = GameDirector.Instance?.GameData;
            if (gameData?.Achievements == null) return;

            _progress = new Dictionary<string, AchievementSaveData>(gameData.Achievements);

            // Re-push unlocked state to platform (handles reinstalls / account changes).
            foreach (var kvp in _progress)
            {
                if (kvp.Value.IsUnlocked)
                    _platform.Unlock(kvp.Key);
            }
        }

        private void HandleBeforeSave(GameData gameData)
        {
            gameData.Achievements = new Dictionary<string, AchievementSaveData>(_progress);
        }

        private AchievementSaveData GetOrCreate(string id)
        {
            if (!_progress.TryGetValue(id, out var data))
            {
                data = new AchievementSaveData();
                _progress[id] = data;
            }
            return data;
        }

        #endregion

        #region Platform Factory

        private static IAchievementPlatform CreatePlatform(AchievementPlatformType type) => type switch
        {
            AchievementPlatformType.Steam => new SteamAchievementPlatform(),
            AchievementPlatformType.Epic  => new EpicAchievementPlatform(),
            _                             => new InGameAchievementPlatform(),
        };

        #endregion

#if UNITY_EDITOR
        [Button("Debug — Unlock All"), BoxGroup("Debug")]
        private void DebugUnlockAll()
        {
            foreach (var def in database.All)
            {
                if (def == null) continue;
                var data = GetOrCreate(def.Id);
                if (!data.IsUnlocked) Unlock(def, data);
            }
        }

        [Button("Debug — Reset All"), BoxGroup("Debug")]
        private void DebugResetAll() => _progress.Clear();
#endif
    }
}
