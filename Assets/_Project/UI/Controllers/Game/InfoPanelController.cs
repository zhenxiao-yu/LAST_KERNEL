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
        }

        private readonly object _hoverRequester = new object();
        private readonly Dictionary<object, InfoRequest> _activeRequests = new();

        private VisualElement _anchor;
        private Label         _headerLabel;
        private Label         _bodyLabel;
        private Button        _actionButton;
        private Action        _currentButtonAction;

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
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
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

            InfoPanel.Register(RequestInfoDisplay, ClearInfoRequest, RegisterHover, UnregisterHover);
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
                _headerLabel.text = hasHeader ? $"[{top.Header}]" : string.Empty;
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

            _anchor.RemoveFromClassList("lk-hidden");
        }

        private void OnActionClicked(ClickEvent evt)
        {
            if (_currentButtonAction != null)
                _currentButtonAction();
        }
    }
}
