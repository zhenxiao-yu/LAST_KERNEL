using UnityEngine;

namespace Markyu.LastKernel
{
    [RequireComponent(typeof(CanvasGroup))]
    public class PauseMenu : LocalizedUIBehaviour
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
            ApplyMenuVisibility(false, updatePauseState: false);

            continueButton.SetOnClick(ToggleActiveState);
            optionsButton.SetOnClick(OpenOptionsMenu);
            titleButton.SetOnClick(ReturnToTitle);
        }

        private void Update()
        {
            InputManager inputManager = InputManager.Instance;
            if (inputManager == null || !inputManager.WasPausePressedThisFrame())
            {
                return;
            }

            DayCycleManager dayCycleManager = DayCycleManager.Instance;
            if (dayCycleManager != null && dayCycleManager.IsEndingCycle)
            {
                return;
            }

            ToggleActiveState();
        }

        protected override void OnDisable()
        {
            ApplyMenuVisibility(false, updatePauseState: true);
            base.OnDisable();
        }

        private void ToggleActiveState()
        {
            ApplyMenuVisibility(!isActive, updatePauseState: true);
        }

        private void ApplyMenuVisibility(bool visible, bool updatePauseState)
        {
            isActive = visible;
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = visible;

            if (updatePauseState)
            {
                TimeManager.Instance?.SetExternalPause(visible);
            }
        }

        private void OpenOptionsMenu()
        {
            gameOptionsUI.Open();
        }

        private void ReturnToTitle()
        {
            GameDirector.Instance?.BackToTitle();
        }

        protected override void RefreshLocalizedText()
        {
            continueButton.SetText(GameLocalization.Get("pause.resume"));
            optionsButton.SetText(GameLocalization.Get("options.header"));
            titleButton.SetText(GameLocalization.Get("pause.backToTitle"));
        }
    }
}

