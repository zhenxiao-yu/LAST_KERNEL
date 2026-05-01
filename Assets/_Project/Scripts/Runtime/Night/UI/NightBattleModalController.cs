using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
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
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class NightBattleModalController : MonoBehaviour
    {
        // ── Phases ────────────────────────────────────────────────────────────────
        private enum Phase { Prep, Battle, Result }
        private Phase _phase = Phase.Prep;

        // ── Prep interaction state ────────────────────────────────────────────────
        private enum PrepInteraction { Idle, AwaitingSlot, AwaitingTarget }
        private PrepInteraction     _interaction   = PrepInteraction.Idle;
        private NightFighter        _pendingFighter;  // villager selected, waiting for slot
        private NightShopItemDefinition _pendingItem; // shop item selected, waiting for target
        private VisualElement       _pendingVillagerEl; // the VE that's highlighted in the list

        // ── Game objects ──────────────────────────────────────────────────────────
        private NightTeam                  _team = new();
        private CombatLane                 _activeLane;

        // All NightFighters built at modal open (from eligible defenders + temp additions).
        private readonly List<NightFighter>      _availableFighters = new();
        // VE entry for each fighter in the villagers scroll list.
        private readonly Dictionary<string, VisualElement> _villagerEls = new();
        // The 5 player prep slot views (index 0 = front).
        private readonly NightPrepSlotView[]     _slotViews = new NightPrepSlotView[NightTeam.MaxSlots];
        // Map CombatUnit → slot view for HP updates during battle.
        private readonly Dictionary<CombatUnit, NightPrepSlotView> _battleMap = new();
        // Active shop item views.
        private readonly List<NightShopItemView> _shopViews = new();

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
        // Static section headers (named in UXML so localization can update them)
        private Label         _lblEnemySection, _lblColonySection;
        private Label         _lblDefendersHeader, _lblLogHeader, _lblShopHeader;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            NightBattleManager.OnNightModalOpened += HandleModalOpened;
            NightBattleManager.OnBattleStarted    += HandleBattleStarted;
            NightBattleManager.OnBattleComplete   += HandleBattleComplete;
            NightBattleManager.OnGoldChanged      += HandleGoldChanged;
        }

        private void OnDestroy()
        {
            NightBattleManager.OnNightModalOpened -= HandleModalOpened;
            NightBattleManager.OnBattleStarted    -= HandleBattleStarted;
            NightBattleManager.OnBattleComplete   -= HandleBattleComplete;
            NightBattleManager.OnGoldChanged      -= HandleGoldChanged;
            UnbindLane();
        }

        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            _root            = root.Q("nbm-root");
            _nightLabel      = root.Q<Label>("nbm-night-label");
            _threatLabel     = root.Q<Label>("nbm-threat-label");
            _incomingLabel   = root.Q<Label>("nbm-incoming-label");
            _enemyCountLabel = root.Q<Label>("nbm-enemy-count-label");
            _playerCountLabel= root.Q<Label>("nbm-player-count-label");
            _enemyRow        = root.Q("nbm-enemy-row");
            _playerRow       = root.Q("nbm-player-row");
            _statusLabel     = root.Q<Label>("nbm-status-label");
            _btnStart        = root.Q<Button>("nbm-btn-start");
            _btnFast         = root.Q<Button>("nbm-btn-fast");
            _btnCancel       = root.Q<Button>("nbm-btn-cancel");
            _btnReturn       = root.Q<Button>("nbm-btn-return");
            _villagersList   = root.Q<ScrollView>("nbm-villagers-list");
            _battleLog       = root.Q<ScrollView>("nbm-battle-log");
            _shopItems       = root.Q("nbm-shop-items");
            _goldLabel       = root.Q<Label>("nbm-gold-label");
            _assignHint      = root.Q<Label>("nbm-assign-hint");
            _shopHint        = root.Q<Label>("nbm-shop-hint");
            _resultPanel     = root.Q("nbm-result-panel");
            _resultTitle     = root.Q<Label>("nbm-result-title");
            _resultSummary   = root.Q<Label>("nbm-result-summary");

            _lblEnemySection     = root.Q<Label>("nbm-lbl-enemy-section");
            _lblColonySection    = root.Q<Label>("nbm-lbl-colony-section");
            _lblDefendersHeader  = root.Q<Label>("nbm-lbl-defenders-header");
            _lblLogHeader        = root.Q<Label>("nbm-lbl-log-header");
            _lblShopHeader       = root.Q<Label>("nbm-lbl-shop-header");

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
            _phase       = Phase.Prep;
            _interaction = PrepInteraction.Idle;
            _pendingFighter  = null;
            _pendingItem     = null;
            _pendingVillagerEl = null;

            _team = new NightTeam();
            _availableFighters.Clear();
            _villagerEls.Clear();
            _shopViews.Clear();
            _battleMap.Clear();
            _logLineCount = 0;

            _villagersList?.Clear();
            _enemyRow?.Clear();
            _playerRow?.Clear();
            _shopItems?.Clear();
            _battleLog?.Clear();

            // Night / threat info
            int day = TimeManager.Instance?.CurrentDay ?? 1;
            if (_nightLabel    != null) _nightLabel.text    = GameLocalization.Format("night.modal.nightTitle", day);
            if (_threatLabel   != null) _threatLabel.text   = ComputeThreatLabel(ctx.Wave);
            if (_incomingLabel != null) _incomingLabel.text = GameLocalization.Format("night.modal.incoming", ctx.Wave?.BuildEnemyList().Count ?? 0);
            if (_goldLabel     != null) _goldLabel.text     = GameLocalization.Format("night.modal.gold", ctx.StartingGold);

            // Enemy preview
            BuildEnemyPreview(ctx.Wave);

            // Player slots
            BuildPlayerSlots();

            // Villager list
            foreach (var card in ctx.EligibleDefenders)
            {
                var fighter = NightFighter.FromCard(card);
                _availableFighters.Add(fighter);
                var el = BuildVillagerEntry(fighter);
                _villagerEls[fighter.Id] = el;
                _villagersList?.Add(el);
            }

            // Shop
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

            // UI state
            _resultPanel?.AddToClassList("lk-hidden");
            _btnStart?.SetEnabled(true);
            _btnFast?.SetEnabled(false); // only active once battle starts
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

            // Map CombatUnit → slot view by matching slot order (slots with fighters in index order).
            int unitIdx = 0;
            for (int i = 0; i < NightTeam.MaxSlots && unitIdx < lane.Defenders.Count; i++)
            {
                if (!_team.IsSlotEmpty(i))
                {
                    _battleMap[lane.Defenders[unitIdx]] = _slotViews[i];
                    unitIdx++;
                }
            }

            // Lock all interaction.
            foreach (var sv in _slotViews) sv.SetLocked(true);
            foreach (var shop in _shopViews) shop.SetLocked(true);
            LockVillagerList(true);

            _btnFast?.SetEnabled(true);
            _btnCancel?.AddToClassList("lk-hidden");

            SetStatus(GameLocalization.Get("night.modal.phase.battle"));
            AddLog(GameLocalization.Get("night.modal.log.battleStart"), "nbm-log-entry--system");
        }

        private void HandleBattleComplete(NightCombatResult result)
        {
            _phase = Phase.Result;

            bool won = result.PlayerWon;
            if (_resultTitle != null)
            {
                _resultTitle.text = won ? GameLocalization.Get("night.modal.result.win") : GameLocalization.Get("night.modal.result.loss");
                _resultTitle.RemoveFromClassList("nbm-result-title--victory");
                _resultTitle.RemoveFromClassList("nbm-result-title--defeat");
                _resultTitle.AddToClassList(won ? "nbm-result-title--victory" : "nbm-result-title--defeat");
            }
            if (_resultSummary  != null) _resultSummary.text  = result.GetSummaryText();

            _resultPanel?.RemoveFromClassList("lk-hidden");
            _btnFast?.SetEnabled(false);

            SetStatus(won ? GameLocalization.Get("night.modal.phase.victory") : GameLocalization.Get("night.modal.phase.defeat"));
            AddLog(won ? GameLocalization.Get("night.modal.log.victoryLog") : GameLocalization.Get("night.modal.log.defeatLog"),
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
            NightBattleManager.Instance?.SetFastResolve();
            AddLog(GameLocalization.Get("night.modal.log.fastResolve"), "nbm-log-entry--system");
        }

        private void OnCancelClicked()
        {
            // Auto-Deploy: fill every empty slot with the next unassigned eligible fighter, then start.
            // Night is never skippable — this just removes the manual step.
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
            SetVisible(false);
            NightBattleManager.Instance?.ConfirmResult();
        }

        // ── Click-to-assign: villager entries ─────────────────────────────────────

        private void OnVillagerClicked(NightFighter fighter)
        {
            if (_phase != Phase.Prep) return;
            if (_team.Contains(fighter)) return; // already assigned — click the slot to unassign

            if (_interaction == PrepInteraction.AwaitingTarget)
                CancelCurrentSelection(); // shop item was pending; cancel it

            if (_interaction == PrepInteraction.AwaitingSlot && _pendingFighter?.Id == fighter.Id)
            {
                CancelCurrentSelection();
                return;
            }

            // Deselect previous
            if (_pendingVillagerEl != null) _pendingVillagerEl.RemoveFromClassList("nbm-villager-entry--selected");
            foreach (var sv in _slotViews) sv.SetHighlighted(false);

            // Select this fighter
            _pendingFighter = fighter;
            _interaction    = PrepInteraction.AwaitingSlot;

            _pendingVillagerEl = _villagerEls.TryGetValue(fighter.Id, out var el) ? el : null;
            _pendingVillagerEl?.AddToClassList("nbm-villager-entry--selected");

            // Highlight empty slots as valid targets.
            for (int i = 0; i < NightTeam.MaxSlots; i++)
                if (_team.IsSlotEmpty(i)) _slotViews[i].SetHighlighted(true);
        }

        // ── Click-to-assign: player prep slots ───────────────────────────────────

        private void OnSlotClicked(int slotIndex)
        {
            if (_phase != Phase.Prep) return;

            switch (_interaction)
            {
                case PrepInteraction.AwaitingSlot:
                    AssignFighterToSlot(slotIndex);
                    break;

                case PrepInteraction.AwaitingTarget:
                    ApplyShopItemToSlot(slotIndex);
                    break;

                case PrepInteraction.Idle:
                    // Unassign any fighter currently in this slot.
                    if (!_team.IsSlotEmpty(slotIndex))
                        UnassignFromSlot(slotIndex);
                    break;
            }
        }

        private void AssignFighterToSlot(int slotIndex)
        {
            if (_pendingFighter == null) return;

            // If slot is filled, evict the existing fighter first.
            if (!_team.IsSlotEmpty(slotIndex))
            {
                var evicted = _team.Clear(slotIndex);
                if (evicted != null) MarkVillagerAvailable(evicted);
            }

            _team.Assign(slotIndex, _pendingFighter);
            _slotViews[slotIndex].Assign(_pendingFighter);
            MarkVillagerAssigned(_pendingFighter);

            CancelCurrentSelection();
            UpdatePlayerCountLabel();
        }

        private void UnassignFromSlot(int slotIndex)
        {
            var fighter = _team.Clear(slotIndex);
            if (fighter == null) return;

            _slotViews[slotIndex].Clear();
            MarkVillagerAvailable(fighter);
            UpdatePlayerCountLabel();
        }

        // ── Click-to-assign: shop items ───────────────────────────────────────────

        private void OnShopItemClicked(NightShopItemDefinition def)
        {
            if (_phase != Phase.Prep) return;

            int gold = NightBattleManager.Instance?.PlayerGold ?? 0;
            if (gold < def.goldCost)
            {
                AddLog(GameLocalization.Format("night.modal.log.noGold", def.goldCost, gold), "nbm-log-entry--system");
                return;
            }

            if (!def.requiresTarget)
            {
                // Immediate purchase — no target needed (e.g. Hired Guard).
                if (NightBattleManager.Instance?.TrySpendGold(def.goldCost) == true)
                {
                    def.Apply(null, _team);
                    MarkShopItemPurchased(def);
                    RefreshTeamSlots();
                    AddLog(GameLocalization.Format("night.modal.log.purchased", def.DisplayName), "nbm-log-entry--system");
                }
                return;
            }

            if (_interaction == PrepInteraction.AwaitingTarget && _pendingItem == def)
            {
                CancelCurrentSelection();
                return;
            }

            CancelCurrentSelection();

            _pendingItem   = def;
            _interaction   = PrepInteraction.AwaitingTarget;

            // Highlight filled slots as valid targets.
            for (int i = 0; i < NightTeam.MaxSlots; i++)
                _slotViews[i].SetHighlighted(!_team.IsSlotEmpty(i));

            foreach (var sv in _shopViews)
                sv.SetSelected(sv.Definition == def);

            AddLog(GameLocalization.Format("night.modal.log.itemSelected", def.DisplayName), "nbm-log-entry--system");
        }

        private void ApplyShopItemToSlot(int slotIndex)
        {
            if (_pendingItem == null || _team.IsSlotEmpty(slotIndex)) return;

            var fighter = _team.GetSlot(slotIndex);
            if (NightBattleManager.Instance?.TrySpendGold(_pendingItem.goldCost) == true)
            {
                _pendingItem.Apply(fighter, _team);
                MarkShopItemPurchased(_pendingItem);
                _slotViews[slotIndex].RefreshDisplay(fighter);
                AddLog(GameLocalization.Format("night.modal.log.itemApplied", _pendingItem.DisplayName, fighter.DisplayName), "nbm-log-entry--system");
            }

            CancelCurrentSelection();
        }

        // ── Slot / fighter state helpers ──────────────────────────────────────────

        private void MarkVillagerAssigned(NightFighter f)
        {
            if (!_villagerEls.TryGetValue(f.Id, out var el)) return;
            el.AddToClassList("nbm-villager-entry--assigned");

            // Show slot badge on the entry.
            var badge = el.Q<Label>("nbm-villager-badge");
            if (badge != null)
            {
                int slot = _team.SlotOf(f);
                badge.text = slot == 0
                    ? GameLocalization.Get("night.modal.slot.front")
                    : GameLocalization.Format("night.modal.slot.n", slot + 1);
                badge.RemoveFromClassList("lk-hidden");
            }
        }

        private void MarkVillagerAvailable(NightFighter f)
        {
            if (!_villagerEls.TryGetValue(f.Id, out var el)) return;
            el.RemoveFromClassList("nbm-villager-entry--assigned");

            var badge = el.Q<Label>("nbm-villager-badge");
            badge?.AddToClassList("lk-hidden");
        }

        private void MarkShopItemPurchased(NightShopItemDefinition def)
        {
            foreach (var sv in _shopViews)
                if (sv.Definition == def) { sv.MarkPurchased(); break; }
        }

        private void RefreshTeamSlots()
        {
            // Called after changes that may have added fighters (e.g. Hired Guard).
            for (int i = 0; i < NightTeam.MaxSlots; i++)
            {
                var f = _team.GetSlot(i);
                if (f == null) continue;
                if (_slotViews[i].AssignedFighter?.Id != f.Id)
                    _slotViews[i].Assign(f);
                else
                    _slotViews[i].RefreshDisplay(f);
            }
            UpdatePlayerCountLabel();
        }

        private void CancelCurrentSelection()
        {
            _pendingFighter    = null;
            _pendingItem       = null;
            _interaction       = PrepInteraction.Idle;

            if (_pendingVillagerEl != null)
            {
                _pendingVillagerEl.RemoveFromClassList("nbm-villager-entry--selected");
                _pendingVillagerEl = null;
            }

            foreach (var sv in _slotViews) sv.SetHighlighted(false);
            foreach (var sv in _shopViews) sv.SetSelected(false);
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
                AddLog(GameLocalization.Format("night.modal.log.missed", attacker.DisplayName, target.DisplayName));
            else
            {
                string crit = isCrit ? GameLocalization.Get("night.modal.log.crit") : "";
                AddLog(GameLocalization.Format("night.modal.log.hit", attacker.DisplayName, target.DisplayName, damage, crit), "nbm-log-entry--damage");
            }

            if (_battleMap.TryGetValue(target, out var sv))
                sv.RefreshBattle(target);
        }

        private void HandleUnitDied(CombatUnit unit)
        {
            AddLog(GameLocalization.Format("night.modal.log.unitDied", unit.DisplayName), "nbm-log-entry--death");
            if (_battleMap.TryGetValue(unit, out var sv)) sv.RefreshBattle(unit);
            UpdateFrontHighlights();
        }

        private void HandleCombatEnded(bool playerWon) { }

        private void UpdateFrontHighlights()
        {
            if (_activeLane == null) return;
            bool frontFound = false;
            for (int i = 0; i < _slotViews.Length; i++)
            {
                _slotViews[i].Root.RemoveFromClassList("nb-prep-slot--front");
                if (!frontFound && _team.GetSlot(i) != null)
                {
                    // Find the CombatUnit for this slot and check if it's alive.
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

        // ── UI construction helpers ───────────────────────────────────────────────

        private void BuildEnemyPreview(NightWaveDefinition wave)
        {
            if (wave == null || _enemyRow == null) return;
            var enemies = wave.BuildEnemyList();

            for (int i = 0; i < enemies.Count; i++)
            {
                var def  = enemies[i];
                bool front = i == 0;

                var card = new VisualElement();
                card.AddToClassList("nbm-enemy-card");
                if (front) card.AddToClassList("nbm-enemy-card--front");

                var name = new Label(def.DisplayName.ToUpper());
                name.AddToClassList("nbm-enemy-card__name");

                var stats = new Label(GameLocalization.Format("night.modal.stats.enemy", def.Attack, def.MaxHP));
                stats.AddToClassList("nbm-enemy-card__stats");

                card.Add(name);
                card.Add(stats);

                if (front)
                {
                    var badge = new Label(GameLocalization.Get("night.modal.slot.front"));
                    badge.AddToClassList("nbm-enemy-card__front-badge");
                    card.Add(badge);
                }

                _enemyRow.Add(card);
            }

            // Fill remaining slots with empty placeholders.
            for (int i = enemies.Count; i < NightTeam.MaxSlots; i++)
            {
                var ph = new VisualElement();
                ph.AddToClassList("nbm-enemy-card");
                ph.style.opacity = 0.12f;
                _enemyRow.Add(ph);
            }

            if (_enemyCountLabel != null)
                _enemyCountLabel.text = GameLocalization.Format("night.modal.units", enemies.Count);
        }

        private void BuildPlayerSlots()
        {
            if (_playerRow == null) return;
            for (int i = 0; i < NightTeam.MaxSlots; i++)
            {
                var sv = new NightPrepSlotView(i);
                sv.OnClicked += OnSlotClicked;
                _slotViews[i] = sv;
                _playerRow.Add(sv.Root);
            }
        }

        private VisualElement BuildVillagerEntry(NightFighter fighter)
        {
            var row = new VisualElement();
            row.AddToClassList("nbm-villager-entry");

            var nameLabel = new Label(fighter.DisplayName);
            nameLabel.AddToClassList("nbm-villager-entry__name");

            var statsLabel = new Label(GameLocalization.Format("night.modal.stats.fighter", fighter.FinalAttack, fighter.FinalMaxHealth));
            statsLabel.AddToClassList("nbm-villager-entry__stats");

            var badge = new Label(GameLocalization.Get("night.modal.slot.front"));
            badge.name = "nbm-villager-badge";
            badge.AddToClassList("nbm-villager-entry__badge");
            badge.AddToClassList("lk-hidden");

            row.Add(nameLabel);
            row.Add(statsLabel);
            row.Add(badge);

            row.RegisterCallback<ClickEvent>(_ => OnVillagerClicked(fighter));

            return row;
        }

        // ── Battle log ────────────────────────────────────────────────────────────

        private void AddLog(string message, string extraClass = null)
        {
            if (_battleLog == null) return;

            var label = new Label($"> {message}");
            label.AddToClassList("nbm-log-entry");
            if (!string.IsNullOrEmpty(extraClass)) label.AddToClassList(extraClass);

            _battleLog.Add(label);
            _logLineCount++;

            if (_logLineCount > maxLogLines && _battleLog.contentContainer.childCount > 0)
            {
                _battleLog.contentContainer.RemoveAt(0);
                _logLineCount--;
            }

            _battleLog.schedule.Execute(() =>
                _battleLog.verticalScroller.value = _battleLog.verticalScroller.highValue
            ).StartingIn(0);
        }

        // ── Localization ──────────────────────────────────────────────────────────

        private void BindStaticText()
        {
            if (_btnStart  != null) _btnStart.text  = GameLocalization.Get("night.modal.btn.start");
            if (_btnFast   != null) _btnFast.text   = GameLocalization.Get("night.modal.btn.fast");
            if (_btnCancel != null) _btnCancel.text = GameLocalization.Get("night.modal.btn.autoDeploy");
            if (_btnReturn != null) _btnReturn.text = GameLocalization.Get("night.modal.btn.return");

            if (_lblEnemySection    != null) _lblEnemySection.text    = GameLocalization.Get("night.modal.section.enemy");
            if (_lblColonySection   != null) _lblColonySection.text   = GameLocalization.Get("night.modal.section.colony");
            if (_lblDefendersHeader != null) _lblDefendersHeader.text = GameLocalization.Get("night.modal.section.defenders");
            if (_lblLogHeader       != null) _lblLogHeader.text       = GameLocalization.Get("night.modal.section.log");
            if (_lblShopHeader      != null) _lblShopHeader.text      = GameLocalization.Get("night.modal.section.shop");

            if (_assignHint != null) _assignHint.text = GameLocalization.Get("night.modal.hint.assign");
            if (_shopHint   != null) _shopHint.text   = GameLocalization.Get("night.modal.hint.shop");
        }

        // ── Misc UI helpers ───────────────────────────────────────────────────────

        private void SetVisible(bool visible)
        {
            if (_root == null) return;
            _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void SetStatus(string text)
        {
            if (_statusLabel != null) _statusLabel.text = text.ToUpper();
        }

        private void UpdatePlayerCountLabel()
        {
            if (_playerCountLabel != null)
                _playerCountLabel.text = GameLocalization.Format("night.modal.slotsStatus", _team.FilledSlotCount, NightTeam.MaxSlots);
        }

        private void LockVillagerList(bool locked)
        {
            foreach (var kv in _villagerEls)
                kv.Value.SetEnabled(!locked);
        }

        private static string ComputeThreatLabel(NightWaveDefinition wave)
        {
            int totalAtk = 0;
            if (wave != null)
                foreach (var e in wave.BuildEnemyList()) totalAtk += e.Attack;

            string tierKey = totalAtk <= 3 ? "night.modal.threat.low"
                           : totalAtk <= 8 ? "night.modal.threat.moderate"
                           : totalAtk <= 15 ? "night.modal.threat.high"
                           : "night.modal.threat.critical";
            return GameLocalization.Format("night.modal.threat", GameLocalization.Get(tierKey));
        }

        // ── Debug hotkeys ─────────────────────────────────────────────────────────
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void HandleDebugKeys()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.bKey.wasPressedThisFrame && _phase == Phase.Prep)
            {
                Debug.Log("[DEBUG] B — force Start Battle");
                OnStartBattleClicked();
            }
            if (kb.vKey.wasPressedThisFrame && _phase == Phase.Battle)
            {
                Debug.Log("[DEBUG] V — Force Victory");
                _activeLane?.ForceEnd();
            }
            if (kb.lKey.wasPressedThisFrame && _phase == Phase.Battle && _activeLane != null)
            {
                Debug.Log("[DEBUG] L — Force Defeat");
                foreach (var u in _activeLane.Defenders) u.TakeDamage(u.MaxHP * 99);
                _activeLane.Tick(0f);
            }
        }
#endif
    }
}
