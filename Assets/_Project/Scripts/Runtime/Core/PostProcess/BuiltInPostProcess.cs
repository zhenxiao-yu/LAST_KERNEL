using UnityEngine;
using DG.Tweening;

namespace Markyu.LastKernel
{
    [ExecuteInEditMode]
    public class BuiltInPostProcess : MonoBehaviour
    {
        public static BuiltInPostProcess Instance { get; private set; }

        [SerializeField, Tooltip("Material that applies the post-processing shader during rendering.")]
        private Material effectMaterial;

        [Header("Pixel Art")]
        [SerializeField, Range(0f, 1f), Tooltip("0 = native resolution. 1 = full 320×180 pixel snap.")]
        private float pixelizeAmount = 1f;
        [SerializeField, Range(80f, 640f),  Tooltip("Horizontal pixels in the pixel-art grid.")]
        private float pixelResX = 320f;
        [SerializeField, Range(45f, 360f),  Tooltip("Vertical pixels in the pixel-art grid.")]
        private float pixelResY = 180f;

        [Header("Base")]
        [SerializeField, Range(0f, 3f)]    private float vignetteIntensity  = 0f;
        [SerializeField, Range(0f, 1f)]    private float grayscaleIntensity = 0f;

        [Header("Neon Style")]
        [SerializeField, Range(0f, 0.02f)]   private float chromaticAmount  = 0.004f;
        [SerializeField, Range(0f, 1f)]      private float scanlineStrength = 0.06f;
        [SerializeField, Range(50f, 1000f)]  private float scanlineFreq     = 270f;
        [SerializeField, Range(0.8f, 1.5f)]  private float contrastBoost    = 1.08f;
        [SerializeField, Range(0.8f, 2.0f)]  private float saturationBoost  = 1.15f;
        [SerializeField]                     private Color neonEdgeColor     = new Color(0f, 0.86f, 1f, 1f);
        [SerializeField, Range(0f, 1f)]      private float neonEdgeIntensity = 0f;

        private Tweener _vignetteTween;
        private Tweener _grayscaleTween;
        private Tweener _neonEdgeTween;
        private Tweener _glitchChromaTween;
        private Tweener _glitchNeonTween;

        private void Awake()
        {
            if (!Application.isPlaying) return;
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnValidate() => Initialize();

        private void Start()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnTimePaceChanged += HandleTimePaceChanged;
                TimeManager.Instance.OnDayEnded        += HandleDayEnded;
                TimeManager.Instance.OnDayStarted      += HandleDayStarted;
            }
            Initialize();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnTimePaceChanged -= HandleTimePaceChanged;
                TimeManager.Instance.OnDayEnded        -= HandleDayEnded;
                TimeManager.Instance.OnDayStarted      -= HandleDayStarted;
            }
            _vignetteTween?.Kill();
            _grayscaleTween?.Kill();
            _neonEdgeTween?.Kill();
            _glitchChromaTween?.Kill();
            _glitchNeonTween?.Kill();
        }

        /// <summary>
        /// Brief screen-space glitch flash: spikes chromatic aberration then restores it.
        /// Safe to call from any gameplay code; ignored in Edit mode.
        /// </summary>
        public void PlayGlitchFlash(float peakChromatic = 0.025f, float peakNeonEdge = 0.35f, float duration = 0.22f)
        {
            if (!Application.isPlaying || effectMaterial == null) return;

            float restoreChroma = chromaticAmount;
            float restoreNeon   = neonEdgeIntensity;
            float attackTime    = duration * 0.2f;
            float releaseTime   = duration * 0.8f;

            _glitchChromaTween?.Kill();
            _glitchChromaTween = DOTween.To(
                    () => chromaticAmount,
                    x => { chromaticAmount = x; effectMaterial.SetFloat("_ChromaticAmount", x); },
                    peakChromatic, attackTime)
                .SetUpdate(true)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    _glitchChromaTween = DOTween.To(
                            () => chromaticAmount,
                            x => { chromaticAmount = x; effectMaterial.SetFloat("_ChromaticAmount", x); },
                            restoreChroma, releaseTime)
                        .SetUpdate(true)
                        .SetLink(gameObject);
                });

            _glitchNeonTween?.Kill();
            _glitchNeonTween = DOTween.To(
                    () => neonEdgeIntensity,
                    x => { neonEdgeIntensity = x; effectMaterial.SetFloat("_NeonEdgeIntensity", x); },
                    peakNeonEdge, attackTime)
                .SetUpdate(true)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    _glitchNeonTween = DOTween.To(
                            () => neonEdgeIntensity,
                            x => { neonEdgeIntensity = x; effectMaterial.SetFloat("_NeonEdgeIntensity", x); },
                            restoreNeon, releaseTime)
                        .SetUpdate(true)
                        .SetLink(gameObject);
                });
        }

        private void Initialize()
        {
            if (effectMaterial == null) return;

            effectMaterial.SetFloat  ("_PixelizeAmount",   pixelizeAmount);
            effectMaterial.SetVector ("_PixelRes",         new Vector4(pixelResX, pixelResY, 0f, 0f));
            effectMaterial.SetFloat  ("_VignettePower",    vignetteIntensity);
            effectMaterial.SetFloat  ("_GrayscaleAmount",  grayscaleIntensity);
            effectMaterial.SetFloat  ("_ChromaticAmount",  chromaticAmount);
            effectMaterial.SetFloat  ("_ScanlineStrength", scanlineStrength);
            effectMaterial.SetFloat  ("_ScanlineFreq",     scanlineFreq);
            effectMaterial.SetFloat  ("_ContrastBoost",    contrastBoost);
            effectMaterial.SetFloat  ("_SaturationBoost",  saturationBoost);
            effectMaterial.SetColor  ("_NeonEdgeColor",    neonEdgeColor);
            effectMaterial.SetFloat  ("_NeonEdgeIntensity", neonEdgeIntensity);
        }

        // ── Day / Night transitions ────────────────────────────────────────────────

        private void HandleTimePaceChanged(TimePace pace)
        {
            if (pace == TimePace.Paused) FadeInGrayscale();
            else FadeOutGrayscale();
        }

        private void HandleDayEnded(int _)   { FadeInVignette();  FadeInNeonEdge(); }
        private void HandleDayStarted(int _) { FadeOutVignette(); FadeOutNeonEdge(); }

        // ── Tweens ────────────────────────────────────────────────────────────────

        private void FadeInGrayscale(float duration = 0.3f, float target = 1f)
        {
            _grayscaleTween?.Kill();
            _grayscaleTween = DOTween.To(
                () => grayscaleIntensity,
                x  => { grayscaleIntensity = x; effectMaterial?.SetFloat("_GrayscaleAmount", x); },
                target, duration).SetUpdate(true).SetLink(gameObject);
        }

        private void FadeOutGrayscale(float duration = 0.3f)
        {
            _grayscaleTween?.Kill();
            _grayscaleTween = DOTween.To(
                () => grayscaleIntensity,
                x  => { grayscaleIntensity = x; effectMaterial?.SetFloat("_GrayscaleAmount", x); },
                0f, duration).SetUpdate(true).SetLink(gameObject);
        }

        private void FadeInVignette(float duration = 0.5f, float target = 1.2f)
        {
            _vignetteTween?.Kill();
            _vignetteTween = DOTween.To(
                () => vignetteIntensity,
                x  => { vignetteIntensity = x; effectMaterial?.SetFloat("_VignettePower", x); },
                target, duration).SetUpdate(true).SetLink(gameObject);
        }

        private void FadeOutVignette(float duration = 0.5f)
        {
            _vignetteTween?.Kill();
            _vignetteTween = DOTween.To(
                () => vignetteIntensity,
                x  => { vignetteIntensity = x; effectMaterial?.SetFloat("_VignettePower", x); },
                0f, duration).SetUpdate(true).SetLink(gameObject);
        }

        private void FadeInNeonEdge(float duration = 1.2f, float target = 0.25f)
        {
            _neonEdgeTween?.Kill();
            _neonEdgeTween = DOTween.To(
                () => neonEdgeIntensity,
                x  => { neonEdgeIntensity = x; effectMaterial?.SetFloat("_NeonEdgeIntensity", x); },
                target, duration).SetUpdate(true).SetLink(gameObject);
        }

        private void FadeOutNeonEdge(float duration = 0.8f)
        {
            _neonEdgeTween?.Kill();
            _neonEdgeTween = DOTween.To(
                () => neonEdgeIntensity,
                x  => { neonEdgeIntensity = x; effectMaterial?.SetFloat("_NeonEdgeIntensity", x); },
                0f, duration).SetUpdate(true).SetLink(gameObject);
        }
    }
}
