using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Markyu.LastKernel
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        private readonly HashSet<object> inputLocks = new();

        public bool IsInputEnabled => inputLocks.Count == 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

#if ENABLE_INPUT_SYSTEM
            if (GameInputHandler.Instance == null && GetComponent<GameInputHandler>() == null)
                gameObject.AddComponent<GameInputHandler>();
#endif
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Adds an input lock, disabling all general user input throughout the game.
        /// </summary>
        /// <remarks>
        /// Input remains disabled until all unique locks (requesters) have been removed.
        /// This is used to block input during transitions, cutscenes, or menus.
        /// </remarks>
        /// <param name="requester">The object requesting the lock. Must be a unique object instance.</param>
        public void AddLock(object requester)
        {
            if (requester != null)
                inputLocks.Add(requester);
        }

        /// <summary>
        /// Removes a previously acquired input lock.
        /// </summary>
        /// <remarks>
        /// If the requester object exists in the set of active locks, it is removed.
        /// Input will only become enabled again when the last lock is removed.
        /// </remarks>
        /// <param name="requester">The object that originally requested the lock.</param>
        public void RemoveLock(object requester)
        {
            if (requester != null)
                inputLocks.Remove(requester);
        }

        // ── Pointer / Touch ───────────────────────────────────────────────────

        /// <summary>
        /// Returns the current pointer position in screen space.
        /// Prefers the primary touch position when a touch is active.
        /// </summary>
        public Vector2 GetPointerScreenPosition()
        {
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
                return Touchscreen.current.primaryTouch.position.ReadValue();
            return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
            if (Input.touchCount > 0)
                return Input.GetTouch(0).position;
            return Input.mousePosition;
#endif
        }

        /// <summary>
        /// Returns true on the frame the primary pointer (left mouse button or first touch) is pressed.
        /// </summary>
        public bool WasPrimaryPointerPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                return true;
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
                return true;
            return Input.GetMouseButtonDown(0);
#endif
        }

        /// <summary>
        /// Returns true on the frame the primary pointer is released.
        /// </summary>
        public bool WasPrimaryPointerReleasedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
                return true;
            return Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
#else
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended)
                return true;
            return Input.GetMouseButtonUp(0);
#endif
        }

        /// <summary>
        /// Returns true on the frame the middle mouse button is pressed.
        /// </summary>
        public bool WasMiddlePointerPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.middleButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(2);
#endif
        }

        /// <summary>
        /// Returns true on the frame the middle mouse button is released.
        /// </summary>
        public bool WasMiddlePointerReleasedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.middleButton.wasReleasedThisFrame;
#else
            return Input.GetMouseButtonUp(2);
#endif
        }

        /// <summary>
        /// Returns the vertical mouse wheel delta for the current frame.
        /// </summary>
        public float GetScrollDeltaY()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
#else
            return Input.mouseScrollDelta.y;
#endif
        }

        /// <summary>Returns true on the frame the pause/escape shortcut is pressed.</summary>
        public bool WasPausePressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            if (GameInputHandler.WasEscapePressedThisFrame) return true;
            // Fallback for scenes without GameInputHandler.
            return WasKeyPressedThisFrame(KeyCode.Escape);
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        /// <summary>Returns true while the grab-whole-stack modifier key is held.</summary>
        public bool IsShiftHeld()
        {
#if ENABLE_INPUT_SYSTEM
            if (GameInputHandler.IsShiftHeld) return true;
            return IsKeyHeld(KeyCode.LeftShift) || IsKeyHeld(KeyCode.RightShift);
#else
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
        }

        /// <summary>Returns a normalised WASD direction vector; X = right/left, Y = forward/back.</summary>
        public Vector2 GetCameraMoveInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (GameInputHandler.Instance != null)
                return GameInputHandler.CameraMoveInput;

            float x = (IsKeyHeld(KeyCode.D) ? 1f : 0f) - (IsKeyHeld(KeyCode.A) ? 1f : 0f);
            float y = (IsKeyHeld(KeyCode.W) ? 1f : 0f) - (IsKeyHeld(KeyCode.S) ? 1f : 0f);
            return new Vector2(x, y);
#else
            float x = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
            float y = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
            return new Vector2(x, y);
#endif
        }

        /// <summary>Returns true on the frame a keyboard key is pressed across old/new input backends.</summary>
        public bool WasKeyPressedThisFrame(KeyCode keyCode)
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null &&
                   TryConvertKeyCode(keyCode, out Key key) &&
                   Keyboard.current[key].wasPressedThisFrame;
#else
            return Input.GetKeyDown(keyCode);
#endif
        }

        /// <summary>Returns true while a keyboard key is held across old/new input backends.</summary>
        public bool IsKeyHeld(KeyCode keyCode)
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null &&
                   TryConvertKeyCode(keyCode, out Key key) &&
                   Keyboard.current[key].isPressed;
#else
            return Input.GetKey(keyCode);
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private static bool TryConvertKeyCode(KeyCode keyCode, out Key key)
        {
            key = keyCode switch
            {
                KeyCode.A => Key.A,
                KeyCode.B => Key.B,
                KeyCode.D => Key.D,
                KeyCode.F1 => Key.F1,
                KeyCode.L => Key.L,
                KeyCode.S => Key.S,
                KeyCode.V => Key.V,
                KeyCode.W => Key.W,
                KeyCode.Escape => Key.Escape,
                KeyCode.LeftShift => Key.LeftShift,
                KeyCode.RightShift => Key.RightShift,
                KeyCode.BackQuote => Key.Backquote,
                _ => Key.None
            };

            return key != Key.None;
        }
#endif

        // ── Multi-touch ───────────────────────────────────────────────────────

        /// <summary>
        /// Returns the number of active touch contacts on the screen.
        /// Returns 0 on desktop (no touchscreen).
        /// </summary>
        public int GetTouchCount()
        {
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current == null) return 0;
            int count = 0;
            foreach (var touch in Touchscreen.current.touches)
                if (touch.press.isPressed) count++;
            return count;
#else
            return Input.touchCount;
#endif
        }

        /// <summary>
        /// Returns the screen-space midpoint between the first two active touch fingers.
        /// Falls back to the primary pointer position when fewer than two touches exist.
        /// </summary>
        public Vector2 GetTwoFingerMidpoint()
        {
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current == null) return GetPointerScreenPosition();
            Vector2 first = Vector2.zero, second = Vector2.zero;
            int found = 0;
            foreach (var touch in Touchscreen.current.touches)
            {
                if (!touch.press.isPressed) continue;
                found++;
                if (found == 1) first = touch.position.ReadValue();
                else if (found == 2) { second = touch.position.ReadValue(); break; }
            }
            return found >= 2 ? (first + second) * 0.5f : GetPointerScreenPosition();
#else
            if (Input.touchCount < 2) return GetPointerScreenPosition();
            return (Input.GetTouch(0).position + Input.GetTouch(1).position) * 0.5f;
#endif
        }

        /// <summary>
        /// Returns the change in distance between two active touch fingers this frame,
        /// suitable for pinch-to-zoom. Positive = fingers spreading (zoom in),
        /// negative = fingers pinching (zoom out). Returns 0 when fewer than 2 touches.
        /// </summary>
        public float GetPinchDelta()
        {
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current == null) return 0f;
            Vector2 firstPos = Vector2.zero, firstPrev = Vector2.zero;
            Vector2 secondPos = Vector2.zero, secondPrev = Vector2.zero;
            int found = 0;
            foreach (var touch in Touchscreen.current.touches)
            {
                if (!touch.press.isPressed) continue;
                Vector2 pos   = touch.position.ReadValue();
                Vector2 delta = touch.delta.ReadValue();
                found++;
                if      (found == 1) { firstPos  = pos; firstPrev  = pos - delta; }
                else if (found == 2) { secondPos = pos; secondPrev = pos - delta; break; }
            }
            if (found < 2) return 0f;
            return Vector2.Distance(firstPos, secondPos) - Vector2.Distance(firstPrev, secondPrev);
#else
            if (Input.touchCount < 2) return 0f;
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);
            float currentDist = Vector2.Distance(t0.position, t1.position);
            float prevDist    = Vector2.Distance(t0.position - t0.deltaPosition,
                                                  t1.position - t1.deltaPosition);
            return currentDist - prevDist;
#endif
        }
    }
}
