using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Markyu.LastKernel
{
    public class SavedGamesUI : LocalizedUIBehaviour
    {
        [SerializeField, Tooltip("Prefab used to create a saved-game slot entry in the list.")]
        private SavedGameSlot slotPrefab;

        [SerializeField, Tooltip("Parent container that holds all generated saved-game slot entries.")]
        private RectTransform contentRect;

        [SerializeField, Tooltip("Button that deletes all saved games after user confirmation.")]
        private TextButton clearButton;

        [SerializeField, Tooltip("Button that closes the Saved Games UI panel.")]
        private TextButton closeButton;

        [SerializeField, Tooltip("Modal confirmation window used for delete actions.")]
        private ModalWindow modalWindow;

        private readonly List<SavedGameSlot> slots = new();

        private void Awake()
        {
            clearButton.SetOnClick(ShowClearConfirmation);
            closeButton.SetOnClick(Close);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            RebuildSlots();
        }

        public void Open()
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
            else
            {
                RebuildSlots();
                RefreshLocalizedText();
            }
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        internal void UnregisterSlot(SavedGameSlot slot)
        {
            slots.Remove(slot);
        }

        private void ShowClearConfirmation()
        {
            modalWindow.Show(
                GameLocalization.Get("save.clearTitle"),
                GameLocalization.Get("save.clearBody"),
                ClearSavedGames
            );
        }

        private void RebuildSlots()
        {
            ClearSlotObjects();

            if (slotPrefab == null || contentRect == null)
            {
                return;
            }

            GameDirector gameDirector = GameDirector.Instance;
            if (gameDirector == null)
            {
                return;
            }

            // Rebuild the slot list on open so the screen always reflects the current save state,
            // even if saves were created or deleted while this panel was hidden.
            foreach (GameData savedGame in gameDirector.SavedGames.Values.OrderBy(data => data.SlotNumber))
            {
                SavedGameSlot slot = Instantiate(slotPrefab, contentRect);
                slot.Initialize(savedGame, modalWindow, this);
                slots.Add(slot);
            }
        }

        private void ClearSlotObjects()
        {
            foreach (SavedGameSlot slot in slots)
            {
                if (slot != null)
                {
                    Destroy(slot.gameObject);
                }
            }

            slots.Clear();
        }

        private void ClearSavedGames()
        {
            List<SavedGameSlot> slotsToDelete = new(slots);

            foreach (SavedGameSlot slot in slotsToDelete)
            {
                slot?.DeleteSavedGame();
            }

            slots.Clear();
        }

        protected override void RefreshLocalizedText()
        {
            clearButton.SetText(GameLocalization.Get("title.clearSaves"));
            closeButton.SetText(GameLocalization.Get("common.closeButton"));
        }
    }
}

