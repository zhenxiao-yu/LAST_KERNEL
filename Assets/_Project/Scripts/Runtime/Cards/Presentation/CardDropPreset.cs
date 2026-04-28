using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Markyu.LastKernel
{
    [CreateAssetMenu(menuName = "Last Kernel/Tween Presets/Card Drop Preset", fileName = "CardDropPreset")]
    public class CardDropPreset : ScriptableObject
    {
        [BoxGroup("Squish")]
        [SerializeField, Range(0.8f, 1f)]
        private float squishScale = 0.965f;

        [BoxGroup("Squish")]
        [SerializeField, Min(0.03f)]
        private float squishDuration = 0.045f;

        [BoxGroup("Squish")]
        [SerializeField, Min(0.05f)]
        private float settleDuration = 0.09f;

        [BoxGroup("Squish")]
        [SerializeField]
        private Ease settleEase = Ease.OutQuad;

        [BoxGroup("Squish")]
        [SerializeField, Range(0f, 0.75f)]
        private float settleOvershoot = 0f;

        [BoxGroup("Spawn")]
        [SerializeField, Range(0.5f, 1f)]
        private float spawnStartScale = 0.85f;

        [BoxGroup("Spawn")]
        [SerializeField, Min(0.05f)]
        private float spawnDuration = 0.12f;

        [BoxGroup("Spawn")]
        [SerializeField]
        private Ease spawnEase = Ease.OutBack;

        [BoxGroup("Spawn")]
        [SerializeField, Range(0f, 0.75f)]
        private float spawnOvershoot = 0.45f;

        [BoxGroup("Merge")]
        [SerializeField, Range(0f, 0.2f)]
        private float mergePunchAmount = 0.055f;

        [BoxGroup("Merge")]
        [SerializeField, Min(0.05f)]
        private float mergePunchDuration = 0.12f;

        [BoxGroup("Merge")]
        [SerializeField, Range(1, 20)]
        private int mergePunchVibrato = 4;

        [BoxGroup("Merge")]
        [SerializeField, Range(0f, 1f)]
        private float mergeFlashAmount = 0.13f;

        public float SquishScale        => squishScale;
        public float SquishDuration     => squishDuration;
        public float SettleDuration     => settleDuration;
        public Ease  SettleEase         => settleEase;
        public float SettleOvershoot    => settleOvershoot;
        public float SpawnStartScale    => spawnStartScale;
        public float SpawnDuration      => spawnDuration;
        public Ease  SpawnEase          => spawnEase;
        public float SpawnOvershoot     => spawnOvershoot;
        public float MergePunchAmount   => mergePunchAmount;
        public float MergePunchDuration => mergePunchDuration;
        public int   MergePunchVibrato  => mergePunchVibrato;
        public float MergeFlashAmount   => mergeFlashAmount;

        private void OnValidate()
        {
            squishDuration     = Mathf.Max(0.03f, squishDuration);
            settleDuration     = Mathf.Max(0.05f, settleDuration);
            settleOvershoot    = Mathf.Clamp(settleOvershoot, 0f, 0.75f);
            spawnStartScale    = Mathf.Clamp(spawnStartScale, 0.5f, 1f);
            spawnDuration      = Mathf.Max(0.05f, spawnDuration);
            spawnOvershoot     = Mathf.Clamp(spawnOvershoot, 0f, 0.75f);
            mergePunchDuration = Mathf.Max(0.05f, mergePunchDuration);
        }
    }
}
