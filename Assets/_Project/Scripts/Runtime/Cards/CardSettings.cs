using Sirenix.OdinInspector;
using UnityEngine;
using DG.Tweening;

namespace Markyu.LastKernel
{
    [CreateAssetMenu(menuName = "Last Kernel/Card Settings")]
    public class CardSettings : ScriptableObject
    {
        // ── Economy ───────────────────────────────────────────────────────────

        [BoxGroup("Economy")]
        [SerializeField, Min(1), Tooltip("Starting card limit for the player.")]
        private int baseCardLimit = 24;

        [BoxGroup("Economy")]
        [SerializeField, Min(1), Tooltip("Nutrition each Character card requires per feeding cycle.")]
        private int hungerPerCharacter = 2;

        // ── Input & Interaction ───────────────────────────────────────────────

        [BoxGroup("Input")]
        [SerializeField, Min(0f), Tooltip("Mouse travel distance before a click becomes a drag.")]
        private float clickThreshold = 0.02f;

        [BoxGroup("Input")]
        [SerializeField, Min(0f), Tooltip("World Y the card lifts to when dragged.")]
        private float dragHeight = 0.1f;

        // ── Physics & Stacking ────────────────────────────────────────────────

        [BoxGroup("Physics")]
        [SerializeField, Min(0f), Tooltip("Drop radius to attach to another stack.")]
        private float attachRadius = 0.25f;

        [BoxGroup("Physics")]
        [SerializeField, Min(0f), Tooltip("Wider auto-attach radius used at spawn.")]
        private float spawnAttachRadius = 1f;

        [BoxGroup("Physics")]
        [SerializeField, Tooltip("Extra space added to the collider used for layout and stacking.")]
        private Vector2 margin = new Vector2(0.1f, 0.1f);

        [BoxGroup("Physics")]
        [SerializeField, Range(1, 20), Tooltip("Max solver iterations per frame.")]
        private int maxIterations = 8;

        // ── Visual Layout & Animation ─────────────────────────────────────────

        [BoxGroup("Animation")]
        [SerializeField, Tooltip("Per-card visual offset within a stack (Z is depth-bias for render order).")]
        private Vector3 stackStep = new Vector3(0f, 0.002f, -0.18f);

        [BoxGroup("Animation")]
        [SerializeField, Min(0.01f), Tooltip("Duration for standard card movement tweens.")]
        private float moveDuration = 0.1f;

        [BoxGroup("Animation")]
        [SerializeField]
        private Ease moveEase = Ease.OutQuad;

        [BoxGroup("Animation")]
        [SerializeField, Range(10f, 300f), Tooltip("Exponential follow sharpness for trailing drag cards.")]
        private float swaySharpness = 100f;

        // ── Feel ──────────────────────────────────────────────────────────────

        [BoxGroup("Feel")]
        [SerializeField, Required, InlineEditor(InlineEditorObjectFieldModes.Foldout)]
        [Tooltip("Hover, drag, snap, spawn, merge tunables. Create via Right-click > Last Kernel > Card Feel Profile.")]
        private CardFeelProfile feelProfile;

        // ── Visual Effects ────────────────────────────────────────────────────

        [BoxGroup("Visual Effects")]
        [SerializeField, Required]
        private PuffParticle puffParticle;

        [BoxGroup("Visual Effects")]
        [SerializeField, Required]
        [Tooltip("Material used by the highlight system for the card outline.")]
        private Material outlineMaterial;

        // ── Mob AI ────────────────────────────────────────────────────────────

        [BoxGroup("Mob AI")]
        [SerializeField, Min(0f), Tooltip("Seconds between each mob's automatic move attempt.")]
        private float moveInterval = 5f;

        [BoxGroup("Mob AI")]
        [SerializeField, Min(0f), Tooltip("Max distance a mob travels per random move.")]
        private float moveRadius = 1f;

        [BoxGroup("Mob AI")]
        [SerializeField, Range(1, 20), Tooltip("Max attempts to find a valid position per move.")]
        private int maxAttemptsPerMove = 5;

        // ── Properties ────────────────────────────────────────────────────────

        public int BaseCardLimit => baseCardLimit;
        public int HungerPerCharacter => hungerPerCharacter;

        public float ClickThreshold => clickThreshold;
        public float DragHeight => dragHeight;

        public float AttachRadius => attachRadius;
        public float SpawnAttachRadius => spawnAttachRadius;
        public Vector2 Margin => margin;
        public int MaxIterations => maxIterations;

        public Vector3 StackStep => stackStep;
        public float MoveDuration => moveDuration;
        public Ease MoveEase => moveEase;
        public float SwaySharpness => swaySharpness;

        public CardFeelProfile FeelProfile => feelProfile;

        public PuffParticle PuffParticle => puffParticle;
        public Material OutlineMaterial => outlineMaterial;

        public float MoveInterval => moveInterval;
        public float MoveRadius => moveRadius;
        public int MaxAttemptsPerMove => maxAttemptsPerMove;
    }
}
