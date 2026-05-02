using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Manages the game board mesh, grid layout, colony expansion, and card placement rules.
    ///
    /// Partial files:
    ///   Board.GridSystem.cs  — grid snap, position queries, overlap detection
    ///   Board.GridOverlay.cs — runtime grid mesh generation
    /// </summary>
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    public partial class Board : MonoBehaviour
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
        [SerializeField, Min(1), Tooltip("Number of card columns. The board mesh is scaled at runtime to fit exactly this many columns.")]
        private int startingColumns = 9;

        [BoxGroup("Grid")]
        [SerializeField, Min(1), Tooltip("Number of rows on a fresh game. Players can purchase additional rows up to maxPurchasedExpansionRows.")]
        private int startingRows = 9;

        [BoxGroup("Grid")]
        [SerializeField, Tooltip("World-space size of each board cell. X = column width, Y = row depth.")]
        private Vector2 gridCellSize = new Vector2(1.0f, 1.2f);

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

        [BoxGroup("Grid")]
        [SerializeField, Tooltip("Shader used by the grid overlay mesh. Assign 'Sprites/Default' or 'Unlit/Color'.")]
        private Shader gridOverlayShader;

        public float   TopMargin                => topMargin;
        public Bounds  WorldBounds              => currentBounds;
        public bool    SnapCardsToGrid          => snapCardsToGrid;
        public Vector2 GridCellSize             => gridCellSize;
        public bool    UsesPurchasedExpansionRows => usePurchasedExpansionRows;
        public int     PurchasedExpansionRows   => purchasedExpansionRows;
        public int     MaxPurchasedExpansionRows => usePurchasedExpansionRows ? maxPurchasedExpansionRows : 0;
        public bool    CanPurchaseExpansionRow  => usePurchasedExpansionRows && purchasedExpansionRows < maxPurchasedExpansionRows;

        private SkinnedMeshRenderer skinnedMesh;
        private Mesh                bakedMesh;
        private Bounds              currentBounds;
        private int                 statBoost;
        private int                 appliedBoost;

        private MeshFilter   gridFilter;
        private MeshRenderer gridRenderer;
        private Mesh         gridMesh;
        private Material     gridMaterial;

        private const string GridOverlayName = "_BoardGridOverlay";

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            skinnedMesh = GetComponent<SkinnedMeshRenderer>();
            bakedMesh   = new Mesh();

            EnsureGridOverlay();
            UpdateCurrentBounds();

            if (GameDirector.Instance != null)
            {
                GameDirector.Instance.OnSceneDataReady += HandleSceneDataReady;
                GameDirector.Instance.OnBeforeSave     += HandleBeforeSave;
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
            if (Instance == this) Instance = null;

            if (CardManager.Instance != null)
                CardManager.Instance.OnStatsChanged -= HandleStatsChanged;

            if (GameDirector.Instance != null)
            {
                GameDirector.Instance.OnSceneDataReady -= HandleSceneDataReady;
                GameDirector.Instance.OnBeforeSave     -= HandleBeforeSave;
            }

            if (bakedMesh    != null) Destroy(bakedMesh);
            if (gridMesh     != null) Destroy(gridMesh);
            if (gridMaterial != null) Destroy(gridMaterial);
        }

        // ── Colony expansion ──────────────────────────────────────────────────────

        private void HandleStatsChanged(StatsSnapshot stats)
        {
            int nextStatBoost = Mathf.Min(stats.TotalBoost, 100);
            if (nextStatBoost == statBoost) return;

            statBoost = nextStatBoost;
            ApplyBoardBoost();
        }

        public bool TryPurchaseExpansionRow()
        {
            if (!CanPurchaseExpansionRow) return false;

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

            skinnedMesh.SetBlendShapeWeight(0, appliedBoost);
            UpdateCurrentBounds();

            if (!resolveStacks || CardManager.Instance == null) return;

            if (snapCardsToGrid)
                CardManager.Instance.ResolveOverlaps();
            else if (isBoardShrinking)
                CardManager.Instance.EnforceBoardLimits();
        }

        private int GetTargetBoost()
        {
            float purchasedBoost = usePurchasedExpansionRows
                ? purchasedExpansionRows * blendShapeWeightPerExpansionRow
                : 0f;

            return Mathf.Clamp(statBoost + Mathf.RoundToInt(purchasedBoost), 0, 100);
        }

        // ── Save / load ───────────────────────────────────────────────────────────

        private void HandleSceneDataReady(SceneData sceneData, bool wasLoaded)
        {
            if (wasLoaded)
                purchasedExpansionRows = sceneData.ColonyBoardPurchasedRows;
            ClampPurchasedExpansionRows();
            ApplyBoardBoost(resolveStacks: false);
        }

        private void HandleBeforeSave(GameData gameData)
        {
            if (gameData.TryGetScene(out var sceneData))
                sceneData.ColonyBoardPurchasedRows = purchasedExpansionRows;
        }

        // ── Bounds ────────────────────────────────────────────────────────────────

        [ContextMenu("Update Current Bounds")]
        private void UpdateCurrentBounds()
        {
            if (bakedMesh == null)
                bakedMesh = new Mesh();

            int   totalRows   = startingRows + (usePurchasedExpansionRows ? purchasedExpansionRows : 0);
            float boardWidth  = startingColumns * gridCellSize.x;
            float boardDepth  = totalRows * gridCellSize.y + topMargin;

            // Bake the mesh to know its current local-space footprint (blend shapes included).
            skinnedMesh.BakeMesh(bakedMesh);
            bakedMesh.RecalculateBounds();
            var localBounds = bakedMesh.bounds;

            // Scale transform so the mesh always fills the code-defined area.
            // Decouples grid size from mesh dimensions — adjust startingColumns/Rows/gridCellSize
            // without ever worrying about the underlying mesh.
            if (Application.isPlaying && localBounds.size.x > 0f && localBounds.size.z > 0f)
            {
                transform.localScale = new Vector3(
                    boardWidth / localBounds.size.x,
                    transform.localScale.y,
                    boardDepth / localBounds.size.z
                );
            }

            var center  = transform.TransformPoint(localBounds.center);
            var extentY = localBounds.extents.y * transform.lossyScale.y;
            currentBounds = new Bounds(center, new Vector3(boardWidth, extentY * 2f, boardDepth));

            if (Application.isPlaying)
                RebuildGridOverlay();

            OnBoundsUpdated?.Invoke(currentBounds);
        }

        // ── Placement queries (public) ────────────────────────────────────────────

        public Vector3 ClampToBounds(Vector3 position, CardStack stack)
        {
            if (stack == null) return position;

            float cardWidth       = stack.Width;
            float stackDepth      = stack.FullDepth;
            float topCardHalfDepth = stack.TopCard.Size.y * 0.5f;

            var min = currentBounds.min + new Vector3(cardWidth * 0.5f, 0, stackDepth - topCardHalfDepth);
            var max = currentBounds.max - new Vector3(cardWidth * 0.5f, 0, topCardHalfDepth);

            return new Vector3(
                Mathf.Clamp(position.x, min.x, max.x),
                position.y,
                Mathf.Clamp(position.z, min.z, max.z)
            );
        }

        public Vector3 EnforcePlacementRules(Vector3 position, CardStack stack)
        {
            if (stack == null) return position;

            var clamped = ClampToBounds(position, stack);

            float restrictedStart = currentBounds.max.z - topMargin;
            float cardTopEdge     = clamped.z + (stack.TopCard.Size.y * 0.5f);

            if (cardTopEdge > restrictedStart)
                clamped.z = restrictedStart - (stack.TopCard.Size.y * 0.5f);

            return clamped.Flatten();
        }

        public Rect GetStackRect(Vector3 anchorPosition, CardStack stack)
        {
            if (stack == null || stack.TopCard == null)
                return new Rect(anchorPosition.x, anchorPosition.z, 0f, 0f);

            float halfWidth    = stack.Width * 0.5f;
            float halfTopDepth = stack.TopCard.Size.y * 0.5f;
            float xMin = anchorPosition.x - halfWidth;
            float xMax = anchorPosition.x + halfWidth;
            float zMax = anchorPosition.z + halfTopDepth;
            float zMin = anchorPosition.z - (stack.FullDepth - halfTopDepth);

            return Rect.MinMaxRect(xMin, zMin, xMax, zMax);
        }

        public Rect GetCombatRectWorldRect(CombatRect combatRect)
        {
            if (combatRect == null || combatRect.Rect == null)
                return new Rect();

            var rectTransform = combatRect.Rect;
            Vector3 worldCenter = rectTransform.TransformPoint(rectTransform.rect.center);
            Vector2 worldSize   = Vector2.Scale(rectTransform.rect.size, rectTransform.lossyScale);
            Vector2 halfSize    = worldSize * 0.5f;

            return Rect.MinMaxRect(
                worldCenter.x - halfSize.x,
                worldCenter.z - halfSize.y,
                worldCenter.x + halfSize.x,
                worldCenter.z + halfSize.y
            );
        }

        public bool IsPointValid(Vector3 position, CardStack stack = null)
        {
            if (currentBounds.extents == Vector3.zero)
                UpdateCurrentBounds();

            float cardHalfWidth = stack != null ? stack.Width * 0.5f : 0f;
            float cardHalfDepth = stack != null ? stack.TopCard.Size.y * 0.5f : 0f;

            var min = currentBounds.min + new Vector3(cardHalfWidth, 0, cardHalfDepth);
            var max = currentBounds.max - new Vector3(cardHalfWidth, 0, cardHalfDepth);

            if (position.x < min.x || position.x > max.x ||
                position.z < min.z || position.z > max.z)
            {
                return false;
            }

            float restrictedStart = currentBounds.max.z - topMargin;
            float cardTopEdge     = position.z + cardHalfDepth;
            return cardTopEdge <= restrictedStart;
        }

        public Vector3 ClampRectTransformToBoard(RectTransform rect)
        {
            if (rect == null) return Vector3.zero;

            if (currentBounds.extents == Vector3.zero)
                UpdateCurrentBounds();

            Vector3 rectSize  = rect.rect.size;
            Vector3 worldSize = Vector3.Scale(rectSize, rect.lossyScale);

            float halfWidth = worldSize.x * 0.5f;
            float halfDepth = worldSize.y * 0.5f;

            Vector3 position = rect.position;
            var min = currentBounds.min + new Vector3(halfWidth, 0, halfDepth);
            var max = currentBounds.max - new Vector3(halfWidth, 0, halfDepth);

            Vector3 clamped = new Vector3(
                Mathf.Clamp(position.x, min.x, max.x),
                0f,
                Mathf.Clamp(position.z, min.z, max.z)
            );

            float restrictedStart = currentBounds.max.z - topMargin;
            float rectTopEdge     = clamped.z + halfDepth;

            if (rectTopEdge > restrictedStart)
                clamped.z = restrictedStart - halfDepth;

            return clamped;
        }

        // ── Validation / editor ───────────────────────────────────────────────────

        private void ClampPurchasedExpansionRows()
        {
            if (!usePurchasedExpansionRows)
            {
                purchasedExpansionRows = 0;
                return;
            }

            maxPurchasedExpansionRows = Mathf.Max(0, maxPurchasedExpansionRows);
            purchasedExpansionRows    = Mathf.Clamp(purchasedExpansionRows, 0, maxPurchasedExpansionRows);
        }

        private void OnValidate()
        {
            blendShapeWeightPerExpansionRow = Mathf.Max(0f, blendShapeWeightPerExpansionRow);
            ClampPurchasedExpansionRows();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            int   totalRows   = startingRows + (usePurchasedExpansionRows ? purchasedExpansionRows : 0);
            float boardWidth  = startingColumns * gridCellSize.x;
            float boardDepth  = totalRows * gridCellSize.y + topMargin;

            Vector3 boardCenter  = transform.position + new Vector3(0f, 0f, boardDepth * 0.5f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(boardCenter, new Vector3(boardWidth, 0.01f, boardDepth));

            Vector3 marginCenter = boardCenter + new Vector3(0f, 0f, boardDepth * 0.5f - topMargin * 0.5f);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(marginCenter, new Vector3(boardWidth - 0.05f, 0.01f, topMargin - 0.05f));
        }
#endif
    }
}
