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

        private Label _logoLabel;
        private Label _subtitleLabel;
        private Label _versionLabel;

        // ── Nav buttons ────────────────────────────────────────────────────────

        private Button _newGameButton;
        private Button _loadGameButton;
        private Button _optionsButton;
        private Button _languageButton;
        private Button _quitButton;

        // ── Sub-panel controllers ──────────────────────────────────────────────

        private ModalController        _modal;
        private GameplayPrefsController _gameplayPrefs;
        private SavedGamesController   _savedGames;
        private GameOptionsController  _gameOptions;

        // ── OnBind ─────────────────────────────────────────────────────────────

        protected override void OnBind()
        {
            // Labels
            _logoLabel     = Root.Q<Label>("lbl-logo");
            _subtitleLabel = Root.Q<Label>("lbl-subtitle");
            _versionLabel  = Root.Q<Label>("lbl-version");

            // Buttons
            _newGameButton  = Root.Q<Button>("btn-new-game");
            _loadGameButton = Root.Q<Button>("btn-load-game");
            _optionsButton  = Root.Q<Button>("btn-options");
            _languageButton = Root.Q<Button>("btn-language");
            _quitButton     = Root.Q<Button>("btn-quit");

            if (_newGameButton == null)
            {
                Debug.LogError("[TitleScreenController] UXML elements not found — check the UIDocument Source Asset is assigned.", this);
                return;
            }

            _newGameButton.clicked  += OpenGameplayPrefs;
            _loadGameButton.clicked += OpenSavedGames;
            _optionsButton.clicked  += OpenOptions;
            _languageButton.clicked += CycleLanguage;
            _quitButton.clicked     += ShowQuitConfirmation;

            // Static labels → Localizer (auto-refreshed by base.OnLocalizationRefresh)
            Localizer.Bind(_logoLabel,     "menu.title");
            Localizer.Bind(_subtitleLabel, "menu.subtitle");
            Localizer.Bind(_versionLabel,  "title.versionDraft");

            // Sub-panel controllers
            _modal = new ModalController();
            _modal.Bind(Root.Q<VisualElement>("panel-modal"));

            _gameplayPrefs = new GameplayPrefsController(_modal);
            _gameplayPrefs.Bind(Root.Q<VisualElement>("panel-gameplay-prefs"));

            _savedGames = new SavedGamesController(_modal);
            _savedGames.Bind(Root.Q<VisualElement>("panel-saved-games"));

            _gameOptions = new GameOptionsController(_modal.Show);
            _gameOptions.Bind(Root.Q<VisualElement>("panel-options"));
        }

        // ── Localization ───────────────────────────────────────────────────────

        public override void OnLocalizationRefresh()
        {
            base.OnLocalizationRefresh(); // Localizer.RefreshAll() → logo, subtitle, version

            // Buttons are TextElement but not Label — set directly
            if (_newGameButton  != null) _newGameButton.text  = GameLocalization.Get("title.newGame");
            if (_loadGameButton != null) _loadGameButton.text = GameLocalization.Get("title.loadGame");
            if (_optionsButton  != null) _optionsButton.text  = GameLocalization.Get("title.options");
            if (_languageButton != null) _languageButton.text = GameLocalization.GetLanguageButtonLabel();
            if (_quitButton     != null) _quitButton.text     = GameLocalization.Get("title.quitGame");

            // Propagate to sub-panels (keeps hidden panels in sync for instant display)
            if (_modal         != null) _modal.OnLocalizationRefresh();
            if (_gameplayPrefs != null) _gameplayPrefs.OnLocalizationRefresh();
            if (_savedGames    != null) _savedGames.OnLocalizationRefresh();
            if (_gameOptions   != null) _gameOptions.OnLocalizationRefresh();
        }

        // ── Button handlers ────────────────────────────────────────────────────

        private void CycleLanguage()
        {
            GameLocalization.CycleLanguage();
        }

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
    }
}
