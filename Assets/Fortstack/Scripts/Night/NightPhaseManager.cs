using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Markyu.FortStack
{
    /// <summary>
    /// Orchestrates the night combat slice. Called explicitly by DayCycleManager at dusk.
    ///
    /// Responsibilities:
    ///   - Build CombatUnits from the deployment plan and wave definition
    ///   - Run CombatLane simulation tick-by-tick
    ///   - Show/hide the CombatLaneView
    ///   - Apply aftermath: kill dead defender cards
    ///   - Expose LastResult for DayCycleManager to apply to RunState
    ///
    /// Explicitly does NOT:
    ///   - Own run-state or board logic
    ///   - Apply morale/fatigue/salvage (delegated to DayCycleManager → RunStateManager)
    ///   - Manage game-phase transitions
    /// </summary>
    public class NightPhaseManager : MonoBehaviour
    {
        public static NightPhaseManager Instance { get; private set; }

        [Header("Wave")]
        [SerializeField, Tooltip("The wave definition to spawn each night. Create via Right-click > LastKernel > Night Wave.")]
        private NightWaveDefinition defaultWave;

        [Header("Simulation")]
        [SerializeField, Min(0.1f), Tooltip("Seconds between simulation ticks. Lower = faster combat.")]
        private float tickInterval = 0.8f;

        [SerializeField, Min(10), Tooltip("Safety cap on tick count to prevent runaway loops.")]
        private int maxTicks = 300;

        [Header("View")]
        [SerializeField, Tooltip("Optional. Wire the CombatLaneView scene component here. Combat runs without it.")]
        private CombatLaneView laneView;

        /// <summary>Available after RunNight() coroutine completes.</summary>
        public NightCombatResult LastResult { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // ── Public entry point ────────────────────────────────────────────────────

        /// <summary>
        /// Full night sequence coroutine. Yields until combat AND aftermath are complete.
        /// DayCycleManager yields on this, then reads LastResult to update RunState.
        /// </summary>
        public IEnumerator RunNight(NightDeploymentPlan plan)
        {
            LastResult = null;

            NightWaveDefinition wave = ResolveWave();

            if (wave == null)
            {
                Debug.LogError("NightPhaseManager: No wave assigned and no fallback available. Skipping night combat.");
                LastResult = BuildSkipResult();
                yield break;
            }

            // 1. Build runtime units
            var defenderUnits = BuildDefenderUnits(plan);
            var enemyUnits = BuildEnemyUnits(wave);

            if (defenderUnits.Count == 0)
            {
                Debug.LogWarning("NightPhaseManager: No defenders deployed. Colony is undefended.");
                yield return HandleUndefendedNight(wave);
                yield break;
            }

            // 2. Build lane
            var lane = new CombatLane(defenderUnits, enemyUnits, wave);

            // 3. Activate view
            laneView?.Bind(lane);
            laneView?.Show();

            // 4. Announce night start
            yield return ShowModal(
                "Night Incursion",
                $"{wave.WaveName}\n\n{wave.FlavorText}\n\n{defenderUnits.Count} defender(s) deployed against {enemyUnits.Count} enemy unit(s).",
                "Deploy"
            );

            // 5. Run simulation
            int ticks = 0;
            while (lane.IsOngoing && ticks < maxTicks)
            {
                lane.Tick(tickInterval);
                laneView?.RefreshDisplay();
                ticks++;

                if (lane.IsOngoing)
                    yield return new WaitForSeconds(tickInterval);
            }

            if (lane.IsOngoing)
            {
                Debug.LogWarning("NightPhaseManager: Max tick limit reached, forcing end.");
                lane.ForceEnd();
                laneView?.RefreshDisplay();
            }

            // 6. Collect result
            LastResult = lane.BuildResult();

            // 7. Announce result
            yield return ShowModal(
                LastResult.PlayerWon ? "Wave Repelled" : "Defenses Breached",
                LastResult.GetSummaryText(),
                "Continue"
            );

            // 8. Hide view
            laneView?.Hide();

            // 9. Apply aftermath: kill dead defender cards on the board
            yield return ApplyAftermath(LastResult);
        }

        // ── Aftermath ─────────────────────────────────────────────────────────────

        private IEnumerator ApplyAftermath(NightCombatResult result)
        {
            foreach (var deadCard in result.DeadDefenders)
            {
                if (deadCard == null || deadCard.gameObject == null) continue;

                deadCard.Kill();
                yield return new WaitForSeconds(0.35f);
            }
        }

        // ── Build helpers ─────────────────────────────────────────────────────────

        private List<CombatUnit> BuildDefenderUnits(NightDeploymentPlan plan)
        {
            var units = new List<CombatUnit>();
            foreach (var card in plan.Defenders)
            {
                if (card != null && card.CurrentHealth > 0)
                    units.Add(CombatUnit.FromCardInstance(card));
            }
            return units;
        }

        private List<CombatUnit> BuildEnemyUnits(NightWaveDefinition wave)
        {
            var units = new List<CombatUnit>();
            foreach (var enemyDef in wave.BuildEnemyList())
            {
                units.Add(CombatUnit.FromEnemyDefinition(enemyDef));
            }
            return units;
        }

        // ── Wave resolution ───────────────────────────────────────────────────────

        private NightWaveDefinition ResolveWave()
        {
            if (defaultWave != null) return defaultWave;

            // Fallback: try loading from Resources/Waves/
            var loaded = Resources.LoadAll<NightWaveDefinition>("Waves");
            if (loaded != null && loaded.Length > 0)
            {
                Debug.LogWarning("NightPhaseManager: No wave assigned in inspector. Using first wave found in Resources/Waves/.");
                return loaded[0];
            }

            return null;
        }

        // ── Edge cases ────────────────────────────────────────────────────────────

        private IEnumerator HandleUndefendedNight(NightWaveDefinition wave)
        {
            yield return ShowModal(
                "Undefended",
                "No defenders were available. The incursion struck unchallenged.",
                "Continue"
            );

            LastResult = new NightCombatResult(
                playerWon: false,
                deadDefenders: new List<CardInstance>(),
                survivorDefenders: new List<CardInstance>(),
                totalDefenders: 0,
                enemiesKilled: 0,
                totalEnemies: wave.BuildEnemyList().Count,
                moraleDelta: wave.DefeatMoraleDelta,
                fatigueDelta: 0,
                salvageDelta: 0
            );
        }

        private NightCombatResult BuildSkipResult()
        {
            return new NightCombatResult(
                playerWon: true,
                deadDefenders: new List<CardInstance>(),
                survivorDefenders: new List<CardInstance>(),
                totalDefenders: 0,
                enemiesKilled: 0,
                totalEnemies: 0,
                moraleDelta: 1,
                fatigueDelta: 0,
                salvageDelta: 0
            );
        }

        // ── Modal helper ──────────────────────────────────────────────────────────

        private IEnumerator ShowModal(string header, string body, string buttonLabel)
        {
            if (InfoPanel.Instance == null)
            {
                Debug.LogWarning($"NightPhaseManager: InfoPanel not found, skipping modal: [{header}]");
                yield break;
            }

            bool confirmed = false;

            InfoPanel.Instance.RequestInfoDisplay(
                this,
                InfoPriority.Modal,
                (header, body),
                buttonLabel,
                () => confirmed = true
            );

            yield return new WaitUntil(() => confirmed);

            InfoPanel.Instance.ClearInfoRequest(this);
        }
    }
}
