using System;
using UnityEngine.UIElements;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Controls the shared confirmation modal panel (#panel-modal) used by all
    /// title-screen sub-panels.  Owned and bound by TitleScreenController.
    /// </summary>
    public sealed class ModalController : UIToolkitComponentController
    {
        private Label  _titleLabel;
        private Label  _bodyLabel;
        private Button _declineButton;
        private Button _acceptButton;

        private Action _pendingAccept;

        // ── Binding ────────────────────────────────────────────────────────────

        protected override void OnBind()
        {
            _titleLabel    = Root.Q<Label> ("lbl-modal-title");
            _bodyLabel     = Root.Q<Label> ("lbl-modal-body");
            _declineButton = Root.Q<Button>("btn-modal-decline");
            _acceptButton  = Root.Q<Button>("btn-modal-accept");

            _declineButton.clicked += Hide;
            _acceptButton.clicked  += Confirm;
        }

        // ── API ────────────────────────────────────────────────────────────────

        public void Show(string title, string body, Action onAccept)
        {
            _titleLabel.text = title;
            _bodyLabel.text  = body;
            _pendingAccept   = onAccept;
            Root.RemoveFromClassList("lk-hidden");
            LKUIInteractionPolisher.PlayPanelOpen();
        }

        public void Hide()
        {
            _pendingAccept = null;
            Root.AddToClassList("lk-hidden");
            LKUIInteractionPolisher.PlayPanelClose();
        }

        // ── Localization ───────────────────────────────────────────────────────

        public override void OnLocalizationRefresh()
        {
            if (_declineButton != null) _declineButton.text = GameLocalization.Get("common.cancelButton");
            if (_acceptButton  != null) _acceptButton.text  = GameLocalization.Get("common.confirmButton");
        }

        // ── Private ────────────────────────────────────────────────────────────

        private void Confirm()
        {
            Action action = _pendingAccept;
            Hide();
            action?.Invoke();
        }
    }
}
