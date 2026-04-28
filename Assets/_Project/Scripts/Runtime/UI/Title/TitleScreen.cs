using Sirenix.OdinInspector;
using UnityEngine;

namespace Markyu.LastKernel
{
    public class TitleScreen : LocalizedUIBehaviour
    {
        [BoxGroup("Menu Buttons")]
        [SerializeField, Tooltip("Opens the Gameplay Preferences UI to start a new game.")]
        private TextButton newGameButton;

        [BoxGroup("Menu Buttons")]
        [SerializeField, Tooltip("Opens the Saved Games UI to load a previous session.")]
        private TextButton loadGameButton;

        [BoxGroup("Menu Buttons")]
        [SerializeField, Tooltip("Opens the Game Options UI for general settings.")]
        private TextButton gameOptionsButton;

        [BoxGroup("Menu Buttons")]
        [SerializeField, Tooltip("Shows a confirmation modal to quit the application.")]
        private TextButton quitGameButton;

        [BoxGroup("Menu Screens")]
        [SerializeField, Tooltip("UI panel for configuring Day Duration and Friendly Mode before a new game.")]
        private GameplayPrefsUI gameplayPrefsUI;

        [BoxGroup("Menu Screens")]
        [SerializeField, Tooltip("UI panel for displaying and selecting saved game files.")]
        private SavedGamesUI savedGamesUI;

        [BoxGroup("Menu Screens")]
        [SerializeField, Tooltip("UI panel for general game settings (volume, display, etc.).")]
        private GameOptionsUI gameOptionsUI;

        [BoxGroup("Menu Screens")]
        [SerializeField, Tooltip("Modal Window for confirmation prompts.")]
        private ModalWindow modalWindow;

        private void Awake()
        {
            newGameButton.SetOnClick(OpenNewGameMenu);
            loadGameButton.SetOnClick(OpenLoadGameMenu);
            gameOptionsButton.SetOnClick(OpenOptionsMenu);
            quitGameButton.SetOnClick(ShowQuitConfirmation);
        }

        private void OpenNewGameMenu()
        {
            gameplayPrefsUI.Open();
        }

        private void OpenLoadGameMenu()
        {
            savedGamesUI.Open();
        }

        private void OpenOptionsMenu()
        {
            gameOptionsUI.Open();
        }

        private void ShowQuitConfirmation()
        {
            modalWindow.Show(
                GameLocalization.Get("title.quitConfirmTitle"),
                GameLocalization.Get("title.quitConfirmBody"),
                Application.Quit
            );
        }

        protected override void RefreshLocalizedText()
        {
            newGameButton.SetText(GameLocalization.Get("title.newGame"));
            loadGameButton.SetText(GameLocalization.Get("title.loadGame"));
            gameOptionsButton.SetText(GameLocalization.Get("title.options"));
            quitGameButton.SetText(GameLocalization.Get("title.quitGame"));
        }
    }
}

