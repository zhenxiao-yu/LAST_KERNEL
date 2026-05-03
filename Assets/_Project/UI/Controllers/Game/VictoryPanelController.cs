using UnityEngine;
using UnityEngine.UIElements;

namespace Markyu.LastKernel
{
    [UIScreen("Assets/_Project/UI/UXML/Game/VictoryPanelView.uxml", sortingOrder: 90)]
    public sealed class VictoryPanelController : UIToolkitScreenController
    {
        protected override bool AffectedByUIScale => true;

        [SerializeField] private RewardController rewardController;

        private VisualElement _backdrop;
        private Label         _titleLabel;
        private Label         _descLabel;
        private Label         _scrapLabel;
        private Button        _continueButton;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        protected override void OnEnable()
        {
            base.OnEnable();
            if (DefensePhaseController.Instance != null)
                DefensePhaseController.Instance.OnPhaseChanged += HandlePhaseChanged;
            if (rewardController != null)
                rewardController.OnRewardsReady += ShowRewards;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (DefensePhaseController.Instance != null)
                DefensePhaseController.Instance.OnPhaseChanged -= HandlePhaseChanged;
            if (rewardController != null)
                rewardController.OnRewardsReady -= ShowRewards;
        }

        // ── Binding ────────────────────────────────────────────────────────────

        protected override void OnBind()
        {
            _backdrop       = Root.Q<VisualElement>("victory-backdrop");
            _titleLabel     = Root.Q<Label>        ("lbl-victory-title");
            _descLabel      = Root.Q<Label>        ("lbl-victory-desc");
            _scrapLabel     = Root.Q<Label>        ("lbl-victory-scrap");
            _continueButton = Root.Q<Button>       ("btn-victory-continue");

            if (_continueButton != null)
                _continueButton.clicked += OnContinueClicked;

            if (_backdrop != null) _backdrop.EnableInClassList("lk-hidden", true);
        }

        // ── Localization ───────────────────────────────────────────────────────

        public override void OnLocalizationRefresh()
        {
            base.OnLocalizationRefresh();
            if (_continueButton != null)
                _continueButton.text = GameLocalization.Get("victory.continue");
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        private void HandlePhaseChanged(DefensePhase phase)
        {
            if (phase != DefensePhase.Victory)
                HideBackdrop();
        }

        private void ShowRewards(RewardData reward)
        {
            if (_titleLabel != null)
                _titleLabel.text = reward != null && !string.IsNullOrEmpty(reward.RewardTitle)
                    ? reward.RewardTitle
                    : GameLocalization.Get("victory.title");

            if (_descLabel != null)
                _descLabel.text = reward != null ? reward.RewardDescription : string.Empty;

            if (_scrapLabel != null)
            {
                _scrapLabel.text = reward != null
                    ? GameLocalization.Format("victory.scrap", reward.ScrapAmount)
                    : string.Empty;
            }

            if (_backdrop != null)
            {
                bool wasHidden = _backdrop.ClassListContains("lk-hidden");
                _backdrop.RemoveFromClassList("lk-hidden");
                if (wasHidden) LKUIInteractionPolisher.PlayPanelOpen();
            }
        }

        private void HideBackdrop()
        {
            if (_backdrop != null)
            {
                bool wasVisible = !_backdrop.ClassListContains("lk-hidden");
                _backdrop.AddToClassList("lk-hidden");
                if (wasVisible) LKUIInteractionPolisher.PlayPanelClose();
            }
        }

        private void OnContinueClicked()
        {
            Time.timeScale = 1f;
            Hide();
            UIEventBus.RaiseContinueToDayRequested();
        }
    }
}
