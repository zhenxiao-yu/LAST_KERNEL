using Sirenix.OdinInspector;
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

        [BoxGroup("Graphics")]
        [SerializeField, Tooltip("Cycles through available screen resolutions.")]
        private TextButton resolutionButton;

        [BoxGroup("Graphics")]
        [SerializeField, Tooltip("Cycles through fullscreen modes (Windowed, Fullscreen, etc.).")]
        private TextButton fullscreenButton;

        [BoxGroup("Graphics")]
        [SerializeField, Tooltip("Toggles V-Sync ON or OFF.")]
        private TextButton vSyncButton;

        [BoxGroup("Graphics")]
        [SerializeField, Tooltip("Cycles through FPS cap presets.")]
        private TextButton fpsButton;

        [BoxGroup("Graphics")]
        [SerializeField, Tooltip("Cycles through Shadow quality presets.")]
        private TextButton shadowButton;

        [BoxGroup("Audio")]
        [SerializeField, Tooltip("Label displaying the current SFX volume percentage.")]
        private TextMeshProUGUI labelSFX;

        [BoxGroup("Audio")]
        [SerializeField, Tooltip("Slider for SFX volume.")]
        private Slider sliderSFX;

        [BoxGroup("Audio")]
        [SerializeField, Tooltip("Label displaying the current BGM volume percentage.")]
        private TextMeshProUGUI labelBGM;

        [BoxGroup("Audio")]
        [SerializeField, Tooltip("Slider for BGM volume.")]
        private Slider sliderBGM;

        [BoxGroup("Footer")]
        [SerializeField, Tooltip("Opens confirmation modal to reset all settings to default.")]
        private TextButton resetButton;

        [BoxGroup("Footer")]
        [SerializeField, Tooltip("Saves and closes the Game Options UI panel.")]
        private TextButton closeButton;

        [BoxGroup("Screens")]
        [SerializeField, Tooltip("Modal Window used for confirmation prompts.")]
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

