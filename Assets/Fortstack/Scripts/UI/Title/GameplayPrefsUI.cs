using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Markyu.FortStack
{
    public class GameplayPrefsUI : MonoBehaviour
    {
        [SerializeField, Tooltip("The TextMeshProUGUI component displaying the current value of the Day Duration slider.")]
        private TextMeshProUGUI durationLabel;

        [SerializeField, Tooltip("The Slider component used to set the duration (in seconds) of a game day.")]
        private Slider durationSlider;

        [SerializeField, Tooltip("The TextMeshProUGUI component displaying the current state and description of the Friendly Mode toggle.")]
        private TextMeshProUGUI isFriendlyLabel;

        [SerializeField, Tooltip("The Toggle component used to enable or disable 'Friendly Mode' (enemy presence).")]
        private Toggle isFriendlyToggle;

        [SerializeField, Tooltip("The button used to close the UI panel without starting a new game.")]
        private TextButton cancelButton;

        [SerializeField, Tooltip("The button used to confirm the settings and start a new game.")]
        private TextButton confirmButton;

        private void Awake()
        {
            durationSlider.onValueChanged.AddListener(value =>
            {
                UpdateDurationLabel((int)value);
            });

            isFriendlyToggle.onValueChanged.AddListener(isOn =>
            {
                UpdateFriendlyLabel(isOn);
                AudioManager.Instance?.PlaySFX(AudioId.Click);
            });

            cancelButton.SetOnClick(Close);
            confirmButton.SetOnClick(StartNewGame);

            GameLocalization.LanguageChanged += HandleLanguageChanged;
            RefreshLocalizedText();
        }

        private void UpdateDurationLabel(int duration)
        {
            durationLabel.text = GameLocalization.Format("gameplay.dayDuration", duration);
        }

        private void UpdateFriendlyLabel(bool isFriendly)
        {
            isFriendlyLabel.text = isFriendly
                ? GameLocalization.Get("gameplay.friendlyOn")
                : GameLocalization.Get("gameplay.friendlyOff");
        }

        public void Open() => gameObject.SetActive(true);
        private void Close() => gameObject.SetActive(false);

        private void OnDestroy()
        {
            GameLocalization.LanguageChanged -= HandleLanguageChanged;
        }

        private void StartNewGame()
        {
            int dayDuration = (int)durationSlider.value;
            bool isFriendlyMode = isFriendlyToggle.isOn;
            var prefs = new GameplayPrefs(dayDuration, isFriendlyMode);
            GameDirector.Instance.NewGame(prefs);
            Close();
        }

        private void HandleLanguageChanged(GameLanguage _)
        {
            RefreshLocalizedText();
        }

        private void RefreshLocalizedText()
        {
            cancelButton.SetText(GameLocalization.Get("common.cancelButton"));
            confirmButton.SetText(GameLocalization.Get("common.confirmButton"));
            UpdateDurationLabel((int)durationSlider.value);
            UpdateFriendlyLabel(isFriendlyToggle.isOn);
        }
    }
}

