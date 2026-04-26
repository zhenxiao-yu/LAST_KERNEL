using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

namespace Markyu.LastKernel
{
    public class GameOptionsUI : LocalizedUIBehaviour
    {
        private TextButton languageButton;
        private UnityAction<float> sfxSliderChangedHandler;
        private UnityAction<float> bgmSliderChangedHandler;

        [Header("Graphics")]
        [SerializeField, Tooltip("The button used to cycle through available screen resolutions.")]
        private TextButton resolutionButton;

        [SerializeField, Tooltip("The button used to cycle through fullscreen modes (e.g., Windowed, Fullscreen).")]
        private TextButton fullscreenButton;

        [SerializeField, Tooltip("The button used to toggle Vertical Sync (V-Sync) ON or OFF.")]
        private TextButton vSyncButton;

        [SerializeField, Tooltip("The button used to cycle through the maximum frame rate cap (FPS limit).")]
        private TextButton fpsButton;

        [SerializeField, Tooltip("The button used to cycle through different Shadow quality presets.")]
        private TextButton shadowButton;

        [Header("Audio")]
        [SerializeField, Tooltip("The TextMeshPro label displaying the current volume percentage for Sound Effects (SFX).")]
        private TextMeshProUGUI labelSFX;

        [SerializeField, Tooltip("The Slider component used to adjust the volume level of Sound Effects (SFX).")]
        private Slider sliderSFX;

        [SerializeField, Tooltip("The TextMeshPro label displaying the current volume percentage for Background Music (BGM).")]
        private TextMeshProUGUI labelBGM;

        [SerializeField, Tooltip("The Slider component used to adjust the volume level of Background Music (BGM).")]
        private Slider sliderBGM;

        [Header("Footer")]
        [SerializeField, Tooltip("The button that opens a confirmation modal to reset all game settings to default.")]
        private TextButton resetButton;

        [SerializeField, Tooltip("The button used to save and close the Game Options UI panel.")]
        private TextButton closeButton;

        [Header("Screens")]
        [SerializeField, Tooltip("Reference to the generic Modal Window used for confirmation prompts, such as resetting settings.")]
        private ModalWindow modalWindow;

        private void Awake()
        {
            EnsureLanguageButton();
            BindButtonEvents();
            BindSliderEvents();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            RefreshStateFromManagers();
        }

        private void OnDestroy()
        {
            if (sliderSFX != null && sfxSliderChangedHandler != null)
            {
                sliderSFX.onValueChanged.RemoveListener(sfxSliderChangedHandler);
            }

            if (sliderBGM != null && bgmSliderChangedHandler != null)
            {
                sliderBGM.onValueChanged.RemoveListener(bgmSliderChangedHandler);
            }
        }

        public void Open()
        {
            RefreshStateFromManagers();

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
            else
            {
                RefreshLocalizedText();
            }
        }

        public void Close()
        {
            PlayerPrefs.Save();
            gameObject.SetActive(false);
        }

        private void BindButtonEvents()
        {
            resolutionButton.SetOnClick(HandleResolutionButtonClicked);
            fullscreenButton.SetOnClick(HandleFullscreenButtonClicked);
            vSyncButton.SetOnClick(HandleVSyncButtonClicked);
            fpsButton.SetOnClick(HandleFpsButtonClicked);
            shadowButton.SetOnClick(HandleShadowButtonClicked);
            resetButton.SetOnClick(ShowResetConfirmation);
            closeButton.SetOnClick(Close);
            languageButton?.SetOnClick(GameLocalization.CycleLanguage);
        }

        private void BindSliderEvents()
        {
            sfxSliderChangedHandler = HandleSfxSliderChanged;
            bgmSliderChangedHandler = HandleBgmSliderChanged;

            sliderSFX.onValueChanged.AddListener(sfxSliderChangedHandler);
            sliderBGM.onValueChanged.AddListener(bgmSliderChangedHandler);
        }

        private void RefreshStateFromManagers()
        {
            RefreshGraphicsButtonLabels();
            RefreshVolumeSliders();
        }

        private void RefreshGraphicsButtonLabels()
        {
            GraphicsManager graphicsManager = GraphicsManager.Instance;
            if (graphicsManager == null)
            {
                return;
            }

            resolutionButton.SetText(graphicsManager.GetResolutionLabel());
            fullscreenButton.SetText(graphicsManager.GetFullscreenLabel());
            vSyncButton.SetText(graphicsManager.FormatVSyncLabel());
            fpsButton.SetText(graphicsManager.FormatFpsLabel());
            shadowButton.SetText(graphicsManager.FormatShadowLabel());
        }

        private void RefreshVolumeSliders()
        {
            AudioManager audioManager = AudioManager.Instance;
            if (audioManager == null)
            {
                return;
            }

            sliderSFX.SetValueWithoutNotify(audioManager.GetSavedSFXVolumeSlider());
            sliderBGM.SetValueWithoutNotify(audioManager.GetSavedBGMVolumeSlider());
            UpdateSfxLabel(sliderSFX.value);
            UpdateBgmLabel(sliderBGM.value);
        }

        private void HandleResolutionButtonClicked()
        {
            GraphicsManager.Instance?.CycleScreenResolution();
            RefreshGraphicsButtonLabels();
        }

        private void HandleFullscreenButtonClicked()
        {
            GraphicsManager.Instance?.CycleFullscreenMode();
            RefreshGraphicsButtonLabels();
        }

        private void HandleVSyncButtonClicked()
        {
            GraphicsManager.Instance?.CycleVSync();
            RefreshGraphicsButtonLabels();
        }

        private void HandleFpsButtonClicked()
        {
            GraphicsManager.Instance?.CycleFrameRateCap();
            RefreshGraphicsButtonLabels();
        }

        private void HandleShadowButtonClicked()
        {
            GraphicsManager.Instance?.CycleShadowPreset();
            RefreshGraphicsButtonLabels();
        }

        private void HandleSfxSliderChanged(float value)
        {
            AudioManager.Instance?.SetSFXVolume(value);
            UpdateSfxLabel(value);
        }

        private void HandleBgmSliderChanged(float value)
        {
            AudioManager.Instance?.SetBGMVolume(value);
            UpdateBgmLabel(value);
        }

        private void ShowResetConfirmation()
        {
            modalWindow.Show(
                GameLocalization.Get("options.resetTitle"),
                GameLocalization.Get("options.resetBody"),
                ResetAllSettings
            );
        }

        private void ResetAllSettings()
        {
            string currentLocaleCode = UnityLocalizationBridge.CurrentLocaleCode;

            // Resetting PlayerPrefs would also wipe the locale choice, so restore it immediately
            // to keep the current UI language stable after the reset.
            PlayerPrefs.DeleteAll();
            GameLocalization.SetLanguageByCode(currentLocaleCode, force: true);

            GraphicsManager.Instance?.InitGraphicsSettings();
            AudioManager.Instance?.InitAudioMixerVolumes();
            RefreshStateFromManagers();
            RefreshLocalizedText();
        }

        private void EnsureLanguageButton()
        {
            if (languageButton != null)
                return;

            TextButton template = shadowButton != null ? shadowButton : closeButton;
            if (template == null)
                return;

            GameObject buttonObject = Instantiate(template.gameObject, template.transform.parent);
            buttonObject.name = "LanguageButton";
            buttonObject.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + 1);
            languageButton = buttonObject.GetComponent<TextButton>();
        }

        private void UpdateSfxLabel(float value)
        {
            labelSFX.text = GameLocalization.Format("options.sfx", Mathf.RoundToInt(value * 100f));
        }

        private void UpdateBgmLabel(float value)
        {
            labelBGM.text = GameLocalization.Format("options.bgm", Mathf.RoundToInt(value * 100f));
        }

        protected override void RefreshLocalizedText()
        {
            RefreshGraphicsButtonLabels();
            UpdateSfxLabel(sliderSFX.value);
            UpdateBgmLabel(sliderBGM.value);

            resetButton.SetText(GameLocalization.Get("common.resetButton"));
            closeButton.SetText(GameLocalization.Get("common.closeButton"));

            if (languageButton != null)
            {
                languageButton.SetText(GameLocalization.GetLanguageButtonLabel());
                languageButton.SetOnClick(GameLocalization.CycleLanguage);
            }
        }
    }
}

