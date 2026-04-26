// CameraSettings — ScriptableObject that documents and stores camera design intent
// for Last Kernel. This asset acts as the single authoritative record of camera
// tuning decisions so designers can adjust values without touching code.
//
// IMPORTANT: Last Kernel uses a 3D isometric-style camera, NOT a 2D pixel-perfect
// camera. The com.unity.2d.pixel-perfect package is intentionally absent.
// See Docs/PixelPerfectSetup.md for the full visual-quality strategy.
//
// HOW TO USE:
//   1. Create the asset: Right-click > Last Kernel > Camera Settings
//   2. Save it to Assets/Fortstack/Settings/Default_Camera_Settings.asset
//   3. Assign it to CameraController._settings in the prefab if you want
//      CameraController to read from it instead of its own [SerializeField] fields
//      (optional — the controller works standalone without this asset).

using UnityEngine;

namespace Markyu.LastKernel
{
    [CreateAssetMenu(menuName = "Last Kernel/Camera Settings", fileName = "Default_Camera_Settings")]
    public class CameraSettings : ScriptableObject
    {
        // ─── Design Resolution ────────────────────────────────────────────────
        // The game targets 1920×1080 as its reference resolution.
        // Canvas Scalers should use Scale With Screen Size at 1920×1080.
        // The camera itself is not constrained to pixels; URP renders at native res.

        [Header("Design Reference (documentation only — not read at runtime)")]
        [SerializeField, Tooltip("Intended target resolution. Used when configuring Canvas Scalers. Not enforced in code.")]
        private Vector2Int referenceResolution = new Vector2Int(1920, 1080);

        [SerializeField, Tooltip("Aspect ratio the game is primarily designed for.")]
        private string targetAspect = "16:9";

        // ─── Camera Rig ───────────────────────────────────────────────────────
        [Header("Camera Rig (for documentation / future CameraController opt-in)")]
        [SerializeField, Tooltip("Typical starting distance from the board when a scene loads.")]
        private float defaultDistance = 12f;

        [SerializeField, Tooltip("Minimum zoom-in distance from the board surface.")]
        private float minDistance = 5f;

        [SerializeField, Tooltip("Maximum zoom-out distance from the board surface.")]
        private float maxDistance = 20f;

        // ─── Pan ──────────────────────────────────────────────────────────────
        [Header("Pan")]
        [SerializeField, Tooltip("Base pan speed multiplier. Actual speed also scales with camera height.")]
        private float panSpeed = 0.01f;

        [SerializeField, Tooltip("SmoothDamp smoothing time for camera movement.")]
        private float smoothTime = 0.15f;

        [SerializeField, Tooltip("Extra padding (world units) beyond the Board edge that the camera can scroll into.")]
        private float panPadding = 0.5f;

        // ─── Zoom ─────────────────────────────────────────────────────────────
        [Header("Zoom")]
        [SerializeField, Tooltip("Scroll-wheel zoom speed multiplier.")]
        private float zoomSpeed = 1f;

        // ─── Fallback Bounds (when no Board is present) ───────────────────────
        [Header("Fallback Pan Bounds (Title scene / no Board)")]
        [SerializeField, Tooltip("Minimum X/Z pan boundary when no Board drives the clamps.")]
        private Vector2 defaultClampMin = new Vector2(-10f, -5f);

        [SerializeField, Tooltip("Maximum X/Z pan boundary when no Board drives the clamps.")]
        private Vector2 defaultClampMax = new Vector2(10f, 5f);

        // ─── Accessors ────────────────────────────────────────────────────────
        public Vector2Int ReferenceResolution => referenceResolution;
        public float DefaultDistance => defaultDistance;
        public float MinDistance => minDistance;
        public float MaxDistance => maxDistance;
        public float PanSpeed => panSpeed;
        public float SmoothTime => smoothTime;
        public float PanPadding => panPadding;
        public float ZoomSpeed => zoomSpeed;
        public Vector2 DefaultClampMin => defaultClampMin;
        public Vector2 DefaultClampMax => defaultClampMax;
    }
}
