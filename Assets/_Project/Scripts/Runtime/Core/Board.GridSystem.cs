using System.Collections.Generic;
using UnityEngine;

namespace Markyu.LastKernel
{
    public partial class Board
    {
        // ── Public grid position API ──────────────────────────────────────────────

        public Vector3 SnapToNearestGridPosition(Vector3 desiredPosition, CardStack stack)
        {
            if (TryFindNearestGridPosition(desiredPosition, stack, null, null, out var snappedPosition))
            {
                return snappedPosition;
            }

            return EnforcePlacementRules(desiredPosition, stack);
        }

        public bool TryFindNearestGridPosition(
            Vector3 desiredPosition,
            CardStack stack,
            IEnumerable<Rect> occupiedRects,
            IEnumerable<Rect> blockedRects,
            out Vector3 snappedPosition)
        {
            snappedPosition = EnforcePlacementRules(desiredPosition, stack);
            Vector3 searchOrigin = snappedPosition;

            if (!snapCardsToGrid || stack == null || stack.TopCard == null)
            {
                return stack != null;
            }

            if (!TryGetGridLayout(out int columns, out int rows, out float left, out float right, out float bottom, out float top))
            {
                return false;
            }

            var takenRects    = occupiedRects != null ? new List<Rect>(occupiedRects) : null;
            var reservedRects = blockedRects  != null ? new List<Rect>(blockedRects)  : null;

            if (TryFindNearestGridPositionInternal(
                searchOrigin, stack, columns, rows, left, right, bottom, top,
                takenRects, reservedRects, out snappedPosition))
            {
                return true;
            }

            return TryFindNearestGridPositionInternal(
                searchOrigin, stack, columns, rows, left, right, bottom, top,
                null, reservedRects, out snappedPosition);
        }

        // ── Grid layout helpers ───────────────────────────────────────────────────

        private bool TryGetGridLayout(
            out int   columns,
            out int   rows,
            out float left,
            out float right,
            out float bottom,
            out float top)
        {
            // Grid is defined by code, not derived from physical board dimensions.
            // Changing startingColumns/Rows/gridCellSize is all that's needed for balancing —
            // the board mesh scales to match automatically.
            columns = startingColumns;
            rows    = startingRows + (usePurchasedExpansionRows ? purchasedExpansionRows : 0);
            left = right = bottom = top = 0f;

            if (columns <= 0 || rows <= 0 || gridCellSize.x <= 0f || gridCellSize.y <= 0f)
                return false;

            float usedWidth  = columns * gridCellSize.x;
            float usedHeight = rows    * gridCellSize.y;

            left  = currentBounds.center.x - usedWidth * 0.5f;
            right = left + usedWidth;
            top    = currentBounds.max.z - topMargin;
            bottom = top - usedHeight;

            return true;
        }

        private bool TryFindNearestGridPositionInternal(
            Vector3    desiredPosition,
            CardStack  stack,
            int        columns,
            int        rows,
            float      left,
            float      right,
            float      bottom,
            float      top,
            List<Rect> occupiedRects,
            List<Rect> blockedRects,
            out Vector3 snappedPosition)
        {
            snappedPosition = EnforcePlacementRules(desiredPosition, stack);

            float halfCellWidth = gridCellSize.x * 0.5f;
            float halfCellDepth = gridCellSize.y * 0.5f;
            float firstX = left + halfCellWidth;
            float firstZ = top  - halfCellDepth;

            float bestSqrDistance = float.MaxValue;
            bool  found           = false;

            for (int row = 0; row < rows; row++)
            {
                float z = firstZ - (row * gridCellSize.y);

                for (int col = 0; col < columns; col++)
                {
                    float   x                 = firstX + (col * gridCellSize.x);
                    Vector3 candidatePosition = new Vector3(x, desiredPosition.y, z);

                    if (!CanPlaceStackAt(candidatePosition, stack, occupiedRects, blockedRects))
                        continue;

                    float sqrDistance = (new Vector2(x, z) - new Vector2(desiredPosition.x, desiredPosition.z)).sqrMagnitude;

                    if (!found || sqrDistance < bestSqrDistance)
                    {
                        bestSqrDistance = sqrDistance;
                        snappedPosition = candidatePosition;
                        found = true;
                    }
                }
            }

            return found;
        }

        private bool CanPlaceStackAt(
            Vector3    anchorPosition,
            CardStack  stack,
            List<Rect> occupiedRects,
            List<Rect> blockedRects)
        {
            Rect  candidateRect = GetStackRect(anchorPosition, stack);
            float playableTop   = currentBounds.max.z - topMargin;
            const float Epsilon = 0.0001f;

            if (candidateRect.xMin < currentBounds.min.x - Epsilon ||
                candidateRect.xMax > currentBounds.max.x + Epsilon ||
                candidateRect.yMin < currentBounds.min.z - Epsilon ||
                candidateRect.yMax > playableTop + Epsilon)
            {
                return false;
            }

            if (blockedRects != null)
            {
                foreach (var blockedRect in blockedRects)
                {
                    if (RectsOverlap(candidateRect, blockedRect))
                        return false;
                }
            }

            if (occupiedRects != null)
            {
                foreach (var occupiedRect in occupiedRects)
                {
                    if (RectsOverlap(candidateRect, occupiedRect))
                        return false;
                }
            }

            return true;
        }

        private static bool RectsOverlap(Rect a, Rect b)
        {
            return a.xMin < b.xMax &&
                   a.xMax > b.xMin &&
                   a.yMin < b.yMax &&
                   a.yMax > b.yMin;
        }
    }
}
