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

            UpdateDurationLabel((int)durationSlider.value);
            UpdateFriendlyLabel(isFriendlyToggle.isOn);
        }

        private void UpdateDurationLabel(int duration)
        {
            durationLabel.text = $"白昼时长：{duration} 秒";
        }

        private void UpdateFriendlyLabel(bool isFriendly)
        {
            string state = isFriendly ? "开启" : "关闭";
            string message = isFriendly ? "（敌对目标不会出现）" : "（敌对目标可能出现）";
            isFriendlyLabel.text = $"友好模式：{state}\n<size=23>{message}";
        }

        public void Open() => gameObject.SetActive(true);
        private void Close() => gameObject.SetActive(false);

        private void StartNewGame()
        {
            int dayDuration = (int)durationSlider.value;
            bool isFriendlyMode = isFriendlyToggle.isOn;
            var prefs = new GameplayPrefs(dayDuration, isFriendlyMode);
            GameDirector.Instance.NewGame(prefs);
            Close();
        }
    }
}

