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
    ///   Each night starts with max(serialized startingGold, current board currency).
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

        [BoxGroup("Rewards")]
        [SerializeField, Tooltip("Cards offered after a victorious night. Leave empty to build choices from Resources/Cards.")]
        private CardDefinition[] rewardPool = Array.Empty<CardDefinition>();

        [BoxGroup("Rewards")]
        [SerializeField, Min(1), Tooltip("How many cards the player chooses from after a win.")]
        private int rewardChoiceCount = 3;

        [BoxGroup("Simulation")]
        [SerializeField, Min(0.05f)] private float tickInterval = 0.7f;

        [BoxGroup("Simulation")]
        [SerializeField, Min(10)]   private int maxTicks = 300;

        // ── Public state ──────────────────────────────────────────────────────────

        public NightCombatResult LastResult { get; private set; }
        public int PlayerGold { get; private set; }
        public IReadOnlyList<CardDefinition> CurrentRewardChoices => _rewardChoices;
        public CardDefinition SelectedReward => _selectedReward;
        public bool IsRewardSelectionPending =>
            LastResult != null && LastResult.PlayerWon && _rewardChoices.Count > 0 && _selectedReward == null;

        // ── Internal wait-state flags ─────────────────────────────────────────────
        private NightTeam  _confirmedTeam;
        private bool       _battleConfirmed;
        private bool       _resultAcknowledged;
        private bool       _fastResolve;
        private bool       _isRunning;
        private readonly List<CardDefinition> _rewardChoices = new();
        private CardDefinition _selectedReward;

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
            if (_isRunning)
            {
                Debug.LogWarning("[NightBattleManager] RunNight re-entry blocked.");
                yield break;
            }
            _isRunning = true;

            LastResult          = null;
            _confirmedTeam      = null;
            _battleConfirmed    = false;
            _resultAcknowledged = false;
            _fastResolve        = false;
            ClearRewardState();
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

            if (OnNightModalOpened != null)
            {
                try
                {
                    OnNightModalOpened.Invoke(context);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[NightBattleManager] OnNightModalOpened handler threw: {e.Message}. Auto-deploying.");
                    _confirmedTeam = BuildAutomaticTeam(eligibleDefenders);
                    _battleConfirmed = true;
                }
            }
            else
            {
                Debug.LogWarning("NightBattleManager: No modal controller subscribed. Auto-deploying eligible defenders.");
                _confirmedTeam = BuildAutomaticTeam(eligibleDefenders);
                _battleConfirmed = true;
            }

            // Wait for player to assign fighters and click Start Battle.
            yield return new WaitUntil(() => _battleConfirmed);

            // Build simulation lane from the confirmed team.
            var defenderUnits = _confirmedTeam?.BuildCombatUnits() ?? new List<CombatUnit>();

            if (defenderUnits.Count == 0)
            {
                yield return HandleUndefendedNight(wave);
                _isRunning = false;
                yield break;
            }

            var enemyDefs   = wave?.BuildEnemyList() ?? new List<EnemyDefinition>();
            var enemyUnits  = enemyDefs.Select(CombatUnit.FromEnemyDefinition).ToList();
            var lane        = new CombatLane(defenderUnits, enemyUnits, wave);

            try
            {
                OnBattleStarted?.Invoke(lane, wave);
            }
            catch (Exception e)
            {
                Debug.LogError($"[NightBattleManager] OnBattleStarted handler threw: {e.Message}");
            }

            yield return null; // One frame for UI to settle before ticking.

            // ── Tick loop ─────────────────────────────────────────────────────────
            int ticks = 0;
            while (lane.IsOngoing && ticks < maxTicks)
            {
                try
                {
                    lane.Tick(tickInterval);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[NightBattleManager] lane.Tick threw (tick {ticks}): {e.Message}");
                }
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
            PrepareRewardChoices(LastResult);
            if (OnBattleComplete != null)
            {
                try
                {
                    OnBattleComplete.Invoke(LastResult);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[NightBattleManager] OnBattleComplete handler threw: {e.Message}. Auto-acknowledging.");
                    AutoSelectFirstReward();
                    _resultAcknowledged = true;
                }
            }
            else
            {
                Debug.LogWarning("NightBattleManager: No result controller subscribed. Continuing without result acknowledgement.");
                AutoSelectFirstReward();
                _resultAcknowledged = true;
            }

            // Wait for player to click "Return to Day".
            yield return new WaitUntil(() => _resultAcknowledged);

            try
            {
                yield return ApplyAftermath(LastResult);
            }
            finally
            {
                _isRunning = false;
            }
        }

        // ── Public control API (called by NightBattleModalController) ─────────────

        /// <summary>Called when the player clicks "Start Battle". Begins the tick loop.</summary>
        public void ConfirmBattle(NightTeam team)
        {
            _confirmedTeam   = team;
            _battleConfirmed = true;
        }

        /// <summary>Called when the player clicks "Return to Day". Begins aftermath.</summary>
        public void ConfirmResult()
        {
            if (IsRewardSelectionPending)
            {
                Debug.LogWarning("NightBattleManager: Result confirmation blocked until a reward is selected.");
                return;
            }

            _resultAcknowledged = true;
        }

        /// <summary>Called when the player selects one of the post-victory card rewards.</summary>
        public bool SelectReward(CardDefinition reward)
        {
            if (reward == null || !_rewardChoices.Contains(reward))
                return false;

            _selectedReward = reward;
            return true;
        }

        /// <summary>Collapses tick delay to one frame so battle resolves immediately.</summary>
        public void SetFastResolve()
        {
            _fastResolve = true;
            OnFastResolveEnabled?.Invoke();
        }

        /// <summary>Fired when the player clicks Fast Resolve. BattleArenaView collapses animation durations.</summary>
        public static event Action OnFastResolveEnabled;

        /// <summary>Deduct gold for a shop purchase. Fires OnGoldChanged.</summary>
        public bool TrySpendGold(int amount)
        {
            if (amount < 0) return false;
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

            if (result.PlayerWon && _selectedReward != null && HasSurvivingCharacterOnBoard())
                yield return SpawnSelectedReward(result);
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
            ClearRewardState();

            if (OnBattleComplete != null)
            {
                try { OnBattleComplete.Invoke(LastResult); }
                catch (Exception e)
                {
                    Debug.LogError($"[NightBattleManager] OnBattleComplete handler threw (undefended): {e.Message}. Auto-acknowledging.");
                    _resultAcknowledged = true;
                }
                yield return new WaitUntil(() => _resultAcknowledged);
            }
            else
            {
                Debug.LogWarning("NightBattleManager: No result controller subscribed for undefended night.");
            }
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
            item.ConfigureRuntime(name, desc, cost, effect, value, requiresTarget, hireAtk, hireHp, name);
            return item;
        }

        private static NightTeam BuildAutomaticTeam(IEnumerable<CardInstance> eligibleDefenders)
        {
            var team = new NightTeam();
            if (eligibleDefenders == null)
                return team;

            int slot = 0;
            foreach (CardInstance defender in eligibleDefenders)
            {
                if (defender == null || defender.CurrentHealth <= 0)
                    continue;

                team.Assign(slot, NightFighter.FromCard(defender));
                slot++;

                if (slot >= NightTeam.MaxSlots)
                    break;
            }

            return team;
        }

        // ── Victory rewards ──────────────────────────────────────────────────────

        private void ClearRewardState()
        {
            _rewardChoices.Clear();
            _selectedReward = null;
        }

        private void PrepareRewardChoices(NightCombatResult result)
        {
            ClearRewardState();

            if (result == null || !result.PlayerWon)
                return;

            // If every real character card died, the day cycle will end the run.
            // Do not let a reward choice soften that loss or repopulate the colony.
            if (result.SurvivorDefenders == null || result.SurvivorDefenders.Count == 0)
                return;

            var pool = ResolveRewardPool();
            if (pool.Count == 0)
                return;

            int count = Mathf.Min(Mathf.Max(1, rewardChoiceCount), pool.Count);
            _rewardChoices.AddRange(pool.OrderBy(_ => UnityEngine.Random.value).Take(count));
        }

        private List<CardDefinition> ResolveRewardPool()
        {
            IEnumerable<CardDefinition> configured = rewardPool != null
                ? rewardPool.Where(IsSelectableReward)
                : Enumerable.Empty<CardDefinition>();

            var configuredList = configured.Distinct().ToList();
            if (configuredList.Count > 0)
                return configuredList;

            return Resources.LoadAll<CardDefinition>("Cards")
                .Where(IsDefaultRewardCandidate)
                .Distinct()
                .ToList();
        }

        private static bool IsSelectableReward(CardDefinition definition)
        {
            if (definition == null)
                return false;

            return definition.Category is not CardCategory.None
                and not CardCategory.Character
                and not CardCategory.Mob
                and not CardCategory.Recipe
                and not CardCategory.Area;
        }

        private static bool IsDefaultRewardCandidate(CardDefinition definition)
        {
            if (definition == null)
                return false;

            return definition.Category is CardCategory.Resource
                or CardCategory.Consumable
                or CardCategory.Material
                or CardCategory.Equipment
                or CardCategory.Structure
                or CardCategory.Valuable;
        }

        private void AutoSelectFirstReward()
        {
            if (_selectedReward == null && _rewardChoices.Count > 0)
                _selectedReward = _rewardChoices[0];
        }

        private IEnumerator SpawnSelectedReward(NightCombatResult result)
        {
            if (_selectedReward == null)
                yield break;

            if (CardManager.Instance == null)
            {
                Debug.LogWarning("NightBattleManager: CardManager missing; selected night reward could not be spawned.");
                yield break;
            }

            Vector3 center = ResolveRewardSpawnPosition(result);
            Vector3 offset = new(
                UnityEngine.Random.Range(-1.5f, 1.5f),
                0f,
                UnityEngine.Random.Range(-1.5f, 1.5f));

            CardManager.Instance.CreateCardInstance(_selectedReward, center + offset);
            AudioManager.Instance?.PlaySFX(AudioId.Coin);
            Debug.Log($"NightBattleManager: Spawned selected reward '{_selectedReward.DisplayName}'.");
            yield return new WaitForSeconds(0.12f);
        }

        private static Vector3 ResolveRewardSpawnPosition(NightCombatResult result)
        {
            CardInstance survivor = result?.SurvivorDefenders?
                .FirstOrDefault(c => c != null && c.gameObject != null);

            if (survivor != null)
                return survivor.transform.position;

            CardInstance liveCharacter = CardManager.Instance?.AllCards?
                .FirstOrDefault(c => c != null
                                     && c.gameObject != null
                                     && c.Definition != null
                                     && c.Definition.Category == CardCategory.Character
                                     && c.CurrentHealth > 0);

            return liveCharacter != null ? liveCharacter.transform.position : Vector3.zero;
        }

        private static bool HasSurvivingCharacterOnBoard()
        {
            return CardManager.Instance?.AllCards?.Any(c => c != null
                                                         && c.gameObject != null
                                                         && c.Definition != null
                                                         && c.Definition.Category == CardCategory.Character
                                                         && c.CurrentHealth > 0) == true;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // ── Debug / quick-test ────────────────────────────────────────────────────

        [BoxGroup("Debug"), Button("▶ Test Night Now  [Shift+N]")]
        public void DebugTriggerNight()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[NightBattleManager] Enter Play Mode first.");
                return;
            }
            if (_isRunning)
            {
                Debug.LogWarning("[NightBattleManager] Night is already running.");
                return;
            }

            var defenders = CardManager.Instance?.AllCards
                ?.Where(c => c != null
                          && c.gameObject != null
                          && c.Definition?.Category == CardCategory.Character
                          && c.CurrentHealth > 0)
                .OrderBy(c => c.name)
                .ToList()
                ?? new List<CardInstance>();

            Debug.Log($"[NightBattleManager] Debug night triggered with {defenders.Count} eligible defender(s).");
            StartCoroutine(RunNight(defenders));
        }
#endif
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
