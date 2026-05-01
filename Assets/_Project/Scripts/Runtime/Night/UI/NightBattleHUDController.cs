using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Markyu.LastKernel
{
    /// <summary>
    /// UI Toolkit controller for the NightBattleHUD overlay.
    ///
    /// Setup:
    ///   1. Add a GameObject to the scene (e.g. "NightBattleHUD").
    ///   2. Attach UIDocument with NightBattleHUD.uxml as the source asset.
    ///   3. Attach this NightBattleHUDController component.
    ///   4. Set Sort Order on the UIDocument to 50 so it renders above the board.
    ///
    /// This controller subscribes to NightPhaseManager static events.
    /// When OnNightPrepared fires it shows the overlay and populates both teams.
    /// It subscribes to CombatLane events for live HP updates and battle log entries.
    /// Buttons call back into NightPhaseManager via ConfirmBattleStart / AcknowledgeResult.
    ///
    /// Debug hotkeys (Editor + Development builds only):
    ///   N = trigger Start Night (calls DayCycleManager if present)
    ///   B = Start Battle (same as clicking the button)
    ///   V = Force Victory (ends the active lane immediately)
    ///   L = Force Defeat (forces loss — calls lane.ForceEnd with player-lost state)
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class NightBattleHUDController : MonoBehaviour
    {
        // ── Serialised ────────────────────────────────────────────────────────────

        [Tooltip("How many log lines to keep before pruning old entries.")]
        [SerializeField, Min(10)] private int maxLogLines = 40;

        // ── Cached UI element references ──────────────────────────────────────────

        private VisualElement _root;
        private Label         _nightTitleLabel;
        private Label         _waveLabel;
        private Label         _enemyCountLabel;
        private Label         _playerCountLabel;
        private VisualElement _enemyTeamRow;
        private VisualElement _playerTeamRow;
        private Label         _battleStatusLabel;
        private ScrollView    _battleLog;
        private Button        _startBattleButton;
        private Button        _fastResolveButton;
        private Button        _returnDayButton;
        private VisualElement _resultPanel;
        private Label         _resultLabel;
        private Label         _resultSummary;

        // ── Runtime state ─────────────────────────────────────────────────────────

        private CombatLane _activeLane;

        // Maps each CombatUnit to its visual card so we can update HP in real time.
        private readonly Dictionary<CombatUnit, FighterCardRefs> _cards = new();

        private int _logLineCount;

        // ─────────────────────────────────────────────────────────────────────────

        private class FighterCardRefs
        {
            public VisualElement Card;
            public Label         NameLabel;
            public Label         StatsLabel;
            public VisualElement HpFill;
            public Label         HpText;
            public bool          IsEnemy;
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            NightPhaseManager.OnNightPrepared += HandleNightPrepared;
            NightPhaseManager.OnNightComplete += HandleNightComplete;
        }

        private void OnDestroy()
        {
            NightPhaseManager.OnNightPrepared -= HandleNightPrepared;
            NightPhaseManager.OnNightComplete -= HandleNightComplete;
            UnbindLane();
        }

        private void Start()
        {
            var doc  = GetComponent<UIDocument>();
            var root = doc.rootVisualElement;

            _root               = root.Q("night-battle-root");
            _nightTitleLabel    = root.Q<Label>("night-title-label");
            _waveLabel          = root.Q<Label>("wave-label");
            _enemyCountLabel    = root.Q<Label>("enemy-count-label");
            _playerCountLabel   = root.Q<Label>("player-count-label");
            _enemyTeamRow       = root.Q("enemy-team-row");
            _playerTeamRow      = root.Q("player-team-row");
            _battleStatusLabel  = root.Q<Label>("battle-status-label");
            _battleLog          = root.Q<ScrollView>("battle-log");
            _startBattleButton  = root.Q<Button>("start-battle-button");
            _fastResolveButton  = root.Q<Button>("fast-resolve-button");
            _returnDayButton    = root.Q<Button>("return-day-button");
            _resultPanel        = root.Q("battle-result-panel");
            _resultLabel        = root.Q<Label>("battle-result-label");
            _resultSummary      = root.Q<Label>("battle-result-summary");

            _startBattleButton?.RegisterCallback<ClickEvent>(_ => OnStartBattleClicked());
            _fastResolveButton?.RegisterCallback<ClickEvent>(_ => OnFastResolveClicked());
            _returnDayButton?.RegisterCallback<ClickEvent>(_ => OnReturnDayClicked());

            SetVisible(false);
        }

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            HandleDebugKeys();
#endif
        }

        // ── NightPhaseManager event handlers ──────────────────────────────────────

        private void HandleNightPrepared(CombatLane lane, NightWaveDefinition wave)
        {
            _activeLane = lane;
            BindLane(lane);

            int day = TimeManager.Instance?.CurrentDay ?? 1;
            _nightTitleLabel.text = $"NIGHT {day}";
            _waveLabel.text       = wave?.WaveName?.ToUpper() ?? "SYSTEM BREACH DETECTED";

            _enemyTeamRow.Clear();
            _playerTeamRow.Clear();
            _cards.Clear();
            _logLineCount = 0;
            _battleLog.Clear();

            PopulateTeam(_enemyTeamRow,  lane.Enemies,   isEnemy: true);
            PopulateTeam(_playerTeamRow, lane.Defenders, isEnemy: false);

            UpdateCountLabels();
            SetStatus("STANDBY — AWAITING ORDERS");

            _resultPanel.AddToClassList("lk-hidden");
            _startBattleButton.RemoveFromClassList("lk-hidden");
            _fastResolveButton.RemoveFromClassList("lk-hidden");
            _returnDayButton.AddToClassList("lk-hidden");
            _startBattleButton.SetEnabled(true);

            AddLog($"Night {day} begins. Incursion detected.", "nb-log-entry--system");
            if (lane.Defenders.Count == 0)
                AddLog("WARNING: No defenders deployed. Colony is undefended.", "nb-log-entry--death");

            SetVisible(true);
        }

        private void HandleNightComplete(NightCombatResult result)
        {
            bool won = result.PlayerWon;

            _resultLabel.text   = won ? "SYSTEM HELD" : "SYSTEM BREACHED";
            _resultSummary.text = result.GetSummaryText();

            _resultLabel.RemoveFromClassList("nb-result-title--victory");
            _resultLabel.RemoveFromClassList("nb-result-title--defeat");
            _resultLabel.AddToClassList(won ? "nb-result-title--victory" : "nb-result-title--defeat");

            SetStatus(won ? "VICTORY" : "DEFEAT");

            _resultPanel.RemoveFromClassList("lk-hidden");
            _startBattleButton.AddToClassList("lk-hidden");
            _fastResolveButton.AddToClassList("lk-hidden");
            _returnDayButton.RemoveFromClassList("lk-hidden");

            AddLog(won ? "Colony survived the night." : "Colony defenses breached.",
                   won ? "nb-log-entry--victory" : "nb-log-entry--defeat");
        }

        // ── Button handlers ───────────────────────────────────────────────────────

        private void OnStartBattleClicked()
        {
            _startBattleButton.SetEnabled(false);
            SetStatus("COMBAT ACTIVE");
            AddLog("Battle started.", "nb-log-entry--system");
            NightPhaseManager.Instance?.ConfirmBattleStart();
        }

        private void OnFastResolveClicked()
        {
            _fastResolveButton.SetEnabled(false);
            NightPhaseManager.Instance?.SetFastResolve();
            AddLog("Fast resolve activated.", "nb-log-entry--system");
        }

        private void OnReturnDayClicked()
        {
            SetVisible(false);
            NightPhaseManager.Instance?.AcknowledgeResult();
        }

        // ── Fighter card population ───────────────────────────────────────────────

        private void PopulateTeam(VisualElement container, IReadOnlyList<CombatUnit> units, bool isEnemy)
        {
            for (int i = 0; i < units.Count; i++)
            {
                var unit   = units[i];
                bool front = i == 0; // slot 0 is front of the list

                var refs = BuildFighterCard(unit, isEnemy, front);
                _cards[unit] = refs;
                container.Add(refs.Card);
            }

            // Add empty slot placeholders so the row fills horizontal space cleanly.
            int empties = Mathf.Max(0, 5 - units.Count);
            for (int i = 0; i < empties; i++)
            {
                var slot = new VisualElement();
                slot.AddToClassList("nb-fighter-card");
                slot.style.opacity = 0.15f;
                container.Add(slot);
            }
        }

        private FighterCardRefs BuildFighterCard(CombatUnit unit, bool isEnemy, bool isFront)
        {
            var card = new VisualElement();
            card.AddToClassList("nb-fighter-card");
            card.AddToClassList(isEnemy ? "nb-fighter-card--enemy" : "nb-fighter-card--player");
            if (isFront)
                card.AddToClassList(isEnemy ? "nb-fighter-card--front-enemy" : "nb-fighter-card--front-player");

            var nameLabel = new Label(unit.DisplayName);
            nameLabel.AddToClassList("nb-fighter-name");
            nameLabel.AddToClassList(isEnemy ? "nb-fighter-name--enemy" : "nb-fighter-name--player");

            var statsLabel = new Label($"ATK {unit.Attack}  |  DEF {unit.Defense}");
            statsLabel.AddToClassList("nb-fighter-stats");

            var hpBar = new VisualElement();
            hpBar.AddToClassList("nb-hp-bar");
            hpBar.style.overflow = Overflow.Hidden;
            hpBar.style.position = Position.Relative;

            var hpFill = new VisualElement();
            hpFill.AddToClassList("nb-hp-fill");
            if (isEnemy) hpFill.AddToClassList("nb-hp-fill--enemy");
            hpFill.style.width = Length.Percent(100f);
            hpBar.Add(hpFill);

            var hpText = new Label($"{unit.CurrentHP}/{unit.MaxHP}");
            hpText.AddToClassList("nb-fighter-hp-text");

            var roleLabel = new Label(isEnemy ? "ENEMY" : "DEFENDER");
            roleLabel.AddToClassList("nb-fighter-role");

            card.Add(nameLabel);
            card.Add(statsLabel);
            card.Add(hpBar);
            card.Add(hpText);
            card.Add(roleLabel);

            return new FighterCardRefs
            {
                Card       = card,
                NameLabel  = nameLabel,
                StatsLabel = statsLabel,
                HpFill     = hpFill,
                HpText     = hpText,
                IsEnemy    = isEnemy
            };
        }

        // ── CombatLane event handling ─────────────────────────────────────────────

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
                AddLog($"{attacker.DisplayName} attacks {target.DisplayName} — MISSED.", "nb-log-entry");
            }
            else
            {
                string critTag = isCrit ? " [CRIT]" : "";
                AddLog($"{attacker.DisplayName} hits {target.DisplayName} for {damage}{critTag}.", "nb-log-entry--damage");
            }

            RefreshCard(target);
        }

        private void HandleUnitDied(CombatUnit unit)
        {
            AddLog($"{unit.DisplayName} is destroyed.", "nb-log-entry--death");
            RefreshCard(unit);
            UpdateCountLabels();

            // Advance front-slot highlight on surviving units.
            RefreshFrontHighlights();
        }

        private void HandleCombatEnded(bool playerWon)
        {
            SetStatus(playerWon ? "VICTORY" : "DEFEAT");
        }

        // ── Card refresh ──────────────────────────────────────────────────────────

        private void RefreshCard(CombatUnit unit)
        {
            if (!_cards.TryGetValue(unit, out var refs)) return;

            if (!unit.IsAlive)
            {
                refs.Card.AddToClassList("nb-fighter-card--dead");
                refs.NameLabel.AddToClassList("nb-fighter-name--dead");
                refs.HpFill.style.width = Length.Percent(0f);
                refs.HpText.text = "DESTROYED";
                return;
            }

            float fraction = unit.HPFraction;
            refs.HpFill.style.width = Length.Percent(fraction * 100f);
            refs.HpText.text = $"{unit.CurrentHP}/{unit.MaxHP}";

            // Switch fill colour when HP is critically low.
            if (fraction < 0.35f)
                refs.HpFill.AddToClassList("nb-hp-fill--low");
            else
                refs.HpFill.RemoveFromClassList("nb-hp-fill--low");
        }

        private void RefreshFrontHighlights()
        {
            UpdateFrontInRow(_activeLane?.Defenders, isEnemy: false);
            UpdateFrontInRow(_activeLane?.Enemies,   isEnemy: true);
        }

        private void UpdateFrontInRow(IReadOnlyList<CombatUnit> units, bool isEnemy)
        {
            if (units == null) return;

            string frontClass = isEnemy ? "nb-fighter-card--front-enemy" : "nb-fighter-card--front-player";
            bool   frontFound = false;

            foreach (var unit in units)
            {
                if (!_cards.TryGetValue(unit, out var refs)) continue;

                refs.Card.RemoveFromClassList(frontClass);

                if (!frontFound && unit.IsAlive)
                {
                    refs.Card.AddToClassList(frontClass);
                    frontFound = true;
                }
            }
        }

        // ── Battle log ────────────────────────────────────────────────────────────

        private void AddLog(string message, string extraClass = null)
        {
            var label = new Label($"> {message}");
            label.AddToClassList("nb-log-entry");
            if (!string.IsNullOrEmpty(extraClass))
                label.AddToClassList(extraClass);

            _battleLog.Add(label);
            _logLineCount++;

            // Prune oldest entries to keep memory bounded.
            if (_logLineCount > maxLogLines && _battleLog.contentContainer.childCount > 0)
            {
                _battleLog.contentContainer.RemoveAt(0);
                _logLineCount--;
            }

            // Scroll to bottom next frame (after layout).
            _battleLog.schedule.Execute(() =>
            {
                _battleLog.verticalScroller.value = _battleLog.verticalScroller.highValue;
            }).StartingIn(0);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void SetVisible(bool visible)
        {
            if (_root == null) return;
            if (visible) _root.RemoveFromClassList("lk-hidden");
            else         _root.AddToClassList("lk-hidden");
        }

        private void SetStatus(string text)
        {
            if (_battleStatusLabel != null)
                _battleStatusLabel.text = text.ToUpper();
        }

        private void UpdateCountLabels()
        {
            if (_activeLane == null) return;

            int enemiesAlive    = 0;
            int defendersAlive  = 0;

            foreach (var u in _activeLane.Enemies)   if (u.IsAlive) enemiesAlive++;
            foreach (var u in _activeLane.Defenders) if (u.IsAlive) defendersAlive++;

            if (_enemyCountLabel  != null) _enemyCountLabel.text  = $"{enemiesAlive} / {_activeLane.Enemies.Count} UNITS";
            if (_playerCountLabel != null) _playerCountLabel.text  = $"{defendersAlive} / {_activeLane.Defenders.Count} DEFENDERS";
        }

        // ── Debug hotkeys ─────────────────────────────────────────────────────────

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void HandleDebugKeys()
        {
            if (Input.GetKeyDown(KeyCode.B))
            {
                if (NightPhaseManager.Instance != null)
                {
                    Debug.Log("[DEBUG] B — ConfirmBattleStart");
                    OnStartBattleClicked();
                }
            }

            if (Input.GetKeyDown(KeyCode.V))
            {
                Debug.Log("[DEBUG] V — Force Victory (ForceEnd)");
                _activeLane?.ForceEnd();
            }

            // L = force defeat: drain all defenders' HP so lane ends with enemy win.
            if (Input.GetKeyDown(KeyCode.L))
            {
                Debug.Log("[DEBUG] L — Force Defeat (drain defenders)");
                if (_activeLane != null)
                {
                    foreach (var u in _activeLane.Defenders)
                    {
                        u.TakeDamage(u.MaxHP * 99);
                    }
                    // Manually tick once to trigger end-condition check.
                    _activeLane.Tick(0f);
                }
            }
        }
#endif
    }
}
