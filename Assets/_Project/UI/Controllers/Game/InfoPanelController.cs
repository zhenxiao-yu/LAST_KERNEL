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
        private Label         _combatTypeBadge;
        private VisualElement _loreSection;
        private Button        _loreToggle;
        private Label         _loreLabel;
        private VisualElement _visibleSection;
        private Button        _visibleToggle;
        private Label         _visibleStatsLabel;
        private VisualElement _hiddenSection;
        private Button        _hiddenToggle;
        private Label         _hiddenStatsLabel;
        private VisualElement _economySection;
        private Button        _economyToggle;
        private Label         _economyLabel;
        private VisualElement _hpRow;
        private Label         _hpLabel;
        private VisualElement _hpFill;
        private Label         _statsLabel;
        private VisualElement _resourceRow;
        private Label         _sellLabel;
        private Label         _nutritionInfoLabel;
        private VisualElement _usesRow;
        private Label         _usesLabel;
        private bool          _loreExpanded = true;
        private bool          _visibleExpanded = true;
        private bool          _hiddenExpanded = true;
        private bool          _economyExpanded = true;

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
            _combatTypeBadge  = Root.Q<Label>        ("lbl-info-combat-type");
            _loreSection      = Root.Q<VisualElement>("info-lore-section");
            _loreToggle       = Root.Q<Button>       ("btn-info-lore");
            _loreLabel        = Root.Q<Label>        ("lbl-info-lore");
            _visibleSection   = Root.Q<VisualElement>("info-visible-section");
            _visibleToggle    = Root.Q<Button>       ("btn-info-visible");
            _visibleStatsLabel = Root.Q<Label>       ("lbl-info-visible-stats");
            _hiddenSection    = Root.Q<VisualElement>("info-hidden-section");
            _hiddenToggle     = Root.Q<Button>       ("btn-info-hidden");
            _hiddenStatsLabel = Root.Q<Label>        ("lbl-info-hidden-stats");
            _economySection   = Root.Q<VisualElement>("info-economy-section");
            _economyToggle    = Root.Q<Button>       ("btn-info-economy");
            _economyLabel     = Root.Q<Label>        ("lbl-info-economy");
            _hpRow            = Root.Q<VisualElement>("info-hp-row");
            _hpLabel          = Root.Q<Label>        ("lbl-info-hp");
            _hpFill           = Root.Q<VisualElement>("fill-info-hp");
            _statsLabel       = Root.Q<Label>        ("lbl-info-stats");
            _resourceRow        = Root.Q<VisualElement>("info-resource-row");
            _sellLabel          = Root.Q<Label>        ("lbl-info-sell");
            _nutritionInfoLabel = Root.Q<Label>        ("lbl-info-nutrition");
            _usesRow            = Root.Q<VisualElement>("info-uses-row");
            _usesLabel          = Root.Q<Label>        ("lbl-info-uses");

            _loreToggle?.RegisterCallback<ClickEvent>(_ => ToggleSection(ref _loreExpanded));
            _visibleToggle?.RegisterCallback<ClickEvent>(_ => ToggleSection(ref _visibleExpanded));
            _hiddenToggle?.RegisterCallback<ClickEvent>(_ => ToggleSection(ref _hiddenExpanded));
            _economyToggle?.RegisterCallback<ClickEvent>(_ => ToggleSection(ref _economyExpanded));

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
            bool hasCard = top.CardInfo.HasValue;

            if (_headerLabel != null)
            {
                _headerLabel.text = hasHeader ? top.Header : string.Empty;
                _headerLabel.EnableInClassList("lk-hidden", !hasHeader);
            }

            if (_bodyLabel != null)
            {
                _bodyLabel.text = top.Body ?? string.Empty;
                _bodyLabel.EnableInClassList("lk-hidden", hasCard || string.IsNullOrWhiteSpace(top.Body));
            }

            if (_actionButton != null)
            {
                _actionButton.text = hasButton ? top.ButtonLabel : string.Empty;
                _actionButton.EnableInClassList("lk-hidden", !hasButton);
                _currentButtonAction = hasButton ? top.ButtonAction : null;
            }

            _cardSection?.EnableInClassList("lk-hidden", !hasCard);
            if (hasCard)
                UpdateCardSection(top.CardInfo.Value);

            _anchor.RemoveFromClassList("lk-hidden");
        }

        private void UpdateCardSection(CardInfoData card)
        {
            if (_categoryBadge != null)
            {
                _categoryBadge.text = CardDossierFormatter.CategoryLabel(card.Category).ToUpperInvariant();
                SetCategoryBadgeModifier(_categoryBadge, card.Category);
            }

            if (_combatTypeBadge != null)
            {
                bool show = card.Combat != CombatType.None;
                _combatTypeBadge.EnableInClassList("lk-hidden", !show);
                if (show)
                    _combatTypeBadge.text = CardDossierFormatter.CombatTypeLabel(card.Combat).ToUpperInvariant();
            }

            SetSection(
                _loreSection,
                _loreToggle,
                _loreLabel,
                GameLocalization.Get("card.section.lore"),
                card.LoreText,
                _loreExpanded);

            SetSection(
                _visibleSection,
                _visibleToggle,
                _visibleStatsLabel,
                GameLocalization.Get("card.section.visibleStats"),
                card.VisibleStatsText,
                _visibleExpanded);

            SetSection(
                _hiddenSection,
                _hiddenToggle,
                _hiddenStatsLabel,
                GameLocalization.Get("card.section.hiddenStats"),
                card.HiddenStatsText,
                _hiddenExpanded);

            SetSection(
                _economySection,
                _economyToggle,
                _economyLabel,
                GameLocalization.Get("card.section.economy"),
                card.EconomyText,
                _economyExpanded);

            if (_hpRow != null)
            {
                bool show = card.HasHP;
                _hpRow.EnableInClassList("lk-hidden", !show);
                if (show)
                {
                    float pct = card.MaxHP > 0 ? (float)card.CurrentHP / card.MaxHP : 0f;
                    if (_hpLabel != null)
                    {
                        _hpLabel.text = GameLocalization.Format("card.health", card.CurrentHP, card.MaxHP);
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
                _statsLabel.EnableInClassList("lk-hidden", true);
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

        private void ToggleSection(ref bool expanded)
        {
            expanded = !expanded;
            RefreshDisplay();
        }

        private static void SetSection(
            VisualElement section,
            Button toggle,
            Label content,
            string title,
            string body,
            bool expanded)
        {
            bool show = !string.IsNullOrWhiteSpace(body);
            section?.EnableInClassList("lk-hidden", !show);
            if (!show) return;

            if (toggle != null)
                toggle.text = $"{(expanded ? "[-]" : "[+]")} {title}";

            if (content != null)
            {
                content.text = body;
                content.EnableInClassList("lk-hidden", !expanded);
            }
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
