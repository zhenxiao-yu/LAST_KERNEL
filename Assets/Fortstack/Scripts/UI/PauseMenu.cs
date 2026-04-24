using UnityEngine;

namespace Markyu.FortStack
{
    [RequireComponent(typeof(CanvasGroup))]
    public class PauseMenu : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField, Tooltip("The button used to close the pause menu and resume the game.")]
        private TextButton continueButton;

        [SerializeField, Tooltip("The button used to open the Game Options menu.")]
        private TextButton optionsButton;

        [SerializeField, Tooltip("The button used to return to the main Title Screen.")]
        private TextButton titleButton;

        [Header("Screens")]
        [SerializeField, Tooltip("Reference to the Game Options UI panel that is opened when the 'Options' button is clicked.")]
        private GameOptionsUI gameOptionsUI;

        private CanvasGroup canvasGroup;
        private bool isActive = false;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;

            continueButton.SetOnClick(ToggleActiveState);
            optionsButton.SetOnClick(gameOptionsUI.Open);
            titleButton.SetOnClick(GameDirector.Instance.BackToTitle);

            GameLocalization.LanguageChanged += HandleLanguageChanged;
            RefreshLocalizedText();
        }

        private void Update()
        {
            var input = InputManager.Instance;
            if (input != null && input.WasPausePressedThisFrame() && !DayCycleManager.Instance.IsEndingCycle)
            {
                ToggleActiveState();
            }
        }

        private void ToggleActiveState()
        {
            isActive = !isActive;
            canvasGroup.alpha = isActive ? 1f : 0f;
            canvasGroup.blocksRaycasts = isActive;

            TimeManager.Instance.SetExternalPause(isActive);
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
            continueButton.SetText(GameLocalization.Get("pause.resume"));
            optionsButton.SetText(GameLocalization.Get("options.header"));
            titleButton.SetText(GameLocalization.Get("pause.backToTitle"));
        }
    }
}

