# LAST KERNEL — Claude Instructions (Optimized 2026)

## CORE PRINCIPLES

- Think before acting. Prefer planning over blind edits.
- Minimize file reads. Load only what is necessary.
- Optimize for correctness, not verbosity.
- Do not hallucinate architecture—verify from code.
- Prefer incremental, reversible changes.

---

## BEFORE ANY ACTION

1. Clarify the task scope.
2. Identify minimal required files.
3. Search → then read → then modify.

Do NOT:
- scan entire repo
- open large unrelated files
- guess system behavior without reading source

---

## FILE SCANNING RULES

### Start with:

- `Assets/_Project/`
- `Assets/StackCraft/Scripts/`
- `Assets/StackCraft/ScriptableObjects/`
- `Packages/manifest.json`
- `ProjectSettings/ProjectVersion.txt`

### Only read additional files if required by the task.

### NEVER scan:

- `Library/`
- `Temp/`
- `Logs/`
- `Obj/`
- `Build/` / `Builds/`
- `UserSettings/`
- `.csproj` / `.sln`
- Asset demo/sample folders

---

## ARCHITECTURE OVERVIEW

Unity: **6000.4.3f1**  
Type: **Pixel-art card survival / auto-battler**

Namespace:
`Markyu.LastKernel`

### Assemblies

- Runtime → `Assets/_Project/Scripts/Runtime/` → `_Project.Runtime`
- Editor → `Assets/_Project/Scripts/Editor/` → `_Project.Editor`
- EditMode tests → `_Project.Tests.EditMode`
- PlayMode tests → `_Project.Tests.PlayMode`

---

## PROJECT PRIORITIES (STRICT ORDER)

1. Stability (no gameplay breakage)
2. Data-driven systems (cards, packs, recipes, quests)
3. Localization (EN + Simplified Chinese)
4. Mobile compatibility (touch + performance)
5. Pixel-perfect UI & camera
6. Code clarity & maintainability

---

## SYSTEM ARCHITECTURE RULES

Separation of concerns:

- ScriptableObject → DATA ONLY
- Service / plain C# → GAME LOGIC
- MonoBehaviour → VIEW / INPUT / PRESENTATION
- Editor tools → VALIDATION / AUTOMATION

Do NOT:
- put gameplay logic in ScriptableObjects
- let UI mutate core game state directly
- couple systems via direct singleton calls when avoidable

---

## ODIN INSPECTOR RULES

Use Odin for:
- editor UI
- validation
- debug tools
- tables and inspectors

Use Odin Serializer ONLY when Unity cannot serialize:
- dictionaries
- polymorphic data

Do NOT:
- convert all MonoBehaviours to SerializedMonoBehaviour
- replace Unity serialization unnecessarily

---

## DOTWEEN RULES

- DOTween is the ONLY tween system
- Always use `.SetLink(gameObject)`
- Kill tweens on destroy
- Never tween physics state directly
- Never use `.material` (use MaterialPropertyBlock)

---

## UI DESIGN RULES

Before UI work, read:
`Assets/_Project/Docs/ART_DIRECTION.md`

Key constraints:

- Style: dark cyberpunk terminal (functional, not decorative)
- Resolution: 320×180 → 1920×1080 (integer scaling)
- Fonts: EN + CN, no hardcoded text
- Layout: allow 30–40% expansion
- Mobile: ≥44px touch targets, safe-area aware
- Animations: short, snappy, no bounce

---

## EDITING RULES

- Search before editing
- Modify only relevant files
- Avoid duplication of systems
- Avoid unrelated refactors
- Keep mouse + touch compatibility
- Preserve existing gameplay behavior

---

## PERFORMANCE RULES

- Avoid allocations in Update loops
- Avoid frequent GetComponent calls
- Prefer pooling for UI/effects
- Avoid excessive Resources.Load at runtime
- Keep mobile performance in mind

---

## VALIDATION

After structural changes:

Run:
`Tools → LAST KERNEL → Validate Project`

Fix:
- missing references
- duplicate IDs
- invalid data

---

## RESPONSE FORMAT

When making changes, ALWAYS:

1. Explain reasoning briefly
2. Show exact code changes (diff-style if possible)
3. List files modified
4. Note any required Unity Editor steps

---

## WHAT NOT TO DO

- Do not run git commands
- Do not modify .gitignore
- Do not delete assets blindly
- Do not move files outside Unity context
- Do not introduce new frameworks without justification
- Do not over-engineer systems

---

## WHEN UNSURE

- Ask for clarification
- Or propose 2–3 options with tradeoffs
- Do NOT guess and proceed blindly

---

## GOAL

Make the project:

- stable
- clean
- data-driven
- AI-friendly
- scalable for solo indie development