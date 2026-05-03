using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Markyu.LastKernel
{
    [UIScreen("Assets/_Project/UI/UXML/Game/PauseMenuView.uxml", sortingOrder: 100)]
    public sealed class PauseMenuController : UIToolkitScreenController
    {
        // ── Elements ───────────────────────────────────────────────────────────

        private VisualElement _backdrop;
        private Label         _titleLabel;
        private Button        _resumeButton;
        private Button        _saveButton;
        private Button        _optionsButton;
        private Button        _backToTitleButton;

        // ── Sub-controllers ────────────────────────────────────────────────────

        private PauseSavePanelController  _savePanel;
        private GameOptionsController     _optionsPanel;
        private LanguageModalController   _langModal;

        // ── Confirm modal ──────────────────────────────────────────────────────

        private VisualElement _confirmPanel;
        private Label         _confirmTitle;
        private Label         _confirmBody;
        private Button        _confirmCancel;
        private Button        _confirmAccept;
        private Action        _pendingConfirm;

        // ── State ──────────────────────────────────────────────────────────────

        private bool _isPaused;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        protected override void OnEnable()
        {
            base.OnEnable();
            UIEventBus.OnResumeRequested += HandleResume;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            UIEventBus.OnResumeRequested -= HandleResume;

            if (_isPaused)
            {
                _isPaused = false;
                if (InputManager.Instance != null) InputManager.Instance.RemoveLock(this);
                if (TimeManager.Instance != null) TimeManager.Instance.SetExternalPause(false);
            }
        }

        private void Update()
        {
            if (InputManager.Instance == null) return;
            if (!InputManager.Instance.WasPausePressedThisFrame()) return;
            if (DayCycleManager.Instance != null && DayCycleManager.Instance.IsEndingCycle) return;

            if (_isPaused) UIEventBus.RaiseResumeRequested();
            else           SetPaused(true);
        }

        // ── Binding ────────────────────────────────────────────────────────────

        protected override void OnBind()
        {
            _backdrop          = Root.Q<VisualElement>("pause-backdrop");
            _titleLabel        = Root.Q<Label>        ("lbl-pause-title");
            _resumeButton      = Root.Q<Button>       ("btn-resume");
            _saveButton        = Root.Q<Button>       ("btn-save");
            _optionsButton     = Root.Q<Button>       ("btn-options");
            _backToTitleButton = Root.Q<Button>       ("btn-back-to-title");

            _confirmPanel  = Root.Q<VisualElement>("panel-confirm");
            _confirmTitle  = Root.Q<Label>        ("lbl-confirm-title");
            _confirmBody   = Root.Q<Label>        ("lbl-confirm-body");
            _confirmCancel = Root.Q<Button>       ("btn-confirm-cancel");
            _confirmAccept = Root.Q<Button>       ("btn-confirm-accept");

            if (_resumeButton      != null) _resumeButton.clicked      += UIEventBus.RaiseResumeRequested;
            if (_saveButton        != null) _saveButton.clicked        += OpenSavePanel;
            if (_optionsButton     != null) _optionsButton.clicked     += OpenOptionsPanel;
            if (_backToTitleButton != null) _backToTitleButton.clicked += OnBackToTitleClicked;
            if (_confirmCancel     != null) _confirmCancel.clicked     += CloseConfirm;
            if (_confirmAccept     != null) _confirmAccept.clicked     += AcceptConfirm;

            VisualElement savePanelRoot = Root.Q<VisualElement>("panel-save-slots");
            if (savePanelRoot != null)
            {
                _savePanel = new PauseSavePanelController(ShowConfirm);
                _savePanel.Bind(savePanelRoot);
            }

            VisualElement langRoot = Root.Q<VisualElement>("panel-language");
            if (langRoot != null)
            {
                _langModal = new LanguageModalController();
                _langModal.Bind(langRoot);
            }

            VisualElement optionsRoot = Root.Q<VisualElement>("panel-options");
            if (optionsRoot != null)
            {
                _optionsPanel = new GameOptionsController(ShowConfirm, () => _langModal?.Show());
                _optionsPanel.Bind(optionsRoot);
            }

            if (_backdrop != null) _backdrop.EnableInClassList("lk-hidden", true);
        }

        // ── Localization ───────────────────────────────────────────────────────

        public override void OnLocalizationRefresh()
        {
            base.OnLocalizationRefresh();

            if (_titleLabel        != null) _titleLabel.text        = GameLocalization.Get("pause.header");
            if (_resumeButton      != null) _resumeButton.text      = GameLocalization.Get("pause.resume");
            if (_saveButton        != null) _saveButton.text        = GameLocalization.Get("pause.save");
            if (_optionsButton     != null) _optionsButton.text     = GameLocalization.Get("options.header");
            if (_backToTitleButton != null) _backToTitleButton.text = GameLocalization.Get("pause.backToTitle");
            if (_confirmCancel     != null) _confirmCancel.text     = GameLocalization.Get("common.cancelButton");
            if (_confirmAccept     != null) _confirmAccept.text     = GameLocalization.Get("common.confirmButton");

            if (_savePanel    != null) _savePanel.OnLocalizationRefresh();
            if (_langModal    != null) _langModal.OnLocalizationRefresh();
            if (_optionsPanel != null) _optionsPanel.OnLocalizationRefresh();
        }

        // ── Pause state ────────────────────────────────────────────────────────

        private void HandleResume()
        {
            SetPaused(false);
        }

        private void SetPaused(bool paused)
        {
            bool wasPaused = _isPaused;
            _isPaused = paused;
            if (_backdrop != null) _backdrop.EnableInClassList("lk-hidden", !paused);
            if (TimeManager.Instance != null) TimeManager.Instance.SetExternalPause(paused);

            if (paused)
            {
                if (InputManager.Instance != null) InputManager.Instance.AddLock(this);
                if (!wasPaused) LKUIInteractionPolisher.PlayPanelOpen();
                _resumeButton?.schedule.Execute(() => _resumeButton.Focus()).StartingIn(50);
            }
            else
            {
                bool nestedPanelVisible = IsAnySubPanelVisible();
                CloseSubPanels();
                if (InputManager.Instance != null) InputManager.Instance.RemoveLock(this);
                if (wasPaused && !nestedPanelVisible) LKUIInteractionPolisher.PlayPanelClose();
            }
        }

        // ── Sub-panel routing ──────────────────────────────────────────────────

        private void OpenSavePanel()
        {
            if (_savePanel != null) _savePanel.Open();
        }

        private void OpenOptionsPanel()
        {
            if (_optionsPanel != null) _optionsPanel.Show();
        }

        private void CloseSubPanels()
        {
            if (_savePanel    != null) _savePanel.Close();
            if (_optionsPanel != null) _optionsPanel.Hide();
            if (_langModal    != null) _langModal.Hide();
            CloseConfirm();
        }

        private bool IsAnySubPanelVisible()
        {
            return IsVisiblePanel("panel-save-slots")
                || IsVisiblePanel("panel-options")
                || IsVisiblePanel("panel-language")
                || IsVisiblePanel("panel-confirm");
        }

        private bool IsVisiblePanel(string name)
        {
            VisualElement panel = Root?.Q<VisualElement>(name);
            return panel != null && !panel.ClassListContains("lk-hidden");
        }

        private void OnBackToTitleClicked()
        {
            CloseSubPanels();
            UIEventBus.RaiseBackToTitleRequested();
        }

        // ── Confirm modal ──────────────────────────────────────────────────────

        private void ShowConfirm(string title, string body, Action onAccept)
        {
            if (_confirmTitle != null) _confirmTitle.text = title;
            if (_confirmBody  != null) _confirmBody.text  = body;
            _pendingConfirm = onAccept;
            if (_confirmPanel != null)
            {
                bool wasHidden = _confirmPanel.ClassListContains("lk-hidden");
                _confirmPanel.RemoveFromClassList("lk-hidden");
                if (wasHidden) LKUIInteractionPolisher.PlayPanelOpen();
            }
        }

        private void CloseConfirm()
        {
            _pendingConfirm = null;
            if (_confirmPanel != null)
            {
                bool wasVisible = !_confirmPanel.ClassListContains("lk-hidden");
                _confirmPanel.AddToClassList("lk-hidden");
                if (wasVisible) LKUIInteractionPolisher.PlayPanelClose();
            }
        }

        private void AcceptConfirm()
        {
            Action action = _pendingConfirm;
            CloseConfirm();
            if (action != null) action.Invoke();
        }
    }
}
