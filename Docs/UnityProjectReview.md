# Unity Project Review

## Current state

- Unity version: `6000.4.3f1`
- Render pipeline: URP 2D template
- Input: new Input System is active
- Project-specific code: almost empty
- Third-party content: already imported under `Assets/ThirdParty` and `Assets/vFolders`

## Good calls already in place

- `Assets/_Project` already separates game-owned content from imported assets.
- `.gitignore` and `.gitattributes` are close to modern Unity defaults.
- The project is still early enough to put strong structure in place before gameplay code spreads.

## Findings

1. `ProjectSettings/ProjectSettings.asset` still referenced the template scene path `Assets/Scenes/SampleScene.unity` even though build settings use `Assets/_Project/Scenes/SampleScene.unity`. That mismatch was fixed.
2. `ProjectSettings/EditorSettings.asset` now uses `Markyu.FortStack` as the default root namespace so new scripts start cleaner.
3. `PlayerSettings` now use `Markyu` and `FortStack`. Bundle IDs and save-data paths should still be sanity-checked before your first public build.
4. `Packages/manifest.json` includes packages that may be optional for this game:
   - `com.unity.collab-proxy`
   - `com.unity.multiplayer.center`
   - `com.unity.visualscripting`
   - `com.unity.timeline`
   - `com.unity.2d.tilemap` and `com.unity.2d.tilemap.extras` if your board is card-only
5. `Assets/ThirdParty` contains large demo/sample content from Feel and related plugins. Keep it isolated, and prune demo scenes before production if you want faster imports and a cleaner hierarchy.

## Recommended production layout

```text
Assets/
  _Project/
    Art/
    Audio/
    Data/
    Prefabs/
    Scenes/
    ScriptableObjects/
    Scripts/
      Runtime/
      Editor/
    UI/
  ThirdParty/
  vFolders/
```

## Stacklands-like architecture suggestion

Use `ScriptableObjects` for card definitions, recipes, loot tables, biome data, and progression unlocks. Keep the actual board logic in runtime services instead of on individual card views.

Suggested runtime domains:
- `Cards` for card definitions, card state, and view presenters
- `Board` for stacking, placement, collision, and merge/recipe checks
- `Systems` for day cycle, crafting resolution, economy, spawning, and win/lose flow
- `Save` for profile persistence and run snapshots
- `UI` for HUD, tooltips, inspectors, and drag previews

## Boilerplate to remove later, not now

- Any leftover template identifiers outside the core project settings
- Unused Unity packages
- Vendor demo scenes and example assets you are not actively learning from
- Legacy placeholder script folders once real runtime/editor folders are in use
