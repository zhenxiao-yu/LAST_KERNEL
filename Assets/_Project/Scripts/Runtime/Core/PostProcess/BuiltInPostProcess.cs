using UnityEngine;
using DG.Tweening;

namespace Markyu.LastKernel
{
    [ExecuteInEditMode]
    public class BuiltInPostProcess : MonoBehaviour
    {
        [SerializeField, Tooltip("Material that applies the post-processing shader during rendering.")]
        private Material effectMaterial;

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
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnTimePaceChanged -= HandleTimePaceChanged;
                TimeManager.Instance.OnDayEnded        -= HandleDayEnded;
                TimeManager.Instance.OnDayStarted      -= HandleDayStarted;
            }
            _vignetteTween?.Kill();
            _grayscaleTween?.Kill();
            _neonEdgeTween?.Kill();
        }

        private void Initialize()
        {
            if (effectMaterial == null) return;
            effectMaterial.SetFloat("_VignettePower",    vignetteIntensity);
            effectMaterial.SetFloat("_GrayscaleAmount",  grayscaleIntensity);
            effectMaterial.SetFloat("_ChromaticAmount",  chromaticAmount);
            effectMaterial.SetFloat("_ScanlineStrength", scanlineStrength);
            effectMaterial.SetFloat("_ScanlineFreq",     scanlineFreq);
            effectMaterial.SetFloat("_ContrastBoost",    contrastBoost);
            effectMaterial.SetFloat("_SaturationBoost",  saturationBoost);
            effectMaterial.SetColor("_NeonEdgeColor",    neonEdgeColor);
            effectMaterial.SetFloat("_NeonEdgeIntensity", neonEdgeIntensity);
        }

        private void HandleTimePaceChanged(TimePace pace)
        {
            if (pace == TimePace.Paused) FadeInGrayscale();
            else FadeOutGrayscale();
        }

        private void HandleDayEnded(int _)   { FadeInVignette();  FadeInNeonEdge(); }
        private void HandleDayStarted(int _) { FadeOutVignette(); FadeOutNeonEdge(); }

        private void FadeInGrayscale(float duration = 0.3f, float target = 1f)
        {
            _grayscaleTween?.Kill();
            _grayscaleTween = DOTween.To(
                () => grayscaleIntensity,
                x  => { grayscaleIntensity = x; effectMaterial.SetFloat("_GrayscaleAmount", x); },
                target, duration).SetUpdate(true).SetLink(gameObject);
        }

        private void FadeOutGrayscale(float duration = 0.3f)
        {
            _grayscaleTween?.Kill();
            _grayscaleTween = DOTween.To(
                () => grayscaleIntensity,
                x  => { grayscaleIntensity = x; effectMaterial.SetFloat("_GrayscaleAmount", x); },
                0f, duration).SetUpdate(true).SetLink(gameObject);
        }

        private void FadeInVignette(float duration = 0.5f, float target = 1.2f)
        {
            _vignetteTween?.Kill();
            _vignetteTween = DOTween.To(
                () => vignetteIntensity,
                x  => { vignetteIntensity = x; effectMaterial.SetFloat("_VignettePower", x); },
                target, duration).SetUpdate(true).SetLink(gameObject);
        }

        private void FadeOutVignette(float duration = 0.5f)
        {
            _vignetteTween?.Kill();
            _vignetteTween = DOTween.To(
                () => vignetteIntensity,
                x  => { vignetteIntensity = x; effectMaterial.SetFloat("_VignettePower", x); },
                0f, duration).SetUpdate(true).SetLink(gameObject);
        }

        private void FadeInNeonEdge(float duration = 1.2f, float target = 0.25f)
        {
            _neonEdgeTween?.Kill();
            _neonEdgeTween = DOTween.To(
                () => neonEdgeIntensity,
                x  => { neonEdgeIntensity = x; effectMaterial.SetFloat("_NeonEdgeIntensity", x); },
                target, duration).SetUpdate(true).SetLink(gameObject);
        }

        private void FadeOutNeonEdge(float duration = 0.8f)
        {
            _neonEdgeTween?.Kill();
            _neonEdgeTween = DOTween.To(
                () => neonEdgeIntensity,
                x  => { neonEdgeIntensity = x; effectMaterial.SetFloat("_NeonEdgeIntensity", x); },
                0f, duration).SetUpdate(true).SetLink(gameObject);
        }
    }
}
