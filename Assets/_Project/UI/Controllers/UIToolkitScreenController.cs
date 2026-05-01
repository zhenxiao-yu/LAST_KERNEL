using System.Reflection;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UIElements;

namespace Markyu.LastKernel
{
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

        protected LKLocalizedBinder Localizer { get; } = new LKLocalizedBinder();

        protected virtual bool AffectedByUIScale => false;

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
            if (AffectedByUIScale) UIScaleManager.Register(Document, Root);

            if (rootController != null && !string.IsNullOrEmpty(screenId))
                rootController.RegisterScreen(screenId, this);

            OnLocalizationRefresh();
        }

        protected virtual void OnEnable()
        {
            GameLocalization.LanguageChanged += HandleLanguageChanged;
            UIScaleManager.ScaleChanged      += HandleScaleChanged;
            if (Root != null) OnLocalizationRefresh();
        }

        protected virtual void OnDisable()
        {
            GameLocalization.LanguageChanged -= HandleLanguageChanged;
            UIScaleManager.ScaleChanged      -= HandleScaleChanged;
        }

        protected virtual void OnDestroy()
        {
            if (AffectedByUIScale) UIScaleManager.Unregister(Document);
        }

        // ── Binding ────────────────────────────────────────────────────────────

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
        private void HandleScaleChanged(UIScale _)
        {
            if (AffectedByUIScale) UIScaleManager.Register(Document, Root);
            OnLocalizationRefresh();
        }

        public virtual void OnLocalizationRefresh() => Localizer.RefreshAll();
    }
}
