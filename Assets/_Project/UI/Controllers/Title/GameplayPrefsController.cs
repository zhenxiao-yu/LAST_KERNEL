using UnityEngine;
using UnityEngine.UIElements;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Controls the Gameplay Preferences sub-panel (#panel-gameplay-prefs).
    /// All settings use cycling buttons for visual consistency.
    /// On confirm, raises UIEventBus.OnStartNewGame with the selected settings.
    /// </summary>
    public sealed class GameplayPrefsController : UIToolkitComponentController
    {
        private readonly ModalController _modal;

        // ── Bound elements ─────────────────────────────────────────────────────

        private Label  _titleLabel;
        private Label  _subtitleLabel;
        private Button _difficultyButton;
        private Label  _durationLabel;
        private Slider _durationSlider;
        private Button _startResourcesButton;
        private Button _friendlyButton;
        private Button _cancelButton;
        private Button _confirmButton;

        // ── Runtime state ──────────────────────────────────────────────────────

        private DifficultyPreset        _difficulty     = DifficultyPreset.Normal;
        private StartingResourcesPreset _startResources = StartingResourcesPreset.Standard;
        private bool                    _isFriendly     = false;

        public GameplayPrefsController(ModalController modal)
        {
            _modal = modal;
        }

        // ── Binding ────────────────────────────────────────────────────────────

        protected override void OnBind()
        {
            _titleLabel           = Root.Q<Label>  ("lbl-prefs-title");
            _subtitleLabel        = Root.Q<Label>  ("lbl-prefs-subtitle");
            _difficultyButton     = Root.Q<Button> ("btn-difficulty");
            _durationLabel        = Root.Q<Label>  ("lbl-duration");
            _durationSlider       = Root.Q<Slider> ("slider-duration");
            _startResourcesButton = Root.Q<Button> ("btn-start-resources");
            _friendlyButton       = Root.Q<Button> ("btn-friendly-mode");
            _cancelButton         = Root.Q<Button> ("btn-prefs-cancel");
            _confirmButton        = Root.Q<Button> ("btn-prefs-confirm");

            _durationSlider.RegisterValueChangedCallback(
                evt => UpdateDurationLabel(Mathf.RoundToInt(evt.newValue)));

            _difficultyButton.clicked     += CycleDifficulty;
            _startResourcesButton.clicked += CycleStartResources;
            _friendlyButton.clicked       += CycleFriendlyMode;
            _cancelButton.clicked         += Hide;
            _confirmButton.clicked        += StartNewGame;
        }

        // ── API ────────────────────────────────────────────────────────────────

        public void Show()
        {
            bool wasHidden = Root.ClassListContains("lk-hidden");
            Root.RemoveFromClassList("lk-hidden");
            OnLocalizationRefresh();
            if (wasHidden) LKUIInteractionPolisher.PlayPanelOpen();
        }

        public void Hide()
        {
            bool wasVisible = !Root.ClassListContains("lk-hidden");
            Root.AddToClassList("lk-hidden");
            if (wasVisible) LKUIInteractionPolisher.PlayPanelClose();
        }

        // ── Localization ───────────────────────────────────────────────────────

        public override void OnLocalizationRefresh()
        {
            if (_titleLabel    != null) _titleLabel.text    = GameLocalization.Get("title.gameplayHeader");
            if (_subtitleLabel != null) _subtitleLabel.text = GameLocalization.GetOptional("title.gameplaySubtitle", "Configure Before Deployment");
            if (_cancelButton  != null) _cancelButton.text  = GameLocalization.Get("common.cancelButton");
            if (_confirmButton != null) _confirmButton.text = GameLocalization.Get("common.confirmButton");

            UpdateDifficultyButton();
            UpdateStartResourcesButton();
            UpdateFriendlyButton();

            if (_durationSlider != null)
                UpdateDurationLabel(Mathf.RoundToInt(_durationSlider.value));
        }

        // ── Difficulty ─────────────────────────────────────────────────────────

        private void CycleDifficulty()
        {
            _difficulty = (DifficultyPreset)(((int)_difficulty + 1) % 3);
            UpdateDifficultyButton();
        }

        private void UpdateDifficultyButton()
        {
            if (_difficultyButton == null) return;
            string valueKey = _difficulty switch
            {
                DifficultyPreset.Easy => "gameplay.difficulty.easy",
                DifficultyPreset.Hard => "gameplay.difficulty.hard",
                _                     => "gameplay.difficulty.normal",
            };
            _difficultyButton.text =
                GameLocalization.Get("gameplay.difficultyLabel") + ": " +
                GameLocalization.Get(valueKey);
        }

        // ── Starting resources ─────────────────────────────────────────────────

        private void CycleStartResources()
        {
            _startResources = (StartingResourcesPreset)(((int)_startResources + 1) % 3);
            UpdateStartResourcesButton();
        }

        private void UpdateStartResourcesButton()
        {
            if (_startResourcesButton == null) return;
            string valueKey = _startResources switch
            {
                StartingResourcesPreset.Minimal  => "gameplay.startResources.minimal",
                StartingResourcesPreset.Generous => "gameplay.startResources.generous",
                _                                => "gameplay.startResources.standard",
            };
            _startResourcesButton.text =
                GameLocalization.Get("gameplay.startResourcesLabel") + ": " +
                GameLocalization.Get(valueKey);
        }

        // ── Day duration ───────────────────────────────────────────────────────

        private void UpdateDurationLabel(int duration)
        {
            if (_durationLabel != null)
                _durationLabel.text = GameLocalization.Format("gameplay.dayDuration", duration);
        }

        // ── Friendly mode ──────────────────────────────────────────────────────

        private void CycleFriendlyMode()
        {
            _isFriendly = !_isFriendly;
            UpdateFriendlyButton();
        }

        private void UpdateFriendlyButton()
        {
            if (_friendlyButton == null) return;
            string stateKey = _isFriendly ? "gameplay.friendlyMode.on" : "gameplay.friendlyMode.off";
            _friendlyButton.text =
                GameLocalization.Get("gameplay.friendlyModeLabel") + ": " +
                GameLocalization.Get(stateKey);
        }

        // ── Confirm ────────────────────────────────────────────────────────────

        private void StartNewGame()
        {
            var prefs = new GameplayPrefs(
                Mathf.RoundToInt(_durationSlider.value),
                _isFriendly)
            {
                Difficulty     = _difficulty,
                StartResources = _startResources
            };
            UIEventBus.RaiseStartNewGame(prefs);
            Hide();
        }
    }
}
