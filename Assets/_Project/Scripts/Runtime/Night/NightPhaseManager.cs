using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Orchestrates the night combat slice. Called explicitly by DayCycleManager at dusk.
    ///
    /// Responsibilities:
    ///   - Build CombatUnits from the deployment plan and wave definition
    ///   - Run CombatLane simulation tick-by-tick
    ///   - Expose events so NightBattleHUDController can bind to the active lane
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

        // ── HUD events ────────────────────────────────────────────────────────────
        // Fired before ticks begin. HUD subscribes, shows itself, then calls ConfirmBattleStart().
        public static event Action<CombatLane, NightWaveDefinition> OnNightPrepared;
        // Fired after all ticks resolve. HUD shows result, then calls AcknowledgeResult().
        public static event Action<NightCombatResult> OnNightComplete;

        [BoxGroup("Wave")]
        [SerializeField, Tooltip("Wave definition spawned each night. Create via Right-click > LastKernel > Night Wave.")]
        private NightWaveDefinition defaultWave;

        [BoxGroup("Simulation")]
        [SerializeField, Min(0.1f), Tooltip("Seconds between simulation ticks. Lower = faster combat.")]
        private float tickInterval = 0.8f;

        [BoxGroup("Simulation")]
        [SerializeField, Min(10), Tooltip("Safety cap on tick count to prevent runaway loops.")]
        private int maxTicks = 300;

        [BoxGroup("View")]
        [SerializeField, Tooltip("Legacy uGUI combat overlay. Used only when no HUD subscribers are present.")]
        private CombatLaneView laneView;

        /// <summary>Available after RunNight() coroutine completes.</summary>
        public NightCombatResult LastResult { get; private set; }

        // Wait-state flags — set internally, cleared by HUD via the public methods below.
        private bool _awaitingBattleStart;
        private bool _awaitingResultAck;
        private bool _fastResolve;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // ── Public control API (called by NightBattleHUDController) ──────────────

        /// <summary>Call when the player clicks "Start Battle". Unblocks the tick loop.</summary>
        public void ConfirmBattleStart() => _awaitingBattleStart = false;

        /// <summary>Call when the player clicks "Return to Day". Unblocks aftermath.</summary>
        public void AcknowledgeResult() => _awaitingResultAck = false;

        /// <summary>Collapses remaining tick delay to one frame per tick — battle resolves in seconds.</summary>
        public void SetFastResolve() => _fastResolve = true;

        // ── Public entry point ────────────────────────────────────────────────────

        /// <summary>
        /// Full night sequence coroutine. Yields until combat AND aftermath are complete.
        /// DayCycleManager yields on this, then reads LastResult to update RunState.
        /// </summary>
        public IEnumerator RunNight(NightDeploymentPlan plan)
        {
            LastResult = null;
            _fastResolve = false;

            NightWaveDefinition wave = ResolveWave();

            if (wave == null)
            {
                Debug.LogError("NightPhaseManager: No wave assigned and no fallback available. Skipping night combat.");
                LastResult = BuildSkipResult();
                yield break;
            }

            var defenderUnits = BuildDefenderUnits(plan);
            var enemyUnits    = BuildEnemyUnits(wave);

            if (defenderUnits.Count == 0)
            {
                Debug.LogWarning("NightPhaseManager: No defenders deployed. Colony is undefended.");
                yield return HandleUndefendedNight(wave);
                yield break;
            }

            var lane = new CombatLane(defenderUnits, enemyUnits, wave);

            // Determine display path: rich HUD if someone subscribed, else legacy laneView + modal.
            bool hudActive = OnNightPrepared != null;

            if (hudActive)
            {
                _awaitingBattleStart = true;
                OnNightPrepared.Invoke(lane, wave);
                yield return new WaitUntil(() => !_awaitingBattleStart);
            }
            else
            {
                laneView?.Bind(lane);
                laneView?.Show();
                yield return ShowModal(
                    GameLocalization.Get("night.incursionTitle"),
                    GameLocalization.Format("night.startBody", wave.WaveName, wave.FlavorText, defenderUnits.Count, enemyUnits.Count),
                    GameLocalization.Get("ui.play")
                );
            }

            // ── Simulation tick loop ──────────────────────────────────────────────
            int ticks = 0;
            while (lane.IsOngoing && ticks < maxTicks)
            {
                lane.Tick(tickInterval);

                if (!hudActive)
                    laneView?.RefreshDisplay();

                ticks++;

                if (lane.IsOngoing)
                {
                    if (_fastResolve)
                        yield return null; // one frame — keeps UI responsive
                    else
                        yield return new WaitForSeconds(tickInterval);
                }
            }

            if (lane.IsOngoing)
            {
                Debug.LogWarning("NightPhaseManager: Max tick limit reached, forcing end.");
                lane.ForceEnd();
                if (!hudActive) laneView?.RefreshDisplay();
            }

            LastResult = lane.BuildResult();

            // ── Present result ────────────────────────────────────────────────────
            if (hudActive)
            {
                _awaitingResultAck = true;
                OnNightComplete.Invoke(LastResult);
                yield return new WaitUntil(() => !_awaitingResultAck);
            }
            else
            {
                yield return ShowModal(
                    GameLocalization.Get(LastResult.PlayerWon ? "night.resultVictory" : "night.resultDefeat"),
                    LastResult.GetSummaryText(),
                    GameLocalization.Get("ui.continue")
                );
                laneView?.Hide();
            }

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
            int day = TimeManager.Instance?.CurrentDay ?? 1;
            var units = new List<CombatUnit>();
            foreach (var enemyDef in wave.BuildEnemyListScaled(day))
                units.Add(CombatUnit.FromEnemyDefinition(enemyDef));
            return units;
        }

        // ── Wave resolution ───────────────────────────────────────────────────────

        private NightWaveDefinition ResolveWave()
        {
            if (defaultWave != null) return defaultWave;

            var loaded = Resources.LoadAll<NightWaveDefinition>("Waves");
            if (loaded != null && loaded.Length > 0)
            {
                Debug.LogWarning("NightPhaseManager: No wave assigned in inspector. Using first wave found in Resources/Waves/.");
                return loaded[0];
            }

            return BuildProceduralWave();
        }

        /// <summary>
        /// Generates a runtime wave when no NightWaveDefinition asset is configured.
        /// Scavenger count and stats scale with the current day so challenge grows
        /// without requiring hand-authored wave assets for every night.
        ///
        /// Scaling (approximate):
        ///   Day  1 → 1 Scavenger  HP 8  ATK 2  DEF 0
        ///   Day  5 → 2 Scavengers HP 14 ATK 2  DEF 0
        ///   Day  9 → 3 Scavengers HP 20 ATK 3  DEF 0
        ///   Day 14 → 4 Scavengers HP 27 ATK 4  DEF 1
        /// </summary>
        private NightWaveDefinition BuildProceduralWave()
        {
            int day   = TimeManager.Instance?.CurrentDay ?? 1;
            int hp    = Mathf.RoundToInt(6 + day * 1.5f);
            int atk   = 2 + day / 7;
            int def   = day / 10;
            int count = Mathf.Clamp(1 + (day - 1) / 4, 1, 6);

            var scavenger = EnemyDefinition.CreateRuntime(
                GameLocalization.GetOptional("night.enemy.scavenger", "Scavenger"),
                hp, atk, def,
                hpFlatPerDay: 0f,
                atkFlatPerDay: 0f);

            return NightWaveDefinition.CreateRuntime(
                GameLocalization.GetOptional("night.wave.procedural.name", "Nightly Incursion"),
                GameLocalization.GetOptional("night.wave.procedural.flavor", "Scavengers probe the perimeter under cover of dark."),
                new List<EnemyEntry> { new EnemyEntry { Enemy = scavenger, Count = count } });
        }

        // ── Edge cases ────────────────────────────────────────────────────────────

        private IEnumerator HandleUndefendedNight(NightWaveDefinition wave)
        {
            // Notify HUD if active, else fall back to modal.
            if (OnNightPrepared != null)
            {
                // Pass an empty lane so the HUD can still display the enemy lineup.
                var emptyLane = new CombatLane(new List<CombatUnit>(), BuildEnemyUnits(wave), wave);
                _awaitingBattleStart = true;
                OnNightPrepared.Invoke(emptyLane, wave);
                yield return new WaitUntil(() => !_awaitingBattleStart);
            }
            else
            {
                yield return ShowModal(
                    GameLocalization.Get("night.undefendedTitle"),
                    GameLocalization.Get("night.undefendedBody"),
                    GameLocalization.Get("ui.continue")
                );
            }

            LastResult = new NightCombatResult(
                playerWon: false,
                deadDefenders: new List<CardInstance>(),
                survivorDefenders: new List<CardInstance>(),
                totalDefenders: 0,
                enemiesKilled: 0,
                totalEnemies: BuildEnemyUnits(wave).Count,
                moraleDelta: wave.DefeatMoraleDelta,
                fatigueDelta: 0,
                salvageDelta: 0
            );

            if (OnNightComplete != null)
            {
                _awaitingResultAck = true;
                OnNightComplete.Invoke(LastResult);
                yield return new WaitUntil(() => !_awaitingResultAck);
            }
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
