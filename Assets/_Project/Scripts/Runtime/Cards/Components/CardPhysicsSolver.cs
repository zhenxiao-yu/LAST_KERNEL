using System.Collections.Generic;
using UnityEngine;

namespace Markyu.LastKernel
{
    public static class CardPhysicsSolver
    {
        /// <summary>
        /// Resolves planar stack placement either by snapping stacks onto the board grid
        /// or, when grid snapping is disabled, by using the original overlap-separation solver.
        /// </summary>
        /// <param name="stacks">A list of all active CardStacks to be checked for overlap.</param>
        /// <param name="combatRects">A collection of active CombatRects that stacks must be separated from.</param>
        /// <param name="maxIterations">Unused by grid mode, but preserved for the legacy solver path.</param>
        public static void ResolveOverlaps(IList<CardStack> stacks, IEnumerable<CombatRect> combatRects, int maxIterations)
        {
            if (Board.Instance != null && Board.Instance.SnapCardsToGrid)
            {
                ResolveToGrid(stacks, combatRects);
                return;
            }

            int iter = 0;
            bool anyOverlap = true;

            var activeCombatRects = combatRects ?? new List<CombatRect>();

            while (iter < maxIterations && anyOverlap)
            {
                anyOverlap = false;
                iter++;

                // 1. Separate Stacks from Combat Rects
                foreach (var combatRect in activeCombatRects)
                {
                    foreach (var stack in stacks)
                    {
                        if (stack.IsLocked) continue;

                        if (CheckAndSeparate(stack, combatRect))
                        {
                            anyOverlap = true;
                        }
                    }
                }

                // 2. Separate Stacks from Each Other (Pairwise)
                for (int i = 0; i < stacks.Count; i++)
                {
                    var stackA = stacks[i];

                    for (int j = i + 1; j < stacks.Count; j++)
                    {
                        var stackB = stacks[j];

                        if (CheckAndSeparate(stackA, stackB))
                        {
                            anyOverlap = true;
                        }
                    }
                }
            }
        }

        private static void ResolveToGrid(IList<CardStack> stacks, IEnumerable<CombatRect> combatRects)
        {
            if (Board.Instance == null || stacks == null)
            {
                return;
            }

            var board = Board.Instance;
            var blockedRects = new List<Rect>();

            if (combatRects != null)
            {
                foreach (var combatRect in combatRects)
                {
                    if (combatRect != null)
                    {
                        blockedRects.Add(board.GetCombatRectWorldRect(combatRect));
                    }
                }
            }

            var occupiedRects = new List<Rect>();
            var orderedStacks = new List<StackEntry>(stacks.Count);

            for (int i = 0; i < stacks.Count; i++)
            {
                var stack = stacks[i];
                if (stack == null || stack.TopCard == null)
                {
                    continue;
                }

                orderedStacks.Add(new StackEntry(stack, i));
            }

            orderedStacks.Sort((a, b) =>
            {
                int lockedCompare = b.Stack.IsLocked.CompareTo(a.Stack.IsLocked);
                return lockedCompare != 0 ? lockedCompare : a.Index.CompareTo(b.Index);
            });

            foreach (var entry in orderedStacks)
            {
                var stack = entry.Stack;
                Vector3 desiredPosition = board.EnforcePlacementRules(stack.TargetPosition, stack);

                if (!board.TryFindNearestGridPosition(
                        desiredPosition,
                        stack,
                        occupiedRects,
                        blockedRects,
                        out var resolvedPosition))
                {
                    resolvedPosition = desiredPosition;
                }

                occupiedRects.Add(board.GetStackRect(resolvedPosition, stack));

                bool instant = Vector3.SqrMagnitude(stack.TargetPosition - resolvedPosition) < 0.0001f;
                stack.SetTargetPosition(resolvedPosition, instant);
            }
        }

        private static bool CheckAndSeparate(CardStack a, CardStack b)
        {
            if (a == b || a == null || b == null) return false;

            var cardA = a.TopCard;
            var cardB = b.TopCard;

            if (cardA == null || cardB == null) return false;

            // Get Bounds
            GetStackBounds(a, out Vector2 aPos, out Vector2 aHalf);
            GetStackBounds(b, out Vector2 bPos, out Vector2 bHalf);

            // Calculate Overlap
            if (!CalculateSeparationVector(aPos, aHalf, bPos, bHalf, out Vector3 separation))
                return false;

            // Apply Separation based on Locks
            bool aLocked = a.IsLocked;
            bool bLocked = b.IsLocked;

            if (aLocked && bLocked) return false;

            if (aLocked)
            {
                b.ApplyTranslation(-separation);
            }
            else if (bLocked)
            {
                a.ApplyTranslation(separation);
            }
            else
            {
                a.ApplyTranslation(separation * 0.5f);
                b.ApplyTranslation(-separation * 0.5f);
            }

            return true;
        }

        private static bool CheckAndSeparate(CardStack stack, CombatRect combatRect)
        {
            if (stack == null || combatRect == null) return false;

            // Get Stack Bounds
            GetStackBounds(stack, out Vector2 stackPos, out Vector2 stackHalf);

            // Get Rect Bounds
            var rectTransform = combatRect.Rect;
            Vector3 worldCenter = rectTransform.TransformPoint(rectTransform.rect.center);
            Vector2 worldSize = Vector2.Scale(rectTransform.rect.size, rectTransform.lossyScale);
            Vector2 rectPos = new Vector2(worldCenter.x, worldCenter.z);
            Vector2 rectHalf = worldSize * 0.5f;

            // Calculate Overlap
            if (!CalculateSeparationVector(stackPos, stackHalf, rectPos, rectHalf, out Vector3 separation))
                return false;

            // Only move the stack
            stack.ApplyTranslation(separation);
            return true;
        }

        private static void GetStackBounds(CardStack stack, out Vector2 pos, out Vector2 halfSize)
        {
            float topZ = stack.TargetPosition.z;
            float bottomZ = stack.TargetPosition.z + (stack.Cards.Count - 1) * stack.TopCard.Settings.StackStep.z;
            float centerZ = (topZ + bottomZ) / 2f;

            pos = new Vector2(stack.TargetPosition.x, centerZ);
            halfSize = new Vector2(stack.Width * 0.5f, stack.FullDepth * 0.5f);
        }

        private static bool CalculateSeparationVector(Vector2 posA, Vector2 halfA, Vector2 posB, Vector2 halfB, out Vector3 separation)
        {
            separation = Vector3.zero;

            float dx = posA.x - posB.x;
            float px = (halfA.x + halfB.x) - Mathf.Abs(dx);
            if (px <= 0) return false;

            float dz = posA.y - posB.y;
            float pz = (halfA.y + halfB.y) - Mathf.Abs(dz);
            if (pz <= 0) return false;

            if (px < pz)
            {
                // Mathf.Sign(0) returns 0, producing a zero-length separation for perfectly
                // coincident stacks. Fall back to +X so two stacks never stay locked together.
                float sign = dx != 0f ? Mathf.Sign(dx) : 1f;
                separation = new Vector3(px * sign, 0f, 0f);
            }
            else
            {
                float sign = dz != 0f ? Mathf.Sign(dz) : 1f;
                separation = new Vector3(0f, 0f, pz * sign);
            }

            return true;
        }

        private readonly struct StackEntry
        {
            public CardStack Stack { get; }
            public int Index { get; }

            public StackEntry(CardStack stack, int index)
            {
                Stack = stack;
                Index = index;
            }
        }
    }
}

