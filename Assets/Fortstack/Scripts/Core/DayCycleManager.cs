using System.Collections;
using System.Linq;
using UnityEngine;

namespace Markyu.FortStack
{
    public class DayCycleManager : MonoBehaviour
    {
        public static DayCycleManager Instance { get; private set; }

        public bool IsEndingCycle { get; private set; }

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
        private void StartSellingPhase()
        {
            // 1. The player MUST be able to interact to sell cards.
            InputManager.Instance.RemoveLock(dayCycleInputLock);

            // 2. Subscribe to the event. We will now react ONLY when cards change.
            CardManager.Instance.OnStatsChanged += OnStatsChangedDuringSelling;

            // 3. Manually trigger the check once immediately.
            //    This handles the case where we might already have 0 excess cards.
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

            if (NightPhaseManager.Instance != null)
            {
                // Build deployment plan from all living character cards
                var colonyCards = CardManager.Instance.AllCards
                    .Where(c => c != null)
                    .ToList();

                var plan = NightDeploymentPlan.BuildAutomatic(colonyCards);

                // Run night combat (blocks until combat + aftermath are fully done)
                yield return NightPhaseManager.Instance.RunNight(plan);

                // Apply run-state consequences from the combat result
                RunStateManager.Instance?.ApplyNightCombatResult(NightPhaseManager.Instance.LastResult);
            }
            else
            {
                // Fallback: legacy encounter path if NightPhaseManager is not in the scene
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

