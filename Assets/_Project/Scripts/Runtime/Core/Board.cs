using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

namespace Markyu.LastKernel
{
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    public class Board : MonoBehaviour
    {
        public static Board Instance { get; private set; }

        public event System.Action<Bounds> OnBoundsUpdated;

        [BoxGroup("Colony Expansion")]
        [SerializeField, Tooltip("Allows this board to grow through purchased colony row upgrades.")]
        private bool usePurchasedExpansionRows = false;

        [BoxGroup("Colony Expansion")]
        [SerializeField, Min(0), Tooltip("Maximum number of board row upgrades this board can buy.")]
        private int maxPurchasedExpansionRows = 0;

        [BoxGroup("Colony Expansion")]
        [SerializeField, Min(0f), Tooltip("Blend shape weight added by each purchased row upgrade.")]
        private float blendShapeWeightPerExpansionRow = 10f;

        [BoxGroup("Colony Expansion")]
        [SerializeField, Min(0), Tooltip("Rows already purchased for this board. Runtime save state keeps this updated.")]
        private int purchasedExpansionRows = 0;

        [BoxGroup("Restricted Area")]
        [SerializeField, Tooltip("Space at the top (Z axis) where cards cannot be placed.")]
        private float topMargin = 1.5f;

        [BoxGroup("Grid")]
        [SerializeField, Tooltip("Whether card stacks should settle onto a board grid instead of using freeform placement.")]
        private bool snapCardsToGrid = true;

        [BoxGroup("Grid")]
        [SerializeField, Tooltip("World-space size of each board cell used for card placement and the visible grid overlay.")]
        private Vector2 gridCellSize = new Vector2(1f, 1.1f);

        [BoxGroup("Grid")]
        [SerializeField, Tooltip("Shows a runtime-generated grid mesh on top of the board so placement slots are easier to read.")]
        private bool showGridOverlay = true;

        [BoxGroup("Grid")]
        [SerializeField, Tooltip("Tint used by the inner grid lines.")]
        private Color gridLineColor = new Color(1f, 1f, 1f, 0.12f);

        [BoxGroup("Grid")]
        [SerializeField, Tooltip("Tint used by the outer border of the playable grid.")]
        private Color gridBorderColor = new Color(1f, 0.85f, 0.45f, 0.28f);

        [BoxGroup("Grid")]
        [SerializeField, Min(0.001f), Tooltip("Thickness of the generated grid line quads.")]
        private float gridLineThickness = 0.02f;

        [BoxGroup("Grid")]
        [SerializeField, Tooltip("Small offset so the overlay renders slightly above the board surface.")]
        private float gridSurfaceOffset = 0.01f;

        public float TopMargin => topMargin;
        public Bounds WorldBounds => currentBounds;
        public bool SnapCardsToGrid => snapCardsToGrid;
        public Vector2 GridCellSize => gridCellSize;
        public bool UsesPurchasedExpansionRows => usePurchasedExpansionRows;
        public int PurchasedExpansionRows => purchasedExpansionRows;
        public int MaxPurchasedExpansionRows => usePurchasedExpansionRows ? maxPurchasedExpansionRows : 0;
        public bool CanPurchaseExpansionRow => usePurchasedExpansionRows && purchasedExpansionRows < maxPurchasedExpansionRows;

        private SkinnedMeshRenderer skinnedMesh;
        private Mesh bakedMesh;
        private Bounds currentBounds;
        private int statBoost;
        private int appliedBoost;

        private MeshFilter gridFilter;
        private MeshRenderer gridRenderer;
        private Mesh gridMesh;
        private Material gridMaterial;

        private const string GridOverlayName = "_BoardGridOverlay";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            skinnedMesh = GetComponent<SkinnedMeshRenderer>();
            bakedMesh = new Mesh();

            EnsureGridOverlay();
            UpdateCurrentBounds();

            if (GameDirector.Instance != null)
            {
                GameDirector.Instance.OnSceneDataReady += HandleSceneDataReady;
                GameDirector.Instance.OnBeforeSave += HandleBeforeSave;
            }
        }

        private void Start()
        {
            if (CardManager.Instance != null)
            {
                CardManager.Instance.OnStatsChanged += HandleStatsChanged;
                HandleStatsChanged(CardManager.Instance.GetStatsSnapshot());
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            if (CardManager.Instance != null)
            {
                CardManager.Instance.OnStatsChanged -= HandleStatsChanged;
            }

            if (GameDirector.Instance != null)
            {
                GameDirector.Instance.OnSceneDataReady -= HandleSceneDataReady;
                GameDirector.Instance.OnBeforeSave -= HandleBeforeSave;
            }

            if (bakedMesh != null)
            {
                Destroy(bakedMesh);
            }

            if (gridMesh != null)
            {
                Destroy(gridMesh);
            }

            if (gridMaterial != null)
            {
                Destroy(gridMaterial);
            }
        }

        private void HandleStatsChanged(StatsSnapshot stats)
        {
            int nextStatBoost = Mathf.Min(stats.TotalBoost, 100);
            if (nextStatBoost == statBoost) return;

            statBoost = nextStatBoost;
            ApplyBoardBoost();
        }

        public bool TryPurchaseExpansionRow()
        {
            if (!CanPurchaseExpansionRow)
            {
                return false;
            }

            purchasedExpansionRows++;
            ApplyBoardBoost();
            return true;
        }

        private void ApplyBoardBoost(bool resolveStacks = true)
        {
            int nextBoost = GetTargetBoost();
            if (nextBoost == appliedBoost) return;

            bool isBoardShrinking = nextBoost < appliedBoost;
            appliedBoost = nextBoost;

            if (skinnedMesh == null)
            {
                skinnedMesh = GetComponent<SkinnedMeshRenderer>();
            }

            skinnedMesh.SetBlendShapeWeight(0, appliedBoost);
            UpdateCurrentBounds();

            if (!resolveStacks || CardManager.Instance == null)
            {
                return;
            }

            if (snapCardsToGrid)
            {
                CardManager.Instance.ResolveOverlaps();
            }
            else if (isBoardShrinking)
            {
                CardManager.Instance.EnforceBoardLimits();
            }
        }

        private int GetTargetBoost()
        {
            float purchasedBoost = usePurchasedExpansionRows
                ? purchasedExpansionRows * blendShapeWeightPerExpansionRow
                : 0f;

            return Mathf.Clamp(statBoost + Mathf.RoundToInt(purchasedBoost), 0, 100);
        }

        private void HandleSceneDataReady(SceneData sceneData, bool wasLoaded)
        {
            purchasedExpansionRows = wasLoaded ? sceneData.ColonyBoardPurchasedRows : 0;
            ClampPurchasedExpansionRows();
            ApplyBoardBoost(resolveStacks: false);
        }

        private void HandleBeforeSave(GameData gameData)
        {
            if (gameData.TryGetScene(out var sceneData))
            {
                sceneData.ColonyBoardPurchasedRows = purchasedExpansionRows;
            }
        }

        [ContextMenu("Update Current Bounds")]
        private void UpdateCurrentBounds()
        {
            if (skinnedMesh == null)
                skinnedMesh = GetComponent<SkinnedMeshRenderer>();

            if (bakedMesh == null)
                bakedMesh = new Mesh();

            skinnedMesh.BakeMesh(bakedMesh);
            bakedMesh.RecalculateBounds();

            var localBounds = bakedMesh.bounds;
            var center = transform.TransformPoint(localBounds.center);
            var extents = Vector3.Scale(localBounds.extents, transform.lossyScale);

            currentBounds = new Bounds(center, extents * 2f);

            if (Application.isPlaying)
            {
                RebuildGridOverlay();
            }

            OnBoundsUpdated?.Invoke(currentBounds);
        }

        /// <summary>
        /// Constrains the movement of a CardStack's root position to ensure its entire physical footprint
        /// remains within the playable world boundaries of the board.
        /// </summary>
        /// <remarks>
        /// The method calculates the effective minimum and maximum coordinates by factoring in the stack's
        /// width and depth relative to the board's Bounds, and then clamps the requested position.
        /// This check enforces the X and Z boundaries of the board but does not specifically handle the
        /// restricted top margin (header area).
        /// </remarks>
        /// <param name="position">The desired world position for the stack.</param>
        /// <param name="stack">The <see cref="CardStack"/> whose dimensions are used for the clamping offset.</param>
        /// <returns>A Vector3 that is guaranteed to be within the board's physical boundaries.</returns>
        public Vector3 ClampToBounds(Vector3 position, CardStack stack)
        {
            if (stack == null) return position;

            float cardWidth = stack.Width;
            float stackDepth = stack.FullDepth;
            float topCardHalfDepth = stack.TopCard.Size.y * 0.5f;

            var min = currentBounds.min + new Vector3(cardWidth * 0.5f, 0, stackDepth - topCardHalfDepth);
            var max = currentBounds.max - new Vector3(cardWidth * 0.5f, 0, topCardHalfDepth);

            return new Vector3(
                Mathf.Clamp(position.x, min.x, max.x),
                position.y,
                Mathf.Clamp(position.z, min.z, max.z)
            );
        }

        /// <summary>
        /// Applies the complete set of placement rules to a CardStack's potential position, 
        /// ensuring it is both within the board's physical boundaries and outside the restricted top margin.
        /// </summary>
        /// <remarks>
        /// This method first uses ClampToBounds to handle the lateral and bottom bounds. 
        /// It then checks if the card's top edge overlaps the restricted margin area and snaps 
        /// the position down (negative Z) if necessary. The result is also flattened to Y=0.
        /// </remarks>
        /// <param name="position">The desired world position for the stack.</param>
        /// <param name="stack">The <see cref="CardStack"/> whose dimensions are used for the placement check.</param>
        /// <returns>A corrected, final Vector3 position that adheres to all board placement rules.</returns>
        public Vector3 EnforcePlacementRules(Vector3 position, CardStack stack)
        {
            if (stack == null) return position;

            var clamped = ClampToBounds(position, stack);

            float restrictedStart = currentBounds.max.z - topMargin;
            float cardTopEdge = clamped.z + (stack.TopCard.Size.y * 0.5f);

            if (cardTopEdge > restrictedStart)
            {
                clamped.z = restrictedStart - (stack.TopCard.Size.y * 0.5f);
            }

            return clamped.Flatten();
        }

        /// <summary>
        /// Returns the nearest valid grid anchor for a card stack, ignoring other stacks.
        /// </summary>
        public Vector3 SnapToNearestGridPosition(Vector3 desiredPosition, CardStack stack)
        {
            if (TryFindNearestGridPosition(desiredPosition, stack, null, null, out var snappedPosition))
            {
                return snappedPosition;
            }

            return EnforcePlacementRules(desiredPosition, stack);
        }

        /// <summary>
        /// Finds the nearest valid board cell for a stack while respecting the board limits,
        /// reserved areas, and already-occupied stack footprints.
        /// </summary>
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

            var takenRects = occupiedRects != null ? new List<Rect>(occupiedRects) : null;
            var reservedRects = blockedRects != null ? new List<Rect>(blockedRects) : null;

            if (TryFindNearestGridPositionInternal(
                searchOrigin,
                stack,
                columns,
                rows,
                left,
                right,
                bottom,
                top,
                takenRects,
                reservedRects,
                out snappedPosition))
            {
                return true;
            }

            return TryFindNearestGridPositionInternal(
                searchOrigin,
                stack,
                columns,
                rows,
                left,
                right,
                bottom,
                top,
                null,
                reservedRects,
                out snappedPosition
            );
        }

        /// <summary>
        /// Returns the world-space footprint a stack occupies on the board when anchored at the given position.
        /// </summary>
        public Rect GetStackRect(Vector3 anchorPosition, CardStack stack)
        {
            if (stack == null || stack.TopCard == null)
            {
                return new Rect(anchorPosition.x, anchorPosition.z, 0f, 0f);
            }

            float halfWidth = stack.Width * 0.5f;
            float halfTopDepth = stack.TopCard.Size.y * 0.5f;
            float xMin = anchorPosition.x - halfWidth;
            float xMax = anchorPosition.x + halfWidth;
            float zMax = anchorPosition.z + halfTopDepth;
            float zMin = anchorPosition.z - (stack.FullDepth - halfTopDepth);

            return Rect.MinMaxRect(xMin, zMin, xMax, zMax);
        }

        /// <summary>
        /// Converts a combat rect into a world-space X/Z footprint for grid avoidance.
        /// </summary>
        public Rect GetCombatRectWorldRect(CombatRect combatRect)
        {
            if (combatRect == null || combatRect.Rect == null)
            {
                return new Rect();
            }

            var rectTransform = combatRect.Rect;
            Vector3 worldCenter = rectTransform.TransformPoint(rectTransform.rect.center);
            Vector2 worldSize = Vector2.Scale(rectTransform.rect.size, rectTransform.lossyScale);
            Vector2 halfSize = worldSize * 0.5f;

            return Rect.MinMaxRect(
                worldCenter.x - halfSize.x,
                worldCenter.z - halfSize.y,
                worldCenter.x + halfSize.x,
                worldCenter.z + halfSize.y
            );
        }

        /// <summary>
        /// Checks if a given world position is a valid placement location for a card or stack on the board.
        /// </summary>
        /// <remarks>
        /// A position is valid if it is entirely within the physical boundaries of the board
        /// and does not overlap with the restricted top margin area (header). The stack's dimensions
        /// are accounted for when performing the boundary checks.
        /// </remarks>
        /// <param name="position">The world position to validate.</param>
        /// <param name="stack">The <see cref="CardStack"/> to check. If null, the check assumes a point (0 size).</param>
        /// <returns>True if the position is a valid placement location; otherwise, false.</returns>
        public bool IsPointValid(Vector3 position, CardStack stack = null)
        {
            if (currentBounds.extents == Vector3.zero)
                UpdateCurrentBounds();

            // If we have a stack, account for its size
            float cardHalfWidth = stack != null ? stack.Width * 0.5f : 0f;
            float cardHalfDepth = stack != null ? stack.TopCard.Size.y * 0.5f : 0f;

            // Effective min/max bounds considering card footprint
            var min = currentBounds.min + new Vector3(cardHalfWidth, 0, cardHalfDepth);
            var max = currentBounds.max - new Vector3(cardHalfWidth, 0, cardHalfDepth);

            // First check: inside overall board bounds
            if (position.x < min.x || position.x > max.x ||
                position.z < min.z || position.z > max.z)
            {
                return false;
            }

            // Second check: not in restricted margin (header)
            float restrictedStart = currentBounds.max.z - topMargin;
            float cardTopEdge = position.z + cardHalfDepth;
            if (cardTopEdge > restrictedStart)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Calculates a corrected world position for a RectTransform to ensure its entire area
        /// stays within the board's playable zone.
        /// </summary>
        /// <remarks>
        /// This method is primarily used for positioning UI elements like combat rectangles. It converts
        /// the RectTransform's local dimensions to world space, clamps the resulting footprint
        /// within the board's physical boundaries, and enforces the restricted top margin rule.
        /// </remarks>
        /// <param name="rect">The RectTransform whose world position needs to be corrected.</param>
        /// <returns>A corrected Vector3 position that keeps the RectTransform fully within the valid board area.</returns>
        public Vector3 ClampRectTransformToBoard(RectTransform rect)
        {
            if (rect == null)
                return Vector3.zero;

            if (currentBounds.extents == Vector3.zero)
                UpdateCurrentBounds();

            // Convert rect size from local to world
            Vector3 rectSize = rect.rect.size;
            Vector3 worldSize = Vector3.Scale(rectSize, rect.lossyScale);

            float halfWidth = worldSize.x * 0.5f;
            float halfDepth = worldSize.y * 0.5f; // RectTransform height: Z axis

            Vector3 position = rect.position;

            // Effective min/max considering rect footprint
            var min = currentBounds.min + new Vector3(halfWidth, 0, halfDepth);
            var max = currentBounds.max - new Vector3(halfWidth, 0, halfDepth);

            // Clamp to board
            Vector3 clamped = new Vector3(
                Mathf.Clamp(position.x, min.x, max.x),
                0f, // keep board flat
                Mathf.Clamp(position.z, min.z, max.z)
            );

            // Handle restricted top margin
            float restrictedStart = currentBounds.max.z - topMargin;
            float rectTopEdge = clamped.z + halfDepth;

            if (rectTopEdge > restrictedStart)
            {
                clamped.z = restrictedStart - halfDepth;
            }

            return clamped;
        }

        private bool TryFindNearestGridPositionInternal(
            Vector3 desiredPosition,
            CardStack stack,
            int columns,
            int rows,
            float left,
            float right,
            float bottom,
            float top,
            List<Rect> occupiedRects,
            List<Rect> blockedRects,
            out Vector3 snappedPosition)
        {
            snappedPosition = EnforcePlacementRules(desiredPosition, stack);

            float halfCellWidth = gridCellSize.x * 0.5f;
            float halfCellDepth = gridCellSize.y * 0.5f;
            float firstX = left + halfCellWidth;
            float firstZ = top - halfCellDepth;

            float bestSqrDistance = float.MaxValue;
            bool found = false;

            for (int row = 0; row < rows; row++)
            {
                float z = firstZ - (row * gridCellSize.y);

                for (int col = 0; col < columns; col++)
                {
                    float x = firstX + (col * gridCellSize.x);
                    Vector3 candidatePosition = new Vector3(x, desiredPosition.y, z);

                    if (!CanPlaceStackAt(candidatePosition, stack, occupiedRects, blockedRects))
                    {
                        continue;
                    }

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
            Vector3 anchorPosition,
            CardStack stack,
            List<Rect> occupiedRects,
            List<Rect> blockedRects)
        {
            Rect candidateRect = GetStackRect(anchorPosition, stack);
            float playableTop = currentBounds.max.z - topMargin;
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
                    {
                        return false;
                    }
                }
            }

            if (occupiedRects != null)
            {
                foreach (var occupiedRect in occupiedRects)
                {
                    if (RectsOverlap(candidateRect, occupiedRect))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool RectsOverlap(Rect a, Rect b)
        {
            return a.xMin < b.xMax &&
                   a.xMax > b.xMin &&
                   a.yMin < b.yMax &&
                   a.yMax > b.yMin;
        }

        private bool TryGetGridLayout(
            out int columns,
            out int rows,
            out float left,
            out float right,
            out float bottom,
            out float top)
        {
            columns = 0;
            rows = 0;
            left = right = bottom = top = 0f;

            if (gridCellSize.x <= 0f || gridCellSize.y <= 0f)
            {
                return false;
            }

            float playableWidth = currentBounds.size.x;
            float playableHeight = currentBounds.size.z - topMargin;

            if (playableWidth < gridCellSize.x || playableHeight < gridCellSize.y)
            {
                return false;
            }

            columns = Mathf.FloorToInt(playableWidth / gridCellSize.x);
            rows = Mathf.FloorToInt(playableHeight / gridCellSize.y);

            if (columns <= 0 || rows <= 0)
            {
                return false;
            }

            float usedWidth = columns * gridCellSize.x;
            float usedHeight = rows * gridCellSize.y;

            left = currentBounds.center.x - (usedWidth * 0.5f);
            right = left + usedWidth;

            float playableTop = currentBounds.max.z - topMargin;
            float verticalPadding = Mathf.Max(0f, playableHeight - usedHeight) * 0.5f;
            bottom = currentBounds.min.z + verticalPadding;
            top = playableTop - verticalPadding;

            return true;
        }

        private void EnsureGridOverlay()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            var existing = transform.Find(GridOverlayName);
            GameObject gridObject = existing != null ? existing.gameObject : new GameObject(GridOverlayName);

            if (existing == null)
            {
                gridObject.transform.SetParent(transform, false);
                gridObject.hideFlags = HideFlags.DontSave;
            }

            gridFilter = gridObject.GetComponent<MeshFilter>();
            if (gridFilter == null)
            {
                gridFilter = gridObject.AddComponent<MeshFilter>();
            }

            gridRenderer = gridObject.GetComponent<MeshRenderer>();
            if (gridRenderer == null)
            {
                gridRenderer = gridObject.AddComponent<MeshRenderer>();
            }

            if (gridMesh == null)
            {
                gridMesh = new Mesh
                {
                    name = "Board Grid Overlay",
                    hideFlags = HideFlags.DontSave
                };
            }

            gridFilter.sharedMesh = gridMesh;

            if (gridMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default") ??
                                Shader.Find("Unlit/Color") ??
                                Shader.Find("Hidden/Internal-Colored");

                if (shader != null)
                {
                    gridMaterial = new Material(shader)
                    {
                        hideFlags = HideFlags.DontSave
                    };

                    if (gridMaterial.HasProperty("_Color"))
                    {
                        gridMaterial.color = Color.white;
                    }
                }
            }

            gridRenderer.sharedMaterial = gridMaterial;
            gridRenderer.shadowCastingMode = ShadowCastingMode.Off;
            gridRenderer.receiveShadows = false;
            gridRenderer.lightProbeUsage = LightProbeUsage.Off;
            gridRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            gridRenderer.allowOcclusionWhenDynamic = false;
        }

        private void RebuildGridOverlay()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            EnsureGridOverlay();

            if (gridMesh == null || gridRenderer == null)
            {
                return;
            }

            if (!showGridOverlay || !TryGetGridLayout(out int columns, out int rows, out float left, out float right, out float bottom, out float top))
            {
                gridMesh.Clear();
                gridRenderer.enabled = false;
                return;
            }

            gridRenderer.enabled = true;

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var colors = new List<Color>();

            for (int col = 0; col <= columns; col++)
            {
                float x = left + (col * gridCellSize.x);
                float halfThickness = (IsOuterLine(col, columns) ? gridLineThickness * 1.35f : gridLineThickness) * 0.5f;
                Color color = IsOuterLine(col, columns) ? gridBorderColor : gridLineColor;

                Vector3 a = transform.InverseTransformPoint(new Vector3(x - halfThickness, gridSurfaceOffset, bottom));
                Vector3 b = transform.InverseTransformPoint(new Vector3(x + halfThickness, gridSurfaceOffset, bottom));
                Vector3 c = transform.InverseTransformPoint(new Vector3(x + halfThickness, gridSurfaceOffset, top));
                Vector3 d = transform.InverseTransformPoint(new Vector3(x - halfThickness, gridSurfaceOffset, top));

                AddQuad(vertices, triangles, colors, a, b, c, d, color);
            }

            for (int row = 0; row <= rows; row++)
            {
                float z = top - (row * gridCellSize.y);
                float halfThickness = (IsOuterLine(row, rows) ? gridLineThickness * 1.35f : gridLineThickness) * 0.5f;
                Color color = IsOuterLine(row, rows) ? gridBorderColor : gridLineColor;

                Vector3 a = transform.InverseTransformPoint(new Vector3(left, gridSurfaceOffset, z - halfThickness));
                Vector3 b = transform.InverseTransformPoint(new Vector3(right, gridSurfaceOffset, z - halfThickness));
                Vector3 c = transform.InverseTransformPoint(new Vector3(right, gridSurfaceOffset, z + halfThickness));
                Vector3 d = transform.InverseTransformPoint(new Vector3(left, gridSurfaceOffset, z + halfThickness));

                AddQuad(vertices, triangles, colors, a, b, c, d, color);
            }

            gridMesh.Clear();
            gridMesh.SetVertices(vertices);
            gridMesh.SetTriangles(triangles, 0);
            gridMesh.SetColors(colors);
            gridMesh.RecalculateBounds();
        }

        private bool IsOuterLine(int index, int maxIndex)
        {
            return index == 0 || index == maxIndex;
        }

        private void AddQuad(
            List<Vector3> vertices,
            List<int> triangles,
            List<Color> colors,
            Vector3 a,
            Vector3 b,
            Vector3 c,
            Vector3 d,
            Color color)
        {
            int startIndex = vertices.Count;

            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            vertices.Add(d);

            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);

            triangles.Add(startIndex + 0);
            triangles.Add(startIndex + 1);
            triangles.Add(startIndex + 2);
            triangles.Add(startIndex + 0);
            triangles.Add(startIndex + 2);
            triangles.Add(startIndex + 3);
        }

        private void ClampPurchasedExpansionRows()
        {
            if (!usePurchasedExpansionRows)
            {
                purchasedExpansionRows = 0;
                return;
            }

            maxPurchasedExpansionRows = Mathf.Max(0, maxPurchasedExpansionRows);
            purchasedExpansionRows = Mathf.Clamp(purchasedExpansionRows, 0, maxPurchasedExpansionRows);
        }

        private void OnValidate()
        {
            blendShapeWeightPerExpansionRow = Mathf.Max(0f, blendShapeWeightPerExpansionRow);
            ClampPurchasedExpansionRows();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (currentBounds.extents == Vector3.zero)
            {
                UpdateCurrentBounds();
            }

            Vector3 boardCenter = currentBounds.center;
            Vector3 boardSize = new Vector3(currentBounds.size.x, 0.01f, currentBounds.size.z);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(boardCenter, boardSize);

            Vector3 marginCenter = boardCenter + new Vector3(0, 0, (currentBounds.size.z * 0.5f) - (topMargin * 0.5f));
            Vector3 marginSize = new Vector3(currentBounds.size.x - 0.05f, 0.01f, topMargin - 0.05f);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(marginCenter, marginSize);
        }
#endif
    }
}

