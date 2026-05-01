using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace Markyu.LastKernel
{
    public class CameraController : MonoBehaviour
    {
        [BoxGroup("References")]
        [Required, SerializeField, Tooltip("The transform of the pivot camera that moves and zooms with this controller.")]
        private Transform cameraTransform;

        [BoxGroup("Pan")]
        [SerializeField, Tooltip("How fast the camera moves across the board when dragging.")]
        private float panSpeed = 0.01f;

        [BoxGroup("Pan")]
        [SerializeField, Tooltip("Smoothing time used when interpolating camera movement toward the target position.")]
        private float smoothTime = 0.15f;

        [BoxGroup("Pan")]
        [SerializeField, Tooltip("How far past the board edge the camera can scroll.")]
        private float panPadding = 0.5f;

        [BoxGroup("Pan")]
        [SerializeField, Tooltip("World-units per second for WASD keyboard pan. Scales with zoom height.")]
        private float wasdPanSpeed = 8f;

        [BoxGroup("Zoom")]
        [SerializeField, Tooltip("How fast the camera zooms in and out when scrolling.")]
        private float zoomSpeed = 1f;

        [BoxGroup("Zoom")]
        [SerializeField, Tooltip("Scale applied to pinch-zoom pixel deltas. Tune per device DPI.")]
        private float pinchZoomSpeed = 0.004f;

        [BoxGroup("Zoom")]
        [SerializeField, Tooltip("Minimum allowed zoom-in distance from the ground.")]
        private float minDistance = 5f;

        [BoxGroup("Zoom")]
        [SerializeField, Tooltip("Maximum allowed zoom-out distance from the ground.")]
        private float maxDistance = 20f;

        [BoxGroup("Fallback Bounds")]
        [InfoBox("Overridden at runtime by Board.WorldBounds when a Board is present.")]
        [SerializeField, Tooltip("Default minimum X/Z pan boundary when no Board is present (Title scene).")]
        private Vector2 clampMin = new Vector2(-10f, -5f);

        [BoxGroup("Fallback Bounds")]
        [SerializeField, Tooltip("Default maximum X/Z pan boundary when no Board is present (Title scene).")]
        private Vector2 clampMax = new Vector2(10f, 5f);

        // Single-finger pan state
        private bool isDragging;
        private Vector3 dragOrigin;

        // Two-finger pan state
        private bool _isTwoFingerPanning;
        private Vector2 _twoFingerPanOrigin;

        private Vector3 targetPos;
        private Vector3 velocity;

        private void Awake()
        {
            targetPos = transform.position;
        }

        private void Start()
        {
            if (Board.Instance != null)
            {
                Board.Instance.OnBoundsUpdated += UpdateMovementClamps;
                UpdateMovementClamps(Board.Instance.WorldBounds);
            }
        }

        private void OnDestroy()
        {
            if (Board.Instance != null)
            {
                Board.Instance.OnBoundsUpdated -= UpdateMovementClamps;
            }
        }

        private void UpdateMovementClamps(Bounds boardBounds)
        {
            clampMin.x = boardBounds.min.x - panPadding;
            clampMax.x = boardBounds.max.x + panPadding;
            clampMin.y = boardBounds.min.z - panPadding;
            clampMax.y = boardBounds.max.z + panPadding;
            ClampTargetPosition();
        }

        private void Update()
        {
            var input = InputManager.Instance;
            if (input == null || !input.IsInputEnabled) return;

            HandlePan(input);
            HandleZoom(input);

            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPos,
                ref velocity,
                smoothTime,
                Mathf.Infinity,
                Time.unscaledDeltaTime
            );
        }

        private void HandlePan(InputManager input)
        {
            int touchCount = input.GetTouchCount();

            // ── Two-finger pan (mobile) ──────────────────────────────────────
            // Two-finger pan takes priority over single-finger pan so the player
            // can reposition while also pinch-zooming. Card drag is never active
            // during two-finger gestures.
            if (touchCount >= 2)
            {
                Vector2 midpoint = input.GetTwoFingerMidpoint();

                if (!_isTwoFingerPanning)
                {
                    _twoFingerPanOrigin = midpoint;
                    _isTwoFingerPanning = true;
                    isDragging = false; // cancel any single-finger pan
                }
                else
                {
                    Vector2 delta = midpoint - _twoFingerPanOrigin;
                    Vector3 move  = new Vector3(-delta.x, 0f, -delta.y) * panSpeed * (transform.position.y / 10f);
                    targetPos += move;
                    ClampTargetPosition();
                    _twoFingerPanOrigin = midpoint;
                }
                return;
            }

            _isTwoFingerPanning = false;

            // ── Guard: never pan while a card is being dragged ───────────────
            // CardFeelPresenter.IsDraggingAny is set synchronously by CardController
            // inside the EventSystem pointer-down callback, which may run in the
            // same frame as this Update. The physics-raycast inside IsPointerBlocked
            // handles the same-frame case; this guard prevents stale-panning on
            // subsequent frames after a drag has begun.
            if (CardFeelPresenter.IsDraggingAny)
            {
                isDragging = false;
                return;
            }

            // ── Single-finger / mouse pan ─────────────────────────────────────
            if ((input.WasPrimaryPointerPressedThisFrame() && !IsPointerBlocked()) ||
                 input.WasMiddlePointerPressedThisFrame())
            {
                dragOrigin = input.GetPointerScreenPosition();
                isDragging = true;
            }

            if (input.WasPrimaryPointerReleasedThisFrame() || input.WasMiddlePointerReleasedThisFrame())
                isDragging = false;

            if (isDragging)
            {
                Vector3 currentPointerPosition = input.GetPointerScreenPosition();
                Vector3 delta = currentPointerPosition - dragOrigin;
                Vector3 move  = new Vector3(-delta.x, 0f, -delta.y) * panSpeed * (transform.position.y / 10f);

                targetPos += move;
                ClampTargetPosition();

                dragOrigin = currentPointerPosition;
            }

            if (!CardFeelPresenter.IsDraggingAny)
            {
                Vector2 keyMove = input.GetCameraMoveInput();
                if (keyMove.sqrMagnitude > 0.001f)
                {
                    float speed = wasdPanSpeed * (transform.position.y / 10f);
                    targetPos += new Vector3(keyMove.x, 0f, keyMove.y) * speed * Time.unscaledDeltaTime;
                    ClampTargetPosition();
                }
            }
        }

        private void ClampTargetPosition()
        {
            if (Mathf.Approximately(clampMin.x, clampMax.x)) return;

            targetPos.x = Mathf.Clamp(targetPos.x, clampMin.x, clampMax.x);
            targetPos.z = Mathf.Clamp(targetPos.z, clampMin.y, clampMax.y);
        }

        private void HandleZoom(InputManager input)
        {
            float scroll = input.GetScrollDeltaY();
            float pinch  = input.GetPinchDelta() * pinchZoomSpeed;

            // Prefer scroll wheel; fall back to pinch on touch.
            float zoomInput = Mathf.Abs(scroll) > 0.01f ? scroll * zoomSpeed : pinch;

            if (Mathf.Abs(zoomInput) > 0.001f)
            {
                Vector3 forward = cameraTransform.forward;
                Vector3 newPos  = targetPos + forward * zoomInput;

                float cosAngle = Mathf.Cos(Mathf.Deg2Rad * (90f - cameraTransform.eulerAngles.x));
                float distance = Mathf.Approximately(cosAngle, 0f) ? maxDistance
                    : newPos.y / cosAngle;

                if (distance >= minDistance && distance <= maxDistance)
                {
                    targetPos = newPos;
                    ClampTargetPosition();
                }
            }
        }

        /// <summary>
        /// Returns true if the current pointer position is blocked by a UI element or a card,
        /// preventing the camera from starting a pan gesture.
        /// On mobile this uses a physics raycast against card colliders because
        /// EventSystem.IsPointerOverGameObject() without a touch-specific pointer ID
        /// only checks the mouse pointer and misses touch contacts.
        /// </summary>
        private bool IsPointerBlocked()
        {
            if (EventSystem.current == null) return false;

            // On mobile: raycast against the physics scene to detect cards under the touch.
            // This is reliable regardless of EventSystem pointer ID mapping.
            if (InputManager.Instance != null && InputManager.Instance.GetTouchCount() > 0)
            {
                Vector2 touchPos = InputManager.Instance.GetPointerScreenPosition();
                Camera cam = Camera.main;
                if (cam != null)
                {
                    Ray ray = cam.ScreenPointToRay(touchPos);
                    if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity) &&
                        hit.collider.GetComponent<CardInstance>() != null)
                        return true;
                }
            }

            // Standard check for UI overlays (works reliably for mouse on all platforms).
            return EventSystem.current.IsPointerOverGameObject();
        }

        /// <summary>
        /// Smoothly moves the camera to a target world position.
        /// </summary>
        public IEnumerator MoveTo(Vector3 target, float duration = 0.5f)
        {
            isDragging = false;
            dragOrigin = InputManager.Instance != null
                ? InputManager.Instance.GetPointerScreenPosition()
                : Vector3.zero;

            float desiredDistance = Mathf.Lerp(maxDistance, minDistance, 0.8f);
            Vector3 offset        = -cameraTransform.forward * desiredDistance;
            Vector3 newCameraPos  = target + offset;

            yield return transform.DOMove(newCameraPos, duration)
                .SetUpdate(true)
                .SetLink(gameObject)
                .WaitForCompletion();

            targetPos = newCameraPos;
        }

        /// <summary>
        /// Shakes the camera additively.
        /// </summary>
        public void Shake(float duration = 0.3f, float strength = 0.1f)
        {
            cameraTransform.DOShakePosition(duration, strength)
                .SetUpdate(true)
                .SetLink(gameObject);
        }
    }
}
