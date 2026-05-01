using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Markyu.LastKernel
{
    [UIScreen("Assets/_Project/UI/UXML/Game/InfoPanelView.uxml", sortingOrder: 50)]
    public sealed class InfoPanelController : UIToolkitScreenController
    {
        public static InfoPanelController Instance { get; private set; }

        protected override bool AffectedByUIScale => true;

        // ── Private state ──────────────────────────────────────────────────────

        private static int s_requestCounter;

        private sealed class InfoRequest
        {
            public int           RequestId;
            public InfoPriority  Priority;
            public string        Header;
            public string        Body;
            public string        ButtonLabel;
            public Action        ButtonAction;
            public CardInfoData? CardInfo;
        }

        private readonly object _hoverRequester = new object();
        private readonly Dictionary<object, InfoRequest> _activeRequests = new();

        private VisualElement _anchor;
        private Label         _headerLabel;
        private Label         _bodyLabel;
        private Button        _actionButton;
        private Action        _currentButtonAction;

        // Card detail section
        private VisualElement _cardSection;
        private Label         _categoryBadge;
        private VisualElement _hpRow;
        private Label         _hpLabel;
        private VisualElement _hpFill;
        private Label         _statsLabel;
        private VisualElement _resourceRow;
        private Label         _sellLabel;
        private Label         _nutritionInfoLabel;
        private VisualElement _usesRow;
        private Label         _usesLabel;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            GameInputHandler.HasActiveModal = HasActiveModal;
            GameInputHandler.AdvanceModal   = AdvanceCurrentModal;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this)
            {
                Instance = null;
                GameInputHandler.HasActiveModal = null;
                GameInputHandler.AdvanceModal   = null;
                InfoPanel.Unregister();
            }
        }

        // ── Binding ────────────────────────────────────────────────────────────

        protected override void OnBind()
        {
            _anchor       = Root.Q<VisualElement>("info-panel-anchor");
            _headerLabel  = Root.Q<Label>        ("lbl-info-header");
            _bodyLabel    = Root.Q<Label>        ("lbl-info-body");
            _actionButton = Root.Q<Button>       ("btn-info-action");

            if (_actionButton != null)
                _actionButton.RegisterCallback<ClickEvent>(OnActionClicked);

            _cardSection      = Root.Q<VisualElement>("info-card-section");
            _categoryBadge    = Root.Q<Label>        ("lbl-info-category");
            _hpRow            = Root.Q<VisualElement>("info-hp-row");
            _hpLabel          = Root.Q<Label>        ("lbl-info-hp");
            _hpFill           = Root.Q<VisualElement>("fill-info-hp");
            _statsLabel       = Root.Q<Label>        ("lbl-info-stats");
            _resourceRow        = Root.Q<VisualElement>("info-resource-row");
            _sellLabel          = Root.Q<Label>        ("lbl-info-sell");
            _nutritionInfoLabel = Root.Q<Label>        ("lbl-info-nutrition");
            _usesRow            = Root.Q<VisualElement>("info-uses-row");
            _usesLabel          = Root.Q<Label>        ("lbl-info-uses");

            InfoPanel.Register(RequestInfoDisplay, ClearInfoRequest, RegisterHover, UnregisterHover, RegisterCardHover);
            RefreshDisplay();
        }

        // ── Public API (mirrors legacy InfoPanel) ──────────────────────────────

        public void RequestInfoDisplay(
            object requester,
            InfoPriority priority,
            (string header, string body) info,
            string buttonLabel = null,
            Action buttonAction = null)
        {
            if (requester == null) return;

            _activeRequests[requester] = new InfoRequest
            {
                RequestId    = s_requestCounter++,
                Priority     = priority,
                Header       = info.header,
                Body         = info.body,
                ButtonLabel  = buttonLabel,
                ButtonAction = buttonAction,
            };

            RefreshDisplay();
        }

        public void ClearInfoRequest(object requester)
        {
            if (requester == null || !_activeRequests.ContainsKey(requester)) return;
            _activeRequests.Remove(requester);
            RefreshDisplay();
        }

        public void RegisterHover((string header, string body) info)
        {
            RequestInfoDisplay(_hoverRequester, InfoPriority.Hover, info);
        }

        public void UnregisterHover()
        {
            ClearInfoRequest(_hoverRequester);
        }

        public void RegisterCardHover((string header, string body) info, CardInfoData? cardInfo)
        {
            _activeRequests[_hoverRequester] = new InfoRequest
            {
                RequestId    = s_requestCounter++,
                Priority     = InfoPriority.Hover,
                Header       = info.header,
                Body         = info.body,
                CardInfo     = cardInfo,
            };
            RefreshDisplay();
        }

        // ── Display ────────────────────────────────────────────────────────────

        private void RefreshDisplay()
        {
            if (_anchor == null) return;

            if (_activeRequests.Count == 0)
            {
                _anchor.AddToClassList("lk-hidden");
                return;
            }

            InfoRequest top = _activeRequests.Values
                .OrderByDescending(r => r.Priority)
                .ThenByDescending(r => r.RequestId)
                .First();

            bool hasHeader = !string.IsNullOrEmpty(top.Header);
            bool hasButton = !string.IsNullOrEmpty(top.ButtonLabel) && top.ButtonAction != null;

            if (_headerLabel != null)
            {
                _headerLabel.text = hasHeader ? top.Header : string.Empty;
                _headerLabel.EnableInClassList("lk-hidden", !hasHeader);
            }

            if (_bodyLabel != null)
                _bodyLabel.text = top.Body ?? string.Empty;

            if (_actionButton != null)
            {
                _actionButton.text = hasButton ? top.ButtonLabel : string.Empty;
                _actionButton.EnableInClassList("lk-hidden", !hasButton);
                _currentButtonAction = hasButton ? top.ButtonAction : null;
            }

            // Card detail section
            bool hasCard = top.CardInfo.HasValue;
            _cardSection?.EnableInClassList("lk-hidden", !hasCard);
            if (hasCard)
                UpdateCardSection(top.CardInfo.Value);

            _anchor.RemoveFromClassList("lk-hidden");
        }

        private void UpdateCardSection(CardInfoData card)
        {
            if (_categoryBadge != null)
            {
                _categoryBadge.text = card.Category.ToString().ToUpperInvariant();
                SetCategoryBadgeModifier(_categoryBadge, card.Category);
            }

            if (_hpRow != null)
            {
                bool show = card.HasHP;
                _hpRow.EnableInClassList("lk-hidden", !show);
                if (show)
                {
                    float pct = card.MaxHP > 0 ? (float)card.CurrentHP / card.MaxHP : 0f;
                    if (_hpLabel != null)
                    {
                        _hpLabel.text = $"{card.CurrentHP}/{card.MaxHP}";
                        _hpLabel.EnableInClassList("lk-label--warning", pct is > 0.2f and <= 0.5f);
                        _hpLabel.EnableInClassList("lk-label--danger",  pct <= 0.2f);
                    }
                    if (_hpFill != null)
                    {
                        _hpFill.style.width = UnityEngine.UIElements.Length.Percent(pct * 100f);
                        _hpFill.EnableInClassList("lk-progress-bar__fill--warning", pct is > 0.2f and <= 0.5f);
                        _hpFill.EnableInClassList("lk-progress-bar__fill--danger",  pct <= 0.2f);
                    }
                }
            }

            if (_statsLabel != null)
            {
                bool show = card.HasCombat;
                _statsLabel.EnableInClassList("lk-hidden", !show);
                if (show)
                    _statsLabel.text = card.FormattedStats;
            }

            if (_resourceRow != null)
            {
                bool show = card.HasSell || card.HasNutrition;
                _resourceRow.EnableInClassList("lk-hidden", !show);
                if (show)
                {
                    if (_sellLabel != null)
                        _sellLabel.text = card.HasSell
                            ? GameLocalization.Format("card.sell", card.SellPrice)
                            : string.Empty;
                    _sellLabel?.EnableInClassList("lk-hidden", !card.HasSell);

                    if (_nutritionInfoLabel != null)
                        _nutritionInfoLabel.text = card.HasNutrition
                            ? GameLocalization.Format("card.nutritionValue", card.Nutrition)
                            : string.Empty;
                    _nutritionInfoLabel?.EnableInClassList("lk-hidden", !card.HasNutrition);
                }
            }

            if (_usesRow != null)
            {
                bool show = card.HasUses;
                _usesRow.EnableInClassList("lk-hidden", !show);
                if (show && _usesLabel != null)
                    _usesLabel.text = GameLocalization.Format("card.usesLeft", card.UsesLeft);
            }
        }

        private static void SetCategoryBadgeModifier(Label badge, CardCategory category)
        {
            badge.RemoveFromClassList("lk-info-badge--character");
            badge.RemoveFromClassList("lk-info-badge--consumable");
            badge.RemoveFromClassList("lk-info-badge--resource");
            badge.RemoveFromClassList("lk-info-badge--material");
            badge.RemoveFromClassList("lk-info-badge--equipment");
            badge.RemoveFromClassList("lk-info-badge--structure");
            badge.RemoveFromClassList("lk-info-badge--mob");

            string mod = category switch
            {
                CardCategory.Character  => "lk-info-badge--character",
                CardCategory.Consumable => "lk-info-badge--consumable",
                CardCategory.Resource   => "lk-info-badge--resource",
                CardCategory.Material   => "lk-info-badge--material",
                CardCategory.Equipment  => "lk-info-badge--equipment",
                CardCategory.Structure  => "lk-info-badge--structure",
                CardCategory.Mob        => "lk-info-badge--mob",
                _                       => null,
            };
            if (mod != null) badge.AddToClassList(mod);
        }

        private void OnActionClicked(ClickEvent evt)
        {
            if (_currentButtonAction != null)
                _currentButtonAction();
        }

        // ── Keyboard advance API (used by GameInputHandler for SPACE key) ──

        public bool HasActiveModal() => _currentButtonAction != null;
        public void AdvanceCurrentModal() => _currentButtonAction?.Invoke();
    }
}
