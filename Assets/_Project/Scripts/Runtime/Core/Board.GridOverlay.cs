using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Markyu.LastKernel
{
    public partial class Board
    {
        // ── Grid overlay mesh ─────────────────────────────────────────────────────

        private void EnsureGridOverlay()
        {
            if (!Application.isPlaying)
                return;

            var existing  = transform.Find(GridOverlayName);
            var gridObject = existing != null ? existing.gameObject : new GameObject(GridOverlayName);

            if (existing == null)
            {
                gridObject.transform.SetParent(transform, false);
                gridObject.hideFlags = HideFlags.DontSave;
            }

            gridFilter = gridObject.GetComponent<MeshFilter>();
            if (gridFilter == null)
                gridFilter = gridObject.AddComponent<MeshFilter>();

            gridRenderer = gridObject.GetComponent<MeshRenderer>();
            if (gridRenderer == null)
                gridRenderer = gridObject.AddComponent<MeshRenderer>();

            if (gridMesh == null)
            {
                gridMesh = new Mesh
                {
                    name      = "Board Grid Overlay",
                    hideFlags = HideFlags.DontSave
                };
            }

            gridFilter.sharedMesh = gridMesh;

            if (gridMaterial == null)
            {
                var shader = gridOverlayShader != null
                    ? gridOverlayShader
                    : Shader.Find("Sprites/Default");

                if (shader != null)
                {
                    gridMaterial = new Material(shader) { hideFlags = HideFlags.DontSave };
                    if (gridMaterial.HasProperty("_Color"))
                        gridMaterial.color = Color.white;
                }
            }

            gridRenderer.sharedMaterial                = gridMaterial;
            gridRenderer.shadowCastingMode             = ShadowCastingMode.Off;
            gridRenderer.receiveShadows                = false;
            gridRenderer.lightProbeUsage               = LightProbeUsage.Off;
            gridRenderer.reflectionProbeUsage          = ReflectionProbeUsage.Off;
            gridRenderer.allowOcclusionWhenDynamic     = false;
        }

        private void RebuildGridOverlay()
        {
            if (!Application.isPlaying)
                return;

            EnsureGridOverlay();

            if (gridMesh == null || gridRenderer == null)
                return;

            if (!showGridOverlay || !TryGetGridLayout(out int columns, out int rows, out float left, out float right, out float bottom, out float top))
            {
                gridMesh.Clear();
                gridRenderer.enabled = false;
                return;
            }

            gridRenderer.enabled = true;

            var vertices  = new List<Vector3>();
            var triangles = new List<int>();
            var colors    = new List<Color>();

            for (int col = 0; col <= columns; col++)
            {
                float x             = left + (col * gridCellSize.x);
                bool  isOuter       = IsOuterLine(col, columns);
                float halfThickness = (isOuter ? gridLineThickness * 1.35f : gridLineThickness) * 0.5f;
                Color color         = isOuter ? gridBorderColor : gridLineColor;

                Vector3 a = transform.InverseTransformPoint(new Vector3(x - halfThickness, gridSurfaceOffset, bottom));
                Vector3 b = transform.InverseTransformPoint(new Vector3(x + halfThickness, gridSurfaceOffset, bottom));
                Vector3 c = transform.InverseTransformPoint(new Vector3(x + halfThickness, gridSurfaceOffset, top));
                Vector3 d = transform.InverseTransformPoint(new Vector3(x - halfThickness, gridSurfaceOffset, top));

                AddQuad(vertices, triangles, colors, a, b, c, d, color);
            }

            for (int row = 0; row <= rows; row++)
            {
                float z             = top - (row * gridCellSize.y);
                bool  isOuter       = IsOuterLine(row, rows);
                float halfThickness = (isOuter ? gridLineThickness * 1.35f : gridLineThickness) * 0.5f;
                Color color         = isOuter ? gridBorderColor : gridLineColor;

                Vector3 a = transform.InverseTransformPoint(new Vector3(left,  gridSurfaceOffset, z - halfThickness));
                Vector3 b = transform.InverseTransformPoint(new Vector3(right, gridSurfaceOffset, z - halfThickness));
                Vector3 c = transform.InverseTransformPoint(new Vector3(right, gridSurfaceOffset, z + halfThickness));
                Vector3 d = transform.InverseTransformPoint(new Vector3(left,  gridSurfaceOffset, z + halfThickness));

                AddQuad(vertices, triangles, colors, a, b, c, d, color);
            }

            gridMesh.Clear();
            gridMesh.SetVertices(vertices);
            gridMesh.SetTriangles(triangles, 0);
            gridMesh.SetColors(colors);
            gridMesh.RecalculateBounds();
        }

        private static bool IsOuterLine(int index, int maxIndex)
        {
            return index == 0 || index == maxIndex;
        }

        private static void AddQuad(
            List<Vector3> vertices,
            List<int>     triangles,
            List<Color>   colors,
            Vector3 a, Vector3 b, Vector3 c, Vector3 d,
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
    }
}
