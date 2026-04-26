using System.Text;
using UnityEngine;
using TMPro;

namespace Markyu.LastKernel
{
    public class SavedGameSlot : LocalizedUIBehaviour
    {
        [SerializeField, Tooltip("UI label displaying the slot number, scene name, progress, and last saved time.")]
        private TextMeshProUGUI labelText;

        [SerializeField, Tooltip("Button used to load the saved game stored in this slot.")]
        private TextButton loadButton;

        [SerializeField, Tooltip("Button used to delete this saved game after user confirmation.")]
        private TextButton deleteButton;

        private GameData data;
        private ModalWindow modalWindow;
        private SavedGamesUI parentUI;

        public void Initialize(GameData data, ModalWindow modalWindow, SavedGamesUI parentUI)
        {
            this.data = data;
            this.modalWindow = modalWindow;
            this.parentUI = parentUI;

            loadButton.SetOnClick(LoadSavedGame);
            deleteButton.SetOnClick(ShowDeleteConfirmation);
            RefreshLocalizedText();
        }

        public void DeleteSavedGame()
        {
            parentUI?.UnregisterSlot(this);
            GameDirector.Instance?.DeleteGame(data);
            Destroy(gameObject);
        }

        private void LoadSavedGame()
        {
            if (data == null || GameDirector.Instance == null)
            {
                return;
            }

            GameDirector.Instance.LoadGame(data);
            parentUI?.Close();
        }

        private void ShowDeleteConfirmation()
        {
            if (data == null)
            {
                return;
            }

            modalWindow.Show(
                GameLocalization.Get("save.deleteTitle"),
                GameLocalization.Format("save.deleteBody", data.SlotNumber),
                DeleteSavedGame
            );
        }

        private string BuildSlotLabel()
        {
            string progressSuffix = string.Empty;
            if (data.TryGetScene(out var sceneData))
            {
                progressSuffix = GameLocalization.Format("save.progressSuffix", sceneData.QuestProgress);
            }

            var builder = new StringBuilder();
            builder.Append(GameLocalization.Format("save.slotLabel", data.SlotNumber, data.CurrentScene, progressSuffix));
            builder.Append(GameLocalization.Format("save.lastSaved", data.LastSaved.ToString("g", GameLocalization.CurrentCulture)));
            return builder.ToString();
        }

        protected override void RefreshLocalizedText()
        {
            loadButton.SetText(GameLocalization.Get("common.loadButton"));
            deleteButton.SetText(GameLocalization.Get("common.deleteButton"));

            if (data == null)
            {
                labelText.text = string.Empty;
                return;
            }

            labelText.text = BuildSlotLabel();
        }
    }
}

