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
//   2. Save it to Assets/_Project/Settings/Default_Camera_Settings.asset
//   3. Assign it to CameraController._settings in the prefab if you want
//      CameraController to read from it instead of its own [SerializeField] fields
//      (optional — the controller works standalone without this asset).

using Sirenix.OdinInspector;
using UnityEngine;

namespace Markyu.LastKernel
{
    [CreateAssetMenu(menuName = "Last Kernel/Camera Settings", fileName = "Default_Camera_Settings")]
    public class CameraSettings : ScriptableObject
    {
        [BoxGroup("Design Reference")]
        [InfoBox("Documentation only — values in this group are not read at runtime.")]
        [SerializeField, Tooltip("Intended target resolution. Used when configuring Canvas Scalers.")]
        private Vector2Int referenceResolution = new Vector2Int(1920, 1080);

        [BoxGroup("Design Reference")]
        [SerializeField, Tooltip("Aspect ratio the game is primarily designed for.")]
        private string targetAspect = "16:9";

        [BoxGroup("Camera Rig")]
        [SerializeField, Tooltip("Starting distance from the board when a scene loads.")]
        private float defaultDistance = 12f;

        [BoxGroup("Camera Rig")]
        [SerializeField, Tooltip("Minimum zoom-in distance from the board surface.")]
        private float minDistance = 5f;

        [BoxGroup("Camera Rig")]
        [SerializeField, Tooltip("Maximum zoom-out distance from the board surface.")]
        private float maxDistance = 20f;

        [BoxGroup("Pan")]
        [SerializeField, Tooltip("Base pan speed multiplier. Actual speed also scales with camera height.")]
        private float panSpeed = 0.01f;

        [BoxGroup("Pan")]
        [SerializeField, Tooltip("SmoothDamp smoothing time for camera movement.")]
        private float smoothTime = 0.15f;

        [BoxGroup("Pan")]
        [SerializeField, Tooltip("Extra padding (world units) beyond the Board edge the camera can scroll into.")]
        private float panPadding = 0.5f;

        [BoxGroup("Zoom")]
        [SerializeField, Tooltip("Scroll-wheel zoom speed multiplier.")]
        private float zoomSpeed = 1f;

        [BoxGroup("Fallback Pan Bounds")]
        [InfoBox("Used when no Board is present (Title scene).")]
        [SerializeField, Tooltip("Minimum X/Z pan boundary when no Board drives the clamps.")]
        private Vector2 defaultClampMin = new Vector2(-10f, -5f);

        [BoxGroup("Fallback Pan Bounds")]
        [SerializeField, Tooltip("Maximum X/Z pan boundary when no Board drives the clamps.")]
        private Vector2 defaultClampMax = new Vector2(10f, 5f);

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
