using UnityEngine;
using UnityEngine.UIElements;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Applies Screen.safeArea insets as padding on a UIDocument's root element.
    /// Attach alongside any UIDocument that needs mobile notch / home-indicator avoidance.
    /// Updates every frame only when the safe area changes (rare — orientation flip, fold).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class UiToolkitSafeAreaAdapter : MonoBehaviour
    {
        private UIDocument   _document;
        private Rect         _lastSafeArea;
        private VisualElement _root;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            _lastSafeArea = default;
            Apply();
        }

        private void Update()
        {
            if (Screen.safeArea != _lastSafeArea)
                Apply();
        }

        private void Apply()
        {
            _lastSafeArea = Screen.safeArea;
            _root = _document?.rootVisualElement;
            if (_root == null) return;

            Rect safe   = Screen.safeArea;
            int  sw     = Screen.width;
            int  sh     = Screen.height;

            float left   = safe.xMin;
            float right  = sw - safe.xMax;
            float bottom = sh - safe.yMax;
            float top    = safe.yMin;

            _root.style.paddingLeft   = left;
            _root.style.paddingRight  = right;
            _root.style.paddingBottom = bottom;
            _root.style.paddingTop    = top;
        }
    }
}
