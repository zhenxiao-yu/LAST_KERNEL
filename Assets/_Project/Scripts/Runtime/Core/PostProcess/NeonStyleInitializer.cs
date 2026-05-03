using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Sirenix.OdinInspector;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Drop this MonoBehaviour into the scene to enable the cyberpunk neon fusion look.
    /// Creates a runtime URP Volume (Bloom + Chromatic Aberration) and scales per-card
    /// emission glow via CardFeelPresenter statics. Remove from scene to revert.
    /// </summary>
    public class NeonStyleInitializer : MonoBehaviour
    {
        [Title("Card Glow")]
        [SerializeField, Range(0f, 8f), Tooltip("Multiplier on CardFeelPresenter hover/drag glow. 4 = vivid neon.")]
        private float neonGlowScale = 4f;
        [SerializeField, Range(0f, 1f), Tooltip("Strength of the per-card idle ambient breathe emission.")]
        private float neonAmbientIntensity = 1f;

        [Title("URP Bloom")]
        [SerializeField] private float bloomIntensity  = 1.8f;
        [SerializeField] private float bloomThreshold  = 0.85f;
        [SerializeField] private float bloomScatter    = 0.7f;
        [SerializeField] private Color bloomTint       = new Color(0.4f, 0.85f, 1f, 1f);

        [Title("URP Chromatic Aberration")]
        [SerializeField, Range(0f, 1f)] private float chromaticIntensity = 0.3f;

        private Volume         _volume;
        private VolumeProfile  _runtimeProfile;

        private void Start() => Apply();

        private void Apply()
        {
            CardFeelPresenter.NeonGlowScale       = neonGlowScale;
            CardFeelPresenter.NeonAmbientIntensity = neonAmbientIntensity;

            var go = new GameObject("NeonStyle_Volume");
            go.transform.SetParent(transform);
            _volume          = go.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = 10f;

            _runtimeProfile = ScriptableObject.CreateInstance<VolumeProfile>();

            var bloom = _runtimeProfile.Add<Bloom>();
            bloom.active = true;
            bloom.intensity.Override(bloomIntensity);
            bloom.threshold.Override(bloomThreshold);
            bloom.scatter.Override(bloomScatter);
            bloom.tint.Override(bloomTint);

            var ca = _runtimeProfile.Add<ChromaticAberration>();
            ca.active = true;
            ca.intensity.Override(chromaticIntensity);

            _volume.sharedProfile = _runtimeProfile;
        }

        private void OnDestroy()
        {
            CardFeelPresenter.NeonGlowScale       = 1f;
            CardFeelPresenter.NeonAmbientIntensity = 0f;

            if (_runtimeProfile != null)
                Destroy(_runtimeProfile);
        }
    }
}
