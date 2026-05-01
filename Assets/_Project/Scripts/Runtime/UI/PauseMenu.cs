using Sirenix.OdinInspector;
using UnityEngine;

namespace Markyu.LastKernel
{
    [RequireComponent(typeof(CanvasGroup))]
    public class PauseMenu : LocalizedUIBehaviour
    {
        public static PauseMenu Instance { get; private set; }

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
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            canvasGroup = GetComponent<CanvasGroup>();
            ApplyMenuVisibility(false, updatePauseState: false);

            continueButton.SetOnClick(ToggleActiveState);
            optionsButton.SetOnClick(OpenOptionsMenu);
            titleButton.SetOnClick(ReturnToTitle);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        protected override void OnDisable()
        {
            ApplyMenuVisibility(false, updatePauseState: true);
            base.OnDisable();
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

        private void ToggleActiveState() => Toggle();

        /// <summary>Toggles the pause menu. Called by keyboard shortcut (SPACE) via GameInputHandler.</summary>
        public void Toggle()
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

