# All In 1 Sprite Shader — Integration Guide
> LAST KERNEL · Updated 2026-05-01

---

## 1. What Was Imported

Asset: **All In 1 Sprite Shader** (Seaside Game Studios)  
Location: `Assets/Plugins/AllIn1SpriteShader/`

| Folder | Keep for Release? | Notes |
|---|---|---|
| `Shaders/` | ✅ Yes | All 8 main shaders + HLSL includes |
| `Scripts/` | ✅ Yes | Runtime helpers (SetGlobalTime, SetAtlasUvs, etc.) |
| `Materials/` | ✅ Yes | 3 template materials |
| `Textures/` | ✅ Yes | Gradient ramps + normals |
| `Demo/` | ⚠️ Optional | 3 demo scenes, 20 demo materials, demo scripts. Safe to delete before shipping to reduce build size. |

**URP Support:** Full. Primary shader for this project: `AllIn1SpriteShader/AllIn1SpriteShaderSRPBatch`.  
**2D Renderer variant** also available: `AllIn1Urp2dRenderer.shader`.

---

## 2. What Was Integrated

### Architecture Decision

Cards in LAST KERNEL use **MeshRenderer + Card.shadergraph** (not SpriteRenderer).  
All In 1 Sprite Shader targets SpriteRenderer/UI Image — it **cannot replace** card materials.

**Integration approach: additive overlay only.**

A `CardFeedbackController` component manages two child `SpriteRenderer` GameObjects:
- `FX_Persistent` — hover outline, selected outline, rare pulse (persistent states)
- `FX_Flash` — damage flash, critical flash, healing pulse (transient events)

These renderers sit on top of the card visually, are disabled by default, and are completely separate from `Card.shadergraph`.

### Files Created / Modified

| File | Type | What It Does |
|---|---|---|
| `Assets/_Project/Scripts/Runtime/VFX/CardFeedbackController.cs` | New script | Manages overlay effects via All In 1 materials |
| `Assets/_Project/Materials/AllIn1/*.mat` | New materials (7) | Project-owned materials using palette |
| `Assets/_Project/Prefabs/VFX/CardFeedback/*.prefab` | New prefabs (4) | Test beds for each effect |
| `Assets/_Project/Scripts/Runtime/Cards/CardInstance.cs` | Modified | +5 lines: null-safe calls to `_feedbackController` |

---

## 3. Materials Created

All materials use `AllIn1SpriteShader/AllIn1SpriteShaderSRPBatch`.  
Located: `Assets/_Project/Materials/AllIn1/`

| Material | Effect | Palette Color | Intent |
|---|---|---|---|
| `LK_A1_HoverOutline_Cyan` | Thin outline, sprite hidden | Cyan `#00DCFF` | Cursor hover on any card |
| `LK_A1_SelectedOutline_Cyan` | Thicker outline, brighter glow | Cyan `#00DCFF` | Card lifted / selected |
| `LK_A1_DamageFlash_Red` | Hit effect overlay | Red `#CC2E2E` | Any damage taken |
| `LK_A1_CriticalFlash_Amber` | Hit effect, higher glow | Amber `#FFA028` | Critical hit |
| `LK_A1_RarePulse_Magenta` | Glow pulse, persistent | Magenta `#A03291` | Rare card ambient effect |
| `LK_A1_HealingPulse_Green` | Glow + soft hit tint | Green `#3FAF6F` | Healing received |
| `LK_A1_GhostPreview_Cyan` | Translucent cyan silhouette | Cyan `#00DCFF` | Drop-zone preview / ghost card |

**Fine-tuning:** Open any material in the Inspector. The All In 1 custom inspector shows all toggleable effects. The most useful parameters per material:
- Outline: `_OutlineWidth` (0–0.2), `_OutlineColor`, `_OutlineGlow`
- Flash: `_HitEffectBlend` (0–1), `_HitEffectColor`, `_HitEffectGlow`
- Glow: `_Glow` (0–100), `_GlowColor`
- Alpha: `_Alpha` (0–1) — general transparency

---

## 4. Scripts Created

### `CardFeedbackController.cs`
**Namespace:** `Markyu.LastKernel`  
**Path:** `Assets/_Project/Scripts/Runtime/VFX/CardFeedbackController.cs`

Optional component — add to card prefabs alongside `CardFeelPresenter` (not replacing it).

#### Inspector Fields

| Field | Purpose |
|---|---|
| `_persistentRenderer` | Auto-created child `SpriteRenderer`. Leave null for auto-setup. |
| `_flashRenderer` | Auto-created child `SpriteRenderer`. Leave null for auto-setup. |
| `hoverOutlineMaterial` | Assign `LK_A1_HoverOutline_Cyan` |
| `selectedOutlineMaterial` | Assign `LK_A1_SelectedOutline_Cyan` |
| `damageFlashMaterial` | Assign `LK_A1_DamageFlash_Red` |
| `criticalFlashMaterial` | Assign `LK_A1_CriticalFlash_Amber` |
| `healingPulseMaterial` | Assign `LK_A1_HealingPulse_Green` |
| `rarePulseMaterial` | Assign `LK_A1_RarePulse_Magenta` |
| `hoverFadeDuration` | Default: 0.08s. Keep under 0.1s. |
| `flashDuration` | Default: 0.12s. Keep under 0.2s. |
| `enableRarePulse` | Set true for rare/epic cards at spawn. |
| `_sortingOrderOffset` | Default: 50. Increase if overlay clips behind card layers. |

#### Public Methods

```csharp
feedbackController.SetHover(bool active)         // hover enter/exit
feedbackController.SetSelected(bool active)      // card lifted/dropped
feedbackController.PlayDamageFlash()             // any hit received
feedbackController.PlayCriticalFlash()           // critical hit
feedbackController.PlayHealingPulse()            // heal received
feedbackController.SetRarePulse(bool active)     // rare card ambient toggle
feedbackController.ClearAll()                    // reset everything (use on kill/disable)
```

#### State Priority

`Selected` overrides `Hover`. Flash effects run independently via `FX_Flash` renderer.  
When selected is cleared with `enableRarePulse = true`, the rare pulse restores automatically.

---

## 5. Where to Use All In 1 Sprite Shader

| Context | Use? | Notes |
|---|---|---|
| Card hover/select feedback | ✅ Yes | Via `CardFeedbackController` child overlay |
| Damage / critical flash | ✅ Yes | Via `CardFeedbackController.PlayDamageFlash()` |
| Healing feedback | ✅ Yes | Via `CardFeedbackController.PlayHealingPulse()` |
| Rare card ambient glow | ✅ Yes | Via `SetRarePulse(true)` at card spawn |
| Drop-zone ghost preview | ✅ Yes | Standalone `SpriteRenderer` with `LK_A1_GhostPreview_Cyan` |
| Combat projectile sprites | ✅ Yes | Attach to any `SpriteRenderer`-based VFX sprite |
| Particle system sprites | ✅ Yes | Assign material to particle system renderer |

---

## 6. Where NOT to Use All In 1 Sprite Shader

| Context | Use? | Reason |
|---|---|---|
| `Card.shadergraph` card body | ❌ Never | Would break existing feel system and art pipeline |
| Card frame materials | ❌ Never | Each card type has a distinct material; overwriting breaks visuals |
| Every card portrait at all times | ❌ Never | 50+ cards × animated shader = serious GPU cost |
| UI Toolkit `.uss` / `UIDocument` | ❌ Not supported | All In 1 targets SpriteRenderer and UI Image; USS styles are CSS-like and incompatible |
| Board / background materials | ❌ No | Not designed for world-space quads; stick to Card.shadergraph derivatives |
| MeshRenderer components in general | ⚠️ Incompatible | Shader is written for SpriteRenderer rendering path |

### UI Toolkit Note
All In 1 Sprite Shader **cannot be applied directly to UI Toolkit elements** (USS backgrounds, VisualElements).  
For UI hover effects, use USS transitions and palette color variables.  
For decorative VFX near UI panels, use a world-space `SpriteRenderer` or `RenderTexture` overlay — not direct UI element materials.

---

## 7. How to Add Effects to a Card Prefab

1. Open any card prefab (e.g. `Assets/_Project/Prefabs/Cards/Card_Character.prefab`)
2. Select the root GameObject
3. **Add Component → Last Kernel/VFX/Card Feedback Controller**
4. Assign materials in the inspector:
   - `Hover Outline Material` → `LK_A1_HoverOutline_Cyan`
   - `Selected Outline Material` → `LK_A1_SelectedOutline_Cyan`
   - `Damage Flash Material` → `LK_A1_DamageFlash_Red`
   - `Critical Flash Material` → `LK_A1_CriticalFlash_Amber`
   - `Healing Pulse Material` → `LK_A1_HealingPulse_Green`
   - `Rare Pulse Material` → `LK_A1_RarePulse_Magenta`
5. Leave renderer fields empty — child objects `FX_Persistent` and `FX_Flash` are auto-created at runtime
6. Also assign `_feedbackController` field in the `CardInstance` component to this new component
7. Save prefab

For **rare cards**: after spawning via `CardManager.CreateCardInstance`, call:
```csharp
if (card.Definition.Rarity >= CardRarity.Rare)
    card.GetComponent<CardFeedbackController>()?.SetRarePulse(true);
```

---

## 8. How to Trigger Feedback from Code

`CardInstance` already wires the following automatically:
```csharp
// Hover — called by Unity EventSystem
CardInstance.OnPointerEnter → feedbackController.SetHover(true)
CardInstance.OnPointerExit  → feedbackController.SetHover(false)

// Damage
CardInstance.TakeDamage(n)  → feedbackController.PlayDamageFlash()

// Healing
CardInstance.Heal(n)        → feedbackController.PlayHealingPulse()
```

For effects **not yet wired** (call manually from combat/ability code):
```csharp
// Critical hit
card.GetComponent<CardFeedbackController>()?.PlayCriticalFlash();

// Selected / lifted
card.GetComponent<CardFeedbackController>()?.SetSelected(true);
card.GetComponent<CardFeedbackController>()?.SetSelected(false);

// Clear on death (before Destroy)
card.GetComponent<CardFeedbackController>()?.ClearAll();
```

> **TODO:** `CardController` drag pickup/release could call `SetSelected(true/false)`. Wire in `CardController.OnPointerDown/Up` when ready — the hook exists, just not connected yet.
>
> **TODO:** `PlayCriticalFlash()` has no automatic caller yet. Wire from combat system when critical hit detection is implemented.

---

## 9. Performance Notes

- **Overlay disabled by default.** Zero rendering cost for cards with no active state.
- **Shared materials.** All instances of `LK_A1_HoverOutline_Cyan` reference the same material object. `SpriteRenderer.color` is per-component (not per-material), so multiple simultaneous hover states are safe and don't create material instances.
- **DOTween + SetLink.** All tweens are linked to the card GameObject — they auto-kill on destroy. No leak risk.
- **Max concurrent effects:** Rare pulse on 10–15 visible rare cards is the heaviest expected scenario. This is acceptable. Do not enable rare pulse on all cards.
- **50+ cards on board:** Flash effects are transient (~120ms). At most 2–3 cards flash simultaneously during combat. Persistent outlines only appear on hovered/selected cards (1–2 at a time).
- **Animated effects (pulse, glow):** Driven by `_Time` in the shader — zero CPU cost, runs on GPU.

### Safeguards
- Never enable animated shader effects on all cards at once
- Hover outline: ≤ 80ms fade
- Damage flash: ≤ 120ms
- Healing pulse: ≤ 240ms (2× flash)
- Rare pulse: reserved for Rare+ rarity only
- Avoid: distortion, chromatic aberration, blur, or large bloom

---

## 10. Limitations Discovered

1. **Cards use MeshRenderer, not SpriteRenderer.** All In 1 cannot be applied directly to card bodies. The overlay approach (child SpriteRenderer) is the correct workaround.
2. **Sorting order is set once in Awake** from the parent MeshRenderer's order at that moment. If `CardRenderOrderController` changes the card's sort order dynamically (hover → dragged transitions), the overlay's absolute order won't track perfectly. In practice this is invisible because the overlay's `_sortingOrderOffset` keeps it above the card body in all states. If you see clipping, increase `_sortingOrderOffset`.
3. **UI Toolkit is not supported.** CSS-based USS styles have no path to All In 1 materials. Use USS transitions for UI hover effects.
4. **No sprite assigned to overlay SpriteRenderers.** The overlay renders white because no sprite texture is assigned — the shader uses `_MainTex` which defaults to white. For outline effects this is correct (outline traces the white rect). For more complex card-shaped outlines, assign a card-silhouette sprite to the auto-created `FX_Persistent` child.
5. **`_OnlyOutline = 1` requirement for outline-only look.** If the outline materials show a white fill, verify `_OnlyOutline` is set to 1 in the material. The Unity inspector shows this as a checkbox in the All In 1 custom inspector.
