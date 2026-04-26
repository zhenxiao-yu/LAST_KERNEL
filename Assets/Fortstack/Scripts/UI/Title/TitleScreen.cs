using UnityEngine;

namespace Markyu.LastKernel
{
    public class TitleScreen : LocalizedUIBehaviour
    {
        [Header("Menu Buttons")]
        [SerializeField, Tooltip("The button that opens the Gameplay Preferences UI to start a new game.")]
        private TextButton newGameButton;

        [SerializeField, Tooltip("The button that opens the Saved Games UI to load a previous session.")]
        private TextButton loadGameButton;

        [SerializeField, Tooltip("The button that opens the Game Options UI for general settings.")]
        private TextButton gameOptionsButton;

        [SerializeField, Tooltip("The button that triggers a confirmation modal to quit the application.")]
        private TextButton quitGameButton;

        [Header("Menu Screens")]
        [SerializeField, Tooltip("Reference to the UI panel used for configuring Day Duration and Friendly Mode before starting a new game.")]
        private GameplayPrefsUI gameplayPrefsUI;

        [SerializeField, Tooltip("Reference to the UI panel used for displaying and selecting saved game files.")]
        private SavedGamesUI savedGamesUI;

        [SerializeField, Tooltip("Reference to the UI panel for general game settings (e.g., volume, display).")]
        private GameOptionsUI gameOptionsUI;

        [SerializeField, Tooltip("Reference to the generic Modal Window used for confirmation prompts, like quitting the game.")]
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

