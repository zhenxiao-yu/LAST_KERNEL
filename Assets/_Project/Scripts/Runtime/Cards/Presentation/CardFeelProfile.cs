using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Top-level feel profile for a card. References four focused tween preset assets
    /// (Hover, Drag, Drop, Damage) and exposes shader globals and snap settings directly.
    /// All public properties are preserved so CardFeelPresenter needs no changes.
    /// </summary>
    [CreateAssetMenu(menuName = "Last Kernel/Card Feel Profile", fileName = "CardFeelProfile")]
    public class CardFeelProfile : ScriptableObject
    {
        // ── Presets ───────────────────────────────────────────────────────────

        [BoxGroup("Presets")]
        [Required, InlineEditor]
        [SerializeField] private CardHoverPreset hover;

        [BoxGroup("Presets")]
        [Required, InlineEditor]
        [SerializeField] private CardDragPreset drag;

        [BoxGroup("Presets")]
        [Required, InlineEditor]
        [SerializeField] private CardDropPreset drop;

        [BoxGroup("Presets")]
        [Required, InlineEditor]
        [SerializeField] private DamagePreset damage;

        // ── Shader Globals ────────────────────────────────────────────────────

        [BoxGroup("Shader Globals")]
        [SerializeField, Range(0f, 0.02f), Tooltip("Keep 0 for stable pixel-art colors.")]
        private float idleHueShiftAmount = 0f;

        [BoxGroup("Shader Globals")]
        [SerializeField, Range(0.1f, 3f)]
        private float idleHueShiftFrequency = 0.45f;

        [BoxGroup("Shader Globals")]
        [SerializeField]
        private Color glowColor = new Color(0.52f, 0.85f, 1f, 1f);

        // ── Snap ──────────────────────────────────────────────────────────────

        [BoxGroup("Snap")]
        [SerializeField, Min(0.01f)]
        private float snapDuration = 0.10f;

        [BoxGroup("Snap")]
        [SerializeField]
        private Ease snapEase = Ease.OutQuad;

        [BoxGroup("Snap")]
        [SerializeField, Range(0f, 0.5f)]
        private float snapOvershoot = 0f;

        // ── Hover ─────────────────────────────────────────────────────────────

        public float HoverScale           => hover ? hover.Scale           : 1.035f;
        public float HoverScaleDuration   => hover ? hover.ScaleDuration   : 0.075f;
        public Ease  HoverScaleEase       => hover ? hover.ScaleEase       : Ease.OutQuad;
        public float HoverFlashAmount     => hover ? hover.FlashAmount     : 0f;
        public float HoverBrightnessBoost => hover ? hover.BrightnessBoost : 0f;
        public float HoverSaturationBoost => hover ? hover.SaturationBoost : 0f;
        public float HoverGlowIntensity   => hover ? hover.GlowIntensity   : 0f;

        // ── Drag / Pickup ─────────────────────────────────────────────────────

        public float PickupPunchAmount   => drag ? drag.PunchAmount     : 0.075f;
        public float PickupPunchDuration => drag ? drag.PunchDuration   : 0.10f;
        public int   PickupPunchVibrato  => drag ? drag.PunchVibrato    : 4;
        public float DragHoldScale       => drag ? drag.HoldScale       : 1.035f;
        public float PickupFlashAmount   => drag ? drag.FlashAmount     : 0f;
        public float DragBrightnessBoost => drag ? drag.BrightnessBoost : 0f;
        public float DragGlowIntensity   => drag ? drag.GlowIntensity   : 0f;

        // ── Drop / Spawn / Merge ──────────────────────────────────────────────

        public float DropSquishScale      => drop ? drop.SquishScale        : 0.965f;
        public float DropSquishDuration   => drop ? drop.SquishDuration     : 0.045f;
        public float DropSettleDuration   => drop ? drop.SettleDuration     : 0.09f;
        public Ease  DropSettleEase       => drop ? drop.SettleEase         : Ease.OutQuad;
        public float DropSettleOvershoot  => drop ? drop.SettleOvershoot    : 0f;
        public float SpawnStartScale      => drop ? drop.SpawnStartScale    : 0.85f;
        public float SpawnDuration        => drop ? drop.SpawnDuration      : 0.12f;
        public Ease  SpawnEase            => drop ? drop.SpawnEase          : Ease.OutBack;
        public float SpawnOvershoot       => drop ? drop.SpawnOvershoot     : 0.45f;
        public float MergePunchAmount     => drop ? drop.MergePunchAmount   : 0.055f;
        public float MergePunchDuration   => drop ? drop.MergePunchDuration : 0.12f;
        public int   MergePunchVibrato    => drop ? drop.MergePunchVibrato  : 4;
        public float MergeFlashAmount     => drop ? drop.MergeFlashAmount   : 0f;

        // ── Damage ────────────────────────────────────────────────────────────

        public float DamageShakeAmount   => damage ? damage.ShakeAmount         : 0.06f;
        public float DamageShakeDuration => damage ? damage.ShakeDuration       : 0.20f;
        public float DamageFlashAmount   => damage ? damage.FlashAmount         : 0f;
        public float FlashReturnDuration => damage ? damage.FlashReturnDuration : 0.13f;

        // ── Shader Globals ────────────────────────────────────────────────────

        public float IdleHueShiftAmount    => idleHueShiftAmount;
        public float IdleHueShiftFrequency => idleHueShiftFrequency;
        public Color GlowColor             => glowColor;

        // ── Snap ──────────────────────────────────────────────────────────────

        public float SnapDuration  => snapDuration;
        public Ease  SnapEase      => snapEase;
        public float SnapOvershoot => snapOvershoot;

        private void OnValidate()
        {
            idleHueShiftAmount    = Mathf.Clamp(idleHueShiftAmount, 0f, 0.02f);
            idleHueShiftFrequency = Mathf.Max(0.1f, idleHueShiftFrequency);
            snapDuration          = Mathf.Max(0.01f, snapDuration);
            snapOvershoot         = Mathf.Clamp(snapOvershoot, 0f, 0.5f);
        }
    }
}
