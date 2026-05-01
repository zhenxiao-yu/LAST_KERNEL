# LAST KERNEL

> Cyberpunk survival strategy game
> Build the system. Watch it run. Survive the night.

---

## Overview

**LAST KERNEL** is a hybrid system-driven game combining:

- Card-based colony simulation (day phase)
- Auto-battler defense (night phase)

You don't control units directly.
You design a system that either holds — or collapses.

---

## Core Gameplay Loop

```
Start Run
  ↓
Day Phase (pauseable)
  ↓
Gather → Combine → Build → Assign
  ↓
Prepare defenses
  ↓
Night Phase (auto combat)
  ↓
Resolve outcome
  ↓
Repeat (increasing difficulty)
```

---

## Game Design

### Day Phase — System Building

Everything is a **card**:

- Resources (scrap, energy, food)
- Units (workers, defenders)
- Structures (generators, labs, defenses)

Cards interact through **stacking**:

- Worker + Resource → production
- Resource + Structure → crafting
- Unit + Equipment → upgrades

You are building a **self-sustaining system**, not just placing objects.

### Night Phase — Auto Battler

At night, the system runs on its own:

- Units auto-fight
- Defenses trigger
- Buffs and modifiers apply

Combat is real-time, deterministic, and hands-off.
Your preparation determines the outcome.

---

## Project Structure

```
Assets/
 ├── _Project/
 │   ├── Scripts/
 │   │   ├── Runtime/              ← assembly: _Project.Runtime
 │   │   │   ├── Core/             (GameDirector, RunStateManager, SaveSystem, DayCycleManager, Board)
 │   │   │   ├── Cards/            (CardInstance, CardManager, CardView, CardStack, Definitions)
 │   │   │   ├── Combat/           (CombatManager, CombatRules, Encounter)
 │   │   │   ├── Crafting/         (recipe matching, output resolution)
 │   │   │   ├── Defense/          (defense placement, trigger logic)
 │   │   │   ├── Input/            (New Input System wrappers)
 │   │   │   ├── Localization/     (key-based runtime switching, EN/CN)
 │   │   │   ├── Night/            (night phase orchestration)
 │   │   │   ├── Packs/            (PackDefinition, PackSlot, PackInstance)
 │   │   │   ├── Quests/           (Quest, QuestInstance, QuestManager)
 │   │   │   ├── Trading/          (market / trading logic)
 │   │   │   ├── Audio/            (AudioManager, SFX, music)
 │   │   │   └── UI/               (HUD, panels, menus, UIEventBus)
 │   │   └── Editor/               ← assembly: _Project.Editor
 │   │       (validators, custom inspectors, dev tools)
 │   ├── Art/
 │   ├── Audio/
 │   ├── Data/                     (ScriptableObject assets)
 │   ├── Docs/
 │   └── Scenes/
 │       ├── Boot.unity
 │       ├── MainMenu.unity
 │       ├── Game.unity
 │       └── Island.unity
 └── ThirdParty/
```

---

## Systems Overview

### Card System

All gameplay entities are cards defined as `CardDefinition` ScriptableObjects.

```
CardDefinition
 ├── ID (string)
 ├── Type (Resource / Unit / Structure / …)
 ├── Tags
 ├── Stats
 └── Localization Key
```

Runtime state lives in `CardInstance`. Visual presentation is handled by `CardView`.

### Pack System

Cards are distributed through packs (`PackDefinition`).
Each pack has weighted `PackEntry` slots that resolve to card draws at runtime.

### Recipe / Crafting System

```
Input Cards → Match RecipeDefinition → Output Cards
```

Matching is tag and type aware. Output is resolved by the crafting service, not by UI.

### Colony System

`RunStateManager` tracks the live run state:

- Resources and stockpiles
- Population and morale
- Day count and difficulty scaling
- Progression flags

### Combat System

`CombatManager` runs a tick-based, deterministic simulation:

- Independent from UI
- Encounter definitions drive enemy waves
- `HitResult` carries per-tick outcome data

### Quest System

`QuestManager` owns active `QuestInstance` objects.
Quests are defined as `Quest` ScriptableObjects and evaluated against run-state conditions.

### Localization

Key-based, runtime-switchable. Supports English and Simplified Chinese.
All UI text goes through localization keys — no hardcoded strings.

### Audio

Managed through `AudioManager`. SFX and music are separate concerns.
DOTween is used for all audio fades and transitions.

### UI

Event-driven via `UIEventBus` / `UIEventBusBridge`.
Screens: `DayHUD`, `NightHUD`, `PauseMenu`, `VictoryPanel`, `DefeatPanel`, `InfoPanel`, `ModalWindow`.
Safe-area aware (`SafeAreaAnchor`) for mobile.

---

## Architecture Principles

| Layer | Responsibility |
| --- | --- |
| `ScriptableObject` | Data only — no logic |
| Service / plain C# | Game logic |
| `MonoBehaviour` | View, input, presentation |
| Editor scripts | Validation, automation, dev tools |

- DOTween is the only tween system. Always `.SetLink(gameObject)`.
- No singleton coupling between systems where avoidable.
- Namespace: `Markyu.LastKernel`

---

## Tech Stack

| Tool | Purpose |
| --- | --- |
| Unity 6000.4.3f1 | Engine (URP 2D) |
| C# | All game logic |
| Unity Localization | EN / CN text |
| Addressables | Asset loading |
| New Input System | Mouse + touch input |
| DOTween | All animations and tweens |
| Odin Inspector | Editor UI and validation |

---

## Installation

Requirements: Unity 6000.4.x, Git

```sh
git clone https://github.com/zhenxiao-yu/LAST_KERNEL.git
```

Open in Unity Hub → Add Project → Select folder.

---

## Running

Open `Assets/_Project/Scenes/Boot.unity` and press Play.
The boot scene auto-loads into `MainMenu` → `Game`.

---

## Validation

After structural changes, run:

**Tools → LAST KERNEL → Validate Project**

This checks for missing references, duplicate IDs, and invalid data.

---

## Roadmap

- More cards and synergies
- Enemy scaling and difficulty curves
- Events system
- Meta progression (unlocks, runs)
- Mobile polish (touch, safe area, performance)
- Full UI pass

---

## Author

Zhenxiao (Mark) Yu
