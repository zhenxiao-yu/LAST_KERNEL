using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Markyu.FortStack
{
    public class GameOptionsUI : MonoBehaviour
    {
        private TextButton languageButton;

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

        private void Start()
        {
            EnsureLanguageButton();

            resolutionButton.SetOnClick(() =>
            {
                GraphicsManager.Instance.CycleScreenResolution();
                resolutionButton.SetText(GraphicsManager.Instance.GetResolutionLabel());
            });

            fullscreenButton.SetOnClick(() =>
            {
                GraphicsManager.Instance.CycleFullscreenMode();
                fullscreenButton.SetText(GraphicsManager.Instance.GetFullscreenLabel());
            });

            vSyncButton.SetOnClick(() =>
            {
                GraphicsManager.Instance.CycleVSync();
                vSyncButton.SetText(GraphicsManager.Instance.FormatVSyncLabel());
            });

            fpsButton.SetOnClick(() =>
            {
                GraphicsManager.Instance.CycleFrameRateCap();
                fpsButton.SetText(GraphicsManager.Instance.FormatFpsLabel());
            });

            shadowButton.SetOnClick(() =>
            {
                GraphicsManager.Instance.CycleShadowPreset();
                shadowButton.SetText(GraphicsManager.Instance.FormatShadowLabel());
            });

            sliderSFX.onValueChanged.AddListener(value =>
            {
                AudioManager.Instance?.SetSFXVolume(value);
                UpdateSfxLabel(value);
            });

            sliderBGM.onValueChanged.AddListener(value =>
            {
                AudioManager.Instance?.SetBGMVolume(value);
                UpdateBgmLabel(value);
            });

            InitVolumeSliders();

            resetButton.SetOnClick(() =>
                modalWindow.Show(
                    GameLocalization.Get("options.resetTitle"),
                    GameLocalization.Get("options.resetBody"),
                    ResetAllSettings
                )
            );

            closeButton.SetOnClick(Close);
            languageButton?.SetOnClick(GameLocalization.CycleLanguage);

            GameLocalization.LanguageChanged += HandleLanguageChanged;
            RefreshLocalizedText();
        }

        private void InitButtonLabels()
        {
            resolutionButton.SetText(GraphicsManager.Instance.GetResolutionLabel());
            fullscreenButton.SetText(GraphicsManager.Instance.GetFullscreenLabel());
            vSyncButton.SetText(GraphicsManager.Instance.FormatVSyncLabel());
            fpsButton.SetText(GraphicsManager.Instance.FormatFpsLabel());
            shadowButton.SetText(GraphicsManager.Instance.FormatShadowLabel());
        }

        private void InitVolumeSliders()
        {
            sliderSFX.value = AudioManager.Instance.GetSavedSFXVolumeSlider();
            sliderBGM.value = AudioManager.Instance.GetSavedBGMVolumeSlider();
            UpdateSfxLabel(sliderSFX.value);
            UpdateBgmLabel(sliderBGM.value);
        }

        private void OnDestroy()
        {
            sliderSFX.onValueChanged.RemoveAllListeners();
            sliderBGM.onValueChanged.RemoveAllListeners();
            GameLocalization.LanguageChanged -= HandleLanguageChanged;
        }

        public void Open()
        {
            gameObject.SetActive(true);
        }

        public void Close()
        {
            PlayerPrefs.Save();
            gameObject.SetActive(false);
        }

        private void ResetAllSettings()
        {
            GameLanguage currentLanguage = GameLocalization.CurrentLanguage;

            PlayerPrefs.DeleteAll();
            GameLocalization.SetLanguage(currentLanguage, force: true);

            GraphicsManager.Instance?.InitGraphicsSettings();
            InitButtonLabels();

            AudioManager.Instance?.InitAudioMixerVolumes();
            InitVolumeSliders();
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

        private void HandleLanguageChanged(GameLanguage _)
        {
            RefreshLocalizedText();
        }

        private void RefreshLocalizedText()
        {
            InitButtonLabels();
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

