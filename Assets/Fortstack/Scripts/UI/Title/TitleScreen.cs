using UnityEngine;

namespace Markyu.FortStack
{
    public class TitleScreen : MonoBehaviour
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

        private void Start()
        {
            RefreshLocalizedText();
            GameLocalization.LanguageChanged += HandleLanguageChanged;

            newGameButton.SetOnClick(() => gameplayPrefsUI.Open());
            loadGameButton.SetOnClick(() => savedGamesUI.Open());
            gameOptionsButton.SetOnClick(() => gameOptionsUI.Open());
            quitGameButton.SetOnClick(() =>
                modalWindow.Show(
                    GameLocalization.Get("title.quitConfirmTitle"),
                    GameLocalization.Get("title.quitConfirmBody"),
                    Application.Quit
                )
            );
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
            newGameButton.SetText(GameLocalization.Get("title.newGame"));
            loadGameButton.SetText(GameLocalization.Get("title.loadGame"));
            gameOptionsButton.SetText(GameLocalization.Get("title.options"));
            quitGameButton.SetText(GameLocalization.Get("title.quitGame"));
        }
    }
}

