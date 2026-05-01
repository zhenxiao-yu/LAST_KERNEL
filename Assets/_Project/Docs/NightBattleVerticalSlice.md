# Night Battle Vertical Slice

## What Was Added

| File | Type | Purpose |
|------|------|---------|
| `Scripts/Runtime/Night/NightPhaseManager.cs` | Modified | Added `OnNightPrepared` / `OnNightComplete` static events and three public control methods |
| `Scripts/Runtime/Night/UI/NightBattleHUDController.cs` | New | UI Toolkit controller — populates fighter cards, battle log, handles buttons |
| `UI/UXML/Game/NightBattleHUD.uxml` | New | Full-screen overlay layout |
| `UI/USS/NightBattleHUD.uss` | New | Cyberpunk terminal styling for all `.nb-*` classes |

No existing files were deleted. The original `CombatLaneView` (uGUI) is still used as a fallback when no HUD is present.

---

## How to Test (Play Mode)

### Scene setup (one-time)

1. Open **Game.unity**.
2. Create a new empty GameObject, name it **NightBattleHUD**.
3. Add component **UIDocument** → set *Source Asset* to `NightBattleHUD.uxml` → set *Sort Order* to `50`.
4. Add component **NightBattleHUDController**.
5. Ensure **NightPhaseManager** exists in the scene and has a wave assigned (or leave it blank for the procedural fallback).
6. Ensure **NightDeploymentController** exists and has `NightDeploymentView` wired (or leave blank for auto-deploy).

### Test path

1. Press **Play**.
2. Spawn some Villager / Worker cards on the board (the system auto-picks the first 5 eligible Character cards).
3. Wait for the day timer to expire **or** press keyboard **N** to trigger night early (calls `DayCycleManager` if wired to a button).
4. The deployment view opens — select defenders or click **SKIP (AUTO)**.
5. The `NightBattleHUD` overlay appears, showing both teams.
6. Click **[ START BATTLE ]** (or press **B**) to begin the tick loop.
7. Watch HP bars drain and log entries appear.
8. Click **[ RESOLVE FAST ]** at any time to collapse ticks to one-frame intervals.
9. When the battle ends, the result panel overlays the battle area.
10. Click **[ RETURN TO DAY ]** to dismiss the HUD and resume the day cycle.

### Debug hotkeys (Editor + Development builds)

| Key | Action |
|-----|--------|
| B | Confirm battle start (same as button) |
| V | Force Victory — ends the active lane immediately (defenders win) |
| L | Force Defeat — drains all defender HP, triggers end condition |

---

## Architecture

### Data flow

```
DayCycleManager.EncounterPhase()
  └─ NightDeploymentController.RunDeploymentPhase(eligibleCards)
       └─ NightDeploymentPlan (ordered list of CardInstances)
  └─ NightPhaseManager.RunNight(plan)
       ├─ Builds CombatUnit[] from plan (defenders) and wave (enemies)
       ├─ Creates CombatLane (pure C# simulation)
       ├─ Fires OnNightPrepared(lane, wave)          ← NightBattleHUDController shows UI
       ├─ Waits for ConfirmBattleStart()             ← player clicks Start Battle
       ├─ Tick loop until IsOngoing == false
       ├─ Fires OnNightComplete(result)              ← HUD shows result panel
       ├─ Waits for AcknowledgeResult()              ← player clicks Return to Day
       └─ ApplyAftermath() — kills dead CardInstances on board
  └─ RunStateManager.ApplyNightCombatResult(result)
       └─ Morale / Fatigue / Salvage / Casualties deltas applied
```

### Separation of concerns

| Layer | Class | Responsibility |
|-------|-------|---------------|
| Simulation | `CombatLane` | Pure C# tick loop — no Unity, no UI |
| Data | `CombatUnit` | Snapshot of one combatant's stats |
| Orchestration | `NightPhaseManager` | Coroutine, events, aftermath |
| Presentation | `NightBattleHUDController` | UI Toolkit overlay |
| State | `RunStateManager` | Morale / fatigue / salvage |
| Persistence | `DayCycleManager` | Wires everything together |

---

## How Villagers Become Fighters

`CombatUnit.FromCardInstance(card)` copies fields from `card.Stats` at battle start:

| CardDefinition field | CombatUnit field |
|----------------------|-----------------|
| `maxHealth` (via CurrentHealth) | `MaxHP / CurrentHP` |
| `attack` | `Attack` |
| `defense` | `Defense` |
| `attackSpeed` (%) | `AttackCooldown = 100 / speed` |
| `accuracy` | `AccuracyPercent` |
| `dodge` | `DodgePercent` |
| `criticalChance` | `CritChancePercent` |
| `criticalMultiplier` | `CritMultiplier` |

Any card with health and attack stats can become a defender. Category filtering (`Character`) is applied by `NightDeploymentPlan.BuildAutomatic()` and the deployment view.

---

## How Waves Are Defined

`NightWaveDefinition` ScriptableObjects live in `Assets/_Project/Data/Balance/Waves/`.  
Each has a list of `EnemyEntry { EnemyDefinition enemy; int count; }`.  
`NightPhaseManager.ResolveWave()` tries: inspector-assigned → `Resources/Waves/` → procedural fallback.

Enemy definitions live in `Assets/_Project/Data/Balance/Enemies/`.

---

## What Was Intentionally Not Reused

- **CombatManager / CombatTask** — day card-vs-card combat. Kept separate to avoid coupling the night lane's ordered-team model to the free-form board combat system.
- **CombatLaneView** — the old uGUI C#-built overlay. Still available as fallback when `NightBattleHUDController` is not in the scene; not removed.

---

## Known Limitations

- Fighter card icons / sprites not yet displayed (no `Sprite` field on `CombatUnit`; add later via `EnemyDefinition.artTexture`).
- No ability system is wired to the HUD yet. `CombatLane` events don't yet carry ability names.
- The `lk-hidden` class must be defined in `layout.uss` or `components.uss` — it is used by existing UXML so it should be present.
- USS relative paths (`../../USS/theme.uss`) depend on Unity resolving them from the UXML file's location. If Unity shows style warnings, reimport the UXML or update paths via the UI Builder.
- No `N` keyboard shortcut is provided for "Start Night" — that requires wiring into `DayCycleManager` or calling `DefensePhaseController.Instance.StartNight()` directly; add a `NightDebugTrigger` MonoBehaviour if needed.

---

## Next Steps

1. **Ability system**: extend `CombatUnit` with `List<string> AbilityIds` and hook into `CombatLane` tick events to fire them.
2. **Fighter sprites**: show `EnemyDefinition.sprite` / `CardDefinition` art in fighter cards.
3. **Formation UI**: let the player drag-reorder the defender line before clicking Start Battle.
4. **Reward spawning**: on victory, spawn a reward card via `CardManager.CreateCardInstance()` instead of just logging a delta.
5. **Animations**: short pulse on HP bar hit; shake on death; flash on crit.
6. **Audio**: call `AudioManager.Instance.PlaySFX(AudioId.Combat*)` from `HandleAttack` / `HandleUnitDied`.
