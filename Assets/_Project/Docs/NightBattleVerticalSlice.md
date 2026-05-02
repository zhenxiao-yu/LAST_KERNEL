# Night Battle Vertical Slice

## What Was Added / Modified

| File | Type | Purpose |
| --- | --- | --- |
| `Scripts/Runtime/Night/NightBattleManager.cs` | New | Primary orchestrator — coroutine, static events, gold economy, aftermath |
| `Scripts/Runtime/Night/NightFighter.cs` | New | Mutable prep-phase DTO; converts to `CombatUnit` at battle start |
| `Scripts/Runtime/Night/NightTeam.cs` | New | 5-slot ordered defender lineup; handles assign/evict logic |
| `Scripts/Runtime/Night/NightShopItemDefinition.cs` | New | ScriptableObject; defines cost, effect, `Apply()` dispatch |
| `Scripts/Runtime/Night/UI/NightBattleModalController.cs` | New | UI Toolkit controller for all three sub-phases (Prep → Shop → Battle → Result) |
| `Scripts/Runtime/Night/UI/NightPrepSlotView.cs` | New | One assignable battle slot in the player row |
| `Scripts/Runtime/Night/UI/NightShopItemView.cs` | New | One purchasable item row in the Night Shop panel |
| `UI/UXML/Game/NightBattleModal.uxml` | New | Full-screen overlay — top bar, battle zone, bottom panels, result overlay |
| `UI/USS/NightBattleModal.uss` | New | All `.nbm-*` styles; dark cyberpunk terminal aesthetic |
| `Scripts/Runtime/Core/DayCycleManager.cs` | Modified | `EncounterPhase()` now checks `NightBattleManager.Instance` first |
| `Scripts/Runtime/Night/NightPhaseManager.cs` | Modified | Added static events + control API (still used as fallback) |

Legacy files (`NightBattleHUD.uxml`, `NightBattleHUD.uss`, `NightBattleHUDController.cs`) were **not deleted** — they remain as a compile-time fallback.

---

## Scene Setup (One-Time)

Open **Game.unity** and add two GameObjects:

### 1. NightBattleModal (UI)

1. Create empty GameObject → name it **NightBattleModal**.
2. Add **UIDocument** → *Source Asset*: `NightBattleModal.uxml` → *Sort Order*: `60`.
3. Add **NightBattleModalController**.

### 2. NightBattleManager

1. Create empty GameObject → name it **NightBattleManager**.
2. Add **NightBattleManager**.
3. Optionally assign a **Shop Pool** (`List<NightShopItemDefinition>`) in the inspector.  
   If left empty, five built-in items are created at runtime as a fallback.
4. Set **Starting Gold** (default: 30) and **Shop Slot Count** (default: 4).

### Night Shop Items (ScriptableObjects)

Create assets: *Right-click Assets/_Project/Data/Balance/Shop/ → Create → LastKernel → Night Shop Item*

| Asset name | Effect | Cost | Requires Target |
| --- | --- | --- | --- |
| `ShopItem_ScrapBlade` | AddAttack (+3) | 10 | Yes |
| `ShopItem_PlatedVest` | AddMaxHealth (+5) | 8 | Yes |
| `ShopItem_EnergyDrink` | FullHeal | 6 | Yes |
| `ShopItem_HiredGuard` | HireGuard (4ATK/10HP) | 15 | No |
| `ShopItem_RepairKit` | FullHeal | 12 | Yes |

---

## How to Test (Play Mode)

1. Press **Play**.
2. Spawn some Villager / Worker cards on the board.
3. Wait for the day timer to expire (the `DayCycleManager` pipeline fires automatically).
4. The **Night Battle Modal** appears — you are now in **Prep Phase**.
5. **Assign defenders**: click a villager in the *Available Defenders* panel (left), then click an empty slot in the *Colony Line* row. Repeat for up to 5 slots.
6. **Buy items** (right panel): click an item, then (if it requires a target) click a fighter slot. Gold deducts immediately.
7. Click **[ START BATTLE ]** — the modal switches to **Battle Phase**; the tick loop runs automatically.
8. Optionally click **[ RESOLVE FAST ]** to collapse ticks to one-frame intervals.
9. When the battle ends, the **Result Panel** overlays the modal.
10. Click **[ RETURN TO DAY ]** — the coroutine resumes, aftermath is applied, and the day cycle continues.
11. Click **[ CANCEL ]** during Prep to auto-deploy all eligible defenders and skip the shop.

### Debug Hotkeys (Editor + Development Builds)

| Phase | Key | Action |
| --- | --- | --- |
| Prep | B | Confirm battle start (same as button) |
| Battle | V | Force Victory — ends lane immediately, defenders win |
| Battle | L | Force Defeat — drains all defender HP, triggers end condition |

---

## Architecture

### Data Flow

```text
TimeManager.OnDayEnded
  └─ DayCycleManager.EncounterPhase()
       ├─ Builds eligibleDefenders (living Character cards)
       │
       ├─ [PRIMARY] NightBattleManager.RunNight(eligibleDefenders)
       │    ├─ ResolveWave()                         ← picks NightWaveDefinition for current day
       │    ├─ PickShopItems()                       ← shuffles pool, takes shopSlotCount
       │    ├─ Fires OnNightModalOpened(context)     ← NightBattleModalController opens UI
       │    ├─ WaitUntil(battleConfirmed)            ← player clicks Start Battle
       │    │    [during wait: player assigns, buys items]
       │    ├─ Builds CombatLane from NightTeam + Wave
       │    ├─ Fires OnBattleStarted(lane, wave)     ← Controller switches to Battle phase
       │    ├─ Tick loop (fires lane events per tick)
       │    │    OnAttackResolved → Controller updates HP bars + log
       │    │    OnUnitDied      → Controller marks slot dead
       │    ├─ Fires OnBattleComplete(result)        ← Controller shows result panel
       │    ├─ WaitUntil(resultAcknowledged)         ← player clicks Return to Day
       │    └─ ApplyAftermath()                      ← kills dead CardInstances on board
       │
       ├─ [FALLBACK] NightPhaseManager.RunNight(plan)   ← if NightBattleManager not in scene
       │
       └─ [LEGACY]  EncounterManager fallback           ← if neither manager is in scene
```

### Separation of Concerns

| Layer | Class | Responsibility |
| --- | --- | --- |
| Simulation | `CombatLane` | Pure C# tick loop — no Unity, no UI |
| Data | `CombatUnit` | Immutable snapshot of one combatant's stats |
| Prep data | `NightFighter` | Mutable DTO; accumulates shop buffs before battle |
| Lineup | `NightTeam` | 5-slot ordered list; eviction / swap logic |
| Shop data | `NightShopItemDefinition` | ScriptableObject; cost, effect, `Apply()` |
| Orchestration | `NightBattleManager` | Coroutine pipeline, static events, gold, aftermath |
| Presentation | `NightBattleModalController` | All UI logic across all sub-phases |
| State | `RunStateManager` | Morale / fatigue / salvage deltas |
| Cycle | `DayCycleManager` | Top-level night integration via priority check |

---

## Click-to-Assign State Machine

The controller uses a `PrepInteraction` enum to track click intent:

```text
Idle
  │  player clicks a villager entry
  ▼
AwaitingSlot
  │  player clicks an empty slot  →  assigns fighter, returns to Idle
  │  player clicks the same villager  →  cancels, returns to Idle
  │  player clicks another villager  →  switches selection (stays in AwaitingSlot)
  │
  └─ (if an item requiring a target was selected instead)
AwaitingTarget
  │  player clicks a filled slot  →  applies item effect to that fighter
  └─ returns to Idle
```

Shop items that do NOT require a target (`HireGuard`) apply immediately on click and try to fill the first empty slot.

---

## How Villagers Become Fighters

`NightFighter.FromCard(card)` snapshots stats at prep time:

| `CardDefinition` field | `NightFighter` field |
| --- | --- |
| `Id` | `CardId` |
| `DisplayName` | `DisplayName` |
| `maxHealth` (via CurrentHealth) | `BaseMaxHealth` |
| `attack` | `BaseAttack` |
| `defense` | `Defense` |
| `attackSpeed` | `AttackSpeed` |

Shop buffs accumulate in `BonusAttack` / `BonusMaxHealth`. At battle start, `NightFighter.ToCombatUnit()` produces the final immutable `CombatUnit`.

---

## How Waves Are Defined

`NightWaveDefinition` ScriptableObjects live in `Assets/_Project/Data/Balance/Waves/`.  
Each has a list of `EnemyEntry { EnemyDefinition enemy; int count; }`.  
`Game.unity` wires `NightBattleManager.wavePool` to `Wave_Night1` through `Wave_Night5`.
`NightBattleManager.ResolveWave()` tries: `defaultWave` override → `wavePool` by day → `Resources/Waves/` → procedural fallback.

Enemy definitions live in `Assets/_Project/Data/Balance/Enemies/`.

---

## Gold Economy

- `startingGold` is the minimum guaranteed shop budget.
- At night start, `PlayerGold = max(startingGold, CardManager.GetStatsSnapshot().Currency)`.
- `TrySpendGold(int cost)` rejects negative or insufficient costs; the modal disables unaffordable items.

---

## What Was Intentionally Not Reused

- **CombatManager / CombatTask** — day card-vs-card combat. Kept separate; the ordered-lane model is incompatible with free-form board combat.
- **DefensePhaseController / NightBattlefieldController** — an independent scene-specific system. Not broken, not coupled.
- **NightDeploymentController** — still used by `NightPhaseManager` (fallback path); not needed by the modal.

---

## Known Limitations

- Fighter card art / sprites are not shown (no `Sprite` field on `CombatUnit`; add via `EnemyDefinition` / `CardDefinition` later).
- No ability system in this slice. `CombatLane` events don't carry ability names yet.
- `lk-hidden { display: none; }` must exist in `layout.uss` or `components.uss` (used by existing UXML so assumed present).
- USS relative paths (`../../USS/theme.uss`) are resolved from the UXML file's location. Reimport if Unity shows style warnings.

---

## Next Steps

1. **Persistent shop economy**: decide whether night purchases consume board currency cards/chest coins or remain a per-night tactical budget.
2. **Fighter sprites**: display `CardDefinition` art inside prep slots and battle cards.
3. **Ability system**: extend `CombatUnit` with `List<string> AbilityIds`; hook `CombatLane` tick events to fire them.
4. **Drag-to-assign**: upgrade from click-to-assign to drag-and-drop for richer UX.
5. **Animations**: HP bar pulse on hit; slot shake on death; flash on crit.
6. **Audio**: call `AudioManager` SFX from `HandleAttack` / `HandleUnitDied` in the controller.
7. **Reward presentation**: surface salvage earned during the victory/result flow.
