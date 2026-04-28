using Sirenix.OdinInspector;
using UnityEngine;

namespace Markyu.LastKernel
{
    [RequireComponent(typeof(CanvasGroup))]
    public class PauseMenu : LocalizedUIBehaviour
    {
        [BoxGroup("Buttons")]
        [SerializeField, Tooltip("Closes the pause menu and resumes the game.")]
        private TextButton continueButton;

        [BoxGroup("Buttons")]
        [SerializeField, Tooltip("Opens the Game Options menu.")]
        private TextButton optionsButton;

        [BoxGroup("Buttons")]
        [SerializeField, Tooltip("Returns to the main Title Screen.")]
        private TextButton titleButton;

        [BoxGroup("Screens")]
        [SerializeField, Tooltip("Game Options UI panel opened when 'Options' is clicked.")]
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

