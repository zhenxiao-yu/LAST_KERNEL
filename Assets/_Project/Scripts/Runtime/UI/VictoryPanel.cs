// VictoryPanel — Shown when a wave is successfully cleared.
//
// Displays reward summary and a "Continue to Day" button.
// Subscribes to RewardController.OnRewardsReady for the data to show.

using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Overlay panel that appears on wave victory, shows rewards,
    /// and returns the player to the day phase on confirm.
    /// </summary>
    public class VictoryPanel : MonoBehaviour
    {
        [BoxGroup("UI Elements")]
        [SerializeField] private TextMeshProUGUI titleLabel;

        [BoxGroup("UI Elements")]
        [SerializeField] private TextMeshProUGUI descriptionLabel;

        [BoxGroup("UI Elements")]
        [SerializeField] private TextMeshProUGUI scrapRewardLabel;

        [BoxGroup("UI Elements")]
        [SerializeField] private Button continueButton;

        [BoxGroup("References")]
        [SerializeField] private RewardController rewardController;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            gameObject.SetActive(false); // hidden by default
        }

        private void OnEnable()
        {
            if (DefensePhaseController.Instance != null)
                DefensePhaseController.Instance.OnPhaseChanged += HandlePhaseChanged;

            if (rewardController != null)
                rewardController.OnRewardsReady += ShowRewards;

            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueClicked);
        }

        private void OnDisable()
        {
            if (DefensePhaseController.Instance != null)
                DefensePhaseController.Instance.OnPhaseChanged -= HandlePhaseChanged;

            if (rewardController != null)
                rewardController.OnRewardsReady -= ShowRewards;

            if (continueButton != null)
                continueButton.onClick.RemoveListener(OnContinueClicked);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        private void HandlePhaseChanged(DefensePhase phase)
        {
            // The panel activates when RewardController fires OnRewardsReady,
            // not directly on phase change — so rewards are populated before shown.
            if (phase != DefensePhase.Victory)
                gameObject.SetActive(false);
        }

        private void ShowRewards(RewardData reward)
        {
            gameObject.SetActive(true);

            if (titleLabel != null)
                titleLabel.text = reward != null ? reward.RewardTitle : "Victory!";

            if (descriptionLabel != null)
                descriptionLabel.text = reward != null ? reward.RewardDescription : string.Empty;

            if (scrapRewardLabel != null)
                scrapRewardLabel.text = reward != null ? $"+{reward.ScrapAmount} Scrap" : string.Empty;
        }

        private void OnContinueClicked()
        {
            Time.timeScale = 1f; // reset if speed was boosted
            gameObject.SetActive(false);
            DefensePhaseController.Instance?.ReturnToDay();
        }
    }
}
