using UnityEngine;
using UnityEngine.UIElements;

namespace Markyu.LastKernel
{
    /// <summary>
    /// UI Toolkit HUD covering both the day and night combat phases.
    /// Replaces DayHUD, NightHUD, DayTimeUI, and CardStatsUI.
    ///
    /// Day strip  — phase label, day counter, time progress bar, pace toggle,
    ///              resource stats (food / gold / cards), Start Night button.
    /// Night strip — phase label, wave number, base HP bar, enemy count, speed toggle.
    ///
    /// Setup: add to a GameObject with a UIDocument set to GameHUDView.uxml.
    /// Also add UIEventBusBridge to the same GameObject so Start Night routes correctly.
    /// </summary>
    [UIScreen("Assets/_Project/UI/UXML/Game/GameHUDView.uxml", sortingOrder: 0)]
    public sealed class GameHUDController : UIToolkitScreenController
    {

        // ── Day strip ──────────────────────────────────────────────────────────

        private VisualElement _hudDay;
        private Label         _phaseLabel;
        private Label         _dayLabel;
        private VisualElement _timeFill;
        private Button        _paceButton;
        private Label         _nutritionLabel;
        private Label         _currencyLabel;
        private Label         _cardsLabel;
        private Button        _startNightButton;

        // ── Night strip ────────────────────────────────────────────────────────

        private VisualElement _hudNight;
        private Label         _nightPhaseLabel;
        private Label         _waveLabel;
        private VisualElement _hpFill;
        private Label         _hpLabel;
        private Label         _enemiesLabel;
        private Button        _speedButton;

        private bool _isDoubleSpeed;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        protected override void OnEnable()
        {
            base.OnEnable();

            if (DefensePhaseController.Instance != null)
                DefensePhaseController.Instance.OnPhaseChanged += HandlePhaseChanged;
            if (BaseCoreController.Instance != null)
                BaseCoreController.Instance.OnDamaged += UpdateBaseHP;
            if (NightBattlefieldController.Instance != null)
                NightBattlefieldController.Instance.OnEnemyCountChanged += UpdateEnemyCount;

            TimeManager tm = TimeManager.Instance;
            if (tm != null)
            {
                tm.OnDayStarted       += UpdateDayLabel;
                tm.OnTimePaceChanged  += UpdatePaceButton;
            }

            if (CardManager.Instance != null)
                CardManager.Instance.OnStatsChanged += UpdateResources;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (DefensePhaseController.Instance != null)
                DefensePhaseController.Instance.OnPhaseChanged -= HandlePhaseChanged;
            if (BaseCoreController.Instance != null)
                BaseCoreController.Instance.OnDamaged -= UpdateBaseHP;
            if (NightBattlefieldController.Instance != null)
                NightBattlefieldController.Instance.OnEnemyCountChanged -= UpdateEnemyCount;

            TimeManager tm = TimeManager.Instance;
            if (tm != null)
            {
                tm.OnDayStarted       -= UpdateDayLabel;
                tm.OnTimePaceChanged  -= UpdatePaceButton;
            }

            if (CardManager.Instance != null)
                CardManager.Instance.OnStatsChanged -= UpdateResources;
        }

        protected override void Start()
        {
            base.Start();

            DefensePhaseController dpc = DefensePhaseController.Instance;
            if (dpc != null)
                HandlePhaseChanged(dpc.CurrentPhase);
            else
                ShowDay(true);

            if (BaseCoreController.Instance != null)
                UpdateBaseHP(BaseCoreController.Instance.CurrentHP, BaseCoreController.Instance.MaxHP);

            TimeManager tm = TimeManager.Instance;
            if (tm != null)
            {
                UpdateDayLabel(tm.CurrentDay);
                UpdatePaceButton(tm.CurrentPace);
            }

            if (CardManager.Instance != null)
                UpdateResources(CardManager.Instance.GetStatsSnapshot());
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }

        private void Update()
        {
            if (_timeFill == null) return;
            float pct = TimeManager.Instance?.NormalizedTime ?? 0f;
            _timeFill.style.width = Length.Percent(pct * 100f);
        }

        // ── Binding ────────────────────────────────────────────────────────────

        protected override void OnBind()
        {
            Root.pickingMode = PickingMode.Ignore;

            _hudDay           = Root.Q<VisualElement>("hud-day");
            _phaseLabel       = Root.Q<Label>        ("lbl-phase");
            _dayLabel         = Root.Q<Label>        ("lbl-day");
            _timeFill         = Root.Q<VisualElement>("fill-time");
            _paceButton       = Root.Q<Button>       ("btn-pace");
            _nutritionLabel   = Root.Q<Label>        ("lbl-nutrition");
            _currencyLabel    = Root.Q<Label>        ("lbl-currency");
            _cardsLabel       = Root.Q<Label>        ("lbl-cards");
            _startNightButton = Root.Q<Button>       ("btn-start-night");

            _hudNight        = Root.Q<VisualElement>("hud-night");
            _nightPhaseLabel = Root.Q<Label>        ("lbl-night-phase");
            _waveLabel       = Root.Q<Label>        ("lbl-wave");
            _hpFill          = Root.Q<VisualElement>("fill-base-hp");
            _hpLabel         = Root.Q<Label>        ("lbl-base-hp");
            _enemiesLabel    = Root.Q<Label>        ("lbl-enemies");
            _speedButton     = Root.Q<Button>       ("btn-speed");

            _startNightButton.clicked += UIEventBus.RaiseStartNightRequested;
            _paceButton.clicked       += HandlePaceToggle;
            _speedButton.clicked      += ToggleSpeed;
        }

        // ── Public API ─────────────────────────────────────────────────────────

        public void SetWaveNumber(int wave)
        {
            if (_waveLabel != null)
                _waveLabel.text = GameLocalization.Format("hud.wave", wave);
        }

        // ── Localization ───────────────────────────────────────────────────────

        public override void OnLocalizationRefresh()
        {
            base.OnLocalizationRefresh();

            if (_phaseLabel       != null) _phaseLabel.text       = GameLocalization.Get("hud.day");
            if (_nightPhaseLabel  != null) _nightPhaseLabel.text  = GameLocalization.Get("hud.night");
            if (_startNightButton != null) _startNightButton.text = GameLocalization.Get("hud.startNight");
            UpdateSpeedButton();

            TimeManager tm = TimeManager.Instance;
            if (tm != null)
            {
                UpdateDayLabel(tm.CurrentDay);
                UpdatePaceButton(tm.CurrentPace);
            }

            if (CardManager.Instance != null)
                UpdateResources(CardManager.Instance.GetStatsSnapshot());
        }

        // ── Phase ──────────────────────────────────────────────────────────────

        private void HandlePhaseChanged(DefensePhase phase)
        {
            ShowDay(phase == DefensePhase.Day || phase == DefensePhase.NightPrep);
        }

        private void ShowDay(bool day)
        {
            _hudDay?.EnableInClassList("lk-hidden", !day);
            _hudNight?.EnableInClassList("lk-hidden", day);
        }

        // ── Day: time progress ─────────────────────────────────────────────────

        private void UpdateDayLabel(int day)
        {
            if (_dayLabel != null)
                _dayLabel.text = GameLocalization.Format("day.current", day);
        }

        private void UpdatePaceButton(TimePace pace)
        {
            if (_paceButton == null) return;
            _paceButton.text = pace switch
            {
                TimePace.Paused => GameLocalization.Get("hud.pace.paused"),
                TimePace.Fast   => GameLocalization.Get("hud.pace.fast"),
                _               => GameLocalization.Get("hud.pace.normal"),
            };
        }

        private void HandlePaceToggle()
        {
            TimeManager.Instance?.CycleTimePace(out _);
        }

        // ── Day: resources ─────────────────────────────────────────────────────

        private void UpdateResources(StatsSnapshot stats)
        {
            if (_nutritionLabel != null)
                _nutritionLabel.text = GameLocalization.Format("hud.nutrition", stats.TotalNutrition, stats.NutritionNeed);
            if (_currencyLabel != null)
                _currencyLabel.text = GameLocalization.Format("hud.currency", stats.Currency);
            if (_cardsLabel != null)
                _cardsLabel.text = GameLocalization.Format("hud.cards", stats.CardsOwned, stats.CardLimit);
        }

        // ── Night: base HP ─────────────────────────────────────────────────────

        private void UpdateBaseHP(int current, int max)
        {
            if (_hpLabel != null)
                _hpLabel.text = GameLocalization.Format("hud.baseHP", current, max);

            if (_hpFill != null)
            {
                float pct = max > 0 ? (float)current / max : 0f;
                _hpFill.style.width = Length.Percent(pct * 100f);
                _hpFill.EnableInClassList("lk-progress-bar__fill--warning", pct is > 0.2f and <= 0.4f);
                _hpFill.EnableInClassList("lk-progress-bar__fill--danger",  pct <= 0.2f);
            }
        }

        // ── Night: enemy count ─────────────────────────────────────────────────

        private void UpdateEnemyCount(int alive, int total)
        {
            if (_enemiesLabel != null)
                _enemiesLabel.text = GameLocalization.Format("hud.enemies", alive, total);
        }

        // ── Night: speed toggle ────────────────────────────────────────────────

        private void ToggleSpeed()
        {
            _isDoubleSpeed = !_isDoubleSpeed;
            Time.timeScale = _isDoubleSpeed ? 2f : 1f;
            UpdateSpeedButton();
        }

        private void UpdateSpeedButton()
        {
            if (_speedButton != null)
                _speedButton.text = _isDoubleSpeed ? "2×" : "1×";
        }
    }
}
