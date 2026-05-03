using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Instantiate at a world position to play a self-contained "glitch destroy" effect:
    ///   - Spikes screen chromatic aberration and neon-edge glow via BuiltInPostProcess
    ///   - Plays the attached ParticleSystem (configure in the prefab Inspector)
    ///   - Optionally shakes its own transform for a local jitter
    ///   - Auto-destroys after <see cref="lifetime"/> seconds
    ///
    /// Assign the prefab to CardBuyer.glitchDestroyEffect in the Inspector.
    /// </summary>
    [AddComponentMenu("Last Kernel/VFX/Glitch Destroy Effect")]
    public class GlitchDestroyEffect : MonoBehaviour
    {
        [BoxGroup("Screen Flash")]
        [SerializeField, Tooltip("Peak chromatic aberration during the glitch flash.")]
        private float peakChromaticAmount = 0.028f;

        [BoxGroup("Screen Flash")]
        [SerializeField, Tooltip("Peak neon-edge intensity during the glitch flash.")]
        private float peakNeonEdge = 0.4f;

        [BoxGroup("Screen Flash")]
        [SerializeField, Tooltip("Total duration of the chromatic spike and return (seconds).")]
        private float glitchFlashDuration = 0.22f;

        [BoxGroup("Jitter")]
        [SerializeField, Tooltip("If true, shakes this transform's position for a local jitter effect.")]
        private bool playLocalJitter = true;

        [BoxGroup("Jitter")]
        [SerializeField, Tooltip("Strength of the local position jitter.")]
        private float jitterStrength = 0.12f;

        [BoxGroup("Jitter")]
        [SerializeField, Tooltip("Duration of the local jitter (seconds).")]
        private float jitterDuration = 0.18f;

        [BoxGroup("Lifetime")]
        [SerializeField, Tooltip("Seconds before this GameObject is destroyed. Should be >= particle duration.")]
        private float lifetime = 0.7f;

        private void Awake()
        {
            // Screen glitch flash
            BuiltInPostProcess.Instance?.PlayGlitchFlash(peakChromaticAmount, peakNeonEdge, glitchFlashDuration);

            // Play attached particle system if present
            var ps = GetComponent<ParticleSystem>();
            if (ps != null)
                ps.Play();

            // Local position jitter
            if (playLocalJitter)
            {
                transform
                    .DOShakePosition(jitterDuration, new Vector3(jitterStrength, 0f, jitterStrength), 14, 0f)
                    .SetLink(gameObject);
            }

            // Audio — distinct two-hit glitch sound: Puff then Click
            AudioManager.Instance?.PlaySFX(AudioId.Puff);
            DOVirtual.DelayedCall(0.07f, () => AudioManager.Instance?.PlaySFX(AudioId.Click))
                .SetLink(gameObject);

            Destroy(gameObject, lifetime);
        }
    }
}
