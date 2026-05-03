using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Markyu.LastKernel
{
    /// <summary>
    /// UI Toolkit controller for the unified Night Battle Modal.
    ///
    /// Setup:
    ///   1. Create a GameObject "NightBattleModal" in Game.unity.
    ///   2. Add UIDocument — source asset: NightBattleModal.uxml, Sort Order: 60.
    ///   3. Add this NightBattleModalController component.
    ///   4. Ensure NightBattleManager is also in the scene.
    ///
    /// Three phases handled by this controller:
    ///   PREP   — player assigns villagers to slots and buys shop items.
    ///   BATTLE — lane ticks automatically; HP bars update from CombatLane events.
    ///   RESULT — victory/defeat panel with "Return to Day" button.
    ///
    /// Click-to-assign flow:
    ///   1. Click a villager entry → it becomes SELECTED (AwaitingSlot state).
    ///   2. Click an empty player slot → villager is assigned there.
    ///   3. Click a filled player slot while Idle → fighter is unassigned (returned to list).
    ///   4. Click a shop item → if no target needed, applied immediately;
    ///      if target needed, enter AwaitingTarget state.
    ///   5. While AwaitingTarget, click a filled player slot → shop item applied to that fighter.
    ///   6. Right-clicking or clicking the same item/villager again cancels the selection.
    ///
    /// Partial files:
    ///   NightBattleModalController.Interaction.cs — click-to-assign state machine
    ///   NightBattleModalController.View.cs        — UI construction, log, localization
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public partial class NightBattleModalController : MonoBehaviour
    {
        public static NightBattleModalController Instance { get; private set; }

        // ── Phases ────────────────────────────────────────────────────────────────
        private enum Phase { Prep, Battle, Result }
        private Phase _phase = Phase.Prep;

        // ── Prep interaction state ────────────────────────────────────────────────
        private enum PrepInteraction { Idle, AwaitingSlot, AwaitingTarget }
        private PrepInteraction         _interaction       = PrepInteraction.Idle;
        private NightFighter            _pendingFighter;
        private NightShopItemDefinition _pendingItem;
        private VisualElement           _pendingVillagerEl;

        // ── Game objects ──────────────────────────────────────────────────────────
        private NightTeam                  _team = new();
        private CombatLane                 _activeLane;

        private readonly List<NightFighter>                    _availableFighters = new();
        private readonly Dictionary<string, VisualElement>     _villagerEls       = new();
        private readonly NightPrepSlotView[]                   _slotViews         = new NightPrepSlotView[NightTeam.MaxSlots];
        private readonly Dictionary<CombatUnit, NightPrepSlotView> _battleMap     = new();
        private readonly List<NightShopItemView>               _shopViews         = new();
        private readonly Dictionary<CardDefinition, VisualElement> _rewardChoiceEls = new();

        [SerializeField, Min(10)] private int maxLogLines = 50;
        private int _logLineCount;

        // ── Cached UI elements ────────────────────────────────────────────────────
        private VisualElement _root;
        private Label         _nightLabel, _threatLabel, _incomingLabel;
        private Label         _enemyCountLabel, _playerCountLabel;
        private VisualElement _enemyRow, _playerRow;
        private Label         _statusLabel;
        private Button        _btnStart, _btnFast, _btnCancel, _btnReturn;
        private ScrollView    _villagersList, _battleLog;
        private VisualElement _shopItems;
        private Label         _goldLabel, _assignHint, _shopHint;
        private VisualElement _resultPanel;
        private Label         _resultTitle, _resultSummary;
        private Label         _rewardTitle;
        private VisualElement _rewardOptions;
        private Label         _lblEnemySection, _lblColonySection;
        private Label         _lblDefendersHeader, _lblLogHeader, _lblShopHeader;

        // ── Battle-mode state ─────────────────────────────────────────────────
        private VisualElement _battleZone;
        private VisualElement _bottomArea;
        private int           _totalEnemyCount;
        private int           _remainingEnemyCount;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            NightBattleManager.OnNightModalOpened += HandleModalOpened;
            NightBattleManager.OnBattleStarted    += HandleBattleStarted;
            NightBattleManager.OnBattleComplete   += HandleBattleComplete;
            NightBattleManager.OnGoldChanged      += HandleGoldChanged;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

            NightBattleManager.OnNightModalOpened -= HandleModalOpened;
            NightBattleManager.OnBattleStarted    -= HandleBattleStarted;
            NightBattleManager.OnBattleComplete   -= HandleBattleComplete;
            NightBattleManager.OnGoldChanged      -= HandleGoldChanged;
            UnbindLane();
        }

        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            _root             = root.Q("nbm-root");
            _nightLabel       = root.Q<Label>("nbm-night-label");
            _threatLabel      = root.Q<Label>("nbm-threat-label");
            _incomingLabel    = root.Q<Label>("nbm-incoming-label");
            _enemyCountLabel  = root.Q<Label>("nbm-enemy-count-label");
            _playerCountLabel = root.Q<Label>("nbm-player-count-label");
            _enemyRow         = root.Q("nbm-enemy-row");
            _playerRow        = root.Q("nbm-player-row");
            _statusLabel      = root.Q<Label>("nbm-status-label");
            _btnStart         = root.Q<Button>("nbm-btn-start");
            _btnFast          = root.Q<Button>("nbm-btn-fast");
            _btnCancel        = root.Q<Button>("nbm-btn-cancel");
            _btnReturn        = root.Q<Button>("nbm-btn-return");
            _villagersList    = root.Q<ScrollView>("nbm-villagers-list");
            _battleLog        = root.Q<ScrollView>("nbm-battle-log");
            _shopItems        = root.Q("nbm-shop-items");
            _goldLabel        = root.Q<Label>("nbm-gold-label");
            _assignHint       = root.Q<Label>("nbm-assign-hint");
            _shopHint         = root.Q<Label>("nbm-shop-hint");
            _resultPanel      = root.Q("nbm-result-panel");
            _resultTitle      = root.Q<Label>("nbm-result-title");
            _resultSummary    = root.Q<Label>("nbm-result-summary");
            _rewardTitle      = root.Q<Label>("nbm-reward-title");
            _rewardOptions    = root.Q("nbm-reward-options");

            _lblEnemySection     = root.Q<Label>("nbm-lbl-enemy-section");
            _lblColonySection    = root.Q<Label>("nbm-lbl-colony-section");
            _lblDefendersHeader  = root.Q<Label>("nbm-lbl-defenders-header");
            _lblLogHeader        = root.Q<Label>("nbm-lbl-log-header");
            _lblShopHeader       = root.Q<Label>("nbm-lbl-shop-header");

            _battleZone = root.Q("nbm-battle-zone");
            _bottomArea = root.Q("nbm-bottom-area");

            _btnStart?.RegisterCallback<ClickEvent>(_ => OnStartBattleClicked());
            _btnFast?.RegisterCallback<ClickEvent>(_ => OnFastResolveClicked());
            _btnCancel?.RegisterCallback<ClickEvent>(_ => OnCancelClicked());
            _btnReturn?.RegisterCallback<ClickEvent>(_ => OnReturnDayClicked());

            BindStaticText();
            SetVisible(false);
        }

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            HandleDebugKeys();
#endif
        }

        // ── NightBattleManager event handlers ─────────────────────────────────────

        private void HandleModalOpened(NightModalContext ctx)
        {
            UnbindLane();

            // Restore prep layout (clear any battle-mode overrides from last night)
            _bottomArea?.RemoveFromClassList("nbm-bottom-area--battle");
            _statusLabel?.RemoveFromClassList("nbm-status-label--battle");
            _assignHint?.RemoveFromClassList("lk-hidden");
            _shopHint?.RemoveFromClassList("lk-hidden");
            _btnFast?.RemoveFromClassList("nbm-btn--fast--active");

            _phase       = Phase.Prep;
            _interaction = PrepInteraction.Idle;
            _pendingFighter    = null;
            _pendingItem       = null;
            _pendingVillagerEl = null;

            _team = new NightTeam();
            _availableFighters.Clear();
            _villagerEls.Clear();
            _shopViews.Clear();
            _battleMap.Clear();
            _logLineCount = 0;
            _rewardChoiceEls.Clear();

            _villagersList?.Clear();
            _enemyRow?.Clear();
            _playerRow?.Clear();
            _shopItems?.Clear();
            _battleLog?.Clear();
            _rewardOptions?.Clear();

            int day = TimeManager.Instance?.CurrentDay ?? 1;
            if (_nightLabel    != null) _nightLabel.text    = GameLocalization.Format("night.modal.nightTitle", day);
            if (_threatLabel   != null) _threatLabel.text   = ComputeThreatLabel(ctx.Wave);
            if (_incomingLabel != null) _incomingLabel.text = GameLocalization.Format("night.modal.incoming", ctx.Wave?.BuildEnemyList().Count ?? 0);
            if (_goldLabel     != null) _goldLabel.text     = GameLocalization.Format("night.modal.gold", ctx.StartingGold);

            BuildEnemyPreview(ctx.Wave);
            BuildPlayerSlots();

            foreach (var card in ctx.EligibleDefenders)
            {
                var fighter = NightFighter.FromCard(card);
                _availableFighters.Add(fighter);
                var el = BuildVillagerEntry(fighter);
                _villagerEls[fighter.Id] = el;
                _villagersList?.Add(el);
            }

            if (ctx.ShopItems != null)
            {
                foreach (var def in ctx.ShopItems)
                {
                    var view = new NightShopItemView(def);
                    view.OnClicked += OnShopItemClicked;
                    view.SetAffordable(ctx.StartingGold >= def.goldCost);
                    _shopViews.Add(view);
                    _shopItems?.Add(view.Root);
                }
            }

            _resultPanel?.AddToClassList("lk-hidden");
            _rewardTitle?.AddToClassList("lk-hidden");
            _rewardOptions?.AddToClassList("lk-hidden");
            _btnStart?.RemoveFromClassList("lk-hidden");
            _btnStart?.SetEnabled(true);
            _btnFast?.SetEnabled(false);
            _btnCancel?.RemoveFromClassList("lk-hidden");
            _btnReturn?.AddToClassList("lk-hidden");

            SetStatus(GameLocalization.Get("night.modal.phase.prep"));
            UpdatePlayerCountLabel();

            AddLog(GameLocalization.Format("night.modal.log.begin", day), "nbm-log-entry--system");
            if (ctx.EligibleDefenders.Count == 0)
                AddLog(GameLocalization.Get("night.modal.log.noDefenders"), "nbm-log-entry--death");

            SetVisible(true);
        }

        private void HandleBattleStarted(CombatLane lane, NightWaveDefinition wave)
        {
            _phase      = Phase.Battle;
            _activeLane = lane;
            BindLane(lane);

            int unitIdx = 0;
            for (int i = 0; i < NightTeam.MaxSlots && unitIdx < lane.Defenders.Count; i++)
            {
                if (!_team.IsSlotEmpty(i))
                {
                    _battleMap[lane.Defenders[unitIdx]] = _slotViews[i];
                    unitIdx++;
                }
            }

            foreach (var sv in _slotViews) sv.SetLocked(true);
            foreach (var shop in _shopViews) shop.SetLocked(true);
            LockVillagerList(true);

            _btnFast?.SetEnabled(true);
            _btnStart?.AddToClassList("lk-hidden");
            _btnCancel?.AddToClassList("lk-hidden");

            // Switch layout: keep battle zone visible (HP bars update live), hide prep/shop, expand log
            _bottomArea?.AddToClassList("nbm-bottom-area--battle");
            _statusLabel?.AddToClassList("nbm-status-label--battle");
            _assignHint?.AddToClassList("lk-hidden");
            _shopHint?.AddToClassList("lk-hidden");

            // Prime enemy count display
            _totalEnemyCount     = lane.Enemies.Count;
            _remainingEnemyCount = lane.Enemies.Count;
            UpdateEnemyRemainingLabel();

            SetStatus(GameLocalization.Get("night.modal.phase.battle"));
            AddLog(GameLocalization.Get("night.modal.log.battleStart"), "nbm-log-entry--system");
        }

        private void HandleBattleComplete(NightCombatResult result)
        {
            if (result == null) return;

            // Undefended night: OnBattleStarted was never fired, so the prep layout
            // is still showing. Switch to battle layout now so the result panel
            // overlays a clean state rather than the prep zone.
            if (_phase == Phase.Prep)
            {
                _battleZone?.AddToClassList("lk-hidden");
                _bottomArea?.AddToClassList("nbm-bottom-area--battle");
                _btnStart?.AddToClassList("lk-hidden");
                _btnCancel?.AddToClassList("lk-hidden");
                _btnFast?.SetEnabled(false);
            }

            _phase = Phase.Result;

            bool won = result.PlayerWon;
            if (_resultTitle != null)
            {
                _resultTitle.text = won
                    ? GameLocalization.Get("night.modal.result.win")
                    : GameLocalization.Get("night.modal.result.loss");
                _resultTitle.RemoveFromClassList("nbm-result-title--victory");
                _resultTitle.RemoveFromClassList("nbm-result-title--defeat");
                _resultTitle.AddToClassList(won ? "nbm-result-title--victory" : "nbm-result-title--defeat");
            }
            if (_resultSummary != null) _resultSummary.text = result.GetSummaryText();

            _resultPanel?.RemoveFromClassList("lk-hidden");
            _btnReturn?.RemoveFromClassList("lk-hidden");
            _btnFast?.SetEnabled(false);

            var rewards = won
                ? NightBattleManager.Instance?.CurrentRewardChoices
                : null;
            BuildRewardChoices(rewards);

            bool mustChooseReward = NightBattleManager.Instance?.IsRewardSelectionPending == true;
            _btnReturn?.SetEnabled(!mustChooseReward);

            SetStatus(won
                ? GameLocalization.Get("night.modal.phase.victory")
                : GameLocalization.Get("night.modal.phase.defeat"));
            AddLog(won
                ? GameLocalization.Get("night.modal.log.victoryLog")
                : GameLocalization.Get("night.modal.log.defeatLog"),
                won ? "nbm-log-entry--victory" : "nbm-log-entry--defeat");
        }

        private void HandleGoldChanged(int newGold)
        {
            if (_goldLabel != null) _goldLabel.text = GameLocalization.Format("night.modal.gold", newGold);
            foreach (var sv in _shopViews)
                sv.SetAffordable(!sv.Purchased && newGold >= sv.Definition.goldCost);
        }

        // ── Button handlers ───────────────────────────────────────────────────────

        private void OnStartBattleClicked()
        {
            if (_phase != Phase.Prep) return;
            AudioManager.Instance?.PlaySFX(AudioId.Click);
            CancelCurrentSelection();

            if (_team.FilledSlotCount == 0)
                AddLog(GameLocalization.Get("night.modal.log.undefended"), "nbm-log-entry--death");

            _btnStart?.SetEnabled(false);
            AddLog(GameLocalization.Get("night.modal.log.formation"), "nbm-log-entry--system");
            NightBattleManager.Instance?.ConfirmBattle(_team);
        }

        private void OnFastResolveClicked()
        {
            _btnFast?.SetEnabled(false);
            _btnFast?.AddToClassList("nbm-btn--fast--active");
            NightBattleManager.Instance?.SetFastResolve();
            AddLog(GameLocalization.Get("night.modal.log.fastResolve"), "nbm-log-entry--system");
        }

        private void OnCancelClicked()
        {
            if (_phase != Phase.Prep) return;

            foreach (var fighter in _availableFighters)
            {
                if (_team.Contains(fighter)) continue;
                int slot = _team.FirstEmptySlot();
                if (slot < 0) break;
                _pendingFighter = fighter;
                _interaction    = PrepInteraction.AwaitingSlot;
                AssignFighterToSlot(slot);
            }

            AddLog(GameLocalization.Get("night.modal.log.autoDeploy"), "nbm-log-entry--system");
            OnStartBattleClicked();
        }

        private void OnReturnDayClicked()
        {
            AudioManager.Instance?.PlaySFX(AudioId.Click);

            if (NightBattleManager.Instance?.IsRewardSelectionPending == true)
            {
                AddLog(GameLocalization.Get("night.modal.log.rewardRequired"), "nbm-log-entry--system");
                return;
            }

            SetVisible(false);
            NightBattleManager.Instance?.ConfirmResult();
        }

        private void OnRewardChoiceClicked(CardDefinition reward)
        {
            if (_phase != Phase.Result || reward == null)
                return;

            if (NightBattleManager.Instance?.SelectReward(reward) != true)
                return;

            foreach (var kv in _rewardChoiceEls)
                kv.Value.EnableInClassList("nbm-reward-card--selected", kv.Key == reward);

            _btnReturn?.SetEnabled(true);
            AudioManager.Instance?.PlaySFX(AudioId.Click);
            AddLog(GameLocalization.Format("night.modal.log.rewardSelected", reward.DisplayName), "nbm-log-entry--victory");
        }

        // ── CombatLane event handling (battle phase) ──────────────────────────────

        private void BindLane(CombatLane lane)
        {
            lane.OnAttackResolved += HandleAttack;
            lane.OnUnitDied       += HandleUnitDied;
            lane.OnCombatEnded    += HandleCombatEnded;
        }

        private void UnbindLane()
        {
            if (_activeLane == null) return;
            _activeLane.OnAttackResolved -= HandleAttack;
            _activeLane.OnUnitDied       -= HandleUnitDied;
            _activeLane.OnCombatEnded    -= HandleCombatEnded;
            _activeLane = null;
        }

        private void HandleAttack(CombatUnit attacker, CombatUnit target, int damage, bool isCrit)
        {
            if (damage == 0)
            {
                AudioManager.Instance?.PlaySFX(AudioId.Miss);
                AddLog(GameLocalization.Format("night.modal.log.missed", attacker.DisplayName, target.DisplayName));
            }
            else if (isCrit)
            {
                AudioManager.Instance?.PlaySFX(AudioId.Critical, interruptBGM: true);
                string crit = GameLocalization.Get("night.modal.log.crit");
                AddLog(GameLocalization.Format("night.modal.log.hit", attacker.DisplayName, target.DisplayName, damage, crit), "nbm-log-entry--damage");
            }
            else
            {
                AudioManager.Instance?.PlaySFX(AudioId.HitMelee);
                AddLog(GameLocalization.Format("night.modal.log.hit", attacker.DisplayName, target.DisplayName, damage, ""), "nbm-log-entry--damage");
            }

            if (_battleMap.TryGetValue(target, out var sv))
                sv.RefreshBattle(target);
        }

        private void HandleUnitDied(CombatUnit unit)
        {
            AudioManager.Instance?.PlaySFX(AudioId.Pop);
            AddLog(GameLocalization.Format("night.modal.log.unitDied", unit.DisplayName), "nbm-log-entry--death");
            if (_battleMap.TryGetValue(unit, out var sv)) sv.RefreshBattle(unit);
            UpdateFrontHighlights();

            if (unit.Side == CombatUnitSide.Enemy)
            {
                _remainingEnemyCount = Mathf.Max(0, _remainingEnemyCount - 1);
                UpdateEnemyRemainingLabel();
            }
        }

        private void HandleCombatEnded(bool playerWon) { }

        private void UpdateEnemyRemainingLabel()
        {
            if (_incomingLabel == null) return;
            _incomingLabel.text = GameLocalization.Format(
                "night.modal.enemiesRemaining", _remainingEnemyCount, _totalEnemyCount);
        }

        private void UpdateFrontHighlights()
        {
            if (_activeLane == null) return;
            bool frontFound = false;
            for (int i = 0; i < _slotViews.Length; i++)
            {
                _slotViews[i].Root.RemoveFromClassList("nb-prep-slot--front");
                if (!frontFound && _team.GetSlot(i) != null)
                {
                    foreach (var (unit, sv) in _battleMap)
                    {
                        if (sv == _slotViews[i] && unit.IsAlive)
                        {
                            _slotViews[i].Root.AddToClassList("nb-prep-slot--front");
                            frontFound = true;
                            break;
                        }
                    }
                }
            }
        }
    }
}
