// CardRenderOrderController — Single authoritative source for card sorting order.
//
// Architecture
// ────────────
// sortingOrder = StackGroupBase
//              + (MaxStackDepth − stackIndex) × ChildSlots   ← TopCard (index 0) = highest
//              + statePriority                                ← Dragged > Merging > Hovered > Normal
//              + childLocalOffset                             ← per-renderer layer within the card
//
// State priorities (leave gaps so future states fit without renumbering):
//   Normal  =    0
//   Hovered =  500
//   Dragged = 2000
//   Merging = 3000
//
// Child offsets (resolved in priority order):
//   1. CardRenderLayer.localOrderOffset  (explicit component on the renderer's GameObject)
//   2. Name heuristic  (title/stats/art/frame/overlay keywords)
//   3. Safe default = 1  (keeps unknown children above the root body)
//
// Lifecycle
// ─────────
// • CacheRenderers() is called in Start() and on every Refresh() call.
// • WriteOrders() is called every Update() only when state or stack-index changed
//   (dirty-checked, no per-frame allocations).
// • CardFeelPresenter calls SetHovered() / SetMerging() directly.
// • CardInstance.SetHighlighted() calls Refresh() after creating the Highlight child.

using UnityEngine;

namespace Markyu.LastKernel
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CardInstance))]
    public sealed class CardRenderOrderController : MonoBehaviour
    {
        // ── Constants ─────────────────────────────────────────────────────────────

        /// <summary>Starting sort order for all cards.</summary>
        private const int StackGroupBase = 100;

        /// <summary>Maximum stack depth handled without overflow.</summary>
        private const int MaxStackDepth = 20;

        /// <summary>
        /// Sorting-order slots reserved per card position.
        /// Must be > the highest child offset (currently 4) with headroom.
        /// </summary>
        public const int ChildSlots = 10;

        private const int NormalPriority  =    0;
        private const int HoveredPriority =  500;
        private const int DraggedPriority = 2000;
        private const int MergingPriority = 3000;

        // ── Visual State ──────────────────────────────────────────────────────────

        public enum VisualState { Normal, Hovered, Dragged, Merging }

        // Separate boolean flags so hover and merging compose cleanly.
        private bool _isHovered;
        private bool _isMerging;

        // Drag is derived from CardInstance.IsBeingDragged each Update — never stored.
        private VisualState ActiveState
        {
            get
            {
                if (_card != null && _card.IsBeingDragged) return VisualState.Dragged;
                if (_isMerging) return VisualState.Merging;
                if (_isHovered) return VisualState.Hovered;
                return VisualState.Normal;
            }
        }

        // ── Dirty Tracking ────────────────────────────────────────────────────────

        private VisualState _lastState      = (VisualState)(-1);
        private int         _lastStackIndex = -1;

        // ── References ────────────────────────────────────────────────────────────

        private CardInstance _card;

        private struct RendererEntry
        {
            public Renderer renderer;
            public int      localOffset;
        }

        private RendererEntry[] _entries;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            _card = GetComponent<CardInstance>();
        }

        private void Start()
        {
            CacheRenderers();
            ForceWrite();
        }

        private void Update()
        {
            if (_card == null) return;

            var state = ActiveState;
            int idx   = StackIndex();

            if (state == _lastState && idx == _lastStackIndex) return;

            WriteOrders(state, idx);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Called by CardFeelPresenter on pointer enter/exit.</summary>
        public void SetHovered(bool on)
        {
            if (_isHovered == on) return;
            _isHovered = on;
            ForceWrite();
        }

        /// <summary>
        /// Called by CardFeelPresenter.OnMergeReceived / merge-complete callback.
        /// Briefly raises the receiving card above everything while the punch plays.
        /// </summary>
        public void SetMerging(bool on)
        {
            if (_isMerging == on) return;
            _isMerging = on;
            ForceWrite();
        }

        /// <summary>
        /// Re-scans all child renderers and immediately reapplies sort orders.
        /// Call after adding/removing child renderers at runtime (e.g. Highlight).
        /// </summary>
        public void Refresh()
        {
            CacheRenderers();
            ForceWrite();
        }

        // ── Internal Helpers ──────────────────────────────────────────────────────

        private void ForceWrite()
        {
            _lastState      = (VisualState)(-1); // invalidate so WriteOrders always runs
            if (_entries != null)
                WriteOrders(ActiveState, StackIndex());
        }

        private int StackIndex()
        {
            if (_card?.Stack == null) return 0;
            int i = _card.Stack.Cards.IndexOf(_card);
            return i < 0 ? 0 : i;
        }

        private void WriteOrders(VisualState state, int stackIndex)
        {
            if (_entries == null || _entries.Length == 0) return;

            _lastState      = state;
            _lastStackIndex = stackIndex;

            int priority = state switch
            {
                VisualState.Hovered => HoveredPriority,
                VisualState.Dragged => DraggedPriority,
                VisualState.Merging => MergingPriority,
                _                   => NormalPriority
            };

            // TopCard (index 0) must render above all cards deeper in the stack.
            int stackContrib = (MaxStackDepth - Mathf.Clamp(stackIndex, 0, MaxStackDepth)) * ChildSlots;
            int baseOrder    = StackGroupBase + stackContrib + priority;

            for (int i = 0; i < _entries.Length; i++)
            {
                if (_entries[i].renderer != null)
                    _entries[i].renderer.sortingOrder = baseOrder + _entries[i].localOffset;
            }
        }

        private void CacheRenderers()
        {
            var raw = GetComponentsInChildren<Renderer>(includeInactive: true);
            _entries = new RendererEntry[raw.Length];

            for (int i = 0; i < raw.Length; i++)
            {
                _entries[i] = new RendererEntry
                {
                    renderer    = raw[i],
                    localOffset = ResolveLocalOffset(raw[i])
                };
            }
        }

        /// <summary>
        /// Resolves the child layer offset for a renderer.
        /// Priority: explicit CardRenderLayer component → name heuristic → safe default.
        /// </summary>
        private int ResolveLocalOffset(Renderer r)
        {
            // 1. Explicit component wins unconditionally.
            var layer = r.GetComponent<CardRenderLayer>();
            if (layer != null) return layer.localOrderOffset;

            // 2. Root body renderer — always the base layer.
            if (r.gameObject == gameObject) return 0;

            // 3. Name-based heuristics for common card child objects.
            string n = r.gameObject.name.ToLowerInvariant();

            if (n.Contains("art")    || n.Contains("icon") || n.Contains("artwork"))
                return 1;

            if (n.Contains("frame")  || n.Contains("border") || n.Contains("card_frame"))
                return 2;

            if (n.Contains("title")       || n.Contains("name")     || n.Contains("label")  ||
                n.Contains("description") || n.Contains("desc")      || n.Contains("price")  ||
                n.Contains("nutrition")   || n.Contains("health")    || n.Contains("stat"))
                return 3;

            if (n.Contains("overlay")   || n.Contains("highlight") || n.Contains("selection") ||
                n.Contains("outline")   || n.Contains("glow")      || n.Contains("vfx"))
                return 4;

            // 4. Unknown child: render just above the root body.
            return 1;
        }
    }
}
