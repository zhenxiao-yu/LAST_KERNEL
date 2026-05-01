# LAST KERNEL — Input Controls

## Default Bindings

| Action                  | Default Key       | Description |
|-------------------------|-------------------|-------------|
| Toggle Ideas/Quests     | **Q**             | Open or close the side menu (Quests / Recipes tab) |
| Cycle Speed             | **TAB**           | Cycle game speed: Normal → Fast → Very Fast → Paused |
| Speed 1 — Normal        | **1**             | Set speed to Normal (1×) |
| Speed 2 — Fast          | **2**             | Set speed to Fast (2×) |
| Speed 3 — Very Fast     | **3**             | Set speed to Very Fast (5×) |
| Pause / Advance         | **SPACE**         | Advance end-of-day modal if one is active; otherwise toggle pause |
| Sell Hovered Card       | **BACKSPACE**     | Sell the card or stack currently under the cursor |
| Camera Up               | **W**             | Pan camera up (north) |
| Camera Left             | **A**             | Pan camera left (west) |
| Camera Down             | **S**             | Pan camera down (south) |
| Camera Right            | **D**             | Pan camera right (east) |
| Grab Whole Stack        | **Left Shift**    | Hold while left-clicking a card to grab the entire stack |
| Escape Menu             | **ESC**           | Open pause menu; if open, close it |

Mouse drag pan and scroll-wheel zoom are always active and cannot be rebound.

---

## How to Rebind Controls

1. In-game, press **ESC** to open the Pause Menu.
2. Click **Options**.
3. Click **CONTROLS** (the button added beside Close).
4. In the Controls screen, click **✎** next to the action you want to rebind.
5. Press the desired key. The binding updates immediately.
6. Click **↩** to reset a single binding to its default.
7. Click **RESET ALL** to restore all bindings to defaults.

---

## Where Bindings Are Saved

Overrides are stored in Unity `PlayerPrefs` under the key `"KeybindOverrides"` as a compact JSON string (Unity `InputActionRebindingExtensions` format).

- **Desktop**: platform-specific PlayerPrefs location (registry on Windows, plist on macOS).
- **WebGL**: browser local storage.
- Bindings survive application restarts and game updates.

---

## Architecture

| File | Role |
|------|------|
| `Runtime/Input/GameInputHandler.cs` | Owns all rebindable `InputAction` objects; polls them each frame and routes to game systems |
| `Runtime/Input/InputManager.cs` | Thin façade for pointer/touch/keyboard; new methods `IsShiftHeld()`, `GetCameraMoveInput()` delegate to `GameInputHandler` |
| `UI/Controllers/Settings/KeybindSettingsController.cs` | UI Toolkit panel for viewing and rebinding controls |
| `UI/UXML/Settings/KeybindSettingsView.uxml` | UXML layout for the keybind panel |

### Key flow

```
GameInputHandler.Update()
  ├─ _toggleIdeasQuests  → SideMenuController.Instance.Toggle()
  ├─ _cycleSpeed         → TimeManager.Instance.CycleTimePace()
  ├─ _setSpeed1/2/3      → TimeManager.Instance.SetTimePace()
  ├─ _pauseOrAdvance     → InfoPanelController.AdvanceCurrentModal()   [if modal active]
  │                         PauseMenu.Instance.Toggle()                [otherwise]
  ├─ _sellHoveredCard    → CardBuyer.TryTradeAndConsumeStack(HoveredCard.Stack)
  └─ (camera/shift)      → polled by CameraController / CardController via InputManager
```

ESC → `InputManager.WasPausePressedThisFrame()` → `PauseMenu.Update()` (existing flow, now routes through the rebindable `EscapeMenu` action when `GameInputHandler` is present).

---

## Scene Setup (Required Unity Editor Steps)

1. **Add `GameInputHandler`** to the scene (same GameObject as `InputManager` is fine).
2. **Add `KeybindSettingsController`**:
   - Create a new empty GameObject named `KeybindSettings`.
   - Add a `UIDocument` component; set its **Visual Tree Asset** to `KeybindSettingsView.uxml`.
   - Set the UIDocument **Sort Order** to `20` (above the game HUD).
   - Add `KeybindSettingsController` to the same GameObject.

No other wiring is needed — `GameOptionsUI` auto-discovers `KeybindSettingsController.Instance` at runtime.

---

## Known Limitations

- **WASD conflict with UI navigation**: The existing `InputSystem_Actions` asset binds WASD to the UI Navigate action map. During gameplay this is harmless (no uGUI selectable focused), but if a focusable uGUI element has keyboard focus the camera will move simultaneously.
- **Mobile / touch**: WASD camera pan and all keyboard shortcuts are desktop-only. Touch pan/zoom (one and two finger) is unchanged.
- **Sell via BACKSPACE**: Requires a `CardBuyer` component in the current scene. If no sell zone exists, the key silently does nothing.
- **Shift + drag on equipped / combat cards**: The whole-stack modifier is ignored for equipped and in-combat cards (they use their own drag paths unchanged).

---

## How to Add a New Input Action

1. Open `GameInputHandler.cs`.
2. Add a private `InputAction` field.
3. In `BuildActions()`, call `Add("ActionName", "Display Name", "<Keyboard>/key")`.
4. In `Update()`, respond to `_yourAction.WasPressedThisFrame()`.
5. The action is automatically available in the keybind settings screen with no extra work.
