using UnityEngine;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Adjusts a RectTransform so its content stays within the device safe area,
    /// avoiding notches, rounded corners, and home-indicator strips.
    ///
    /// Usage: attach to the root RectTransform of any Screen Space — Overlay or
    /// Screen Space — Camera canvas panel that must not bleed under hardware cutouts.
    /// Not required for World Space canvases (they are positioned in 3D).
    ///
    /// The component re-applies whenever the safe area or orientation changes so
    /// dynamic orientation switches (e.g. tablet autorotate) are handled.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaAnchor : MonoBehaviour
    {
        private RectTransform    _rt;
        private Rect             _lastSafeArea   = Rect.zero;
        private ScreenOrientation _lastOrientation;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
        }

        private void Start()
        {
            Apply(Screen.safeArea);
        }

        private void LateUpdate()
        {
            if (Screen.safeArea != _lastSafeArea || Screen.orientation != _lastOrientation)
                Apply(Screen.safeArea);
        }

        private void Apply(Rect safeArea)
        {
            _lastSafeArea    = safeArea;
            _lastOrientation = Screen.orientation;

            // Convert pixel rect to normalised anchor values in [0,1] space.
            var anchorMin = safeArea.position;
            var anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            _rt.anchorMin = anchorMin;
            _rt.anchorMax = anchorMax;
            _rt.offsetMin = Vector2.zero;
            _rt.offsetMax = Vector2.zero;
        }
    }
}
