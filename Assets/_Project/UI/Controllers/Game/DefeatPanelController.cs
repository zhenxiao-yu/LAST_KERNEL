using UnityEngine;
using UnityEngine.UIElements;

namespace Markyu.LastKernel
{
    [UIScreen("Assets/_Project/UI/UXML/Game/DefeatPanelView.uxml", sortingOrder: 95)]
    public sealed class DefeatPanelController : UIToolkitScreenController
    {
        protected override bool AffectedByUIScale => true;

        private VisualElement _backdrop;
        private Label         _titleLabel;
        private Label         _messageLabel;
        private Button        _retryButton;
        private Button        _backTitleButton;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        protected override void OnEnable()
        {
            base.OnEnable();
            if (DefensePhaseController.Instance != null)
                DefensePhaseController.Instance.OnPhaseChanged += HandlePhaseChanged;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (DefensePhaseController.Instance != null)
                DefensePhaseController.Instance.OnPhaseChanged -= HandlePhaseChanged;
        }

        // ── Binding ────────────────────────────────────────────────────────────

        protected override void OnBind()
        {
            _backdrop        = Root.Q<VisualElement>("defeat-backdrop");
            _titleLabel      = Root.Q<Label>        ("lbl-defeat-title");
            _messageLabel    = Root.Q<Label>        ("lbl-defeat-message");
            _retryButton     = Root.Q<Button>       ("btn-defeat-retry");
            _backTitleButton = Root.Q<Button>       ("btn-defeat-backtitle");

            if (_retryButton != null)
                _retryButton.clicked += OnRetryClicked;

            if (_backTitleButton != null)
                _backTitleButton.clicked += OnBackTitleClicked;

            if (_backdrop != null)
                _backdrop.EnableInClassList("lk-hidden", true);
        }

        // ── Localization ───────────────────────────────────────────────────────

        public override void OnLocalizationRefresh()
        {
            base.OnLocalizationRefresh();

            if (_retryButton != null)
                _retryButton.text = GameLocalization.Get("defeat.retry");

            if (_backTitleButton != null)
                _backTitleButton.text = GameLocalization.Get("pause.backToTitle");
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        private void HandlePhaseChanged(DefensePhase phase)
        {
            if (phase == DefensePhase.Defeat)
                ShowBackdrop();
            else
                HideBackdrop();
        }

        private void ShowBackdrop()
        {
            Time.timeScale = 1f;

            if (_titleLabel != null)
                _titleLabel.text = GameLocalization.Get("defeat.title");

            if (_messageLabel != null)
                _messageLabel.text = GameLocalization.Get("defeat.message");

            if (_backdrop != null)
                _backdrop.RemoveFromClassList("lk-hidden");
        }

        private void HideBackdrop()
        {
            if (_backdrop != null)
                _backdrop.AddToClassList("lk-hidden");
        }

        private void OnRetryClicked()
        {
            HideBackdrop();
            UIEventBus.RaiseRetryRequested();
        }

        private void OnBackTitleClicked()
        {
            Time.timeScale = 1f;
            HideBackdrop();
            UIEventBus.RaiseBackToTitleRequested();
        }
    }
}
