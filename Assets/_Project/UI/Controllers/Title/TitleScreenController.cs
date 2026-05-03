using UnityEngine;
using UnityEngine.UIElements;

namespace Markyu.LastKernel
{
    /// <summary>
    /// UI Toolkit screen controller for the Main Menu / Title screen.
    ///
    /// Owns all four sub-panel controllers (modal, prefs, saves, options).
    /// User-intent signals flow outward via UIEventBus — no direct calls to
    /// GameDirector or Application from here (except Application.Quit which
    /// is wired through ModalController's onAccept callback).
    ///
    /// Setup: add this component to a GameObject that also has a UIDocument
    /// referencing TitleScreenView.uxml and the PanelSettings asset.
    /// </summary>
    [UIScreen("Assets/_Project/UI/UXML/Title/TitleScreenView.uxml", sortingOrder: 0)]
    public sealed class TitleScreenController : UIToolkitScreenController
    {
        // ── Labels (bound to Localizer — auto-refreshed on language change) ────

        private Label             _subtitleLabel;
        private Label             _versionLabel;
        private Label             _titleKickerLabel;
        private Label             _telemetryTitleLabel;
        private Label             _telemetryCoreKeyLabel;
        private Label             _telemetryCoreValueLabel;
        private Label             _telemetrySignalKeyLabel;
        private Label             _telemetrySignalValueLabel;
        private Label             _telemetrySectorKeyLabel;
        private Label             _telemetrySectorValueLabel;
        private Label             _telemetryThreatKeyLabel;
        private Label             _telemetryThreatValueLabel;
        private Label             _bootTargetKeyLabel;
        private Label             _bootTargetValueLabel;
        private TitleLogoAnimator _logoAnimator;

        // ── Nav buttons ────────────────────────────────────────────────────────

        private Button _newGameButton;
        private Button _loadGameButton;
        private Button _achievementsButton;
        private Button _optionsButton;
        private Button _quitButton;

        // ── Sub-panel controllers ──────────────────────────────────────────────

        private ModalController          _modal;
        private GameplayPrefsController  _gameplayPrefs;
        private SavedGamesController     _savedGames;
        private GameOptionsController    _gameOptions;
        private LanguageModalController  _langModal;
        private AchievementsController   _achievements;

        // ── OnBind ─────────────────────────────────────────────────────────────

        protected override void OnBind()
        {
            // Labels
            _subtitleLabel             = Root.Q<Label>("lbl-subtitle");
            _versionLabel              = Root.Q<Label>("lbl-version");
            _titleKickerLabel          = Root.Q<Label>("lbl-title-kicker");
            _telemetryTitleLabel       = Root.Q<Label>("lbl-telemetry-title");
            _telemetryCoreKeyLabel     = Root.Q<Label>("lbl-telemetry-core-key");
            _telemetryCoreValueLabel   = Root.Q<Label>("lbl-telemetry-core-value");
            _telemetrySignalKeyLabel   = Root.Q<Label>("lbl-telemetry-signal-key");
            _telemetrySignalValueLabel = Root.Q<Label>("lbl-telemetry-signal-value");
            _telemetrySectorKeyLabel   = Root.Q<Label>("lbl-telemetry-sector-key");
            _telemetrySectorValueLabel = Root.Q<Label>("lbl-telemetry-sector-value");
            _telemetryThreatKeyLabel   = Root.Q<Label>("lbl-telemetry-threat-key");
            _telemetryThreatValueLabel = Root.Q<Label>("lbl-telemetry-threat-value");
            _bootTargetKeyLabel        = Root.Q<Label>("lbl-boot-target-key");
            _bootTargetValueLabel      = Root.Q<Label>("lbl-boot-target-value");

            // Buttons
            _newGameButton      = Root.Q<Button>("btn-new-game");
            _loadGameButton     = Root.Q<Button>("btn-load-game");
            _achievementsButton = Root.Q<Button>("btn-achievements");
            _optionsButton      = Root.Q<Button>("btn-options");
            _quitButton         = Root.Q<Button>("btn-quit");

            if (_newGameButton == null)
            {
                Debug.LogError("[TitleScreenController] UXML elements not found — check the UIDocument Source Asset is assigned.", this);
                return;
            }

            _newGameButton.clicked  += OpenGameplayPrefs;
            _loadGameButton.clicked += OpenSavedGames;
            if (_achievementsButton != null) _achievementsButton.clicked += OpenAchievements;
            _optionsButton.clicked  += OpenOptions;
            _quitButton.clicked     += ShowQuitConfirmation;

            // Static labels → Localizer (auto-refreshed by base.OnLocalizationRefresh)
            Localizer.Bind(_subtitleLabel, "menu.subtitle");
            Localizer.Bind(_versionLabel,  "title.versionDraft");

            // Animated logo — letters + particles built at runtime
            _logoAnimator = new TitleLogoAnimator();
            _logoAnimator.Init(Root.Q<VisualElement>("title-logo-area"));

            // Sub-panel controllers
            _modal = new ModalController();
            _modal.Bind(Root.Q<VisualElement>("panel-modal"));

            _langModal = new LanguageModalController();
            _langModal.Bind(Root.Q<VisualElement>("panel-language"));

            _gameplayPrefs = new GameplayPrefsController(_modal);
            _gameplayPrefs.Bind(Root.Q<VisualElement>("panel-gameplay-prefs"));

            _savedGames = new SavedGamesController(_modal);
            _savedGames.Bind(Root.Q<VisualElement>("panel-saved-games"));

            _gameOptions = new GameOptionsController(_modal.Show, OpenLanguageModal);
            _gameOptions.Bind(Root.Q<VisualElement>("panel-options"));

            _achievements = new AchievementsController();
            _achievements.Bind(Root.Q<VisualElement>("panel-achievements"));

            _newGameButton.schedule.Execute(() => _newGameButton.Focus()).StartingIn(150);
        }

        // ── Localization ───────────────────────────────────────────────────────

        public override void OnLocalizationRefresh()
        {
            base.OnLocalizationRefresh(); // Localizer.RefreshAll() → logo, subtitle, version

            // Buttons are TextElement but not Label — set directly
            if (_newGameButton      != null) _newGameButton.text      = GameLocalization.Get("title.newGame").ToUpper();
            if (_loadGameButton     != null) _loadGameButton.text     = GameLocalization.Get("title.loadGame").ToUpper();
            if (_achievementsButton != null) _achievementsButton.text = GameLocalization.GetOptional("title.achievements", "Achievements").ToUpper();
            if (_optionsButton      != null) _optionsButton.text      = GameLocalization.Get("title.options").ToUpper();
            if (_quitButton         != null) _quitButton.text         = GameLocalization.Get("title.quitGame").ToUpper();

            SetTelemetryLabels();

            // Propagate to sub-panels (keeps hidden panels in sync for instant display)
            if (_modal         != null) _modal.OnLocalizationRefresh();
            if (_langModal     != null) _langModal.OnLocalizationRefresh();
            if (_gameplayPrefs != null) _gameplayPrefs.OnLocalizationRefresh();
            if (_savedGames    != null) _savedGames.OnLocalizationRefresh();
            if (_gameOptions   != null) _gameOptions.OnLocalizationRefresh();
            if (_achievements  != null) _achievements.OnLocalizationRefresh();
        }

        // ── Button handlers ────────────────────────────────────────────────────

        private void OpenLanguageModal()   => _langModal?.Show();

        private void OpenAchievements()    => _achievements?.Show();

        private void OpenGameplayPrefs()
        {
            if (_gameplayPrefs != null) _gameplayPrefs.Show();
        }

        private void OpenSavedGames()
        {
            if (_savedGames != null) _savedGames.Show();
        }

        private void OpenOptions()
        {
            if (_gameOptions != null) _gameOptions.Show();
        }

        private void ShowQuitConfirmation()
        {
            if (_modal == null) return;
            _modal.Show(
                GameLocalization.Get("title.quitConfirmTitle"),
                GameLocalization.Get("title.quitConfirmBody"),
                Application.Quit);
        }

        private void SetTelemetryLabels()
        {
            SetLabel(_titleKickerLabel,          "title.kicker",          "COLONY COMMAND INTERFACE");
            SetLabel(_telemetryTitleLabel,       "title.telemetry",       "BUNKER TELEMETRY");
            SetLabel(_telemetryCoreKeyLabel,     "title.telemetry.core",  "CORE");
            SetLabel(_telemetryCoreValueLabel,   "title.telemetry.stable","STABLE");
            SetLabel(_telemetrySignalKeyLabel,   "title.telemetry.signal","SIGNAL");
            SetLabel(_telemetrySignalValueLabel, "title.telemetry.value", "72%");
            SetLabel(_telemetrySectorKeyLabel,   "title.telemetry.sector","SECTOR");
            SetLabel(_telemetrySectorValueLabel, "title.telemetry.blackout", "BLACKOUT");
            SetLabel(_telemetryThreatKeyLabel,   "title.telemetry.threat","THREAT");
            SetLabel(_telemetryThreatValueLabel, "title.telemetry.rising","RISING");
            SetLabel(_bootTargetKeyLabel,        "title.bootTarget",      "BOOT TARGET");
            SetLabel(_bootTargetValueLabel,      "title.bootPlayer",      "PLAYER");
        }

        private static void SetLabel(Label label, string key, string fallback)
        {
            if (label != null)
                label.text = GameLocalization.GetOptional(key, fallback);
        }
    }
}
