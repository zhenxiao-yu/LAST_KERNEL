using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

namespace Markyu.LastKernel
{
    public class GameplayPrefsUI : LocalizedUIBehaviour
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

        private UnityAction<float> durationChangedHandler;
        private UnityAction<bool> friendlyChangedHandler;

        private void Awake()
        {
            durationChangedHandler = HandleDurationSliderChanged;
            friendlyChangedHandler = HandleFriendlyToggleChanged;

            durationSlider.onValueChanged.AddListener(durationChangedHandler);
            isFriendlyToggle.onValueChanged.AddListener(friendlyChangedHandler);
            cancelButton.SetOnClick(Close);
            confirmButton.SetOnClick(StartNewGame);
        }

        private void OnDestroy()
        {
            if (durationSlider != null && durationChangedHandler != null)
            {
                durationSlider.onValueChanged.RemoveListener(durationChangedHandler);
            }

            if (isFriendlyToggle != null && friendlyChangedHandler != null)
            {
                isFriendlyToggle.onValueChanged.RemoveListener(friendlyChangedHandler);
            }
        }

        public void Open()
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
            else
            {
                RefreshLocalizedText();
            }
        }

        private void Close()
        {
            gameObject.SetActive(false);
        }

        private void HandleDurationSliderChanged(float value)
        {
            UpdateDurationLabel(Mathf.RoundToInt(value));
        }

        private void HandleFriendlyToggleChanged(bool isOn)
        {
            UpdateFriendlyLabel(isOn);
            AudioManager.Instance?.PlaySFX(AudioId.Click);
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

        private void StartNewGame()
        {
            GameDirector gameDirector = GameDirector.Instance;
            if (gameDirector == null)
            {
                return;
            }

            int dayDuration = Mathf.RoundToInt(durationSlider.value);
            bool isFriendlyMode = isFriendlyToggle.isOn;
            var prefs = new GameplayPrefs(dayDuration, isFriendlyMode);
            gameDirector.NewGame(prefs);
            Close();
        }

        protected override void RefreshLocalizedText()
        {
            cancelButton.SetText(GameLocalization.Get("common.cancelButton"));
            confirmButton.SetText(GameLocalization.Get("common.confirmButton"));
            UpdateDurationLabel((int)durationSlider.value);
            UpdateFriendlyLabel(isFriendlyToggle.isOn);
        }
    }
}

