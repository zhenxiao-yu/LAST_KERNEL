# Architecture

This document is a lightweight map for active development. It records current ownership without proposing gameplay refactors.

## Card System

- Runtime code: `Assets/_Project/Scripts/Runtime/Cards/`
- Card definitions describe authoring data.
- Card instances and managers own runtime state, movement, stacking, equipment, feel presentation, and card UI helpers.
- Card data assets should live under `Assets/_Project/Data/Cards/` or the existing project Resources layout until a deliberate data migration is planned.

## Recipe System

- Runtime code: `Assets/_Project/Scripts/Runtime/Crafting/`
- Recipe definitions describe combinations and results.
- `RecipeMatcher` handles matching logic; managers/tasks coordinate gameplay use.
- Recipe assets should live under `Assets/_Project/Data/Recipes/` or the existing Resources layout until migrated safely.

## Colony System

- Runtime home: `Assets/_Project/Scripts/Runtime/Colony/`
- Colony-specific systems should be added here as they emerge.
- Shared board, day cycle, save, stats, and director code currently lives under `Assets/_Project/Scripts/Runtime/Core/`.

## Night Combat System

- Runtime code: `Assets/_Project/Scripts/Runtime/Night/` and `Assets/_Project/Scripts/Runtime/Combat/`
- Night phase code owns deployment, lanes, units, waves, and night-combat results.
- General combat rules, stats, hit results, and combat UI helpers live under Combat.
- Enemy data should live under `Assets/_Project/Data/Enemies/` when new data is organized.

## UI Layer

- Runtime code: `Assets/_Project/Scripts/Runtime/UI/`
- UI scripts should subscribe/unsubscribe cleanly to gameplay and localization events.
- Main-menu/title UI, gameplay HUD, modal windows, progress displays, and card stats presentation belong here.

## Input Layer

- Runtime home: `Assets/_Project/Scripts/Runtime/Input/`
- The project uses Unity's Input System package.
- Shared input orchestration currently includes `InputManager` under Core; future input-specific code should move into the Input folder during a deliberate refactor.

## Localization Layer

- Runtime code: `Assets/_Project/Scripts/Runtime/Localization/` and related Core bridge classes.
- Editor tooling: `Assets/_Project/Scripts/Editor/Localization/`
- Localization assets: `Assets/_Project/Localization/`
- Visible strings should go through localization keys instead of hardcoded UI text.

## Audio Layer

- Runtime home: `Assets/_Project/Scripts/Runtime/Audio/`
- Shared audio orchestration currently includes `AudioManager` under Core; future audio-specific features should use the Audio folder.
- Audio assets belong under `Assets/_Project/Audio/Music/`, `Assets/_Project/Audio/SFX/`, and `Assets/_Project/Audio/Mixers/`.
