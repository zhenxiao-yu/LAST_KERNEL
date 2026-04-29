using System.Reflection;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UIElements;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Base class for all UI Toolkit screen and panel controllers.
    ///
    /// Each concrete subclass:
    ///   1. Overrides OnBind() to query and cache child VisualElements.
    ///   2. Overrides OnLocalizationRefresh() to update visible labels.
    ///   3. Raises UIEventBus events for all user-intent signals (no direct singleton calls).
    ///
    /// Show/Hide enable or disable the UIDocument so panels are fully removed
    /// from the render tree when not in use. Subclasses can override for animation.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public abstract class UIToolkitScreenController : MonoBehaviour
    {
        [BoxGroup("Registration")]
        [SerializeField, Tooltip("Optional: register this screen with a root controller for coordinated show/hide.")]
        private UIToolkitRootController rootController;

        [BoxGroup("Registration")]
        [SerializeField, Tooltip("Unique ID used when registering with the root controller.")]
        private string screenId;

        protected UIDocument Document { get; private set; }
        protected VisualElement Root   { get; private set; }

        public bool IsVisible => Document != null && Document.enabled;

        /// <summary>
        /// Label → localization-key registry. Call Localizer.Bind/BindFormat in OnBind().
        /// RefreshAll() is called automatically on language change and after Show().
        /// </summary>
        protected LKLocalizedBinder Localizer { get; } = new LKLocalizedBinder();

        // ── Lifecycle ──────────────────────────────────────────────────────────

        protected virtual void Awake()
        {
            Document = GetComponent<UIDocument>();
        }

        // UIDocument.rootVisualElement is only ready after UIDocument.OnEnable() runs.
        // Start() is the first safe point to query it (all Awake + OnEnable have completed).
        protected virtual void Start()
        {
            Root = Document.rootVisualElement;

            var attr = GetType().GetCustomAttribute<UIScreenAttribute>(false);
            if (attr != null) Document.sortingOrder = attr.SortingOrder;

            OnBind();

            if (rootController != null && !string.IsNullOrEmpty(screenId))
                rootController.RegisterScreen(screenId, this);

            OnLocalizationRefresh();
        }

        protected virtual void OnEnable()
        {
            GameLocalization.LanguageChanged += HandleLanguageChanged;
            // OnLocalizationRefresh is deferred to Start() on first run.
            // On subsequent enable/disable cycles Root is already bound, so refresh immediately.
            if (Root != null) OnLocalizationRefresh();
        }

        protected virtual void OnDisable()
        {
            GameLocalization.LanguageChanged -= HandleLanguageChanged;
        }

        // ── Binding ────────────────────────────────────────────────────────────

        /// <summary>
        /// Query and store child VisualElement references, and register event callbacks.
        /// Called once in Awake after the UIDocument is ready.
        /// </summary>
        protected abstract void OnBind();

        // ── Visibility ─────────────────────────────────────────────────────────

        public virtual void Show()
        {
            if (Document == null) return;
            Document.enabled = true;
            OnShow();
            OnLocalizationRefresh();
        }

        public virtual void Hide()
        {
            if (Document == null) return;
            Document.enabled = false;
            OnHide();
        }

        protected virtual void OnShow() { }
        protected virtual void OnHide() { }

        // ── Localization ───────────────────────────────────────────────────────

        private void HandleLanguageChanged(GameLanguage _) => OnLocalizationRefresh();

        /// <summary>
        /// Update all visible localized labels. Called on language change and after Show().
        /// Base implementation calls Localizer.RefreshAll(); subclasses should call base
        /// before updating any additional dynamic labels.
        /// </summary>
        public virtual void OnLocalizationRefresh() => Localizer.RefreshAll();
    }
}
