using UnityEngine;
using DG.Tweening;

namespace Markyu.FortStack
{
    /// <summary>
    /// Centralised tunable settings for all card interaction feel.
    /// Assign in CardSettings.FeelProfile and tune per-project without touching code.
    ///
    /// Design intent — Last Kernel aesthetic:
    ///   Sharp, mechanical, controlled. Short tweens. Subtle settle.
    ///   Nothing bubbly. Nothing casino. Industrial tension.
    /// </summary>
    [CreateAssetMenu(menuName = "Last Kernel/Card Feel Profile", fileName = "CardFeelProfile")]
    public class CardFeelProfile : ScriptableObject
    {
        // ── Hover ─────────────────────────────────────────────────────────────────

        [Header("Hover")]
        [SerializeField, Tooltip("Uniform scale when cursor is over the card. 1.04 = 4% larger.")]
        private float hoverScale = 1.04f;

        [SerializeField, Min(0.02f)]
        private float hoverScaleDuration = 0.1f;

        [SerializeField]
        private Ease hoverScaleEase = Ease.OutQuad;

        // ── Hover rotation punch ──────────────────────────────────────────────────

        [Header("Hover Rotation Punch")]
        [SerializeField, Tooltip("Yaw snap angle when cursor enters the card. 0 disables.")]
        private float hoverPunchAngle = 5f;

        [SerializeField, Min(0.1f), Tooltip("Total duration of the yaw punch oscillation.")]
        private float hoverPunchDuration = 0.25f;

        // ── Pickup ────────────────────────────────────────────────────────────────

        [Header("Pickup")]
        [SerializeField, Tooltip("Punch magnitude on grab. 0.1 = 10% scale spike then settle.")]
        private float pickupPunchAmount = 0.1f;

        [SerializeField, Min(0.04f)]
        private float pickupPunchDuration = 0.13f;

        [SerializeField, Range(1, 20)]
        private int pickupPunchVibrato = 6;

        [SerializeField, Tooltip("Scale held while actively dragging. Slightly above 1 to suggest lift.")]
        private float dragHoldScale = 1.03f;

        // ── Drag tilt ─────────────────────────────────────────────────────────────

        [Header("Drag Tilt")]
        [SerializeField, Tooltip("Max tilt angle in degrees. 8 is readable; avoid >15 on pixel art.")]
        private float dragTiltMax = 7f;

        [SerializeField, Tooltip("Scales velocity into tilt. Tune until tilt feels proportional to drag speed.")]
        private float dragTiltStrength = 2.5f;

        [SerializeField, Tooltip("How quickly tilt follows velocity and returns to zero.")]
        private float dragTiltSmoothing = 10f;

        // ── Mouse tilt ────────────────────────────────────────────────────────────

        [Header("Mouse Tilt")]
        [SerializeField, Tooltip("Card leans toward the cursor when hovered. Disable to use only velocity tilt.")]
        private bool mouseTiltEnabled = true;

        [SerializeField, Range(0f, 40f), Tooltip("How strongly the card leans toward the cursor. ~18 works for top-down 3D.")]
        private float mouseTiltAmount = 18f;

        [SerializeField, Tooltip("How quickly the card tracks the cursor direction. Higher = snappier.")]
        private float mouseTiltSmoothing = 20f;

        // ── Idle auto-tilt ────────────────────────────────────────────────────────

        [Header("Idle Auto-Tilt")]
        [SerializeField, Tooltip("Sine/cosine breathing motion when card is at rest. Each card uses a different phase so they don't bob in sync.")]
        private bool autoTiltEnabled = true;

        [SerializeField, Range(0f, 5f), Tooltip("Max idle tilt in degrees. Keep 1-2 for top-down cards; large values look seasick.")]
        private float autoTiltAmount = 1.2f;

        [SerializeField, Range(0.1f, 3f), Tooltip("Speed of the oscillation cycle in Hz.")]
        private float autoTiltFrequency = 0.8f;

        // ── Drop / settle ─────────────────────────────────────────────────────────

        [Header("Drop / Settle")]
        [SerializeField, Tooltip("Brief squish scale on card release. 0.94 = quick 6% squeeze before bounce.")]
        private float dropSquishScale = 0.94f;

        [SerializeField, Min(0.03f)]
        private float dropSquishDuration = 0.065f;

        [SerializeField, Min(0.05f), Tooltip("Duration of the bounce-back to rest scale after the squish.")]
        private float dropSettleDuration = 0.11f;

        [SerializeField]
        private Ease dropSettleEase = Ease.OutBack;

        [SerializeField, Range(0f, 2f), Tooltip("Overshoot amount for OutBack settle. 0.6 is subtle.")]
        private float dropSettleOvershoot = 0.65f;

        // ── Spawn ─────────────────────────────────────────────────────────────────

        [Header("Spawn")]
        [SerializeField, Tooltip("Scale cards start at when they appear. 0.0 = pop in from nothing.")]
        private float spawnStartScale = 0f;

        [SerializeField, Min(0.05f)]
        private float spawnDuration = 0.17f;

        [SerializeField]
        private Ease spawnEase = Ease.OutBack;

        [SerializeField, Range(0f, 2.5f), Tooltip("OutBack overshoot on spawn pop.")]
        private float spawnOvershoot = 1.05f;

        // ── Merge / stack accept ──────────────────────────────────────────────────

        [Header("Merge / Stack Accept")]
        [SerializeField, Tooltip("Punch magnitude on the receiving stack when a card is added.")]
        private float mergePunchAmount = 0.08f;

        [SerializeField, Min(0.05f)]
        private float mergePunchDuration = 0.17f;

        [SerializeField, Range(1, 20)]
        private int mergePunchVibrato = 5;

        // ── Shader / material feedback ───────────────────────────────────────────

        [Header("Shader Feedback")]
        [SerializeField, Range(0f, 1f), Tooltip("Temporary flash added when the cursor enters a card.")]
        private float hoverFlashAmount = 0.08f;

        [SerializeField, Range(0f, 1f), Tooltip("Temporary flash added when a card is picked up.")]
        private float pickupFlashAmount = 0.16f;

        [SerializeField, Range(0f, 1f), Tooltip("Temporary flash added to a stack when it accepts another card.")]
        private float mergeFlashAmount = 0.18f;

        [SerializeField, Range(0f, 1f), Tooltip("Temporary flash added when a card takes damage.")]
        private float damageFlashAmount = 0.65f;

        [SerializeField, Min(0.03f)]
        private float flashReturnDuration = 0.18f;

        [SerializeField, Range(0f, 0.25f), Tooltip("Moves the card overlay texture opposite the tilt to create a small parallax shimmer.")]
        private float overlayParallaxAmount = 0.055f;

        [SerializeField, Range(0f, 0.1f), Tooltip("Subtle idle overlay drift. Keep low so card art remains readable.")]
        private float idleOverlayDriftAmount = 0.012f;

        [SerializeField, Range(0.1f, 3f)]
        private float idleOverlayDriftFrequency = 0.7f;

        [SerializeField, Range(0f, 0.4f)]
        private float hoverBrightnessBoost = 0.08f;

        [SerializeField, Range(0f, 0.4f)]
        private float dragBrightnessBoost = 0.14f;

        [SerializeField, Range(0f, 0.4f)]
        private float hoverSaturationBoost = 0.05f;

        [SerializeField, Range(0f, 0.15f), Tooltip("Tiny color drift used while idle so cards do not look perfectly static.")]
        private float idleHueShiftAmount = 0.01f;

        [SerializeField, Range(0.1f, 3f)]
        private float idleHueShiftFrequency = 0.45f;

        [SerializeField, Tooltip("Emission color used by card shaders that expose _EmissionColor.")]
        private Color glowColor = new Color(0.52f, 0.85f, 1f, 1f);

        [SerializeField, Range(0f, 1f)]
        private float hoverGlowIntensity = 0.12f;

        [SerializeField, Range(0f, 1f)]
        private float dragGlowIntensity = 0.18f;

        // ── Damage feedback ──────────────────────────────────────────────────────

        [Header("Damage Feedback")]
        [SerializeField, Range(0f, 30f)]
        private float damagePunchAngle = 12f;

        [SerializeField, Min(0.05f)]
        private float damagePunchDuration = 0.2f;

        // ── Movement snap ─────────────────────────────────────────────────────────

        [Header("Movement Snap")]
        [SerializeField, Tooltip("Duration for post-drop grid snap animation.")]
        private float snapDuration = 0.13f;

        [SerializeField]
        private Ease snapEase = Ease.OutBack;

        [SerializeField, Range(0f, 2f)]
        private float snapOvershoot = 0.55f;

        // ── Public accessors ──────────────────────────────────────────────────────

        public float HoverScale => hoverScale;
        public float HoverScaleDuration => hoverScaleDuration;
        public Ease HoverScaleEase => hoverScaleEase;

        public float HoverPunchAngle => hoverPunchAngle;
        public float HoverPunchDuration => hoverPunchDuration;

        public float PickupPunchAmount => pickupPunchAmount;
        public float PickupPunchDuration => pickupPunchDuration;
        public int PickupPunchVibrato => pickupPunchVibrato;
        public float DragHoldScale => dragHoldScale;

        public float DragTiltMax => dragTiltMax;
        public float DragTiltStrength => dragTiltStrength;
        public float DragTiltSmoothing => dragTiltSmoothing;

        public bool MouseTiltEnabled => mouseTiltEnabled;
        public float MouseTiltAmount => mouseTiltAmount;
        public float MouseTiltSmoothing => mouseTiltSmoothing;

        public bool AutoTiltEnabled => autoTiltEnabled;
        public float AutoTiltAmount => autoTiltAmount;
        public float AutoTiltFrequency => autoTiltFrequency;

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
        public float OverlayParallaxAmount => overlayParallaxAmount;
        public float IdleOverlayDriftAmount => idleOverlayDriftAmount;
        public float IdleOverlayDriftFrequency => idleOverlayDriftFrequency;
        public float HoverBrightnessBoost => hoverBrightnessBoost;
        public float DragBrightnessBoost => dragBrightnessBoost;
        public float HoverSaturationBoost => hoverSaturationBoost;
        public float IdleHueShiftAmount => idleHueShiftAmount;
        public float IdleHueShiftFrequency => idleHueShiftFrequency;
        public Color GlowColor => glowColor;
        public float HoverGlowIntensity => hoverGlowIntensity;
        public float DragGlowIntensity => dragGlowIntensity;
        public float DamagePunchAngle => damagePunchAngle;
        public float DamagePunchDuration => damagePunchDuration;

        public float SnapDuration => snapDuration;
        public Ease SnapEase => snapEase;
        public float SnapOvershoot => snapOvershoot;

        private void OnValidate()
        {
            hoverScale = Mathf.Max(0.01f, hoverScale);
            hoverScaleDuration = Mathf.Max(0.02f, hoverScaleDuration);
            hoverPunchDuration = Mathf.Max(0.05f, hoverPunchDuration);
            pickupPunchDuration = Mathf.Max(0.04f, pickupPunchDuration);
            dragTiltMax = Mathf.Max(0f, dragTiltMax);
            dragTiltSmoothing = Mathf.Max(0.01f, dragTiltSmoothing);
            mouseTiltSmoothing = Mathf.Max(0.01f, mouseTiltSmoothing);
            autoTiltFrequency = Mathf.Max(0.01f, autoTiltFrequency);
            dropSquishDuration = Mathf.Max(0.03f, dropSquishDuration);
            dropSettleDuration = Mathf.Max(0.05f, dropSettleDuration);
            spawnDuration = Mathf.Max(0.05f, spawnDuration);
            mergePunchDuration = Mathf.Max(0.05f, mergePunchDuration);
            flashReturnDuration = Mathf.Max(0.03f, flashReturnDuration);
            damagePunchDuration = Mathf.Max(0.05f, damagePunchDuration);
            snapDuration = Mathf.Max(0.01f, snapDuration);
        }
    }
}
