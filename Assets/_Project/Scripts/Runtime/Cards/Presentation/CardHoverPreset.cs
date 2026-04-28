using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Markyu.LastKernel
{
    [CreateAssetMenu(menuName = "Last Kernel/Tween Presets/Card Hover Preset", fileName = "CardHoverPreset")]
    public class CardHoverPreset : ScriptableObject
    {
        [BoxGroup("Scale")]
        [SerializeField, Range(1f, 1.2f)]
        private float scale = 1.035f;

        [BoxGroup("Scale")]
        [SerializeField, Min(0.02f)]
        private float scaleDuration = 0.075f;

        [BoxGroup("Scale")]
        [SerializeField]
        private Ease scaleEase = Ease.OutQuad;

        [BoxGroup("Flash")]
        [SerializeField, Range(0f, 1f)]
        private float flashAmount = 0.05f;

        [BoxGroup("Material")]
        [SerializeField, Range(0f, 0.4f)]
        private float brightnessBoost = 0.06f;

        [BoxGroup("Material")]
        [SerializeField, Range(0f, 0.4f)]
        private float saturationBoost = 0.02f;

        [BoxGroup("Material")]
        [SerializeField, Range(0f, 1f)]
        private float glowIntensity = 0.08f;

        public float Scale           => scale;
        public float ScaleDuration   => scaleDuration;
        public Ease  ScaleEase       => scaleEase;
        public float FlashAmount     => flashAmount;
        public float BrightnessBoost => brightnessBoost;
        public float SaturationBoost => saturationBoost;
        public float GlowIntensity   => glowIntensity;

        private void OnValidate()
        {
            scale         = Mathf.Max(0.01f, scale);
            scaleDuration = Mathf.Max(0.02f, scaleDuration);
        }
    }
}
