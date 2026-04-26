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

        /// <summary>
        /// Returns the current pointer position in screen space.
        /// </summary>
        public Vector2 GetPointerScreenPosition()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
            return Input.mousePosition;
#endif
        }

        /// <summary>
        /// Returns true on the frame the primary pointer button is pressed.
        /// </summary>
        public bool WasPrimaryPointerPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }

        /// <summary>
        /// Returns true on the frame the primary pointer button is released.
        /// </summary>
        public bool WasPrimaryPointerReleasedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
#else
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

        /// <summary>
        /// Returns true on the frame the pause shortcut is pressed.
        /// </summary>
        public bool WasPausePressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }
    }
}

