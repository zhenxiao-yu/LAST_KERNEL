using UnityEngine;
using TMPro;

namespace Markyu.LastKernel
{
    public class ModalWindow : LocalizedUIBehaviour
    {
        [SerializeField, Tooltip("UI text element used to display the modal window title.")]
        private TextMeshProUGUI titleText;

        [SerializeField, Tooltip("UI text element used to display the dialog or confirmation message.")]
        private TextMeshProUGUI dialogText;

        [SerializeField, Tooltip("Button that closes the modal without executing the action.")]
        private TextButton declineButton;

        [SerializeField, Tooltip("Button that confirms the action and triggers the assigned callback.")]
        private TextButton acceptButton;

        private System.Action pendingAcceptAction;

        private void Awake()
        {
            declineButton.SetOnClick(Hide);
            acceptButton.SetOnClick(Confirm);
        }

        public void Show(string title, string dialog, System.Action onAccept)
        {
            titleText.text = title;
            dialogText.text = dialog;
            pendingAcceptAction = onAccept;

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
            else
            {
                RefreshLocalizedText();
            }
        }

        public void Hide()
        {
            pendingAcceptAction = null;
            gameObject.SetActive(false);
        }

        private void Confirm()
        {
            System.Action acceptAction = pendingAcceptAction;
            Hide();
            acceptAction?.Invoke();
        }

        protected override void RefreshLocalizedText()
        {
            acceptButton.SetText(GameLocalization.Get("common.confirmButton"));
            declineButton.SetText(GameLocalization.Get("common.cancelButton"));
        }
    }
}

