// DayCycleManager — Orchestrates the end-of-day phase sequence.
//
// Drives a linear coroutine pipeline that runs when TimeManager fires OnDayEnded:
//
//   Phase 1 — Notification  : "Day N ended" modal, player dismisses to continue
//   Phase 2 — Feeding       : CardManager.FeedCharacters(); game over if no survivors
//   Phase 3 — Selling       : Player sells excess cards; event-driven, not a coroutine
//   Phase 4 — Night Combat  : NightPhaseManager runs deployment + combat simulation
//   Phase 5 — New Day       : Dawn modal; unlocks input; advances TimeManager day count
//
// Key dependencies:
//   CardManager          — FeedCharacters, GetStatsSnapshot, OnStatsChanged
//   TimeManager          — OnDayEnded subscription, StartNewDay
//   InputManager         — board lock/unlock around each phase
//   NightPhaseManager    — full night combat pipeline
//   RunStateManager      — tracks current game phase (Day/Dusk/Night/Dawn)
//   InfoPanel            — modal dialog display for each phase message
//   GameDirector         — SaveGame on new-day confirmation
//
// IMPORTANT: IsEndingCycle must remain true for the entire duration of the pipeline.
//            GameDirector.OnApplicationQuit reads it to skip the autosave mid-cycle,
//            preventing a corrupt save with half-resolved feeding or combat state.

using System.Collections;
using System.Linq;
using UnityEngine;

namespace Markyu.LastKernel
{
    public class DayCycleManager : MonoBehaviour
    {
        public static DayCycleManager Instance { get; private set; }

        public bool IsEndingCycle { get; private set; }

        // Stable reference-type tokens used as identifiers with InfoPanel and InputManager.
        // Passing an object reference (rather than a string literal) avoids allocation on
        // repeated calls and guarantees uniqueness — no other system can accidentally hold
        // the same token and clear our lock or info panel entry.
        private readonly object dayCycleRequester = "DayCycleRequester";
        private readonly object dayCycleInputLock = "DayCycleInputLock";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnDayEnded += HandleDayEnded;
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnDayEnded -= HandleDayEnded;

            if (CardManager.Instance != null)
                CardManager.Instance.OnStatsChanged -= OnStatsChangedDuringSelling;
        }

        private void HandleDayEnded(int day)
        {
            RunStateManager.Instance?.SetPhase(GamePhase.Dusk);
            RunStateManager.Instance?.ApplyDuskPressure(CardManager.Instance.GetStatsSnapshot());

            InputManager.Instance.AddLock(dayCycleInputLock);
            IsEndingCycle = true;
            StartCoroutine(NotificationPhase(day));
        }

        // --- PHASE 1: NOTIFICATION ---
        private IEnumerator NotificationPhase(int day)
        {
            InfoPanel.Instance?.RequestInfoDisplay(
                dayCycleRequester,
                InfoPriority.Modal,
                (GameLocalization.Format("daycycle.dayEndedTitle", day), GameLocalization.Get("daycycle.dayEndedBody")),
                GameLocalization.Get("daycycle.dayEndedAction"),
                () =>
                {
                    StartCoroutine(FeedingPhase());
                }
            );
            yield break;
        }

        // --- PHASE 2: FEEDING ---
        private IEnumerator FeedingPhase()
        {
            InfoPanel.Instance?.RequestInfoDisplay(
                dayCycleRequester,
                InfoPriority.Modal,
                (GameLocalization.Get("daycycle.feedingTitle"), GameLocalization.Get("daycycle.feedingBody"))
            );

            // Wait for the card animations and logic to finish
            yield return CardManager.Instance.FeedCharacters();

            // Check if anyone survived
            var stats = CardManager.Instance.GetStatsSnapshot();
            if (stats.TotalCharacters <= 0)
            {
                HandleGameOver();
            }
            else
            {
                // Transition to Selling Phase
                StartSellingPhase();
            }
        }

        // --- PHASE 3: SELLING ---
        // The selling phase is event-driven rather than a polling coroutine: we subscribe to
        // OnStatsChanged and react the instant the player sells or discards a card. This keeps
        // the modal prompt in sync without a per-frame conditional check in Update.
        private void StartSellingPhase()
        {
            // Unlock board interaction so the player can drag and sell cards.
            InputManager.Instance.RemoveLock(dayCycleInputLock);

            CardManager.Instance.OnStatsChanged += OnStatsChangedDuringSelling;

            // Trigger immediately in case the player is already under the card limit
            // (e.g., characters died during feeding and the count already dropped).
            CheckSellingCondition(CardManager.Instance.GetStatsSnapshot());
        }

        private void OnStatsChangedDuringSelling(StatsSnapshot stats)
        {
            CheckSellingCondition(stats);
        }

        private void CheckSellingCondition(StatsSnapshot stats)
        {
            int excess = stats.ExcessCards;

            if (excess <= 0)
            {
                // CONDITION MET: We are done selling.

                // 1. Unsubscribe immediately so this doesn't fire again during gameplay.
                CardManager.Instance.OnStatsChanged -= OnStatsChangedDuringSelling;

                // 2. Proceed to next phase.
                StartCoroutine(EncounterPhase());
            }
            else
            {
                // CONDITION NOT MET: Update UI.
                InfoPanel.Instance?.RequestInfoDisplay(
                    dayCycleRequester,
                    InfoPriority.Modal,
                    (
                        GameLocalization.Get("daycycle.cleanupTitle"),
                        GameLocalization.Format("daycycle.cleanupBody", excess)
                    )
                );
            }
        }

        // --- PHASE 4: NIGHT COMBAT ---
        private IEnumerator EncounterPhase()
        {
            RunStateManager.Instance?.SetPhase(GamePhase.Night);

            InputManager.Instance.AddLock(dayCycleInputLock);
            InfoPanel.Instance?.ClearInfoRequest(dayCycleRequester);

            var eligibleDefenders = CardManager.Instance.AllCards
                .Where(c => c != null
                         && c.Definition != null
                         && c.Definition.Category == CardCategory.Character
                         && c.CurrentHealth > 0)
                .OrderBy(c => c.name)
                .ToList();

            NightCombatResult nightResult = null;
            bool primaryNightBattleHandledRewards = false;

            if (NightBattleManager.Instance != null)
            {
                yield return NightBattleManager.Instance.RunNight(eligibleDefenders);
                nightResult = NightBattleManager.Instance.LastResult;
                RunStateManager.Instance?.ApplyNightCombatResult(nightResult);
                primaryNightBattleHandledRewards = true;
            }
            else if (NightPhaseManager.Instance != null)
            {
                NightDeploymentPlan plan;

                if (NightDeploymentController.Instance != null)
                {
                    yield return NightDeploymentController.Instance.RunDeploymentPhase(eligibleDefenders);
                    plan = NightDeploymentController.Instance.ConfirmedPlan
                        ?? NightDeploymentPlan.BuildAutomatic(eligibleDefenders);
                }
                else
                {
                    Debug.LogWarning("DayCycleManager: NightDeploymentController not found. " +
                                     "Auto-deploying all eligible defenders.");
                    plan = NightDeploymentPlan.BuildAutomatic(eligibleDefenders);
                }

                yield return NightPhaseManager.Instance.RunNight(plan);
                nightResult = NightPhaseManager.Instance.LastResult;
                RunStateManager.Instance?.ApplyNightCombatResult(nightResult);
            }
            else
            {
                Debug.LogWarning("DayCycleManager: NightPhaseManager not found. Running legacy encounter fallback.");

                int currentDay = TimeManager.Instance.CurrentDay;
                var encounter = EncounterManager.Instance.GetBestEncounter(currentDay);
                bool hostileContact = encounter != null &&
                    encounter.CardToSpawn != null &&
                    encounter.CardToSpawn.IsAggressive;

                if (encounter != null)
                    yield return EncounterManager.Instance.ExecuteEncounter(encounter);

                RunStateManager.Instance?.RecordNightContact(hostileContact);
            }

            // Legacy night flow still converts salvage into coins. The unified night battle
            // manager handles its own choose-one-card reward before returning control here.
            if (!primaryNightBattleHandledRewards &&
                nightResult != null &&
                nightResult.PlayerWon &&
                nightResult.SalvageDelta > 0)
            {
                yield return SpawnSalvageRewards(nightResult.SalvageDelta);
            }

            InputManager.Instance.RemoveLock(dayCycleInputLock);

            // All colonists dead → hard game over.
            if (CardManager.Instance.GetStatsSnapshot().TotalCharacters <= 0)
            {
                HandleGameOver();
                yield break;
            }

            // Morale collapse → colony abandons the run.
            if (RunStateManager.Instance != null && RunStateManager.Instance.State.Morale <= 0)
            {
                HandleMoraleCollapse();
                yield break;
            }

            // Defeat but colony survived — show consequence panel so player understands their situation.
            if (nightResult != null && !nightResult.PlayerWon)
                yield return ShowDefeatConsequencePanel(nightResult);

            PrepareForNewDay();
        }

        private IEnumerator SpawnSalvageRewards(int salvageDelta)
        {
            var currency = TradeManager.Instance?.CurrencyCard;
            if (currency == null || CardManager.Instance == null) yield break;

            var anchor = CardManager.Instance.AllCards
                .FirstOrDefault(c => c?.Definition?.Category == CardCategory.Character && c.CurrentHealth > 0);
            Vector3 center = anchor != null ? anchor.transform.position : Vector3.zero;

            int count = Mathf.Min(salvageDelta, 8);
            for (int i = 0; i < count; i++)
            {
                Vector3 offset = new Vector3(
                    UnityEngine.Random.Range(-2f, 2f), 0f, UnityEngine.Random.Range(-2f, 2f));
                CardManager.Instance.CreateCardInstance(currency, center + offset);
                AudioManager.Instance?.PlaySFX(AudioId.Coin);
                yield return new WaitForSeconds(0.12f);
            }
        }

        private void HandleMoraleCollapse()
        {
            InputManager.Instance.AddLock(dayCycleInputLock);
            InfoPanel.Instance?.RequestInfoDisplay(
                dayCycleRequester,
                InfoPriority.Modal,
                (GameLocalization.GetOptional("daycycle.moraleCollapseTitle", "Colony Abandoned"),
                 GameLocalization.GetOptional("daycycle.moraleCollapseBody",
                    "Morale has reached zero. The colony has dispersed — nothing remains worth defending.")),
                GameLocalization.GetOptional("daycycle.gameOverAction", "End Run"),
                () => GameDirector.Instance.GameOver()
            );
        }

        private IEnumerator ShowDefeatConsequencePanel(NightCombatResult result)
        {
            int morale = RunStateManager.Instance?.State.Morale ?? 0;
            bool dismissed = false;
            InfoPanel.Instance?.RequestInfoDisplay(
                dayCycleRequester,
                InfoPriority.Modal,
                (GameLocalization.GetOptional("daycycle.nightDefeatTitle", "Colony Attacked"),
                 GameLocalization.GetOptional("daycycle.nightDefeatBody",
                    $"The perimeter was breached. Morale is now {morale}. Rebuild your defenses before the next incursion.")),
                GameLocalization.GetOptional("daycycle.nightDefeatAction", "Rebuild"),
                () => dismissed = true
            );
            yield return new WaitUntil(() => dismissed);
            InfoPanel.Instance?.ClearInfoRequest(dayCycleRequester);
        }

        // --- PHASE 5: NEW DAY ---
        private void PrepareForNewDay()
        {
            RunStateManager.Instance?.SetPhase(GamePhase.Dawn);
            RunStateManager.Instance?.ApplyDawnRecovery(CardManager.Instance.GetStatsSnapshot());

            InputManager.Instance.AddLock(dayCycleInputLock);

            int nextDay = TimeManager.Instance.CurrentDay + 1;

            InfoPanel.Instance?.RequestInfoDisplay(
                dayCycleRequester,
                InfoPriority.Modal,
                (GameLocalization.Format("daycycle.dayStartedTitle", nextDay), GameLocalization.Get("daycycle.dayStartedBody")),
                GameLocalization.Get("daycycle.dayStartedAction"),
                () =>
                {
                    IsEndingCycle = false;
                    InfoPanel.Instance?.ClearInfoRequest(dayCycleRequester);
                    InputManager.Instance.RemoveLock(dayCycleInputLock);
                    TimeManager.Instance.StartNewDay();
                    RunStateManager.Instance?.SetPhase(GamePhase.Day);
                    GameDirector.Instance.SaveGame();
                }
            );
        }

        private void HandleGameOver()
        {
            InputManager.Instance.AddLock(dayCycleInputLock);

            InfoPanel.Instance?.RequestInfoDisplay(
                dayCycleRequester,
                InfoPriority.Modal,
                (GameLocalization.Get("daycycle.gameOverTitle"), GameLocalization.Get("daycycle.gameOverBody")),
                GameLocalization.Get("daycycle.gameOverAction"),
                () => GameDirector.Instance.GameOver()
            );
        }
    }
}

