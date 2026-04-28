using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Markyu.LastKernel
{
    [CreateAssetMenu(menuName = "Last Kernel/Tween Presets/Card Drag Preset", fileName = "CardDragPreset")]
    public class CardDragPreset : ScriptableObject
    {
        [BoxGroup("Pickup Punch")]
        [SerializeField, Range(0f, 0.3f)]
        private float punchAmount = 0.075f;

        [BoxGroup("Pickup Punch")]
        [SerializeField, Min(0.04f)]
        private float punchDuration = 0.10f;

        [BoxGroup("Pickup Punch")]
        [SerializeField, Range(1, 20)]
        private int punchVibrato = 4;

        [BoxGroup("Pickup Punch")]
        [SerializeField, Range(1f, 1.2f)]
        private float holdScale = 1.035f;

        [BoxGroup("Flash")]
        [SerializeField, Range(0f, 1f)]
        private float flashAmount = 0.10f;

        [BoxGroup("Material")]
        [SerializeField, Range(0f, 0.4f)]
        private float brightnessBoost = 0.10f;

        [BoxGroup("Material")]
        [SerializeField, Range(0f, 1f)]
        private float glowIntensity = 0.14f;

        public float PunchAmount     => punchAmount;
        public float PunchDuration   => punchDuration;
        public int   PunchVibrato    => punchVibrato;
        public float HoldScale       => holdScale;
        public float FlashAmount     => flashAmount;
        public float BrightnessBoost => brightnessBoost;
        public float GlowIntensity   => glowIntensity;

        private void OnValidate()
        {
            punchDuration = Mathf.Max(0.04f, punchDuration);
            holdScale     = Mathf.Max(0.01f, holdScale);
        }
    }
}
