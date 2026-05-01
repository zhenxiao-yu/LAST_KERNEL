using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        // Stored refs so we can unsubscribe even after scene objects are destroyed.
        private ICardService    _cardSvc;
        private IQuestService   _questSvc;
        private ICraftingService _craftingSvc;
        private TradeManager    _tradeSvc;

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (transform.parent == null)
                DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            _platform = CreatePlatform(platformType);
            _platform.Initialize();

            LoadProgress();

            // Static events — safe to subscribe immediately regardless of scene.
            NightPhaseManager.OnNightComplete    += OnNightComplete;
            GameDirector.OnGameOver              += OnGameOverFired;
            UIEventBus.OnLanguageSelectRequested += OnLanguageChanged;
            UIEventBus.OnLanguageCycleRequested  += OnLanguageCycled;

            // Scene-local singletons (CardManager, QuestManager, etc.) only exist in
            // the Game scene. Subscribe when that scene is ready, clean up when it unloads.
            if (GameDirector.Instance != null)
            {
                GameDirector.Instance.OnBeforeSave    += HandleBeforeSave;
                GameDirector.Instance.OnSceneDataReady += HandleSceneDataReady;
            }

            SceneManager.sceneUnloaded += HandleSceneUnloaded;
        }

        private void OnDestroy()
        {
            NightPhaseManager.OnNightComplete    -= OnNightComplete;
            GameDirector.OnGameOver              -= OnGameOverFired;
            UIEventBus.OnLanguageSelectRequested -= OnLanguageChanged;
            UIEventBus.OnLanguageCycleRequested  -= OnLanguageCycled;

            UnsubscribeFromSceneServices();

            if (GameDirector.Instance != null)
            {
                GameDirector.Instance.OnBeforeSave     -= HandleBeforeSave;
                GameDirector.Instance.OnSceneDataReady -= HandleSceneDataReady;
            }

            SceneManager.sceneUnloaded -= HandleSceneUnloaded;
        }

        #endregion

        #region Scene Service Subscriptions

        private void HandleSceneDataReady(SceneData _, bool __)
        {
            // Called every time a game scene finishes loading — re-bind scene singletons.
            UnsubscribeFromSceneServices();
            SubscribeToSceneServices();
        }

        private void HandleSceneUnloaded(Scene _)
        {
            // Called when any scene unloads. Safe because we use stored refs, not Instance.
            UnsubscribeFromSceneServices();
        }

        private void SubscribeToSceneServices()
        {
            _cardSvc = CardManager.Instance;
            if (_cardSvc != null)
            {
                _cardSvc.OnCardCreated  += OnCardCreated;
                _cardSvc.OnCardKilled   += OnCardKilled;
                _cardSvc.OnCardEquipped += OnCardEquipped;
            }

            _questSvc = QuestManager.Instance;
            if (_questSvc != null)
                _questSvc.OnQuestCompleted += OnQuestCompleted;

            _craftingSvc = CraftingManager.Instance;
            if (_craftingSvc != null)
            {
                _craftingSvc.OnRecipeDiscovered += OnRecipeDiscovered;
                _craftingSvc.OnCraftingFinished += OnCraftingFinished;
            }

            _tradeSvc = TradeManager.Instance;
            if (_tradeSvc != null)
                _tradeSvc.OnPackOpened += OnPackOpened;
        }

        private void UnsubscribeFromSceneServices()
        {
            if (_cardSvc != null)
            {
                _cardSvc.OnCardCreated  -= OnCardCreated;
                _cardSvc.OnCardKilled   -= OnCardKilled;
                _cardSvc.OnCardEquipped -= OnCardEquipped;
                _cardSvc = null;
            }

            if (_questSvc != null)
            {
                _questSvc.OnQuestCompleted -= OnQuestCompleted;
                _questSvc = null;
            }

            if (_craftingSvc != null)
            {
                _craftingSvc.OnRecipeDiscovered -= OnRecipeDiscovered;
                _craftingSvc.OnCraftingFinished -= OnCraftingFinished;
                _craftingSvc = null;
            }

            if (_tradeSvc != null)
            {
                _tradeSvc.OnPackOpened -= OnPackOpened;
                _tradeSvc = null;
            }
        }

        #endregion

        #region Event Handlers

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

        private void OnPackOpened(PackDefinition pack)
            => EvaluateTrigger(AchievementTrigger.PackOpened, pack.Id);

        private void OnLanguageChanged(GameLanguage _)
            => EvaluateTrigger(AchievementTrigger.LanguageChanged, string.Empty);

        private void OnLanguageCycled()
            => EvaluateTrigger(AchievementTrigger.LanguageChanged, string.Empty);

        private void OnNightComplete(NightCombatResult result)
        {
            if (result.PlayerWon) NotifyNightSurvived();
        }

        private void OnGameOverFired() => NotifyGameLost();

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

        // Bypasses trigger evaluation — used by the debug window and editor tooling only.
        public void ForceUnlock(string id)
        {
            if (database == null) return;
            var def = database.GetById(id);
            if (def == null) { Debug.LogWarning($"[Achievements] ForceUnlock: no definition found for id '{id}'"); return; }
            var data = GetOrCreate(def.Id);
            if (!data.IsUnlocked) Unlock(def, data);
        }

        #endregion

        #region Core Evaluation

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

            Debug.Log($"[Achievements] Unlocked: {def.Id}");
            OnAchievementUnlocked?.Invoke(def);
        }

        #endregion

        #region Persistence

        private void LoadProgress()
        {
            var gameData = GameDirector.Instance?.GameData;
            if (gameData?.Achievements == null) return;

            _progress = new Dictionary<string, AchievementSaveData>(gameData.Achievements);

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
