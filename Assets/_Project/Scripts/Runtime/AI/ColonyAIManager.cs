// ColonyAIManager — Global colony brain.  Optional scene component.
//
// Runs a planning tick every PlanningInterval seconds and assigns the best
// available AIJob to each free villager.
//
// OPTIONAL: If this component is NOT in the scene, each VillagerBrain falls back
// to standalone mode and self-generates jobs using AIPlanner directly.  The game
// plays correctly either way; this manager just produces smarter, coordinated behaviour.
//
// Add to scene:
//   Create a GameObject in your main game scene, add ColonyAIManager.
//   Assign a VillagerAISettings asset in the Inspector.
//
// Planning tick (per PlanningInterval):
//   1. Build ColonyStateSnapshot     — read all cards, stacks, resources, phase.
//   2. Clean stale reservations       — release locked / dead villager claims.
//   3. Determine colony goal          — for debug display only.
//   4. For each available worker
//        a. Skip if already has a valid active job.
//        b. Generate candidate jobs via AIPlanner.
//        c. Assign the top-scoring job.

using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Markyu.LastKernel
{
    [DisallowMultipleComponent]
    public class ColonyAIManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        public static ColonyAIManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────
        [BoxGroup("Settings")]
        [SerializeField, Required,
         Tooltip("ScriptableObject with all colony AI tuning values.  Create via: " +
                 "Assets → Create → Last Kernel → AI → Villager AI Settings.")]
        private VillagerAISettings settings;

        public VillagerAISettings Settings => settings;

        // ── Debug (read-only in inspector) ────────────────────────────────────
        [BoxGroup("Debug"), ReadOnly, ShowInInspector,
         Tooltip("Current top-level colony objective.")]
        public string CurrentColonyGoal { get; private set; } = "Waiting for first tick…";

        [BoxGroup("Debug"), ReadOnly, ShowInInspector,
         Tooltip("Number of reserved cards and stacks this tick.")]
        private string ReservationSummary =>
            $"Cards: {AIReservationSystem.Instance.ReservedCardCount}  " +
            $"Stacks: {AIReservationSystem.Instance.ReservedStackCount}";

        [BoxGroup("Debug"), ReadOnly, ShowInInspector,
         Tooltip("Current job description per villager.")]
        private readonly List<string> _debugJobLines = new();

        // ── Internal ──────────────────────────────────────────────────────────
        private float _lastPlanningTime = -999f;

        // The most recent snapshot — stored for Gizmo drawing and external debug queries.
        public ColonyStateSnapshot LastSnapshot { get; private set; }

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                // Clear all reservations so a fresh session starts clean.
                AIReservationSystem.Instance.Reset();
            }
        }

        private void Update()
        {
            if (settings == null || !settings.EnableColonyAutopilot) return;

            // Never plan during the end-of-day modal pipeline.
            if (DayCycleManager.Instance != null && DayCycleManager.Instance.IsEndingCycle) return;

            if (Time.time - _lastPlanningTime >= settings.PlanningInterval)
            {
                _lastPlanningTime = Time.time;
                RunPlanningTick();
            }
        }

        // ── Planning ──────────────────────────────────────────────────────────

        private void RunPlanningTick()
        {
            if (CardManager.Instance == null) return;

            var snapshot  = ColonyStateSnapshot.Build(settings);
            LastSnapshot  = snapshot;

            CleanupStaleReservations();
            CurrentColonyGoal = DetermineColonyGoal(snapshot);

            _debugJobLines.Clear();
            int actionsThisTick = 0;

            foreach (var worker in snapshot.AvailableWorkers)
            {
                if (actionsThisTick >= settings.MaxActionsPerPlanningTick) break;

                var brain = worker.Brain;
                if (brain == null)
                {
                    _debugJobLines.Add($"{worker.gameObject.name}: no brain (not a villager?)");
                    continue;
                }

                // Skip workers that already have a valid, active non-idle job.
                bool hasActiveJob = brain.CurrentJob != null
                                 && brain.CurrentJob.Type != AIJobType.Idle
                                 && !brain.CurrentJob.IsCanceled
                                 && !brain.CurrentJob.IsComplete;
                if (hasActiveJob)
                {
                    _debugJobLines.Add($"{worker.gameObject.name}: {brain.DebugDescription}");
                    continue;
                }

                // Generate candidate jobs and assign the best one.
                var jobs = AIPlanner.GenerateJobsForVillager(worker, snapshot, settings);
                var best = jobs.Count > 0 ? jobs[0] : null;

                if (best != null)
                {
                    brain.AssignJob(best);
                    actionsThisTick++;
                    _debugJobLines.Add(
                        $"{worker.gameObject.name}: → {best.Description}  " +
                        $"(score {best.Score:F1}, urgency {best.Urgency:F2})");
                }
                else
                {
                    _debugJobLines.Add($"{worker.gameObject.name}: no job available");
                }
            }

            // Log a compact summary at most once every 10 seconds to avoid console spam.
            if (Application.isEditor && Time.time % 10f < settings.PlanningInterval)
            {
                if (_debugJobLines.Count > 0)
                    Debug.Log($"[ColonyAI] {CurrentColonyGoal}\n"
                            + string.Join("\n", _debugJobLines));
            }
        }

        // ── Stale reservation cleanup ─────────────────────────────────────────
        // Release reservations from villagers that are now locked, in combat, or
        // being dragged — they can no longer pursue their jobs.
        private void CleanupStaleReservations()
        {
            if (CardManager.Instance == null) return;
            foreach (var card in CardManager.Instance.AllCards)
            {
                if (card == null) continue;
                var ai        = card.GetComponent<CardAI>();
                var combatant = card.GetComponent<CardCombatant>();
                if (ai == null) continue;

                bool shouldRelease = ai.IsLocked
                                  || card.IsBeingDragged
                                  || (combatant != null && combatant.IsInCombat);

                if (shouldRelease)
                {
                    AIReservationSystem.Instance.ReleaseAll(ai);
                    ai.Brain?.CancelCurrentJob();
                }
            }
        }

        // ── Colony goal ───────────────────────────────────────────────────────
        // Produces a short human-readable string describing the colony's current
        // most-pressing situation.  Used only for debug display.
        private string DetermineColonyGoal(ColonyStateSnapshot snapshot)
        {
            if (snapshot.IsFoodCritical)
            {
                float daysLeft = snapshot.Stats.NutritionNeed > 0
                    ? (float)snapshot.Stats.TotalNutrition / snapshot.Stats.NutritionNeed
                    : 0f;
                return $"⚠ CRITICAL: Food at {snapshot.FoodRatio * 100f:F0}% — produce food now";
            }
            if (snapshot.IsCardCapPressure)
                return $"⚠ Card cap at {snapshot.CardCapRatio * 100f:F0}% — sell / discard excess";
            if (snapshot.IsNightApproaching)
                return $"Night approaching (day {snapshot.CurrentDay}) — prepare defences";
            if (snapshot.AvailableWorkers.Count == 0)
                return $"All {snapshot.Villagers.Count} villagers busy";

            // Count how many crafting opportunities exist right now.
            int craftable = snapshot.IdleStacks
                .Count(s => s.Cards.Count >= 2);
            return craftable > 0
                ? $"Day {snapshot.CurrentDay} — crafting ({craftable} active stacks)"
                : $"Day {snapshot.CurrentDay} — optimising colony";
        }

        // ── Gizmos ────────────────────────────────────────────────────────────
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (LastSnapshot == null || LastSnapshot.Villagers.Count == 0) return;

            // Draw a cyan sphere at the colony centre.
            Vector3 center = Vector3.zero;
            foreach (var v in LastSnapshot.Villagers) center += v.transform.position;
            center /= LastSnapshot.Villagers.Count;

            Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
            Gizmos.DrawWireSphere(center, 0.5f);

            // Draw a line from each villager to its current job target (if any).
            foreach (var worker in LastSnapshot.AvailableWorkers)
            {
                var brain = worker.Brain;
                if (brain?.CurrentJob == null) continue;

                Vector3 workerPos = worker.transform.position;
                Vector3 targetPos = brain.CurrentJob.DestinationStack?.TargetPosition
                                 ?? brain.CurrentJob.SourceStack?.TargetPosition
                                 ?? brain.CurrentJob.TargetPosition;

                if (targetPos == Vector3.zero) continue;

                Gizmos.color = brain.CurrentJob.Type == AIJobType.Idle
                    ? new Color(0.5f, 0.5f, 0.5f, 0.3f)
                    : new Color(0f, 1f, 0.3f, 0.7f);

                Gizmos.DrawLine(workerPos, targetPos);
                Gizmos.DrawWireSphere(targetPos, 0.15f);
            }
        }
#endif
    }
}
