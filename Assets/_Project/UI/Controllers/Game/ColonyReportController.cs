using UnityEngine;
using UnityEngine.UIElements;

namespace Markyu.LastKernel
{
    [UIScreen("Assets/_Project/UI/UXML/Game/ColonyReportModal.uxml", sortingOrder: 60)]
    public sealed class ColonyReportController : UIToolkitScreenController
    {
        public static ColonyReportController Instance { get; private set; }

        protected override bool AffectedByUIScale => true;

        private NightCombatResult _lastNightResult;

        // ── Backdrop & close ──────────────────────────────────────────────────
        private VisualElement _backdrop;
        private Button        _closeBtn;

        // ── Section containers ────────────────────────────────────────────────
        private VisualElement _sectionLastNight;

        // ── Population & Morale ───────────────────────────────────────────────
        private Label         _lblCharacters;
        private Label         _lblCasualties;
        private Label         _lblInjured;
        private Label         _lblMoraleValue;
        private VisualElement _fillMorale;
        private Label         _lblFatigueValue;
        private VisualElement _fillFatigue;

        // ── Resources ─────────────────────────────────────────────────────────
        private Label         _lblFood;
        private Label         _lblCurrency;
        private Label         _lblCards;
        private VisualElement _rowExcessCards;
        private Label         _lblExcessCards;

        // ── Colony Pressures ──────────────────────────────────────────────────
        private Label         _lblPressuresClear;
        private VisualElement _rowStructural;
        private VisualElement _barStructural;
        private Label         _lblStructuralValue;
        private VisualElement _fillStructural;
        private VisualElement _rowPower;
        private VisualElement _barPower;
        private Label         _lblPowerValue;
        private VisualElement _fillPower;
        private VisualElement _rowCorruption;
        private VisualElement _barCorruption;
        private Label         _lblCorruptionValue;
        private VisualElement _fillCorruption;

        // ── Last Night ────────────────────────────────────────────────────────
        private Label _lblLnEnemies;
        private Label _lblLnDefenders;
        private Label _lblLnMorale;
        private Label _lblLnFatigue;
        private Label _lblLnSalvage;

        // ── Survival Record ───────────────────────────────────────────────────
        private Label _lblSurvDay;
        private Label _lblSurvNights;
        private Label _lblSurvTotalCas;
        private Label _lblSurvSalvage;

        private IVisualElementScheduledItem _refreshSchedule;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            Instance = this;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            NightBattleManager.OnBattleComplete += CacheLastNight;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            NightBattleManager.OnBattleComplete -= CacheLastNight;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this) Instance = null;
        }

        // ── Binding ───────────────────────────────────────────────────────────

        protected override void OnBind()
        {
            _backdrop = Root.Q<VisualElement>("colony-report-backdrop");
            _closeBtn = Root.Q<Button>("btn-report-close");

            _sectionLastNight = Root.Q<VisualElement>("section-last-night");

            _lblCharacters   = Root.Q<Label>("lbl-pop-characters-value");
            _lblCasualties   = Root.Q<Label>("lbl-pop-casualties-value");
            _lblInjured      = Root.Q<Label>("lbl-pop-injured-value");
            _lblMoraleValue  = Root.Q<Label>("lbl-morale-value");
            _fillMorale      = Root.Q<VisualElement>("fill-morale");
            _lblFatigueValue = Root.Q<Label>("lbl-fatigue-value");
            _fillFatigue     = Root.Q<VisualElement>("fill-fatigue");

            _lblFood        = Root.Q<Label>("lbl-res-food-value");
            _lblCurrency    = Root.Q<Label>("lbl-res-currency-value");
            _lblCards       = Root.Q<Label>("lbl-res-cards-value");
            _rowExcessCards = Root.Q<VisualElement>("row-excess-cards");
            _lblExcessCards = Root.Q<Label>("lbl-res-excess-value");

            _lblPressuresClear  = Root.Q<Label>("lbl-pressures-clear");
            _rowStructural      = Root.Q<VisualElement>("row-structural");
            _barStructural      = Root.Q<VisualElement>("bar-structural");
            _lblStructuralValue = Root.Q<Label>("lbl-structural-value");
            _fillStructural     = Root.Q<VisualElement>("fill-structural");
            _rowPower           = Root.Q<VisualElement>("row-power");
            _barPower           = Root.Q<VisualElement>("bar-power");
            _lblPowerValue      = Root.Q<Label>("lbl-power-value");
            _fillPower          = Root.Q<VisualElement>("fill-power");
            _rowCorruption      = Root.Q<VisualElement>("row-corruption");
            _barCorruption      = Root.Q<VisualElement>("bar-corruption");
            _lblCorruptionValue = Root.Q<Label>("lbl-corruption-value");
            _fillCorruption     = Root.Q<VisualElement>("fill-corruption");

            _lblLnEnemies   = Root.Q<Label>("lbl-ln-enemies-value");
            _lblLnDefenders = Root.Q<Label>("lbl-ln-defenders-value");
            _lblLnMorale    = Root.Q<Label>("lbl-ln-morale-value");
            _lblLnFatigue   = Root.Q<Label>("lbl-ln-fatigue-value");
            _lblLnSalvage   = Root.Q<Label>("lbl-ln-salvage-value");

            _lblSurvDay      = Root.Q<Label>("lbl-surv-day-value");
            _lblSurvNights   = Root.Q<Label>("lbl-surv-nights-value");
            _lblSurvTotalCas = Root.Q<Label>("lbl-surv-total-cas-value");
            _lblSurvSalvage  = Root.Q<Label>("lbl-surv-salvage-value");

            UIFonts.AccentSemibold(Root.Q<Label>("lbl-report-title"));

            if (_closeBtn != null) _closeBtn.clicked += Hide;
            _backdrop?.RegisterCallback<ClickEvent>(OnBackdropClick);
        }

        public override void OnLocalizationRefresh()
        {
            base.OnLocalizationRefresh();
            BindStaticLabels();
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Show()
        {
            PopulateData();
            _backdrop?.RemoveFromClassList("lk-hidden");
            _refreshSchedule = _backdrop?.schedule.Execute(PopulateData).Every(2000).StartingIn(2000);
        }

        public void Hide()
        {
            _backdrop?.AddToClassList("lk-hidden");
            _refreshSchedule?.Pause();
        }

        // ── Data population ───────────────────────────────────────────────────

        private void PopulateData()
        {
            RunStateData   state    = RunStateManager.Instance?.State;
            StatsSnapshot  snapshot = CardManager.Instance != null
                ? CardManager.Instance.GetStatsSnapshot()
                : default;
            TimeManager    time     = TimeManager.Instance;

            if (state == null) return;

            PopulatePopulationMorale(state, snapshot);
            PopulateResources(snapshot);
            PopulatePressures(state);
            PopulateSurvivalRecord(state, time);

            bool showLastNight = state.NightsSurvived > 0 && _lastNightResult != null;
            _sectionLastNight.EnableInClassList("lk-hidden", !showLastNight);
            if (showLastNight)
                PopulateLastNight(_lastNightResult);

            Root.Q<Label>("lbl-report-subtitle").text =
                GameLocalization.GetOptional("colony_report.subtitle", "DAY {0}")
                    .Replace("{0}", time != null ? time.CurrentDay.ToString() : "—");
        }

        private void PopulatePopulationMorale(RunStateData state, StatsSnapshot snap)
        {
            _lblCharacters.text   = snap.TotalCharacters.ToString();
            _lblCasualties.text   = state.Casualties.ToString();
            _lblInjured.text      = state.InjuredPersonnel.ToString();

            _lblMoraleValue.text  = state.Morale.ToString();
            SetBarFill(_fillMorale, state.Morale / 100f);
            _fillMorale.EnableInClassList("lk-progress-bar__fill--warning",
                state.Morale < 40 && state.Morale >= 20);
            _fillMorale.EnableInClassList("lk-progress-bar__fill--danger",
                state.Morale < 20);

            _lblFatigueValue.text = state.Fatigue.ToString();
            SetBarFill(_fillFatigue, state.Fatigue / 100f);
            _fillFatigue.EnableInClassList("lk-progress-bar__fill--warning",
                state.Fatigue > 60 && state.Fatigue <= 80);
            _fillFatigue.EnableInClassList("lk-progress-bar__fill--danger",
                state.Fatigue > 80);
        }

        private void PopulateResources(StatsSnapshot snap)
        {
            int foodDelta = snap.TotalNutrition - snap.NutritionNeed;
            if (foodDelta >= 0)
            {
                _lblFood.text = GameLocalization.GetOptional(
                    "colony_report.food_surplus", "+{0} SURPLUS").Replace("{0}", foodDelta.ToString());
                _lblFood.EnableInClassList("colony-report__value--surplus", true);
                _lblFood.EnableInClassList("colony-report__value--danger",  false);
            }
            else
            {
                _lblFood.text = GameLocalization.GetOptional(
                    "colony_report.food_deficit", "-{0} DEFICIT").Replace("{0}", Mathf.Abs(foodDelta).ToString());
                _lblFood.EnableInClassList("colony-report__value--surplus", false);
                _lblFood.EnableInClassList("colony-report__value--danger",  true);
            }

            _lblCurrency.text = snap.Currency.ToString();
            _lblCards.text    = GameLocalization.GetOptional("colony_report.cards_fraction", "{0} / {1}")
                .Replace("{0}", snap.CardsOwned.ToString())
                .Replace("{1}", snap.CardLimit.ToString());

            bool hasExcess = snap.ExcessCards > 0;
            _rowExcessCards.EnableInClassList("lk-hidden", !hasExcess);
            if (hasExcess)
                _lblExcessCards.text = snap.ExcessCards.ToString();
        }

        private void PopulatePressures(RunStateData state)
        {
            bool anyPressure = state.StructuralDamage > 0
                            || state.PowerDeficit    > 0
                            || state.Corruption      > 0;

            _lblPressuresClear.EnableInClassList("lk-hidden", anyPressure);

            SetPressureRow(_rowStructural, _barStructural, _fillStructural,
                           _lblStructuralValue, state.StructuralDamage, !anyPressure);
            SetPressureRow(_rowPower,      _barPower,      _fillPower,
                           _lblPowerValue,      state.PowerDeficit,     !anyPressure);
            SetPressureRow(_rowCorruption, _barCorruption, _fillCorruption,
                           _lblCorruptionValue, state.Corruption,       !anyPressure);
        }

        private void PopulateLastNight(NightCombatResult r)
        {
            _lblLnEnemies.text = GameLocalization.GetOptional("colony_report.ln_enemies", "{0} / {1} KILLED")
                .Replace("{0}", r.EnemiesKilled.ToString())
                .Replace("{1}", r.TotalEnemies.ToString());

            _lblLnDefenders.text = r.DeadDefenders.Count.ToString();

            _lblLnMorale.text  = FormatDelta(r.MoraleDelta);
            _lblLnFatigue.text = FormatDelta(r.FatigueDelta);
            _lblLnSalvage.text = FormatDelta(r.SalvageDelta);

            ApplyDeltaClass(_lblLnMorale,  r.MoraleDelta,  positiveIsGood: true);
            ApplyDeltaClass(_lblLnFatigue, r.FatigueDelta, positiveIsGood: false);
        }

        private void PopulateSurvivalRecord(RunStateData state, TimeManager time)
        {
            _lblSurvDay.text      = time != null ? time.CurrentDay.ToString() : "—";
            _lblSurvNights.text   = state.NightsSurvived.ToString();
            _lblSurvTotalCas.text = state.Casualties.ToString();
            _lblSurvSalvage.text  = state.SalvageValue.ToString();
        }

        // ── Static label binding (called on open + localization change) ────────

        private void BindStaticLabels()
        {
            BindKey("lbl-report-title",       "colony_report.title",             "COLONY REPORT");
            BindKey("lbl-section-population", "colony_report.section.population","POPULATION & MORALE");
            BindKey("lbl-section-resources",  "colony_report.section.resources", "RESOURCES");
            BindKey("lbl-section-pressures",  "colony_report.section.pressures", "COLONY PRESSURES");
            BindKey("lbl-section-lastnight",  "colony_report.section.lastnight", "LAST NIGHT");
            BindKey("lbl-section-survival",   "colony_report.section.survival",  "SURVIVAL RECORD");

            if (_lblPressuresClear != null) _lblPressuresClear.text = GameLocalization.GetOptional("colony_report.pressures_clear", "NO ACTIVE THREATS");
            if (_closeBtn != null)          _closeBtn.text          = GameLocalization.GetOptional("colony_report.close",            "CLOSE REPORT");

            BindKey("lbl-pop-characters-key",  "colony_report.pop.characters",  "PERSONNEL");
            BindKey("lbl-pop-casualties-key",  "colony_report.pop.casualties",  "CASUALTIES");
            BindKey("lbl-pop-injured-key",     "colony_report.pop.injured",     "INJURED");
            BindKey("lbl-morale-key",          "colony_report.morale",          "MORALE");
            BindKey("lbl-fatigue-key",         "colony_report.fatigue",         "FATIGUE");
            BindKey("lbl-res-food-key",        "colony_report.res.food",        "FOOD");
            BindKey("lbl-res-currency-key",    "colony_report.res.currency",    "CURRENCY");
            BindKey("lbl-res-cards-key",       "colony_report.res.cards",       "CARDS");
            BindKey("lbl-res-excess-key",      "colony_report.res.excess",      "EXCESS CARDS");
            BindKey("lbl-structural-key",      "colony_report.structural",      "STRUCTURAL DMG");
            BindKey("lbl-power-key",           "colony_report.power",           "POWER DEFICIT");
            BindKey("lbl-corruption-key",      "colony_report.corruption",      "CORRUPTION");
            BindKey("lbl-ln-enemies-key",      "colony_report.ln.enemies",      "ENEMIES");
            BindKey("lbl-ln-defenders-key",    "colony_report.ln.defenders",    "DEFENDERS LOST");
            BindKey("lbl-ln-morale-key",       "colony_report.ln.morale",       "MORALE IMPACT");
            BindKey("lbl-ln-fatigue-key",      "colony_report.ln.fatigue",      "FATIGUE IMPACT");
            BindKey("lbl-ln-salvage-key",      "colony_report.ln.salvage",      "SALVAGE");
            BindKey("lbl-surv-day-key",        "colony_report.surv.day",        "CURRENT DAY");
            BindKey("lbl-surv-nights-key",     "colony_report.surv.nights",     "NIGHTS SURVIVED");
            BindKey("lbl-surv-total-cas-key",  "colony_report.surv.casualties", "TOTAL CASUALTIES");
            BindKey("lbl-surv-salvage-key",    "colony_report.surv.salvage",    "TOTAL SALVAGE");
        }

        private void BindKey(string elementName, string locKey, string fallback)
        {
            var label = Root.Q<Label>(elementName);
            if (label != null) label.text = GameLocalization.GetOptional(locKey, fallback);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetBarFill(VisualElement fill, float t)
            => fill.style.width = Length.Percent(Mathf.Clamp01(t) * 100f);

        private static void SetPressureRow(
            VisualElement row, VisualElement bar,
            VisualElement fill, Label valueLabel,
            int value, bool forceHidden)
        {
            bool hidden = forceHidden || value <= 0;
            row.EnableInClassList("lk-hidden", hidden);
            bar.EnableInClassList("lk-hidden", hidden);
            if (!hidden)
            {
                valueLabel.text = value.ToString();
                SetBarFill(fill, value / 100f);
            }
        }

        private static string FormatDelta(int delta)
            => delta >= 0 ? $"+{delta}" : delta.ToString();

        private static void ApplyDeltaClass(Label lbl, int delta, bool positiveIsGood)
        {
            bool good = positiveIsGood ? delta > 0 : delta < 0;
            lbl.EnableInClassList("colony-report__value--surplus", good);
            lbl.EnableInClassList("colony-report__value--danger",  !good && delta != 0);
        }

        private void OnBackdropClick(ClickEvent evt)
        {
            if (evt.target == _backdrop)
                Hide();
        }

        private void CacheLastNight(NightCombatResult result)
        {
            _lastNightResult = result;
        }
    }
}
