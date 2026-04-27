using UnityEngine;
using DG.Tweening;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Tunable settings for card interaction feel.
    /// Pure 2D — no tilt, no lift, no UV parallax.
    /// All state priorities are owned by CardRenderOrderController.
    /// </summary>
    [CreateAssetMenu(menuName = "Last Kernel/Card Feel Profile", fileName = "CardFeelProfile")]
    public class CardFeelProfile : ScriptableObject
    {
        [Header("Hover")]
        [SerializeField, Tooltip("Uniform scale when cursor is over the card.")]
        private float hoverScale = 1.035f;

        [SerializeField, Min(0.02f)]
        private float hoverScaleDuration = 0.075f;

        [SerializeField]
        private Ease hoverScaleEase = Ease.OutQuad;

        [Header("Pickup")]
        [SerializeField]
        private float pickupPunchAmount = 0.075f;

        [SerializeField, Min(0.04f)]
        private float pickupPunchDuration = 0.10f;

        [SerializeField, Range(1, 20)]
        private int pickupPunchVibrato = 4;

        [SerializeField]
        private float dragHoldScale = 1.035f;

        [Header("Drop / Settle")]
        [SerializeField]
        private float dropSquishScale = 0.965f;

        [SerializeField, Min(0.03f)]
        private float dropSquishDuration = 0.045f;

        [SerializeField, Min(0.05f)]
        private float dropSettleDuration = 0.09f;

        [SerializeField]
        private Ease dropSettleEase = Ease.OutQuad;

        [SerializeField, Range(0f, 2f)]
        private float dropSettleOvershoot = 0f;

        [Header("Spawn")]
        [SerializeField]
        private float spawnStartScale = 0.85f;

        [SerializeField, Min(0.05f)]
        private float spawnDuration = 0.12f;

        [SerializeField]
        private Ease spawnEase = Ease.OutBack;

        [SerializeField, Range(0f, 2.5f)]
        private float spawnOvershoot = 0.45f;

        [Header("Merge / Stack Accept")]
        [SerializeField]
        private float mergePunchAmount = 0.055f;

        [SerializeField, Min(0.05f)]
        private float mergePunchDuration = 0.12f;

        [SerializeField, Range(1, 20)]
        private int mergePunchVibrato = 4;

        [Header("Shader Feedback")]
        [SerializeField, Range(0f, 1f)]
        private float hoverFlashAmount = 0.05f;

        [SerializeField, Range(0f, 1f)]
        private float pickupFlashAmount = 0.10f;

        [SerializeField, Range(0f, 1f)]
        private float mergeFlashAmount = 0.13f;

        [SerializeField, Range(0f, 1f)]
        private float damageFlashAmount = 0.55f;

        [SerializeField, Min(0.03f)]
        private float flashReturnDuration = 0.13f;

        [SerializeField, Range(0f, 0.4f)]
        private float hoverBrightnessBoost = 0.06f;

        [SerializeField, Range(0f, 0.4f)]
        private float dragBrightnessBoost = 0.10f;

        [SerializeField, Range(0f, 0.4f)]
        private float hoverSaturationBoost = 0.02f;

        [SerializeField, Range(0f, 0.15f), Tooltip("Keep 0 for stable pixel-art colors.")]
        private float idleHueShiftAmount = 0f;

        [SerializeField, Range(0.1f, 3f)]
        private float idleHueShiftFrequency = 0.45f;

        [SerializeField]
        private Color glowColor = new Color(0.52f, 0.85f, 1f, 1f);

        [SerializeField, Range(0f, 1f)]
        private float hoverGlowIntensity = 0.08f;

        [SerializeField, Range(0f, 1f)]
        private float dragGlowIntensity = 0.14f;

        [Header("Damage Feedback")]
        [SerializeField, Range(0f, 0.3f), Tooltip("Scale punch magnitude for 2D shake on damage.")]
        private float damageShakeAmount = 0.06f;

        [SerializeField, Min(0.05f)]
        private float damageShakeDuration = 0.20f;

        [Header("Movement Snap")]
        [SerializeField]
        private float snapDuration = 0.10f;

        [SerializeField]
        private Ease snapEase = Ease.OutQuad;

        [SerializeField, Range(0f, 2f)]
        private float snapOvershoot = 0f;

        public float HoverScale => hoverScale;
        public float HoverScaleDuration => hoverScaleDuration;
        public Ease HoverScaleEase => hoverScaleEase;

        public float PickupPunchAmount => pickupPunchAmount;
        public float PickupPunchDuration => pickupPunchDuration;
        public int PickupPunchVibrato => pickupPunchVibrato;
        public float DragHoldScale => dragHoldScale;

        public float DropSquishScale => dropSquishScale;
        public float DropSquishDuration => dropSquishDuration;
        public float DropSettleDuration => dropSettleDuration;
        public Ease DropSettleEase => dropSettleEase;
        public float DropSettleOvershoot => dropSettleOvershoot;

        public float SpawnStartScale => spawnStartScale;
        public float SpawnDuration => spawnDuration;
        public Ease SpawnEase => spawnEase;
        public float SpawnOvershoot => spawnOvershoot;

        public float MergePunchAmount => mergePunchAmount;
        public float MergePunchDuration => mergePunchDuration;
        public int MergePunchVibrato => mergePunchVibrato;

        public float HoverFlashAmount => hoverFlashAmount;
        public float PickupFlashAmount => pickupFlashAmount;
        public float MergeFlashAmount => mergeFlashAmount;
        public float DamageFlashAmount => damageFlashAmount;
        public float FlashReturnDuration => flashReturnDuration;

        public float HoverBrightnessBoost => hoverBrightnessBoost;
        public float DragBrightnessBoost => dragBrightnessBoost;
        public float HoverSaturationBoost => hoverSaturationBoost;
        public float IdleHueShiftAmount => idleHueShiftAmount;
        public float IdleHueShiftFrequency => idleHueShiftFrequency;

        public Color GlowColor => glowColor;
        public float HoverGlowIntensity => hoverGlowIntensity;
        public float DragGlowIntensity => dragGlowIntensity;

        public float DamageShakeAmount => damageShakeAmount;
        public float DamageShakeDuration => damageShakeDuration;

        public float SnapDuration => snapDuration;
        public Ease SnapEase => snapEase;
        public float SnapOvershoot => snapOvershoot;

        private void OnValidate()
        {
            hoverScale = Mathf.Max(0.01f, hoverScale);
            hoverScaleDuration = Mathf.Max(0.02f, hoverScaleDuration);

            pickupPunchDuration = Mathf.Max(0.04f, pickupPunchDuration);
            dragHoldScale = Mathf.Max(0.01f, dragHoldScale);

            dropSquishDuration = Mathf.Max(0.03f, dropSquishDuration);
            dropSettleDuration = Mathf.Max(0.05f, dropSettleDuration);
            dropSettleOvershoot = Mathf.Clamp(dropSettleOvershoot, 0f, 0.75f);

            spawnStartScale = Mathf.Clamp(spawnStartScale, 0.5f, 1f);
            spawnDuration = Mathf.Max(0.05f, spawnDuration);
            spawnOvershoot = Mathf.Clamp(spawnOvershoot, 0f, 0.75f);

            mergePunchDuration = Mathf.Max(0.05f, mergePunchDuration);
            flashReturnDuration = Mathf.Max(0.03f, flashReturnDuration);

            idleHueShiftAmount = Mathf.Clamp(idleHueShiftAmount, 0f, 0.02f);

            damageShakeDuration = Mathf.Max(0.05f, damageShakeDuration);
            snapDuration = Mathf.Max(0.01f, snapDuration);
            snapOvershoot = Mathf.Clamp(snapOvershoot, 0f, 0.5f);
        }
    }
}
