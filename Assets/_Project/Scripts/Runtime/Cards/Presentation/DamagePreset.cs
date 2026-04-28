using Sirenix.OdinInspector;
using UnityEngine;

namespace Markyu.LastKernel
{
    [CreateAssetMenu(menuName = "Last Kernel/Tween Presets/Damage Preset", fileName = "DamagePreset")]
    public class DamagePreset : ScriptableObject
    {
        [BoxGroup("Shake")]
        [SerializeField, Range(0f, 0.3f), Tooltip("Scale punch magnitude for 2D visual shake on damage.")]
        private float shakeAmount = 0.06f;

        [BoxGroup("Shake")]
        [SerializeField, Min(0.05f)]
        private float shakeDuration = 0.20f;

        [BoxGroup("Flash")]
        [SerializeField, Range(0f, 1f)]
        private float flashAmount = 0.55f;

        [BoxGroup("Flash")]
        [SerializeField, Min(0.03f)]
        private float flashReturnDuration = 0.13f;

        public float ShakeAmount         => shakeAmount;
        public float ShakeDuration       => shakeDuration;
        public float FlashAmount         => flashAmount;
        public float FlashReturnDuration => flashReturnDuration;

        private void OnValidate()
        {
            shakeDuration       = Mathf.Max(0.05f, shakeDuration);
            flashReturnDuration = Mathf.Max(0.03f, flashReturnDuration);
        }
    }
}
