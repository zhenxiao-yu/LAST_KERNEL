using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Markyu.LastKernel
{
    [System.Serializable]
    public struct ParallaxLayer
    {
        public Transform transform;
        [Range(0f, 1f)]
        public float strength;
    }

    /// <summary>
    /// Animates the title screen background:
    ///   - Parallax layers driven by mouse / touch position
    ///   - Optional server-core scale + alpha pulse (Mathf.Sin)
    ///   - Optional scanline sprite scrolling vertically
    ///
    /// Operates entirely in world-space via SpriteRenderers — never moves the camera.
    /// Uses unscaledDeltaTime so animations run while the game is paused.
    /// </summary>
    public sealed class TitleBackgroundController : MonoBehaviour
    {
        // ── Parallax ───────────────────────────────────────────────────────────

        [Header("Parallax")]
        [SerializeField] private ParallaxLayer[] _parallaxLayers;
        [SerializeField, Range(0.01f, 1f)] private float _parallaxSmoothing  = 0.08f;
        [SerializeField]                   private float _parallaxMaxOffset   = 24f;

        // ── Server-core pulse ──────────────────────────────────────────────────

        [Header("Server Core (optional)")]
        [SerializeField] private Transform      _serverCore;
        [SerializeField] private SpriteRenderer _serverCoreRenderer;
        [SerializeField] private float          _pulseSpeed    = 1.2f;
        [SerializeField, Range(0.85f, 1f)]   private float _pulseScaleMin = 0.95f;
        [SerializeField, Range(1f,    1.2f)] private float _pulseScaleMax = 1.05f;
        [SerializeField, Range(0f,    1f)]   private float _pulseAlphaMin = 0.65f;
        [SerializeField, Range(0f,    1f)]   private float _pulseAlphaMax = 1.0f;

        // ── Scanline scroll ────────────────────────────────────────────────────

        [Header("Scanline Scroll (optional)")]
        [SerializeField] private Transform _scanlineTransform;
        [SerializeField] private float     _scanlineScrollSpeed = 0.4f;
        [SerializeField] private float     _scanlineScrollWrap  = 4f;

        // ── Runtime state ──────────────────────────────────────────────────────

        private Vector3[] _layerOrigins;
        private Vector2   _currentOffset;
        private Vector2   _targetOffset;
        private float     _pulsePhase;
        private Vector3   _coreBaseScale;
        private Vector3   _scanlineOrigin;

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            CacheLayerOrigins();
            CacheServerCoreState();
            CacheScanlineOrigin();
        }

        private void Update()
        {
            UpdateParallax();
            UpdateServerCorePulse();
            UpdateScanlineScroll();
        }

        // ── Init ───────────────────────────────────────────────────────────────

        private void CacheLayerOrigins()
        {
            if (_parallaxLayers == null || _parallaxLayers.Length == 0)
            {
                _layerOrigins = new Vector3[0];
                return;
            }

            _layerOrigins = new Vector3[_parallaxLayers.Length];
            for (int i = 0; i < _parallaxLayers.Length; i++)
            {
                if (_parallaxLayers[i].transform != null)
                    _layerOrigins[i] = _parallaxLayers[i].transform.localPosition;
            }
        }

        private void CacheServerCoreState()
        {
            if (_serverCore != null)
                _coreBaseScale = _serverCore.localScale;
        }

        private void CacheScanlineOrigin()
        {
            if (_scanlineTransform != null)
                _scanlineOrigin = _scanlineTransform.localPosition;
        }

        // ── Parallax ───────────────────────────────────────────────────────────

        private void UpdateParallax()
        {
            if (_layerOrigins == null || _layerOrigins.Length == 0)
                return;

            Vector2 inputPos   = GetPointerPosition();
            float   halfWidth  = Screen.width  * 0.5f;
            float   halfHeight = Screen.height * 0.5f;

            float dx = (halfWidth  > 0f) ? (inputPos.x - halfWidth)  / halfWidth  : 0f;
            float dy = (halfHeight > 0f) ? (inputPos.y - halfHeight) / halfHeight : 0f;

            dx = Mathf.Clamp(dx, -1f, 1f);
            dy = Mathf.Clamp(dy, -1f, 1f);

            _targetOffset  = new Vector2(dx, dy) * _parallaxMaxOffset;
            _currentOffset = Vector2.Lerp(_currentOffset, _targetOffset, _parallaxSmoothing);

            for (int i = 0; i < _parallaxLayers.Length; i++)
            {
                Transform t = _parallaxLayers[i].transform;
                if (t == null)
                    continue;

                float   s      = _parallaxLayers[i].strength;
                Vector3 origin = _layerOrigins[i];
                t.localPosition = new Vector3(
                    origin.x + _currentOffset.x * s,
                    origin.y - _currentOffset.y * s,   // screen Y is inverted relative to world Y
                    origin.z);
            }
        }

        private static Vector2 GetPointerPosition()
        {
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
                return Touchscreen.current.primaryTouch.position.ReadValue();
            return Mouse.current != null ? Mouse.current.position.ReadValue() : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
#else
            if (Input.touchCount > 0)
                return Input.GetTouch(0).position;
            return Input.mousePosition;
#endif
        }

        // ── Server-core pulse ──────────────────────────────────────────────────

        private void UpdateServerCorePulse()
        {
            if (_serverCore == null)
                return;

            _pulsePhase += _pulseSpeed * Time.unscaledDeltaTime;

            float t     = (Mathf.Sin(_pulsePhase) + 1f) * 0.5f;
            float scale = Mathf.Lerp(_pulseScaleMin, _pulseScaleMax, t);
            _serverCore.localScale = _coreBaseScale * scale;

            if (_serverCoreRenderer != null)
            {
                float alpha = Mathf.Lerp(_pulseAlphaMin, _pulseAlphaMax, t);
                Color c     = _serverCoreRenderer.color;
                c.a = alpha;
                _serverCoreRenderer.color = c;
            }
        }

        // ── Scanline scroll ────────────────────────────────────────────────────

        private void UpdateScanlineScroll()
        {
            if (_scanlineTransform == null)
                return;

            Vector3 pos = _scanlineTransform.localPosition;
            pos.y += _scanlineScrollSpeed * Time.unscaledDeltaTime;

            if (pos.y > _scanlineOrigin.y + _scanlineScrollWrap)
                pos.y -= _scanlineScrollWrap;

            _scanlineTransform.localPosition = pos;
        }
    }
}
