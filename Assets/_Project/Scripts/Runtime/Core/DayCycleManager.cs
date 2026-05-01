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

            // Lock board interaction for the entire dusk-to-dawn sequence.
            // InputManager only blocks camera pan/zoom and card drag (Update-driven).
            // UI EventSystem is unaffected, so the deployment panel buttons work normally.
            InputManager.Instance.AddLock(dayCycleInputLock);

            // Clear any lingering day-cycle InfoPanel message before the deployment UI opens.
            InfoPanel.Instance?.ClearInfoRequest(dayCycleRequester);

            // Collect living Character cards — eligible to defend this night.
            var eligibleDefenders = CardManager.Instance.AllCards
                .Where(c => c != null
                         && c.Definition != null
                         && c.Definition.Category == CardCategory.Character
                         && c.CurrentHealth > 0)
                .OrderBy(c => c.name)
                .ToList();

            if (NightBattleManager.Instance != null)
            {
                // PRIMARY PATH: unified Night Battle Modal (prep + shop + battle in one overlay).
                yield return NightBattleManager.Instance.RunNight(eligibleDefenders);
                RunStateManager.Instance?.ApplyNightCombatResult(NightBattleManager.Instance.LastResult);
            }
            else if (NightPhaseManager.Instance != null)
            {
                // FALLBACK PATH: legacy two-step flow (deployment panel → combat simulation).
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
                RunStateManager.Instance?.ApplyNightCombatResult(NightPhaseManager.Instance.LastResult);
            }
            else
            {
                // Fallback: legacy encounter path if NightPhaseManager is not in the scene.
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

            InputManager.Instance.RemoveLock(dayCycleInputLock);

            // Night combat may have killed the last colonists — end the run before starting a new day.
            if (CardManager.Instance.GetStatsSnapshot().TotalCharacters <= 0)
            {
                HandleGameOver();
                yield break;
            }

            PrepareForNewDay();
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

