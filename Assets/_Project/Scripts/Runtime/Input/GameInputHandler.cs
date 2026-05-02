using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Markyu.LastKernel
{
    public sealed class ActionEntry
    {
        public readonly string DisplayName;
#if ENABLE_INPUT_SYSTEM
        public readonly InputAction Action;
        public ActionEntry(string displayName, InputAction action) { DisplayName = displayName; Action = action; }
#else
        public ActionEntry(string displayName) { DisplayName = displayName; }
#endif
    }

    public sealed class GameInputHandler : MonoBehaviour
    {
        public static GameInputHandler Instance { get; private set; }

        // Delegate bridges — wired by UIToolkit controllers to avoid a circular assembly reference.
        public static Func<bool> HasActiveModal { get; set; }
        public static Action     AdvanceModal   { get; set; }

        private const string OverridesKey = "KeybindOverrides";

#if ENABLE_INPUT_SYSTEM
        private InputActionMap _map;

        private InputAction _toggleIdeasQuests;
        private InputAction _cycleSpeed;
        private InputAction _setSpeed0;
        private InputAction _setSpeed1;
        private InputAction _setSpeed2;
        private InputAction _setSpeed3;
        private InputAction _pauseOrAdvance;
        private InputAction _sellHoveredCard;
        private InputAction _cameraUp;
        private InputAction _cameraLeft;
        private InputAction _cameraDown;
        private InputAction _cameraRight;
        private InputAction _grabWholeStack;
        private InputAction _escapeMenu;

        private readonly List<ActionEntry> _entries = new();
        public IReadOnlyList<ActionEntry> AllActions => _entries;

        public static bool IsShiftHeld =>
            Instance != null &&
            Instance._grabWholeStack != null &&
            Instance._grabWholeStack.IsPressed();

        public static Vector2 CameraMoveInput
        {
            get
            {
                if (Instance == null ||
                    Instance._cameraRight == null ||
                    Instance._cameraLeft == null ||
                    Instance._cameraUp == null ||
                    Instance._cameraDown == null)
                {
                    return Vector2.zero;
                }

                float x = (Instance._cameraRight.IsPressed() ? 1f : 0f)
                         - (Instance._cameraLeft .IsPressed() ? 1f : 0f);
                float y = (Instance._cameraUp   .IsPressed() ? 1f : 0f)
                         - (Instance._cameraDown .IsPressed() ? 1f : 0f);
                return new Vector2(x, y);
            }
        }

        public static bool WasEscapePressedThisFrame =>
            Instance != null &&
            Instance._escapeMenu != null &&
            Instance._escapeMenu.WasPressedThisFrame();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            BuildActions();
            LoadBindings();
            _map.Enable();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            _map?.Disable();
        }

        private InputAction Add(string name, string displayName, string binding)
        {
            var action = _map.AddAction(name, InputActionType.Button);
            action.AddBinding(binding);
            _entries.Add(new ActionEntry(displayName, action));
            return action;
        }

        private void BuildActions()
        {
            _map = new InputActionMap("Gameplay");

            _toggleIdeasQuests = Add("ToggleIdeasQuests", "Toggle Ideas/Quests", "<Keyboard>/q");
            _cycleSpeed        = Add("CycleSpeed",        "Cycle Speed",         "<Keyboard>/tab");
            _setSpeed1         = Add("SetSpeed1",         "Speed 1×",            "<Keyboard>/1");
            _setSpeed2         = Add("SetSpeed2",         "Speed 2×",            "<Keyboard>/2");
            _setSpeed3         = Add("SetSpeed3",         "Speed 3×",            "<Keyboard>/3");
            _setSpeed0         = Add("SetSpeed0",         "Speed 0× (Pause)",    "<Keyboard>/4");
            _pauseOrAdvance    = Add("PauseOrAdvance",    "Pause / Advance",      "<Keyboard>/space");
            _sellHoveredCard   = Add("SellHoveredCard",   "Sell Hovered Card",    "<Keyboard>/backspace");
            _cameraUp          = Add("CameraUp",          "Camera Up",            "<Keyboard>/w");
            _cameraLeft        = Add("CameraLeft",        "Camera Left",          "<Keyboard>/a");
            _cameraDown        = Add("CameraDown",        "Camera Down",          "<Keyboard>/s");
            _cameraRight       = Add("CameraRight",       "Camera Right",         "<Keyboard>/d");
            _grabWholeStack    = Add("GrabWholeStack",    "Grab Whole Stack",     "<Keyboard>/leftShift");
            _escapeMenu        = Add("EscapeMenu",        "Escape Menu",          "<Keyboard>/escape");
        }

        private void Update()
        {
            // SPACE advances modals even when the board input lock is active (e.g. end-of-day phase).
            if (_pauseOrAdvance.WasPressedThisFrame() && HasActiveModal?.Invoke() == true)
            {
                AdvanceModal?.Invoke();
                return;
            }

            if (InputManager.Instance == null || !InputManager.Instance.IsInputEnabled) return;

            if (_toggleIdeasQuests.WasPressedThisFrame())
                SideMenuController.Instance?.Toggle();

            if (_cycleSpeed.WasPressedThisFrame())
                TimeManager.Instance?.CycleTimePace(out _);

            if (_pauseOrAdvance.WasPressedThisFrame())
                ToggleTimePause();

            if (_setSpeed0.WasPressedThisFrame())
                TimeManager.Instance?.SetTimePace(TimePace.Paused);

            if (_setSpeed1.WasPressedThisFrame())
                TimeManager.Instance?.SetTimePace(TimePace.Normal);

            if (_setSpeed2.WasPressedThisFrame())
                TimeManager.Instance?.SetTimePace(TimePace.Fast);

            if (_setSpeed3.WasPressedThisFrame())
                TimeManager.Instance?.SetTimePace(TimePace.VeryFast);

            if (_sellHoveredCard.WasPressedThisFrame())
                TrySellHoveredCard();
        }

        private static void TrySellHoveredCard()
        {
            CardInstance hovered = CardFeelPresenter.HoveredCard;
            if (hovered == null || hovered.Stack == null) return;

            CardBuyer buyer = FindAnyObjectByType<CardBuyer>();
            buyer?.TryTradeAndConsumeStack(hovered.Stack);
        }

        private static void ToggleTimePause()
        {
            TimeManager timeManager = TimeManager.Instance;
            if (timeManager == null)
                return;

            timeManager.SetTimePace(
                timeManager.CurrentPace == TimePace.Paused
                    ? TimePace.Normal
                    : TimePace.Paused);
        }

        public void SaveBindings()
        {
            string json = InputActionRebindingExtensions.SaveBindingOverridesAsJson(_map);
            PlayerPrefs.SetString(OverridesKey, json);
            PlayerPrefs.Save();
        }

        public void LoadBindings()
        {
            string json = PlayerPrefs.GetString(OverridesKey, string.Empty);
            if (!string.IsNullOrEmpty(json))
                InputActionRebindingExtensions.LoadBindingOverridesFromJson(_map, json);
        }

        public void ResetAllBindings()
        {
            InputActionRebindingExtensions.RemoveAllBindingOverrides(_map);
            PlayerPrefs.DeleteKey(OverridesKey);
            PlayerPrefs.Save();
        }

        public void ResetBinding(InputAction action)
        {
            InputActionRebindingExtensions.RemoveAllBindingOverrides(action);
            SaveBindings();
        }

#else
        public IReadOnlyList<ActionEntry> AllActions => new List<ActionEntry>();
        public static bool    IsShiftHeld              => false;
        public static Vector2 CameraMoveInput          => Vector2.zero;
        public static bool    WasEscapePressedThisFrame => false;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        public void SaveBindings()    { }
        public void LoadBindings()    { }
        public void ResetAllBindings() { }
#endif
    }
}
