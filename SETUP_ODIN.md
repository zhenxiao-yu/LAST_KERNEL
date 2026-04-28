# Odin Inspector Setup

Odin Inspector is a **required** dependency. The project **will not compile** without it.  
It is excluded from source control (`Assets/Plugins/` is in `.gitignore`) because it is a paid Asset Store package that cannot be redistributed.

## Install

1. Purchase **Odin Inspector & Serializer** on the Unity Asset Store (one-time purchase, all team seats covered by the same licence).  
   → https://assetstore.unity.com/packages/tools/utilities/odin-inspector-and-serializer-89041

2. Open Unity Hub → open this project in **Unity 6000.4.3f1** (or the version in `ProjectSettings/ProjectVersion.txt`).

3. In the Unity Editor menu: **Window → Package Manager → My Assets** → find Odin Inspector → **Download**, then **Import**.  
   Accept all default import locations (Odin installs into `Assets/Plugins/Sirenix/`).

4. Once imported, Unity will recompile. If there are no errors the setup is complete.

## Version

The project was last tested with **Odin Inspector 4.0.1.6**.  
Newer versions are generally backwards-compatible; avoid Odin 3.x.

## What Odin is used for

| Feature | Usage |
|---|---|
| `[BoxGroup]` | Replaces all `[Header]` attributes on MonoBehaviours and ScriptableObjects |
| `[TableList]` | Renders `List<T>` as a sortable inline table (card prefabs, SFX list, night wave enemies) |
| `[ShowIf]` / `[HideIf]` | Conditional field visibility (Quest type constraints, EncounterDefinition range) |
| `[Button]` | Editor-only helper buttons on ScriptableObjects (GameDatabase rebuild, StackingRulesMatrix bulk set) |
| `[ValidateInput]` | Inline inspector validation (AudioManager SFX list duplicates/missing clips) |
| `[Required]` | Highlights null object references in red |
| `[ReadOnly]` | Displays computed/runtime values in the inspector without allowing edits |
| `OdinEditor` | Base class for custom editors that still need Odin property drawers |

**Odin Serializer is NOT used.** All fields use standard Unity serialization (`[SerializeField]`). Odin attributes here are inspector-only and add zero runtime overhead.

## Compile guards

No `#if ODIN_INSPECTOR` guards are used. If Odin is missing the project simply will not compile — this is intentional, as a silent fallback would hide missing inspector validation that affects data integrity.

## gitignore entry

```
# Third-party plugins installed locally (e.g. Odin Inspector) — not committed to source control
/Assets/Plugins/
/Assets/Plugins.meta
```
