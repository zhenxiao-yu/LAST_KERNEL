using System.Text;
using UnityEngine;
using TMPro;

namespace Markyu.FortStack
{
    public class SavedGameSlot : MonoBehaviour
    {
        [SerializeField, Tooltip("UI label displaying the slot number, scene name, progress, and last saved time.")]
        private TextMeshProUGUI labelText;

        [SerializeField, Tooltip("Button used to load the saved game stored in this slot.")]
        private TextButton loadButton;

        [SerializeField, Tooltip("Button used to delete this saved game after user confirmation.")]
        private TextButton deleteButton;

        private GameData data;

        public void Initialize(GameData data, ModalWindow modalWindow, SavedGamesUI parentUI)
        {
            this.data = data;
            GameLocalization.LanguageChanged += HandleLanguageChanged;
            RefreshLocalizedText();

            loadButton.SetOnClick(() =>
            {
                GameDirector.Instance.LoadGame(data);
                parentUI.Close();
            });

            deleteButton.SetOnClick(() =>
                modalWindow.Show(
                    GameLocalization.Get("save.deleteTitle"),
                    GameLocalization.Format("save.deleteBody", data.SlotNumber),
                    DeleteSavedGame
                )
            );
        }

        public void DeleteSavedGame()
        {
            GameDirector.Instance?.DeleteGame(data);
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            GameLocalization.LanguageChanged -= HandleLanguageChanged;
        }

        private void HandleLanguageChanged(GameLanguage _)
        {
            RefreshLocalizedText();
        }

        private void RefreshLocalizedText()
        {
            if (data == null)
                return;

            string progressSuffix = string.Empty;
            if (data.TryGetScene(out var sceneData))
            {
                progressSuffix = GameLocalization.Format("save.progressSuffix", sceneData.QuestProgress);
            }

            var sb = new StringBuilder();
            sb.Append(GameLocalization.Format("save.slotLabel", data.SlotNumber, data.CurrentScene, progressSuffix));
            sb.Append(GameLocalization.Format(
                "save.lastSaved",
                data.LastSaved.ToString("g", GameLocalization.CurrentCulture)));

            labelText.text = sb.ToString();
            loadButton.SetText(GameLocalization.Get("common.loadButton"));
            deleteButton.SetText(GameLocalization.Get("common.deleteButton"));
        }
    }
}

