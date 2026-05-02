using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Orchestrates the unified Night Battle Modal flow:
    ///   1. Resolve wave + pick shop items
    ///   2. Fire OnNightModalOpened → NightBattleModalController shows the modal
    ///   3. Wait for player to finish prep and click "Start Battle" (ConfirmBattle)
    ///   4. Build CombatLane from the confirmed NightTeam
    ///   5. Tick the lane, firing events that NightBattleModalController observes
    ///   6. Fire OnBattleComplete → controller shows result
    ///   7. Wait for player to click "Return to Day" (ConfirmResult)
    ///   8. Apply aftermath (kill dead colony cards)
    ///
    /// Integration:
    ///   In DayCycleManager.EncounterPhase(), check for NightBattleManager.Instance first:
    ///     yield return NightBattleManager.Instance.RunNight(eligibleDefenders);
    ///     RunStateManager.Instance?.ApplyNightCombatResult(NightBattleManager.Instance.LastResult);
    ///
    /// Shop gold:
    ///   MVP uses the serialized startingGold value. TODO: integrate with CardManager currency.
    /// </summary>
    public class NightBattleManager : MonoBehaviour
    {
        public static NightBattleManager Instance { get; private set; }

        // ── Events (static — NightBattleModalController subscribes without a direct reference) ──

        /// <summary>Fired at night start. Controller opens the modal and populates all sections.</summary>
        public static event Action<NightModalContext> OnNightModalOpened;

        /// <summary>Fired when the lane starts ticking. Controller switches to battle display mode.</summary>
        public static event Action<CombatLane, NightWaveDefinition> OnBattleStarted;

        /// <summary>Fired when the lane resolves. Controller shows the result panel.</summary>
        public static event Action<NightCombatResult> OnBattleComplete;

        /// <summary>Fired when gold changes (shop purchase). Controller updates the gold label.</summary>
        public static event Action<int> OnGoldChanged;

        // ── Inspector ─────────────────────────────────────────────────────────────

        [BoxGroup("Wave")]
        [SerializeField, Tooltip("Single override wave. If set, always used regardless of day. Leave blank to use wavePool or procedural fallback.")]
        private NightWaveDefinition defaultWave;

        [BoxGroup("Wave")]
        [SerializeField, Tooltip("Ordered waves by progression. Day 1 = index 0, Day 2 = index 1, etc. Loops from last entry on later days.")]
        private NightWaveDefinition[] wavePool = Array.Empty<NightWaveDefinition>();

        [BoxGroup("Shop")]
        [SerializeField, Tooltip("All possible shop items. Manager picks shopSlotCount from this pool randomly.")]
        private NightShopItemDefinition[] shopPool = Array.Empty<NightShopItemDefinition>();

        [BoxGroup("Shop")]
        [SerializeField, Min(1)] private int shopSlotCount = 4;

        [BoxGroup("Shop")]
        [SerializeField, Min(0), Tooltip("Minimum gold guaranteed to the player each night. Actual gold = max(this, board coin count).")]
        private int startingGold = 10;

        [BoxGroup("Simulation")]
        [SerializeField, Min(0.05f)] private float tickInterval = 0.7f;

        [BoxGroup("Simulation")]
        [SerializeField, Min(10)]   private int maxTicks = 300;

        // ── Public state ──────────────────────────────────────────────────────────

        public NightCombatResult LastResult { get; private set; }
        public int PlayerGold { get; private set; }

        // ── Internal wait-state flags ─────────────────────────────────────────────
        private NightTeam  _confirmedTeam;
        private bool       _battleConfirmed;
        private bool       _resultAcknowledged;
        private bool       _fastResolve;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ── Public entry point (called by DayCycleManager) ────────────────────────

        /// <summary>Full night sequence coroutine. Yields until aftermath is complete.</summary>
        public IEnumerator RunNight(List<CardInstance> eligibleDefenders)
        {
            LastResult          = null;
            _confirmedTeam      = null;
            _battleConfirmed    = false;
            _resultAcknowledged = false;
            _fastResolve        = false;
            int boardCoins = CardManager.Instance != null
                ? CardManager.Instance.GetStatsSnapshot().Currency
                : 0;
            PlayerGold = Mathf.Max(startingGold, boardCoins);

            var wave      = ResolveWave();
            var shopItems = PickShopItems();

            var context = new NightModalContext
            {
                EligibleDefenders = eligibleDefenders,
                Wave              = wave,
                ShopItems         = shopItems,
                StartingGold      = PlayerGold
            };

            OnNightModalOpened?.Invoke(context);

            // Wait for player to assign fighters and click Start Battle.
            yield return new WaitUntil(() => _battleConfirmed);

            // Build simulation lane from the confirmed team.
            var defenderUnits = _confirmedTeam?.BuildCombatUnits() ?? new List<CombatUnit>();

            if (defenderUnits.Count == 0)
            {
                yield return HandleUndefendedNight(wave);
                yield break;
            }

            var enemyDefs   = wave?.BuildEnemyList() ?? new List<EnemyDefinition>();
            var enemyUnits  = enemyDefs.Select(CombatUnit.FromEnemyDefinition).ToList();
            var lane        = new CombatLane(defenderUnits, enemyUnits, wave);

            OnBattleStarted?.Invoke(lane, wave);

            // ── Tick loop ─────────────────────────────────────────────────────────
            int ticks = 0;
            while (lane.IsOngoing && ticks < maxTicks)
            {
                lane.Tick(tickInterval);
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
                Debug.LogWarning("NightBattleManager: Max tick limit reached, forcing end.");
                lane.ForceEnd();
            }

            LastResult = lane.BuildResult();
            OnBattleComplete?.Invoke(LastResult);

            // Wait for player to click "Return to Day".
            yield return new WaitUntil(() => _resultAcknowledged);

            yield return ApplyAftermath(LastResult);
        }

        // ── Public control API (called by NightBattleModalController) ─────────────

        /// <summary>Called when the player clicks "Start Battle". Begins the tick loop.</summary>
        public void ConfirmBattle(NightTeam team)
        {
            _confirmedTeam   = team;
            _battleConfirmed = true;
        }

        /// <summary>Called when the player clicks "Return to Day". Begins aftermath.</summary>
        public void ConfirmResult() => _resultAcknowledged = true;

        /// <summary>Collapses tick delay to one frame so battle resolves immediately.</summary>
        public void SetFastResolve() => _fastResolve = true;

        /// <summary>Deduct gold for a shop purchase. Fires OnGoldChanged.</summary>
        public bool TrySpendGold(int amount)
        {
            if (amount > PlayerGold) return false;
            PlayerGold -= amount;
            OnGoldChanged?.Invoke(PlayerGold);
            return true;
        }

        // ── Aftermath ─────────────────────────────────────────────────────────────

        private IEnumerator ApplyAftermath(NightCombatResult result)
        {
            foreach (var dead in result.DeadDefenders)
            {
                if (dead == null || dead.gameObject == null) continue;
                dead.Kill();
                yield return new WaitForSeconds(0.3f);
            }
        }

        // ── Edge case: no defenders ───────────────────────────────────────────────

        private IEnumerator HandleUndefendedNight(NightWaveDefinition wave)
        {
            int totalEnemies = wave?.BuildEnemyList().Count ?? 0;

            LastResult = new NightCombatResult(
                playerWon:         false,
                deadDefenders:     new List<CardInstance>(),
                survivorDefenders: new List<CardInstance>(),
                totalDefenders:    0,
                enemiesKilled:     0,
                totalEnemies:      totalEnemies,
                moraleDelta:       wave?.DefeatMoraleDelta ?? -10,
                fatigueDelta:      0,
                salvageDelta:      0
            );

            OnBattleComplete?.Invoke(LastResult);
            yield return new WaitUntil(() => _resultAcknowledged);
        }

        // ── Wave resolution ───────────────────────────────────────────────────────

        private NightWaveDefinition ResolveWave()
        {
            if (defaultWave != null) return defaultWave;

            int day = TimeManager.Instance?.CurrentDay ?? 1;

            if (wavePool != null && wavePool.Length > 0)
            {
                int idx = Mathf.Clamp(day - 1, 0, wavePool.Length - 1);
                return wavePool[idx];
            }

            var loaded = Resources.LoadAll<NightWaveDefinition>("Waves");
            if (loaded != null && loaded.Length > 0)
            {
                int idx = Mathf.Clamp(day - 1, 0, loaded.Length - 1);
                return loaded[idx];
            }

            return BuildProceduralWave();
        }

        private NightWaveDefinition BuildProceduralWave()
        {
            int day   = TimeManager.Instance?.CurrentDay ?? 1;
            int hp    = Mathf.RoundToInt(6 + day * 1.5f);
            int atk   = 2 + day / 7;
            int def   = day / 10;
            int count = Mathf.Clamp(1 + (day - 1) / 4, 1, 6);

            var scavenger = EnemyDefinition.CreateRuntime(
                GameLocalization.GetOptional("night.enemy.scavenger", "Scavenger"),
                hp, atk, def);

            return NightWaveDefinition.CreateRuntime(
                GameLocalization.GetOptional("night.wave.procedural.name", "Nightly Incursion"),
                GameLocalization.GetOptional("night.wave.procedural.flavor", "Scavengers probe the perimeter under cover of dark."),
                new List<EnemyEntry> { new EnemyEntry { Enemy = scavenger, Count = count } });
        }

        // ── Shop selection ────────────────────────────────────────────────────────

        private NightShopItemDefinition[] PickShopItems()
        {
            if (shopPool == null || shopPool.Length == 0)
                return BuildDefaultShopItems();

            // Shuffle pool and take shopSlotCount.
            var shuffled = shopPool.OrderBy(_ => UnityEngine.Random.value).ToArray();
            return shuffled.Take(Mathf.Min(shopSlotCount, shuffled.Length)).ToArray();
        }

        private static NightShopItemDefinition[] BuildDefaultShopItems()
        {
            // Runtime fallback items so the shop is functional without any assets configured.
            return new[]
            {
                MakeItem("Scrap Blade",   "+1 ATK to fighter",          5,  NightShopEffect.AddAttack,    1, true),
                MakeItem("Plated Vest",   "+3 Max HP to fighter",       8,  NightShopEffect.AddMaxHealth, 3, true),
                MakeItem("Energy Drink",  "+1 ATK this battle",         4,  NightShopEffect.AddAttack,    1, true),
                MakeItem("Repair Kit",    "Restore fighter to full HP", 12, NightShopEffect.FullHeal,     0, true),
                MakeItem("Hired Guard",   "Add temp 3ATK/6HP fighter",  15, NightShopEffect.HireGuard,    0, false, 3, 6),
            };
        }

        private static NightShopItemDefinition MakeItem(
            string name, string desc, int cost, NightShopEffect effect, int value,
            bool requiresTarget, int hireAtk = 0, int hireHp = 0)
        {
            var item = ScriptableObject.CreateInstance<NightShopItemDefinition>();
            // Use reflection-free public fields (set via the ScriptableObject's serialized fields directly
            // is not possible at runtime, so we use a helper on the SO instead).
            item.goldCost      = cost;
            item.effect        = effect;
            item.effectValue   = value;
            item.requiresTarget = requiresTarget;
            item.hireAttack    = hireAtk;
            item.hireHealth    = hireHp;
            // Name + description are private with serialized backing fields — use the SO's name as fallback.
            item.name = name; // Unity SO name (shown in the shop view via item.name)
            return item;
        }
    }

    // ── Context struct passed to the modal ────────────────────────────────────────

    /// <summary>All data the Night Battle Modal needs to populate its UI at open time.</summary>
    public struct NightModalContext
    {
        public List<CardInstance>          EligibleDefenders;
        public NightWaveDefinition         Wave;
        public NightShopItemDefinition[]   ShopItems;
        public int                         StartingGold;
    }
}
